// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using @unsafe = unsafe_package;

partial class syscall_package {

// fcntl64Syscall is usually SYS_FCNTL, but is overridden on 32-bit Linux
// systems by flock_linux_32bit.go to be SYS_FCNTL64.
internal static uintptr fcntl64Syscall = SYS_FCNTL;

// go2cs generated this placeholder — func FcntlFlock is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

} // end syscall_package
