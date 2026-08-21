// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto.@internal;

using bytes = bytes_package;
using rand = go.crypto.rand_package;
// blank import: embed_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
using hex = encoding.hex_package;
using errors = errors_package;
using flag = flag_package;
using big = math.big_package;
using strconv = strconv_package;
using testing = testing_package;
using sha3 = vendor.golang.org.x.crypto.sha3_package;
using encoding;
using go.crypto;
using math;
using static go.crypto.@internal.mlkem768_package;
using vendor.golang.org.x.crypto;

partial class mlkem768_internal_test_package {

// Go runs a blank-imported package's `init` before this package's own; .NET would never
// load an assembly nothing references, so the side effects the import exists for are forced.
[GoInit] internal static void initᴛᴛblankImportꓸembed() {
    builtin.initPackage(typeof(embed_package));
}

public static void TestFieldReduce(ж<testing.T> Ꮡt) {
    for (var a = (uint32)0; a < 2 * q * q; a++) {
        var got = fieldReduce(a);
        var exp = ((global::go.crypto.@internal.mlkem768_package.fieldElement)(uint16)(a % (uint32)q));
        if (got != exp) {
            Ꮡt.Fatalf("reduce(%d) = %d, expected %d"u8, a, got, exp);
        }
    }
}

public static void TestFieldAdd(ж<testing.T> Ꮡt) {
    for (var a = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); a < q; a++) {
        for (var b = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); b < q; b++) {
            var got = fieldAdd(a, b);
            var exp = (global::go.crypto.@internal.mlkem768_package.fieldElement)((a + b) % (uint16)q);
            if (got != exp) {
                Ꮡt.Fatalf("%d + %d = %d, expected %d"u8, a, b, got, exp);
            }
        }
    }
}

public static void TestFieldSub(ж<testing.T> Ꮡt) {
    for (var a = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); a < q; a++) {
        for (var b = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); b < q; b++) {
            var got = fieldSub(a, b);
            var exp = (global::go.crypto.@internal.mlkem768_package.fieldElement)((a - b + (uint16)q) % (uint16)q);
            if (got != exp) {
                Ꮡt.Fatalf("%d - %d = %d, expected %d"u8, a, b, got, exp);
            }
        }
    }
}

public static void TestFieldMul(ж<testing.T> Ꮡt) {
    for (var a = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); a < q; a++) {
        for (var b = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); b < q; b++) {
            var got = fieldMul(a, b);
            var exp = ((global::go.crypto.@internal.mlkem768_package.fieldElement)(uint16)(((uint32)(uint16)a * (uint32)(uint16)b) % (uint32)q));
            if (got != exp) {
                Ꮡt.Fatalf("%d * %d = %d, expected %d"u8, a, b, got, exp);
            }
        }
    }
}

public static void TestDecompressCompress(ж<testing.T> Ꮡt) {
    foreach (var (_, bits) in new uint8[]{1, 4, 10}.slice()) {
        for (var a = (uint16)0; a < (uint16)(((uint16)1).Lsh((uint64)(bits))); a++) {
            var f = decompress(a, bits);
            if (f >= q) {
                Ꮡt.Fatalf("decompress(%d, %d) = %d >= q"u8, a, bits, f);
            }
            var got = compress(f, bits);
            if (got != a) {
                Ꮡt.Fatalf("compress(decompress(%d, %d), %d) = %d"u8, a, bits, bits, got);
            }
        }
        for (var a = ((global::go.crypto.@internal.mlkem768_package.fieldElement)0); a < q; a++) {
            var c = compress(a, bits);
            if (c >= (uint16)(((uint16)1).Lsh((uint64)(bits)))) {
                Ꮡt.Fatalf("compress(%d, %d) = %d >= 2^bits"u8, a, bits, c);
            }
            var got = decompress(c, bits);
            var diff = min((global::go.crypto.@internal.mlkem768_package.fieldElement)(a - got), (global::go.crypto.@internal.mlkem768_package.fieldElement)(got - a), (global::go.crypto.@internal.mlkem768_package.fieldElement)(a - got + (uint16)q), (global::go.crypto.@internal.mlkem768_package.fieldElement)(got - a + (uint16)q));
            nint ceil = (nint)q / (((nint)1).Lsh((uint64)(bits)));
            if (diff > ((global::go.crypto.@internal.mlkem768_package.fieldElement)(uint16)ceil)) {
                Ꮡt.Fatalf("decompress(compress(%d, %d), %d) = %d (diff %d, max diff %d)"u8,
                    a, bits, bits, got, diff, ceil);
            }
        }
    }
}

internal static uint16 CompressRat(global::go.crypto.@internal.mlkem768_package.fieldElement x, uint8 d) {
    if (x >= q) {
        throw panic("x out of range");
    }
    if (d <= 0 || d >= 12) {
        throw panic("d out of range");
    }
    var precise = big.NewRat((((int64)1).Lsh((uint64)(d))) * (int64)(uint16)x, q); // (2ᵈ / q) * x == (2ᵈ * x) / q
    // FloatString rounds halves away from 0, and our result should always be positive,
    // so it should work as we expect. (There's no direct way to round a Rat.)
    var (rounded, err) = strconv.ParseInt(precise.FloatString(0), 10, 64);
    if (err != default!) {
        throw panic(err);
    }
    // If we rounded up, `rounded` may be equal to 2ᵈ, so we perform a final reduction.
    return (uint16)(rounded % (((int64)1).Lsh((uint64)(d))));
}

public static void TestCompress(ж<testing.T> Ꮡt) {
    for (nint d = 1; d < 12; d++) {
        for (nint n = 0; n < q; n++) {
            var expected = CompressRat(((global::go.crypto.@internal.mlkem768_package.fieldElement)(uint16)n), (uint8)d);
            var result = compress(((global::go.crypto.@internal.mlkem768_package.fieldElement)(uint16)n), (uint8)d);
            if (result != expected) {
                Ꮡt.Errorf("compress(%d, %d): got %d, expected %d"u8, n, d, result, expected);
            }
        }
    }
}

internal static global::go.crypto.@internal.mlkem768_package.fieldElement DecompressRat(uint16 y, uint8 d) {
    if (y >= (uint16)(((uint16)1).Lsh((uint64)(d)))) {
        throw panic("y out of range");
    }
    if (d <= 0 || d >= 12) {
        throw panic("d out of range");
    }
    var precise = big.NewRat((int64)q * (int64)y, ((int64)1).Lsh((uint64)(d))); // (q / 2ᵈ) * y  ==  (q * y) / 2ᵈ
    // FloatString rounds halves away from 0, and our result should always be positive,
    // so it should work as we expect. (There's no direct way to round a Rat.)
    var (rounded, err) = strconv.ParseInt(precise.FloatString(0), 10, 64);
    if (err != default!) {
        throw panic(err);
    }
    // If we rounded up, `rounded` may be equal to q, so we perform a final reduction.
    return ((global::go.crypto.@internal.mlkem768_package.fieldElement)(uint16)(rounded % (int64)q));
}

public static void TestDecompress(ж<testing.T> Ꮡt) {
    for (nint d = 1; d < 12; d++) {
        for (nint n = 0; n < (((nint)1).Lsh((uint64)(d))); n++) {
            var expected = DecompressRat((uint16)n, (uint8)d);
            var result = decompress((uint16)n, (uint8)d);
            if (result != expected) {
                Ꮡt.Errorf("decompress(%d, %d): got %d, expected %d"u8, n, d, result, expected);
            }
        }
    }
}

public static uint8 BitRev7(uint8 n) {
    if ((uint8)((n >> (int)(7))) != 0) {
        throw panic("not 7 bits");
    }
    uint8 r = default!;
    r |= (uint8)((uint8)((n >> (int)(6)) & 0b0000_0001));
    r |= (uint8)((uint8)((n >> (int)(4)) & 0b0000_0010));
    r |= (uint8)((uint8)((n >> (int)(2)) & 0b0000_0100));
    r |= (uint8)((uint8)(n & 0b0000_1000));
    /**/
    r |= (uint8)((uint8)((uint8)(n << (int)(2)) & 0b0001_0000));
    r |= (uint8)((uint8)((uint8)(n << (int)(4)) & 0b0010_0000));
    r |= (uint8)((uint8)((uint8)(n << (int)(6)) & 0b0100_0000));
    return r;
}

public static void TestZetas(ж<testing.T> Ꮡt) {
    var ζ = big.NewInt(17);
    var q = big.NewInt(mlkem768_package.q);
    foreach (var (k, zeta) in zetas) {
        // ζ^BitRev7(k) mod q
        var exp = @new<bigꓸInt>().Exp(ζ, big.NewInt((int64)BitRev7((uint8)k)), q);
        if (big.NewInt((int64)(uint16)zeta).Cmp(exp) != 0) {
            Ꮡt.Errorf("zetas[%d] = %v, expected %v"u8, k, zeta, exp.OrTypedNil());
        }
    }
}

public static void TestGammas(ж<testing.T> Ꮡt) {
    var ζ = big.NewInt(17);
    var q = big.NewInt(mlkem768_package.q);
    foreach (var (k, gamma) in gammas) {
        // ζ^2BitRev7(i)+1
        var exp = @new<bigꓸInt>().Exp(ζ, big.NewInt((int64)BitRev7((uint8)k) * 2 + 1), q);
        if (big.NewInt((int64)(uint16)gamma).Cmp(exp) != 0) {
            Ꮡt.Errorf("gammas[%d] = %v, expected %v"u8, k, gamma, exp.OrTypedNil());
        }
    }
}

public static void TestRoundTrip(ж<testing.T> Ꮡt) {
    var (dk, err) = GenerateKey();
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    (var c, var Ke, err) = Encapsulate(dk.EncapsulationKey());
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    (var Kd, err) = Decapsulate(dk, c);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    if (!bytes.Equal(Ke, Kd)) {
        Ꮡt.Fail();
    }
    (var dk1, err) = GenerateKey();
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    if (bytes.Equal(dk.EncapsulationKey(), dk1.EncapsulationKey())) {
        Ꮡt.Fail();
    }
    if (bytes.Equal(dk.Bytes(), dk1.Bytes())) {
        Ꮡt.Fail();
    }
    if (bytes.Equal(dk.Bytes()[(int)(EncapsulationKeySize - 32)..], dk1.Bytes()[(int)(EncapsulationKeySize - 32)..])) {
        Ꮡt.Fail();
    }
    (var c1, var Ke1, err) = Encapsulate(dk.EncapsulationKey());
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    if (bytes.Equal(c, c1)) {
        Ꮡt.Fail();
    }
    if (bytes.Equal(Ke, Ke1)) {
        Ꮡt.Fail();
    }
}

public static void TestBadLengths(ж<testing.T> Ꮡt) {
    var (dk, err) = GenerateKey();
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    var ek = dk.EncapsulationKey();
    for (nint i = 0; i < len(ek) - 1; i++) {
        {
            var (_, _, errΔ1) = Encapsulate(ek[..(int)(i)]); if (errΔ1 == default!) {
                Ꮡt.Errorf("expected error for ek length %d"u8, i);
            }
        }
    }
    var ekLong = ek;
    for (nint i = 0; i < 100; i++) {
        ekLong = append(ekLong, (byte)(0));
        {
            var (_, _, errΔ2) = Encapsulate(ekLong); if (errΔ2 == default!) {
                Ꮡt.Errorf("expected error for ek length %d"u8, len(ekLong));
            }
        }
    }
    (var c, _, err) = Encapsulate(ek);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    for (nint i = 0; i < len(dk.Bytes()) - 1; i++) {
        {
            var (_, errΔ3) = NewKeyFromExtendedEncoding(dk.Bytes()[..(int)(i)]); if (errΔ3 == default!) {
                Ꮡt.Errorf("expected error for dk length %d"u8, i);
            }
        }
    }
    var dkLong = dk.Bytes();
    for (nint i = 0; i < 100; i++) {
        dkLong = append(dkLong, (byte)(0));
        {
            var (_, errΔ4) = NewKeyFromExtendedEncoding(dkLong); if (errΔ4 == default!) {
                Ꮡt.Errorf("expected error for dk length %d"u8, len(dkLong));
            }
        }
    }
    for (nint i = 0; i < len(c) - 1; i++) {
        {
            var (_, errΔ5) = Decapsulate(dk, c[..(int)(i)]); if (errΔ5 == default!) {
                Ꮡt.Errorf("expected error for c length %d"u8, i);
            }
        }
    }
    var cLong = c;
    for (nint i = 0; i < 100; i++) {
        cLong = append(cLong, (byte)(0));
        {
            var (_, errΔ6) = Decapsulate(dk, cLong); if (errΔ6 == default!) {
                Ꮡt.Errorf("expected error for c length %d"u8, len(cLong));
            }
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string badMessageLengthˢ = "bad message length"u8;

public static (slice<byte> c, slice<byte> K, error err) EncapsulateDerand(slice<byte> ek, slice<byte> m) {
    if (len(m) != messageSize) {
        return (default!, default!, errors.New(badMessageLengthˢ));
    }
    return kemEncaps(nil, ek, Ꮡ(array<byte>.Alias(m, 32)));
}

public static (slice<byte>, error) DecapsulateFromBytes(slice<byte> dkBytes, slice<byte> c) {
    var (dk, err) = NewKeyFromExtendedEncoding(dkBytes);
    if (err != default!) {
        return (default!, err);
    }
    return Decapsulate(dk, c);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object badLengthˢ = (@string)"bad length"u8;

public static (slice<byte>, ж<global::go.crypto.@internal.mlkem768_package.DecapsulationKey>) GenerateKeyDerand(testing.TB t, slice<byte> d, slice<byte> z) {
    if (len(d) != 32 || len(z) != 32) {
        t.Fatal(badLengthˢ);
    }
    var dk = kemKeyGen(nil, Ꮡ(array<byte>.Alias(d, 32)), Ꮡ(array<byte>.Alias(z, 32)));
    return (dk.EncapsulationKey(), dk);
}

internal static ж<bool> millionFlag = flag.Bool("million"u8, false, "run the million vector test"u8);

// TestPQCrystalsAccumulated accumulates the 10k vectors generated by the
// reference implementation and checks the hash of the result, to avoid checking
// in 150MB of test vectors.
public static void TestPQCrystalsAccumulated(ж<testing.T> Ꮡt) {
    nint n = 10000;
    @string expected = "f7db260e1137a742e05fe0db9525012812b004d29040a5b606aad3d134b548d3"u8;
    if (testing.Short()) {
        n = 100;
        expected = "8d0c478ead6037897a0da6be21e5399545babf5fc6dd10c061c99b7dee2bf0dc"u8;
    }
    if (millionFlag.Value) {
        n = 1000000;
        expected = "70090cc5842aad0ec43d5042c783fae9bc320c047b5dafcb6e134821db02384d"u8;
    }
    var s = sha3.NewShake128();
    var o = sha3.NewShake128();
    var d = new slice<byte>(32);
    var z = new slice<byte>(32);
    var msg = new slice<byte>(32);
    var ct1 = new slice<byte>(CiphertextSize);
    for (nint i = 0; i < n; i++) {
        s.Read(d);
        s.Read(z);
        var (ek, dk) = GenerateKeyDerand(new mlkem768_internal_test_package.testing_TжTB(Ꮡt), d, z);
        o.Write(ek);
        o.Write(dk.Bytes());
        s.Read(msg);
        var (ct, k, err) = EncapsulateDerand(ek, msg);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        o.Write(ct);
        o.Write(k);
        (var kk, err) = Decapsulate(dk, ct);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        if (!bytes.Equal(kk, k)) {
            Ꮡt.Errorf("k: got %x, expected %x"u8, kk, k);
        }
        s.Read(ct1);
        (var k1, err) = Decapsulate(dk, ct1);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        o.Write(k1);
    }
    @string got = hex.EncodeToString(o.Sum(default!));
    if (got != expected) {
        Ꮡt.Errorf("got %s, expected %s"u8, got, expected);
    }
}

internal static byte sink;

public static void BenchmarkKeyGen(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    ref var dk = ref heap(new global::go.crypto.@internal.mlkem768_package.DecapsulationKey(), out var Ꮡdk);
    ref var d = ref heap(new array<byte>(32), out var Ꮡd);
    ref var z = ref heap(new array<byte>(32), out var Ꮡz);
    rand.Read(d[..]);
    rand.Read(z[..]);
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        var dkΔ1 = kemKeyGen(Ꮡdk, Ꮡd, Ꮡz);
        sink ^= (byte)(dkΔ1.EncapsulationKey()[0]);
    }
}

public static void BenchmarkEncaps(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = new slice<byte>(32);
    rand.Read(d);
    var z = new slice<byte>(32);
    rand.Read(z);
    ref var m = ref heap(new array<byte>(32), out var Ꮡm);
    rand.Read(m[..]);
    var (ek, _) = GenerateKeyDerand(new mlkem768_internal_test_package.testing_BжTB(Ꮡb), d, z);
    ref var c = ref heap(new array<byte>(1088), out var Ꮡc);
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        var (cΔ1, K, err) = kemEncaps(Ꮡc, ek, Ꮡm);
        if (err != default!) {
            Ꮡb.Fatal(err);
        }
        sink ^= (byte)((byte)(cΔ1[0] ^ K[0]));
    }
}

public static void BenchmarkDecaps(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var d = new slice<byte>(32);
    rand.Read(d);
    var z = new slice<byte>(32);
    rand.Read(z);
    var m = new slice<byte>(32);
    rand.Read(m);
    var (ek, dk) = GenerateKeyDerand(new mlkem768_internal_test_package.testing_BжTB(Ꮡb), d, z);
    var (c, _, err) = EncapsulateDerand(ek, m);
    if (err != default!) {
        Ꮡb.Fatal(err);
    }
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        var K = kemDecaps(ref (dk).DerefOrNull(), Ꮡ(array<byte>.Alias(c, 1088)));
        sink ^= (byte)(K[0]);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string aliceˢ = "Alice"u8;
internal static readonly @string bobˢ = "Bob"u8;

public static void BenchmarkRoundTrip(ж<testing.B> Ꮡb) {
    var (dk, err) = GenerateKey();
    if (err != default!) {
        Ꮡb.Fatal(err);
    }
    var ek = dk.EncapsulationKey();
    (var c, _, err) = Encapsulate(ek);
    if (err != default!) {
        Ꮡb.Fatal(err);
    }
    var cʗ1 = c;
    var dkʗ1 = dk;
    Ꮡb.Run(aliceˢ, (ж<testing.B> bΔ1) => {
        for (nint i = 0; i < (~bΔ1).N; i++) {
            var (dkS, errΔ1) = GenerateKey();
            if (errΔ1 != default!) {
                bΔ1.Fatal(errΔ1);
            }
            var ekS = dkS.EncapsulationKey();
            sink ^= (byte)(ekS[0]);
            (var Ks, errΔ1) = Decapsulate(dkʗ1, cʗ1);
            if (errΔ1 != default!) {
                bΔ1.Fatal(errΔ1);
            }
            sink ^= (byte)(Ks[0]);
        }
    });
    var ekʗ1 = ek;
    Ꮡb.Run(bobˢ, (ж<testing.B> bΔ2) => {
        for (nint i = 0; i < (~bΔ2).N; i++) {
            var (cS, Ks, errΔ2) = Encapsulate(ekʗ1);
            if (errΔ2 != default!) {
                bΔ2.Fatal(errΔ2);
            }
            sink ^= (byte)((byte)(cS[0] ^ Ks[0]));
        }
    });
}

} // end mlkem768_internal_test_package
