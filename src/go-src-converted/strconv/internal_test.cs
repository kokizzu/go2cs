// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// export access to strconv internals for tests
namespace go;

partial class strconv_package {

internal static ж<@decimal> NewDecimal(uint64 i) {
    var d = @new<@decimal>();
    d.Assign(i);
    return d;
}

public static bool SetOptimize(bool b) {
    var old = optimize;
    optimize = b;
    return old;
}

public static (float64, nint, error) ParseFloatPrefix(@string s, nint bitSize) {
    return parseFloatPrefix(s, bitSize);
}

public static nint MulByLog2Log10(nint x) {
    return mulByLog2Log10(x);
}

public static nint MulByLog10Log2(nint x) {
    return mulByLog10Log2(x);
}

} // end strconv_package
