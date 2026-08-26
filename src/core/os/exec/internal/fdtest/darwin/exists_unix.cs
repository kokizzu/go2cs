// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix || wasm

// Package fdtest provides test helpers for working with file descriptors across exec.
namespace go.os.exec.@internal;

using syscall = syscall_package;

partial class fdtest_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

// Exists returns true if fd is a valid file descriptor.
public static bool Exists(uintptr fd) {
    ref var s = ref heap(new syscall.Stat_t(), out var Ꮡs);
    var err = syscall.Fstat((nint)fd, Ꮡs);
    return !AreEqual(err, syscall.EBADF);
}

} // end fdtest_package
