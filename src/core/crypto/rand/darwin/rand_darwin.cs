// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using unix = go.@internal.syscall.unix_package;
using go.@internal.syscall;

partial class rand_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸsyscallꓸunix() {
    builtin.initPackage(typeof(go.@internal.syscall.unix_package));
}

[GoInit] internal static void init() {
    // arc4random_buf is the recommended application CSPRNG, accepts buffers of
    // any size, and never returns an error.
    //
    // "The subsystem is re-seeded from the kernel random number subsystem on a
    // regular basis, and also upon fork(2)." - arc4random(3)
    //
    // Note that despite its legacy name, it uses a secure CSPRNG (not RC4) in
    // all supported macOS versions.
    altGetRandom = error (slice<byte> b) => {
        unix.ARC4Random(b);
        return default!;
    };
}

} // end rand_package
