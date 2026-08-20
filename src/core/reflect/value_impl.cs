// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;
using System;
using System.Collections;
using System.Reflection;
using abi = go.@internal.abi_package;
using @unsafe = go.unsafe_package;

// Hand-finished conversion (the reflection bridge — Phase 4, value side). Go's reflect.Value reads the
// value through v.ptr as flat memory at computed field/element offsets — reinterpreting an interface's
// data word — which has no managed form. Instead, reflect.Value carries the boxed managed value
// DIRECTLY (a companion `partial struct Value { object boxed }` field), and the value-reader methods
// read it with System.Reflection + the golib container interfaces (IArray for slices/arrays, ж<T> for
// pointers). The entry (ValueOf/unpackEface) sets typ_ (the Phase-1 synthetic abi.Type, Kind_ from the
// managed System.Type) and the flag's Kind bits, so Kind()/IsValid()/CanAddr() keep working from
// value.cs (Type() is hand-owned below so it returns a CANONICAL reflect.Type). The converter skips
// these declarations via the manualConversionFuncs registry
// (go2cs/manualTypeOperations.go); this module marker also makes go2cs skip re-converting this file.
// INCREMENT 1: scalars, slices, arrays, pointers. Struct Field/NumField + map MapRange land next.
// See docs/phase4/DESIGN-reflection-bridge.md.

[module: GoManualConversion]

namespace go;

partial class reflect_package {

// The managed backing for a Value: the boxed Go value this Value represents (null for the zero
// Value — or for a VALID typed-nil/nil-interface Value, distinguished by typ_/flag being set),
// plus, when the Value is ADDRESSABLE (flagAddr), the ж<T> box it ALIASES: every read goes
// through the box lazily (a write through another alias of the same box — poser.As's direct
// `x.Value = …` — must be visible to a later Interface() read), and Set writes through it.
partial struct ΔValue {
    [GoReflectCompanion] internal object? boxed;
    [GoReflectCompanion] internal object? addrBox;

    // The LIVE value this Value represents (read-through for an addressable Value).
    internal object? live => addrBox is null ? boxed : GoReflect.ReadPointerSlot(addrBox);
}

// makeReflectValue builds a Value carrying a boxed managed value, typed by its GO DYNAMIC type.
// typ_ is the Phase-1 synthetic abi.Type (Kind_ classified from the managed System.Type); the flag
// holds the Kind so Kind()/IsValid() resolve from value.cs unchanged. Used where Go derives the
// type from the VALUE (ValueOf, interface Elem); slot-derived Values use makeTypedValue.
internal static ΔValue makeReflectValue(object? boxed) {
    if (boxed is null) {
        return new ΔValue(nil);
    }
    var t = abi.TypeOf(boxed);
    var v = new ΔValue(t, default!, ((flag)(uintptr)(uint8)GoReflect.KindOf(GoReflect.GoDynamicTypeOf(boxed))));
    v.boxed = boxed;
    return v;
}

// makeTypedValue builds a Value typed by a STATIC slot type (a struct field's declared type, a
// slice's element type, a func's out type) — Go's rule for every slot-derived Value: an
// interface-typed slot reports Kind Interface regardless of the dynamic value, and a nil-valued
// slot is a VALID nil Value of the slot's kind (never the invalid zero Value). inheritRO carries
// the parent's read-only bits (Go's flagRO stickiness).
internal static ΔValue makeTypedValue(object? boxed, System.Type staticType, nint[]? arrayDims, flag inheritRO, GoChanDir chanDir = GoChanDir.Unstamped, nint[]? keyDims = null) {
    var t = abi.synthType(staticType, arrayDims, null, chanDir, keyDims);
    var v = new ΔValue(t, default!, ((flag)(uintptr)(uint8)GoReflect.KindOf(staticType)) | ((flag)(inheritRO & flagRO)));
    v.boxed = boxed;
    return v;
}

// isNilGoValue answers Go nilness for a boxed container/pointer/func value — since 2026-08-18 a
// direct delegation to golib's GoReflect.IsNilGoValue, where the rule moved so
// internal/reflectlite's mirror IsNil reads the SAME nilness (its own switch lacked the
// generated-operator probe, so a nil slice/chan read out of a struct field answered NOT nil —
// reflectlite's TestIsNil rows).
internal static bool isNilGoValue(object? cur) {
    return GoReflect.IsNilGoValue(cur);
}

// ValueOf returns a new Value initialized to the concrete value stored in the interface i.
public static ΔValue ValueOf(any i) {
    return i == default! ? new ΔValue(nil) : makeReflectValue(i);
}

internal static ΔValue unpackEface(any i) {
    return ValueOf(i);
}

// Interface returns v's current value as an interface{}. A valid typed-nil pointer Value
// yields its canonical nil box — a NON-nil `any` holding `(*T)(nil)`, exactly Go's packEface
// (the type is never erased to a bare null one call after X2 restored it).
public static any /*i*/ Interface(this ΔValue v) {
    return packInterfaceValue(v);
}

internal static any /*i*/ valueInterface(ΔValue v, bool safe) {
    return packInterfaceValue(v);
}

// packInterfaceValue is the bridge's packEface: it builds the interface value for v, and Go's
// rule for that is entirely about the TYPE half. An eface carries (type, data word), so a
// POINTER-kinded Value whose data word is nil packs as a NON-nil interface holding
// (type=*T, value=nil) — Go's typed nil. Managed storage has no data word to keep the type
// beside: a *T slot physically holds C# `null`, and handing that straight out ERASES the type.
// Everything downstream then reads the nil INTERFACE instead: `i == nil` answers true, `%T`
// prints <nil>, and `i.(Iface)` takes the failure arm — so a method written to handle its nil
// receiver never runs. That last one is not hypothetical; it is the whole of
// `func (x *Int) GobEncode() { if x == nil { … } }`, which encoding/gob reaches as
// `v.Interface().(GobEncoder)` for every zero-filled element of a `make([]*Int, 1)`.
//
// So a null read out of a POINTER-kinded slot is re-encoded as the CANONICAL typed nil for
// that slot's static type — ж<T>.NilBox, the one instance `reflect.Zero` of a pointer kind
// already yields (GoReflect.ZeroValueOf) and every emitted nil→*T conversion already produces.
// One nil encoding system-wide; this is the READ path joining the encoding the write path and
// the fabrication path have always used. Because it is that same instance, the packed value
// also compares equal to a language-level `(*T)(nil)` and asserts through the ordinary witness
// machinery — nothing here is a second nil representation.
//
// POINTER KINDS ONLY. An interface- or func-typed slot holding null IS the nil interface / nil
// func — Go packs THAT as the nil eface, and re-encoding it would invert the bug rather than
// fix it. A slot whose static type resolves to no canonical nil (a shape with neither ж<T>'s
// NilBox nor a generated wrapper's NilInstance) keeps the null it had, so this can only ever
// ADD type information, never substitute a wrong one.
internal static any /*i*/ packInterfaceValue(ΔValue v) {
    object? cur = v.live;
    if (cur is not null) {
        return cur;
    }
    ΔKind k = v.kind();
    // A nil FUNC packs as (type=func-type, value=nil) exactly as a nil pointer does — the
    // delegate-shaped half of the one-nil-encoding rule (GoReflect.CanonicalNilFunc; a null
    // delegate slot is correct IN the slot and type-erasing in interface space, where `%T`
    // must print `func(int8, int32)` — reflectlite's TestFunctionValue/TestTypes rows).
    if (k == Func) {
        System.Type? ft = v.typ_ == nil ? null : v.typ_.Value.sysType;
        return (ft is null ? null : GoReflect.CanonicalNilFunc(ft))!;
    }
    if (k != ΔPointer && k != ΔUnsafePointer) {
        return cur!;
    }
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    return (st is null ? null : GoReflect.CanonicalNilPointer(st))!;
}

public static bool Bool(this ΔValue v) {
    object? cur = v.live;
    if (cur is bool b) {
        return b;
    }
    // A named bool type unwraps to its underlying (the read mirror of SetBool).
    if (cur is not null && GoReflect.TryUnwrapWrapperValue(cur, out object? unwrapped) && unwrapped is bool ub) {
        return ub;
    }
    return (bool)cur!;
}

public static int64 Int(this ΔValue v) {
    return numericValue(v.live) switch {
        nint n => (int64)n,
        int i => i,
        long l => l,
        short s => s,
        sbyte b => b,
        var n => System.Convert.ToInt64(n)
    };
}

public static uint64 Uint(this ΔValue v) {
    return numericValue(v.live) switch {
        nuint n => (uint64)n,
        uintptr up => (uint64)up.Value,
        uint u => u,
        ulong l => l,
        ushort s => s,
        byte b => b,
        var n => System.Convert.ToUInt64(n)
    };
}

public static float64 Float(this ΔValue v) {
    return System.Convert.ToDouble(numericValue(v.live));
}

// numericValue unwraps a NAMED numeric type (`type Celsius float64` → a [GoType("num:float64")] struct)
// to its underlying primitive so Int/Uint/Float can read it — a primitive (int/double/…) or golib
// uintptr is returned unchanged; a wrapper struct yields its single primitive field.
private static object? numericValue(object? boxed) {
    if (boxed is null || boxed.GetType().IsPrimitive || boxed is uintptr) {
        return boxed;
    }
    foreach (FieldInfo f in boxed.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
        object? val = f.GetValue(boxed);
        if (val is not null && (val.GetType().IsPrimitive || val is uintptr)) {
            return val;
        }
    }
    return boxed;
}

public static complex128 Complex(this ΔValue v) {
    // golib complex64 is its own struct — an unbox-cast to Complex would throw; and a named
    // complex wrapper unwraps to its underlying first (the read mirror of SetComplex).
    object? cur = v.live;
    if (cur is not null && GoReflect.TryUnwrapWrapperValue(cur, out object? unwrapped)) {
        cur = unwrapped;
    }
    return cur switch {
        complex128 c => c,
        complex64 c64 => (complex128)c64,
        _ => (complex128)cur!
    };
}

public static @string String(this ΔValue v) {
    // fmt only calls String() for Kind String; a boxed @string returns itself (a named string
    // wrapper unwraps), anything else the Go "<T Value>" placeholder.
    if (v.live is @string s) {
        return s;
    }
    if (v.live is not null && GoReflect.TryUnwrapWrapperValue(v.live, out object? unwrapped) && unwrapped is @string us) {
        return us;
    }
    if (v.live is null) {
        return "<invalid Value>";
    }
    return (@string)("<" + v.Type().String().ToString() + " Value>");
}

// IsNil reports whether its argument v is nil (v must be a chan, func, interface, map, pointer, or slice).
// STRUCTURAL nil for pointers (INilPointer — the canonical typed-nil form): a heap box holding a
// nil value is a NON-nil pointer holding nil, and an adapter-held *T asks its receiver box.
// Slices/channels/named wrappers answer through their own generated `== nil` operator — the same
// nilness the emitted comparisons observe (isNilGoValue).
public static bool IsNil(this ΔValue v) {
    // An INTERFACE-kind value's nilness is a property of the INTERFACE, never of whatever
    // pointer it happens to carry: an interface holding a typed nil `(*T)(nil)` is a NON-nil
    // interface (Go packs (type=*T, value=nil) — packInterfaceValue's own encoding). The
    // unwrap below asked the POINTEE instead and inverted the answer, and IsZero for an
    // interface IS IsNil, so encoding/gob's `!state.sendZero && v.IsZero()` skipped the field
    // outright and its "gob: cannot encode nil pointer inside interface" path was unreachable
    // (TestNilPointerInsideInterface; the ReflectTypedNilInterface behavioral shape pins both
    // directions with the nil-interface control).
    if (v.kind() == ΔInterface) {
        return v.live is null;
    }
    object? cur = v.live;
    while (cur is IInterfaceAdapter { Value: not null } interfaceAdapter) {
        cur = interfaceAdapter.Value;
    }
    if (cur is IжAdapter { Box: not null } pointerAdapter) {
        cur = pointerAdapter.Box;
    }
    return isNilGoValue(cur);
}

// Len returns v's length (v must be an Array, Chan, Map, Slice, String, or pointer-to-Array).
// A NAMED string unwraps to the string it wraps, exactly as String() does: every other named
// container answers through the golib interface its wrapper implements (a named slice is an
// IArray, a named map an IMap), but a `type NS string` wrapper implements none of them, so
// without this arm it fell to the 0 default — SILENTLY, because 0 is a real length. IsZero's
// String arm is `Len() == 0`, so that made every non-empty named string report itself ZERO,
// and encoding/gob then omitted such a field from the wire entirely.
public static nint Len(this ΔValue v) {
    object? cur = v.live;
    if (cur is not null && cur is not @string && v.kind() == ΔString && GoReflect.TryUnwrapWrapperValue(cur, out object? unwrapped)) {
        cur = unwrapped;
    }
    return cur switch {
        @string s => s.Length,
        IArray a => a.Length,
        IMap m => m.Length,
        // Go's Len covers Chan too — `len(ch)` is the count of buffered elements — and this arm was
        // simply missing, so every channel Value reported 0 while Cap answered correctly one method
        // away. Silent for the same reason the named-string arm above was: 0 is a real length. It
        // surfaced when Value.Recv started working and a test could finally ask what a channel it
        // had just drained still held.
        IChannel c => c.Length,
        _ => 0
    };
}

// Index returns v's i'th element (v must be an Array, Slice, or String). Slice elements are
// ALWAYS addressable (the shared backing store is the address — golib slices alias their T[]
// across struct copies); array elements are addressable iff the array Value is, routed through
// ж.at<E>() so a lazily-backed named-array wrapper materializes on the REAL storage (the
// pallocBits lesson). The element Value is typed by the STATIC element type and inherits the
// parent's read-only bits (Go flag stickiness).
public static ΔValue Index(this ΔValue v, nint i) {
    ΔKind k = v.kind();
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    System.Type? elemType = GoReflect.ElementType(st);
    flag ro = (flag)(v.flag & flagRO);
    if (k == ΔSlice && elemType is not null) {
        object? liveSlice = v.live;
        if (liveSlice is not IArray sliceArr || (nuint)i >= (nuint)sliceArr.Length) {
            throw panic("reflect: array index out of range");
        }
        var elem = makeTypedValue(null, elemType, null, ro);
        elem.flag |= flagAddr | flagIndir;
        elem.addrBox = GoReflect.ElementAliasBoxOfValue(liveSlice, elemType, i);
        return elem;
    }
    if (k == Array && elemType is not null) {
        object? liveArr = v.live;
        if (liveArr is not IArray arr || (nuint)i >= (nuint)arr.Length) {
            throw panic("reflect: array index out of range");
        }
        nint[]? elemDims = v.typ_.Value.arrayDims is { Length: > 1 } dims ? dims[1..] : null;
        if (v.addrBox is not null && v.addrBox.GetType() is { IsGenericType: true } boxType && boxType.GetGenericTypeDefinition() == typeof(ж<>)) {
            var elem = makeTypedValue(null, elemType, elemDims, ro);
            elem.flag |= flagAddr | flagIndir;
            elem.addrBox = GoReflect.ElementAliasBoxOfBox(v.addrBox, elemType, i);
            return elem;
        }
        return makeTypedValue(arr[i], elemType, elemDims, ro);
    }
    // A STRING indexes to its i'th BYTE as a uint8 Value — never addressable (Go's strings are
    // immutable, so its own arm sets flagIndir but no flagAddr) and typed by the byte type rather
    // than by the string's, which is why it cannot share the element-type route above: a string
    // Value has no ElementType at all. text/template's `index` builtin is the measured consumer
    // (`{{index `x` 0}}`), and it reached the ValueError below — "call of reflect.Value.Index on
    // string Value" — for every string, which is Go's message for a kind that does NOT support
    // indexing at all.
    if (k == ΔString) {
        @string s = goStringOf(v);
        if ((nuint)i >= (nuint)s.Length) {
            throw panic("reflect: string index out of range");
        }
        return makeTypedValue(s[i], typeof(uint8), null, ro);
    }
    throw panic(Ꮡ(new ValueError("reflect.Value.Index", v.kind())));
}

// The @string behind a string-kinded Value, unwrapping a NAMED string wrapper the same way Len
// does — a `type NS string` wrapper implements none of the golib container interfaces, so it has
// to be unwrapped explicitly or it reads as the zero string. One helper, so Index, Slice and Len
// can never disagree about what a named string's bytes are.
internal static @string goStringOf(ΔValue v) {
    object? cur = v.live;
    if (cur is not null && cur is not @string && GoReflect.TryUnwrapWrapperValue(cur, out object? unwrapped)) {
        cur = unwrapped;
    }
    return cur is @string s ? s : default;
}

// Slice returns v[i:j] (v must be an Array, Slice, or String; an array must be addressable).
// The result SHARES the source's backing store — golib slices window their T[] — which the
// round-trip consumers depend on (encoding/binary's TestSliceRoundTrip decodes through the
// window into the original array).
// Copy copies src's elements into dst until dst is full or src is exhausted, returning the count.
// dst and src must share an element type; as a special case src may be a String when dst's element
// type is byte.
//
// The auto form reinterprets BOTH operands' data words as flat `unsafeheader.Slice` headers
// (`*(*unsafeheader.Slice)(dst.ptr)`) and hands them to typedslicecopy — a raw memory move with no
// managed form, and on the bridge's never-populated ptr slot it dereferenced a nil ж outright
// (`op_OnesComplement`). encoding/asn1's parseField copies every parsed []byte into its destination
// through it, so this NRE was crypto/x509's ParsePKCS8PrivateKey and therefore crypto/ecdsa's
// TestEqual. Copying element-wise through the same golib container interfaces every other bridged
// container method uses keeps the aliasing exact: a slice VALUE windows the backing store it shares
// with its parent, so a write through the indexer is a write the parent sees — which is what Go's
// typedslicecopy does to the same memory.
public static nint Copy(ΔValue dst, ΔValue src) {
    ΔKind dk = dst.kind();
    if (dk != Array && dk != ΔSlice) {
        throw panic(Ꮡ(new ValueError("reflect.Copy"u8, dk)));
    }
    if (dk == Array) {
        dst.flag.mustBeAssignable();
    }
    dst.flag.mustBeExported();
    System.Type? dstElem = GoReflect.ElementType(dst.typ_ == nil ? null : dst.typ_.Value.sysType);
    ΔKind sk = src.kind();
    bool stringCopy = false;
    if (sk != Array && sk != ΔSlice) {
        stringCopy = sk == ΔString && dstElem == typeof(byte);
        if (!stringCopy) {
            throw panic(Ꮡ(new ValueError("reflect.Copy"u8, sk)));
        }
    }
    src.flag.mustBeExported();
    if (!stringCopy) {
        System.Type? srcElem = GoReflect.ElementType(src.typ_ == nil ? null : src.typ_.Value.sysType);
        if (dstElem is null || srcElem is null || dstElem != srcElem) {
            throw panic("reflect.Copy: type mismatch: " + GoReflect.GoTypeName(srcElem) +
                        " is not assignable to type " + GoReflect.GoTypeName(dstElem));
        }
    }
    // A nil container on either side copies nothing — Go's headers report length 0 there.
    if (dst.live is not IArray dstArr) {
        return 0;
    }
    nint n;
    if (stringCopy) {
        @string s = src.live is @string str ? str : default;
        n = dstArr.Length < s.Length ? dstArr.Length : s.Length;
        for (nint i = 0; i < n; i++) {
            dstArr[i] = s[i];
        }
        return n;
    }
    if (src.live is not IArray srcArr) {
        return 0;
    }
    n = dstArr.Length < srcArr.Length ? dstArr.Length : srcArr.Length;
    for (nint i = 0; i < n; i++) {
        dstArr[i] = srcArr[i];
    }
    return n;
}

public static ΔValue Slice(this ΔValue v, nint i, nint j) {
    ΔKind k = v.kind();
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    System.Type? elemType = GoReflect.ElementType(st);
    // A STRING slices to a string of v's OWN type — Go returns `Value{v.typ(), ...}`, so a named
    // string stays named — and needs no addressability, since the result copies no storage the
    // caller could write through. text/template's `slice` builtin is the measured consumer.
    if (k == ΔString) {
        @string s = goStringOf(v);
        if (i < 0 || j < i || j > s.Length) {
            throw panic("reflect.Value.Slice: string slice index out of bounds");
        }
        @string sub = s[((int)i)..((int)j)];
        object boxedWindow = sub;
        if (st is not null && st != typeof(@string) && GoReflect.TryConvertTo(sub, st, out object? named)) {
            boxedWindow = named;
        }
        return makeTypedValue(boxedWindow, st ?? typeof(@string), null, (flag)(v.flag & flagRO));
    }
    if (elemType is null || (k != Array && k != ΔSlice)) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Slice", v.kind())));
    }
    if (k == Array && (flag)(v.flag & flagAddr) == 0) {
        throw panic("reflect.Value.Slice: slice of unaddressable array");
    }
    object? liveContainer = v.live;
    if (liveContainer is null) {
        throw panic("reflect.Value.Slice: slice of nil container");
    }
    object window = GoReflect.SliceWindow(liveContainer, elemType, i, j);
    return makeTypedValue(window, typeof(slice<>).MakeGenericType(elemType), null, (flag)(v.flag & flagRO));
}

// Slice3 is the 3-index form of the slice operation — v[i:j:k] — where k bounds the result's
// CAPACITY. Array and Slice only; Go's own Slice3 has no String arm, because a string has no
// capacity to bound.
//
// The auto form is the same raw-header walk Slice's was: it reinterprets v.ptr as an
// unsafeheader.Slice and edits Data/Len/Cap in place. The bridge never populates ptr, so
// `(ж<unsafeheader.Slice>)(uintptr)(v.ptr)` dereferenced nil outright — which is why this
// surfaced as "invalid memory address or nil pointer dereference" rather than as a missing
// feature. text/template's `slice` builtin with three indexes is the measured consumer
// (`{{slice .SICap 6 10 10}}`); it is bridged here over the SAME golib window machinery Slice
// uses, so the two-index and three-index forms cannot disagree about what a window is.
public static ΔValue Slice3(this ΔValue v, nint i, nint j, nint k) {
    ΔKind kind = v.kind();
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    System.Type? elemType = GoReflect.ElementType(st);
    if (elemType is null || (kind != Array && kind != ΔSlice)) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Slice3", v.kind())));
    }
    if (kind == Array && (flag)(v.flag & flagAddr) == 0) {
        throw panic("reflect.Value.Slice3: slice of unaddressable array");
    }
    object? liveContainer = v.live;
    if (liveContainer is null) {
        throw panic("reflect.Value.Slice3: slice of nil container");
    }
    // Go bounds Slice3 by the source's CAPACITY (an array's is its length), and reports its own
    // message — checked here rather than left to the golib window, whose bounds panic is the
    // backstop and carries golib's wording.
    nint cap = liveContainer switch {
        ISlice s => s.Capacity,
        IArray a => a.Length,
        _ => 0
    };
    if (i < 0 || j < i || k < j || k > cap) {
        throw panic("reflect.Value.Slice3: slice index out of bounds");
    }
    object window = GoReflect.SliceWindow(liveContainer, elemType, i, j, k);
    return makeTypedValue(window, typeof(slice<>).MakeGenericType(elemType), null, (flag)(v.flag & flagRO));
}

// Cap returns v's capacity (v must be an Array, Chan, or Slice) through the golib container
// interfaces — the auto form reads the never-populated v.ptr slice header (gob's decodeSlice
// probes `value.Cap() < n` before allocating). A valid nil container Value answers 0 (Go).
public static nint Cap(this ΔValue v) {
    object? cur = v.live;
    ΔKind k = v.kind();
    if (cur is null && (k == ΔSlice || k == Array || k == Chan)) {
        return 0;
    }
    return cur switch {
        ISlice s => s.Capacity,
        IArray a => a.Length,
        IChannel c => c.Capacity,
        _ => throw panic(Ꮡ(new ValueError("reflect.Value.Cap", v.kind())))
    };
}

// SetLen sets v's length to n (v must be an addressable Slice; 0 <= n <= cap, Go's panic).
// The managed slice value is a HEADER struct, so the re-lengthened window (same backing, same
// capacity — Go's s[:n]) is written back through the aliased box, coerced for a NAMED slice
// wrapper slot via the single convertibility relation.
public static void SetLen(this ΔValue v, nint n) {
    v.flag.mustBeAssignable();
    v.flag.mustBe(ΔSlice);
    System.Type? slotType = v.typ_ == nil ? null : v.typ_.Value.sysType;
    System.Type? elemType = GoReflect.ElementType(slotType);
    object? live = v.live;
    if (slotType is null || elemType is null || v.addrBox is null || live is null) {
        throw panic("reflect: SetLen using unaddressable value");
    }
    if (live is not ISlice s || n < 0 || n > s.Capacity) {
        throw panic("reflect: slice length out of range in SetLen");
    }
    object window = GoReflect.SliceWindow(live, elemType, 0, n);
    if (!GoReflect.TryConvertTo(window, slotType, out object? converted)) {
        throw panic("reflect: SetLen window is not assignable to the slice slot");
    }
    GoReflect.WritePointerSlot(v.addrBox, converted);
}

// Grow increases v's capacity, if necessary, to guarantee space for another n elements (v must
// be an addressable Slice). The LENGTH is unchanged and the contents are preserved — Go's
// growslice contract, which encoding/gob's decUint8Slice and decodeArrayHelper lean on to
// allocate incrementally once a decoded slice passes internal/saferio's 10 MiB chunk.
//
// The auto form reads a *unsafeheader.Slice off the never-populated v.ptr, so it nil-deref'd for
// every caller. Here the reallocation is an ordinary managed one, written back through the
// aliased box like SetLen — and, like SetLen, coerced into a NAMED slice wrapper's slot through
// the single convertibility relation. Growing WITHIN the existing capacity writes nothing at
// all: Go reallocates only past the capacity, and a spurious write would detach any other view
// still sharing the backing store.
public static void Grow(this ΔValue v, nint n) {
    v.flag.mustBeAssignable();
    v.flag.mustBe(ΔSlice);
    if (n < 0) {
        throw panic("reflect.Value.Grow: negative len");
    }
    System.Type? slotType = v.typ_ == nil ? null : v.typ_.Value.sysType;
    System.Type? elemType = GoReflect.ElementType(slotType);
    if (slotType is null || elemType is null || v.addrBox is null) {
        throw panic("reflect: Grow using unaddressable value");
    }
    object? live = v.live;
    object? grown = GoReflect.GrowSlice(live, elemType, n);
    if (ReferenceEquals(grown, live)) {
        return;
    }
    if (grown is null || !GoReflect.TryConvertTo(grown, slotType, out object? converted)) {
        throw panic("reflect: Grow result is not assignable to the slice slot");
    }
    GoReflect.WritePointerSlot(v.addrBox, converted);
}

// IsZero reports whether v is the zero value for its type.
//
// Go's own form is three DESCRIPTOR reads over flat memory — an `Equal` function pointer against
// the shared zeroVal buffer, a TFlagRegularMemory all-bits-zero scan, and `v.ptr == nil` when
// the value is not indirect — and a synthesized descriptor populates none of them. The
// consequence was total rather than partial: the Array and Struct arms both fell to
// `v.ptr == nil`, which the bridge never populates, so EVERY array and EVERY struct reported
// itself zero whatever it held. That is silent, not a fault, because `true` is the right answer
// for the zero value of the same type.
//
// The managed answer is Go's own recursive definition with the memory shortcuts removed: a
// composite is zero exactly when every element or field is. That is strictly the WALK the
// shortcuts stand in for, so it needs no descriptor state — only Index/Field/NumField, which
// the bridge already answers, and which is why the walk and the leaves land together.
public static bool IsZero(this ΔValue v) {
    ΔKind k = v.kind();
    if (k == ΔBool) {
        return !v.Bool();
    }
    if (k == ΔInt || k == Int8 || k == Int16 || k == Int32 || k == Int64) {
        return v.Int() == 0;
    }
    if (k == ΔUint || k == Uint8 || k == Uint16 || k == Uint32 || k == Uint64 || k == Uintptr) {
        return v.Uint() == 0;
    }
    if (k == Float32 || k == Float64) {
        return v.Float() == 0D;
    }
    if (k == Complex64 || k == Complex128) {
        return v.Complex() == 0D;
    }
    if (k == Chan || k == Func || k == ΔInterface || k == Map || k == ΔPointer || k == ΔSlice || k == ΔUnsafePointer) {
        return v.IsNil();
    }
    if (k == ΔString) {
        return v.Len() == 0;
    }
    if (k == Array) {
        nint n = v.Len();
        for (nint i = 0; i < n; i++) {
            if (!v.Index(i).IsZero()) {
                return false;
            }
        }
        return true;
    }
    if (k == Struct) {
        nint n = v.NumField();
        for (nint i = 0; i < n; i++) {
            // Go skips the BLANK field: `_` is padding that carries no value identity.
            if (!v.Field(i).IsZero() && v.Type().Field(i).Name != "_"u8) {
                return false;
            }
        }
        return true;
    }
    // Go panics for an invalid Value and for any kind it has no rule for.
    throw panic(Ꮡ(new ValueError("reflect.Value.IsZero", v.kind())));
}

// Elem returns the value that the interface v contains or that the pointer v points to.
// The pointer form returns an ADDRESSABLE Value ALIASING the receiver box (Go: "the returned
// value's address is v's value") — reads go through the box lazily and Set writes through it.
// An adapter-held *T aliases the adapter's receiver box; a structurally nil pointer yields the
// invalid zero Value (Go).
public static ΔValue Elem(this ΔValue v) {
    ΔKind k = v.kind();
    if (k == ΔInterface) {
        return makeReflectValue(v.live);
    }
    if (k == ΔPointer) {
        object? cur = v.live;
        while (cur is IInterfaceAdapter { Value: not null } interfaceAdapter) {
            cur = interfaceAdapter.Value;
        }
        if (cur is IжAdapter { Box: not null } pointerAdapter) {
            cur = pointerAdapter.Box;
        }
        if (cur is null || (cur is INilPointer nilable && nilable.IsNilPointer)) {
            return new ΔValue(nil);
        }
        if (!GoReflect.TryPointerBoxElement(cur.GetType(), out Type? pointee)) {
            // An OPAQUE managed handle, not a pointer box — the value-side twin of the descent
            // rule. KindOf reports Pointer for every managed reference it does not otherwise
            // recognize (one word wide, never looked inside), and a hand-owned shim's backing
            // object is exactly that: sync.Mutex's SemaphoreSlim gate, sync.RWMutex's RWState.
            // Nothing behind such a handle has a Go representation, so there is no pointee to
            // hand back and the walk STOPS here with the invalid Value — the same answer a nil
            // pointer already gives. Reading a slot instead threw "Not a pointer box type" and
            // took out every DeepEqual over a struct holding a sync primitive; and the blindness
            // is what makes two such structs compare deeply equal, which is Go's own answer
            // (Go compares the primitives' state WORDS, and a used-then-released lock is back at
            // its zero state — crypto/tls's TestCloneNonFuncFields is the measured case).
            return new ΔValue(nil);
        }
        // An array pointee reveals its real dims through the live value behind the box (the
        // TestSliceRoundTrip path: ValueOf(&[100]T{}).Elem().Type() must carry 100) — but the
        // DESCRIPTOR wins wherever it carries them, because a pointer's cargo IS its pointee's, and
        // in Go a `*[3]int` has element type `[3]int` whatever its backing currently holds.
        //
        // The ordering is load-bearing at exactly one place, and it is a decode target:
        // encoding/gob's `decIndirect` walks a `***[3]int` field by allocating each level from
        // `value.Type().Elem()`, so a hop that answered from the live value alone would read the nil
        // pointer it is standing on, drop the cargo, allocate a ZERO-length array from the
        // dimension-less descriptor — and the NEXT hop would then measure that zero as the truth.
        // This is the same rule rtype.Elem and abi.Elem apply on the type side (a pointer hands its
        // cargo down UNSHIFTED); a value the bridge hands out must describe itself the same way.
        // Carried dims descend through EVERY pointer hop, not only the one whose pointee is the
        // array: `***[3]int`'s intermediate pointees are pointers, and a hop that answered null for
        // them would lose the cargo two levels before the array it describes. That is precisely
        // rtype.Elem's `throughPointer ? dims : …`.
        nint[]? carriedDims = v.typ_ == nil ? null : v.typ_.Value.arrayDims;
        nint[]? dims = carriedDims is { Length: > 0 } ? carriedDims
                     : GoReflect.KindOf(pointee) == GoReflect.Array ? GoReflect.ArrayDimsOfValue(GoReflect.ReadPointerSlot(cur))
                     : null;
        // A CHANNEL pointee reveals its direction the same way — off the value behind the box,
        // which for `new(chan<- string)` is the direction-carrying nil the converter seeded.
        GoChanDir pointeeDir = GoReflect.KindOf(pointee) == GoReflect.Chan ? GoReflect.ChanDirOfValue(GoReflect.ReadPointerSlot(cur)) : GoChanDir.Unstamped;
        // A MAP pointee's KEY dims have no live source at all — a map value cannot carry them — so
        // they descend from the descriptor or not at all.
        nint[]? pointeeKeyDims = v.typ_ == nil ? null : v.typ_.Value.keyDims;
        var t = abi.synthType(pointee, dims, null, pointeeDir, pointeeKeyDims);
        var elem = new ΔValue(t, default!, ((flag)(uintptr)(uint8)GoReflect.KindOf(pointee)) | flagAddr | flagIndir | ((flag)(v.flag & flagRO)));
        elem.addrBox = cur;
        return elem;
    }
    throw panic(Ꮡ(new ValueError("reflect.Value.Elem", v.kind())));
}

// Addr returns a pointer Value representing the address of v (v must be addressable). The bridge
// already HOLDS that address: an addressable Value ALIASES the ж<T> box its storage lives in
// (addrBox), so Addr just surfaces that box as a Pointer-kind Value — and Elem on the result
// aliases the same box, which is exactly Go's `v.Addr().Elem()` equivalence (#32772). The auto
// form derives the pointer TYPE through ptrTo → typesByString → the typelinks() runtime stub: the
// linker-built type table has no managed form, so every Addr threw NotImplementedException. gob's
// gobEncodeOpFor/gobDecodeOpFor climb one level with Addr for every GobEncoder-implementing field,
// which is why all eleven GobEncoder round-trip tests died there.
public static ΔValue Addr(this ΔValue v) {
    if ((flag)(v.flag & flagAddr) == 0) {
        throw panic("reflect.Value.Addr of unaddressable value");
    }
    if (v.addrBox is null) {
        // flagAddr without an aliased box is a bridge invariant violation, not a Go state — fail
        // loud rather than hand back a pointer to a detached copy.
        throw panic("reflect.Value.Addr of value with no aliased storage");
    }
    var p = makeReflectValue(v.addrBox);
    // Preserve flagRO instead of using v.flag.ro() so that v.Addr().Elem() is equivalent to v.
    p.flag |= (flag)(v.flag & flagRO);
    return p;
}

// Bytes returns v's underlying value (v's underlying value must be a slice of bytes or an addressable array of bytes).
// A named []byte wrapper answers through its ISlice<byte> view (sharing the backing store).
//
// The ARRAY arm is Go's bytesSlow Array case (reflect/value.go), and it is NOT optional: fmt's
// printValue calls Bytes() whenever `f.Kind() == Slice || f.CanAddr()`, so an addressable byte
// array — `Sprintf("%s", &[3]byte{'a','b','c'})`, whose pointer deref IS addressable — reaches
// here as a `go.array<byte>` and used to fall to the catch-all conversion, throwing
// InvalidCastException (array<byte> declares no conversion to slice<byte>). Go returns
// `unsafe.Slice(p, n)`, an ALIAS of the array's storage rather than a copy, which array<T>.Slice
// reproduces exactly (it windows the same backing store), so a write through the returned slice
// is still visible in the array — the semantics Go's callers may rely on.
public static slice<byte> Bytes(this ΔValue v) {
    if (v.live is array<byte> arr) {
        // Go panics on an unaddressable byte array rather than silently copying; fmt takes its own
        // element-by-element path for that case and never calls Bytes(). Both messages, and the
        // non-byte-element ones below, are Go's own text.
        if (!v.CanAddr()) {
            throw panic("reflect.Value.Bytes of unaddressable byte array");
        }
        return arr.Slice(0, (int)arr.Length);
    }
    // Go decides on the element KIND, not the element TYPE — `[]renamedByte` and
    // `type S []Uint8` qualify exactly as `[]byte` does — and it ALIASES. GoReflect.TryByteSliceView
    // is that whole relation in one place (see its banner for why the alias is not negotiable and
    // what makes the defined-element case safe); reaching it is the ONLY way a caller writing
    // through the result stays visible in the original.
    if (GoReflect.TryByteSliceView(v.live, out slice<byte> aliased)) {
        return aliased;
    }
    if (v.live is null) {
        // The nil slice: nothing to alias, and Go's own header re-typing answers the nil []byte.
        return default!;
    }
    throw panic("reflect.Value.Bytes of non-byte slice");
}

// SetBytes sets v's underlying value — the WRITE half of Bytes, and the same element-KIND relation.
//
// The auto form is `*(*[]byte)(v.ptr) = x`, which converts to a store through
// `(ж<slice<byte>>)(uintptr)(v.ptr)`: `v.ptr` is the Go data word, which this bridge never
// populates, so the store went through a box over address 0 and landed nowhere — SILENTLY, for
// EVERY byte slice including a plain []byte. encoding/json's literalStore decodes base64 into a
// fresh buffer and hands it over with exactly this call, so every []byte field decoded as empty
// and TestLargeByteSlice reported a 2000-byte round trip diverging at byte 0. The bridge writes
// where every other setter writes — through the addressable Value's aliased box — and re-spells the
// incoming []byte as the SLOT's own slice type without copying, which is what Go's header
// assignment does.
public static void SetBytes(this ΔValue v, slice<byte> x) {
    v.flag.mustBeAssignable();
    v.flag.mustBe(ΔSlice);
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    if (st is null || !GoReflect.TryByteSliceAs(st, x, out object? stored)) {
        throw panic("reflect.Value.SetBytes of non-byte slice");
    }
    if (v.addrBox is null) {
        // mustBeAssignable already rejected an unaddressable Value, so a missing box is a bridge
        // invariant violation rather than a Go state — fail loud instead of writing into a copy.
        throw panic("reflect.Value.SetBytes of value with no aliased storage");
    }
    GoReflect.WritePointerSlot(v.addrBox, stored);
}

// NumField returns the number of fields in the struct v — the PROJECTED Go fields of the
// STATIC struct type (promoted embeds project as the embedded Go field; a defined-type-over-
// struct wrapper exposes its underlying's fields; bridge companions are excluded by attribute).
public static nint NumField(this ΔValue v) {
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    return st is null ? 0 : GoReflect.GoFields(st).Length;
}

// Field returns the i'th field of the struct v: typed by the field's STATIC Go type (an
// interface-typed field reports Kind Interface; a nil-valued field is a VALID nil Value),
// ADDRESSABLE when v is (aliasing the parent box through a ValueSlot-routed field accessor —
// the increment-1 ref-accessor contract), and read-only for unexported/blank fields with the
// parent's read-only bits inherited (Go flag stickiness). The same projection indexes
// rtype.Field(i), so value- and type-side field walks can never disagree.
public static ΔValue Field(this ΔValue v, nint i) {
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    if (st is null || GoReflect.KindOf(st) != GoReflect.Struct) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Field", v.kind())));
    }
    GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(st);
    if ((nuint)i >= (nuint)fields.Length) {
        throw panic("reflect: Field index out of range");
    }
    GoReflect.GoFieldInfo f = fields[(int)i];
    // Go's two read-only bits are NOT interchangeable, and this is the one place that decides which
    // of them a field gets — the same clause as reflect's own Value.Field:
    //
    //     fl := v.flag&(flagStickyRO|flagIndir|flagAddr) | flag(typ.Kind())
    //     if !field.Name.IsExported() {
    //         if field.Embedded() { fl |= flagEmbedRO } else { fl |= flagStickyRO }
    //     }
    //
    // Both bits block a write through the field ITSELF, so reading one for the other looks
    // harmless. What differs is INHERITANCE: only flagStickyRO propagates to a child, so an
    // exported field reached THROUGH an unexported embedded struct is writable in Go — which is the
    // whole of `type S struct{ embed }` where `embed` carries exported fields, an ordinary Go idiom
    // every decoder meets. Marking an unexported EMBED sticky made every promoted field read-only,
    // and encoding/json's Unmarshal panicked in mustBeAssignable instead of filling it
    // (TestUnmarshalEmbeddedUnexported, plus TestUnmarshal's DisallowUnknownFields rows).
    // GoFieldInfo.Embedded is what makes the distinction expressible; before it the two cases were
    // indistinguishable through this projection.
    flag ro = (flag)((flag)(v.flag & flagStickyRO) | (f.Exported ? default : f.Embedded ? flagEmbedRO : flagStickyRO));
    if (v.addrBox is not null) {
        var elem = makeTypedValue(null, f.Type, f.ArrayDims, ro, f.ChanDir, f.KeyDims);
        elem.flag |= flagAddr | flagIndir;
        elem.addrBox = GoReflect.FieldAliasBox(v.addrBox, f);
        return elem;
    }
    object? cur = v.live;
    if (cur is null) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Field", v.kind())));
    }
    return makeTypedValue(f.Read(cur), f.Type, f.ArrayDims, ro, f.ChanDir, f.KeyDims);
}

// UnsafePointer returns v's value as an unsafe.Pointer (v must be a Chan, Func, Map, Pointer, or
// UnsafePointer). A managed pointer (ж<T>) has no numeric address, so return a STABLE non-zero
// object-identity token for a non-nil pointer (opaque, like the guintptr manual model) and 0 for nil —
// fmt uses it only to test nil-ness (`f.UnsafePointer() != nil`) and to print an address for %p.
public static @unsafe.Pointer UnsafePointer(this ΔValue v) {
    return ((@unsafe.Pointer)reflectPointerToken(v));
}

// Pointer returns v's value as a uintptr (the deprecated form of UnsafePointer).
public static uintptr Pointer(this ΔValue v) {
    return reflectPointerToken(v);
}

// A slice's Go data address is `&s[0]` — its BACKING STORE plus its window offset — so the token
// combines the two, exactly as deepValueEqual's identityRoot does. A nil slice has no storage and
// tokens 0, which is what the nil test one level up already answers for every other kind.
private static uintptr sliceStorageToken(object boxed) {
    (object? data, nint low) = sliceData(boxed);
    return data is null
        ? 0
        : ((uintptr)(nuint)(uint)System.HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(data), low));
}

private static uintptr reflectPointerToken(ΔValue v) {
    object? cur = v.live;
    while (cur is IInterfaceAdapter { Value: not null } interfaceAdapter) {
        cur = interfaceAdapter.Value;
    }
    if (cur is IжAdapter { Box: not null } pointerAdapter) {
        cur = pointerAdapter.Box;
    }
    if (cur is null || (cur is INilPointer nilable && nilable.IsNilPointer)) {
        return 0;
    }
    // A TYPE DESCRIPTOR pointer is ordered by the type it describes, never by its box identity —
    // see typeDescriptorOrderToken.
    if (typeDescriptorOrderToken(cur) is {} descriptorToken) {
        return descriptorToken;
    }
    // Pointer-bearing golib values answer their own stable, order-consistent address token
    // (equal pointers token equally; same-storage element pointers order by index; channel
    // copies share their core's token — internal/fmtsort orders map keys by this).
    //
    // A MAP or a SLICE is the case that cannot use the boxed value's own identity: Go's
    // UnsafePointer answers the STORAGE address — the hmap for a map, `&s[0]` for a slice — while
    // the managed value is a HEADER STRUCT, freshly boxed on every read out of a slot. So two
    // reads of ONE Go map tokened differently, which is not a wrong ORDER (nothing orders maps) but
    // a broken IDENTITY, and identity is exactly what encoding/json's cycle detector asks for:
    // `e.ptrSeen[v.UnsafePointer()]` never matched an entry it had itself stored, so a
    // self-referential map or slice was never detected, `interfaceEncoder`→`mapEncoder` recursed
    // without bound, and the process died of stack exhaustion (0xc00000fd) — taking every verdict
    // the run had not yet produced with it. The storage identity is the SAME root deepValueEqual
    // keys its cycle detection on (mapBacking / sliceData), so the two walks cannot disagree about
    // what "the same map" means. A slice folds its window offset in, as Go's `&s[0]` does.
    // Anything else (a func delegate) falls back to reference identity.
    uintptr token = cur switch {
        INilPointer p => ((uintptr)p.PointerOrderToken),
        IChannel c => ((uintptr)c.PointerOrderToken),
        IMap => ((uintptr)(nuint)(uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(mapBacking(cur) ?? cur)),
        ISlice => sliceStorageToken(cur),
        _ => ((uintptr)(nuint)(uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(cur))
    };
    // Go also permits the OTHER direction — converting the scalar back to a pointer and
    // dereferencing it (`(*bool)(v.FieldByName(name).Addr().UnsafePointer())`, go/types'
    // check_test.go) — which an order token alone cannot serve, because the box it named is
    // exactly what the projection to a scalar drops. Remember the association so the uintptr →
    // pointer conversion can put it back; the token VALUE is unchanged, so every consumer that
    // only orders or nil-tests the result sees precisely what it saw before.
    // The type-descriptor path above is deliberately excluded: its tokens are packed type NAMES,
    // shared by every descriptor with the same name, and so are not identities to recover.
    ManagedPointerTokens.Register((nuint)token.Value, cur);
    return token;
}

// typeDescriptorOrderToken answers the order token for a pointer to a TYPE DESCRIPTOR (*rtype or
// *abi.Type), which is the one pointer whose ordering is OBSERVABLE in ordinary program output:
// internal/fmtsort compares interface-kinded map keys by their dynamic types, and it does that by
// comparing the two descriptor pointers arithmetically (`compare(reflect.ValueOf(a.Elem().Type()),
// …)` lands in the ΔPointer branch), so this token IS the printed order of
// `fmt.Println(map[I]int{…})`.
//
// Go answers with the descriptor's machine address, i.e. the linker's type-section layout. That is
// deliberately unspecified — fmtsort's own TestInterface says "the relative ordering of types is
// unspecified" and asserts only that same-type keys form contiguous groups — and it is not a
// function of anything the managed side can see (measured: Go orders `main.Apple` before
// `main.Mango` in one program and after it in another).
//
// The box identity hash the general path falls back to is WORSE than unspecified, though: it is
// drawn from a per-thread PRNG, so it is fixed for a given build but bears no relation to the type,
// and the printed order flips whenever an unrelated edit shifts how many hashes are drawn before
// these. Order descriptors by their Go type NAME instead — the only key that is stable across
// builds, runs and unrelated edits — by packing the name's leading bytes big-endian, so comparing
// tokens arithmetically compares the names lexically. The name is the one `Type.String()` prints,
// so types that print alike token alike and same-type grouping is exact.
//
// Names agreeing over the whole packed prefix tie; fmtsort then falls through to its concrete-value
// comparison, the same arm Go reaches for two keys of one type. Returns null for anything that is
// not a descriptor, and for a descriptor with no managed type or an empty name, since zero is the
// reserved nil token.
private static uintptr? typeDescriptorOrderToken(object box) {
    System.Type? st = null;
    nint[]? dims = null;
    switch (box) {
    case ж<rtype> Ꮡrt:
        st = Ꮡrt.Value.t.sysType;
        dims = Ꮡrt.Value.t.arrayDims;
        break;
    case ж<abi.Type> Ꮡt:
        st = Ꮡt.Value.sysType;
        dims = Ꮡt.Value.arrayDims;
        break;
    }
    if (st is null) {
        return null;
    }
    byte[] name = System.Text.Encoding.UTF8.GetBytes(GoReflect.GoTypeName(st, dims));
    nuint token = 0;
    for (int i = 0; i < System.IntPtr.Size; i++) {
        nuint b = i < name.Length ? name[i] : (nuint)0;
        token = (token << 8) | b;
    }
    return token == 0 ? null : ((uintptr)token);
}

// The managed backing for a MapIter: the map's enumerator (a golib map<K,V> enumerates as
// IEnumerable of KeyValuePair<K,V>). The Go hiter-based iteration has no managed form.
partial struct MapIter {
    [GoReflectCompanion] internal IEnumerator? mapEnum;

    // The map's DECLARED key and value types, plus the map Value's read-only bits. Go types every
    // entry Value by the map's declared types — the same slot rule Index/Field follow — so an
    // interface-keyed map yields Kind Interface keys whatever the dynamic value is, and a NIL key or
    // value is a VALID nil Value of that type rather than the invalid zero Value.
    [GoReflectCompanion] internal System.Type? mapKeyType;
    [GoReflectCompanion] internal System.Type? mapValueType;
    [GoReflectCompanion] internal flag mapRO;
}

// MapRange returns a range iterator for a map.
public static ж<MapIter> MapRange(this ΔValue v) {
    ref var it = ref heap<MapIter>(out var Ꮡit);
    if (v.live is IEnumerable e) {
        it.mapEnum = e.GetEnumerator();
    }
    System.Type? mapType = v.typ_ == nil ? null : v.typ_.Value.sysType;
    it.mapKeyType = GoReflect.KeyType(mapType);
    it.mapValueType = GoReflect.ElementType(mapType);
    it.mapRO = (flag)(v.flag & flagRO);
    return Ꮡit;
}

// MapKeys returns a slice containing all the keys present in the map, in unspecified order.
//
// The converted body reinterprets the descriptor as a *mapType (`v.typ().Reinterpret<abi.Type,
// mapType>()`) to read the map's key type off the embedded abi.MapType. That reinterpret is NOT the
// managed-box aliasing case toRType relies on: a synthesized descriptor is a bare abi.Type with no
// abi.MapType allocated behind it, and the emitted mapType holds its embed as a REFERENCE (the
// promoted ᏑʗMapType box), so the reinterpreted field reads whatever the descriptor's first word
// happens to be — go/ast's TestPrint died on exactly that. Iteration is the same hiter/mapiterinit
// machinery MapRange already replaced, so MapKeys is MapRange collected: the key-typing rule
// (declared key type, nil key included, flagRO inherited) stays in ONE place.
public static slice<ΔValue> MapKeys(this ΔValue v) {
    v.flag.mustBe(Map);
    // Presized from Len and TRIMMED to what iteration actually yielded, exactly as Go's own body
    // does: the length is read before the walk, so a concurrent writer can only make the walk
    // shorter (Go tolerates the race and documents it as the caller's problem).
    var keys = new ΔValue[(nint)v.Len()];
    nint i = 0;
    var iter = v.MapRange();
    while (i < keys.Length && iter.Next()) {
        keys[i] = iter.Key();
        i++;
    }
    return new slice<ΔValue>(keys)[..((int)i)];
}

// MapIndex returns the value associated with key in the map v, or the INVALID zero Value when the
// key is absent or v is a nil map. Same root as MapKeys above — the converted body reinterprets the
// descriptor as a *mapType and then reads the entry through Go's mapaccess/mapaccess_faststr
// runtime intrinsics. The key marshals into the map's STATIC key type under Go assignability, the
// same relation (and the same failure-text shape) SetMapIndex applies on the write side.
public static ΔValue MapIndex(this ΔValue v, ΔValue key) {
    v.flag.mustBe(Map);
    System.Type? st = v.typ_ == nil ? null : v.typ_.Value.sysType;
    System.Type? keyType = GoReflect.KeyType(st);
    System.Type? elemType = GoReflect.ElementType(st);
    object? liveMap = v.live;
    // Go: indexing a nil map is legal and yields the zero Value — unlike ASSIGNING to one, which
    // panics — so this is a miss, not an error.
    if (liveMap is null || keyType is null || elemType is null || (liveMap is IMap nilProbe && nilProbe.IsNil)) {
        return new ΔValue(nil);
    }
    if (!GoReflect.TryMarshalAssignable(key.live, keyType, out object? k)) {
        throw panic("reflect.Value.MapIndex: key of type " + GoReflect.GoTypeName(key.live?.GetType()) +
                    " is not assignable to type " + GoReflect.GoTypeName(keyType));
    }
    if (!GoReflect.TryGetMapEntry(liveMap, keyType, elemType, k, out object? e)) {
        return new ΔValue(nil);
    }
    // Typed by the map's DECLARED element type, inheriting BOTH operands' read-only bits (Go's
    // `fl := (v.flag | key.flag).ro()`) — the same slot rule MapIter.Value follows, so a lookup and
    // a range over one map agree.
    return makeTypedValue(e, elemType, null, (flag)(v.flag | key.flag));
}

// ==== Phase-3 write-back: Set, Zero, methodName ====

// Set assigns x to the value v (v must be addressable and x assignable to v's type — Go's
// assignTo). Marshalling and the assignability decision share the golib machinery emitted
// asserts use (GoReflect.TryMarshalAssignable): identity — with adapter/box unwrap, so an
// interface-held *T stores its receiver box — or interface-implements, where a typed-nil
// pointer source stores its canonical nil box wrapped for the destination (a NON-nil interface
// holding (*T)(nil), Go's packEface result). The store writes through the aliased ж box's slot
// ref; a structurally nil box panics Go-style before any write (blessing condition Q1a).
public static void Set(this ΔValue v, ΔValue x) {
    v.flag.mustBeAssignable();
    x.flag.mustBeExported();
    System.Type? dstType = v.typ_ == nil ? null : v.typ_.Value.sysType;
    if (dstType is null || v.addrBox is null) {
        throw panic("reflect: Set using unaddressable value");
    }
    if (!GoReflect.TryMarshalAssignable(x.live, dstType, out object? marshalled)) {
        throw panic("reflect.Set: value of type " + GoReflect.GoTypeName(x.live?.GetType()) +
                    " is not assignable to type " + GoReflect.GoTypeName(dstType));
    }
    GoReflect.WritePointerSlot(v.addrBox, marshalled);
}

// Zero returns a Value representing the zero value for the specified type, total over every
// kind through the shared golib rule (GoReflect.ZeroValueOf): pointer kinds the canonical
// typed-nil box (one nil encoding system-wide); interface/func kinds a valid nil Value;
// slice/map/chan kinds their nil container struct default; array kinds a dims-sized backing
// when the descriptor carries dims. quick's sizedValue probes Zero(t).Interface().(Generator)
// for EVERY generated type, so Zero must never throw for a representable kind.
public static ΔValue Zero(ΔType typ) {
    if (typ == default!) {
        throw panic("reflect: Zero(nil)");
    }
    System.Type? st = sysTypeOfReflectType(typ);
    if (st is null) {
        throw panic("reflect: Zero of non-synthesized type");
    }
    nint[]? dims = arrayDimsOfReflectType(typ);
    // The fabricated zero carries the descriptor's CHANNEL DIRECTION for the same reason it is
    // sized from its array dims: the value must describe itself the way the type does, or the
    // cargo dies the moment it is boxed and re-described.
    GoChanDir chanDir = chanDirOfReflectType(typ);
    return makeTypedValue(GoReflect.ZeroValueOf(st, dims, chanDir), st, dims, default, chanDir);
}

// New returns a Value representing a pointer to a new zero value for the specified type —
// a fresh ж<T> heap box (never nil; the canonical-nil singleton is a DIFFERENT instance), its
// pointee sized from the descriptor's array dims when present (reflect.New([100]T) must
// allocate a real 100-element backing — TestSliceRoundTrip's dst side).
public static ΔValue New(ΔType typ) {
    if (typ == default!) {
        throw panic("reflect: New(nil)");
    }
    System.Type? st = sysTypeOfReflectType(typ);
    if (st is null) {
        throw panic("reflect: New of non-synthesized type");
    }
    nint[]? dims = arrayDimsOfReflectType(typ);
    GoChanDir chanDir = chanDirOfReflectType(typ);
    object box = GoReflect.NewPointerBox(st, GoReflect.ZeroValueOf(st, dims, chanDir));
    // The POINTER descriptor carries its pointee's direction unshifted, so Elem() hands it back.
    return makeTypedValue(box, typeof(ж<>).MakeGenericType(st), null, default, chanDir);
}

// MakeSlice creates a new zero-initialized slice value for the specified slice type, length,
// and capacity — through the same ISupportMake construction `make()` emissions use, so a NAMED
// slice type yields the wrapper (Go's named result). The result is not addressable; its
// ELEMENTS are (through the shared backing).
public static ΔValue MakeSlice(ΔType typ, nint len, nint cap) {
    System.Type? st = sysTypeOfReflectType(typ);
    if (st is null || GoReflect.KindOf(st) != GoReflect.Slice) {
        throw panic("reflect.MakeSlice of non-slice type");
    }
    if (len < 0) {
        throw panic("reflect.MakeSlice: negative len");
    }
    if (cap < 0) {
        throw panic("reflect.MakeSlice: negative cap");
    }
    if (len > cap) {
        throw panic("reflect.MakeSlice: len > cap");
    }
    return makeTypedValue(GoReflect.MakeContainer(st, len, cap), st, null, default);
}

// MakeMap creates a new empty map value of the specified map type.
public static ΔValue MakeMap(ΔType typ) {
    return MakeMapWithSize(typ, 0);
}

// MakeMapWithSize creates a new empty map value of the specified map type with a size hint.
public static ΔValue MakeMapWithSize(ΔType typ, nint n) {
    System.Type? st = sysTypeOfReflectType(typ);
    if (st is null || GoReflect.KindOf(st) != GoReflect.Map) {
        throw panic("reflect.MakeMapWithSize of non-map type");
    }
    return makeTypedValue(GoReflect.MakeContainer(st, n), st, null, default);
}

// SetMapIndex sets the element associated with key in the map v (v must be a Map; the key and
// elem marshal under Go assignability into the map's STATIC key/element types, through the
// golib IDictionary surface both raw maps and named wrappers implement).
public static void SetMapIndex(this ΔValue v, ΔValue key, ΔValue elem) {
    v.flag.mustBe(Map);
    v.flag.mustBeExported();
    System.Type st = v.typ_.Value.sysType!;
    object? liveMap = v.live;
    if (liveMap is null || (liveMap is IMap m && m.IsNil)) {
        throw panic("assignment to entry in nil map");
    }
    System.Type keyType = GoReflect.KeyType(st)!;
    System.Type elemType = GoReflect.ElementType(st)!;
    if (elem.flag == 0) {
        // Go: an invalid elem DELETES the key — no demonstrated consumer yet.
        throw new NotImplementedException("reflect.Value.SetMapIndex: delete-on-invalid-elem is not implemented (next consumer: encoding/json)");
    }
    if (!GoReflect.TryMarshalAssignable(key.live, keyType, out object? k)) {
        throw panic("reflect.Value.SetMapIndex: key of type " + GoReflect.GoTypeName(key.live?.GetType()) +
                    " is not assignable to type " + GoReflect.GoTypeName(keyType));
    }
    if (!GoReflect.TryMarshalAssignable(elem.live, elemType, out object? e)) {
        throw panic("reflect.Value.SetMapIndex: value of type " + GoReflect.GoTypeName(elem.live?.GetType()) +
                    " is not assignable to type " + GoReflect.GoTypeName(elemType));
    }
    GoReflect.SetMapEntry(liveMap, keyType, elemType, k, e);
}

// ==== the Set{Bool,Int,Uint,Float,Complex,String,Zero} family — one kinded-store rule ====
// Go semantics verified against Go 1.23 reflect: integer stores TRUNCATE to the slot's width
// (no overflow panic), floats/complex narrow; a NAMED slot constructs its wrapper from the
// coerced underlying (GoReflect.TryConvertTo — the single convertibility relation). The store
// writes through the aliased box's slot ref; a structurally nil box panics Go-style (Q1a).

private static void setKinded(ΔValue v, object wide, @string op) {
    v.flag.mustBeAssignable();
    System.Type? slotType = v.typ_ == nil ? null : v.typ_.Value.sysType;
    if (slotType is null || v.addrBox is null) {
        throw panic("reflect: " + op + " using unaddressable value");
    }
    if (!GoReflect.TryConvertTo(wide, slotType, out object? converted)) {
        throw panic("reflect: call of reflect.Value." + op + " on " + v.kind().String() + " Value");
    }
    GoReflect.WritePointerSlot(v.addrBox, converted);
}

public static void SetBool(this ΔValue v, bool x) {
    setKinded(v, x, "SetBool"u8);
}

public static void SetInt(this ΔValue v, int64 x) {
    setKinded(v, x, "SetInt"u8);
}

public static void SetUint(this ΔValue v, uint64 x) {
    setKinded(v, x, "SetUint"u8);
}

public static void SetFloat(this ΔValue v, float64 x) {
    setKinded(v, x, "SetFloat"u8);
}

public static void SetComplex(this ΔValue v, complex128 x) {
    setKinded(v, x, "SetComplex"u8);
}

public static void SetString(this ΔValue v, @string x) {
    setKinded(v, x, "SetString"u8);
}

// SetZero sets v to be the zero value of v's type — the same zero rule Zero/New use.
public static void SetZero(this ΔValue v) {
    v.flag.mustBeAssignable();
    System.Type? slotType = v.typ_ == nil ? null : v.typ_.Value.sysType;
    if (slotType is null || v.addrBox is null) {
        throw panic("reflect: SetZero using unaddressable value");
    }
    GoReflect.WritePointerSlot(v.addrBox, GoReflect.ZeroValueOf(slotType, arrayDimsOfDescriptor(v.typ_)));
}

// ==== Value.Call — delegate DynamicInvoke over the converted func value ====

// Call calls the function v with the input arguments in, marshalled under the SAME
// assignability rule emitted asserts use, and returns the outputs as Values typed by the
// func's STATIC out types (a nil result is a VALID nil Value of the out type). A converted Go
// multi-return is a ValueTuple, destructured positionally. A panic inside the callee is
// unwrapped from TargetInvocationException and rethrown untouched.
//
// A VARIADIC func takes the other path. Go's contract is that Call itself builds the tail slice
// from the trailing arguments (CallSlice is the form that takes it pre-built), so the arity rule
// changes shape — the last In() is the tail SLICE, every argument from that position on is
// assignable to its ELEMENT, and there is no upper bound. The invocation changes too: the tail
// lowers to `params Span<T>`, which no reflective invoke can carry, so it goes through
// GoReflect.InvokeVariadic's typed dispatch (see that method for why). Everything else — the
// marshalling rule, the zero-Value guard, the result unpacking — is shared with the fixed path.
public static slice<ΔValue> Call(this ΔValue v, slice<ΔValue> @in) {
    v.flag.mustBe(Func);
    v.flag.mustBeExported();
    object? fn = v.live;
    if (fn is null) {
        throw panic("reflect.Value.Call: call of nil function");
    }
    var del = (Delegate)fn;
    if (!GoReflect.TryFuncShape(del.GetType(), out System.Type[]? ins, out System.Type[]? outs, out bool isVariadic)) {
        throw panic("reflect.Value.Call: not a func value");
    }
    if (isVariadic) {
        return callVariadic(del, @in, ins, outs);
    }
    if (len(@in) < ins.Length) {
        throw panic("reflect: Call with too few input arguments");
    }
    if (len(@in) > ins.Length) {
        throw panic("reflect: Call with too many input arguments");
    }
    object?[] args = new object?[ins.Length];
    for (nint i = 0; i < ins.Length; i++) {
        args[i] = marshalCallArg(@in[i], ins[i]);
    }
    object? result;
    try {
        result = del.DynamicInvoke(args);
    } catch (TargetInvocationException tie) when (tie.InnerException is not null) {
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
        throw;
    }
    return callResults(result, outs);
}

// The variadic arm of Call. `ins` counts the tail as ONE parameter (TryFuncShape reports it as
// Go does, `[]T` rather than the emitted Span<T>), so `ins.Length - 1` fixed arguments are
// marshalled positionally and everything after them packs into a fresh T[] the typed dispatch
// hands over as the Span. A panic inside the callee needs no unwrapping here: the trampoline
// calls the delegate directly, so nothing wraps it in a TargetInvocationException.
private static slice<ΔValue> callVariadic(Delegate del, slice<ΔValue> @in, System.Type[] ins, System.Type[] outs) {
    nint fixedCount = ins.Length - 1;
    if (len(@in) < fixedCount) {
        throw panic("reflect: Call with too few input arguments");
    }
    object?[] args = new object?[fixedCount];
    for (nint i = 0; i < fixedCount; i++) {
        args[i] = marshalCallArg(@in[i], ins[i]);
    }
    // ins[^1] is the Go tail type `[]T`; its element is what each trailing argument marshals to.
    System.Type tailElem = ins[^1].GetGenericArguments()[0];
    // `Array` alone is reflect.Array (a Kind constant is in scope here), so this one is qualified.
    System.Array tail = System.Array.CreateInstance(tailElem, (int)(len(@in) - fixedCount));
    for (nint i = fixedCount; i < len(@in); i++) {
        tail.SetValue(marshalCallArg(@in[i], tailElem), (int)(i - fixedCount));
    }
    return callResults(GoReflect.InvokeVariadic(del, args, tail), outs);
}

// One argument, marshalled under the assignability rule emitted asserts use.
//
// Into INTERFACE space the argument packs exactly the way Value.Interface() packs it, because Go's
// assignment to an interface-typed parameter BUILDS an eface and an eface keeps the type half of a
// nil pointer. Reading arg.live straight out handed a bare C# null across that boundary and erased
// it — the very loss packInterfaceValue exists to prevent one call away, reached through a third
// path that had not joined the one-nil-encoding rule. Downstream the erasure is total: the callee's
// `reflect.ValueOf(arg)` answers the INVALID zero Value, so text/template's printableValue reported
// "<no value>" for a nil *int where Go prints "<nil>" (exec_test's "html typed nil" row), and any
// callee asserting the parameter to an interface takes the failure arm.
//
// A CONCRETE parameter type is untouched: no eface is built there, so the box/null the slot already
// holds is what Go assigns.
private static object? marshalCallArg(ΔValue arg, System.Type want) {
    if (arg.flag == 0) {
        throw panic("reflect: " + "Call" + " using zero Value argument");
    }
    if (!marshalIntoSlot(arg, want, out object? marshalled)) {
        throw panic("reflect: Call using " + GoReflect.GoTypeName(arg.live?.GetType()) +
                    " as type " + GoReflect.GoTypeName(want));
    }
    return marshalled;
}

// The rule above with the message left to the caller — shared with Value.send, the OTHER boundary
// that assigns a Value into a typed slot. Both would otherwise reach Go's assignTo, whose managed
// form returns a Value carrying only the never-populated raw `ptr` slot: the boxed companion the
// bridge actually reads is dropped, so the receiving side gets null. Keeping the two on one
// renderer is what makes a channel send and a call argument box a typed nil the same way.
private static bool marshalIntoSlot(ΔValue arg, System.Type want, out object? marshalled) {
    object? source = want.IsInterface || want == typeof(object) ? packInterfaceValue(arg) : arg.live;
    return GoReflect.TryMarshalAssignable(source, want, out marshalled);
}

// The delegate's raw result as Values typed by the func's STATIC out types — a Go multi-return
// arrives as a ValueTuple and destructures positionally.
private static slice<ΔValue> callResults(object? result, System.Type[] outs) {
    var ret = new slice<ΔValue>(outs.Length);
    if (outs.Length == 1) {
        ret[0] = makeTypedValue(result, outs[0], null, default);
    } else if (outs.Length > 1) {
        var tuple = (System.Runtime.CompilerServices.ITuple)result!;
        for (nint i = 0; i < outs.Length; i++) {
            ret[i] = makeTypedValue(tuple[(int)i], outs[i], null, default);
        }
    }
    return ret;
}

// ==== Value.Method — a method value is the receiver BOUND into an ordinary func value ====

// Method returns a func value for v's i'th method, with v already bound as the receiver, so a
// Call on it takes only the method's own arguments — Go's method-value contract. Go carries that
// as v's own Value plus a flagMethod bit and the index packed into the flag, then rebuilds the
// signature (typeSlow) and re-resolves the receiver (methodReceiver) on every use, all reads of
// the uncommon() table a synthesized descriptor never populates. Binding the receiver into a
// managed delegate HERE makes the result an ordinary Kind-Func Value, so Type(), NumIn/In/NumOut/
// Out and Call are the existing bridge surface rather than new machinery — and the receiver is
// already gone from the signature, which is exactly what Go's method value reports. Read-only
// bits are inherited (Go's flagRO stickiness), so Call still refuses a method value obtained
// through an unexported field.
public static ΔValue Method(this ΔValue v, nint i) {
    if (v.typ_ == nil) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Method"u8, Invalid)));
    }
    System.Type? st = v.typ_.Value.sysType;
    if (i < 0 || i >= GoReflect.GoMethodCount(st)) {
        throw panic("reflect: Method index out of range");
    }
    object? recv = v.live;
    if (v.kind() == ΔInterface && recv is null) {
        throw panic("reflect: Method on nil interface value");
    }
    var bound = GoReflect.GoMethodValue(st, (int)i, recv);
    return makeTypedValue(bound, bound.GetType(), null, (flag)(v.flag & flagRO));
}

// CallSlice is unimplemented on the bridge, and its named next consumer is RETIRED as measured
// wrong: text/template's safeCall reaches Go through `fun.Call(args)` and never CallSlice
// (funcs.go:375), so implementing Call's variadic arm cleared it with nothing left here to serve.
// A GOROOT-wide census finds no other caller either. The machinery it would need now exists
// (GoReflect.InvokeVariadic) — the tail Value would be unpacked into the array instead of built
// from the trailing arguments — so this stays a stub for want of a consumer, not for want of a way.
public static slice<ΔValue> CallSlice(this ΔValue v, slice<ΔValue> @in) {
    throw new NotImplementedException("reflect.Value.CallSlice is not implemented (no demonstrated consumer)");
}

// sysTypeOfReflectType recovers the managed System.Type a canonical reflect.Type wrapper
// describes (the rtype's abi.Type carries it — synthType stamped it).
private static System.Type? sysTypeOfReflectType(ΔType typ) {
    var (rt, ok) = typ._<ж<rtype>>(ᐧ);
    return ok && rt != nil ? rt.Value.t.sysType : null;
}

// arrayDimsOfReflectType recovers the descriptor's carried array dims (non-identity cargo).
private static nint[]? arrayDimsOfReflectType(ΔType typ) {
    var (rt, ok) = typ._<ж<rtype>>(ᐧ);
    return ok && rt != nil ? rt.Value.t.arrayDims : null;
}

// chanDirOfReflectType recovers the descriptor's carried channel direction (non-identity cargo).
private static GoChanDir chanDirOfReflectType(ΔType typ) {
    var (rt, ok) = typ._<ж<rtype>>(ᐧ);
    return ok && rt != nil ? rt.Value.t.chanDir : GoChanDir.Unstamped;
}

private static nint[]? arrayDimsOfDescriptor(ж<abi.Type> Ꮡt) {
    return Ꮡt == nil ? null : Ꮡt.Value.arrayDims;
}

// methodName returns a best-effort Go-shaped name of the calling reflect method for panic
// messages ("reflect.Value.Set using unaddressable value"). Go resolves it from the PC via
// runtime.Caller — unimplementable here (no Go stack); walk the managed stack to the first
// converted-package frame instead. The name is only ever observed in panic text.
internal static @string methodName() {
    var trace = new System.Diagnostics.StackTrace(2, false);
    for (int i = 0; i < trace.FrameCount; i++) {
        var method = trace.GetFrame(i)?.GetMethod();
        System.Type? decl = method?.DeclaringType;
        if (method is null || decl is null) {
            continue;
        }
        if (decl.Name.EndsWith("_package") && !method.Name.StartsWith("mustBe")) {
            return (@string)(decl.Name[..^"_package".Length] + "." + method.Name);
        }
    }
    return "unknown method"u8;
}

// Next advances the map iterator and reports whether there is another entry.
[GoRecv] public static bool Next(this ref MapIter iter) {
    return iter.mapEnum is not null && iter.mapEnum.MoveNext();
}

// Key returns the key of the iterator's current map entry — typed by the map's DECLARED key type
// (see MapIter): Go's `map[any]V` hands out Kind Interface keys, and a NIL key (golib keeps it in a
// dedicated slot, since Dictionary rejects a null key) is a valid nil Value of that type. Inferring
// the type from the boxed key instead left a nil key as the INVALID zero Value, which
// internal/fmtsort's key ordering cannot compare at all — `compare` fell through to
// `panic("bad type in compare: " + aType.String())` on a nil type, so PRINTING any map with a nil
// key died inside fmt.
[GoRecv] public static ΔValue Key(this ref MapIter iter) {
    object? cur = iter.mapEnum?.Current;
    object? key = cur?.GetType().GetProperty("Key")?.GetValue(cur);
    return iter.mapKeyType is null ? makeReflectValue(key) : makeTypedValue(key, iter.mapKeyType, null, iter.mapRO);
}

// Value returns the value of the iterator's current map entry, typed by the map's declared value
// type (see Key).
[GoRecv] public static ΔValue Value(this ref MapIter iter) {
    object? cur = iter.mapEnum?.Current;
    object? value = cur?.GetType().GetProperty("Value")?.GetValue(cur);
    return iter.mapValueType is null ? makeReflectValue(value) : makeTypedValue(value, iter.mapValueType, null, iter.mapRO);
}

// ==== reflect.Type canonicalization (hand-owned Value.Type + toType) ====
// Go's reflect.Type is a canonical interned descriptor: TypeOf(x) == TypeOf(y) exactly when x and y
// have the same dynamic type, so `aType == bType` is a pointer compare that internal/fmtsort.compare
// relies on (`if aType != bType { return -1 }`). The managed bridge synthesizes a fresh abi.Type box
// per TypeOf call and wraps it in a fresh rtypeжΔType (an IжAdapter compared by box identity), so two
// Types describing the same Go type never compared equal — compare() always returned -1 and the stable
// sort REVERSED the map keys (map[b:2 a:1] instead of map[a:1 b:2]). Intern the ΔType wrapper by the
// underlying System.Type so identity-equality matches Go. The cache is process-lifetime (type
// descriptors are permanent, exactly like Go's). See docs/phase4/DESIGN-reflection-bridge.md.
private static readonly System.Collections.Concurrent.ConcurrentDictionary<(System.Type, string), ΔType> s_canonTypeCache = new();

// (toRType stays AUTO: the ruled managed-box reinterpret model — FINDING-managed-box-uintptr-
// lifetime.md — makes the converter emit `Ꮡt.Reinterpret<abi.Type, rtype>()`, a GC-safe
// storage-aliasing box, so the descriptor's sysType/arrayDims cargo reads live through the
// managed reference; no hand-owned form is needed.)

// valueMethodName is Go's runtime.Callers-based caller-name resolution for Value panic
// messages (flag.mustBe's ValueError) — unimplementable over getcallersp; walk the managed
// stack like methodName. The name is only ever observed in panic text.
internal static @string valueMethodName() {
    return methodName();
}

// canonType returns the canonical reflect.Type wrapper for the underlying type of Ꮡt, keyed by
// the managed System.Type synthType stamped on the abi.Type PLUS the descriptor's carried array
// dims (increment 2): [4]byte and [8]byte are DISTINCT Go types and must intern separately, or
// the first to intern would answer Len()/Size() for both. A dims-less array descriptor (a
// type-only path — no value, no field source) interns as its own knowledge class; comparing it
// to a dims-carrying Type of the same Go type is the recorded under-equal residual (no measured
// consumer does). A nil descriptor maps to the nil Type; a descriptor with no System.Type
// (never synthesized) falls back to a fresh, uninterned wrapper.
internal static ΔType canonType(ж<abi.Type> Ꮡt) {
    if (Ꮡt == nil) {
        return default!;
    }
    System.Type? st = Ꮡt.Value.sysType;
    if (st is null) {
        // No System.Type stamped on the descriptor: the feeding path did not go through
        // abi.synthType. Such a wrapper is UN-interned — it would compare unequal to the
        // canonical Type for the same Go type, silently reintroducing the reversed-map-sort
        // bug this file fixes. This branch is dead today (synthType always stamps sysType
        // after its own nil guard, and every canonType caller feeds a synthType/abi.TypeOf
        // box or nil), so assert to surface a future non-canonical feeder LOUDLY in dev
        // (Debug builds) while still degrading gracefully in Release rather than crashing.
        System.Diagnostics.Debug.Assert(false,
            "reflect.canonType: abi.Type has no System.Type (synthType was bypassed); the " +
            "resulting reflect.Type is non-canonical. Route the feeding path through abi.synthType.");
        return new rtypeжΔType(toRType(Ꮡt));
    }
    // The key is the descriptor's OWN dims-knowledge rendering (abi.descriptorDimsKey), so a Type
    // wrapper and the descriptor it wraps intern under exactly the same classes — including a func
    // type's per-parameter dims, without which `func([32]byte) bool` and `func([64]byte) bool`
    // (ONE managed delegate type, no arrayDims of their own) would share a wrapper and the first to
    // intern would answer In(0).Len() for both.
    string dimsKey = abi.descriptorDimsKey(Ꮡt.Value.arrayDims, Ꮡt.Value.funcParamDims, Ꮡt.Value.chanDir, Ꮡt.Value.keyDims);
    return s_canonTypeCache.GetOrAdd((st, dimsKey), _ => new rtypeжΔType(toRType(Ꮡt)));
}

// Type returns v's type. Hand-owned so the common (non-method) fast path returns the CANONICAL Type
// (canonType); the method-value path stays in the auto typeSlow. Mirrors the auto Value.Type shape.
public static ΔType Type(this ΔValue v) {
    if (v.flag != 0 && (flag)(v.flag & flagMethod) == 0) {
        return canonType(v.typ_);
    }
    return v.typeSlow();
}

// toType converts a *rtype to a client-facing reflect.Type, coalescing multiple descriptors for the
// same underlying type into a single canonical Type (Go's gc interns descriptors; the managed bridge
// interns here). Hand-owned so reflect.TypeOf routes through canonType. The hand-owned rtype.Elem/
// Field re-synthesize their element/field descriptor via abi.synthType and route here too, so they
// are canonical as well. NOTE: rtype.In/Out/Key also call toType, but they read func/map sub-
// descriptors that synthType never populates, so they currently NRE / return the nil Type — an
// unimplemented bridge gap, NOT canonical (tracked separately); do not rely on their identity.
internal static ΔType toType(ж<abi.Type> Ꮡt) {
    return canonType(Ꮡt);
}

// ==== Type side: reflect.rtype's ΔType methods over the abi.Type's carried System.Type ====
// rtype wraps an abi.Type by value, so `Ꮡt.Value.t.sysType` is the managed System.Type the Phase-1
// synthType stamped on the descriptor. These bypass Go's name/offset resolution (resolveNameOff, a
// stub) entirely, deriving Go type info from System.Type via GoReflect.

// String returns the Go source type string (`main.Point`, `[]int`, `*T`) — the value of %T.
internal static @string String(this ж<rtype> Ꮡt) {
    return (@string)GoReflect.GoTypeName(Ꮡt.Value.t.sysType, Ꮡt.Value.t.arrayDims, Ꮡt.Value.t.chanDir, Ꮡt.Value.t.keyDims);
}

// Name returns the type's name within its package (empty for an unnamed composite). The gate is
// GoReflect.HasGoName — the managed stand-in for the descriptor's TFlagNamed bit, which a
// synthesized abi.Type never carries. It was `ElementType(st) is not null` until 2026-08-11: a
// proxy for "unnamed composite" that also caught every DEFINED container type, so `type testSET
// []int` reported "" while PkgPath() — reading the same managed nesting — reported "main".
// encoding/asn1's getUniversalType picks the SET tag on `HasSuffix(t.Name(), "SET")` alone, so
// TestMarshal #37 marshalled 0x30 SEQUENCE where Go writes 0x31 SET.
internal static @string Name(this ж<rtype> Ꮡt) {
    System.Type? st = Ꮡt.Value.t.sysType;
    if (!GoReflect.HasGoName(st)) {
        return "";
    }
    string full = GoReflect.GoTypeName(st);
    int dot = full.LastIndexOf('.');
    return (@string)(dot >= 0 ? full[(dot + 1)..] : full);
}

// PkgPath returns a DEFINED type's package import path ("encoding/gob"), empty for a type that is
// not a defined Go type — the managed nesting carries that identity (GoReflect.GoPackagePath). The
// auto form reads the descriptor's TFlagNamed bit and uncommon().PkgPath name-offset, sub-records a
// synthesized abi.Type never populates, so it answered "" for EVERY type: gob's Register then keyed
// its registry on the bare "N2" instead of "encoding/gob.N2" (TestRegistrationNaming).
internal static @string PkgPath(this ж<rtype> Ꮡt) {
    return (@string)GoReflect.GoPackagePath(Ꮡt.Value.t.sysType);
}

// Elem returns the element type of a slice/array/pointer/map/chan. An array descriptor's inner
// dims thread through (the element of a dims-carrying [4][8]byte is [8]byte with dims [8]).
//
// A POINTER descriptor's dims are the POINTEE's and pass through UNSHIFTED — there is nothing else
// they could describe, a pointer having no length of its own. That is the shape a `*[N]T` parameter
// carries (see In and emitGoArrayDimsAttribute): the caller allocates from `In(i).Elem()`, so the
// length has to survive exactly this hop or reflect.New builds a zero-length array for it.
internal static ΔType Elem(this ж<rtype> Ꮡt) {
    System.Type? st = Ꮡt.Value.t.sysType;
    nint[]? dims = Ꮡt.Value.t.arrayDims;
    nint kind = st is null ? -1 : GoReflect.KindOf(st);
    bool throughPointer = kind == GoReflect.Pointer || kind == GoReflect.UnsafePointer;
    // A MAP's carried dims are its ELEMENT's, so they pass unshifted exactly as a pointer's do —
    // the slot means "what Elem() hands down" for every kind but an array, which consumes its head.
    nint[]? elemDims = throughPointer || kind == GoReflect.Map ? dims : dims is { Length: > 1 } ? dims[1..] : null;
    // A pointer hands its POINTEE's channel direction down the same unshifted way — the hop
    // `new(chan<- string)` takes to reach Elem().String(). A channel's own direction describes the
    // channel and stops here. A map's KEY dims describe the key, so they stop at a map and descend
    // only through a pointer, whose cargo is its pointee's whole type.
    GoChanDir elemChanDir = throughPointer ? Ꮡt.Value.t.chanDir : GoChanDir.Unstamped;
    nint[]? elemKeyDims = throughPointer ? Ꮡt.Value.t.keyDims : null;
    return toType(abi.synthType(GoReflect.ElementType(st), elemDims, null, elemChanDir, elemKeyDims));
}

// Key returns a map type's key type — dimensioned from the descriptor's keyDims cargo, the one
// accessor arrayDims cannot feed (see abi.Type.Key).
internal static ΔType Key(this ж<rtype> Ꮡt) {
    return toType(abi.synthType(GoReflect.KeyType(Ꮡt.Value.t.sysType), Ꮡt.Value.t.keyDims));
}

// Len returns an array type's length — the descriptor's carried dims (non-identity cargo; 0
// when no source knew the length, the recorded managed-type limitation).
internal static nint Len(this ж<rtype> Ꮡt) {
    return Ꮡt.Value.t.arrayDims is { Length: > 0 } dims ? dims[0] : 0;
}

// NumField returns the number of fields in a struct type (the projected Go fields — shared
// with the value side, so the two walks index identically).
internal static nint NumField(this ж<rtype> Ꮡt) {
    System.Type? st = Ꮡt.Value.t.sysType;
    return st is null ? 0 : GoReflect.GoFields(st).Length;
}

// NumMethod returns the size of the type's method set: every method for an interface type
// (exported and unexported — Go's interface contract), the EXPORTED methods only for a concrete
// type — a pointer type *X counts X's value- AND pointer-receiver methods, a value type only the
// value-receiver ones. The auto form reads uncommon() method tables that a synthesized descriptor
// never populates, so it answered 0 for EVERY concrete type: encoding/json's indirect() gates its
// Unmarshaler/TextUnmarshaler discovery on NumMethod() > 0, so no custom UnmarshalJSON was ever
// dispatched ("json: cannot unmarshal string into Go value of type time.Time" — time's
// TestTimeJSON / TestUnmarshalInvalidTimes). Answered over the same golib method-set machinery the
// emitted asserts resolve through (GoReflect.GoMethodCount), so this gate and the interface assert
// that follows it cannot disagree about a method set.
internal static nint NumMethod(this ж<rtype> Ꮡt) {
    return GoReflect.GoMethodCount(Ꮡt.Value.t.sysType);
}

// Method returns the i'th method in the type's method set, indexing the SAME table NumMethod
// sizes — Go's order, sorted by name. The auto form reads exportedMethods() off the uncommon()
// sub-record a synthesized descriptor never populates, so it found an EMPTY table and panicked
// "reflect: Method index out of range" for every i — which is what a truthful NumMethod turns
// from unreachable into reachable, and why the count and this walk are one increment (math/rand
// and math/rand/v2's TestRegress enumerate every generator method and call it).
// Method.Type carries the receiver as its first argument and Func is the UNBOUND func value, Go's
// contract for the type side; an interface method has neither a receiver nor a Func (zero Value),
// and its PkgPath qualifies an unexported name.
internal static ΔMethod Method(this ж<rtype> Ꮡt, nint i) {
    System.Type? st = Ꮡt.Value.t.sysType;
    if (i < 0 || i >= GoReflect.GoMethodCount(st)) {
        throw panic("reflect: Method index out of range");
    }
    string name = GoReflect.GoMethodName(st, (int)i);
    var fn = GoReflect.GoMethodFunc(st, (int)i);
    return new ΔMethod(
        Name: (@string)name,
        PkgPath: (@string)(isExportedGoName(name) ? "" : GoReflect.GoPackagePath(st)),
        // The func type carries the method's per-parameter array dims (receiver included, so the
        // indices are In(i)'s): a method type is built from the method TABLE and never passes
        // through a delegate instance, so abi.TypeOf's func-value route cannot supply them here.
        // net/rpc reads exactly this — mtype.In(2) for every service method's reply — and without
        // the cargo a `*[1]int` reply allocated a ZERO-length array through reflect.New.
        Type: toType(abi.synthType(GoReflect.GoMethodFuncType(st, (int)i), null, GoReflect.MethodParamDims(st, (int)i))),
        Func: fn is null ? new ΔValue(nil) : makeReflectValue(fn),
        Index: i
    );
}

// MethodByName returns the method with that name from the same table, over the same name
// projection Method(i) reports — so `t.Method(t.MethodByName(n).Index).Name == n` holds. The auto
// form reads the same absent uncommon() table, but MISSES SILENTLY (not-found is a legal answer),
// which is the quieter half of the same descriptor gap.
internal static (ΔMethod m, bool ok) MethodByName(this ж<rtype> Ꮡt, @string name) {
    nint i = GoReflect.GoMethodIndex(Ꮡt.Value.t.sysType, name.ToString());
    return i < 0 ? (default!, false) : (Method(Ꮡt, i), true);
}

// isExportedGoName reports Go's exported rule — the first RUNE is upper case. Only an interface's
// method table can carry an unexported name (a concrete type's table is exported-only).
private static bool isExportedGoName(string name) {
    return System.Text.Rune.DecodeFromUtf16(name, out System.Text.Rune first, out _) == System.Buffers.OperationStatus.Done &&
           System.Text.Rune.IsUpper(first);
}

// Field returns the i'th struct field's descriptor: the projected Go name (blank fields are
// "_"; a promoted embed carries the embedded type's name), the field's STATIC Go type
// (dims-stamped when the declaring zero instance reveals an array field's length), the declared
// struct TAG, and the single-hop Index sequence — Value.FieldByIndex(f.Index) must reach the
// field (an EMPTY index makes the auto FieldByIndex return the struct itself, which is how gob's
// encodeStruct walked every wireType field as the whole struct and encIndirect died in
// Elem-on-struct).
//
// The Tag is a real READ, not a reconstruction, so it satisfies the descriptor rule: the
// converter emits every tagged field's tag as `[GoTag]` at the declaration and golib's field
// projection carries it through verbatim. It had never been surfaced, so StructField.Tag came
// back empty for EVERY converted struct and every tag-driven decoder saw an untagged type —
// encoding/asn1 marshalled crypto/x509's `optional` NamedCurveOID instead of omitting its nil
// value, which is the "asn1: structure error: invalid object identifier" behind crypto/ecdsa's
// TestEqual.
//
// PkgPath is a real read too, and the same silent-degradation class as the Tag: Go sets it to
// the declaring package's import path for an UNEXPORTED field and leaves it empty for an
// exported one, so `StructField.IsExported()` — which is nothing but `PkgPath == ""` — answered
// TRUE for every field of every converted struct. Silently, because "" is the correct answer for
// most fields. The consequence is a guard that can never fire: encoding/asn1 opens both its
// struct arms with `if !t.Field(i).IsExported() { return StructuralError{"struct contains
// unexported fields"} }`, so `Marshal(unexported{X:5,y:1})` returned a nil error where Go returns
// that error, and `Unmarshal` ran on to write through the unexported field and panicked in
// mustBeAssignable instead (asn1's TestUnexportedStructField). Note the two halves of the
// read-only model degraded INDEPENDENTLY: the VALUE side was already right (Value.Field stamps
// flagStickyRO from the same GoReflect.GoFields projection, which is why the write panicked
// rather than silently succeeding) — it was the TYPE-side descriptor that had no answer. Both
// now read exportedness from that one projection, so a probe of the type and a write through the
// value can never disagree about a field.
//
// Offset is a real READ, over the SAME memoized Go layout walk abi.StructType already publishes
// (GoReflect.GoFieldOffsets — Go amd64 rules, so a Go zero-size field such as sync/atomic's
// `noCopy` occupies nothing and an align64-bearing field lands on its 8-byte boundary), so a
// probe of the reflect descriptor and a probe of the abi one can never disagree about one struct.
//
// It had stayed unpopulated on the r39d rule — "a descriptor field whose read cannot be honored
// must not be populated to look truthful" — reasoning that a Go byte offset exists only to be
// added to a data pointer, and managed storage has no such pointer. That reasoning applies to
// Offset as an ADDRESS and not to Offset as layout METADATA, which is the only way abi's
// consumers (unique's clone sequencer, reflectlite) have ever read it, and the only way the
// measured consumer here reads it: sync/atomic's TestAutoAligned64 asserts
// `TypeOf(&struct{_ uint32; i Int64}{}).Elem().Field(1).Offset == 8` and got 0 — which is a real
// answer for a field at the front of a struct, so it read as a LAYOUT failure rather than as an
// unpopulated descriptor. The r39d rule is still honored where it bites: a struct holding a field
// whose Go size is unknowable makes every later offset a guess, and GoFieldOffsets answers null
// for the whole struct there, so every field keeps the zero rather than a plausible-looking
// number.
//
// Anonymous IS populated, and its measured consumer is the whole Go EMBEDDING contract:
// encoding/json's typeFields (and encoding/xml's, encoding/gob's, text/template's) flattens a
// field's own fields into the enclosing object exactly when StructField.Anonymous is set and the
// field carries no name tag. Reported false, every embed became an ORDINARY field named after its
// type — `{"S1":{"X":2},"S2":{"X":4}}` where Go writes `{}`, `{"S":"B","BugA":{"S":"A"}}` where Go
// writes `{"S":"B"}` — and `DisallowUnknownFields` then named the promoted field's own key as the
// unknown one. It is a real READ, not a reconstruction: the converter emits an embed as a partial
// property over a marker-prefixed backing box and golib's field projection records that shape as
// GoFieldInfo.Embedded, which is the same flag reflect's struct-identity walk already compares
// (Go's haveIdenticalUnderlyingType ends each field with `tf.Embedded() != vf.Embedded()`).
//
// The recorded next gap of this shape is the field ORDER an embedded field lands in: go2cs-gen
// emits the promoted-embed backing box AFTER the declared fields, so `Host{X; y; Inner; inner; Ptr}`
// walks as X, y, Ptr, Inner, inner here where Go walks it in declaration order. It is deliberately
// NOT fixed with Anonymous, because no measured consumer observes it yet (the r39d rule): json's
// dominance rules are decided by DEPTH and tag, not by declaration order, and its one order-sensitive
// test — TestMarshalEmbeds — declares its single plain field FIRST, so the projected order and Go's
// coincide. A struct that interleaves plain and embedded fields AND is marshalled by key order is
// the shape that will expose it, and the remedy is declaration-order cargo, not a re-sort here.
internal static StructField Field(this ж<rtype> Ꮡt, nint i) {
    System.Type st = Ꮡt.Value.t.sysType!;
    return structFieldOf(st, GoReflect.GoFields(st)[(int)i], [i]);
}

// FieldByIndex walks an index PATH, one hop per element, and is Go's own loop verbatim — the only
// thing hand-owned is where the walk STARTS.
//
// Go seeds it by reinterpreting the rtype as a *structType and taking `&t.Type`. That reinterpret has
// no managed form: structType is LARGER than rtype (it carries PkgPath and Fields past the embedded
// Type), and ж.Reinterpret aliases storage only when the destination FITS in the source, so the pair
// falls to the raw-address route and the derived box's embedded descriptor carries no managed cargo.
// Its sysType is null, so the seeding statement alone tripped canonType's synthType-was-bypassed
// assertion — a Debug.Assert, which KILLS THE PROCESS (0x80131623) rather than failing a test.
// encoding/xml reaches it from getTypeInfo → addFieldInfo whenever it unmarshals a struct with a
// promoted field, and 15 of its verdicts came back EMPTY rather than failed, which reads like a
// missing suite instead of one dead call.
//
// `common()` IS the descriptor Go's `&t.Type` names after that reinterpret — the rtype's own,
// synthType-stamped abi.Type — so seeding from it is the same Go value by a route the managed model
// can express, and no structType is ever synthesized. Every subsequent hop goes through the
// hand-owned rtype.Field above, so the promoted-field projection stays the one GoReflect provides.
internal static StructField FieldByIndex(this ж<rtype> Ꮡt, slice<nint> index) {
    if (Ꮡt.Kind() != Struct) {
        throw panic("reflect: FieldByIndex of non-struct type " + Ꮡt.String());
    }
    StructField f = default!;
    f.Type = toType(Ꮡt.common());
    foreach (var (i, x) in index) {
        if (i > 0) {
            var ft = f.Type;
            if (ft.Kind() == ΔPointer && ft.Elem().Kind() == Struct) {
                ft = ft.Elem();
            }
            f.Type = ft;
        }
        f = f.Type.Field(x);
    }
    return f;
}

// The descriptor for one projected field of `st`, reached by `index`. Split out of Field so a
// PROMOTED field (whose index is a PATH through one or more embeds) is described by the same rule
// as a direct one — everything but Index is the deepest field's own property.
private static StructField structFieldOf(System.Type st, GoReflect.GoFieldInfo f, nint[] index) {
    // The field's position WITHIN st is index's LAST hop, whether this is a direct field or the
    // deepest field of a promotion path — Go's promoted StructField.Offset is likewise relative to
    // the field's own declaring struct, never to the outer one.
    nint[]? offsets = GoReflect.GoFieldOffsets(st);
    nint at = index.Length == 0 ? -1 : index[^1];
    return new StructField(
        Name: (@string)f.Name,
        // ⚠ GoReflect.GoPackagePath DIRECTLY, never "tidied" to route through rtype.PkgPath():
        // StructField.PkgPath is NOT derivable from the type's own PkgPath. Verified against Go —
        // for an UNNAMED struct both Type.Name() and Type.PkgPath() are "", yet its unexported
        // field's StructField.PkgPath is still the declaring package (e.g. "main"). Routing
        // through the defined-type gate would silently blank exactly the case this exists for.
        PkgPath: f.Exported ? "" : (@string)GoReflect.GoPackagePath(st),
        Type: toType(structFieldDescriptor(f)),
        Tag: ((StructTag)(@string)f.Tag),
        Offset: offsets is not null && (nuint)at < (nuint)offsets.Length ? (uintptr)(nuint)offsets[at] : 0,
        Index: new slice<nint>(index),
        Anonymous: f.Embedded
    );
}

// ==== the type-relation mirrors: Implements / AssignableTo / PointerTo / Convert ====
// The auto forms walk descriptor sub-records that only exist in Go's runtime layout —
// implements() reinterprets the abi.Type as an interfaceType specialization
// (Reinterpret<abi.Type, interfaceType>) and reads .Methods off a promoted-embed box that is
// DEFAULT behind a synthesized descriptor (the first read throws from ж.ValueSlot); ptrTo
// builds a ptrType prototype through an eface Reinterpret; convertOp's cvt* family allocates
// through the nil unsafe_New stub. Bridged over the SAME golib machinery emitted asserts and
// the Set/Set* family use (GoReflect.GoImplements / TryConvertTo), so reflection and direct
// asserts can never disagree about a method set or a conversion. Demonstrated consumers:
// encoding/gob's init (validUserType → implementsInterface → Implements/PointerTo) and
// internal/fmtsort's package-level ct() table (Convert). Mirrors the reflectlite increment-1
// surface (internal/reflectlite/type_impl.cs).

// Implements reports whether the type implements the interface type u (Go method-set rules:
// nominal or structural via golib StructurallyImplements).
internal static bool Implements(this ж<rtype> Ꮡt, ΔType u) {
    if (u == default!) {
        throw panic("reflect: nil type passed to Type.Implements");
    }
    if (u.Kind() != ΔInterface) {
        throw panic("reflect: non-interface type passed to Type.Implements");
    }
    return GoReflect.GoImplements(sysTypeOfReflectType(u), Ꮡt.Value.t.sysType);
}

// AssignableTo is NO LONGER HAND-OWNED. It read `identity on the carried System.Type, or
// interface-implements`, which is a strictly narrower relation than Go's: Go also admits a value
// whose type has the same UNDERLYING type as the destination when at least one of the two is not
// a defined type. database/sql's TestUserDefinedBytes is the measured consumer — convertAssignRows
// assigns a driver's []byte into a `type userDefinedBytes []byte`, which Go accepts and CLONES,
// while the identity rule rejected it and fell through to the CONVERT arm, handing the caller a
// view over the driver's own array ("got potentially dirty driver memory").
//
// Go's own body is now what runs: `directlyAssignable(uu.t, t.t) || implements(uu.t, t.t)`. It
// could not run before because three of the things it stands on were not answerable — the
// descriptor's TFlagNamed bit (now carried, internal/abi/type_impl.cs), the `implements` free
// function and haveIdenticalUnderlyingType's downcast arms (both below). Retiring a hand-own is
// the point of fixing those: the less of Go's algorithm this bridge restates, the fewer places
// its semantics can drift.

// implements reports whether the type V implements the interface type T — the FREE function Go's
// own directlyAssignable/AssignableTo/convertOp/assignTo all route through, as distinct from the
// rtype.Implements method below (which is the public API boundary and panics for a non-interface
// argument; this one answers false, exactly as Go's does).
//
// The auto form reinterprets the abi.Type as an interfaceType specialization and reads .Methods
// off a promoted-embed box that is DEFAULT behind a synthesized descriptor, so the first read of a
// NON-EMPTY interface throws from ж.ValueSlot. Bridged over GoReflect.GoImplements — the same
// method-set probe the emitted `_<T>` asserts and rtype.Implements use — so a method set can never
// be answered one way by a type assertion and another by reflection, and so the three call sites
// that reach this function cannot disagree with the one that reaches the method.
internal static bool implements(ж<abi.Type> ᏑT, ж<abi.Type> ᏑV) {
    if (ᏑT == nil || abi.Kind(ref ᏑT.Value) != abi.Interface) {
        return false;
    }
    return GoReflect.GoImplements(ᏑT.Value.sysType, ᏑV == nil ? null : ᏑV.Value.sysType);
}

// ChanDir returns a channel type's direction — the direction the descriptor CARRIES as cargo, and
// BothDir when nothing stamped one. See internal/abi's ChanDir for the sources and the boundaries.
// The auto form downcast the descriptor onto the chanType record Go's linker allocates behind it
// and read a direction out of the memory that follows the value slot instead —
// non-deterministically, so reflect.MakeChan's `ChanDir() != BothDir` guard and the identity
// walk's chan arm each answered differently run to run.
internal static ΔChanDir ChanDir(this ж<rtype> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();
    if (t.Kind() != Chan) {
        throw panic("reflect: ChanDir of non-chan type " + Ꮡt.String());
    }
    return ((ΔChanDir)(nint)abi.ChanDir(Ꮡt.common()));
}

// ==== the channel OPERATIONS: recv / send ====
//
// Both auto forms opened with the same `(*chanType)(unsafe.Pointer(t))` downcast ChanDir above
// retired, one layer down: behind a synthesized descriptor the reinterpreted `.Dir` reads zero, and
// `0 & RecvDir == 0` holds for EVERY channel — so a plain bidirectional `chan string` was refused
// as send-only. Past that test neither could have worked either, because both then hand a uintptr
// channel address and an unsafe.Pointer element slot to `chanrecv`/`chansend0`, external stubs the
// PartialStubGenerator fills with NotImplementedException. Bridging them removes the last live
// caller of both stubs.
//
// The direction guard is asked of the DESCRIPTOR's cargo, which is the one authority, and it is why
// these two could not land before the direction did: a working recv behind a direction that always
// reads bidirectional converts text/template's `range` over a send-only channel from a fast,
// attributable error into an unbounded hang (measured — 51 verdicts lost to a package deadline
// against the 1 the bridge buys). Guard first, receive second, in that order and for that reason.

// recv receives from a channel Value, blocking unless nb. It reports Go's (value, ok) pair, where
// a not-selected non-blocking receive answers the INVALID zero Value, exactly as the auto form did.
internal static (ΔValue val, bool ok) recv(this ΔValue v, bool nb) {
    if (((ΔChanDir)(((ΔChanDir)(nint)abi.ChanDir(v.typ_)) & RecvDir)) == 0) {
        throw panic("reflect: recv on send-only channel");
    }
    System.Type? elem = GoReflect.ElementType(v.typ_ == nil ? null : v.typ_.Value.sysType);
    if (v.live is not IChannel ch || elem is null) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Recv", v.kind())));
    }
    if (!ch.ChanRecv(out object received, out bool ok, !nb)) {
        return (new ΔValue(nil), false);
    }
    // A closed-and-drained receive yields the ELEMENT's zero, which the channel reports as its own
    // default — null for a reference element, where Go's zero is a typed nil. Fabricating it here
    // through the one zero-builder the bridge uses everywhere keeps `x.IsZero()`/`x.Interface()` on
    // a closed channel's value agreeing with reflect.Zero of the same type.
    object? boxed = ok ? received : GoReflect.ZeroValueOf(elem, null);
    return (makeTypedValue(boxed, elem, null, (flag)0), ok);
}

// send sends x on a channel Value, blocking unless nb. x is assigned to the channel's element type
// first (Go's assignability rule, same context string Go uses), so a named element or an interface
// element boxes exactly as an ordinary send would.
internal static bool /*selected*/ send(this ΔValue v, ΔValue xʗp, bool nb) {
    if (((ΔChanDir)(((ΔChanDir)(nint)abi.ChanDir(v.typ_)) & SendDir)) == 0) {
        throw panic("reflect: send on recv-only channel");
    }
    ΔValue x = xʗp;
    x.mustBeExported();
    System.Type? elem = GoReflect.ElementType(v.typ_ == nil ? null : v.typ_.Value.sysType);
    if (v.live is not IChannel ch || elem is null) {
        throw panic(Ꮡ(new ValueError("reflect.Value.Send", v.kind())));
    }
    // Go's assignTo, with its message and without its Value: the sent element is the MANAGED value
    // the channel's element slot holds, marshalled by the one rule Value.Call's arguments use.
    if (!marshalIntoSlot(x, elem, out object? sent)) {
        throw panic("reflect.Value.Send: value of type " + GoReflect.GoTypeName(x.live?.GetType()) +
                    " is not assignable to type " + GoReflect.GoTypeName(elem));
    }
    return ch.ChanSend(sent!, !nb);
}

// ==== type IDENTITY: haveIdenticalUnderlyingType, arm for arm over answerable accessors ====
//
// THE seat of Go's type-identity relation: `ConvertibleTo` reaches it through convertOp,
// `AssignableTo` through directlyAssignable, and Value.assignTo/Convert through both. Go's body is
// a switch on kind, and five of its eight arms already worked here — the scalar arm needs nothing,
// and Array/Map/Pointer/Slice recurse through Elem()/Key()/Len(), which internal/abi synthesizes
// from the descriptor's carried System.Type.
//
// The other three reached their operands by the PREFIX-DOWNCAST idiom the rest of this bridge has
// already had to replace — `(*structType)(unsafe.Pointer(t))` and its funcType/interfaceType
// siblings — and there is nothing behind a ж<abi.Type> to downcast to. They did not fail loudly.
// They read ZERO of everything and returned TRUE:
//
//   * STRUCT — `len(t.Fields)` came back 0 for both operands, so the field loop never ran and any
//     two structs compared identical. Measured: `struct{B []byte; M map[string]int}` was reported
//     convertible to the same struct with `M map[string]int64`, AND to one whose second field is
//     merely RENAMED, AND to one with a different field COUNT.
//   * FUNC — the same, through InCount/OutCount: any two func types compared identical.
//   * INTERFACE — Go's own arm answers true only when BOTH sides have zero methods; reading zero
//     methods off both made every interface pair identical.
//
// A false positive in an identity relation is the most dangerous shape this board tracks, because
// every caller reads it as permission. It was already live through ConvertibleTo, and retiring the
// AssignableTo hand-own would have widened it to assignment — which is why the sequence recorded
// on the board fixes these arms in the SAME change, not after it.
//
// The struct arm is bridged at the REFLECT level rather than in internal/abi on purpose. abi's
// synthesized StructType() deliberately leaves StructField.Name the zero ΔName — a ΔName is a
// pointer into the linker's name blob and every reader walks it with raw-address arithmetic — so
// the field NAMES and TAGS Go's identity walk compares are not there to be had one layer down.
// reflect already owns the named-field projection (rtype.Field, over GoReflect.GoFields), and this
// walk reads the SAME projection, so the fields a type hands out and the fields its identity is
// decided by cannot disagree.
internal static bool haveIdenticalUnderlyingType(ж<abi.Type> ᏑT, ж<abi.Type> ᏑV, bool cmpTags) {
    if (ᏑT == ᏑV) {
        return true;
    }
    if (ᏑT == nil || ᏑV == nil) {
        return false;
    }
    ref var T = ref ᏑT.DerefOrNull();
    ref var V = ref ᏑV.DerefOrNull();
    // The internal/abi accessors are called in QUALIFIED STATIC form throughout this walk
    // (`abi.Elem(x)`, not `x.Elem()`). Extension-method lookup searches the file's enclosing
    // namespaces, and `go.@internal` is a CHILD of this file's `go`, not a parent — so an
    // unqualified call binds to reflect_package's own same-named extension over ж<rtype> and
    // fails to compile. internal/abi's own type_impl.cs can use the instance form because it
    // lives in that namespace; this file cannot.
    ΔKind kind = ((ΔKind)(nuint)(uint8)abi.Kind(ref T));
    if (kind != ((ΔKind)(nuint)(uint8)abi.Kind(ref V))) {
        return false;
    }
    // Non-composite types of equal kind have the same underlying type (the predefined instance).
    if (ΔBool <= kind && kind <= Complex128 || kind == ΔString || kind == ΔUnsafePointer) {
        return true;
    }
    // Composite types — Go's switch, in Go's order.
    var exprᴛ1 = kind;
    if (exprᴛ1 == Array) {
        return abi.Len(ᏑT) == abi.Len(ᏑV) && haveIdenticalType(abi.Elem(ᏑT), abi.Elem(ᏑV), cmpTags);
    }
    if (exprᴛ1 == Chan) {
        return abi.ChanDir(ᏑT) == abi.ChanDir(ᏑV) && haveIdenticalType(abi.Elem(ᏑT), abi.Elem(ᏑV), cmpTags);
    }
    if (exprᴛ1 == Func) {
        return haveIdenticalFuncShape(ᏑT, ᏑV, cmpTags);
    }
    if (exprᴛ1 == ΔInterface) {
        return isEmptyGoInterface(T.sysType) && isEmptyGoInterface(V.sysType);
    }
    if (exprᴛ1 == Map) {
        return haveIdenticalType(abi.Key(ᏑT), abi.Key(ᏑV), cmpTags) && haveIdenticalType(abi.Elem(ᏑT), abi.Elem(ᏑV), cmpTags);
    }
    if (exprᴛ1 == ΔPointer || exprᴛ1 == ΔSlice) {
        return haveIdenticalType(abi.Elem(ᏑT), abi.Elem(ᏑV), cmpTags);
    }
    if (exprᴛ1 == Struct) {
        return haveIdenticalStructShape(ᏑT, ᏑV, cmpTags);
    }
    return false;
}

// isEmptyGoInterface answers Go's `len(interfaceType.Methods) == 0` for a managed interface type.
// Go's `any`/`interface{}` is emitted as `object`, which is the only interface type this bridge
// can prove methodless: a DEFINED empty interface with a managed type of its own is answered
// false, i.e. NOT identical. That is the conservative direction on purpose — a false negative in
// an identity relation degrades a caller to "this needs a conversion", while a false positive
// hands it a silent wrong assignment. (No measured consumer compares two distinct empty interface
// types; the assignability of a concrete value TO `any` does not come through here at all, it
// comes through implements().)
private static bool isEmptyGoInterface(System.Type? st) {
    return st == typeof(object);
}

// haveIdenticalFuncShape compares two func types by the parameter and result types the delegate's
// Invoke signature carries (GoReflect.TryFuncShape — the SAME shape rtype.NumIn/In/NumOut/Out
// read), plus variadicity, which Go carries in the top bit of the descriptor's OutCount and
// therefore compares as part of the same count check. A parameter's ARRAY DIMS ride the
// descriptor's funcParamDims cargo, so `func([32]byte) bool` and `func([64]byte) bool` — ONE
// managed delegate type — stay distinguishable exactly where a source knew the lengths.
private static bool haveIdenticalFuncShape(ж<abi.Type> ᏑT, ж<abi.Type> ᏑV, bool cmpTags) {
    System.Type? ts = ᏑT.Value.sysType;
    System.Type? vs = ᏑV.Value.sysType;
    if (ts is null || vs is null ||
        !GoReflect.TryFuncShape(ts, out System.Type[]? tin, out System.Type[]? tout, out bool tVariadic) ||
        !GoReflect.TryFuncShape(vs, out System.Type[]? vin, out System.Type[]? vout, out bool vVariadic)) {
        return false;
    }
    if (tin!.Length != vin!.Length || tout!.Length != vout!.Length || tVariadic != vVariadic) {
        return false;
    }
    nint[]?[]? tParamDims = ᏑT.Value.funcParamDims;
    nint[]?[]? vParamDims = ᏑV.Value.funcParamDims;
    for (int i = 0; i < tin.Length; i++) {
        var tp = abi.synthType(tin[i], funcParamDimsAt(tParamDims, i));
        var vp = abi.synthType(vin[i], funcParamDimsAt(vParamDims, i));
        if (!haveIdenticalType(tp, vp, cmpTags)) {
            return false;
        }
    }
    for (int i = 0; i < tout.Length; i++) {
        if (!haveIdenticalType(abi.synthType(tout[i]), abi.synthType(vout[i]), cmpTags)) {
            return false;
        }
    }
    return true;
}

private static nint[]? funcParamDimsAt(nint[]?[]? paramDims, int i) {
    return paramDims is not null && i < paramDims.Length ? paramDims[i] : null;
}

// haveIdenticalStructShape is Go's field loop over GoReflect.GoFields — the projection rtype.Field
// and the value side already read, so a struct's identity and the fields it hands out are decided
// by one walk. Every clause Go compares is compared here: field COUNT, the struct's PkgPath, and
// per field the NAME, the TYPE, the TAG (only when cmpTags — the single place assignability and
// convertibility diverge, which is why the projection has to carry tags at all), the OFFSET and
// EMBEDDEDNESS (`struct{T}` is not `struct{T T}`, and nothing else separates them: an embed's Go
// field name IS its type name).
//
// The offsets are compared only when BOTH sides can compute a layout. A struct holding a field of
// unknowable Go size has no truthful offset table at all — the same condition under which abi's
// StructType() answers Go's nil — and in that state the comparison is not weakened in any way that
// matters: identical field names, types and order determine identical offsets by construction, so
// Go compares them defensively rather than decisively.
private static bool haveIdenticalStructShape(ж<abi.Type> ᏑT, ж<abi.Type> ᏑV, bool cmpTags) {
    System.Type? ts = ᏑT.Value.sysType;
    System.Type? vs = ᏑV.Value.sysType;
    if (ts is null || vs is null) {
        return false;
    }
    GoReflect.GoFieldInfo[] tFields = GoReflect.GoFields(ts);
    GoReflect.GoFieldInfo[] vFields = GoReflect.GoFields(vs);
    if (tFields.Length != vFields.Length) {
        return false;
    }
    if (structTypePkgPath(ts, tFields) != structTypePkgPath(vs, vFields)) {
        return false;
    }
    nint[]? tOffsets = GoReflect.GoFieldOffsets(ts);
    nint[]? vOffsets = GoReflect.GoFieldOffsets(vs);
    bool compareOffsets = tOffsets is not null && vOffsets is not null;
    for (int i = 0; i < tFields.Length; i++) {
        GoReflect.GoFieldInfo tf = tFields[i];
        GoReflect.GoFieldInfo vf = vFields[i];
        if (tf.Name != vf.Name || tf.Embedded != vf.Embedded) {
            return false;
        }
        if (cmpTags && tf.Tag != vf.Tag) {
            return false;
        }
        if (!haveIdenticalType(structFieldDescriptor(tf), structFieldDescriptor(vf), cmpTags)) {
            return false;
        }
        if (compareOffsets && tOffsets![i] != vOffsets![i]) {
            return false;
        }
    }
    return true;
}

// structTypePkgPath is Go's abi.StructType.PkgPath: the declaring package when the struct holds an
// unexported field, "" otherwise. It is what makes two structurally identical structs from
// DIFFERENT packages non-identical when either hides a field.
private static @string structTypePkgPath(System.Type st, GoReflect.GoFieldInfo[] fields) {
    foreach (GoReflect.GoFieldInfo f in fields) {
        if (!f.Exported) {
            return (@string)GoReflect.GoPackagePath(st);
        }
    }
    return "";
}

// structFieldDescriptor mints a field's descriptor exactly as abi's synthesizeStructType does, so
// the identity walk and the abi.StructType a caller can read are built from one rule.
private static ж<abi.Type> structFieldDescriptor(GoReflect.GoFieldInfo f) {
    nint kind = GoReflect.KindOf(f.Type);
    // An ARRAY field's dims come off the initializer the converter emitted, read from the declaring
    // struct's zero instance; a POINTER's and a MAP's come off the [GoArrayDims] stamp, because a
    // nil pointee and an absent map entry reveal nothing — and both hops are ordinary at a DECODE
    // target, which is a struct nothing has populated yet.
    nint[]? dims = kind == GoReflect.Array || kind == GoReflect.Pointer || kind == GoReflect.Map ? f.ArrayDims : null;
    nint[]? keyDims = kind == GoReflect.Map || kind == GoReflect.Pointer ? f.KeyDims : null;
    // A channel field carries its DIRECTION the same way an array field carries its length: off
    // the initializer the converter emitted, read from the declaring struct's zero instance.
    GoChanDir fieldDir = kind == GoReflect.Chan ? f.ChanDir : GoChanDir.Unstamped;
    return abi.synthType(f.Type, dims, null, fieldDir, keyDims);
}

// PointerTo returns the pointer type with element t — the managed ж<T> pointer form,
// canonical via toType (gob's implementsInterface probes reflect.PointerTo(typ) for every
// non-pointer user type).
public static ΔType PointerTo(ΔType t) {
    System.Type? st = sysTypeOfReflectType(t);
    if (st is null) {
        throw panic("reflect: PointerTo of non-synthesized type");
    }
    return toType(abi.synthType(typeof(ж<>).MakeGenericType(st)));
}

// PtrTo is the deprecated spelling of PointerTo. (The auto form already delegates; kept auto.)

// Convert returns the value v converted to type t under Go's conversion rules, routed through
// GoReflect.TryConvertTo — THE convertibility relation (assignability with adapter/box unwrap,
// named-wrapper construction/unwrap, kinded scalar conversions with Go truncation semantics).
// A conversion the relation cannot express panics with Go's message (fail loud, never a
// silent wrong value). The result carries the DESTINATION type and inherits v's read-only
// bits (Go flag stickiness).
public static ΔValue Convert(this ΔValue v, ΔType t) {
    System.Type? dstType = sysTypeOfReflectType(t);
    if (dstType is null) {
        throw panic("reflect.Value.Convert: convert to non-synthesized type");
    }
    object? src = v.live;
    if (!GoReflect.TryConvertTo(src, dstType, out object? converted)) {
        throw panic("reflect.Value.Convert: value of type " + GoReflect.GoTypeName(src is null ? null : GoReflect.GoDynamicTypeOf(src)) +
                    " cannot be converted to type " + t.String());
    }
    return makeTypedValue(converted, dstType, arrayDimsOfReflectType(t), (flag)(v.flag & flagRO));
}

// FieldByName returns the struct field with the given name over the SAME projected Go field
// table NumField/Field/the value side use (the auto form reinterprets the descriptor as a
// structType — the promoted-embed box is default behind a synthesized descriptor). Top-level
// names only: Go's embedded-field depth search (FieldByNameFunc BFS) is deferred with a named
// consumer — a promoted name answers (zero, false), exactly like an absent field, so a caller
// degrades to Go's not-found path rather than crashing. gob's compileDec (matching wire-type
// field names to the local struct) is the demonstrated consumer.
internal static (StructField, bool) FieldByName(this ж<rtype> Ꮡt, @string name) {
    System.Type? st = Ꮡt.Value.t.sysType;
    if (st is null || GoReflect.KindOf(st) != GoReflect.Struct) {
        throw panic("reflect: FieldByName of non-struct type");
    }
    GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(st);
    bool hasEmbeds = false;
    for (nint i = 0; i < fields.Length; i++) {
        if ((@string)fields[i].Name == name) {
            return (Field(Ꮡt, i), true);
        }
        hasEmbeds |= fields[(int)i].Embedded;
    }
    // Go's own shape: the direct scan above is the quick path AND the whole answer for a struct
    // with no embedded fields; only an embed makes a deeper search possible at all.
    if (!hasEmbeds) {
        return (default!, false);
    }
    return promotedFieldByName(Ꮡt, st, name);
}

// Go's PROMOTED-field search — structType.FieldByNameFunc, breadth first over embedded fields.
//
// Until StructField.Anonymous became truthful this could not be written: an embed is what defines a
// promotion, and nothing distinguished one. Without it FieldByName answered only DIRECT fields and
// reported a promoted name as ABSENT — silently, and then destructively, because Value.FieldByName
// hands the zero index sequence to FieldByIndex, which answers the STRUCT ITSELF, so a write through
// the "field" landed on the whole value.
//
// Two properties of Go's search are load-bearing and are reproduced exactly rather than
// approximated:
//
//   * BREADTH FIRST, with a SHALLOWER name always winning — that is Go's field-dominance rule, the
//     same one encoding/json states as "the least deeply nested field wins";
//   * an AMBIGUITY at one depth is NOT a match. Two embeds carrying the same name at the same depth
//     annihilate each other and the name is simply absent (Go's `ok == false`), which is why the
//     count at each level is what decides rather than the first hit found.
//
// An embedded POINTER is followed through its pointee, as Go does; a visited set keeps a cyclic
// embed graph finite (`type Loop struct { Loop1 int; *Loop }` — encoding/json's own fixture).
private static (StructField, bool) promotedFieldByName(ж<rtype> Ꮡt, System.Type st, @string name) {
    var current = new System.Collections.Generic.List<(System.Type owner, nint[] index)> { (st, []) };
    var visited = new System.Collections.Generic.HashSet<System.Type> { st };

    while (current.Count > 0) {
        var next = new System.Collections.Generic.List<(System.Type owner, nint[] index)>();
        nint[]? found = null;
        System.Type? foundOwner = null;
        int matches = 0;

        foreach ((System.Type owner, nint[] index) in current) {
            GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(owner);

            for (int i = 0; i < fields.Length; i++) {
                GoReflect.GoFieldInfo f = fields[i];
                nint[] path = [.. index, (nint)i];

                if ((@string)f.Name == name) {
                    matches++;
                    found = path;
                    foundOwner = owner;
                    continue;
                }
                if (!f.Embedded) {
                    continue;
                }
                // An embedded pointer promotes its POINTEE's fields.
                System.Type embedded = GoReflect.KindOf(f.Type) == GoReflect.Pointer
                    ? GoReflect.ElementType(f.Type)!
                    : f.Type;
                if (embedded is not null && GoReflect.KindOf(embedded) == GoReflect.Struct && visited.Add(embedded)) {
                    next.Add((embedded, path));
                }
            }
        }
        // Exactly one match at this depth wins; two or more annihilate (Go reports absent).
        if (matches == 1) {
            return (structFieldOf(foundOwner!, GoReflect.GoFields(foundOwner!)[(int)found![^1]], found), true);
        }
        if (matches > 1) {
            return (default!, false);
        }
        current = next;
    }
    return (default!, false);
}

// ==== func-type introspection over the delegate Invoke signature (GoReflect.TryFuncShape) ====
// A converted Go func value is a C# delegate; NumIn/In/NumOut/Out derive from its Invoke
// signature (multi-return = ValueTuple, unambiguous), never from funcType sub-descriptors the
// bridge never populates. In/Out are canonical (toType-interned).

private static (System.Type[] ins, System.Type[] outs, bool isVariadic) funcShapeOf(ж<rtype> Ꮡt, @string op) {
    System.Type? st = Ꮡt.Value.t.sysType;
    if (st is null || !GoReflect.TryFuncShape(st, out System.Type[]? ins, out System.Type[]? outs, out bool isVariadic)) {
        throw panic("reflect: " + op + " of non-func type");
    }
    return (ins, outs, isVariadic);
}

internal static nint NumIn(this ж<rtype> Ꮡt) {
    return funcShapeOf(Ꮡt, "NumIn"u8).ins.Length;
}

// In returns the i'th input parameter type. Its ARRAY DIMENSION rides the descriptor's
// funcParamDims cargo: a `[32]byte` parameter emits as a bare `array<byte>` and the delegate type
// is a `Func<array<byte>, bool>` shared with every other `func([N]byte) bool`, so the length has no
// managed type to live in and no value or field initializer to be recovered from — the converter
// stamps it on the parameter as [GoArrayDims] and abi.TypeOf reads it off the delegate instance.
// Without it In(0) answered a dims-less array: Len() 0, String() "[]uint8", and reflect.New of it a
// ZERO-length array — which is why testing/quick generated the empty value for every property test
// over a fixed-size array (edwards25519's TestScalarSetCanonicalBytes indexed `in[len(in)-1]` and
// panicked with index -1). A parameter the cargo does not cover keeps the dims-less descriptor,
// which is the state every other type-only path already produces.
internal static ΔType In(this ж<rtype> Ꮡt, nint i) {
    nint[]?[]? paramDims = Ꮡt.Value.t.funcParamDims;
    nint[]? dims = paramDims is not null && i >= 0 && (int)i < paramDims.Length ? paramDims[(int)i] : null;
    return toType(abi.synthType(funcShapeOf(Ꮡt, "In"u8).ins[(int)i], dims));
}

internal static nint NumOut(this ж<rtype> Ꮡt) {
    return funcShapeOf(Ꮡt, "NumOut"u8).outs.Length;
}

internal static ΔType Out(this ж<rtype> Ꮡt, nint i) {
    return toType(abi.synthType(funcShapeOf(Ꮡt, "Out"u8).outs[(int)i]));
}

internal static bool IsVariadic(this ж<rtype> Ꮡt) {
    return funcShapeOf(Ꮡt, "IsVariadic"u8).isVariadic;
}

} // end reflect_package
