// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bytes = bytes_package;
using aes = go.crypto.aes_package;
using cipher = go.crypto.cipher_package;
using des = go.crypto.des_package;
using testing = testing_package;
using go.crypto;
using static go.crypto.cipher_internal_test_package;

partial class cipher_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string cryptoCipherInputNotFullˢ = "crypto/cipher: input not full blocks"u8;
internal static readonly @string cryptoCipherOutputˢ = "crypto/cipher: output smaller than input"u8;

public static void TestCryptBlocks(ж<testing.T> Ꮡt) {
    var buf = new slice<byte>(16);
    var (block, _) = aes.NewCipher(buf);
    ref var mode = ref heap<cipher.BlockMode>(out var Ꮡmode);
    mode = cipher.NewCBCDecrypter(block, buf);
    var bufʗ1 = buf;
    mustPanic(Ꮡt, cryptoCipherInputNotFullˢ, () => {
        Ꮡmode.ValueSlot.CryptBlocks(bufʗ1, bufʗ1[..3]);
    });
    var bufʗ2 = buf;
    mustPanic(Ꮡt, cryptoCipherOutputˢ, () => {
        Ꮡmode.ValueSlot.CryptBlocks(bufʗ2[..3], bufʗ2);
    });
    mode = cipher.NewCBCEncrypter(block, buf);
    var bufʗ3 = buf;
    mustPanic(Ꮡt, cryptoCipherInputNotFullˢ, () => {
        Ꮡmode.ValueSlot.CryptBlocks(bufʗ3, bufʗ3[..3]);
    });
    var bufʗ4 = buf;
    mustPanic(Ꮡt, cryptoCipherOutputˢ, () => {
        Ꮡmode.ValueSlot.CryptBlocks(bufʗ4[..3], bufʗ4);
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
internal static readonly @string cbcEncryptˢ = "CBC encrypt"u8;
internal static readonly @string cbcDecryptˢ = "CBC decrypt"u8;
internal static readonly @string cfbEncryptˢ = "CFB encrypt"u8;
internal static readonly @string cfbDecryptˢ = "CFB decrypt"u8;
internal static readonly @string ctrˢ = "CTR"u8;
internal static readonly @string ofbˢ = "OFB"u8;

public static void TestEmptyPlaintext(ж<testing.T> Ꮡt) {
    array<byte> key = new(16);
    var (a, err) = aes.NewCipher(key[..16]);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    (var d, err) = des.NewCipher(key[..8]);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    nint s = 16;
    var pt = new slice<byte>(s);
    var ct = new slice<byte>(s);
    for (nint i = 0; i < 16; i++) {
        (pt[i], ct[i]) = ((byte)i, (byte)i);
    }
    void assertEqual(@string name, slice<byte> got, slice<byte> want) {
        if (!bytes.Equal(got, want)) {
            Ꮡt.Fatalf("%s: got %v, want %v"u8, name, got, want);
        }
    }
    foreach (var (_, b) in new cipher.Block[]{a, d}.slice()) {
        var iv = new slice<byte>(b.BlockSize());
        var cbce = cipher.NewCBCEncrypter(b, iv);
        cbce.CryptBlocks(ct, pt[..0]);
        assertEqual(cbcEncryptˢ, ct, pt);
        var cbcd = cipher.NewCBCDecrypter(b, iv);
        cbcd.CryptBlocks(ct, pt[..0]);
        assertEqual(cbcDecryptˢ, ct, pt);
        var cfbe = cipher.NewCFBEncrypter(b, iv);
        cfbe.XORKeyStream(ct, pt[..0]);
        assertEqual(cfbEncryptˢ, ct, pt);
        var cfbd = cipher.NewCFBDecrypter(b, iv);
        cfbd.XORKeyStream(ct, pt[..0]);
        assertEqual(cfbDecryptˢ, ct, pt);
        var ctr = cipher.NewCTR(b, iv);
        ctr.XORKeyStream(ct, pt[..0]);
        assertEqual(ctrˢ, ct, pt);
        var ofb = cipher.NewOFB(b, iv);
        ofb.XORKeyStream(ct, pt[..0]);
        assertEqual(ofbˢ, ct, pt);
    }
}

} // end cipher_test_package
