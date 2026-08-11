// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion (the reflection bridge — Phase 4).
//
// Go's abi.TypeOf reads an interface's type-word via unsafe.Pointer to reach a Go runtime type
// descriptor that has no managed equivalent: an `any` here is a single System.Object reference, not
// a two-word eface over a descriptor, so reinterpreting the reference reads garbage and NREs (the
// first operational hit is fmt.Print/Sprint → doPrint → reflect.TypeOf(arg).Kind()). Instead,
// synthesize an abi.Type descriptor whose Kind_ is classified from the value's managed System.Type
// (golib GoReflect.KindOf), and record the System.Type on the descriptor box so the hand-owned
// reflect Type/Value methods can recover Go type info from it. The converter skips the auto form of
// TypeOf via the manualConversionFuncs registry (go2cs/manualTypeOperations.go); this module marker
// also makes go2cs skip re-converting this file wholesale, and the overlay restores it over auto
// output on every reconversion. See docs/phase4/DESIGN-reflection-bridge.md.

[module: GoManualConversion]

namespace go.@internal;

partial class abi_package {

// The managed System.Type this synthetic abi.Type stands for — carried directly on the descriptor so
// the reflect Type methods (String/Name/Elem/Field) can recover Go type info from it (the reflect
// rtype wraps an abi.Type by value, so the field rides along the copy). Null for a non-synthesized Type.
// arrayDims is NON-IDENTITY cargo (increment 2): the Go array length(s) when the descriptor stands for
// an array type AND a source knew them (a live value, a declaring struct's zero-instance field
// initializers, a pointee behind an addressable Value) — reflect.Type interning stays keyed on the
// System.Type alone, so identity is deliberately length-blind (the recorded §5 limitation) while
// Len()/Size()/New/Zero consume real lengths wherever one is knowable. Null = unknown ([0]T is [0]).
// funcParamDims is the same NON-IDENTITY cargo one level out: a FUNC descriptor's per-parameter
// array dims, one entry per Go parameter and null where that parameter is not a fixed-size array.
// It exists because the parameter position is the one place no other dims source reaches — a
// `[32]byte` parameter has no value to measure and no field initializer to read, and the emitted
// delegate type is a bare Func<array<byte>, bool> that func([32]byte) bool and func([64]byte) bool
// share — so the converter stamps [GoArrayDims] on the parameter and GoReflect.FuncParamDims reads
// it back off the delegate INSTANCE here. RESULT dims are deliberately not carried: a multi-result
// Go func returns a ValueTuple, which has no per-element attribute position, and no measured
// consumer reads Out(i).Len().
partial struct Type {
    [GoReflectCompanion] public System.Type? sysType;
    [GoReflectCompanion] public nint[]? arrayDims;
    [GoReflectCompanion] public nint[]?[]? funcParamDims;
}

// synthType builds a managed-backed abi.Type from a System.Type: Kind_ classified from it (GoReflect),
// the System.Type carried on the descriptor, Go size/alignment stamped when knowable (binary's sizeof
// reads Size_ for the scalar kinds), and array dims carried as cargo when the caller knew them. The
// single builder behind both TypeOf (from a value) and reflect's Type.Elem/Field (from a type).
public static ж<Type> synthType(System.Type? st) {
    return synthType(st, null, null);
}

public static ж<Type> synthType(System.Type? st, nint[]? arrayDims) {
    return synthType(st, arrayDims, null);
}

// Descriptors are immutable once synthesized — intern them per (managed type, dims) so the
// per-TypeOf cost (kind classification + size stamping's field walk) is paid once, not per
// call (fmt classifies every argument; binary sizes in loops). The func param dims join the key
// for the same reason the array dims are in it: func([32]byte) bool and func([64]byte) bool are
// DISTINCT Go types over ONE managed delegate type, so interning them together would let the
// first to arrive answer In(0).Len() for both.
private static readonly System.Collections.Concurrent.ConcurrentDictionary<(System.Type, string), ж<Type>> s_descriptors = new();

public static ж<Type> synthType(System.Type? st, nint[]? arrayDims, nint[]?[]? funcParamDims) {
    if (st is null) {
        return default!;
    }
    string dimsKey = descriptorDimsKey(arrayDims, funcParamDims);
    return s_descriptors.GetOrAdd((st, dimsKey), _ => synthesizeDescriptor(st, arrayDims, funcParamDims));
}

// descriptorDimsKey renders the descriptor's dims cargo as the interning key's second component —
// shared with reflect's canonType so a Type wrapper and the descriptor it wraps are interned under
// the same knowledge classes.
public static string descriptorDimsKey(nint[]? arrayDims, nint[]?[]? funcParamDims) {
    string key = arrayDims is null ? "" : string.Join(',', arrayDims);
    if (funcParamDims is null) {
        return key;
    }
    var builder = new System.Text.StringBuilder(key);
    foreach (nint[]? paramDims in funcParamDims) {
        builder.Append('|');
        if (paramDims is not null) {
            builder.Append(string.Join(',', paramDims));
        }
    }
    return builder.ToString();
}

private static ж<Type> synthesizeDescriptor(System.Type st, nint[]? arrayDims, nint[]?[]? funcParamDims) {
    ref var t = ref heap<Type>(out var Ꮡt);
    t.Kind_ = (ΔKind)((uint8)GoReflect.KindOf(st));
    t.sysType = st;
    t.arrayDims = arrayDims;
    t.funcParamDims = funcParamDims;
    nint size = GoReflect.GoSizeOf(st, arrayDims);
    if (size >= 0) {
        t.Size_ = (uintptr)(nuint)size;
        nint align = GoReflect.GoAlignOf(st);
        t.Align_ = (uint8)align;
        t.FieldAlign_ = (uint8)align;
    }
    // Carry Go comparability on the descriptor: reflect.Type.Comparable and internal/reflectlite's
    // Comparable both report `Equal != nil`, and errors.Is gates its equality match on the latter — so a
    // comparable Go type (e.g. the *errorString behind a sentinel like csv.ErrFieldCount) must have a
    // non-nil Equal or errors.Is(err, sentinel) silently returns false. A synthetic descriptor carries no
    // addressable value memory, so this is a comparability signal, not a real bit-compare; the delegate
    // compares its pointer arguments as a safe, non-throwing fallback should any path invoke it directly.
    if (GoReflect.IsComparable(st)) {
        t.Equal = static (p, q) => AreEqual(p, q);
    }
    return Ꮡt;
}

// TypeOf returns the abi.Type of some value. The descriptor stands for the value's GO dynamic
// type: an interface-carrier wrapper (IжAdapter / IInterfaceAdapter chain) unwraps to the *T box
// / original value it stands for (GoReflect.GoDynamicTypeOf, R10), so adapter-held and raw-box
// values of one Go type share one canonical descriptor. A live array value reveals its real
// dims, carried on the descriptor as non-identity cargo (increment 2) — and a live FUNC value
// reveals its parameters' dims the same way, off the [GoArrayDims] the converter stamped, which is
// the only route by which a `[32]byte` PARAMETER's length reaches reflect at all.
public static ж<Type> TypeOf(any a) {
    if (a == default!) {
        return default!;
    }
    System.Type dyn = GoReflect.GoDynamicTypeOf(a);
    nint kind = GoReflect.KindOf(dyn);
    nint[]? dims = kind == GoReflect.Array ? GoReflect.ArrayDimsOfValue(a) : null;
    nint[]?[]? paramDims = kind == GoReflect.Func ? GoReflect.FuncParamDims(a) : null;
    return synthType(dyn, dims, paramDims);
}

// ==== the descriptor SPECIALIZATIONS: StructType() / ArrayType() ====
//
// Go's `(*structType)(unsafe.Pointer(t))` is the prefix-downcast idiom: the linker really did
// allocate a structType and hand out a pointer to its embedded Type header, so casting back
// reaches the sub-record. There is nothing behind a ж<abi.Type> to downcast to — the box holds an
// abi.Type and only an abi.Type — and golib's Reinterpret correctly REFUSES to alias managed
// storage for a reference-bearing pair (it would fabricate object references), so the auto form
// fell back to the raw-address route and read the specialization's fields out of whatever memory
// follows the value slot. Measured: `Fields` came back with Length 8830452760576 over a fabricated
// StructField[] reference — an IndexOutOfRangeException on the FIRST iteration of
// unique.buildStructCloneSeq, and internal/reflectlite's NumField/Len read the same garbage.
//
// The specializations are therefore SYNTHESIZED from the descriptor's carried System.Type, exactly
// as the descriptor itself is, over the same golib layout machinery (GoReflect.GoFields /
// GoSizeOf / GoAlignOf) that stamps Size_/Align_ — so a field's Offset and the descriptor's Size_
// can never disagree. What is NOT knowable is not invented: a descriptor with no System.Type, or a
// struct holding a field whose Go size cannot be computed, answers Go's nil rather than a
// plausible-looking record (the r39d rule — a descriptor field whose read cannot be honored must
// not be populated to look truthful), and every Go caller already tests that nil.
//
// StructField.Name and StructType.PkgPath stay the ZERO ΔName on purpose. A ΔName is a pointer
// into the linker's name blob, and every reader of one (Name/IsExported/IsEmbedded/ReadVarint)
// walks it with `addChecked` raw-address arithmetic — the same route that produced the garbage
// above. Go's own ΔName.Name() answers "" for a nil Bytes, so the zero value is a state the
// format DEFINES, not a fabrication; a synthesized encoding would instead hand every reader an
// address whose arithmetic has never been exercised. reflect's own hand-owned rtype.Field is
// where a named field descriptor already comes from (over GoReflect.GoFields), and no converted
// caller of abi.StructType reads a field name: unique reads Typ and Offset, internal/reflectlite
// reads len(Fields).

private static readonly System.Collections.Concurrent.ConcurrentDictionary<ж<Type>, ж<ΔStructType>> s_structTypes = new();
private static readonly System.Collections.Concurrent.ConcurrentDictionary<ж<Type>, ж<ΔArrayType>> s_arrayTypes = new();

// StructType returns t cast to a *StructType, or nil if its tag does not match.
public static ж<ΔStructType> StructType(this ж<Type> Ꮡt) {
    if (Ꮡt == nil || Ꮡt.Value.Kind() != Struct || Ꮡt.Value.sysType is null) {
        return default!;
    }
    return s_structTypes.GetOrAdd(Ꮡt, static box => synthesizeStructType(box));
}

private static ж<ΔStructType> synthesizeStructType(ж<Type> Ꮡt) {
    System.Type st = Ꮡt.Value.sysType!;
    GoReflect.GoFieldInfo[] infos = GoReflect.GoFields(st);
    nint[]? offsets = GoReflect.GoFieldOffsets(st);

    // A struct holding a field whose Go size is unknowable has no truthful layout at all — one
    // unknown size makes every later offset a guess. Answer Go's nil rather than a plausible record.
    if (offsets is null) {
        return default!;
    }
    StructField[] fields = new StructField[infos.Length];

    for (int i = 0; i < infos.Length; i++) {
        GoReflect.GoFieldInfo info = infos[i];
        nint[]? dims = GoReflect.KindOf(info.Type) == GoReflect.Array ? info.ArrayDims : null;
        fields[i] = new StructField(
            Name: default!,
            Typ: synthType(info.Type, dims),
            Offset: (uintptr)(nuint)offsets[i]
        );
    }

    return new ж<ΔStructType>(new ΔStructType(
        Type: Ꮡt.Value,
        PkgPath: default!,
        Fields: new slice<StructField>(fields)
    ));
}

// ArrayType returns t cast to a *ArrayType, or nil if its tag does not match.
public static ж<ΔArrayType> ArrayType(this ж<Type> Ꮡt) {
    if (Ꮡt == nil || Ꮡt.Value.Kind() != Array || Ꮡt.Value.sysType is null) {
        return default!;
    }
    return s_arrayTypes.GetOrAdd(Ꮡt, static box => synthesizeArrayType(box));
}

private static ж<ΔArrayType> synthesizeArrayType(ж<Type> Ꮡt) {
    System.Type at = Ꮡt.Value.sysType!;
    System.Type? elem = GoReflect.ElementType(at);
    nint[]? dims = Ꮡt.Value.arrayDims;

    // array<T> carries its element type but not its LENGTH — a descriptor built without dims
    // cannot answer Len, and guessing 0 would read as a real empty array.
    if (elem is null || dims is not { Length: > 0 }) {
        return default!;
    }
    nint[]? elemDims = dims.Length > 1 ? dims[1..] : null;

    return new ж<ΔArrayType>(new ΔArrayType(
        Type: Ꮡt.Value,
        Elem: synthType(elem, elemDims),
        Slice: synthType(typeof(slice<>).MakeGenericType(elem)),
        Len: (uintptr)(nuint)dims[0]
    ));
}

} // end abi_package
