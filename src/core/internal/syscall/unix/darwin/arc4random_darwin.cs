// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal.syscall;

using abi = go.@internal.abi_package;
using @unsafe = unsafe_package;
using go.@internal;
using syscall = syscall_package;

partial class unix_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸabi() {
    builtin.initPackage(typeof(go.@internal.abi_package));
}

//go:cgo_import_dynamic libc_arc4random_buf arc4random_buf "/usr/lib/libSystem.B.dylib"
internal static partial void libc_arc4random_buf_trampoline();

// ARC4Random calls the macOS arc4random_buf(3) function.
public static void ARC4Random(slice<byte> p) {
    // macOS 11 and 12 abort if length is 0.
    if (len(p) == 0) {
        return;
    }
    syscall_syscall(abi.FuncPCABI0(libc_arc4random_buf_trampoline),
        (uintptr)@unsafe.SliceData(p), (uintptr)len(p), 0);
}

} // end unix_package
