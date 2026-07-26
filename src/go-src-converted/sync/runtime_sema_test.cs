// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using Δruntime = runtime_package;
using static go.sync_package;
using Δtesting = testing_package;

partial class sync_test_package {

[GoLocalName("PaddedSem")] [GoType("dyn")] [GoValueClone("pad")] partial struct BenchmarkSemaUncontended_PaddedSem {
    internal uint32 sem;
    internal array<uint32> pad = new(32);
}

public static void BenchmarkSemaUncontended(ж<Δtesting.B> Ꮡb) {
    Ꮡb.RunParallel((ж<Δtesting.PB> pb) => {
        var sem = @new<BenchmarkSemaUncontended_PaddedSem>();
        while (pb.Next()) {
            Runtime_Semrelease(sem.of(BenchmarkSemaUncontended_PaddedSem.Ꮡsem), false, 0);
            Runtime_Semacquire(sem.of(BenchmarkSemaUncontended_PaddedSem.Ꮡsem));
        }
    });
}

internal static void benchmarkSema(ж<Δtesting.B> Ꮡb, bool block, bool work) => func((defer, recover) => {
    ref var b = ref Ꮡb.Value;

    if (b.N == 0) {
        return;
    }
    ref var sem = ref heap<uint32>(out var Ꮡsem);
    sem = (uint32)0;
    if (block) {
        var done = new channel<bool>(0);
        var doneʗ1 = done;
        goǃ(() => {
            for (nint p = 0; p < Δruntime.GOMAXPROCS(0) / 2; p++) {
                Runtime_Semacquire(Ꮡsem);
            }
            doneʗ1.ᐸꟷ(true);
        });
        var doneʗ2 = done;
        defer(() => {
            ᐸꟷ(doneʗ2);
        });
    }
    Ꮡb.RunParallel((ж<Δtesting.PB> pb) => {
        nint foo = 0;
        while (pb.Next()) {
            Runtime_Semrelease(Ꮡsem, false, 0);
            if (work) {
                for (nint i = 0; i < 100; i++) {
                    foo *= 2;
                    foo /= 2;
                }
            }
            Runtime_Semacquire(Ꮡsem);
        }
        _ = foo;
        Runtime_Semrelease(Ꮡsem, false, 0);
    });
});

public static void BenchmarkSemaSyntNonblock(ж<Δtesting.B> Ꮡb) {
    benchmarkSema(Ꮡb, false, false);
}

public static void BenchmarkSemaSyntBlock(ж<Δtesting.B> Ꮡb) {
    benchmarkSema(Ꮡb, true, false);
}

public static void BenchmarkSemaWorkNonblock(ж<Δtesting.B> Ꮡb) {
    benchmarkSema(Ꮡb, false, true);
}

public static void BenchmarkSemaWorkBlock(ж<Δtesting.B> Ꮡb) {
    benchmarkSema(Ꮡb, true, true);
}

} // end sync_test_package
