// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using fmt = fmt_package;
using reflect = reflect_package;
using Δsync = go.sync_package;
using atomic = go.sync.atomic_package;
using Δtesting = testing_package;
using go;
using go.sync;

partial class sync_test_package {

[GoType] partial struct bench {
    internal Action<ж<Δtesting.B>, mapInterface> setup;
    internal Action<ж<Δtesting.B>, ж<Δtesting.PB>, nint, mapInterface> perG;
}

internal static void benchMap(ж<Δtesting.B> Ꮡb, bench bench) {
    foreach (var (_, vᴛ1) in new mapInterface[]{new DeepCopyMapжmapInterface(Ꮡ(new DeepCopyMap(nil))), new RWMutexMapжmapInterface(Ꮡ(new RWMutexMap(nil))), new Δsync.MapжmapInterface(Ꮡ(new Δsync.Map(nil)))}.array()) {
        var m = vᴛ1;

        var benchʗ1 = bench;
        var mʗ1 = m;
        Ꮡb.Run(fmt.Sprintf("%T"u8, m), (ж<Δtesting.B> bΔ1) => {
            mʗ1 = reflect.New(reflect.TypeOf(mʗ1).Elem()).Interface()._<mapInterface>();
            if (benchʗ1.setup != default!) {
                benchʗ1.setup(bΔ1, mʗ1);
            }
            bΔ1.ResetTimer();
            ref var i = ref heap(new int64(), out var Ꮡi);
            var benchʗ2 = benchʗ1;
            var mʗ2 = mʗ1;
            bΔ1.RunParallel((ж<Δtesting.PB> pb) => {
                nint id = (nint)(atomic.AddInt64(Ꮡi, 1) - 1);
                benchʗ2.perG(bΔ1, pb, id * (~bΔ1).N, mʗ2);
            });
        });
    }
}

public static void BenchmarkLoadMostlyHits(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1023;
    UntypedInt misses = 1;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.Load(i % (nint)(hits + misses));
            }
        }
    ));
}

public static void BenchmarkLoadMostlyMisses(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1;
    UntypedInt misses = 1023;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.Load(i % (nint)(hits + misses));
            }
        }
    ));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object deepCopyMapHasQuadraticˢ = (@string)"DeepCopyMap has quadratic running time."u8;

public static void BenchmarkLoadOrStoreBalanced(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 128;
    UntypedInt misses = 128;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> bΔ1, mapInterface m) => {
            {
                var (_, ok) = m._<ж<DeepCopyMap>>(ᐧ); if (ok) {
                    bΔ1.Skip(deepCopyMapHasQuadraticˢ);
                }
            }
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ2, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint j = i % (nint)(hits + misses);
                if (j < hits){
                    {
                        var (_, ok) = m.LoadOrStore(j, i); if (!ok) {
                            bΔ2.Fatalf("unexpected miss for %v"u8, j);
                        }
                    }
                } else {
                    {
                        var (v, loaded) = m.LoadOrStore(i, i); if (loaded) {
                            bΔ2.Fatalf("failed to store %v: existing value %v"u8, i, v);
                        }
                    }
                }
            }
        }
    ));
}

public static void BenchmarkLoadOrStoreUnique(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> bΔ1, mapInterface m) => {
            {
                var (_, ok) = m._<ж<DeepCopyMap>>(ᐧ); if (ok) {
                    bΔ1.Skip(deepCopyMapHasQuadraticˢ);
                }
            }
        },
        perG: (ж<Δtesting.B> bΔ2, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.LoadOrStore(i, i);
            }
        }
    ));
}

public static void BenchmarkLoadOrStoreCollision(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            m.LoadOrStore((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.LoadOrStore((nint)(0), (nint)(0));
            }
        }
    ));
}

public static void BenchmarkLoadAndDeleteBalanced(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 128;
    UntypedInt misses = 128;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> bΔ1, mapInterface m) => {
            {
                var (_, ok) = m._<ж<DeepCopyMap>>(ᐧ); if (ok) {
                    bΔ1.Skip(deepCopyMapHasQuadraticˢ);
                }
            }
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ2, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint j = i % (nint)(hits + misses);
                if (j < hits){
                    m.LoadAndDelete(j);
                } else {
                    m.LoadAndDelete(i);
                }
            }
        }
    ));
}

public static void BenchmarkLoadAndDeleteUnique(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> bΔ1, mapInterface m) => {
            {
                var (_, ok) = m._<ж<DeepCopyMap>>(ᐧ); if (ok) {
                    bΔ1.Skip(deepCopyMapHasQuadraticˢ);
                }
            }
        },
        perG: (ж<Δtesting.B> bΔ2, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.LoadAndDelete(i);
            }
        }
    ));
}

public static void BenchmarkLoadAndDeleteCollision(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _Δp0, mapInterface m) => {
            m.LoadOrStore((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                {
                    var (_, loaded) = m.LoadAndDelete((nint)(0)); if (loaded) {
                        m.Store((nint)(0), (nint)(0));
                    }
                }
            }
        }
    ));
}

public static void BenchmarkRange(ж<Δtesting.B> Ꮡb) {
    const nint mapSize = /* 1 << 10 */ 1024;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < mapSize; i++) {
                m.Store(i, i);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.Range((any _Δp0, any _Δp1) => true);
            }
        }
    ));
}

// BenchmarkAdversarialAlloc tests performance when we store a new value
// immediately whenever the map is promoted to clean and otherwise load a
// unique, missing key.
//
// This forces the Load calls to always acquire the map's mutex.
public static void BenchmarkAdversarialAlloc(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            int64 stores = default!;
            int64 loadsSinceStore = default!;
            for (; pb.Next(); i++) {
                m.Load(i);
                {
                    loadsSinceStore++; if (loadsSinceStore > stores) {
                        m.LoadOrStore(i, stores);
                        loadsSinceStore = 0;
                        stores++;
                    }
                }
            }
        }
    ));
}

// BenchmarkAdversarialDelete tests performance when we periodically delete
// one key and add a different one in a large map.
//
// This forces the Load calls to always acquire the map's mutex and periodically
// makes a full copy of the map despite changing only one entry.
public static void BenchmarkAdversarialDelete(ж<Δtesting.B> Ꮡb) {
    const nint mapSize = /* 1 << 10 */ 1024;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < mapSize; i++) {
                m.Store(i, i);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.Load(i);
                if (i % mapSize == 0) {
                    m.Range((any k, any _) => {
                        m.Delete(k);
                        return false;
                    });
                    m.Store(i, i);
                }
            }
        }
    ));
}

public static void BenchmarkDeleteCollision(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            m.LoadOrStore((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.Delete((nint)(0));
            }
        }
    ));
}

public static void BenchmarkSwapCollision(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            m.LoadOrStore((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.Swap((nint)(0), (nint)(0));
            }
        }
    ));
}

public static void BenchmarkSwapMostlyHits(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1023;
    UntypedInt misses = 1;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                if (i % (nint)(hits + misses) < hits){
                    nint v = i % (nint)(hits + misses);
                    m.Swap(v, v);
                } else {
                    m.Swap(i, i);
                    m.Delete(i);
                }
            }
        }
    ));
}

public static void BenchmarkSwapMostlyMisses(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1;
    UntypedInt misses = 1023;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                if (i % (nint)(hits + misses) < hits){
                    nint v = i % (nint)(hits + misses);
                    m.Swap(v, v);
                } else {
                    m.Swap(i, i);
                    m.Delete(i);
                }
            }
        }
    ));
}

public static void BenchmarkCompareAndSwapCollision(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            m.LoadOrStore((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            while (pb.Next()) {
                if (m.CompareAndSwap((nint)(0), (nint)(0), (nint)(42))) {
                    m.CompareAndSwap((nint)(0), (nint)(42), (nint)(0));
                }
            }
        }
    ));
}

public static void BenchmarkCompareAndSwapNoExistingKey(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                if (m.CompareAndSwap(i, (nint)(0), (nint)(0))) {
                    m.Delete(i);
                }
            }
        }
    ));
}

public static void BenchmarkCompareAndSwapValueNotEqual(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            m.Store((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                m.CompareAndSwap((nint)(0), (nint)(1), (nint)(2));
            }
        }
    ));
}

public static void BenchmarkCompareAndSwapMostlyHits(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1023;
    UntypedInt misses = 1;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> bΔ1, mapInterface m) => {
            {
                var (_, ok) = m._<ж<DeepCopyMap>>(ᐧ); if (ok) {
                    bΔ1.Skip(deepCopyMapHasQuadraticˢ);
                }
            }
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ2, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint v = i;
                if (i % (nint)(hits + misses) < hits) {
                    v = i % (nint)(hits + misses);
                }
                m.CompareAndSwap(v, v, v);
            }
        }
    ));
}

public static void BenchmarkCompareAndSwapMostlyMisses(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1;
    UntypedInt misses = 1023;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint v = i;
                if (i % (nint)(hits + misses) < hits) {
                    v = i % (nint)(hits + misses);
                }
                m.CompareAndSwap(v, v, v);
            }
        }
    ));
}

public static void BenchmarkCompareAndDeleteCollision(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            m.LoadOrStore((nint)(0), (nint)(0));
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                if (m.CompareAndDelete((nint)(0), (nint)(0))) {
                    m.Store((nint)(0), (nint)(0));
                }
            }
        }
    ));
}

public static void BenchmarkCompareAndDeleteMostlyHits(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1023;
    UntypedInt misses = 1;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> bΔ1, mapInterface m) => {
            {
                var (_, ok) = m._<ж<DeepCopyMap>>(ᐧ); if (ok) {
                    bΔ1.Skip(deepCopyMapHasQuadraticˢ);
                }
            }
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ2, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint v = i;
                if (i % (nint)(hits + misses) < hits) {
                    v = i % (nint)(hits + misses);
                }
                if (m.CompareAndDelete(v, v)) {
                    m.Store(v, v);
                }
            }
        }
    ));
}

public static void BenchmarkCompareAndDeleteMostlyMisses(ж<Δtesting.B> Ꮡb) {
    UntypedInt hits = 1;
    UntypedInt misses = 1023;
    benchMap(Ꮡb, new bench(
        setup: (ж<Δtesting.B> _, mapInterface m) => {
            for (nint i = 0; i < hits; i++) {
                m.LoadOrStore(i, i);
            }
            // Prime the map to get it into a steady state.
            for (nint i = 0; i < hits * 2; i++) {
                m.Load(i % (nint)hits);
            }
        },
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint v = i;
                if (i % (nint)(hits + misses) < hits) {
                    v = i % (nint)(hits + misses);
                }
                if (m.CompareAndDelete(v, v)) {
                    m.Store(v, v);
                }
            }
        }
    ));
}

public static void BenchmarkClear(ж<Δtesting.B> Ꮡb) {
    benchMap(Ꮡb, new bench(
        perG: (ж<Δtesting.B> bΔ1, ж<Δtesting.PB> pb, nint i, mapInterface m) => {
            for (; pb.Next(); i++) {
                nint k = i % 256;
                nint v = i % 256;
                m.Clear();
                m.Store(k, v);
            }
        }
    ));
}

} // end sync_test_package
