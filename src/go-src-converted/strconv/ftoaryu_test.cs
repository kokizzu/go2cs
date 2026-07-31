// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using Δmath = math_package;
using static strconv_package;
using testing = testing_package;
using static go.strconv_internal_test_package;

partial class strconv_test_package {

public static void TestMulByLog2Log10(ж<testing.T> Ꮡt) {
    for (nint x = -1600; x <= +1600; x++) {
        nint iMath = strconv_internal_test_package.MulByLog2Log10(x);
        nint fMath = (nint)Δmath.Floor((float64)x * (float64)Δmath.Ln2 / (float64)Δmath.Ln10);
        if (iMath != fMath) {
            Ꮡt.Errorf("mulByLog2Log10(%d) failed: %d vs %d\n"u8, x, iMath, fMath);
        }
    }
}

public static void TestMulByLog10Log2(ж<testing.T> Ꮡt) {
    for (nint x = -500; x <= +500; x++) {
        nint iMath = strconv_internal_test_package.MulByLog10Log2(x);
        nint fMath = (nint)Δmath.Floor((float64)x * (float64)Δmath.Ln10 / (float64)Δmath.Ln2);
        if (iMath != fMath) {
            Ꮡt.Errorf("mulByLog10Log2(%d) failed: %d vs %d\n"u8, x, iMath, fMath);
        }
    }
}

} // end strconv_test_package
