// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !linux && !solaris
[assembly: go.GoPositionMap("internal/syscall/unix/kernel_version_other.go", "kernel_version_other.cs", "AAoSgg==")]

namespace go.@internal.syscall;

partial class unix_package {

public static (nint major, nint minor) KernelVersion() {
    return (0, 0);
}

} // end unix_package
