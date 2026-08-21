// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("unicode/digit.go", "digit.cs", "AAkQkoKU")]

namespace go;

partial class unicode_package {

// IsDigit reports whether the rune is a decimal digit.
public static bool IsDigit(rune r) {
    if (r <= MaxLatin1) {
        return (rune)'0' <= r && r <= (rune)'9';
    }
    return isExcludingLatin(ref (Digit).DerefOrNull(), r);
}

} // end unicode_package
