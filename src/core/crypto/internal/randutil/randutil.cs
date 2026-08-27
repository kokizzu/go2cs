// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package randutil contains internal randomness utilities for various
// crypto packages.
namespace go.crypto.@internal;

using io = io_package;
using sync = sync_package;

partial class randutil_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸio() {
    builtin.initPackage(typeof(io_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsync() {
    builtin.initPackage(typeof(sync_package));
}

internal static ж<sync.Once> ᏑclosedChanOnce = new StandardBox<sync.Once>(default(sync.Once));
internal static ref sync.Once closedChanOnce => ref ᏑclosedChanOnce.Value;
internal static channel<EmptyStruct> closedChan;

// MaybeReadByte reads a single byte from r with ~50% probability. This is used
// to ensure that callers do not depend on non-guaranteed behaviour, e.g.
// assuming that rsa.GenerateKey is deterministic w.r.t. a given random stream.
//
// This does not affect tests that pass a stream of fixed bytes as the random
// source (e.g. a zeroReader).
public static void MaybeReadByte(io.Reader r) {
    ᏑclosedChanOnce.Do(() => {
        closedChan = new channel<EmptyStruct>(0);
        close(closedChan);
    });
    var selᴛ1 = closedChan;
    var selᴛ2 = closedChan;
    switch (select(ᐸꟷ(selᴛ1, ꓸꓸꓸ), ᐸꟷ(selᴛ2, ꓸꓸꓸ))) {
    case 0 when selᴛ1.ꟷᐳ(out _): {
        return;
    }
    case 1 when selᴛ2.ꟷᐳ(out _): {
        array<byte> buf = new(1);
        r.Read(buf[..]);
        break;
    }}
}

} // end randutil_package
