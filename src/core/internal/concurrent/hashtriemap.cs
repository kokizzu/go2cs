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
using @unsafe = go.unsafe_package;

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

    // THE HASH HOOK. Go carries `keyHash func(unsafe.Pointer, uintptr) uintptr` as a plain field and
    // the package's own test WRITES it: TestHashTrieMapBadHash — one of the suite's two top-level
    // tests, and the parent of nine of its eighteen subtests — replaces it on a freshly built map
    // with `func(unsafe.Pointer, uintptr) uintptr { return 0 }` under the comment "Stub out the good
    // hash function with a terrible one. Everything should still work as expected." The assertion is
    // therefore not about the hash at all: it is that the map's CONTRACT survives total collision.
    //
    // That contract is honored here for real, not simulated. Installing a hook rebuilds the store
    // behind an IEqualityComparer<K> whose GetHashCode returns whatever the hook returns, leaving
    // Equals as it was — which is exactly the split Go's test makes (only keyHash is replaced;
    // keyEqual and valEqual stay the map's own). Every key then lands in one bucket and correctness
    // rests entirely on equality and on the store's own concurrency guarantees, which is the state
    // Go's badly-hashed trie is in and the state the nine subtests measure.
    //
    // ⚠ Only ONE of the hook's two inputs can be honored, and the other is passed as the NIL pointer
    // rather than as a plausible substitute. `seed` is genuine — a real per-map salt (see
    // mapStore.seed) — but the unsafe.Pointer argument means "the key's bytes are AT this address",
    // and a managed address names no value; that is this whole file's founding premise. A hook that
    // ignores the address — the only kind that can be honored, and the kind Go's test writes — gets
    // its exact answer. A hook that dereferences it faults loudly, which is the correct outcome under
    // the same rule that keeps the descriptor's Hasher empty: a loud failure beats a silent lie.
    //
    // Note what this does NOT claim. Because the address is unavailable, every honorable hook is a
    // function of the seed alone, i.e. constant across keys — so honoring a hook and forcing total
    // collision are the same act here. Nothing is lost by that: no hook this implementation could
    // accept was ever able to distinguish two keys.
    internal Func<@unsafe.Pointer, uintptr, uintptr> keyHash
    {
        get => storeOf(ref this).hashHook!;
        set => installKeyHash(ref this, value);
    }

    // The map's hash salt. Go draws it from math/rand at construction and feeds it to keyHash as the
    // second argument; here it serves the same one purpose and nothing else, since the store's own
    // hashing goes through EqualityComparer<K>.Default and takes no salt of ours.
    internal uintptr seed => storeOf(ref this).seed;
}

// The map's backing store. Named as a type of its own so that the [GoType]-generated members of
// HashTrieMap (its field-wise constructor, its field-reference accessor) never restate
// ConcurrentDictionary<K, V>: the converter renders Go's `comparable` constraint as `new()`, which
// carries no C# `notnull` signal, and generated code cannot carry the suppression that vacuous
// mismatch would need (a Go map key is never a null reference).
#pragma warning disable CS8714
internal sealed class mapStore<K, V> : ConcurrentDictionary<K, V>
{
    // THE NIL KEY. Go admits a nil interface as a map key: `HashTrieMap[K, V comparable]`
    // instantiated at an interface K hashes and compares nil exactly like any other value, and
    // `unique.Make[testEface](nil)` — a row of Go's own unique suite — depends on it.
    // ConcurrentDictionary refuses it outright, and refuses it EARLY: every accessor runs its own
    // `if (key is null) ThrowKeyNullException()` before the comparer is ever consulted, so no
    // IEqualityComparer<K> can rescue the key. Substituting a sentinel is not available either — a
    // sentinel has to BE a K, and no object implements an arbitrary named Go interface.
    //
    // So the nil key gets one dedicated slot beside the dictionary. A null holder means ABSENT and a
    // non-null one carries the value, which puts presence in the reference itself: Interlocked over
    // that one field gives the nil key the same publish/retract atomicity TryAdd/TryRemove give every
    // other key, with no lock and no second dictionary. Inert for a value-type K, where null never
    // arrives and the JIT drops the branch.
    internal nilEntry<V>? nilKey;

    // The salt Go's NewHashTrieMap draws from math/rand — genuine here too, and per store. Its only
    // consumer is an installed hash hook's second argument (see HashTrieMap.keyHash): the dictionary
    // itself hashes through its comparer and never sees it.
    internal readonly uintptr seed;

    // The installed whitebox hash hook, or null in the normal case, where the store hashes through
    // EqualityComparer<K>.Default. Held so HashTrieMap.keyHash can read back what was written, and
    // so a store rebuilt for any other reason can carry the hook across.
    internal readonly Func<@unsafe.Pointer, uintptr, uintptr>? hashHook;

    internal mapStore() : this(newSeed(), null) { }

    // A hooked store is built through hookedHash; an unhooked one passes null and gets
    // ConcurrentDictionary's default comparer, which keeps the fast path — the one unique and
    // net/netip actually run on — free of the seam entirely.
    internal mapStore(uintptr seed, Func<@unsafe.Pointer, uintptr, uintptr>? hook)
        : base(hook is null ? null : new hookedHash<K>(hook, seed))
    {
        this.seed = seed;
        this.hashHook = hook;
    }
}

// The comparer an installed hash hook is honored through. Equality is untouched — Go's test replaces
// keyHash alone — so membership still answers exactly as it did; only bucket placement moves, to
// wherever the hook says.
internal sealed class hookedHash<K> : IEqualityComparer<K>
{
    private readonly Func<@unsafe.Pointer, uintptr, uintptr> hook;
    private readonly uintptr seed;

    internal hookedHash(Func<@unsafe.Pointer, uintptr, uintptr> hook, uintptr seed)
    {
        this.hook = hook;
        this.seed = seed;
    }

    public bool Equals(K? x, K? y)
    {
        return EqualityComparer<K>.Default.Equals(x, y);
    }

    // The hook's answer IS the hash code. The width narrows — Go's uintptr is 64-bit here and a
    // .NET hash code is 32 — which loses nothing a hash cares about: a narrowing of the hash
    // function is still a hash function, and the collision behavior the test forces survives it
    // exactly.
    public int GetHashCode(K obj)
    {
        return unchecked((int)(uint)hook(noAddress, seed).Value);
    }
}

// Presence-carrying holder for the nil key's value — see mapStore.nilKey. A plain field could not
// serve: V may be a value type, so "absent" would be indistinguishable from a stored zero, and a
// separate bool could not be published with it in one atomic step.
internal sealed class nilEntry<V>
{
    internal readonly V value;

    internal nilEntry(V value)
    {
        this.value = value;
    }
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
    mapStore<K, V> store = storeOf(ref ht);
    // The nil key lives beside the dictionary, never in it (see mapStore.nilKey).
    if (key is null) {
        nilEntry<V>? entry = Volatile.Read(ref store.nilKey);
        return entry is null ? (@new<V>().ValueSlot, false) : (entry.value, true);
    }
    if (store.TryGetValue(key, out V? value)) {
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
    // The nil key's one-shot publish is a single CAS, and it reports the same winner/loser split the
    // TryAdd loop below does: the thread whose exchange observed no holder stored, everyone else
    // loaded (see mapStore.nilKey).
    if (key is null) {
        nilEntry<V> candidate = new(value);
        nilEntry<V>? existingEntry = Interlocked.CompareExchange(ref store.nilKey, candidate, null);
        return existingEntry is null ? (value, false) : (existingEntry.value, true);
    }
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
    // The nil key's retraction is the same compare-and-remove, spelled against its own slot: the CAS
    // succeeds only if the holder we compared is still the published one, so a racing LoadOrStore
    // cannot have its value deleted out from under it (see mapStore.nilKey). Key-found-first and the
    // panic order below hold here too.
    if (key is null) {
        nilEntry<V>? entry = Volatile.Read(ref store.nilKey);
        if (entry is null) {
            return false;
        }
        mustBeComparable(old);
        return EqualityComparer<V>.Default.Equals(entry.value, old) &&
               ReferenceEquals(Interlocked.CompareExchange(ref store.nilKey, null, entry), entry);
    }
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
        // The nil key is not in the dictionary, so the enumeration below cannot reach it — yield it
        // first, under the key it actually has (see mapStore.nilKey). Reading the holder once keeps
        // the walk weakly consistent in the same way the dictionary's own enumerator is: a
        // concurrently published nil entry may or may not be seen, and one deleted mid-walk is
        // yielded at most once. unique's cleanup pass relies on visiting it at all — without this,
        // a dead weak pointer under the nil key could never be reclaimed.
        nilEntry<V>? entry = Volatile.Read(ref store.nilKey);
        if (entry is not null && !yield(default!, entry.value)) {
            return;
        }
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

// The "no address" the hash hook is handed in place of Go's `unsafe.Pointer(&key)` — the nil
// pointer, stated once. Go passes the address of the key's bytes; there is no such address here, and
// this file's whole argument is that inventing one would be worse than admitting there is none (see
// HashTrieMap.keyHash).
private static readonly @unsafe.Pointer noAddress = new(nil);

// A real per-store salt. The hook's second argument is the one half of its contract that CAN be
// honored, so it is honored with a genuine random value rather than a constant.
private static uintptr newSeed()
{
    return new uintptr(unchecked((nuint)System.Random.Shared.NextInt64()));
}

// Installing a hook REBUILDS the store behind it. ConcurrentDictionary fixes its comparer at
// construction and caches each entry's hash code in its nodes, so swapping the hash of a live
// dictionary in place would strand every entry already in it. Go's plain field write has no such
// constraint — its next Load simply hashes differently — so the entries are carried across here to
// reach the same end state. TestHashTrieMapBadHash writes the hook on an empty map, where the copy
// moves nothing; a populated map is handled anyway rather than left as a trap.
//
// The publish is a single reference write, so a concurrent reader sees either the whole old store or
// the whole new one. It is NOT atomic with respect to the copy — an entry stored by another thread
// mid-rebuild can be lost — and Go's field write is no better: both expect the hook to be installed
// before the map is shared, which is what the test does.
private static void installKeyHash<K, V>(ref HashTrieMap<K, V> ht, Func<@unsafe.Pointer, uintptr, uintptr> hook)
{
    mapStore<K, V> current = storeOf(ref ht);
    mapStore<K, V> replacement = new(current.seed, hook);

    replacement.nilKey = Volatile.Read(ref current.nilKey);

    foreach (KeyValuePair<K, V> pair in current) {
        replacement.TryAdd(pair.Key, pair.Value);
    }

    Volatile.Write(ref ht.store, replacement);
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
