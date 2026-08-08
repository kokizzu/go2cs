// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.runtime.@internal;

using sys = go.runtime.@internal.sys_package;
using testing = testing_package;
using go.runtime.@internal;

partial class sys_test_package {

public static void TestTrailingZeros64(ж<testing.T> Ꮡt) {
    for (nint i = 0; i <= 64; i++) {
        var x = ((uint64)5).Lsh((nuint)i);
        {
            nint got = sys.TrailingZeros64(x); if (got != i) {
                Ꮡt.Errorf("TrailingZeros64(%d)=%d, want %d"u8, x, got, i);
            }
        }
    }
}

public static void TestTrailingZeros32(ж<testing.T> Ꮡt) {
    for (nint i = 0; i <= 32; i++) {
        var x = ((uint32)5).Lsh((nuint)i);
        {
            nint got = sys.TrailingZeros32(x); if (got != i) {
                Ꮡt.Errorf("TrailingZeros32(%d)=%d, want %d"u8, x, got, i);
            }
        }
    }
}

public static void TestBswap64(ж<testing.T> Ꮡt) {
    var x = (uint64)0x1122334455667788UL;
    var y = sys.Bswap64(x);
    if (y != 0x8877665544332211UL) {
        Ꮡt.Errorf("Bswap(%x)=%x, want 0x8877665544332211"u8, x, y);
    }
}

public static void TestBswap32(ж<testing.T> Ꮡt) {
    var x = (uint32)0x11223344;
    var y = sys.Bswap32(x);
    if (y != 0x44332211) {
        Ꮡt.Errorf("Bswap(%x)=%x, want 0x44332211"u8, x, y);
    }
}

} // end sys_test_package
