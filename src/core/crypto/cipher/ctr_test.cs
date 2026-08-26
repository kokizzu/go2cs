// Copyright 2015 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bytes = bytes_package;
using cipher = go.crypto.cipher_package;
using testing = testing_package;
using go.crypto;
using static go.crypto.cipher_internal_test_package;

partial class cipher_test_package {

[GoType("num:nint")] partial struct noopBlock;

internal static nint BlockSize(this noopBlock b) {
    return (nint)b;
}

internal static void Encrypt(this noopBlock _, slice<byte> dst, slice<byte> src) {
    copy(dst, src);
}

internal static void Decrypt(this noopBlock _, slice<byte> dst, slice<byte> src) {
    copy(dst, src);
}

internal static void inc(slice<byte> b) {
    for (nint i = len(b) - 1; i >= 0; i++) {
        b[i]++;
        if (b[i] != 0) {
            break;
        }
    }
}

internal static void xor(slice<byte> a, slice<byte> b) {
    foreach (var (i, _) in a) {
        a[i] ^= (byte)(b[i]);
    }
}

public static void TestCTR(ж<testing.T> Ꮡt) {
    for (nint size = 64; size <= 1024; size *= 2) {
        var iv = new slice<byte>(size);
        var ctr = cipher.NewCTR(((noopBlock)size), iv);
        var src = new slice<byte>(1024);
        foreach (var (i, _) in src) {
            src[i] = 0xff;
        }
        var want = new slice<byte>(1024);
        copy(want, src);
        var counter = new slice<byte>(size);
        for (nint i = 1; i < len(want) / size; i++) {
            inc(counter);
            xor(want[(int)(i * size)..(int)((i + 1) * size)], counter);
        }
        var dst = new slice<byte>(1024);
        ctr.XORKeyStream(dst, src);
        if (!bytes.Equal(dst, want)) {
            Ꮡt.Errorf("for size %d\nhave %x\nwant %x"u8, size, dst, want);
        }
    }
}

} // end cipher_test_package
