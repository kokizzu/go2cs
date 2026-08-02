// Copyright 2015 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build math_big_pure_go
namespace go.math;

partial class big_package {

internal static Word /*c*/ addVV(slice<Word> z, slice<Word> x, slice<Word> y) {
    Word c = default!;

    return addVV_g(z, x, y);
}

internal static Word /*c*/ subVV(slice<Word> z, slice<Word> x, slice<Word> y) {
    Word c = default!;

    return subVV_g(z, x, y);
}

internal static Word /*c*/ addVW(slice<Word> z, slice<Word> x, Word y) {
    Word c = default!;

    // TODO: remove indirect function call when golang.org/issue/30548 is fixed
    var fn = addVW_g;
    if (len(z) > 32) {
        fn = addVWlarge;
    }
    return fn(z, x, y);
}

internal static Word /*c*/ subVW(slice<Word> z, slice<Word> x, Word y) {
    Word c = default!;

    // TODO: remove indirect function call when golang.org/issue/30548 is fixed
    var fn = subVW_g;
    if (len(z) > 32) {
        fn = subVWlarge;
    }
    return fn(z, x, y);
}

internal static Word /*c*/ shlVU(slice<Word> z, slice<Word> x, nuint s) {
    Word c = default!;

    return shlVU_g(z, x, s);
}

internal static Word /*c*/ shrVU(slice<Word> z, slice<Word> x, nuint s) {
    Word c = default!;

    return shrVU_g(z, x, s);
}

internal static Word /*c*/ mulAddVWW(slice<Word> z, slice<Word> x, Word y, Word r) {
    Word c = default!;

    return mulAddVWW_g(z, x, y, r);
}

internal static Word /*c*/ addMulVVW(slice<Word> z, slice<Word> x, Word y) {
    Word c = default!;

    return addMulVVW_g(z, x, y);
}

} // end big_package
