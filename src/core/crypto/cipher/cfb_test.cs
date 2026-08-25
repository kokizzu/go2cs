// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bytes = bytes_package;
using aes = go.crypto.aes_package;
using cipher = go.crypto.cipher_package;
using rand = go.crypto.rand_package;
using hex = encoding.hex_package;
using testing = testing_package;
using encoding;
using go.crypto;
using static go.crypto.cipher_internal_test_package;

partial class cipher_test_package {

// cfbTests contains the test vectors from
// https://csrc.nist.gov/publications/nistpubs/800-38a/sp800-38a.pdf, section
// F.3.13.

[GoType("dyn")] partial struct cfbTestsᴛ1 {
    internal @string key, iv, plaintext, ciphertext;
}
internal static slice<cfbTestsᴛ1> cfbTests = new cfbTestsᴛ1[]{
    new(
        "2b7e151628aed2a6abf7158809cf4f3c"u8,
        "000102030405060708090a0b0c0d0e0f"u8,
        "6bc1bee22e409f96e93d7e117393172a"u8,
        "3b3fd92eb72dad20333449f8e83cfb4a"u8
    ),
    new(
        "2b7e151628aed2a6abf7158809cf4f3c"u8,
        "3B3FD92EB72DAD20333449F8E83CFB4A"u8,
        "ae2d8a571e03ac9c9eb76fac45af8e51"u8,
        "c8a64537a0b3a93fcde3cdad9f1ce58b"u8
    ),
    new(
        "2b7e151628aed2a6abf7158809cf4f3c"u8,
        "C8A64537A0B3A93FCDE3CDAD9F1CE58B"u8,
        "30c81c46a35ce411e5fbc1191a0a52ef"u8,
        "26751f67a3cbb140b1808cf187a4f4df"u8
    ),
    new(
        "2b7e151628aed2a6abf7158809cf4f3c"u8,
        "26751F67A3CBB140B1808CF187A4F4DF"u8,
        "f69f2445df4f9b17ad2b417be66c3710"u8,
        "c04b05357c5d1c0eeac4c66f9ff7f2e6"u8
    )
}.slice();

public static void TestCFBVectors(ж<testing.T> Ꮡt) {
    foreach (var (i, test) in cfbTests) {
        var (key, err) = hex.DecodeString(test.key);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        (var iv, err) = hex.DecodeString(test.iv);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        (var plaintext, err) = hex.DecodeString(test.plaintext);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        (var expected, err) = hex.DecodeString(test.ciphertext);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        (var block, err) = aes.NewCipher(key);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        var ciphertext = new slice<byte>(len(plaintext));
        var cfb = cipher.NewCFBEncrypter(block, iv);
        cfb.XORKeyStream(ciphertext, plaintext);
        if (!bytes.Equal(ciphertext, expected)) {
            Ꮡt.Errorf("#%d: wrong output: got %x, expected %x"u8, i, ciphertext, expected);
        }
        var cfbdec = cipher.NewCFBDecrypter(block, iv);
        var plaintextCopy = new slice<byte>(len(ciphertext));
        cfbdec.XORKeyStream(plaintextCopy, ciphertext);
        if (!bytes.Equal(plaintextCopy, plaintext)) {
            Ꮡt.Errorf("#%d: wrong plaintext: got %x, expected %x"u8, i, plaintextCopy, plaintext);
        }
    }
}

public static void TestCFBInverse(ж<testing.T> Ꮡt) {
    var (block, err) = aes.NewCipher(commonKey128);
    if (err != default!) {
        Ꮡt.Error(err);
        return;
    }
    var plaintext = slice<byte>("this is the plaintext. this is the plaintext."u8);
    var iv = new slice<byte>(block.BlockSize());
    rand.Reader.Read(iv);
    var cfb = cipher.NewCFBEncrypter(block, iv);
    var ciphertext = new slice<byte>(len(plaintext));
    copy(ciphertext, plaintext);
    cfb.XORKeyStream(ciphertext, ciphertext);
    var cfbdec = cipher.NewCFBDecrypter(block, iv);
    var plaintextCopy = new slice<byte>(len(plaintext));
    copy(plaintextCopy, ciphertext);
    cfbdec.XORKeyStream(plaintextCopy, plaintextCopy);
    if (!bytes.Equal(plaintextCopy, plaintext)) {
        Ꮡt.Errorf("got: %x, want: %x"u8, plaintextCopy, plaintext);
    }
}

} // end cipher_test_package
