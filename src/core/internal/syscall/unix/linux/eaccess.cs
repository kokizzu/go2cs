// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix
namespace go.@internal.syscall;

using runtime = runtime_package;
using syscall = syscall_package;

partial class unix_package {

public static error Eaccess(@string path, uint32 mode) {
    if (runtime.GOOS == "android"u8) {
        // syscall.Faccessat for Android implements AT_EACCESS check in
        // userspace. Since Android doesn't have setuid programs and
        // never runs code with euid!=uid, AT_EACCESS check is not
        // really required. Return ENOSYS so the callers can fall back
        // to permission bits check.
        return syscall.ENOSYS;
    }
    return faccessat(AT_FDCWD, path, mode, AT_EACCESS);
}

} // end unix_package
