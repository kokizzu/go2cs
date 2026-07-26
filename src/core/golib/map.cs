// map.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace go;

public interface IMap
{
    nint Length { get; }

    /// <summary>
    /// Gets a flag indicating whether the map is nil — no backing store; an empty but
    /// allocated map is NOT nil (Go's only legal map comparison is against nil).
    /// </summary>
    bool IsNil { get; }

    /// <summary>
    /// Gets the map's NIL-KEY entry: <c>(false, null)</c> when the key type cannot be nil or no
    /// nil-key entry is present, otherwise <c>(true, value)</c> with the value boxed.
    /// </summary>
    /// <remarks>
    /// Go's <c>range</c> visits a nil key like any other, but the nil entry cannot live in the
    /// backing <see cref="Dictionary{TKey, TValue}"/> (which rejects a null key), so it occupies a
    /// dedicated slot. This member is how a NON-generic consumer — the reflection bridge's
    /// <c>DeepEqual</c>, which walks the backing <see cref="IDictionary"/> directly — observes the
    /// entry that walk cannot see.
    /// </remarks>
    (bool present, object? value) NilKeyEntry { get; }

    /// <summary>
    /// Returns a shallow clone of this map with an INDEPENDENT backing store — the runtime
    /// intrinsic behind <c>maps.Clone</c> (Go's <c>runtime.mapclone</c>). Keys and values are
    /// copied by ordinary assignment; mutating the clone does not affect the original. A nil
    /// map clones to a nil map.
    /// </summary>
    IMap CloneMap();
}

public interface IMap<TKey, TValue> : IMap, IDictionary<TKey, TValue> where TKey : notnull
{
    (TValue, bool) this[TKey key, bool _] { get; }

    /// <summary>
    /// Default <see cref="IMap.CloneMap"/> for any <see cref="IMap{TKey, TValue}"/> — covers both
    /// the concrete <see cref="map{TKey, TValue}"/> and the generated named-map wrappers (which
    /// wrap a shared <see cref="map{TKey, TValue}"/>). Builds a fresh <see cref="map{TKey, TValue}"/>
    /// populated from this map's entries, so the clone's backing store is independent of the
    /// original (Go's shallow clone: keys/values set by ordinary assignment). A nil map clones to nil.
    /// </summary>
    IMap IMap.CloneMap()
    {
        return IsNil ? default(map<TKey, TValue>) : new map<TKey, TValue>(this);
    }

    /// <summary>
    /// Default <see cref="IMap.NilKeyEntry"/> for any <see cref="IMap{TKey, TValue}"/> — asks the
    /// comma-ok indexer about the nil key, which both the concrete map and the generated named-map
    /// wrappers already answer. A value-type key can never BE nil, and that test is a JIT-time
    /// constant, so those instantiations fold to a constant <c>(false, null)</c>.
    /// </summary>
    (bool present, object? value) IMap.NilKeyEntry
    {
        get
        {
            if (typeof(TKey).IsValueType)
                return (false, null);

            (TValue value, bool present) = this[default!, true];

            return (present, value);
        }
    }
}

public readonly struct map<TKey, TValue> : IMap<TKey, TValue>, ISupportMake<map<TKey, TValue>> where TKey : notnull
{
    /// <summary>
    /// The backing store — a <see cref="Dictionary{TKey, TValue}"/> that additionally carries Go's
    /// NIL-KEY slot. Go's map accepts a nil key whenever the key type can be nil (<c>map[any]V</c>,
    /// <c>map[error]V</c>, <c>map[*T]V</c>, a named-interface key…), while
    /// <see cref="Dictionary{TKey, TValue}"/> rejects one with an
    /// <see cref="System.ArgumentNullException"/> before the comparer is ever consulted — so the nil
    /// entry gets a slot of its own instead of a bucket.
    /// </summary>
    /// <remarks>
    /// The slot belongs to the STORE, not to this struct. A Go map is a reference type: every copy
    /// of a <c>map&lt;TKey, TValue&gt;</c> value must observe the same nil entry, and a field on the
    /// struct would make a write through one copy invisible through another. DERIVING from
    /// Dictionary (rather than wrapping it) also keeps the struct exactly one reference wide — no
    /// extra allocation, no widened value — and leaves every existing Dictionary interop path (the
    /// implicit conversions, <c>Keys</c>/<c>Values</c>, the <see cref="ICollection{T}"/> casts, the
    /// reflection bridge's backing-field probe) binding exactly as before.
    /// </remarks>
    private sealed class NilKeyDictionary : Dictionary<TKey, TValue>
    {
        public NilKeyDictionary()
        {
        }

        public NilKeyDictionary(int capacity) : base(capacity)
        {
        }

        public NilKeyDictionary(IDictionary<TKey, TValue> source) : base(source)
        {
        }

        public NilKeyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) : base(source)
        {
        }

        /// <summary>Gets or sets a flag indicating whether an entry is stored under the nil key.</summary>
        public bool HasNilKey;

        /// <summary>Gets or sets the value stored under the nil key (meaningful only when <see cref="HasNilKey"/>).</summary>
        public TValue NilKeyValue = default!;
    }

    private readonly NilKeyDictionary m_map;

    public map()
    {
        m_map = new NilKeyDictionary();
    }

    public map(nint size)
    {
        m_map = new NilKeyDictionary((int)size);
    }

    public map(IEnumerable<KeyValuePair<TKey, TValue>> map)
    {
        // A value-type key can never BE nil — a JIT-time constant — so no source can carry a nil-key
        // entry and Dictionary's own bulk copy applies exactly as it did before nil keys existed.
        if (typeof(TKey).IsValueType)
        {
            m_map = new NilKeyDictionary(map);
            return;
        }

        // A concrete golib map copies through its store directly, carrying the nil-key entry over as
        // the slot it is (no Dictionary constructor would accept it as a pair).
        if (map is map<TKey, TValue> source)
        {
            m_map = source.m_map is null ? new NilKeyDictionary() : new NilKeyDictionary((IDictionary<TKey, TValue>)source.m_map);

            if (source.m_map is { HasNilKey: true })
            {
                m_map.NilKeyValue = source.m_map.NilKeyValue;
                m_map.HasNilKey = true;
            }

            return;
        }

        // Any other source — a generated named-map wrapper, a plain Dictionary — is copied entry by
        // entry through the indexer, so a null key ROUTES to the slot rather than throwing.
        m_map = new NilKeyDictionary();

        foreach (KeyValuePair<TKey, TValue> entry in map)
            this[entry.Key] = entry.Value;
    }

    // Reports whether key is Go's NIL key. `typeof(TKey).IsValueType` is a JIT-time constant, so for
    // a value-type key — the overwhelmingly common shape, and the one PerfMap measures — this test
    // and every nil-slot branch it guards are folded away entirely: those instantiations compile to
    // exactly the code they had before nil keys were supported. Only a reference-typed key (`any`,
    // an interface, `*T`, a func/map/slice-typed key) pays a null check.
    private static bool isNilKey(TKey key)
    {
        return !typeof(TKey).IsValueType && (object?)key is null;
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            if (m_map is null)
                return 0;

            // The nil entry counts toward len(m) like any other key.
            return !typeof(TKey).IsValueType && m_map.HasNilKey ? m_map.Count + 1 : m_map.Count;
        }
    }

    public TValue this[TKey key]
    {
        // Reading from a nil map yields the zero value in Go, so route through the
        // null-safe TryGetValue rather than dereferencing m_map directly.
        get => TryGetValue(key, out TValue? value) ? value : default!;
        set
        {
            // Writing to a nil map panics in Go ("assignment to entry in nil map").
            if (m_map is null)
                throw new PanicException("assignment to entry in nil map");

            if (isNilKey(key))
                setNilKey(value);
            else
                m_map[key] = value;
        }
    }

    public (TValue, bool) this[TKey key, bool _]
    {
        // Comma-ok read of a nil (or absent) key yields (zero, false).
        get => TryGetValue(key, out TValue? value) ? (value!, true) : (default!, false);
    }

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        // Adding to a nil map panics in Go, the same as an index assignment.
        if (m_map is null)
            throw new PanicException("assignment to entry in nil map");

        if (isNilKey(key))
            addNilKey(value);
        else
            m_map.Add(key, value);
    }

    // Set writes a key with Go's OVERWRITE semantics (unlike Add, which throws on a duplicate
    // key). It exists so a nested map assignment `m[k1][k2] = v` can emit as
    // `m[k1].Set(k2, v)`: `m[k1]` returns a map VALUE (readonly struct), and a C# indexer
    // SETTER cannot run on that rvalue (CS1612) — but a METHOD can, and it mutates the shared
    // backing dictionary, so the write is visible through the original (internal/dag).
    public void Set(TKey key, TValue value)
    {
        if (m_map is null)
            throw new PanicException("assignment to entry in nil map");

        if (isNilKey(key))
            setNilKey(value);
        else
            m_map[key] = value;
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (isNilKey(key))
            return removeNilKey();

        // delete() on a nil map is a no-op in Go (no panic).
        return m_map?.Remove(key) ?? false;
    }

    public void Clear()
    {
        // clear() on a nil map is a no-op in Go.
        if (m_map is null)
            return;

        m_map.Clear();

        if (!typeof(TKey).IsValueType)
        {
            m_map.HasNilKey = false;
            m_map.NilKeyValue = default!;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (isNilKey(key))
            return tryGetNilKey(out value);

        if (m_map is not null)
            return m_map.TryGetValue(key, out value!);

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        if (isNilKey(key))
            return m_map is { HasNilKey: true };

        // A nil map contains no keys.
        return m_map?.ContainsKey(key) ?? false;
    }

    #region [ Nil-key slot ]

    // The nil-key operations live out of line so the members above stay small enough for the JIT to
    // inline: a value-type key never reaches them (the guard folds to false and the call site is
    // dropped), and a reference-type key only pays the call on the nil-key path itself.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void setNilKey(TValue value)
    {
        m_map.NilKeyValue = value;
        m_map.HasNilKey = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void addNilKey(TValue value)
    {
        // Matches Dictionary<TKey, TValue>.Add's duplicate-key contract (the emitted converted code
        // always writes through the indexer or Set, both of which OVERWRITE like Go).
        if (m_map.HasNilKey)
            throw new System.ArgumentException("An item with the same key has already been added.", nameof(value));

        m_map.NilKeyValue = value;
        m_map.HasNilKey = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool removeNilKey()
    {
        if (m_map is not { HasNilKey: true })
            return false;

        m_map.HasNilKey = false;
        m_map.NilKeyValue = default!;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool tryGetNilKey(out TValue value)
    {
        if (m_map is { HasNilKey: true })
        {
            value = m_map.NilKeyValue;
            return true;
        }

        value = default!;
        return false;
    }

    // Yields the nil-key entry ahead of the dictionary's buckets. Go's range order over a map is
    // unspecified (and deliberately randomized), so the position is free — putting it first keeps
    // every map WITHOUT a nil key on the dictionary's own enumerator, unwrapped.
    private static IEnumerator<KeyValuePair<TKey, TValue>> enumerateWithNilKey(NilKeyDictionary store)
    {
        yield return new KeyValuePair<TKey, TValue>(default!, store.NilKeyValue);

        foreach (KeyValuePair<TKey, TValue> entry in (Dictionary<TKey, TValue>)store)
            yield return entry;
    }

    #endregion

    public bool Equals(map<TKey, TValue> other)
    {
        // Go maps are reference types: `m == nil` is true only for the nil map, and two
        // map values are equal only when they share the same backing store. Comparing by
        // reference identity captures both (and is null-safe — two nil maps share a null
        // backing store and so compare equal, while a nil map differs from an empty one).
        return ReferenceEquals(m_map, other.m_map);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is map<TKey, TValue> other && Equals(other);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        // Enumerates through this map (not the raw dictionary) so a nil-key entry appears, and
        // renders a nil key or value as Go's fmt does.
        return $"map[{(m_map is null ? "<nil>" : string.Join(" ", this.Select(static kvp => $"{render(kvp.Key)}:{render(kvp.Value)}").Take(20)))}{(Count > 20 ? " ..." : "")}]";

        static string render<T>(T value)
        {
            return value?.ToString() ?? "<nil>";
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Reference-based hash, consistent with the identity Equals above; a nil map hashes to 0.
        return m_map?.GetHashCode() ?? 0;
    }

    #region [ Operators ]

    // Enable implicit conversions between map<TKey, TValue> and IDictionary<T>
    public static implicit operator map<TKey, TValue>(Dictionary<TKey, TValue> value)
    {
        return new map<TKey, TValue>(value);
    }

    public static implicit operator Dictionary<TKey, TValue>(map<TKey, TValue> value)
    {
        return value.m_map;
    }

    // map<TKey, TValue> to map<TKey, TValue> comparisons
    public static bool operator ==(map<TKey, TValue> a, map<TKey, TValue> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(map<TKey, TValue> a, map<TKey, TValue> b)
    {
        return !(a == b);
    }

    // map<T> to IMap comparisons
    public static bool operator ==(IMap a, map<TKey, TValue> b)
    {
        return b.Equals(a);
    }

    public static bool operator !=(IMap a, map<TKey, TValue> b)
    {
        return !(a == b);
    }

    public static bool operator ==(map<TKey, TValue> a, IMap b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(map<TKey, TValue> a, IMap b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Gets a flag indicating whether the map is nil — no backing store.
    /// </summary>
    public bool IsNil => m_map is null;

    // map<T> to nil comparisons
    public static bool operator ==(map<TKey, TValue> map, NilType _)
    {
        // A map is nil only when it has no backing store — an empty but allocated map is not nil.
        return map.m_map is null;
    }

    public static bool operator !=(map<TKey, TValue> map, NilType nil)
    {
        return !(map == nil);
    }

    public static bool operator ==(NilType nil, map<TKey, TValue> map)
    {
        return map == nil;
    }

    public static bool operator !=(NilType nil, map<TKey, TValue> map)
    {
        return map != nil;
    }

    public static implicit operator map<TKey, TValue>(NilType _)
    {
        return default;
    }

    #endregion

    #region [ Interface Implementations ]

    nint IMap.Length => Count;

    (bool present, object? value) IMap.NilKeyEntry
    {
        // Reads the slot directly rather than through the IMap<TKey, TValue> default (which asks the
        // comma-ok indexer) — same answer, no interface dispatch.
        get
        {
            if (typeof(TKey).IsValueType || m_map is not { HasNilKey: true })
                return (false, null);

            return (true, m_map.NilKeyValue);
        }
    }

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => this[key];
        set => this[key] = value;
    }

    // Keys/Values MATERIALIZE when a nil-key entry is present — Dictionary's own live views cannot
    // represent it — and hand back those views unchanged otherwise. A nil map has neither.
    ICollection<TValue> IDictionary<TKey, TValue>.Values =>
        m_map is null ? System.Array.Empty<TValue>() :
        !typeof(TKey).IsValueType && m_map.HasNilKey ? this.Select(static kvp => kvp.Value).ToList() :
        m_map.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys =>
        m_map is null ? System.Array.Empty<TKey>() :
        !typeof(TKey).IsValueType && m_map.HasNilKey ? this.Select(static kvp => kvp.Key).ToList() :
        m_map.Keys;

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        // ICollection's contract: remove only when the stored value matches too.
        return ((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item) && Remove(item.Key);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Clear()
    {
        Clear();
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (KeyValuePair<TKey, TValue> entry in this)
            array[arrayIndex++] = entry;
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        // Ranging over a nil map performs zero iterations in Go.
        if (m_map is null)
            return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();

        // Go's range visits the nil key like any other key.
        if (!typeof(TKey).IsValueType && m_map.HasNilKey)
            return enumerateWithNilKey(m_map);

        return ((IEnumerable<KeyValuePair<TKey, TValue>>)m_map).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        // Yields KeyValuePair<TKey, TValue> entries, matching what Dictionary's own non-generic
        // enumerator produces (the reflection bridge's MapIter reads .Key/.Value off Current).
        return ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
    }

    #endregion

    /// <inheritdoc />
    public static map<TKey, TValue> Make(nint p1 = 0, nint p2 = -1)
    {
        return new map<TKey, TValue>(p1);
    }
}
