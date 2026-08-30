// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using testing = testing_package;
using static go.@internal.concurrent_package;

partial class concurrent_internal_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸtesting() {
    builtin.initPackage(typeof(testing_package));
}

public static void BenchmarkHashTrieMapLoadSmall(ж<testing.B> Ꮡb) {
    benchmarkHashTrieMapLoad(Ꮡb, testDataSmall[..]);
}

public static void BenchmarkHashTrieMapLoad(ж<testing.B> Ꮡb) {
    benchmarkHashTrieMapLoad(Ꮡb, testData[..]);
}

public static void BenchmarkHashTrieMapLoadLarge(ж<testing.B> Ꮡb) {
    benchmarkHashTrieMapLoad(Ꮡb, testDataLarge[..]);
}

internal static void benchmarkHashTrieMapLoad(ж<testing.B> Ꮡb, slice<@string> data) {
    ref var b = ref Ꮡb.DerefOrNull();

    b.ReportAllocs();
    var m = NewHashTrieMap<@string, nint>();
    foreach (var (i, _) in data) {
        m.LoadOrStore(data[i], i);
    }
    b.ResetTimer();
    var dataʗ1 = data;
    var mʗ1 = m;
    Ꮡb.RunParallel((ж<testing.PB> pb) => {
        nint i = 0;
        while (pb.Next()) {
            (_, _) = mʗ1.Load(dataʗ1[i]);
            i++;
            if (i >= len(dataʗ1)) {
                i = 0;
            }
        }
    });
}

public static void BenchmarkHashTrieMapLoadOrStore(ж<testing.B> Ꮡb) {
    benchmarkHashTrieMapLoadOrStore(Ꮡb, testData[..]);
}

public static void BenchmarkHashTrieMapLoadOrStoreLarge(ж<testing.B> Ꮡb) {
    benchmarkHashTrieMapLoadOrStore(Ꮡb, testDataLarge[..]);
}

internal static void benchmarkHashTrieMapLoadOrStore(ж<testing.B> Ꮡb, slice<@string> data) {
    ref var b = ref Ꮡb.DerefOrNull();

    b.ReportAllocs();
    var m = NewHashTrieMap<@string, nint>();
    var dataʗ1 = data;
    var mʗ1 = m;
    Ꮡb.RunParallel((ж<testing.PB> pb) => {
        nint i = 0;
        while (pb.Next()) {
            (_, _) = mʗ1.LoadOrStore(dataʗ1[i], i);
            i++;
            if (i >= len(dataʗ1)) {
                i = 0;
            }
        }
    });
}

} // end concurrent_internal_test_package
