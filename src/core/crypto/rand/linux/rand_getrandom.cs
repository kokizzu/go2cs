// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux || solaris
namespace go.crypto;

using unix = go.@internal.syscall.unix_package;
using runtime = runtime_package;
using syscall = syscall_package;
using go.@internal.syscall;

partial class rand_package {

[GoInit] internal static void init() {
    nint maxGetRandomRead = default!;
    var exprᴛ1 = runtime.GOOS;
    if (exprᴛ1 == "linux"u8 || exprᴛ1 == "android"u8) {
        maxGetRandomRead = ((1 << (int)(25))) - 1;
    }
    else if (exprᴛ1 == "dragonfly"u8 || exprᴛ1 == "freebsd"u8 || exprᴛ1 == "illumos"u8 || exprᴛ1 == "solaris"u8) {
        maxGetRandomRead = (1 << (int)(8));
    }
    else { /* default: */
        throw panic("no maximum specified for GetRandom");
    }

    // Per the manpage:
    //     When reading from the urandom source, a maximum of 33554431 bytes
    //     is returned by a single call to getrandom() on systems where int
    //     has a size of 32 bits.
    altGetRandom = batched(getRandom, maxGetRandomRead);
}

// If the kernel is too old to support the getrandom syscall(),
// unix.GetRandom will immediately return ENOSYS and we will then fall back to
// reading from /dev/urandom in rand_unix.go. unix.GetRandom caches the ENOSYS
// result so we only suffer the syscall overhead once in this case.
// If the kernel supports the getrandom() syscall, unix.GetRandom will block
// until the kernel has sufficient randomness (as we don't use GRND_NONBLOCK).
// In this case, unix.GetRandom will not return an error.
internal static error getRandom(slice<byte> p) {
    var (n, err) = unix.GetRandom(p, 0);
    if (err != default!) {
        return err;
    }
    if (n != len(p)) {
        return syscall.EIO;
    }
    return default!;
}

} // end rand_package
