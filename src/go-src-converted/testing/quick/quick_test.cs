// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.testing;

using rand = go.math.rand_package;
using reflect = reflect_package;
using testing = testing_package;
using go.math;

partial class quick_package {

internal static array<byte> fArray(array<byte> a) {
    a = a.Clone();

    return a.Clone();
}

[GoType("[4]byte")] partial struct TestArrayAlias;

internal static TestArrayAlias fArrayAlias(TestArrayAlias a) {
    a = a.Clone();

    return a.Clone();
}

internal static bool fBool(bool a) {
    return a;
}

[GoType("bool")] partial struct TestBoolAlias;

internal static TestBoolAlias fBoolAlias(TestBoolAlias a) {
    return a;
}

internal static float32 fFloat32(float32 a) {
    return a;
}

[GoType("num:float32")] partial struct TestFloat32Alias;

internal static TestFloat32Alias fFloat32Alias(TestFloat32Alias a) {
    return a;
}

internal static float64 fFloat64(float64 a) {
    return a;
}

[GoType("num:float64")] partial struct TestFloat64Alias;

internal static TestFloat64Alias fFloat64Alias(TestFloat64Alias a) {
    return a;
}

internal static complex64 fComplex64(complex64 a) {
    return a;
}

[GoType("num:complex64")] partial struct TestComplex64Alias;

internal static TestComplex64Alias fComplex64Alias(TestComplex64Alias a) {
    return a;
}

internal static complex128 fComplex128(complex128 a) {
    return a;
}

[GoType("num:complex128")] partial struct TestComplex128Alias;

internal static TestComplex128Alias fComplex128Alias(TestComplex128Alias a) {
    return a;
}

internal static int16 fInt16(int16 a) {
    return a;
}

[GoType("num:int16")] partial struct TestInt16Alias;

internal static TestInt16Alias fInt16Alias(TestInt16Alias a) {
    return a;
}

internal static int32 fInt32(int32 a) {
    return a;
}

[GoType("num:int32")] partial struct TestInt32Alias;

internal static TestInt32Alias fInt32Alias(TestInt32Alias a) {
    return a;
}

internal static int64 fInt64(int64 a) {
    return a;
}

[GoType("num:int64")] partial struct TestInt64Alias;

internal static TestInt64Alias fInt64Alias(TestInt64Alias a) {
    return a;
}

internal static int8 fInt8(int8 a) {
    return a;
}

[GoType("num:int8")] partial struct TestInt8Alias;

internal static TestInt8Alias fInt8Alias(TestInt8Alias a) {
    return a;
}

internal static nint fInt(nint a) {
    return a;
}

[GoType("num:nint")] partial struct TestIntAlias;

internal static TestIntAlias fIntAlias(TestIntAlias a) {
    return a;
}

internal static map<nint, nint> fMap(map<nint, nint> a) {
    return a;
}

[GoType("map[nint, nint]")] partial struct TestMapAlias;

internal static TestMapAlias fMapAlias(TestMapAlias a) {
    return a;
}

internal static ж<nint> fPtr(ж<nint> Ꮡa) {
    ref var a = ref Ꮡa.DerefOrNil();

    if (Ꮡa == nil) {
        return default!;
    }
    ref var b = ref heap<nint>(out var Ꮡb);
    b = a;
    return Ꮡb;
}

[GoType("ж<nint>")] partial class TestPtrAlias;

internal static TestPtrAlias fPtrAlias(TestPtrAlias a) {
    return a;
}

internal static slice<byte> fSlice(slice<byte> a) {
    return a;
}

[GoType("[]byte")] partial struct TestSliceAlias;

internal static TestSliceAlias fSliceAlias(TestSliceAlias a) {
    return a;
}

internal static @string fString(@string a) {
    return a;
}

[GoType("@string")] partial struct TestStringAlias;

internal static TestStringAlias fStringAlias(TestStringAlias a) {
    return a;
}

[GoType] partial struct TestStruct {
    public nint A;
    public @string B;
}

internal static TestStruct fStruct(TestStruct a) {
    return a;
}

[GoType("TestStruct")] partial struct TestStructAlias;

internal static TestStructAlias fStructAlias(TestStructAlias a) {
    return a;
}

internal static uint16 fUint16(uint16 a) {
    return a;
}

[GoType("num:uint16")] partial struct TestUint16Alias;

internal static TestUint16Alias fUint16Alias(TestUint16Alias a) {
    return a;
}

internal static uint32 fUint32(uint32 a) {
    return a;
}

[GoType("num:uint32")] partial struct TestUint32Alias;

internal static TestUint32Alias fUint32Alias(TestUint32Alias a) {
    return a;
}

internal static uint64 fUint64(uint64 a) {
    return a;
}

[GoType("num:uint64")] partial struct TestUint64Alias;

internal static TestUint64Alias fUint64Alias(TestUint64Alias a) {
    return a;
}

internal static uint8 fUint8(uint8 a) {
    return a;
}

[GoType("num:uint8")] partial struct TestUint8Alias;

internal static TestUint8Alias fUint8Alias(TestUint8Alias a) {
    return a;
}

internal static nuint fUint(nuint a) {
    return a;
}

[GoType("num:nuint")] partial struct TestUintAlias;

internal static TestUintAlias fUintAlias(TestUintAlias a) {
    return a;
}

internal static uintptr fUintptr(uintptr a) {
    return a;
}

[GoType("num:uintptr")] partial struct TestUintptrAlias;

internal static TestUintptrAlias fUintptrAlias(TestUintptrAlias a) {
    return a;
}

internal static void reportError(@string property, error err, ж<testing.T> Ꮡt) {
    if (err != default!) {
        Ꮡt.Errorf("%s: %s"u8, property, err);
    }
}

public static void TestCheckEqual(ж<testing.T> Ꮡt) {
    reportError("fArray"u8, CheckEqual(fArray, fArray, nil), Ꮡt);
    reportError("fArrayAlias"u8, CheckEqual(fArrayAlias, fArrayAlias, nil), Ꮡt);
    reportError("fBool"u8, CheckEqual(fBool, fBool, nil), Ꮡt);
    reportError("fBoolAlias"u8, CheckEqual(fBoolAlias, fBoolAlias, nil), Ꮡt);
    reportError("fFloat32"u8, CheckEqual(fFloat32, fFloat32, nil), Ꮡt);
    reportError("fFloat32Alias"u8, CheckEqual(fFloat32Alias, fFloat32Alias, nil), Ꮡt);
    reportError("fFloat64"u8, CheckEqual(fFloat64, fFloat64, nil), Ꮡt);
    reportError("fFloat64Alias"u8, CheckEqual(fFloat64Alias, fFloat64Alias, nil), Ꮡt);
    reportError("fComplex64"u8, CheckEqual(fComplex64, fComplex64, nil), Ꮡt);
    reportError("fComplex64Alias"u8, CheckEqual(fComplex64Alias, fComplex64Alias, nil), Ꮡt);
    reportError("fComplex128"u8, CheckEqual(fComplex128, fComplex128, nil), Ꮡt);
    reportError("fComplex128Alias"u8, CheckEqual(fComplex128Alias, fComplex128Alias, nil), Ꮡt);
    reportError("fInt16"u8, CheckEqual(fInt16, fInt16, nil), Ꮡt);
    reportError("fInt16Alias"u8, CheckEqual(fInt16Alias, fInt16Alias, nil), Ꮡt);
    reportError("fInt32"u8, CheckEqual(fInt32, fInt32, nil), Ꮡt);
    reportError("fInt32Alias"u8, CheckEqual(fInt32Alias, fInt32Alias, nil), Ꮡt);
    reportError("fInt64"u8, CheckEqual(fInt64, fInt64, nil), Ꮡt);
    reportError("fInt64Alias"u8, CheckEqual(fInt64Alias, fInt64Alias, nil), Ꮡt);
    reportError("fInt8"u8, CheckEqual(fInt8, fInt8, nil), Ꮡt);
    reportError("fInt8Alias"u8, CheckEqual(fInt8Alias, fInt8Alias, nil), Ꮡt);
    reportError("fInt"u8, CheckEqual(fInt, fInt, nil), Ꮡt);
    reportError("fIntAlias"u8, CheckEqual(fIntAlias, fIntAlias, nil), Ꮡt);
    reportError("fInt32"u8, CheckEqual(fInt32, fInt32, nil), Ꮡt);
    reportError("fInt32Alias"u8, CheckEqual(fInt32Alias, fInt32Alias, nil), Ꮡt);
    reportError("fMap"u8, CheckEqual(fMap, fMap, nil), Ꮡt);
    reportError("fMapAlias"u8, CheckEqual(fMapAlias, fMapAlias, nil), Ꮡt);
    reportError("fPtr"u8, CheckEqual(fPtr, fPtr, nil), Ꮡt);
    reportError("fPtrAlias"u8, CheckEqual(fPtrAlias, fPtrAlias, nil), Ꮡt);
    reportError("fSlice"u8, CheckEqual(fSlice, fSlice, nil), Ꮡt);
    reportError("fSliceAlias"u8, CheckEqual(fSliceAlias, fSliceAlias, nil), Ꮡt);
    reportError("fString"u8, CheckEqual(fString, fString, nil), Ꮡt);
    reportError("fStringAlias"u8, CheckEqual(fStringAlias, fStringAlias, nil), Ꮡt);
    reportError("fStruct"u8, CheckEqual(fStruct, fStruct, nil), Ꮡt);
    reportError("fStructAlias"u8, CheckEqual(fStructAlias, fStructAlias, nil), Ꮡt);
    reportError("fUint16"u8, CheckEqual(fUint16, fUint16, nil), Ꮡt);
    reportError("fUint16Alias"u8, CheckEqual(fUint16Alias, fUint16Alias, nil), Ꮡt);
    reportError("fUint32"u8, CheckEqual(fUint32, fUint32, nil), Ꮡt);
    reportError("fUint32Alias"u8, CheckEqual(fUint32Alias, fUint32Alias, nil), Ꮡt);
    reportError("fUint64"u8, CheckEqual(fUint64, fUint64, nil), Ꮡt);
    reportError("fUint64Alias"u8, CheckEqual(fUint64Alias, fUint64Alias, nil), Ꮡt);
    reportError("fUint8"u8, CheckEqual(fUint8, fUint8, nil), Ꮡt);
    reportError("fUint8Alias"u8, CheckEqual(fUint8Alias, fUint8Alias, nil), Ꮡt);
    reportError("fUint"u8, CheckEqual(fUint, fUint, nil), Ꮡt);
    reportError("fUintAlias"u8, CheckEqual(fUintAlias, fUintAlias, nil), Ꮡt);
    reportError("fUintptr"u8, CheckEqual(fUintptr, fUintptr, nil), Ꮡt);
    reportError("fUintptrAlias"u8, CheckEqual(fUintptrAlias, fUintptrAlias, nil), Ꮡt);
}

// This tests that ArbitraryValue is working by checking that all the arbitrary
// values of type MyStruct have x = 42.
[GoType] partial struct myStruct {
    internal nint x;
}

internal static reflectꓸValue Generate(this myStruct m, ж<rand.Rand> Ꮡr, nint _) {
    return reflect.ValueOf(new myStruct(x: 42));
}

internal static bool myStructProperty(myStruct @in) {
    return @in.x == 42;
}

public static void TestCheckProperty(ж<testing.T> Ꮡt) {
    reportError("myStructProperty"u8, Check(myStructProperty, nil), Ꮡt);
}

public static void TestFailure(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.Value;

    var f = (nint x) => false;
    var err = Check(f, nil);
    if (err == default!) {
        Ꮡt.Errorf("Check didn't return an error"u8);
    }
    {
        var (_, ok) = err._<ж<CheckError>>(ᐧ); if (!ok) {
            Ꮡt.Errorf("Error was not a CheckError: %s"u8, err);
        }
    }
    err = CheckEqual(fUint, fUint32, nil);
    if (err == default!) {
        Ꮡt.Errorf("#1 CheckEqual didn't return an error"u8);
    }
    {
        var (_, ok) = err._<SetupError>(ᐧ); if (!ok) {
            Ꮡt.Errorf("#1 Error was not a SetupError: %s"u8, err);
        }
    }
    err = CheckEqual((nint x, nint y) => {
    }, (nint x) => {
    }, nil);
    if (err == default!) {
        Ꮡt.Errorf("#2 CheckEqual didn't return an error"u8);
    }
    {
        var (_, ok) = err._<SetupError>(ᐧ); if (!ok) {
            Ꮡt.Errorf("#2 Error was not a SetupError: %s"u8, err);
        }
    }
    err = CheckEqual(nint (nint x) => 0, int32 (nint x) => 0, nil);
    if (err == default!) {
        Ꮡt.Errorf("#3 CheckEqual didn't return an error"u8);
    }
    {
        var (_, ok) = err._<SetupError>(ᐧ); if (!ok) {
            Ꮡt.Errorf("#3 Error was not a SetupError: %s"u8, err);
        }
    }
}

[GoLocalName("R")] [GoType("dyn")] partial struct TestRecursive_R {
    public ж<TestRecursive_R> Ptr;
    public slice<ж<TestRecursive_R>> SliceP;
    public slice<TestRecursive_R> Slice;
    public map<nint, TestRecursive_R> Map;
    public map<nint, ж<TestRecursive_R>> MapP;
    public map<ж<TestRecursive_R>, ж<TestRecursive_R>> MapR;
    public slice<map<nint, TestRecursive_R>> SliceMap;
}

// Recursive data structures didn't terminate.
// Issues 8818 and 11148.
public static void TestRecursive(ж<testing.T> Ꮡt) {
    var f = (TestRecursive_R r) => true;
    Check(f, nil);
}

public static void TestEmptyStruct(ж<testing.T> Ꮡt) {
    var f = (EmptyStruct _) => true;
    Check(f, nil);
}

[GoType] partial struct A {
    public ж<B> B;
}

[GoType] partial struct B {
    public ж<A> A;
}

public static void TestMutuallyRecursive(ж<testing.T> Ꮡt) {
    var f = (A a) => true;
    Check(f, nil);
}

[GoLocalName("Q")] [GoType("dyn")] partial struct TestNonZeroSliceAndMap_Q {
    public map<nint, nint> M;
    public slice<nint> S;
}

// Some serialization formats (e.g. encoding/pem) cannot distinguish
// between a nil and an empty map or slice, so avoid generating the
// zero value for these.
public static void TestNonZeroSliceAndMap(ж<testing.T> Ꮡt) {
    var f = (TestNonZeroSliceAndMap_Q q) => q.M != default! && q.S != default!;
    var err = Check(f, nil);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
}

public static void TestInt64(ж<testing.T> Ꮡt) {
    int64 lo = default!;
    int64 hi = default!;
    var f = (int64 x) => {
        if (x < lo) {
            lo = x;
        }
        if (x > hi) {
            hi = x;
        }
        return true;
    };
    var cfg = Ꮡ(new Config(MaxCount: 10000));
    Check(f, cfg);
    if (((uint64)lo >> (int)(62)) == 0 || ((uint64)hi >> (int)(62)) == 0) {
        Ꮡt.Errorf("int64 returned range %#016x,%#016x; does not look like full range"u8, lo, hi);
    }
}

} // end quick_package
