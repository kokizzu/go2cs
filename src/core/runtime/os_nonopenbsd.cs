// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !openbsd
[assembly: go.GoPositionMap("runtime/os_nonopenbsd.go", "os_nonopenbsd.cs", "AAoWyg==")]

namespace go;

partial class runtime_package {

// osStackAlloc performs OS-specific initialization before s is used
// as stack memory.
internal static void osStackAlloc(ref mspan s) {
}

// osStackFree undoes the effect of osStackAlloc before s is returned
// to the heap.
internal static void osStackFree(ref mspan s) {
}

} // end runtime_package
