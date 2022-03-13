// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build compiler_bootstrap
// +build compiler_bootstrap

// package strconv -- go2cs converted at 2022 March 06 22:30:29 UTC
// import "strconv" ==> using strconv = go.strconv_package
// Original source: C:\Program Files\Go\src\strconv\bytealg_bootstrap.go


namespace go;

public static partial class strconv_package {

    // index returns the index of the first instance of c in s, or -1 if missing.
private static nint index(@string s, byte c) {
    for (nint i = 0; i < len(s); i++) {
        if (s[i] == c) {
            return i;
        }
    }
    return -1;

}

} // end strconv_package