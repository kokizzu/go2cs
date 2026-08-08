// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux || netbsd || openbsd || solaris
namespace go;

partial class runtime_package {

internal static (int32 r, int32 w, int32 errno) nonblockingPipe() {
    return pipe2((int32)((int32)_O_NONBLOCK | (int32)_O_CLOEXEC));
}

} // end runtime_package
