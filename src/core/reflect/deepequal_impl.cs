// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// Deep equality test via reflection
using go;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

// Hand-finished conversion (the reflection bridge — Phase 4, DeepEqual). Go's deepValueEqual keys its
// cycle-detection `visited` map on the values' internal data words (v.ptr / v.pointer()) — raw eface
// addresses that the managed bridge never populates, so the auto form NREs converting the null
// unsafe.Pointer slot (first operational hits: strings/bytes TestSplit/TestSplitAfter). The managed
// form recurses over the SAME boxed values the bridge Value carries (value_impl.cs) and keys cycle
// detection on managed reference identity instead: a pointer is its ж<T> box, a map is its backing
// Dictionary, and a slice is its backing array + window offset (Go keys on &s[0] — base plus offset).
// DeepEqual itself stays auto (it only uses the bridged ValueOf/Type/AreEqual); the converter skips
// only deepValueEqual via the manualConversionFuncs registry (go2cs/manualTypeOperations.go); this
// module marker also makes go2cs skip re-converting this file.
// See docs/phase4/DESIGN-reflection-bridge.md.

[module: GoManualConversion]

namespace go;

partial class reflect_package {

// Tests for deep equality using reflected types. Mirrors Go's deepValueEqual over the bridge's boxed
// managed values. The map argument is Go's address-keyed visited map — unusable in the managed model
// (no data words); the managed recursion carries a reference-identity set instead, with the same
// semantics: all checks in progress are assumed true when re-encountered, and entries persist for the
// whole DeepEqual call.
internal static bool deepValueEqual(ΔValue v1, ΔValue v2, map<visit, bool> visited) {
    return deepValueEqualBoxed(v1, v2, new HashSet<visitPair>());
}

private static bool deepValueEqualBoxed(ΔValue v1, ΔValue v2, HashSet<visitPair> visited) {
    if (!v1.IsValid() || !v2.IsValid()) {
        return v1.IsValid() == v2.IsValid();
    }
    if (!AreEqual(v1.Type(), v2.Type())) {
        return false;
    }
    // The LIVE value, never the raw `boxed` field: an ADDRESSABLE Value — a slice element, an array
    // element, a struct field — carries its value behind `addrBox` (the ж<T> it aliases) and leaves
    // `boxed` null, so every raw read below saw null on BOTH sides and the identity short-circuits
    // fired. `DeepEqual([][]byte{[]byte("ab")}, [][]byte{[]byte("ac")})` was TRUE: each element's
    // backing read as null, matched "same initial entry of the same underlying array", and the
    // elementwise walk never ran (`live` IS `boxed` whenever the Value is not addressable, so this
    // changes nothing else).
    object? live1 = v1.live, live2 = v2.live;
    // Go's hard()/visited step: only pointer, map, and slice values can head a reference cycle in the
    // managed model (a bridge Value never has Kind Interface — the boxed value is always concrete).
    // Go also keys the visit on the Type; managed identity roots are per-variable objects with a fixed
    // type, so the (root1, root2) pair alone cannot collide across types.
    ΔKind kind = v1.Kind();
    if (kind == ΔPointer || kind == Map || kind == ΔSlice) {
        (object? root1, nint off1) = identityRoot(live1);
        (object? root2, nint off2) = identityRoot(live2);
        if (root1 is not null && root2 is not null && !visited.Add(new visitPair(root1, off1, root2, off2))) {
            // Already seen further up the recursion — the comparison algorithm assumes checks in
            // progress are true when it reencounters them (this is what makes DeepEqual terminate).
            return true;
        }
    }
    if (kind == Array) {
        for (nint i = 0; i < v1.Len(); i++) {
            if (!deepValueEqualBoxed(v1.Index(i), v2.Index(i), visited)) {
                return false;
            }
        }
        return true;
    }
    if (kind == ΔSlice) {
        (object? data1, nint low1) = sliceData(live1);
        (object? data2, nint low2) = sliceData(live2);
        if (data1 is null != data2 is null) {
            // A nil slice (null backing — the golib `default`) and a non-nil empty slice are not
            // deeply equal, per the DeepEqual doc.
            return false;
        }
        if (v1.Len() != v2.Len()) {
            return false;
        }
        if (v1.Len() == 0) {
            return true;
        }
        if (ReferenceEquals(data1, data2) && low1 == low2) {
            // Same initial entry of the same underlying array (&x[0] == &y[0]).
            return true;
        }
        if (GoReflect.TryByteSliceView(live1, out slice<byte> b1) &&
            GoReflect.TryByteSliceView(live2, out slice<byte> b2)) {
            // Special case for []byte, which is common (Go routes this through bytealg.Equal). Go
            // reaches it by the element KIND, never by the slice's or the element's NAME — a raw
            // []byte, a defined slice type over byte (xml.CharData, net.IP) and a slice over a
            // defined byte element all qualify — and that is exactly the set TryByteSliceView
            // aliases, so all three take one route here as they already do in Value.Bytes. The
            // `is slice<byte>` test this replaces covered only the first, silently sending the
            // other two around the elementwise Value.Index walk (both sides are the SAME Go type
            // by the AreEqual check above, so one view test settling both is sound).
            return b1.ToSpan().SequenceEqual(b2.ToSpan());
        }
        for (nint i = 0; i < v1.Len(); i++) {
            if (!deepValueEqualBoxed(v1.Index(i), v2.Index(i), visited)) {
                return false;
            }
        }
        return true;
    }
    if (kind == ΔInterface) {
        if (v1.IsNil() || v2.IsNil()) {
            return v1.IsNil() == v2.IsNil();
        }
        return deepValueEqualBoxed(v1.Elem(), v2.Elem(), visited);
    }
    if (kind == ΔPointer) {
        if (live1 is not null && ReferenceEquals(live1, live2)) {
            // Same ж<T> box — Go's same-address short-circuit (one box per variable).
            return true;
        }
        // Elem maps a nil box to the invalid Value, so two distinct nil pointers compare equal
        // through the invalid==invalid rule, and nil-vs-non-nil compares false — matching Go.
        return deepValueEqualBoxed(v1.Elem(), v2.Elem(), visited);
    }
    if (kind == Struct) {
        for ((nint i, nint n) = (0, v1.NumField()); i < n; i++) {
            if (!deepValueEqualBoxed(v1.Field(i), v2.Field(i), visited)) {
                return false;
            }
        }
        return true;
    }
    if (kind == Map) {
        if (v1.IsNil() != v2.IsNil()) {
            return false;
        }
        if (v1.Len() != v2.Len()) {
            return false;
        }
        IDictionary? m1 = mapBacking(live1);
        IDictionary? m2 = mapBacking(live2);
        if (ReferenceEquals(m1, m2)) {
            // The same map object (or both nil) — deeply equal regardless of content.
            return true;
        }
        if (m1 is null || m2 is null) {
            return m1 is null == m2 is null;
        }
        // Every element Value is typed by the map's DECLARED element type, exactly as MapIndex and
        // MapIter.Value type theirs — never by the stored object's dynamic type. A slot-derived Value
        // is Go's rule for the whole bridge, and a map entry is a slot: an element read through
        // makeReflectValue instead reports the INVALID zero Value whenever the entry physically holds
        // C# null, so a nil map element compared EQUAL to a missing key and UNEQUAL to the canonical
        // typed nil the write path stores. The two spellings of one nil then separated every map with
        // a nil element that had been WRITTEN through reflect on one side and declared as a literal on
        // the other — `map[string]*Small{"20": nil}` decoded by encoding/json vs the same literal
        // (encoding/json's TestUnmarshal `All` fixture, rows #56–#63, is exactly that pair).
        System.Type? elemType1 = elementTypeOf(v1);
        System.Type? elemType2 = elementTypeOf(v2);
        // Go's range visits a NIL key like any other, but the backing Dictionary cannot HOLD one —
        // golib keeps that entry in a dedicated slot, invisible to the walk below (and its presence
        // alone does not show up in the Len comparison above, which one extra ordinary key hides).
        (bool nilPresent1, object? nilValue1) = live1 is IMap nilMap1 ? nilMap1.NilKeyEntry : (false, null);
        (bool nilPresent2, object? nilValue2) = live2 is IMap nilMap2 ? nilMap2.NilKeyEntry : (false, null);
        if (nilPresent1 != nilPresent2) {
            return false;
        }
        if (nilPresent1 && !deepValueEqualBoxed(mapElemValue(nilValue1, elemType1, v1.flag),
                                                mapElemValue(nilValue2, elemType2, v2.flag), visited)) {
            return false;
        }
        foreach (DictionaryEntry entry in m1) {
            if (!m2.Contains(entry.Key)) {
                // Go: MapIndex yields the invalid Value for a missing key → not equal.
                return false;
            }
            // Two stored nil elements now recurse as two VALID nil Values of the element type, and
            // agree through the kind's own nil rule (pointer: both boxes nil; interface: IsNil ==
            // IsNil), rather than through the invalid==invalid rule that only held when BOTH sides
            // happened to spell nil the same way.
            if (!deepValueEqualBoxed(mapElemValue(entry.Value, elemType1, v1.flag),
                                     mapElemValue(m2[entry.Key], elemType2, v2.flag), visited)) {
                return false;
            }
        }
        return true;
    }
    if (kind == Func) {
        // Go: "Func values are deeply equal if both are nil; otherwise they are not deeply equal."
        // This must be asked of the values, NOT inferred from the invalid==invalid rule above: that
        // rule only fires for a nil func boxed as `any` (the null object). A nil func reached as a
        // STRUCT FIELD — or as a slice/array element, or a map value — is typed by its static func
        // type and is therefore a VALID nil Value (see Value.Field), so an unconditional false
        // declared two nil func fields unequal. compress/flate's TestWriterReset nils out the
        // compressor's fill/step/bulkHasher precisely so DeepEqual can compare the rest, and every
        // one of its ten levels failed on that.
        return v1.IsNil() && v2.IsNil();
    }
    if (kind == ΔInt || kind == Int8 || kind == Int16 || kind == Int32 || kind == Int64) {
        return v1.Int() == v2.Int();
    }
    if (kind == ΔUint || kind == Uint8 || kind == Uint16 || kind == Uint32 || kind == Uint64 || kind == Uintptr) {
        return v1.Uint() == v2.Uint();
    }
    if (kind == ΔString) {
        return v1.String() == v2.String();
    }
    if (kind == ΔBool) {
        return v1.Bool() == v2.Bool();
    }
    if (kind == Float32 || kind == Float64) {
        // C# double == carries IEEE semantics: a NaN is not equal to itself, exactly like Go.
        return v1.Float() == v2.Float();
    }
    if (kind == Complex64 || kind == Complex128) {
        return v1.Complex() == v2.Complex();
    }
    { /* default: */
        // Can't do better than this: normal equality suffices.
        return AreEqual(valueInterface(v1, false), valueInterface(v2, false));
    }
}

// elementTypeOf returns a map Value's DECLARED element type, or null when the bridge has no managed
// type for it (a synthetic descriptor with no sysType) — in which case the walk falls back to the
// dynamic typing it had before, which is right for every entry that physically holds a value.
private static System.Type? elementTypeOf(ΔValue v) {
    return v.typ_ == nil ? null : GoReflect.ElementType(v.typ_.Value.sysType);
}

// mapElemValue builds the Value for one map entry, typed by the map's declared element type — the
// slot rule MapIndex and MapIter.Value already follow, so a lookup, a range and a DeepEqual over one
// map all describe its elements identically.
private static ΔValue mapElemValue(object? boxed, System.Type? elemType, flag inheritRO) {
    return elemType is null ? makeReflectValue(boxed) : makeTypedValue(boxed, elemType, null, inheritRO);
}

// A visited entry: the identity roots of two values under in-progress comparison, compared by managed
// reference identity plus the slice window offset (Go keys on the data addresses; a pointer's root is
// its ж<T> box, a map's its backing Dictionary, a slice's its backing array + Low).
private readonly struct visitPair(object a1, nint off1, object a2, nint off2) : IEquatable<visitPair> {
    private readonly object m_a1 = a1;
    private readonly nint m_off1 = off1;
    private readonly object m_a2 = a2;
    private readonly nint m_off2 = off2;

    public bool Equals(visitPair other) {
        return ReferenceEquals(m_a1, other.m_a1) && ReferenceEquals(m_a2, other.m_a2) &&
               m_off1 == other.m_off1 && m_off2 == other.m_off2;
    }

    public override bool Equals(object? obj) {
        return obj is visitPair other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(RuntimeHelpers.GetHashCode(m_a1), m_off1, RuntimeHelpers.GetHashCode(m_a2), m_off2);
    }
}

// identityRoot returns the managed object that stands for a value's Go data address, for cycle
// detection: a pointer's ж<T> box, a map's backing Dictionary, a slice's backing array + Low. A nil
// value (null box, nil pointer, null backing) has no root — Go never puts nil in the visited map.
private static (object? root, nint offset) identityRoot(object? boxed) {
    switch (boxed) {
        case null:
            return (null, 0);
        case ISlice:
            return sliceData(boxed);
        case IMap:
            return (mapBacking(boxed), 0);
        // The STRUCTURAL nil-pointer question, asked through the interface every pointer box
        // implements — not the reflected `IsNull` property this used to read, which reports a real
        // address whose pointee is nil (`&i` with a nil `i`) as nil and would drop it from cycle
        // detection. See ж<T>.IsNilPointer.
        case INilPointer { IsNilPointer: true }:
            return (null, 0);
        default:
            return (boxed, 0);
    }
}

// Per-closed-generic-type accessors for the REAL backing store of a boxed golib container. slice<T>'s
// public Source materializes a detached copy, so identity (and nil-ness — a nil slice is the golib
// `default`, null m_array) must come from the actual m_array/m_low fields; map<K,V> likewise only
// exposes its Dictionary internally. Field reads are cached per type.
private static readonly ConcurrentDictionary<System.Type, (FieldInfo? array, FieldInfo? low, FieldInfo? nested)> s_sliceFields = new();
private static readonly ConcurrentDictionary<System.Type, FieldInfo?> s_mapField = new();

// sliceData returns a boxed slice's backing array and window offset — (null, 0) for the nil slice.
//
// A generated NAMED-slice wrapper (`type S []E`) holds a slice<E> STRUCT, not the m_array/m_low pair
// this probe reads, so — exactly as for the named-MAP wrapper mapBacking handles below, and with the
// same signature — the probe takes a second step through such a field. Without it BOTH sides of a
// named-slice comparison resolved to null and the "same initial entry of the same underlying array"
// short-circuit above matched them, so two named slices of equal length were reported deeply equal
// REGARDLESS of their contents; a nil named slice compared equal to an empty one (both backings read
// null, so the nil/empty rule never fired); and identityRoot was blind the same way, so a named-slice
// cycle was never detected either. encoding/xml's TestCopyTokenCharData/TestCopyTokenComment are
// exactly that shape: CopyToken really does clone its buffer, yet mutating the ORIGINAL still
// compared equal to the clone — which the test reports as "uses same buffer", pointing at a copy that
// was never the defect. The second step is taken only for a slice-KINDED type, so a struct that
// merely HAS a slice field can never be mistaken for one, and the recursion terminates because the
// nested value is a strictly smaller struct — slice<E> carries the m_array/m_low pair itself.
private static (object? data, nint low) sliceData(object? boxed) {
    if (boxed is null) {
        return (null, 0);
    }
    (FieldInfo? array, FieldInfo? low, FieldInfo? nested) = s_sliceFields.GetOrAdd(boxed.GetType(), static t => {
        FieldInfo? array = t.GetField("m_array", BindingFlags.Instance | BindingFlags.NonPublic);
        if (array is not null) {
            return (array, t.GetField("m_low", BindingFlags.Instance | BindingFlags.NonPublic), null);
        }
        if (typeof(ISlice).IsAssignableFrom(t)) {
            foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
                if (typeof(ISlice).IsAssignableFrom(f.FieldType)) {
                    return (null, null, f);
                }
            }
        }
        return (null, null, null);
    });
    if (array is null) {
        return nested is null ? (null, 0) : sliceData(nested.GetValue(boxed));
    }
    object? data = array.GetValue(boxed);
    return data is null ? (null, 0) : (data, low is null ? 0 : (nint)low.GetValue(boxed)!);
}

// mapBacking returns a boxed map's backing Dictionary — null for the nil map (no backing store).
//
// A generated NAMED-map wrapper (`type M map[K]V`) holds a map<K,V> STRUCT, and map<K,V> implements
// only the GENERIC dictionary surface (IMap<K,V> : IDictionary<K,V>) — nothing assignable to the
// non-generic IDictionary this walk needs. The probe therefore takes a second step through such a
// field: without it BOTH sides of a named-map comparison resolved to null, the ReferenceEquals(m1, m2)
// short-circuit above matched them as "the same map object", and two named maps of equal length were
// reported deeply equal REGARDLESS of their contents (identityRoot was blind the same way, so a
// named-map cycle was never detected either). The recursion terminates because the nested value is a
// strictly smaller struct — map<K,V>'s own backing store IS an IDictionary.
private static IDictionary? mapBacking(object? boxed) {
    if (boxed is null) {
        return null;
    }
    FieldInfo? field = s_mapField.GetOrAdd(boxed.GetType(), static t => {
        FieldInfo? nested = null;
        foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
            if (typeof(IDictionary).IsAssignableFrom(f.FieldType)) {
                return f;
            }
            if (nested is null && typeof(IMap).IsAssignableFrom(f.FieldType)) {
                nested = f;
            }
        }
        return nested;
    });
    object? value = field?.GetValue(boxed);
    return value as IDictionary ?? (value is IMap ? mapBacking(value) : null);
}

} // end reflect_package
