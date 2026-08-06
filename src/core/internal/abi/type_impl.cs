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
partial struct Type {
    [GoReflectCompanion] public System.Type? sysType;
    [GoReflectCompanion] public nint[]? arrayDims;
}

// synthType builds a managed-backed abi.Type from a System.Type: Kind_ classified from it (GoReflect),
// the System.Type carried on the descriptor, Go size/alignment stamped when knowable (binary's sizeof
// reads Size_ for the scalar kinds), and array dims carried as cargo when the caller knew them. The
// single builder behind both TypeOf (from a value) and reflect's Type.Elem/Field (from a type).
public static ж<Type> synthType(System.Type? st) {
    return synthType(st, null);
}

// Descriptors are immutable once synthesized — intern them per (managed type, dims) so the
// per-TypeOf cost (kind classification + size stamping's field walk) is paid once, not per
// call (fmt classifies every argument; binary sizes in loops).
private static readonly System.Collections.Concurrent.ConcurrentDictionary<(System.Type, string), ж<Type>> s_descriptors = new();

public static ж<Type> synthType(System.Type? st, nint[]? arrayDims) {
    if (st is null) {
        return default!;
    }
    string dimsKey = arrayDims is null ? "" : string.Join(',', arrayDims);
    return s_descriptors.GetOrAdd((st, dimsKey), _ => synthesizeDescriptor(st, arrayDims));
}

private static ж<Type> synthesizeDescriptor(System.Type st, nint[]? arrayDims) {
    ref var t = ref heap<Type>(out var Ꮡt);
    t.Kind_ = (ΔKind)((uint8)GoReflect.KindOf(st));
    t.sysType = st;
    t.arrayDims = arrayDims;
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
// dims, carried on the descriptor as non-identity cargo (increment 2).
public static ж<Type> TypeOf(any a) {
    if (a == default!) {
        return default!;
    }
    System.Type dyn = GoReflect.GoDynamicTypeOf(a);
    nint[]? dims = GoReflect.KindOf(dyn) == GoReflect.Array ? GoReflect.ArrayDimsOfValue(a) : null;
    return synthType(dyn, dims);
}

} // end abi_package
