// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using cipher = go.crypto.cipher_package;
using testing = testing_package;
using go.crypto;
using static go.crypto.aes_package;

partial class aes_internal_test_package {

// Check that the optimized implementations of cipher modes will
// be picked up correctly.

// testInterface can be asserted to check that a type originates
// from this test group.
[GoType] internal partial interface testInterface {
    bool InAESPackage();
}

// testBlock implements the cipher.Block interface and any *Able
// interfaces that need to be tested.
[GoType] internal partial struct testBlock {
}

[GoRecv] internal static nint BlockSize(this ref testBlock _) {
    return 0;
}

[GoRecv] internal static void Encrypt(this ref testBlock _, slice<byte> a, slice<byte> b) {
}

[GoRecv] internal static void Decrypt(this ref testBlock _, slice<byte> a, slice<byte> b) {
}

[GoRecv] internal static (cipher.AEAD, error) NewGCM(this ref testBlock _Δp0, nint _Δp1, nint _Δp2) {
    return (new aes_internal_test_package.testAEADжAEAD(Ꮡ(new testAEAD(nil))), default!);
}

[GoRecv] internal static cipher.BlockMode NewCBCEncrypter(this ref testBlock _Δp0, slice<byte> _Δp1) {
    return new aes_internal_test_package.testBlockModeжBlockMode(Ꮡ(new testBlockMode(nil)));
}

[GoRecv] internal static cipher.BlockMode NewCBCDecrypter(this ref testBlock _Δp0, slice<byte> _Δp1) {
    return new aes_internal_test_package.testBlockModeжBlockMode(Ꮡ(new testBlockMode(nil)));
}

[GoRecv] internal static cipher.Stream NewCTR(this ref testBlock _Δp0, slice<byte> _Δp1) {
    return new aes_internal_test_package.testStreamжStream(Ꮡ(new testStream(nil)));
}

// testAEAD implements the cipher.AEAD interface.
[GoType] internal partial struct testAEAD {
}

[GoRecv] internal static nint NonceSize(this ref testAEAD _) {
    return 0;
}

[GoRecv] internal static nint Overhead(this ref testAEAD _) {
    return 0;
}

[GoRecv] internal static slice<byte> Seal(this ref testAEAD _, slice<byte> a, slice<byte> b, slice<byte> c, slice<byte> d) {
    return new byte[]{}.slice();
}

[GoRecv] internal static (slice<byte>, error) Open(this ref testAEAD _, slice<byte> a, slice<byte> b, slice<byte> c, slice<byte> d) {
    return (new byte[]{}.slice(), default!);
}

[GoRecv] internal static bool InAESPackage(this ref testAEAD _) {
    return true;
}

// Test the gcmAble interface is detected correctly by the cipher package.
public static void TestGCMAble(ж<testing.T> Ꮡt) {
    var b = ((cipher.Block)new aes_internal_test_package.testBlockжBlock(Ꮡ(new testBlock(nil))));
    {
        var (_, ok) = b._<gcmAble>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("testBlock does not implement the gcmAble interface"u8);
        }
    }
    var (aead, err) = cipher.NewGCM(b);
    if (err != default!) {
        Ꮡt.Fatalf("%v"u8, err);
    }
    {
        var (_, ok) = aead._<testInterface>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("cipher.NewGCM did not use gcmAble interface"u8);
        }
    }
}

// testBlockMode implements the cipher.BlockMode interface.
[GoType] internal partial struct testBlockMode {
}

[GoRecv] internal static nint BlockSize(this ref testBlockMode _) {
    return 0;
}

[GoRecv] internal static void CryptBlocks(this ref testBlockMode _, slice<byte> a, slice<byte> b) {
}

[GoRecv] internal static bool InAESPackage(this ref testBlockMode _) {
    return true;
}

// Test the cbcEncAble interface is detected correctly by the cipher package.
public static void TestCBCEncAble(ж<testing.T> Ꮡt) {
    var b = ((cipher.Block)new aes_internal_test_package.testBlockжBlock(Ꮡ(new testBlock(nil))));
    {
        var (_, ok) = b._<cbcEncAble>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("testBlock does not implement the cbcEncAble interface"u8);
        }
    }
    var bm = cipher.NewCBCEncrypter(b, new byte[]{}.slice());
    {
        var (_, ok) = bm._<testInterface>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("cipher.NewCBCEncrypter did not use cbcEncAble interface"u8);
        }
    }
}

// Test the cbcDecAble interface is detected correctly by the cipher package.
public static void TestCBCDecAble(ж<testing.T> Ꮡt) {
    var b = ((cipher.Block)new aes_internal_test_package.testBlockжBlock(Ꮡ(new testBlock(nil))));
    {
        var (_, ok) = b._<cbcDecAble>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("testBlock does not implement the cbcDecAble interface"u8);
        }
    }
    var bm = cipher.NewCBCDecrypter(b, new byte[]{}.slice());
    {
        var (_, ok) = bm._<testInterface>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("cipher.NewCBCDecrypter did not use cbcDecAble interface"u8);
        }
    }
}

// testStream implements the cipher.Stream interface.
[GoType] internal partial struct testStream {
}

[GoRecv] internal static void XORKeyStream(this ref testStream _, slice<byte> a, slice<byte> b) {
}

[GoRecv] internal static bool InAESPackage(this ref testStream _) {
    return true;
}

// Test the ctrAble interface is detected correctly by the cipher package.
public static void TestCTRAble(ж<testing.T> Ꮡt) {
    var b = ((cipher.Block)new aes_internal_test_package.testBlockжBlock(Ꮡ(new testBlock(nil))));
    {
        var (_, ok) = b._<ctrAble>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("testBlock does not implement the ctrAble interface"u8);
        }
    }
    var s = cipher.NewCTR(b, new byte[]{}.slice());
    {
        var (_, ok) = s._<testInterface>(ᐧ); if (!ok) {
            Ꮡt.Fatalf("cipher.NewCTR did not use ctrAble interface"u8);
        }
    }
}

} // end aes_internal_test_package
