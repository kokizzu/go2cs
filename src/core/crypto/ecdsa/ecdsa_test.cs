// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bufio = bufio_package;
using bytes = bytes_package;
using bzip2 = compress.bzip2_package;
using elliptic = go.crypto.elliptic_package;
using bigmod = go.crypto.@internal.bigmod_package;
using rand = go.crypto.rand_package;
using sha1 = go.crypto.sha1_package;
using sha256 = go.crypto.sha256_package;
using sha512 = go.crypto.sha512_package;
using hex = encoding.hex_package;
using hash = hash_package;
using io = io_package;
using big = math.big_package;
using os = os_package;
using strings = strings_package;
using testing = testing_package;
using compress;
using encoding;
using go.crypto;
using go.crypto.@internal;
using math;
using nistec = go.crypto.@internal.nistec_package;
using static go.crypto.ecdsa_package;

partial class ecdsa_internal_test_package {

[GoType("dyn")] partial struct testAllCurves_tests {
    internal @string name;
    internal elliptic.Curve curve;
}

internal static void testAllCurves(ж<testing.T> Ꮡt, Action<ж<testing.T>, elliptic.Curve> f) {
    var tests = new testAllCurves_tests[]{
        new("P256"u8, elliptic.P256()),
        new("P224"u8, elliptic.P224()),
        new("P384"u8, elliptic.P384()),
        new("P521"u8, elliptic.P521()),
        new("P256/Generic"u8, new elliptic.CurveParamsжCurve(genericParamsForCurve(elliptic.P256())))
    }.slice();
    if (testing.Short()) {
        tests = tests[..1];
    }
    foreach (var (_, test) in tests) {
        var curve = test.curve;
        var curveʗ1 = curve;
        Ꮡt.Run(test.name, (ж<testing.T> tΔ1) => {
            tΔ1.Parallel();
            f(tΔ1, curveʗ1);
        });
    }
}

// genericParamsForCurve returns the dereferenced CurveParams for
// the specified curve. This is used to avoid the logic for
// upgrading a curve to its specific implementation, forcing
// usage of the generic implementation.
internal static ж<elliptic.CurveParams> genericParamsForCurve(elliptic.Curve c) {
    ref var d = ref heap<elliptic.CurveParams>(out var Ꮡd);
    d = (c.Params()).Value;
    return Ꮡd;
}

public static void TestKeyGeneration(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testKeyGeneration);
}

internal static void testKeyGeneration(ж<testing.T> Ꮡt, elliptic.Curve c) {
    var (priv, err) = GenerateKey(c, rand.Reader);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    if (!c.IsOnCurve((~priv).PublicKey.X, (~priv).PublicKey.Y)) {
        Ꮡt.Errorf("public key invalid: %s"u8, err);
    }
}

public static void TestSignAndVerify(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testSignAndVerify);
}

internal static void testSignAndVerify(ж<testing.T> Ꮡt, elliptic.Curve c) {
    var (priv, _) = GenerateKey(c, rand.Reader);
    var hashed = slice<byte>("testing"u8);
    var (r, s, err) = Sign(rand.Reader, priv, hashed);
    if (err != default!) {
        Ꮡt.Errorf("error signing: %s"u8, err);
        return;
    }
    if (!Verify(priv.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), hashed, r, s)) {
        Ꮡt.Errorf("Verify failed"u8);
    }
    hashed[0] ^= (byte)(0xff);
    if (Verify(priv.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), hashed, r, s)) {
        Ꮡt.Errorf("Verify always works!"u8);
    }
}

public static void TestSignAndVerifyASN1(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testSignAndVerifyASN1);
}

internal static void testSignAndVerifyASN1(ж<testing.T> Ꮡt, elliptic.Curve c) {
    var (priv, _) = GenerateKey(c, rand.Reader);
    var hashed = slice<byte>("testing"u8);
    var (sig, err) = SignASN1(rand.Reader, priv, hashed);
    if (err != default!) {
        Ꮡt.Errorf("error signing: %s"u8, err);
        return;
    }
    if (!VerifyASN1(priv.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), hashed, sig)) {
        Ꮡt.Errorf("VerifyASN1 failed"u8);
    }
    hashed[0] ^= (byte)(0xff);
    if (VerifyASN1(priv.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), hashed, sig)) {
        Ꮡt.Errorf("VerifyASN1 always works!"u8);
    }
}

public static void TestNonceSafety(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testNonceSafety);
}

internal static void testNonceSafety(ж<testing.T> Ꮡt, elliptic.Curve c) {
    var (priv, _) = GenerateKey(c, rand.Reader);
    var hashed = slice<byte>("testing"u8);
    var (r0, s0, err) = Sign(zeroReader, priv, hashed);
    if (err != default!) {
        Ꮡt.Errorf("error signing: %s"u8, err);
        return;
    }
    hashed = slice<byte>("testing..."u8);
    (var r1, var s1, err) = Sign(zeroReader, priv, hashed);
    if (err != default!) {
        Ꮡt.Errorf("error signing: %s"u8, err);
        return;
    }
    if (s0.Cmp(s1) == 0) {
        // This should never happen.
        Ꮡt.Errorf("the signatures on two different messages were the same"u8);
    }
    if (r0.Cmp(r1) == 0) {
        Ꮡt.Errorf("the nonce used for two different messages was the same"u8);
    }
}

public static void TestINDCCA(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testINDCCA);
}

internal static void testINDCCA(ж<testing.T> Ꮡt, elliptic.Curve c) {
    var (priv, _) = GenerateKey(c, rand.Reader);
    var hashed = slice<byte>("testing"u8);
    var (r0, s0, err) = Sign(rand.Reader, priv, hashed);
    if (err != default!) {
        Ꮡt.Errorf("error signing: %s"u8, err);
        return;
    }
    (var r1, var s1, err) = Sign(rand.Reader, priv, hashed);
    if (err != default!) {
        Ꮡt.Errorf("error signing: %s"u8, err);
        return;
    }
    if (s0.Cmp(s1) == 0) {
        Ꮡt.Errorf("two signatures of the same message produced the same result"u8);
    }
    if (r0.Cmp(r1) == 0) {
        Ꮡt.Errorf("two signatures of the same message produced the same nonce"u8);
    }
}

internal static ж<bigꓸInt> fromHex(@string s) {
    var (r, ok) = @new<bigꓸInt>().SetString(s, 16);
    if (!ok) {
        throw panic("bad hex");
    }
    return r;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string testdataSigVerRspBz2ˢ = "testdata/SigVer.rsp.bz2"u8;
internal static readonly @string msgˢ = "Msg = "u8;
internal static readonly @string resultˢ = "Result = "u8;

public static void TestVectors(ж<testing.T> Ꮡt) {
    // This test runs the full set of NIST test vectors from
    // https://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3ecdsatestvectors.zip
    //
    // The SigVer.rsp file has been edited to remove test vectors for
    // unsupported algorithms and has been compressed.
    if (testing.Short()) {
        return;
    }
    var (f, err) = os.Open(testdataSigVerRspBz2ˢ);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    var buf = bufio.NewReader(bzip2.NewReader(new ecdsa_test_package.os_FileжReader(f)));
    nint lineNo = 1;
    hash.Hash h = default!;
    slice<byte> msg = default!;
    slice<byte> hashed = default!;
    ж<bigꓸInt> r = default!;
    ж<bigꓸInt> s = default!;
    var pub = @new<global::go.crypto.ecdsa_package.PublicKey>();
    while (ᐧ) {
        var (line, errΔ1) = buf.ReadString((rune)'\n');
        if (len(line) == 0) {
            if (AreEqual(errΔ1, io.EOF)) {
                break;
            }
            Ꮡt.Fatalf("error reading from input: %s"u8, errΔ1);
        }
        lineNo++;
        // Need to remove \r\n from the end of the line.
        if (!strings.HasSuffix(line, "\r\n"u8)) {
            Ꮡt.Fatalf("bad line ending (expected \\r\\n) on line %d"u8, lineNo);
        }
        line = line[..(int)(len(line) - 2)];
        if (len(line) == 0 || line[0] == (rune)'#') {
            continue;
        }
        if (line[0] == (rune)'[') {
            line = line[1..(int)(len(line) - 1)];
            var (curve, hashΔ1, _) = strings.Cut(line, ","u8);
            var exprᴛ1 = curve;
            if (exprᴛ1 == "P-224"u8) {
                pub.Value.Curve = elliptic.P224();
            }
            else if (exprᴛ1 == "P-256"u8) {
                pub.Value.Curve = elliptic.P256();
            }
            else if (exprᴛ1 == "P-384"u8) {
                pub.Value.Curve = elliptic.P384();
            }
            else if (exprᴛ1 == "P-521"u8) {
                pub.Value.Curve = elliptic.P521();
            }
            else { /* default: */
                pub.Value.Curve = default!;
            }

            var exprᴛ2 = hashΔ1;
            if (exprᴛ2 == "SHA-1"u8) {
                h = sha1.New();
            }
            else if (exprᴛ2 == "SHA-224"u8) {
                h = sha256.New224();
            }
            else if (exprᴛ2 == "SHA-256"u8) {
                h = sha256.New();
            }
            else if (exprᴛ2 == "SHA-384"u8) {
                h = sha512.New384();
            }
            else if (exprᴛ2 == "SHA-512"u8) {
                h = sha512.New();
            }
            else { /* default: */
                h = default!;
            }

            continue;
        }
        if (h == default! || (~pub).Curve == default!) {
            continue;
        }
        switch (ᐧ) {
        case {} when strings.HasPrefix(line, msgˢ): {
            {
                (msg, errΔ1) = hex.DecodeString(line[6..]); if (errΔ1 != default!) {
                    Ꮡt.Fatalf("failed to decode message on line %d: %s"u8, lineNo, errΔ1);
                }
            }
            break;
        }
        case {} when strings.HasPrefix(line, "Qx = "u8): {
            pub.Value.X = fromHex(line[5..]);
            break;
        }
        case {} when strings.HasPrefix(line, "Qy = "u8): {
            pub.Value.Y = fromHex(line[5..]);
            break;
        }
        case {} when strings.HasPrefix(line, "R = "u8): {
            r = fromHex(line[4..]);
            break;
        }
        case {} when strings.HasPrefix(line, "S = "u8): {
            s = fromHex(line[4..]);
            break;
        }
        case {} when strings.HasPrefix(line, resultˢ): {
            var expected = line[9] == (rune)'P';
            h.Reset();
            h.Write(msg);
            var hashedΔ2 = h.Sum(hashed[..0]);
            if (Verify(pub, hashedΔ2, r, s) != expected) {
                Ꮡt.Fatalf("incorrect result on line %d"u8, lineNo);
            }
            break;
        }
        default: {
            Ꮡt.Fatalf("unknown variable on line %d: %s"u8, lineNo, line);
            break;
        }}

    }
}

public static void TestNegativeInputs(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testNegativeInputs);
}

internal static void testNegativeInputs(ж<testing.T> Ꮡt, elliptic.Curve curve) {
    var (key, err) = GenerateKey(curve, rand.Reader);
    if (err != default!) {
        Ꮡt.Errorf("failed to generate key"u8);
    }
    array<byte> hash = new(32);
    var r = @new<bigꓸInt>().SetInt64(1);
    r.Lsh(r, 550);
    /* larger than any supported curve */
    r.Neg(r);
    if (Verify(key.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), hash[..], r, r)) {
        Ꮡt.Errorf("bogus signature accepted"u8);
    }
}

public static void TestZeroHashSignature(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testZeroHashSignature);
}

internal static void testZeroHashSignature(ж<testing.T> Ꮡt, elliptic.Curve curve) {
    var zeroHash = new slice<byte>(64);
    var (privKey, err) = GenerateKey(curve, rand.Reader);
    if (err != default!) {
        throw panic(err);
    }
    // Sign a hash consisting of all zeros.
    (var r, var s, err) = Sign(rand.Reader, privKey, zeroHash);
    if (err != default!) {
        throw panic(err);
    }
    // Confirm that it can be verified.
    if (!Verify(privKey.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), zeroHash, r, s)) {
        Ꮡt.Errorf("zero hash signature verify failed for %T"u8, curve);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string p224ˢ = "P-224"u8;
internal static readonly @string p256ˢ = "P-256"u8;
internal static readonly @string p384ˢ = "P-384"u8;
internal static readonly @string p521ˢ = "P-521"u8;

public static void TestRandomPoint(ж<testing.T> Ꮡt) {
    Ꮡt.Run(p224ˢ, (ж<testing.T> tΔ1) => {
        testRandomPoint<P224PointжnistPoint>(tΔ1, p224());
    });
    Ꮡt.Run(p256ˢ, (ж<testing.T> tΔ2) => {
        testRandomPoint<P256PointжnistPoint>(tΔ2, p256());
    });
    Ꮡt.Run(p384ˢ, (ж<testing.T> tΔ3) => {
        testRandomPoint<P384PointжnistPoint>(tΔ3, p384());
    });
    Ꮡt.Run(p521ˢ, (ж<testing.T> tΔ4) => {
        testRandomPoint<P521PointжnistPoint>(tΔ4, p521());
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object kIsZeroˢ = (@string)"k is zero"u8;
internal static readonly object pIsInfinityˢ = (@string)"p is infinity"u8;
internal static readonly object overflowWasNotRejectedˢ = (@string)"overflow was not rejected"u8;
internal static readonly object zeroWasNotRejectedˢ = (@string)"zero was not rejected"u8;
internal static readonly object unexpectedRejectionˢ = (@string)"unexpected rejection"u8;

internal static void testRandomPoint<Point>(ж<testing.T> Ꮡt, ж<global::go.crypto.ecdsa_package.nistCurve<Point>> Ꮡc)
    where Point : global::go.crypto.ecdsa_package.nistPoint<Point>
{
    ref var c = ref Ꮡc.DerefOrNull();

    Ꮡt.Cleanup(() => {
        testingOnlyRejectionSamplingLooped = default!;
    });
    nint loopCount = default!;
    testingOnlyRejectionSamplingLooped = () => {
        loopCount++;
    };
    // A sequence of all ones will generate 2^N-1, which should be rejected.
    // (Unless, for example, we are masking too many bits.)
    var r = io.MultiReader(new ecdsa_test_package.bytes_ReaderжReader(bytes.NewReader(bytes.Repeat(new byte[]{0xff}.slice(), 100))), rand.Reader);
    {
        var (k, p, err) = randomPoint(ref (Ꮡc).DerefOrNull(), r); if (err != default!){
            Ꮡt.Fatal(err);
        } else 
        if (k.IsZero() == 1){
            Ꮡt.Error(kIsZeroˢ);
        } else 
        if (p.Bytes()[0] != 4) {
            Ꮡt.Error(pIsInfinityˢ);
        }
    }
    if (loopCount == 0) {
        Ꮡt.Error(overflowWasNotRejectedˢ);
    }
    loopCount = 0;
    // A sequence of all zeroes will generate zero, which should be rejected.
    r = io.MultiReader(new ecdsa_test_package.bytes_ReaderжReader(bytes.NewReader(bytes.Repeat(new byte[]{0}.slice(), 100))), rand.Reader);
    {
        var (k, p, err) = randomPoint(ref (Ꮡc).DerefOrNull(), r); if (err != default!){
            Ꮡt.Fatal(err);
        } else 
        if (k.IsZero() == 1){
            Ꮡt.Error(kIsZeroˢ);
        } else 
        if (p.Bytes()[0] != 4) {
            Ꮡt.Error(pIsInfinityˢ);
        }
    }
    if (loopCount == 0) {
        Ꮡt.Error(zeroWasNotRejectedˢ);
    }
    loopCount = 0;
    // P-256 has a 2⁻³² chance or randomly hitting a rejection. For P-224 it's
    // 2⁻¹¹², for P-384 it's 2⁻¹⁹⁴, and for P-521 it's 2⁻²⁶², so if we hit in
    // tests, something is horribly wrong. (For example, we are masking the
    // wrong bits.)
    if (AreEqual(c.curve, elliptic.P256())) {
        return;
    }
    {
        var (k, p, err) = randomPoint(ref (Ꮡc).DerefOrNull(), rand.Reader); if (err != default!){
            Ꮡt.Fatal(err);
        } else 
        if (k.IsZero() == 1){
            Ꮡt.Error(kIsZeroˢ);
        } else 
        if (p.Bytes()[0] != 4) {
            Ꮡt.Error(pIsInfinityˢ);
        }
    }
    if (loopCount > 0) {
        Ꮡt.Error(unexpectedRejectionˢ);
    }
}

public static void TestHashToNat(ж<testing.T> Ꮡt) {
    Ꮡt.Run(p224ˢ, (ж<testing.T> tΔ1) => {
        testHashToNat<P224PointжnistPoint>(tΔ1, p224());
    });
    Ꮡt.Run(p256ˢ, (ж<testing.T> tΔ2) => {
        testHashToNat<P256PointжnistPoint>(tΔ2, p256());
    });
    Ꮡt.Run(p384ˢ, (ж<testing.T> tΔ3) => {
        testHashToNat<P384PointжnistPoint>(tΔ3, p384());
    });
    Ꮡt.Run(p521ˢ, (ж<testing.T> tΔ4) => {
        testHashToNat<P521PointжnistPoint>(tΔ4, p521());
    });
}

internal static void testHashToNat<Point>(ж<testing.T> Ꮡt, ж<global::go.crypto.ecdsa_package.nistCurve<Point>> Ꮡc)
    where Point : global::go.crypto.ecdsa_package.nistPoint<Point>
{
    for (nint l = 0; l < 600; l++) {
        var h = bytes.Repeat(new byte[]{0xff}.slice(), l);
        hashToNat(ref (Ꮡc).DerefOrNull(), bigmod.NewNat(), h);
    }
}

public static void TestZeroSignature(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testZeroSignature);
}

internal static void testZeroSignature(ж<testing.T> Ꮡt, elliptic.Curve curve) {
    var (privKey, err) = GenerateKey(curve, rand.Reader);
    if (err != default!) {
        throw panic(err);
    }
    if (Verify(privKey.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), new slice<byte>(64), big.NewInt(0), big.NewInt(0))) {
        Ꮡt.Errorf("Verify with r,s=0 succeeded: %T"u8, curve);
    }
}

public static void TestNegativeSignature(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testNegativeSignature);
}

internal static void testNegativeSignature(ж<testing.T> Ꮡt, elliptic.Curve curve) {
    var zeroHash = new slice<byte>(64);
    var (privKey, err) = GenerateKey(curve, rand.Reader);
    if (err != default!) {
        throw panic(err);
    }
    (var r, var s, err) = Sign(rand.Reader, privKey, zeroHash);
    if (err != default!) {
        throw panic(err);
    }
    r = r.Neg(r);
    if (Verify(privKey.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), zeroHash, r, s)) {
        Ꮡt.Errorf("Verify with r=-r succeeded: %T"u8, curve);
    }
}

public static void TestRPlusNSignature(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testRPlusNSignature);
}

internal static void testRPlusNSignature(ж<testing.T> Ꮡt, elliptic.Curve curve) {
    var zeroHash = new slice<byte>(64);
    var (privKey, err) = GenerateKey(curve, rand.Reader);
    if (err != default!) {
        throw panic(err);
    }
    (var r, var s, err) = Sign(rand.Reader, privKey, zeroHash);
    if (err != default!) {
        throw panic(err);
    }
    r = r.Add(r, (~curve.Params()).N);
    if (Verify(privKey.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), zeroHash, r, s)) {
        Ꮡt.Errorf("Verify with r=r+n succeeded: %T"u8, curve);
    }
}

public static void TestRMinusNSignature(ж<testing.T> Ꮡt) {
    testAllCurves(Ꮡt, testRMinusNSignature);
}

internal static void testRMinusNSignature(ж<testing.T> Ꮡt, elliptic.Curve curve) {
    var zeroHash = new slice<byte>(64);
    var (privKey, err) = GenerateKey(curve, rand.Reader);
    if (err != default!) {
        throw panic(err);
    }
    (var r, var s, err) = Sign(rand.Reader, privKey, zeroHash);
    if (err != default!) {
        throw panic(err);
    }
    r = r.Sub(r, (~curve.Params()).N);
    if (Verify(privKey.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), zeroHash, r, s)) {
        Ꮡt.Errorf("Verify with r=r-n succeeded: %T"u8, curve);
    }
}

internal static error randomPointForCurve(elliptic.Curve curve, io.Reader rand) {
    var exprᴛ1 = curve.Params();
    if (exprᴛ1 == elliptic.P224().Params()) {
        var (_, _, err) = randomPoint<P224PointжnistPoint>(ref (p224()).DerefOrNull(), rand);
        return err;
    }
    if (exprᴛ1 == elliptic.P256().Params()) {
        var (_, _, err) = randomPoint<P256PointжnistPoint>(ref (p256()).DerefOrNull(), rand);
        return err;
    }
    if (exprᴛ1 == elliptic.P384().Params()) {
        var (_, _, err) = randomPoint<P384PointжnistPoint>(ref (p384()).DerefOrNull(), rand);
        return err;
    }
    if (exprᴛ1 == elliptic.P521().Params()) {
        var (_, _, err) = randomPoint<P521PointжnistPoint>(ref (p521()).DerefOrNull(), rand);
        return err;
    }
    { /* default: */
        throw panic("unknown curve");
    }

}

[GoType("dyn")] partial struct benchmarkAllCurves_tests {
    internal @string name;
    internal elliptic.Curve curve;
}

internal static void benchmarkAllCurves(ж<testing.B> Ꮡb, Action<ж<testing.B>, elliptic.Curve> f) {
    var tests = new benchmarkAllCurves_tests[]{
        new("P256"u8, elliptic.P256()),
        new("P384"u8, elliptic.P384()),
        new("P521"u8, elliptic.P521())
    }.slice();
    foreach (var (_, test) in tests) {
        var curve = test.curve;
        var curveʗ1 = curve;
        Ꮡb.Run(test.name, (ж<testing.B> bΔ1) => {
            f(bΔ1, curveʗ1);
        });
    }
}

public static void BenchmarkSign(ж<testing.B> Ꮡb) {
    benchmarkAllCurves(Ꮡb, (ж<testing.B> bΔ1, elliptic.Curve curve) => {
        var r = bufio.NewReaderSize(rand.Reader, (1 << (int)(15)));
        var (priv, err) = GenerateKey(curve, new ecdsa_test_package.bufio_ReaderжReader(r));
        if (err != default!) {
            bΔ1.Fatal(err);
        }
        var hashed = slice<byte>("testing"u8);
        bΔ1.ReportAllocs();
        bΔ1.ResetTimer();
        for (nint i = 0; i < (~bΔ1).N; i++) {
            var (sig, errΔ1) = SignASN1(new ecdsa_test_package.bufio_ReaderжReader(r), priv, hashed);
            if (errΔ1 != default!) {
                bΔ1.Fatal(errΔ1);
            }
            // Prevent the compiler from optimizing out the operation.
            hashed[0] = sig[0];
        }
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object verifyFailedˢ = (@string)"verify failed"u8;

public static void BenchmarkVerify(ж<testing.B> Ꮡb) {
    benchmarkAllCurves(Ꮡb, (ж<testing.B> bΔ1, elliptic.Curve curve) => {
        var r = bufio.NewReaderSize(rand.Reader, (1 << (int)(15)));
        var (priv, err) = GenerateKey(curve, new ecdsa_test_package.bufio_ReaderжReader(r));
        if (err != default!) {
            bΔ1.Fatal(err);
        }
        var hashed = slice<byte>("testing"u8);
        (var sig, err) = SignASN1(new ecdsa_test_package.bufio_ReaderжReader(r), priv, hashed);
        if (err != default!) {
            bΔ1.Fatal(err);
        }
        bΔ1.ReportAllocs();
        bΔ1.ResetTimer();
        for (nint i = 0; i < (~bΔ1).N; i++) {
            if (!VerifyASN1(priv.of(global::go.crypto.ecdsa_package.PrivateKey.ᏑPublicKey), hashed, sig)) {
                bΔ1.Fatal(verifyFailedˢ);
            }
        }
    });
}

public static void BenchmarkGenerateKey(ж<testing.B> Ꮡb) {
    benchmarkAllCurves(Ꮡb, (ж<testing.B> bΔ1, elliptic.Curve curve) => {
        var r = bufio.NewReaderSize(rand.Reader, (1 << (int)(15)));
        bΔ1.ReportAllocs();
        bΔ1.ResetTimer();
        for (nint i = 0; i < (~bΔ1).N; i++) {
            {
                var (_, err) = GenerateKey(curve, new ecdsa_test_package.bufio_ReaderжReader(r)); if (err != default!) {
                    bΔ1.Fatal(err);
                }
            }
        }
    });
}

} // end ecdsa_internal_test_package
