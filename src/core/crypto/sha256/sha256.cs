// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package sha256 implements the SHA224 and SHA256 hash algorithms as defined
// in FIPS 180-4.
namespace go.crypto;

using crypto = crypto_package;
using boring = go.crypto.@internal.boring_package;
using sha256 = go.crypto.@internal.fips140.sha256_package;
using hash = hash_package;
using go.crypto.@internal;
using go.crypto.@internal.fips140;

partial class sha256_package {

[GoInit] internal static void init() {
    crypto.RegisterHash(crypto.SHA224, New224);
    crypto.RegisterHash(crypto.SHA256, New);
}

// The size of a SHA256 checksum in bytes.
public static UntypedInt Size => 32;

// The size of a SHA224 checksum in bytes.
public static UntypedInt Size224 => 28;

// The blocksize of SHA256 and SHA224 in bytes.
public static UntypedInt BlockSize => 64;

// New returns a new [hash.Hash] computing the SHA256 checksum. The Hash
// also implements [encoding.BinaryMarshaler], [encoding.BinaryAppender] and
// [encoding.BinaryUnmarshaler] to marshal and unmarshal the internal
// state of the hash.
public static hash.Hash New() {
    if (boring.Enabled) {
        return boring.NewSHA256();
    }
    return new sha256_DigestжHash(sha256.New());
}

// New224 returns a new [hash.Hash] computing the SHA224 checksum. The Hash
// also implements [encoding.BinaryMarshaler], [encoding.BinaryAppender] and
// [encoding.BinaryUnmarshaler] to marshal and unmarshal the internal
// state of the hash.
public static hash.Hash New224() {
    if (boring.Enabled) {
        return boring.NewSHA224();
    }
    return new sha256_DigestжHash(sha256.New224());
}

// Sum256 returns the SHA256 checksum of the data.
public static array<byte> Sum256(slice<byte> data) {
    if (boring.Enabled) {
        return boring.SHA256(data);
    }
    var h = New();
    h.Write(data);
    array<byte> sum = new(32); /* Size */
    h.Sum(sum[..0]);
    return sum.Clone();
}

// Sum224 returns the SHA224 checksum of the data.
public static array<byte> Sum224(slice<byte> data) {
    if (boring.Enabled) {
        return boring.SHA224(data);
    }
    var h = New224();
    h.Write(data);
    array<byte> sum = new(28); /* Size224 */
    h.Sum(sum[..0]);
    return sum.Clone();
}

} // end sha256_package
