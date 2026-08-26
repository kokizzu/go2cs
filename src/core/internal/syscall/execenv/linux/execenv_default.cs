// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !windows
namespace go.@internal.syscall;

using syscall = syscall_package;

partial class execenv_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

// Default will return the default environment
// variables based on the process attributes
// provided.
//
// Defaults to syscall.Environ() on all platforms
// other than Windows.
public static (slice<@string>, error) Default(ж<syscall.SysProcAttr> Ꮡsys) {
    return (syscall.Environ(), default!);
}

} // end execenv_package
