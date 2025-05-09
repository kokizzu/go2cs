// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package hkdf implements the HMAC-based Extract-and-Expand Key Derivation
// Function (HKDF) as defined in RFC 5869.
//
// HKDF is a cryptographic key derivation function (KDF) with the goal of
// expanding limited input keying material into one or more cryptographically
// strong secret keys.
namespace go.vendor.golang.org.x.crypto;

// import "golang.org/x/crypto/hkdf"
using hmac = crypto.hmac_package;
using errors = errors_package;
using hash = hash_package;
using io = io_package;
using crypto;

partial class hkdf_package {

// Extract generates a pseudorandom key for use with Expand from an input secret
// and an optional independent salt.
//
// Only use this function if you need to reuse the extracted key with multiple
// Expand invocations and different context values. Most common scenarios,
// including the generation of multiple keys, should use New instead.
public static slice<byte> Extract(Func<hash.Hash> hash, slice<byte> secret, slice<byte> salt) {
    if (salt == default!) {
        salt = new slice<byte>(hash().Size());
    }
    var extractor = hmac.New(hash, salt);
    extractor.Write(secret);
    return extractor.Sum(default!);
}

[GoType] partial struct hkdf {
    internal hash_package.Hash expander;
    internal nint size;
    internal slice<byte> info;
    internal byte counter;
    internal slice<byte> prev;
    internal slice<byte> buf;
}

[GoRecv] internal static (nint, error) Read(this ref hkdf f, slice<byte> p) {
    // Check whether enough data can be generated
    nint need = len(p);
    nint remains = len(f.buf) + ((nint)(255 - f.counter + 1)) * f.size;
    if (remains < need) {
        return (0, errors.New("hkdf: entropy limit reached"u8));
    }
    // Read any leftover from the buffer
    nint n = copy(p, f.buf);
    p = p[(int)(n)..];
    // Fill the rest of the buffer
    while (len(p) > 0) {
        if (f.counter > 1) {
            f.expander.Reset();
        }
        f.expander.Write(f.prev);
        f.expander.Write(f.info);
        f.expander.Write(new byte[]{f.counter}.slice());
        f.prev = f.expander.Sum(f.prev[..0]);
        f.counter++;
        // Copy the new batch into p
        f.buf = f.prev;
        n = copy(p, f.buf);
        p = p[(int)(n)..];
    }
    // Save leftovers for next run
    f.buf = f.buf[(int)(n)..];
    return (need, default!);
}

// Expand returns a Reader, from which keys can be read, using the given
// pseudorandom key and optional context info, skipping the extraction step.
//
// The pseudorandomKey should have been generated by Extract, or be a uniformly
// random or pseudorandom cryptographically strong key. See RFC 5869, Section
// 3.3. Most common scenarios will want to use New instead.
public static io.Reader Expand(Func<hash.Hash> hash, slice<byte> pseudorandomKey, slice<byte> info) {
    var expander = hmac.New(hash, pseudorandomKey);
    return new hkdf(expander, expander.Size(), info, 1, default!, default!);
}

// New returns a Reader, from which keys can be read, using the given hash,
// secret, salt and context info. Salt and info can be nil.
public static io.Reader New(Func<hash.Hash> hash, slice<byte> secret, slice<byte> salt, slice<byte> info) {
    var prk = Extract(hash, secret, salt);
    return Expand(hash, prk, info);
}

} // end hkdf_package
