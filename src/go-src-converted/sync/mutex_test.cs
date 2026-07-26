// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// GOMAXPROCS=10 go test
namespace go;

using fmt = fmt_package;
using testenv = @internal.testenv_package;
using Δos = os_package;
using exec = go.os.exec_package;
using Δruntime = runtime_package;
using strings = strings_package;
using static go.sync_package;
using Δtesting = testing_package;
using time = time_package;
using @internal;
using go.os;
using Δsync = sync_package;

partial class sync_test_package {

public static void HammerSemaphore(ж<uint32> Ꮡs, nint loops, channel<bool> cdone) {
    for (nint i = 0; i < loops; i++) {
        Runtime_Semacquire(Ꮡs);
        Runtime_Semrelease(Ꮡs, false, 0);
    }
    cdone.ᐸꟷ(true);
}

public static void TestSemaphore(ж<Δtesting.T> Ꮡt) {
    var s = @new<uint32>();
    s.Value = 1;
    var c = new channel<bool>(0);
    for (nint i = 0; i < 10; i++) {
        goǃ(HammerSemaphore, s, (nint)(1000), c);
    }
    for (nint i = 0; i < 10; i++) {
        ᐸꟷ(c);
    }
}

public static void BenchmarkUncontendedSemaphore(ж<Δtesting.B> Ꮡb) {
    ref var b = ref Ꮡb.Value;

    var s = @new<uint32>();
    s.Value = 1;
    HammerSemaphore(s, b.N, new channel<bool>(2));
}

public static void BenchmarkContendedSemaphore(ж<Δtesting.B> Ꮡb) => func((defer, recover) => {
    ref var b = ref Ꮡb.Value;

    b.StopTimer();
    var s = @new<uint32>();
    s.Value = 1;
    var c = new channel<bool>(0);
    deferǃ(Δruntime.GOMAXPROCS, Δruntime.GOMAXPROCS(2), defer);
    b.StartTimer();
    goǃ(HammerSemaphore, s, Ꮡb.Value.N / 2, c);
    goǃ(HammerSemaphore, s, Ꮡb.Value.N / 2, c);
    ᐸꟷ(c);
    ᐸꟷ(c);
});

public static void HammerMutex(ж<Δsync.Mutex> Ꮡm, nint loops, channel<bool> cdone) {
    for (nint i = 0; i < loops; i++) {
        if (i % 3 == 0) {
            if (Ꮡm.TryLock()) {
                Ꮡm.Unlock();
            }
            continue;
        }
        Ꮡm.Lock();
        Ꮡm.Unlock();
    }
    cdone.ᐸꟷ(true);
}

public static void TestMutex(ж<Δtesting.T> Ꮡt) => func((defer, recover) => {
    {
        nint n = Δruntime.SetMutexProfileFraction(1); if (n != 0) {
            Ꮡt.Logf("got mutexrate %d expected 0"u8, n);
        }
    }
    deferǃ(Δruntime.SetMutexProfileFraction, (nint)(0), defer);
    var m = @new<Δsync.Mutex>();
    m.Lock();
    if (m.TryLock()) {
        Ꮡt.Fatalf("TryLock succeeded with mutex locked"u8);
    }
    m.Unlock();
    if (!m.TryLock()) {
        Ꮡt.Fatalf("TryLock failed with mutex unlocked"u8);
    }
    m.Unlock();
    var c = new channel<bool>(0);
    for (nint i = 0; i < 10; i++) {
        goǃ(HammerMutex, m, (nint)(1000), c);
    }
    for (nint i = 0; i < 10; i++) {
        ᐸꟷ(c);
    }
});


[GoType("dyn")] partial struct misuseTestsᴛ1 {
    internal @string name;
    internal Action f;
}
internal static slice<misuseTestsᴛ1> misuseTests = new misuseTestsᴛ1[]{
    new(
        "Mutex.Unlock"u8,
        () => {
            ref var mu = ref heap(new Δsync.Mutex(), out var Ꮡmu);
            Ꮡmu.Unlock();
        }
    ),
    new(
        "Mutex.Unlock2"u8,
        () => {
            ref var mu = ref heap(new Δsync.Mutex(), out var Ꮡmu);
            Ꮡmu.Lock();
            Ꮡmu.Unlock();
            Ꮡmu.Unlock();
        }
    ),
    new(
        "RWMutex.Unlock"u8,
        () => {
            ref var mu = ref heap(new Δsync.RWMutex(), out var Ꮡmu);
            Ꮡmu.Unlock();
        }
    ),
    new(
        "RWMutex.Unlock2"u8,
        () => {
            ref var mu = ref heap(new Δsync.RWMutex(), out var Ꮡmu);
            Ꮡmu.RLock();
            Ꮡmu.Unlock();
        }
    ),
    new(
        "RWMutex.Unlock3"u8,
        () => {
            ref var mu = ref heap(new Δsync.RWMutex(), out var Ꮡmu);
            Ꮡmu.Lock();
            Ꮡmu.Unlock();
            Ꮡmu.Unlock();
        }
    ),
    new(
        "RWMutex.RUnlock"u8,
        () => {
            ref var mu = ref heap(new Δsync.RWMutex(), out var Ꮡmu);
            Ꮡmu.RUnlock();
        }
    ),
    new(
        "RWMutex.RUnlock2"u8,
        () => {
            ref var mu = ref heap(new Δsync.RWMutex(), out var Ꮡmu);
            Ꮡmu.Lock();
            Ꮡmu.RUnlock();
        }
    ),
    new(
        "RWMutex.RUnlock3"u8,
        () => {
            ref var mu = ref heap(new Δsync.RWMutex(), out var Ꮡmu);
            Ꮡmu.RLock();
            Ꮡmu.RUnlock();
            Ꮡmu.RUnlock();
        }
    )
}.slice();

[GoInit] internal static void init() {
    if (len(Δos.Args) == 3 && Δos.Args[1] == "TESTMISUSE") {
        foreach (var (_, vᴛ1) in misuseTests) {
            ref var test = ref heap(new misuseTestsᴛ1(), out var Ꮡtest);
            test = vᴛ1;

            if (test.name == Δos.Args[2]) {
                var testʗ1 = test;
                ((Action)(() => func((defer, recover) => {
                    defer(() => {
                        recover();
                    });
                    testʗ1.f();
                })))();
                fmt.Printf("test completed\n"u8);
                Δos.Exit(0);
            }
        }
        fmt.Printf("unknown test\n"u8);
        Δos.Exit(0);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string testmisuseˢ = "TESTMISUSE"u8;
private static readonly @string unlockedˢ = "unlocked"u8;

public static void TestMutexMisuse(ж<Δtesting.T> Ꮡt) {
    testenv.MustHaveExec(new testing_TжTB(Ꮡt));
    foreach (var (_, test) in misuseTests) {
        var (@out, err) = exec.Command(Δos.Args[0], testmisuseˢ, test.name).CombinedOutput();
        if (err == default! || !strings.Contains(((@string)@out), unlockedˢ)) {
            Ꮡt.Errorf("%s: did not find failure with message about unlocked lock: %s\n%s\n"u8, test.name, err, @out);
        }
    }
}

public static void TestMutexFairness(ж<Δtesting.T> Ꮡt) => func((defer, recover) => {
    ref var t = ref Ꮡt.Value;

    ref var mu = ref heap(new Δsync.Mutex(), out var Ꮡmu);
    var stop = new channel<bool>(0);
    deferǃ(ᴛ1 => close(ᴛ1), stop, defer);
    var stopʗ1 = stop;
    goǃ(() => {
        while (ᐧ) {
            Ꮡmu.Lock();
            time.Sleep(100 * time.Microsecond);
            Ꮡmu.Unlock();
            var selᴛ7 = stopʗ1;
            switch (trySelect(ᐸꟷ(selᴛ7, ꓸꓸꓸ))) {
            case 0 when selᴛ7.ꟷᐳ(out _): {
                return;
            }
            default: {
                break;
            }}
        }
    });
    var done = new channel<bool>(1);
    var doneʗ1 = done;
    goǃ(() => {
        for (nint i = 0; i < 10; i++) {
            time.Sleep(100 * time.Microsecond);
            Ꮡmu.Lock();
            Ꮡmu.Unlock();
        }
        doneʗ1.ᐸꟷ(true);
    });
    var selᴛ8 = done;
    var selᴛ9 = time.After(10000000000L);
    switch (select(ᐸꟷ(selᴛ8, ꓸꓸꓸ), ᐸꟷ(selᴛ9, ꓸꓸꓸ))) {
    case 0 when selᴛ8.ꟷᐳ(out _): {
        break;
    }
    case 1 when selᴛ9.ꟷᐳ(out _): {
        Ꮡt.Fatalf("can't acquire Mutex in 10 seconds"u8);
        break;
    }}
});

[GoLocalName("PaddedMutex")] [GoType("dyn")] partial struct BenchmarkMutexUncontended_PaddedMutex {
    public partial ref sync_package.Mutex Mutex { get; }
    internal array<uint8> pad = new(128);
}

public static void BenchmarkMutexUncontended(ж<Δtesting.B> Ꮡb) {
    Ꮡb.RunParallel((ж<Δtesting.PB> pb) => {
        ref var mu = ref heap(new BenchmarkMutexUncontended_PaddedMutex(), out var Ꮡmu);
        while (pb.Next()) {
            Ꮡmu.of(BenchmarkMutexUncontended_PaddedMutex.ᏑMutex).Lock();
            Ꮡmu.of(BenchmarkMutexUncontended_PaddedMutex.ᏑMutex).Unlock();
        }
    });
}

internal static void benchmarkMutex(ж<Δtesting.B> Ꮡb, bool slack, bool work) {
    ref var b = ref Ꮡb.Value;

    ref var mu = ref heap(new Δsync.Mutex(), out var Ꮡmu);
    if (slack) {
        b.SetParallelism(10);
    }
    Ꮡb.RunParallel((ж<Δtesting.PB> pb) => {
        nint foo = 0;
        while (pb.Next()) {
            Ꮡmu.Lock();
            Ꮡmu.Unlock();
            if (work) {
                for (nint i = 0; i < 100; i++) {
                    foo *= 2;
                    foo /= 2;
                }
            }
        }
        _ = foo;
    });
}

public static void BenchmarkMutex(ж<Δtesting.B> Ꮡb) {
    benchmarkMutex(Ꮡb, false, false);
}

public static void BenchmarkMutexSlack(ж<Δtesting.B> Ꮡb) {
    benchmarkMutex(Ꮡb, true, false);
}

public static void BenchmarkMutexWork(ж<Δtesting.B> Ꮡb) {
    benchmarkMutex(Ꮡb, false, true);
}

public static void BenchmarkMutexWorkSlack(ж<Δtesting.B> Ꮡb) {
    benchmarkMutex(Ꮡb, true, true);
}

public static void BenchmarkMutexNoSpin(ж<Δtesting.B> Ꮡb) {
    ref var b = ref Ꮡb.Value;

    // This benchmark models a situation where spinning in the mutex should be
    // non-profitable and allows to confirm that spinning does not do harm.
    // To achieve this we create excess of goroutines most of which do local work.
    // These goroutines yield during local work, so that switching from
    // a blocked goroutine to other goroutines is profitable.
    // As a matter of fact, this benchmark still triggers some spinning in the mutex.
    ref var m = ref heap(new Δsync.Mutex(), out var Ꮡm);
    uint64 acc0 = default!;
    uint64 acc1 = default!;
    b.SetParallelism(4);
    Ꮡb.RunParallel((ж<Δtesting.PB> pb) => {
        var c = new channel<bool>(0);
        ref var data = ref heap(new array<uint64>(4096), out var Ꮡdata);
        for (nint i = 0; pb.Next(); i++) {
            if (i % 4 == 0){
                Ꮡm.Lock();
                acc0 -= 100;
                acc1 += 100;
                Ꮡm.Unlock();
            } else {
                for (nint iΔ1 = 0; iΔ1 < len(data); iΔ1 += 4) {
                    data[iΔ1]++;
                }
                // Elaborate way to say runtime.Gosched
                // that does not put the goroutine onto global runq.
                var cʗ1 = c;
                goǃ(() => {
                    cʗ1.ᐸꟷ(true);
                });
                ᐸꟷ(c);
            }
        }
    });
}

public static void BenchmarkMutexSpin(ж<Δtesting.B> Ꮡb) {
    // This benchmark models a situation where spinning in the mutex should be
    // profitable. To achieve this we create a goroutine per-proc.
    // These goroutines access considerable amount of local data so that
    // unnecessary rescheduling is penalized by cache misses.
    ref var m = ref heap(new Δsync.Mutex(), out var Ꮡm);
    uint64 acc0 = default!;
    uint64 acc1 = default!;
    Ꮡb.RunParallel((ж<Δtesting.PB> pb) => {
        ref var data = ref heap(new array<uint64>(16384), out var Ꮡdata);
        for (nint i = 0; pb.Next(); i++) {
            Ꮡm.Lock();
            acc0 -= 100;
            acc1 += 100;
            Ꮡm.Unlock();
            for (nint iΔ1 = 0; iΔ1 < len(data); iΔ1 += 4) {
                data[iΔ1]++;
            }
        }
    });
}

} // end sync_test_package
