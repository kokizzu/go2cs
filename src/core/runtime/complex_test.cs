// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using cmplx = global::go.math.cmplx_package;
using testing = testing_package;
using global::go.math;
using static global::go.runtime_internal_test_package;

partial class runtime_test_package {

internal static complex128 result;

public static void BenchmarkComplex128DivNormal(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = 15D + 2D.i();
    var n = 32D + 3D.i();
    var res = 0D.i();
    for (nint i = 0; i < b.N; i++) {
        n += 0.1D.i();
        res += n / d;
    }
    result = res;
}

public static void BenchmarkComplex128DivNisNaN(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = cmplx.NaN();
    var n = 32D + 3D.i();
    var res = 0D.i();
    for (nint i = 0; i < b.N; i++) {
        n += 0.1D.i();
        res += n / d;
    }
    result = res;
}

public static void BenchmarkComplex128DivDisNaN(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = 15D + 2D.i();
    var n = cmplx.NaN();
    var res = 0D.i();
    for (nint i = 0; i < b.N; i++) {
        d += 0.1D.i();
        res += n / d;
    }
    result = res;
}

public static void BenchmarkComplex128DivNisInf(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = 15D + 2D.i();
    var n = cmplx.Inf();
    var res = 0D.i();
    for (nint i = 0; i < b.N; i++) {
        d += 0.1D.i();
        res += n / d;
    }
    result = res;
}

public static void BenchmarkComplex128DivDisInf(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = cmplx.Inf();
    var n = 32D + 3D.i();
    var res = 0D.i();
    for (nint i = 0; i < b.N; i++) {
        n += 0.1D.i();
        res += n / d;
    }
    result = res;
}

} // end runtime_test_package
