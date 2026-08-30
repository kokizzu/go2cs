// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using fmt = fmt_package;
using math = math_package;
using Δruntime = runtime_package;
using strconv = strconv_package;
using strings = strings_package;
using sync = sync_package;
using testing = testing_package;
using @unsafe = unsafe_package;
using atomic = go.sync.atomic_package;
using go.sync;
using io = io_package;
using static go.@internal.concurrent_package;

partial class concurrent_internal_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸmath() {
    builtin.initPackage(typeof(math_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸruntime() {
    builtin.initPackage(typeof(runtime_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸstrconv() {
    builtin.initPackage(typeof(strconv_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸstrings() {
    builtin.initPackage(typeof(strings_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsync() {
    builtin.initPackage(typeof(sync_package));
}

public static void TestHashTrieMap(ж<testing.T> Ꮡt) {
    testHashTrieMap(Ꮡt, () => NewHashTrieMap<@string, nint>());
}

public static void TestHashTrieMapBadHash(ж<testing.T> Ꮡt) {
    testHashTrieMap(Ꮡt, () => {
        // Stub out the good hash function with a terrible one.
        // Everything should still work as expected.
        var m = NewHashTrieMap<@string, nint>();
        m.Value.keyHash = uintptr (@unsafe.Pointer _Δp0, uintptr _Δp1) => (uintptr)(0);
        return m;
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string loadEmptyˢ = "LoadEmpty"u8;
internal static readonly @string loadOrStoreˢ = "LoadOrStore"u8;
internal static readonly @string compareAndDeleteAllˢ = "CompareAndDeleteAll"u8;
internal static readonly @string compareAndDeleteOneˢ = "CompareAndDeleteOne"u8;
internal static readonly @string deleteMultipleˢ = "DeleteMultiple"u8;
internal static readonly @string allˢ = "All"u8;
internal static readonly @string allDeleteˢ = "AllDelete"u8;

internal static void testHashTrieMap(ж<testing.T> Ꮡt, Func<ж<global::go.@internal.concurrent_package.HashTrieMap<@string, nint>>> newMap) {
    Ꮡt.Run(loadEmptyˢ, (ж<testing.T> tΔ1) => {
        var m = newMap();
        foreach (var (_, s) in testData) {
            var (ᴛ1, ᴛ2) = m.Load(s);
            expectMissing(tΔ1, s, (nint)(0))(ᴛ1, ᴛ2);
        }
    });
    Ꮡt.Run(loadOrStoreˢ, (ж<testing.T> tΔ2) => {
        var m = newMap();
        foreach (var (i, s) in testData) {
            var (ᴛ3, ᴛ4) = m.Load(s);
            expectMissing(tΔ2, s, (nint)(0))(ᴛ3, ᴛ4);
            var (ᴛ5, ᴛ6) = m.LoadOrStore(s, i);
            expectStored(tΔ2, s, i)(ᴛ5, ᴛ6);
            var (ᴛ7, ᴛ8) = m.Load(s);
            expectPresent(tΔ2, s, i)(ᴛ7, ᴛ8);
            var (ᴛ9, ᴛ10) = m.LoadOrStore(s, 0);
            expectLoaded(tΔ2, s, i)(ᴛ9, ᴛ10);
        }
        foreach (var (i, s) in testData) {
            var (ᴛ11, ᴛ12) = m.Load(s);
            expectPresent(tΔ2, s, i)(ᴛ11, ᴛ12);
            var (ᴛ13, ᴛ14) = m.LoadOrStore(s, 0);
            expectLoaded(tΔ2, s, i)(ᴛ13, ᴛ14);
        }
    });
    Ꮡt.Run(compareAndDeleteAllˢ, (ж<testing.T> tΔ3) => {
        var m = newMap();
        foreach (var _ᴛ1 in range(3)) {
            foreach (var (i, s) in testData) {
                var (ᴛ15, ᴛ16) = m.Load(s);
                expectMissing(tΔ3, s, (nint)(0))(ᴛ15, ᴛ16);
                var (ᴛ17, ᴛ18) = m.LoadOrStore(s, i);
                expectStored(tΔ3, s, i)(ᴛ17, ᴛ18);
                var (ᴛ19, ᴛ20) = m.Load(s);
                expectPresent(tΔ3, s, i)(ᴛ19, ᴛ20);
                var (ᴛ21, ᴛ22) = m.LoadOrStore(s, 0);
                expectLoaded(tΔ3, s, i)(ᴛ21, ᴛ22);
            }
            foreach (var (i, s) in testData) {
                var (ᴛ23, ᴛ24) = m.Load(s);
                expectPresent(tΔ3, s, i)(ᴛ23, ᴛ24);
                expectNotDeleted(tΔ3, s, math.MaxInt)(m.CompareAndDelete(s, math.MaxInt));
                expectDeleted(tΔ3, s, i)(m.CompareAndDelete(s, i));
                expectNotDeleted(tΔ3, s, i)(m.CompareAndDelete(s, i));
                var (ᴛ25, ᴛ26) = m.Load(s);
                expectMissing(tΔ3, s, (nint)(0))(ᴛ25, ᴛ26);
            }
            foreach (var (_, s) in testData) {
                var (ᴛ27, ᴛ28) = m.Load(s);
                expectMissing(tΔ3, s, (nint)(0))(ᴛ27, ᴛ28);
            }
        }
    });
    Ꮡt.Run(compareAndDeleteOneˢ, (ж<testing.T> tΔ4) => {
        var m = newMap();
        foreach (var (i, s) in testData) {
            var (ᴛ29, ᴛ30) = m.Load(s);
            expectMissing(tΔ4, s, (nint)(0))(ᴛ29, ᴛ30);
            var (ᴛ31, ᴛ32) = m.LoadOrStore(s, i);
            expectStored(tΔ4, s, i)(ᴛ31, ᴛ32);
            var (ᴛ33, ᴛ34) = m.Load(s);
            expectPresent(tΔ4, s, i)(ᴛ33, ᴛ34);
            var (ᴛ35, ᴛ36) = m.LoadOrStore(s, 0);
            expectLoaded(tΔ4, s, i)(ᴛ35, ᴛ36);
        }
        expectNotDeleted(tΔ4, testData[15], math.MaxInt)(m.CompareAndDelete(testData[15], math.MaxInt));
        expectDeleted(tΔ4, testData[15], 15)(m.CompareAndDelete(testData[15], 15));
        expectNotDeleted(tΔ4, testData[15], 15)(m.CompareAndDelete(testData[15], 15));
        foreach (var (i, s) in testData) {
            if (i == 15){
                var (ᴛ37, ᴛ38) = m.Load(s);
                expectMissing(tΔ4, s, (nint)(0))(ᴛ37, ᴛ38);
            } else {
                var (ᴛ39, ᴛ40) = m.Load(s);
                expectPresent(tΔ4, s, i)(ᴛ39, ᴛ40);
            }
        }
    });
    Ꮡt.Run(deleteMultipleˢ, (ж<testing.T> tΔ5) => {
        var m = newMap();
        foreach (var (i, s) in testData) {
            var (ᴛ41, ᴛ42) = m.Load(s);
            expectMissing(tΔ5, s, (nint)(0))(ᴛ41, ᴛ42);
            var (ᴛ43, ᴛ44) = m.LoadOrStore(s, i);
            expectStored(tΔ5, s, i)(ᴛ43, ᴛ44);
            var (ᴛ45, ᴛ46) = m.Load(s);
            expectPresent(tΔ5, s, i)(ᴛ45, ᴛ46);
            var (ᴛ47, ᴛ48) = m.LoadOrStore(s, 0);
            expectLoaded(tΔ5, s, i)(ᴛ47, ᴛ48);
        }
        foreach (var (_, i) in new nint[]{1, 105, 6, 85}.slice()) {
            expectNotDeleted(tΔ5, testData[i], math.MaxInt)(m.CompareAndDelete(testData[i], math.MaxInt));
            expectDeleted(tΔ5, testData[i], i)(m.CompareAndDelete(testData[i], i));
            expectNotDeleted(tΔ5, testData[i], i)(m.CompareAndDelete(testData[i], i));
        }
        foreach (var (i, s) in testData) {
            if (i == 1 || i == 105 || i == 6 || i == 85){
                var (ᴛ49, ᴛ50) = m.Load(s);
                expectMissing(tΔ5, s, (nint)(0))(ᴛ49, ᴛ50);
            } else {
                var (ᴛ51, ᴛ52) = m.Load(s);
                expectPresent(tΔ5, s, i)(ᴛ51, ᴛ52);
            }
        }
    });
    Ꮡt.Run(allˢ, (ж<testing.T> tΔ6) => {
        var m = newMap();
        testAll(tΔ6, m, testDataMap(testData[..]), (@string _Δp0, nint _Δp1) => true);
    });
    Ꮡt.Run(allDeleteˢ, (ж<testing.T> tΔ7) => {
        var m = newMap();
        var mʗ1 = m;
        testAll(tΔ7, m, testDataMap(testData[..]), (@string s, nint i) => {
            expectDeleted(tΔ7, s, i)(mʗ1.CompareAndDelete(s, i));
            return true;
        });
        foreach (var (_, s) in testData) {
            var (ᴛ53, ᴛ54) = m.Load(s);
            expectMissing(tΔ7, s, (nint)(0))(ᴛ53, ᴛ54);
        }
    });
    Ꮡt.Run("ConcurrentLifecycleUnsharedKeys"u8, (ж<testing.T> tΔ8) => {
        var m = newMap();
        nint gmp = Δruntime.GOMAXPROCS(-1);
        ref var wg = ref heap(new sync.WaitGroup(), out var Ꮡwg);
        foreach (var i in range(gmp)) {
            Ꮡwg.Add(1);
            var mʗ2 = m;
            goǃ((nint id) => {
                GoFrame ᒐ = default;
                try {
                    defer(Ꮡwg.Done, ref ᒐ);
                    @string makeKey(@string s) => s + "-"u8 + strconv.Itoa(id);
                    foreach (var (_, s) in testData) {
                        @string key = makeKey(s);
                        var (ᴛ55, ᴛ56) = mʗ2.Load(key);
                        expectMissing(tΔ8, key, (nint)(0))(ᴛ55, ᴛ56);
                        var (ᴛ57, ᴛ58) = mʗ2.LoadOrStore(key, id);
                        expectStored(tΔ8, key, id)(ᴛ57, ᴛ58);
                        var (ᴛ59, ᴛ60) = mʗ2.Load(key);
                        expectPresent(tΔ8, key, id)(ᴛ59, ᴛ60);
                        var (ᴛ61, ᴛ62) = mʗ2.LoadOrStore(key, 0);
                        expectLoaded(tΔ8, key, id)(ᴛ61, ᴛ62);
                    }
                    foreach (var (_, s) in testData) {
                        @string key = makeKey(s);
                        var (ᴛ63, ᴛ64) = mʗ2.Load(key);
                        expectPresent(tΔ8, key, id)(ᴛ63, ᴛ64);
                        expectDeleted(tΔ8, key, id)(mʗ2.CompareAndDelete(key, id));
                        var (ᴛ65, ᴛ66) = mʗ2.Load(key);
                        expectMissing(tΔ8, key, (nint)(0))(ᴛ65, ᴛ66);
                    }
                    foreach (var (_, s) in testData) {
                        @string key = makeKey(s);
                        var (ᴛ67, ᴛ68) = mʗ2.Load(key);
                        expectMissing(tΔ8, key, (nint)(0))(ᴛ67, ᴛ68);
                    }
                }
                catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
                finally { ᒐ.Run(); }
            }, i);
        }
        Ꮡwg.Wait();
    });
    Ꮡt.Run("ConcurrentDeleteSharedKeys"u8, (ж<testing.T> tΔ9) => {
        var m = newMap();
        // Load up the map.
        foreach (var (i, s) in testData) {
            var (ᴛ69, ᴛ70) = m.Load(s);
            expectMissing(tΔ9, s, (nint)(0))(ᴛ69, ᴛ70);
            var (ᴛ71, ᴛ72) = m.LoadOrStore(s, i);
            expectStored(tΔ9, s, i)(ᴛ71, ᴛ72);
        }
        nint gmp = Δruntime.GOMAXPROCS(-1);
        ref var wg = ref heap(new sync.WaitGroup(), out var Ꮡwg);
        foreach (var i in range(gmp)) {
            Ꮡwg.Add(1);
            var mʗ3 = m;
            goǃ((nint id) => {
                GoFrame ᒐ = default;
                try {
                    defer(Ꮡwg.Done, ref ᒐ);
                    foreach (var (iΔ1, s) in testData) {
                        expectNotDeleted(tΔ9, s, math.MaxInt)(mʗ3.CompareAndDelete(s, math.MaxInt));
                        mʗ3.CompareAndDelete(s, iΔ1);
                        var (ᴛ73, ᴛ74) = mʗ3.Load(s);
                        expectMissing(tΔ9, s, (nint)(0))(ᴛ73, ᴛ74);
                    }
                    foreach (var (_, s) in testData) {
                        var (ᴛ75, ᴛ76) = mʗ3.Load(s);
                        expectMissing(tΔ9, s, (nint)(0))(ᴛ75, ᴛ76);
                    }
                }
                catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
                finally { ᒐ.Run(); }
            }, i);
        }
        Ꮡwg.Wait();
    });
}

internal static void testAll<K, V>(ж<testing.T> Ꮡt, ж<global::go.@internal.concurrent_package.HashTrieMap<K, V>> Ꮡm, map<K, V> testData, Func<K, V, bool> yield) {
    foreach (var (k, v) in testData) {
        var (ᴛ77, ᴛ78) = Ꮡm.LoadOrStore(k, v);
        expectStored(Ꮡt, k, v)(ᴛ77, ᴛ78);
    }
    var visited = new map<K, nint>();
    var testDataʗ1 = testData;
    var visitedʗ1 = visited;
    Ꮡm.All()((K key, V got) => {
        var (want, ok) = testDataʗ1[key, ꟷ];
        if (!ok) {
            Ꮡt.Errorf("unexpected key %v in map"u8, key);
            return false;
        }
        if (!AreEqual(got, want)) {
            Ꮡt.Errorf("expected key %v to have value %v, got %v"u8, key, want, got);
            return false;
        }
        visitedʗ1[key]++;
        return yield(key, got);
    });
    foreach (var (key, n) in visited) {
        if (n > 1) {
            Ꮡt.Errorf("visited key %v more than once"u8, key);
        }
    }
}

internal static Action<V, bool> expectPresent<K, V>(ж<testing.T> Ꮡt, K key, V want) {
    Ꮡt.Helper();
    return (V got, bool ok) => {
        Ꮡt.Helper();
        if (!ok) {
            Ꮡt.Errorf("expected key %v to be present in map"u8, key);
        }
        if (ok && !AreEqual(got, want)) {
            Ꮡt.Errorf("expected key %v to have value %v, got %v"u8, key, want, got);
        }
    };
}

internal static Action<V, bool> expectMissing<K, V>(ж<testing.T> Ꮡt, K key, V want) {
    Ꮡt.Helper();
    if (!AreEqual(want, @new<V>().ValueSlot)) {
        // This is awkward, but the want argument is necessary to smooth over type inference.
        // Just make sure the want argument always looks the same.
        throw panic("expectMissing must always have a zero value variable");
    }
    return (V got, bool ok) => {
        Ꮡt.Helper();
        if (ok) {
            Ꮡt.Errorf("expected key %v to be missing from map, got value %v"u8, key, got);
        }
        if (!ok && !AreEqual(got, want)) {
            Ꮡt.Errorf("expected missing key %v to be paired with the zero value; got %v"u8, key, got);
        }
    };
}

internal static Action<V, bool> expectLoaded<K, V>(ж<testing.T> Ꮡt, K key, V want) {
    Ꮡt.Helper();
    return (V got, bool loaded) => {
        Ꮡt.Helper();
        if (!loaded) {
            Ꮡt.Errorf("expected key %v to have been loaded, not stored"u8, key);
        }
        if (!AreEqual(got, want)) {
            Ꮡt.Errorf("expected key %v to have value %v, got %v"u8, key, want, got);
        }
    };
}

internal static Action<V, bool> expectStored<K, V>(ж<testing.T> Ꮡt, K key, V want) {
    Ꮡt.Helper();
    return (V got, bool loaded) => {
        Ꮡt.Helper();
        if (loaded) {
            Ꮡt.Errorf("expected inserted key %v to have been stored, not loaded"u8, key);
        }
        if (!AreEqual(got, want)) {
            Ꮡt.Errorf("expected inserted key %v to have value %v, got %v"u8, key, want, got);
        }
    };
}

internal static Action<bool> expectDeleted<K, V>(ж<testing.T> Ꮡt, K key, V old) {
    Ꮡt.Helper();
    return (bool deleted) => {
        Ꮡt.Helper();
        if (!deleted) {
            Ꮡt.Errorf("expected key %v with value %v to be in map and deleted"u8, key, old);
        }
    };
}

internal static Action<bool> expectNotDeleted<K, V>(ж<testing.T> Ꮡt, K key, V old) {
    Ꮡt.Helper();
    return (bool deleted) => {
        Ꮡt.Helper();
        if (deleted) {
            Ꮡt.Errorf("expected key %v with value %v to not be in map and thus not deleted"u8, key, old);
        }
    };
}

internal static map<@string, nint> testDataMap(slice<@string> data) {
    var m = new map<@string, nint>();
    foreach (var (i, s) in data) {
        m[s] = i;
    }
    return m;
}

internal static array<@string> testDataSmall = new(8);
internal static array<@string> testData = new(128);
internal static array<@string> testDataLarge = new(131072);

[GoInit] internal static void init() {
    foreach (var (i, _) in testDataSmall) {
        testDataSmall[i] = fmt.Sprintf("%b"u8, i);
    }
    foreach (var (i, _) in testData) {
        testData[i] = fmt.Sprintf("%b"u8, i);
    }
    foreach (var (i, _) in testDataLarge) {
        testDataLarge[i] = fmt.Sprintf("%b"u8, i);
    }
}

internal static void dumpMap<K, V>(ж<global::go.@internal.concurrent_package.HashTrieMap<K, V>> Ꮡht) {
    ref var ht = ref Ꮡht.DerefOrNull();

    dumpNode(Ꮡht, ht.root.of(global::go.@internal.concurrent_package.Δindirect<K, V>.Ꮡnode), 0);
}

internal static void dumpNode<K, V>(ж<global::go.@internal.concurrent_package.HashTrieMap<K, V>> Ꮡht, ж<global::go.@internal.concurrent_package.node<K, V>> Ꮡn, nint depth) {
    ref var ht = ref Ꮡht.DerefOrNull();
    ref var n = ref Ꮡn.DerefOrNull();

    ref var sb = ref heap(new strings.Builder(), out var Ꮡsb);
    foreach (var _ᴛ1 in range(depth)) {
        fmt.Fprintf(new concurrent_internal_test_package.strings_BuilderжWriter(Ꮡsb), "\t"u8);
    }
    @string prefix = sb.String();
    if (n.isEntry) {
        var e = Ꮡn.entry();
        while (e != nil) {
            fmt.Printf("%s%p [Entry Key=%v Value=%v Overflow=%p, Hash=%016x]\n"u8, prefix, e.OrTypedNil(), (~e).key, (~e).value, e.of(global::go.@internal.concurrent_package.Δentry<K, V>.Ꮡoverflow).Load().OrTypedNil(), ht.keyHash(new @unsafe.Pointer(e.of(global::go.@internal.concurrent_package.Δentry<K, V>.Ꮡkey)), ht.seed));
            e = e.of(global::go.@internal.concurrent_package.Δentry<K, V>.Ꮡoverflow).Load();
        }
        return;
    }
    var i = Ꮡn.indirect();
    fmt.Printf("%s%p [Indirect Parent=%p Dead=%t Children=["u8, prefix, i.OrTypedNil(), (~i).parent.OrTypedNil(), i.of(global::go.@internal.concurrent_package.Δindirect<K, V>.Ꮡdead).Load());
    foreach (var (j, _) in (~i).children) {
        var c = i.at(global::go.@internal.concurrent_package.Δindirect<K, V>.Ꮡchildren, j).Load();
        fmt.Printf("%p"u8, c.OrTypedNil());
        if (j != len((~i).children) - 1) {
            fmt.Printf(", "u8);
        }
    }
    fmt.Printf("]]\n"u8);
    foreach (var (j, _) in (~i).children) {
        var c = i.at(global::go.@internal.concurrent_package.Δindirect<K, V>.Ꮡchildren, j).Load();
        if (c != nil) {
            dumpNode(Ꮡht, c, depth + 1);
        }
    }
}

} // end concurrent_internal_test_package
