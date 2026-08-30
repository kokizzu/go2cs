// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// go2cs HAND-OWNED WHITEBOX SURFACE (companion to hashtriemap.cs). Nothing here is part of the
// implementation, is reachable from it, or is constructed anywhere in the corpus. It exists for one
// reason: `hashtriemap_test.go` ends with two DEBUG helpers, `dumpMap` and `dumpNode`, which print
// the trie's internal node graph. No `Test` function calls either one — Go compiles them only
// because a package's files must type-check as a whole — but C# is no different, and their
// SIGNATURE alone (`n *node[K, V]`) is a declaration-phase reference. Without a `node` type the
// converted suite does not compile at all, and CS0426 at that signature suppresses body binding for
// the whole compilation, so all eighteen subtests are lost to two functions nobody runs.
//
// WHAT THIS FILE IS. The three node shapes below are the CONVERTER'S OWN declarations, copied
// verbatim from the hashtriemap.cs.auto review sibling beside this file — Go's `node`, `entry` and
// `indirect` as go2cs renders them, unedited. `entry()` and `indirect()` are likewise verbatim. So
// the shapes are not invented: they are exactly what a literal conversion of hashtriemap.go
// declares, and a reader diffing this file against the sibling finds one divergence, in one token,
// for a reason the ⚠ below states.
//
// WHAT THIS FILE IS NOT. There is no trie here and this does not pretend otherwise. Not one of these
// types is ever instantiated: hashtriemap.cs keeps its entries in a ConcurrentDictionary and has no
// nodes, which is the whole point of the replacement (see that file's header for why the literal
// conversion compiles and can never run). The single door from the map into this graph — Go's
// `HashTrieMap.root` — is therefore NOT a field holding a permanent nil, which would let dumpMap
// walk into a null dereference and read like a defect. It is a property that THROWS, naming the
// reason, so the one way to reach this surface fails immediately and says what is true. Every
// declaration below is downstream of that door.
//
// This is the milestone-stub position, applied to a case where the stubbed thing is not merely
// unimplementable but genuinely dead: a shape that compiles and refuses to run is honest, whereas a
// shape that compiles and answers is the fake-but-plausible move hashtriemap.cs's header forbids.
// The moment `internal/concurrent` acquires a real trie again — or Go retires these two helpers —
// this file goes away whole, and nothing in the package changes.

// ⚠ ONE spelling differs from the sibling, and only because of where these using directives sit.
// The sibling declares them INSIDE `namespace go.@internal;`, where a `using sync = sync_package;`
// alias is found before the enclosing `go` namespace's nested `go.sync` namespace. A [module:]
// attribute must precede every namespace declaration, so this file's usings are at compilation-unit
// scope, where that lookup order reverses and `sync.Mutex` binds to the NAMESPACE (CS0234). The
// field below therefore names go.sync_package.Mutex outright; the type is identical.
using go.golib;
using atomic = go.sync.atomic_package;

[module: go.GoManualConversion]

namespace go.@internal;

partial class concurrent_package {

partial struct HashTrieMap<K, V>
{
    // THE DOOR, and the only one. Go's `root *indirect[K, V]` is the trie's head; this
    // implementation has no trie, so there is no head to hand back and no honest value to invent.
    // `dumpMap` reads this on its first line and dies here — which is the correct outcome for a
    // debug printer aimed at a representation that does not exist.
    internal ж<Δindirect<K, V>> root =>
        throw panic((@string)"internal/concurrent: HashTrieMap has no trie in this implementation — " +
                             "the entries live in a ConcurrentDictionary and there is no root node to walk");
}

internal static UntypedInt nChildrenLog2 => 4;
internal static UntypedInt nChildren => /* 1 << nChildrenLog2 */ 16;
internal static UntypedInt nChildrenMask => /* nChildren - 1 */ 15;

// indirect is an internal node in the hash-trie.
[GoType] [GoValueClone("children")] partial struct Δindirect<K, V> {
    internal partial ref node<K, V> node { get; }
    internal atomic.Bool dead;
    internal go.sync_package.Mutex mu; // Protects mutation to children and any children that are entry nodes.
    internal ж<Δindirect<K, V>> parent;
    internal array<atomic.Pointer<node<K, V>>> children = new(nChildren);
}

// entry is a leaf node in the hash-trie.
[GoType] partial struct Δentry<K, V> {
    internal partial ref node<K, V> node { get; }
    internal atomic.Pointer<Δentry<K, V>> overflow; // Overflow for hash collisions.
    internal K key;
    internal V value;
}

// node is the header for a node. It's polymorphic and
// is actually either an entry or an indirect.
[GoType] partial struct node<K, V> {
    internal bool isEntry;
}

internal static ж<Δentry<K, V>> entry<K, V>(this ж<node<K, V>> Ꮡn) {
    ref var n = ref Ꮡn.DerefOrNull();

    if (!n.isEntry) {
        throw panic("called entry on non-entry node");
    }
    return Ꮡn.Reinterpret<node<K, V>, Δentry<K, V>>();
}

internal static ж<Δindirect<K, V>> indirect<K, V>(this ж<node<K, V>> Ꮡn) {
    ref var n = ref Ꮡn.DerefOrNull();

    if (n.isEntry) {
        throw panic("called indirect on entry node");
    }
    return Ꮡn.Reinterpret<node<K, V>, Δindirect<K, V>>();
}

} // end concurrent_package
