// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// go2cs NATIVE IMPLEMENTATION (hand-owned; replaces the converted hashtriemap.go output). Go's
// HashTrieMap is a lock-free hash-trie seeded entirely from one runtime descriptor read:
// NewHashTrieMap takes `abi.TypeOf(m).MapType().Hasher` — a raw function pointer into the hashing
// machinery the compiler emits for `map[K]V` — together with `Key.Equal` and `Elem.Equal`, its
// matching bit-compare thunks. All three take unsafe.Pointers and mean "hash / compare the bytes AT
// this address".
//
// The managed reflection bridge cannot honor that contract and must not pretend to. A managed
// address names no value: two boxes holding equal strings sit at different addresses, and a pointee
// containing references moves across a GC — so an address-derived hash would stop `unique.Make(x)`
// agreeing with itself, the precise inverse of the package's purpose. Populating `Hasher` with
// something plausible-but-fake is barred by the standing rule that a descriptor field whose read
// cannot be honored must stay EMPTY: it would turn a loud construction failure into a map that is
// silently wrong. So the literal conversion compiles and can never run — `NewHashTrieMap` threw
// inside the package initializer of every `unique` consumer, taking net/netip down with it.
//
// The remedy is the sync.Mutex / sync.WaitGroup precedent (mutex.cs): runtime-coupled machinery gets
// a managed-native rewrite that keeps the Go API and its concurrency contract while dropping the
// mechanism. Semantics over mechanism — nothing below is a trie. The store is a
// ConcurrentDictionary, whose guarantees line up member for member (see each method), and Go's
// keyHash/keyEqual/valEqual triple becomes EqualityComparer<K>/<V>.Default, which for every key
// shape the converted corpus interns IS Go's `==`:
//
//   - ж<T> — unique's own `map[*abi.Type]any` — implements IEquatable<ж<T>> as pointer IDENTITY with
//     a matching identity hash, and abi.TypeFor<T>() interns one descriptor box per System.Type, so
//     one Go type always presents one key.
//   - A [GoType] struct — net/netip's `addrDetail{isV6 bool; zoneV6 string}`, the shape unique
//     actually interns — carries a generated field-wise Equals over `==` plus a HashCode.Combine of
//     the same fields, which is Go's struct `==` exactly. It does not implement IEquatable<T>, so
//     EqualityComparer<T>.Default routes through the object override; that lands on the same
//     comparison, at the cost of a box per lookup.
//   - @string compares and hashes by CONTENT, as Go's string `==` does.
//
// ⚠ If a lookup here ever dies with EntryPointNotFoundException at IEquatable<T>.Equals, the key is
// not the type it claims to be and the defect is upstream, not in this file. The known instance:
// abi.TypeFor<T>() for an INTERFACE T returns the descriptor's `Equal` DELEGATE — Type.Elem()'s
// PtrType reinterpretation lands on the wrong member under the managed layout — and shared generics
// let that object into a ConcurrentDictionary<ж<abi.Type>, …> uncast-checked. Deliberately NOT
// defended against: tolerating a type-unsafe key is the same fake-but-plausible move the empty-Hasher
// rule forbids. The loud failure is correct; the fix belongs in internal/abi.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using go.golib;

// Hand-owned native replacement of the converted hashtriemap.go output — the converter skips
// regenerating a file that carries this marker, so a -stdlib reconvert preserves it (see
// containsManualConversionMarker). hashtriemap.go is this package's ONLY Go file, so marking it makes
// the whole package hand-owned: the driver `continue`s on unmarkedFileCount == 0 and stops re-emitting
// internal.concurrent.csproj, package_info.cs and README.md as well (the position internal/godebug is
// in). A hashtriemap.cs.auto review sibling IS produced, since 2026-08-19: the fully-hand-owned
// branch used to panic here, and the cause was neither this file's genericity nor its reduced set of
// whole-package pre-passes — emitAutoConversionSiblings hand-rolled a copy of newFileVisitor that had
// drifted, leaving `blankImportInits` nil for visitFile to dereference. It builds through the real
// constructor now. The marker's protection is unaffected and is proven in both directions by a seeded
// reconvert.
[module: go.GoManualConversion]

namespace go.@internal;

partial class concurrent_package {

// HashTrieMap is an implementation of a concurrent hash-trie. The implementation
// is designed around frequent loads, but offers decent performance for stores
// and deletes as well, especially if the map is larger. It's primary use-case is
// the unique package, but can be used elsewhere as well.
[GoType] partial struct HashTrieMap<K, V>
{
    // The whole of the map's state, held behind a REFERENCE exactly as Go holds it behind the
    // `root *indirect[K, V]` pointer: a by-value copy of a HashTrieMap shares one map, and
    // NewHashTrieMap hands the map out as a ж<HashTrieMap<K, V>> either way.
    internal mapStore<K, V> store;
}

// The map's backing store. Named as a type of its own so that the [GoType]-generated members of
// HashTrieMap (its field-wise constructor, its field-reference accessor) never restate
// ConcurrentDictionary<K, V>: the converter renders Go's `comparable` constraint as `new()`, which
// carries no C# `notnull` signal, and generated code cannot carry the suppression that vacuous
// mismatch would need (a Go map key is never a null reference).
#pragma warning disable CS8714
internal sealed class mapStore<K, V> : ConcurrentDictionary<K, V>
{
}
#pragma warning restore CS8714

// NewHashTrieMap creates a new HashTrieMap for the provided key and value.
public static ж<HashTrieMap<K, V>> NewHashTrieMap<K, V>()
{
    return Ꮡ(new HashTrieMap<K, V>(store: new mapStore<K, V>()));
}

// Load returns the value stored in the map for a key, or nil if no
// value is present.
// The ok result indicates whether value was found in the map.
[GoRecv] public static (V value, bool ok) Load<K, V>(this ref HashTrieMap<K, V> ht, K key)
{
    if (storeOf(ref ht).TryGetValue(key, out V? value)) {
        return (value!, true);
    }
    return (@new<V>().ValueSlot, false);
}

// LoadOrStore returns the existing value for the key if present.
// Otherwise, it stores and returns the given value.
// The loaded result is true if the value was loaded, false if stored.
//
// Exactly one caller of a racing set observes loaded == false, which is what unique.Make relies on
// to keep one canonical value per key: TryAdd fails for every loser, and the retry then finds the
// winner's value. (GetOrAdd would be a single call but cannot report WHICH outcome occurred.)
public static (V result, bool loaded) LoadOrStore<K, V>(this ж<HashTrieMap<K, V>> Ꮡht, K key, V value)
{
    mapStore<K, V> store = storeOf(ref Ꮡht.Value);
    while (ᐧ) {
        if (store.TryGetValue(key, out V? existing)) {
            return (existing!, true);
        }
        if (store.TryAdd(key, value)) {
            return (value, false);
        }
    }
}

// CompareAndDelete deletes the entry for key if its value is equal to old.
//
// If there is no current value for key in the map, CompareAndDelete returns false
// (even if the old value is the nil interface value).
[GoRecv] public static bool /*deleted*/ CompareAndDelete<K, V>(this ref HashTrieMap<K, V> ht, K key, V old)
{
    mapStore<K, V> store = storeOf(ref ht);
    // Go reaches its value comparison only once the key is found, and only then can that comparison
    // panic — mirror both the order and the panic (see mustBeComparable).
    if (!store.ContainsKey(key)) {
        return false;
    }
    mustBeComparable(old);
    // Atomic compare-and-remove: the pair overload removes only if the stored value still compares
    // equal to old under EqualityComparer<V>.Default, which is Go's valEqual for every V above.
    return store.TryRemove(new KeyValuePair<K, V>(key, old));
}

// All returns an iter.Seq2 that produces all key-value pairs in the map.
// The enumeration does not represent any consistent snapshot of the map,
// but is guaranteed to visit each unique key-value pair only once. It is
// safe to operate on the tree during iteration. No particular enumeration
// order is guaranteed.
//
// ConcurrentDictionary's enumerator satisfies all three: it is weakly consistent (never throws on
// concurrent mutation, so unique's cleanup pass can CompareAndDelete while it walks), it yields each
// live key once, and it promises no order.
public static Action<Func<K, V, bool>> All<K, V>(this ж<HashTrieMap<K, V>> Ꮡht)
{
    mapStore<K, V> store = storeOf(ref Ꮡht.Value);
    return (Func<K, V, bool> yield) => {
        foreach (KeyValuePair<K, V> pair in store) {
            if (!yield(pair.Key, pair.Value)) {
                return;
            }
        }
    };
}

// storeOf returns the map's backing store, creating it once on first use (race-safe). Go 1.23's
// zero HashTrieMap is not usable — every method dereferences the nil root and keyHash that
// NewHashTrieMap would have filled in — but nothing in the converted corpus depends on that panic,
// and seeding lazily costs one null check while removing a whole class of null dereference from the
// accessors. Same idiom, and same reasoning, as sync.Mutex's gateOf.
private static mapStore<K, V> storeOf<K, V>(ref HashTrieMap<K, V> ht)
{
    mapStore<K, V>? store = Volatile.Read(ref ht.store);

    if (store is not null) {
        return store;
    }

    mapStore<K, V> created = new();

    return Interlocked.CompareExchange(ref ht.store, created, null) ?? created;
}

// Go compares the two values with V's own `==`, and for an INTERFACE V that comparison panics when
// the dynamic type is not comparable: `HashTrieMap[K, V comparable]` admits `any` (Go 1.20 let
// interfaces satisfy comparable), which moves the check to run time. Mirror the panic rather than
// letting EqualityComparer<V>.Default answer a question Go refuses to answer. Inert for every other
// V — the converter renders Go's `comparable` as `new()`, so the constraint carries no C# signal,
// but a non-interface V that reached here was comparable at the Go type check.
private static void mustBeComparable<V>(V value) {
    if (!dynamicallyComparable<V>.applies || value is null) {
        return;
    }
    Type dynamicType = value.GetType();
    if (!GoReflect.IsComparable(dynamicType)) {
        throw panic((@string)$"comparing uncomparable type {GoReflect.GoTypeName(dynamicType)}");
    }
}

// Whether V's comparability is a run-time question at all, resolved once per instantiation. Only an
// interface-typed V is: `any` converts to object, a named Go interface to a C# interface.
private static class dynamicallyComparable<V> {
    internal static readonly bool applies = typeof(V).IsInterface || typeof(V) == typeof(object);
}

} // end concurrent_package
