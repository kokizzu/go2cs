// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using testing = testing_package;
using cipher = go.crypto.cipher_package;
using go.crypto;
using static go.crypto.aes_package;

partial class aes_internal_test_package {

// See const.go for overview of math here.

// Test that powx is initialized correctly.
// (Can adapt this code to generate it too.)
public static void TestPowx(ж<testing.T> Ꮡt) {
    nint p = 1;
    for (nint i = 0; i < len(powx); i++) {
        if (powx[i] != (byte)p) {
            Ꮡt.Errorf("powx[%d] = %#x, want %#x"u8, i, powx[i], p);
        }
        p <<= (int)(1);
        if ((nint)(p & 0x100) != 0) {
            p ^= (nint)(poly);
        }
    }
}

// Multiply b and c as GF(2) polynomials modulo poly
internal static uint32 mul(uint32 b, uint32 c) {
    var i = b;
    var j = c;
    var s = (uint32)0;
    for (var k = (uint32)1; k < 0x100 && j != 0; k <<= (int)(1)) {
        // Invariant: k == 1<<n, i == b * xⁿ
        if ((uint32)(j & k) != 0) {
            // s += i in GF(2); xor in binary
            s ^= (uint32)(i);
            j ^= (uint32)(k); // turn off bit to end loop early
        }
        // i *= x in GF(2) modulo the polynomial
        i <<= (int)(1);
        if ((uint32)(i & 0x100) != 0) {
            i ^= (uint32)(poly);
        }
    }
    return s;
}

// Test all mul inputs against bit-by-bit n² algorithm.
public static void TestMul(ж<testing.T> Ꮡt) {
    for (var i = (uint32)0; i < 256; i++) {
        for (var j = (uint32)0; j < 256; j++) {
            // Multiply i, j bit by bit.
            var s = (uint8)0;
            for (nuint k = (nuint)0; k < 8; k++) {
                for (nuint l = (nuint)0; l < 8; l++) {
                    if ((uint32)(i & (((uint32)1).Lsh(k))) != 0 && (uint32)(j & (((uint32)1).Lsh(l))) != 0) {
                        s ^= (byte)(powx[(nint)(k + l)]);
                    }
                }
            }
            {
                var x = mul(i, j); if (x != (uint32)s) {
                    Ꮡt.Fatalf("mul(%#x, %#x) = %#x, want %#x"u8, i, j, x, s);
                }
            }
        }
    }
}

// Check that S-boxes are inverses of each other.
// They have more structure that we could test,
// but if this sanity check passes, we'll assume
// the cut and paste from the FIPS PDF worked.
public static void TestSboxes(ж<testing.T> Ꮡt) {
    for (nint i = 0; i < 256; i++) {
        {
            var j = sbox0[sbox1[i]]; if (j != (byte)i) {
                Ꮡt.Errorf("sbox0[sbox1[%#x]] = %#x"u8, i, j);
            }
        }
        {
            var j = sbox1[sbox0[i]]; if (j != (byte)i) {
                Ꮡt.Errorf("sbox1[sbox0[%#x]] = %#x"u8, i, j);
            }
        }
    }
}

// Test that encryption tables are correct.
// (Can adapt this code to generate them too.)
public static void TestTe(ж<testing.T> Ꮡt) {
    for (nint i = 0; i < 256; i++) {
        var s = (uint32)sbox0[i];
        var s2 = mul(s, 2);
        var s3 = mul(s, 3);
        var w = (uint32)((uint32)((uint32)((s2 << (int)(24)) | (s << (int)(16))) | (s << (int)(8))) | s3);
        var te = new array<uint32>[]{te0.Clone(), te1.Clone(), te2.Clone(), te3.Clone()}.slice();
        for (nint j = 0; j < 4; j++) {
            {
                var x = te[j][i]; if (x != w) {
                    Ꮡt.Fatalf("te[%d][%d] = %#x, want %#x"u8, j, i, x, w);
                }
            }
            w = (uint32)((w << (int)(24)) | (w >> (int)(8)));
        }
    }
}

// Test that decryption tables are correct.
// (Can adapt this code to generate them too.)
public static void TestTd(ж<testing.T> Ꮡt) {
    for (nint i = 0; i < 256; i++) {
        var s = (uint32)sbox1[i];
        var s9 = mul(s, 0x9);
        var sb = mul(s, 0xb);
        var sd = mul(s, 0xd);
        var se = mul(s, 0xe);
        var w = (uint32)((uint32)((uint32)((se << (int)(24)) | (s9 << (int)(16))) | (sd << (int)(8))) | sb);
        var td = new array<uint32>[]{td0.Clone(), td1.Clone(), td2.Clone(), td3.Clone()}.slice();
        for (nint j = 0; j < 4; j++) {
            {
                var x = td[j][i]; if (x != w) {
                    Ꮡt.Fatalf("td[%d][%d] = %#x, want %#x"u8, j, i, x, w);
                }
            }
            w = (uint32)((w << (int)(24)) | (w >> (int)(8)));
        }
    }
}

// Test vectors are from FIPS 197:
//	https://csrc.nist.gov/publications/fips/fips197/fips-197.pdf

// Appendix A of FIPS 197: Key expansion examples
[GoType] public partial struct KeyTest {
    internal slice<byte> key;
    internal slice<uint32> enc;
    internal slice<uint32> dec; // decryption expansion; not in FIPS 197, computed from C implementation.
}

// A.1.  Expansion of a 128-bit Cipher Key
// A.2.  Expansion of a 192-bit Cipher Key
// A.3.  Expansion of a 256-bit Cipher Key
internal static slice<KeyTest> keyTests = new KeyTest[]{
    new(
        new byte[]{0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6, 0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c}.slice(),
        new uint32[]{
            0x2b7e1516, 0x28aed2a6, 0xabf71588U, 0x09cf4f3c,
            0xa0fafe17U, 0x88542cb1U, 0x23a33939, 0x2a6c7605,
            0xf2c295f2U, 0x7a96b943, 0x5935807a, 0x7359f67f,
            0x3d80477d, 0x4716fe3e, 0x1e237e44, 0x6d7a883b,
            0xef44a541U, 0xa8525b7fU, 0xb671253bU, 0xdb0bad00U,
            0xd4d1c6f8U, 0x7c839d87, 0xcaf2b8bcU, 0x11f915bc,
            0x6d88a37a, 0x110b3efd, 0xdbf98641U, 0xca0093fdU,
            0x4e54f70e, 0x5f5fc9f3, 0x84a64fb2U, 0x4ea6dc4f,
            0xead27321U, 0xb58dbad2U, 0x312bf560, 0x7f8d292f,
            0xac7766f3U, 0x19fadc21, 0x28d12941, 0x575c006e,
            0xd014f9a8U, 0xc9ee2589U, 0xe13f0cc8U, 0xb6630ca6U
        }.slice(),
        new uint32[]{
            0xd014f9a8U, 0xc9ee2589U, 0xe13f0cc8U, 0xb6630ca6U,
            0xc7b5a63, 0x1319eafe, 0xb0398890U, 0x664cfbb4,
            0xdf7d925aU, 0x1f62b09d, 0xa320626eU, 0xd6757324U,
            0x12c07647, 0xc01f22c7U, 0xbc42d2f3U, 0x7555114a,
            0x6efcd876, 0xd2df5480U, 0x7c5df034, 0xc917c3b9U,
            0x6ea30afc, 0xbc238cf6U, 0xae82a4b4U, 0xb54a338dU,
            0x90884413U, 0xd280860aU, 0x12a12842, 0x1bc89739,
            0x7c1f13f7, 0x4208c219, 0xc021ae48U, 0x969bf7b,
            0xcc7505ebU, 0x3e17d1ee, 0x82296c51U, 0xc9481133U,
            0x2b3708a7, 0xf262d405U, 0xbc3ebdbfU, 0x4b617d62,
            0x2b7e1516, 0x28aed2a6, 0xabf71588U, 0x9cf4f3c
        }.slice()
    ),
    new(
        new byte[]{
            0x8e, 0x73, 0xb0, 0xf7, 0xda, 0x0e, 0x64, 0x52, 0xc8, 0x10, 0xf3, 0x2b, 0x80, 0x90, 0x79, 0xe5,
            0x62, 0xf8, 0xea, 0xd2, 0x52, 0x2c, 0x6b, 0x7b
        }.slice(),
        new uint32[]{
            0x8e73b0f7U, 0xda0e6452U, 0xc810f32bU, 0x809079e5U,
            0x62f8ead2, 0x522c6b7b, 0xfe0c91f7U, 0x2402f5a5,
            0xec12068eU, 0x6c827f6b, 0x0e7a95b9, 0x5c56fec2,
            0x4db7b4bd, 0x69b54118, 0x85a74796U, 0xe92538fdU,
            0xe75fad44U, 0xbb095386U, 0x485af057, 0x21efb14f,
            0xa448f6d9U, 0x4d6dce24, 0xaa326360U, 0x113b30e6,
            0xa25e7ed5U, 0x83b1cf9aU, 0x27f93943, 0x6a94f767,
            0xc0a69407U, 0xd19da4e1U, 0xec1786ebU, 0x6fa64971,
            0x485f7032, 0x22cb8755, 0xe26d1352U, 0x33f0b7b3,
            0x40beeb28, 0x2f18a259, 0x6747d26b, 0x458c553e,
            0xa7e1466cU, 0x9411f1dfU, 0x821f750aU, 0xad07d753U,
            0xca400538U, 0x8fcc5006U, 0x282d166a, 0xbc3ce7b5U,
            0xe98ba06fU, 0x448c773c, 0x8ecc7204U, 0x01002202
        }.slice(),
        default!
    ),
    new(
        new byte[]{
            0x60, 0x3d, 0xeb, 0x10, 0x15, 0xca, 0x71, 0xbe, 0x2b, 0x73, 0xae, 0xf0, 0x85, 0x7d, 0x77, 0x81,
            0x1f, 0x35, 0x2c, 0x07, 0x3b, 0x61, 0x08, 0xd7, 0x2d, 0x98, 0x10, 0xa3, 0x09, 0x14, 0xdf, 0xf4
        }.slice(),
        new uint32[]{
            0x603deb10, 0x15ca71be, 0x2b73aef0, 0x857d7781U,
            0x1f352c07, 0x3b6108d7, 0x2d9810a3, 0x0914dff4,
            0x9ba35411U, 0x8e6925afU, 0xa51a8b5fU, 0x2067fcde,
            0xa8b09c1aU, 0x93d194cdU, 0xbe49846eU, 0xb75d5b9aU,
            0xd59aecb8U, 0x5bf3c917, 0xfee94248U, 0xde8ebe96U,
            0xb5a9328aU, 0x2678a647, 0x98312229U, 0x2f6c79b3,
            0x812c81adU, 0xdadf48baU, 0x24360af2, 0xfab8b464U,
            0x98c5bfc9U, 0xbebd198eU, 0x268c3ba7, 0x09e04214,
            0x68007bac, 0xb2df3316U, 0x96e939e4U, 0x6c518d80,
            0xc814e204U, 0x76a9fb8a, 0x5025c02d, 0x59c58239,
            0xde136967U, 0x6ccc5a71, 0xfa256395U, 0x9674ee15U,
            0x5886ca5d, 0x2e2f31d7, 0x7e0af1fa, 0x27cf73c3,
            0x749c47ab, 0x18501dda, 0xe2757e4fU, 0x7401905a,
            0xcafaaae3U, 0xe4d59b34U, 0x9adf6aceU, 0xbd10190dU,
            0xfe4890d1U, 0xe6188d0bU, 0x046df344, 0x706c631e
        }.slice(),
        default!
    )
}.slice();

// Test key expansion against FIPS 197 examples.
public static void TestExpandKey(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

L:
    foreach (var (i, tt) in keyTests) {
        var enc = new slice<uint32>(len(tt.enc));
        slice<uint32> dec = default!;
        if (tt.dec != default!) {
            dec = new slice<uint32>(len(tt.dec));
        }
        // This test could only test Go version of expandKey because asm
        // version might use different memory layout for expanded keys
        // This is OK because we don't expose expanded keys to the outside
        expandKeyGo(tt.key, enc, dec);
        foreach (var (j, v) in enc) {
            if (v != tt.enc[j]) {
                Ꮡt.Errorf("key %d: enc[%d] = %#x, want %#x"u8, i, j, v, tt.enc[j]);
                goto continue_L;
            }
        }
        foreach (var (j, v) in dec) {
            if (v != tt.dec[j]) {
                Ꮡt.Errorf("key %d: dec[%d] = %#x, want %#x"u8, i, j, v, tt.dec[j]);
                goto continue_L;
            }
        }
continue_L:;
    }
break_L:;
}

// Appendix B, C of FIPS 197: Cipher examples, Example vectors.
[GoType] public partial struct CryptTest {
    internal slice<byte> key;
    internal slice<byte> @in;
    internal slice<byte> @out;
}

// Appendix B.
// Appendix C.1.  AES-128
// Appendix C.2.  AES-192
// Appendix C.3.  AES-256
internal static slice<CryptTest> encryptTests = new CryptTest[]{
    new(
        new byte[]{0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6, 0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c}.slice(),
        new byte[]{0x32, 0x43, 0xf6, 0xa8, 0x88, 0x5a, 0x30, 0x8d, 0x31, 0x31, 0x98, 0xa2, 0xe0, 0x37, 0x07, 0x34}.slice(),
        new byte[]{0x39, 0x25, 0x84, 0x1d, 0x02, 0xdc, 0x09, 0xfb, 0xdc, 0x11, 0x85, 0x97, 0x19, 0x6a, 0x0b, 0x32}.slice()
    ),
    new(
        new byte[]{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f}.slice(),
        new byte[]{0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff}.slice(),
        new byte[]{0x69, 0xc4, 0xe0, 0xd8, 0x6a, 0x7b, 0x04, 0x30, 0xd8, 0xcd, 0xb7, 0x80, 0x70, 0xb4, 0xc5, 0x5a}.slice()
    ),
    new(
        new byte[]{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17
        }.slice(),
        new byte[]{0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff}.slice(),
        new byte[]{0xdd, 0xa9, 0x7c, 0xa4, 0x86, 0x4c, 0xdf, 0xe0, 0x6e, 0xaf, 0x70, 0xa0, 0xec, 0x0d, 0x71, 0x91}.slice()
    ),
    new(
        new byte[]{0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
        }.slice(),
        new byte[]{0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff}.slice(),
        new byte[]{0x8e, 0xa2, 0xb7, 0xca, 0x51, 0x67, 0x45, 0xbf, 0xea, 0xfc, 0x49, 0x90, 0x4b, 0x49, 0x60, 0x89}.slice()
    )
}.slice();

// Test Cipher Encrypt method against FIPS 197 examples.
public static void TestCipherEncrypt(ж<testing.T> Ꮡt) {
    foreach (var (i, tt) in encryptTests) {
        var (c, err) = NewCipher(tt.key);
        if (err != default!) {
            Ꮡt.Errorf("NewCipher(%d bytes) = %s"u8, len(tt.key), err);
            continue;
        }
        var @out = new slice<byte>(len(tt.@in));
        c.Encrypt(@out, tt.@in);
        foreach (var (j, v) in @out) {
            if (v != tt.@out[j]) {
                Ꮡt.Errorf("Cipher.Encrypt %d: out[%d] = %#x, want %#x"u8, i, j, v, tt.@out[j]);
                break;
            }
        }
    }
}

// Test Cipher Decrypt against FIPS 197 examples.
public static void TestCipherDecrypt(ж<testing.T> Ꮡt) {
    foreach (var (i, tt) in encryptTests) {
        var (c, err) = NewCipher(tt.key);
        if (err != default!) {
            Ꮡt.Errorf("NewCipher(%d bytes) = %s"u8, len(tt.key), err);
            continue;
        }
        var plain = new slice<byte>(len(tt.@in));
        c.Decrypt(plain, tt.@out);
        foreach (var (j, v) in plain) {
            if (v != tt.@in[j]) {
                Ꮡt.Errorf("decryptBlock %d: plain[%d] = %#x, want %#x"u8, i, j, v, tt.@in[j]);
                break;
            }
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string cryptoAesInputNotFullˢ = "crypto/aes: input not full block"u8;
internal static readonly @string cryptoAesOutputNotFullˢ = "crypto/aes: output not full block"u8;

// Test short input/output.
// Assembly used to not notice.
// See issue 7928.
public static void TestShortBlocks(ж<testing.T> Ꮡt) {
    slice<byte> bytes(nint n) => new slice<byte>(n);
    var (c, _) = NewCipher(bytes(16));
    var bytesʗ1 = bytes;
    var cʗ1 = c;
    mustPanic(Ꮡt, cryptoAesInputNotFullˢ, () => {
        cʗ1.Encrypt(bytesʗ1(1), bytesʗ1(1));
    });
    var bytesʗ2 = bytes;
    var cʗ2 = c;
    mustPanic(Ꮡt, cryptoAesInputNotFullˢ, () => {
        cʗ2.Decrypt(bytesʗ2(1), bytesʗ2(1));
    });
    var bytesʗ3 = bytes;
    var cʗ3 = c;
    mustPanic(Ꮡt, cryptoAesInputNotFullˢ, () => {
        cʗ3.Encrypt(bytesʗ3(100), bytesʗ3(1));
    });
    var bytesʗ4 = bytes;
    var cʗ4 = c;
    mustPanic(Ꮡt, cryptoAesInputNotFullˢ, () => {
        cʗ4.Decrypt(bytesʗ4(100), bytesʗ4(1));
    });
    var bytesʗ5 = bytes;
    var cʗ5 = c;
    mustPanic(Ꮡt, cryptoAesOutputNotFullˢ, () => {
        cʗ5.Encrypt(bytesʗ5(1), bytesʗ5(100));
    });
    var bytesʗ6 = bytes;
    var cʗ6 = c;
    mustPanic(Ꮡt, cryptoAesOutputNotFullˢ, () => {
        cʗ6.Decrypt(bytesʗ6(1), bytesʗ6(100));
    });
}

internal static void mustPanic(ж<testing.T> Ꮡt, @string msg, Action f) {
    GoFrame ᒐ = default;
    try {
        defer(() => {
            var err = recover();
            if (err == default!){
                Ꮡt.Errorf("function did not panic, wanted %q"u8, msg);
            } else 
            if (!AreEqual(err, msg)) {
                Ꮡt.Errorf("got panic %v, wanted %q"u8, err, msg);
            }
        }, ref ᒐ);
        f();
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string aes128ˢ = "AES-128"u8;
internal static readonly @string aes192ˢ = "AES-192"u8;
internal static readonly @string aes256ˢ = "AES-256"u8;

public static void BenchmarkEncrypt(ж<testing.B> Ꮡb) {
    Ꮡb.Run(aes128ˢ, (ж<testing.B> bΔ1) => {
        benchmarkEncrypt(bΔ1, encryptTests[1]);
    });
    Ꮡb.Run(aes192ˢ, (ж<testing.B> bΔ2) => {
        benchmarkEncrypt(bΔ2, encryptTests[2]);
    });
    Ꮡb.Run(aes256ˢ, (ж<testing.B> bΔ3) => {
        benchmarkEncrypt(bΔ3, encryptTests[3]);
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object newCipherˢ = (@string)"NewCipher:"u8;

internal static void benchmarkEncrypt(ж<testing.B> Ꮡb, CryptTest tt) {
    ref var b = ref Ꮡb.DerefOrNull();

    var (c, err) = NewCipher(tt.key);
    if (err != default!) {
        Ꮡb.Fatal(newCipherˢ, err);
    }
    var @out = new slice<byte>(len(tt.@in));
    b.SetBytes((int64)len(@out));
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        c.Encrypt(@out, tt.@in);
    }
}

public static void BenchmarkDecrypt(ж<testing.B> Ꮡb) {
    Ꮡb.Run(aes128ˢ, (ж<testing.B> bΔ1) => {
        benchmarkDecrypt(bΔ1, encryptTests[1]);
    });
    Ꮡb.Run(aes192ˢ, (ж<testing.B> bΔ2) => {
        benchmarkDecrypt(bΔ2, encryptTests[2]);
    });
    Ꮡb.Run(aes256ˢ, (ж<testing.B> bΔ3) => {
        benchmarkDecrypt(bΔ3, encryptTests[3]);
    });
}

internal static void benchmarkDecrypt(ж<testing.B> Ꮡb, CryptTest tt) {
    ref var b = ref Ꮡb.DerefOrNull();

    var (c, err) = NewCipher(tt.key);
    if (err != default!) {
        Ꮡb.Fatal(newCipherˢ, err);
    }
    var @out = new slice<byte>(len(tt.@out));
    b.SetBytes((int64)len(@out));
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        c.Decrypt(@out, tt.@out);
    }
}

public static void BenchmarkExpand(ж<testing.B> Ꮡb) {
    Ꮡb.Run(aes128ˢ, (ж<testing.B> bΔ1) => {
        benchmarkExpand(bΔ1, encryptTests[1]);
    });
    Ꮡb.Run(aes192ˢ, (ж<testing.B> bΔ2) => {
        benchmarkExpand(bΔ2, encryptTests[2]);
    });
    Ꮡb.Run(aes256ˢ, (ж<testing.B> bΔ3) => {
        benchmarkExpand(bΔ3, encryptTests[3]);
    });
}

internal static void benchmarkExpand(ж<testing.B> Ꮡb, CryptTest tt) {
    ref var b = ref Ꮡb.DerefOrNull();

    var c = Ꮡ(new aesCipher(l: (uint8)(len(tt.key) + 28)));
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        expandKey(tt.key, (~c).enc[..(int)((~c).l)], (~c).dec[..(int)((~c).l)]);
    }
}

public static void BenchmarkCreateCipher(ж<testing.B> Ꮡb) {
    Ꮡb.Run(aes128ˢ, (ж<testing.B> bΔ1) => {
        benchmarkCreateCipher(bΔ1, encryptTests[1]);
    });
    Ꮡb.Run(aes192ˢ, (ж<testing.B> bΔ2) => {
        benchmarkCreateCipher(bΔ2, encryptTests[2]);
    });
    Ꮡb.Run(aes256ˢ, (ж<testing.B> bΔ3) => {
        benchmarkCreateCipher(bΔ3, encryptTests[3]);
    });
}

internal static void benchmarkCreateCipher(ж<testing.B> Ꮡb, CryptTest tt) {
    ref var b = ref Ꮡb.DerefOrNull();

    b.ReportAllocs();
    for (nint i = 0; i < b.N; i++) {
        {
            var (_, err) = NewCipher(tt.key); if (err != default!) {
                Ꮡb.Fatal(err);
            }
        }
    }
}

} // end aes_internal_test_package
