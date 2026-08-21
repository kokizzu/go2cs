// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bufio = bufio_package;
using bytes = bytes_package;
using bzip2 = compress.bzip2_package;
using crypto = crypto_package;
using rand = go.crypto.rand_package;
using static go.crypto.rsa_package;
using sha1 = go.crypto.sha1_package;
using sha256 = go.crypto.sha256_package;
using hex = encoding.hex_package;
using big = math.big_package;
using os = os_package;
using strconv = strconv_package;
using strings = strings_package;
using testing = testing_package;
using compress;
using encoding;
using go.crypto;
using hash = hash_package;
using io = io_package;
using math;
using rsa = go.crypto.rsa_package;
using static go.crypto.rsa_internal_test_package;

partial class rsa_test_package {

public static void TestEMSAPSS(ж<testing.T> Ꮡt) {
    // Test vector in file pss-int.txt from: ftp://ftp.rsasecurity.com/pub/pkcs/pkcs-1/pkcs-1v2-1-vec.zip
    var msg = new byte[]{
        0x85, 0x9e, 0xef, 0x2f, 0xd7, 0x8a, 0xca, 0x00, 0x30, 0x8b,
        0xdc, 0x47, 0x11, 0x93, 0xbf, 0x55, 0xbf, 0x9d, 0x78, 0xdb,
        0x8f, 0x8a, 0x67, 0x2b, 0x48, 0x46, 0x34, 0xf3, 0xc9, 0xc2,
        0x6e, 0x64, 0x78, 0xae, 0x10, 0x26, 0x0f, 0xe0, 0xdd, 0x8c,
        0x08, 0x2e, 0x53, 0xa5, 0x29, 0x3a, 0xf2, 0x17, 0x3c, 0xd5,
        0x0c, 0x6d, 0x5d, 0x35, 0x4f, 0xeb, 0xf7, 0x8b, 0x26, 0x02,
        0x1c, 0x25, 0xc0, 0x27, 0x12, 0xe7, 0x8c, 0xd4, 0x69, 0x4c,
        0x9f, 0x46, 0x97, 0x77, 0xe4, 0x51, 0xe7, 0xf8, 0xe9, 0xe0,
        0x4c, 0xd3, 0x73, 0x9c, 0x6b, 0xbf, 0xed, 0xae, 0x48, 0x7f,
        0xb5, 0x56, 0x44, 0xe9, 0xca, 0x74, 0xff, 0x77, 0xa5, 0x3c,
        0xb7, 0x29, 0x80, 0x2f, 0x6e, 0xd4, 0xa5, 0xff, 0xa8, 0xba,
        0x15, 0x98, 0x90, 0xfc
    }.slice();
    var salt = new byte[]{
        0xe3, 0xb5, 0xd5, 0xd0, 0x02, 0xc1, 0xbc, 0xe5, 0x0c, 0x2b,
        0x65, 0xef, 0x88, 0xa1, 0x88, 0xd8, 0x3b, 0xce, 0x7e, 0x61
    }.slice();
    var expected = new byte[]{
        0x66, 0xe4, 0x67, 0x2e, 0x83, 0x6a, 0xd1, 0x21, 0xba, 0x24,
        0x4b, 0xed, 0x65, 0x76, 0xb8, 0x67, 0xd9, 0xa4, 0x47, 0xc2,
        0x8a, 0x6e, 0x66, 0xa5, 0xb8, 0x7d, 0xee, 0x7f, 0xbc, 0x7e,
        0x65, 0xaf, 0x50, 0x57, 0xf8, 0x6f, 0xae, 0x89, 0x84, 0xd9,
        0xba, 0x7f, 0x96, 0x9a, 0xd6, 0xfe, 0x02, 0xa4, 0xd7, 0x5f,
        0x74, 0x45, 0xfe, 0xfd, 0xd8, 0x5b, 0x6d, 0x3a, 0x47, 0x7c,
        0x28, 0xd2, 0x4b, 0xa1, 0xe3, 0x75, 0x6f, 0x79, 0x2d, 0xd1,
        0xdc, 0xe8, 0xca, 0x94, 0x44, 0x0e, 0xcb, 0x52, 0x79, 0xec,
        0xd3, 0x18, 0x3a, 0x31, 0x1f, 0xc8, 0x96, 0xda, 0x1c, 0xb3,
        0x93, 0x11, 0xaf, 0x37, 0xea, 0x4a, 0x75, 0xe2, 0x4b, 0xdb,
        0xfd, 0x5c, 0x1d, 0xa0, 0xde, 0x7c, 0xec, 0xdf, 0x1a, 0x89,
        0x6f, 0x9d, 0x8b, 0xc8, 0x16, 0xd9, 0x7c, 0xd7, 0xa2, 0xc4,
        0x3b, 0xad, 0x54, 0x6f, 0xbe, 0x8c, 0xfe, 0xbc
    }.slice();
    var hash = sha1.New();
    hash.Write(msg);
    var hashed = hash.Sum(default!);
    var (encoded, err) = rsa_internal_test_package.EMSAPSSEncode(hashed, 1023, salt, sha1.New());
    if (err != default!) {
        Ꮡt.Errorf("Error from emsaPSSEncode: %s\n"u8, err);
    }
    if (!bytes.Equal(encoded, expected)) {
        Ꮡt.Errorf("Bad encoding. got %x, want %x"u8, encoded, expected);
    }
    {
        err = rsa_internal_test_package.EMSAPSSVerify(hashed, encoded, 1023, len(salt), sha1.New()); if (err != default!) {
            Ꮡt.Errorf("Bad verification: %s"u8, err);
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string testdataPssVectTxtBz2ˢ = "testdata/pss-vect.txt.bz2"u8;

// TestPSSGolden tests all the test vectors in pss-vect.txt from
// ftp://ftp.rsasecurity.com/pub/pkcs/pkcs-1/pkcs-1v2-1-vec.zip
public static void TestPSSGolden(ж<testing.T> Ꮡt) {
    GoFrame ᒐ = default;
    try {
        ref var t = ref Ꮡt.DerefOrNull();

        var (inFile, err) = os.Open(testdataPssVectTxtBz2ˢ);
        if (err != default!) {
            Ꮡt.Fatalf("Failed to open input file: %s"u8, err);
        }
        var inFileʗ1 = inFile;
        defer(() => inFileʗ1.Close(), ref ᒐ);
        // The pss-vect.txt file contains RSA keys and then a series of
        // signatures. A goroutine is used to preprocess the input by merging
        // lines, removing spaces in hex values and identifying the start of
        // new keys and signature blocks.
        @string newKeyMarker = "START NEW KEY"u8;
        @string newSignatureMarker = "START NEW SIGNATURE"u8;
        var values = new channel<@string>(0);
        var inFileʗ2 = inFile;
        var valuesʗ1 = values;
        goǃ(() => {
            GoFrame ᒐ = default;
            try {
                defer(ᴛ1 => close(ᴛ1), valuesʗ1, ref ᒐ);
                var scanner = bufio.NewScanner(bzip2.NewReader(new rsa_test_package.os_FileжReader(inFileʗ2)));
                @string partialValue = default!;
                var lastWasValue = true;
                while (scanner.Scan()) {
                    @string line = scanner.Text();
                    switch (ᐧ) {
                    case {} when len(line) is 0: {
                        if (len(partialValue) > 0) {
                            valuesʗ1.ᐸꟷ(strings.ReplaceAll(partialValue, " "u8, ""u8));
                            partialValue = ""u8;
                            lastWasValue = true;
                        }
                        continue;
                        break;
                    }
                    case {} when strings.HasPrefix(line, "# ======"u8) && lastWasValue: {
                        valuesʗ1.ᐸꟷ(newKeyMarker);
                        lastWasValue = false;
                        break;
                    }
                    case {} when strings.HasPrefix(line, "# ------"u8) && lastWasValue: {
                        valuesʗ1.ᐸꟷ(newSignatureMarker);
                        lastWasValue = false;
                        break;
                    }
                    case {} when strings.HasPrefix(line, "#"u8): {
                        continue;
                        break;
                    }
                    default: {
                        partialValue += line;
                        break;
                    }}

                }
                {
                    var errΔ1 = scanner.Err(); if (errΔ1 != default!) {
                        throw panic(errΔ1);
                    }
                }
            }
            catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
            finally { ᒐ.Run(); }
        });
        ж<rsa.PublicKey> key = default!;
        slice<byte> hashed = default!;
        crypto.Hash hash = crypto.SHA1;
        var h = hash.New();
        var opts = Ꮡ(new PSSOptions(
            SaltLength: PSSSaltLengthEqualsHash
        ));
        foreach (var marker in values) {
            var exprᴛ1 = marker;
            if (exprᴛ1 == newKeyMarker) {
                key = @new<rsa.PublicKey>();
                var (nHex, ok) = ᐸꟷ(values, ꟷ);
                if (!ok) {
                    continue;
                }
                key.Value.N = bigFromHex(nHex);
                key.Value.E = intFromHex(ᐸꟷ(values));
                for (nint i = 0; i < 6; i++) {
                    // We don't care for d, p, q, dP, dQ or qInv.
                    ᐸꟷ(values);
                }
            }
            else if (exprᴛ1 == newSignatureMarker) {
                var msg = fromHex(ᐸꟷ(values));
                ᐸꟷ(values); // skip salt
                var sig = fromHex(ᐸꟷ(values));
                h.Reset();
                h.Write(msg);
                hashed = h.Sum(hashed[..0]);
                {
                    var errΔ3 = VerifyPSS(key, hash, hashed, sig, opts); if (errΔ3 != default!) {
                        Ꮡt.Error(errΔ3);
                    }
                }
            }
            else { /* default: */
                Ꮡt.Fatalf("unknown marker: %s"u8, marker);
            }

        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// TestPSSOpenSSL ensures that we can verify a PSS signature from OpenSSL with
// the default options. OpenSSL sets the salt length to be maximal.
public static void TestPSSOpenSSL(ж<testing.T> Ꮡt) {
    crypto.Hash hash = crypto.SHA256;
    var h = hash.New();
    h.Write(slice<byte>("testing"u8));
    var hashed = h.Sum(default!);
    // Generated with `echo -n testing | openssl dgst -sign key.pem -sigopt rsa_padding_mode:pss -sha256 > sig`
    var sig = new byte[]{
        0x95, 0x59, 0x6f, 0xd3, 0x10, 0xa2, 0xe7, 0xa2, 0x92, 0x9d,
        0x4a, 0x07, 0x2e, 0x2b, 0x27, 0xcc, 0x06, 0xc2, 0x87, 0x2c,
        0x52, 0xf0, 0x4a, 0xcc, 0x05, 0x94, 0xf2, 0xc3, 0x2e, 0x20,
        0xd7, 0x3e, 0x66, 0x62, 0xb5, 0x95, 0x2b, 0xa3, 0x93, 0x9a,
        0x66, 0x64, 0x25, 0xe0, 0x74, 0x66, 0x8c, 0x3e, 0x92, 0xeb,
        0xc6, 0xe6, 0xc0, 0x44, 0xf3, 0xb4, 0xb4, 0x2e, 0x8c, 0x66,
        0x0a, 0x37, 0x9c, 0x69
    }.slice();
    {
        var err = VerifyPSS(rsaPrivateKey.of(rsa.PrivateKey.ᏑPublicKey), hash, hashed, sig, nil); if (err != default!) {
            Ꮡt.Error(err);
        }
    }
}

public static void TestPSSNilOpts(ж<testing.T> Ꮡt) {
    crypto.Hash hash = crypto.SHA256;
    var h = hash.New();
    h.Write(slice<byte>("testing"u8));
    var hashed = h.Sum(default!);
    SignPSS(rand.Reader, rsaPrivateKey, hash, hashed, nil);
}

[GoType("dyn")] partial struct TestPSSSigning_type {
    internal nint signSaltLength, verifySaltLength;
    internal bool good;
}

public static void TestPSSSigning(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    slice<TestPSSSigning_type> saltLengthCombinations = new TestPSSSigning_type[]{
        new(PSSSaltLengthAuto, PSSSaltLengthAuto, true),
        new(PSSSaltLengthEqualsHash, PSSSaltLengthAuto, true),
        new(PSSSaltLengthEqualsHash, PSSSaltLengthEqualsHash, true),
        new(PSSSaltLengthEqualsHash, 8, false),
        new(PSSSaltLengthAuto, PSSSaltLengthEqualsHash, false),
        new(8, 8, true),
        new(PSSSaltLengthAuto, 42, true),
        new(PSSSaltLengthAuto, 20, false),
        new(PSSSaltLengthAuto, -2, false)
    }.slice();
    crypto.Hash hash = crypto.SHA1;
    var h = hash.New();
    h.Write(slice<byte>("testing"u8));
    var hashed = h.Sum(default!);
    ref var opts = ref heap(new rsa.PSSOptions(), out var Ꮡopts);
    foreach (var (i, test) in saltLengthCombinations) {
        opts.SaltLength = test.signSaltLength;
        var (sig, err) = SignPSS(rand.Reader, rsaPrivateKey, hash, hashed, Ꮡopts);
        if (err != default!) {
            Ꮡt.Errorf("#%d: error while signing: %s"u8, i, err);
            continue;
        }
        opts.SaltLength = test.verifySaltLength;
        err = VerifyPSS(rsaPrivateKey.of(rsa.PrivateKey.ᏑPublicKey), hash, hashed, sig, Ꮡopts);
        if ((err == default!) != test.good) {
            Ꮡt.Errorf("#%d: bad result, wanted: %t, got: %s"u8, i, test.good, err);
        }
    }
}

public static void TestPSS513(ж<testing.T> Ꮡt) {
    // See Issue 42741, and separately, RFC 8017: "Note that the octet length of
    // EM will be one less than k if modBits - 1 is divisible by 8 and equal to
    // k otherwise, where k is the length in octets of the RSA modulus n."
    var (key, err) = GenerateKey(rand.Reader, 513);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    var digest = sha256.Sum256(slice<byte>("message"u8));
    (var signature, err) = key.Sign(rand.Reader, digest[..], new rsa_test_package.rsa_PSSOptionsжSignerOpts(Ꮡ(new PSSOptions(
        SaltLength: PSSSaltLengthAuto,
        Hash: crypto.SHA256
    ))));
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    err = VerifyPSS(key.of(rsa.PrivateKey.ᏑPublicKey), crypto.SHA256, digest[..], signature, nil);
    if (err != default!) {
        Ꮡt.Error(err);
    }
}

internal static ж<bigꓸInt> bigFromHex(@string hex) {
    var (n, ok) = @new<bigꓸInt>().SetString(hex, 16);
    if (!ok) {
        throw panic("bad hex: " + hex);
    }
    return n;
}

internal static nint intFromHex(@string hex) {
    var (i, err) = strconv.ParseInt(hex, 16, 32);
    if (err != default!) {
        throw panic(err);
    }
    return (nint)i;
}

internal static slice<byte> fromHex(@string hexStr) {
    var (s, err) = hex.DecodeString(hexStr);
    if (err != default!) {
        throw panic(err);
    }
    return s;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object verifyPSSUnexpectedˢ = (@string)"VerifyPSS unexpected success"u8;

public static void TestInvalidPSSSaltLength(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    var (key, err) = GenerateKey(rand.Reader, 245);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    var digest = sha256.Sum256(slice<byte>("message"u8));
    // We don't check the exact error matches, because crypto/rsa and crypto/internal/boring
    // return two different error variables, which have the same content but are not equal.
    {
        var (_, errΔ1) = SignPSS(rand.Reader, key, crypto.SHA256, digest[..], Ꮡ(new PSSOptions(
            SaltLength: -2,
            Hash: crypto.SHA256
        ))); if (errΔ1.Error() != rsa_internal_test_package.InvalidSaltLenErr.Error()) {
            Ꮡt.Fatalf("SignPSS unexpected error: got %v, want %v"u8, errΔ1, rsa_internal_test_package.InvalidSaltLenErr);
        }
    }
    // We don't check the specific error here, because crypto/rsa and crypto/internal/boring
    // return different errors, so we just check that _an error_ was returned.
    {
        var errΔ2 = VerifyPSS(key.of(rsa.PrivateKey.ᏑPublicKey), crypto.SHA256, new byte[]{1, 2, 3}.slice(), new slice<byte>(31), Ꮡ(new PSSOptions(
            SaltLength: -2
        ))); if (errΔ2 == default!) {
            Ꮡt.Fatal(verifyPSSUnexpectedˢ);
        }
    }
}

} // end rsa_test_package
