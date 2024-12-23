// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build !amd64 && !s390x && !ppc64le && !arm64
// +build !amd64,!s390x,!ppc64le,!arm64

// package aes -- go2cs converted at 2022 March 13 05:32:27 UTC
// import "crypto/aes" ==> using aes = go.crypto.aes_package
// Original source: C:\Program Files\Go\src\crypto\aes\cipher_generic.go
namespace go.crypto;

using cipher = crypto.cipher_package;


// newCipher calls the newCipherGeneric function
// directly. Platforms with hardware accelerated
// implementations of AES should implement their
// own version of newCipher (which may then call
// newCipherGeneric if needed).

public static partial class aes_package {

private static (cipher.Block, error) newCipher(slice<byte> key) {
    cipher.Block _p0 = default;
    error _p0 = default!;

    return newCipherGeneric(key);
}

// expandKey is used by BenchmarkExpand and should
// call an assembly implementation if one is available.
private static void expandKey(slice<byte> key, slice<uint> enc, slice<uint> dec) {
    expandKeyGo(key, enc, dec);
}

} // end aes_package
