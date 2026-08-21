// Copyright 2017 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !linux
[assembly: go.GoPositionMap("runtime/stubs_nonlinux.go", "stubs_nonlinux.cs", "AAoUkg==")]

namespace go;

partial class runtime_package {

// sbrk0 returns the current process brk, or 0 if not implemented.
internal static uintptr sbrk0() {
    return 0;
}

} // end runtime_package
