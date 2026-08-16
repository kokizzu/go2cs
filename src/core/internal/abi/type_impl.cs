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
    // TFlagNamed — the descriptor bit that says "this is a DEFINED type", carried because three
    // separate readers consult it and a synthesized descriptor answered NO to all of them:
    // abi.Type.HasName(), internal/reflectlite's rtype.Name() (which gates on it and therefore
    // answered "" for EVERY type), and — the reason this had to land before Go's assignability
    // rule could — reflect/reflectlite's directlyAssignable, whose FIRST gate is
    // `T.HasName() && V.HasName()`. Go's rule admits two types with identical underlying types
    // only when at least one side is UNDEFINED; with the bit never set, that gate passed for
    // every pair and would have called two DISTINCT named types over one underlying type
    // assignable, which Go rejects.
    //
    // GoReflect.HasGoName is the managed reconstruction of the bit (it mirrors GoTypeName arm for
    // arm, so a type reports a name exactly when it has one), and it is the SAME gate reflect's
    // own hand-owned rtype.Name() already stood on — so the descriptor bit and the name a Type
    // reports cannot disagree. Populating it honors the r39d rule rather than bending it: this is
    // a field whose read CAN be honored, and leaving it zero was the untruth.
    //
    // The rest of TFlag stays zero, deliberately. TFlagUncommon would promise a uncommonType
    // sub-record behind the descriptor (there is none); TFlagExtraStar promises a name blob whose
    // first byte is a '*' to strip; TFlagRegularMemory/TFlagUnrolledBitmap describe a GC bitmap
    // layout the managed heap does not have. Each is a read this bridge cannot honor.
    if (GoReflect.HasGoName(st)) {
        t.TFlag |= (TFlag)TFlagNamed;
    }
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

// ==== the descriptor ACCESSORS that reach an element or a key: Elem() / Key() ====
//
// The SAME prefix-downcast idiom as the specializations above, one level in: Go's Elem() casts the
// Type header to the sliceType/arrayType/chanType/mapType/ptrType the linker really allocated
// behind it and reads that record's Elem field, and Key() does it for a mapType. So both inherited
// exactly the defect documented above — Reinterpret rightly refuses to alias managed storage for a
// reference-bearing pair — and answered nil for every slice, array, chan, map and pointer
// descriptor in the corpus.
//
// Nil is NOT a state Go's callers test for here, which is what made it fatal rather than merely
// wrong: reflect's haveIdenticalType recurses straight into nameFor(t), which reads the
// descriptor's carried System.Type and nil-dereferences. That is the whole of
// ConvertibleTo/AssignableTo for any operand that is not a scalar — measured as database/sql's
// TestConversions and TestUserDefinedBytes, and it gates every such recursion corpus-wide.
//
// Synthesized from the carried System.Type over the SAME golib element/key resolution that
// reflect's own hand-owned rtype.Elem / rtype.Key use one layer up, so the descriptor layer and
// the reflect layer cannot disagree about what an element type is. Nothing that is not knowable is
// invented (the r39d rule): a descriptor with no System.Type, or a managed type with no element,
// answers Go's nil — which is exactly what Go's own Elem() answers for a kind that has none.
public static ж<Type> Elem(this ж<Type> Ꮡt) {
    if (Ꮡt == nil) {
        return default!;
    }
    var kind = Ꮡt.Value.Kind();
    // Go's own switch: the five kinds that carry an element type, and nil for everything else.
    if (kind != Array && kind != Chan && kind != Map && kind != Pointer && kind != Slice) {
        return default!;
    }
    System.Type? elem = GoReflect.ElementType(Ꮡt.Value.sysType);
    if (elem is null) {
        return default!;
    }
    // An ARRAY descriptor's dims read [outer]…[inner], so its element takes the TAIL; a POINTER's
    // dims are the pointee's already and pass through UNSHIFTED, a pointer having no length of its
    // own. The same rule rtype.Elem applies to the same cargo one layer up.
    nint[]? dims = Ꮡt.Value.arrayDims;
    nint[]? elemDims = kind == Pointer ? dims : dims is { Length: > 1 } ? dims[1..] : null;
    return synthType(elem, elemDims);
}

// ChanDir returns the direction of t if t is a channel type, otherwise InvalidDir.
//
// The FOURTH accessor of the same downcast family — `(*chanType)(unsafe.Pointer(t))` reaching the
// record's Dir field — and the ONE member with no synthesis waiting for it, because the direction
// is not merely unpopulated: it is not IN the managed type at all. A Go channel type emits as
// golib's `channel<T>` whatever its direction, so `<-chan int`, `chan<- int` and `chan int` are
// ONE managed type, and no descriptor built from a value can tell them apart.
//
// That makes the honest answer BothDir rather than a guess, and the distinction matters. The
// bridge is not failing to recover a direction it holds; it can only ever DESCRIBE the
// bidirectional channel type, and BothDir is that type's true direction. Type.String() has always
// said the same thing — GoTypeName renders every `channel<T>` as `chan T` — so this makes the
// descriptor's three answers (kind, name, direction) consistent where the downcast made ChanDir
// alone disagree by reading a direction out of the memory following the value slot. That read was
// the worst kind of wrong: NON-DETERMINISTIC, so reflect.MakeChan's `ChanDir() != BothDir` guard
// and haveIdenticalUnderlyingType's chan arm both answered differently run to run.
//
// The residual is stated rather than hidden: a DIRECTIONAL Go channel type cannot be described by
// this bridge, so reflect.TypeOf over a `<-chan int` reports `chan int` and any consumer that
// branches on direction (text/template's walkRange rejecting a send-only channel) sees the
// bidirectional answer. It is a limit of the converter's channel EMISSION, one layer above this
// accessor, and it is recorded in ConversionStrategies-Reference.md; no roster package observes
// it. Recovering it would mean carrying direction as descriptor cargo the way array dims are
// carried, and no measured consumer asks (the r39d rule).
public static ΔChanDir ChanDir(this ж<Type> Ꮡt) {
    if (Ꮡt == nil || Ꮡt.Value.Kind() != Chan) {
        return InvalidDir;
    }
    return BothDir;
}

// Key returns the key type for t if t is a map, otherwise nil.
public static ж<Type> Key(this ж<Type> Ꮡt) {
    if (Ꮡt == nil || Ꮡt.Value.Kind() != Map) {
        return default!;
    }
    System.Type? key = GoReflect.KeyType(Ꮡt.Value.sysType);
    return key is null ? default! : synthType(key);
}

// Len returns the length of t if t is an array type, otherwise 0 — the descriptor's carried dims,
// which is exactly what reflect's own rtype.Len reads one layer up.
//
// The third accessor of the same recursion, and the one whose downcast failed WORST: Elem()/Key()
// answered a clean nil, but Len() read a uintptr out of the memory following the descriptor's value
// slot, so two array descriptors read two DIFFERENT pieces of garbage and haveIdenticalUnderlyingType
// reported [3]byte and [3]byte as different types. A length the descriptor does not know is still
// answered as Go's 0 — array dims are non-identity cargo (the recorded §5 limitation), so two
// dimension-less array descriptors compare equal rather than randomly unequal.
public static nint Len(this ж<Type> Ꮡt) {
    if (Ꮡt == nil || Ꮡt.Value.Kind() != Array) {
        return 0;
    }
    return Ꮡt.Value.arrayDims is { Length: > 0 } dims ? dims[0] : 0;
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
