// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build !windows
// +build !windows

// package testenv -- go2cs converted at 2022 March 13 06:44:33 UTC
// import "internal/testenv" ==> using testenv = go.@internal.testenv_package
// Original source: C:\Program Files\Go\src\internal\testenv\testenv_notwin.go
namespace go.@internal;

using runtime = runtime_package;

public static partial class testenv_package {

private static (bool, @string) hasSymlink() {
    bool ok = default;
    @string reason = default;

    switch (runtime.GOOS) {
        case "android": 

        case "plan9": 
            return (false, "");
            break;
    }

    return (true, "");
}

} // end testenv_package
