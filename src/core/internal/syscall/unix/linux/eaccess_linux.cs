// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal.syscall;

using syscall = syscall_package;

partial class unix_package {

public static error Eaccess(@string path, uint32 mode) {
    return syscall.Faccessat(AT_FDCWD, path, mode, AT_EACCESS);
}

} // end unix_package
