// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !linux && !darwin && !dragonfly && !freebsd && !netbsd && !solaris
[assembly: go.GoPositionMap("runtime/auxv_none.go", "auxv_none.cs", "AAoS")]

namespace go;

partial class runtime_package {

internal static void sysargs(int32 argc, ref ж<byte> argv) {
}

} // end runtime_package
