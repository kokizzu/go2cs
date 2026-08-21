// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("crypto/internal/bigmod/nat_test.go", "nat_test.cs", "ABUkgoKClKrCgoKUpqKCgoKCgqaCgoK4ooKCgoKmgoKCuIKCgoKCgoKCgoKCgoKUlIIACwiCgpQAFTKCgoKAgtrKppSC9oIAKVqCgoKCgpSUgoKUgIK4goKCgpSWgoIACQiCggANJIKCgsqCgoKCgoKCuIKCgoKCgoKUgoKCuIKCgoKCgoKUgoKCuIKCgoKCgoK4goKCgoKCgv7EgoKEgoKEgpaCgoKChIK4oqaUgqaCgoKokoKCgoKmgqaCgoKUpoKCgpSmooKChIKCuKKCgoSCgriigoSCgriigoKChIKCuKKCgoSCgriigoKCgoKChIKCuKKCgoKEgoK4goKCgpaCgoI=")]

namespace go.crypto.@internal;

using fmt = fmt_package;
using big = math.big_package;
using bits = math.bits_package;
using rand = math.rand_package;
using reflect = reflect_package;
using strings = strings_package;
using testing = testing_package;
using quick = go.testing.quick_package;
using go.testing;
using math;
using static go.crypto.@internal.bigmod_package;

partial class bigmod_internal_test_package {

[GoRecv] internal static @string String(this ref global::go.crypto.@internal.bigmod_package.ΔNat n) {
    slice<@string> limbs = default!;
    foreach (var (i, _) in n.limbs) {
        limbs = append(limbs, fmt.Sprintf("%016X"u8, n.limbs[len(n.limbs) - 1 - i]));
    }
    return "{"u8 + strings.Join(limbs, " "u8) + "}"u8;
}

// Generate generates an even nat. It's used by testing/quick to produce random
// *nat values for quick.Check invocations.
[GoRecv] internal static reflectꓸValue Generate(this ref global::go.crypto.@internal.bigmod_package.ΔNat _, ж<rand.Rand> Ꮡr, nint size) {
    ref var r = ref Ꮡr.DerefOrNull();

    var limbs = new slice<nuint>(size);
    for (nint i = 0; i < size; i++) {
        limbs[i] = (nuint)((nuint)r.Uint64() & (nuint)(unchecked((nuint)(18446744073709551614UL))));
    }
    return reflect.ValueOf(Ꮡ(new ΔNat(limbs)));
}

internal static bool testModAddCommutative(ж<global::go.crypto.@internal.bigmod_package.ΔNat> Ꮡa, ж<global::go.crypto.@internal.bigmod_package.ΔNat> Ꮡb) {
    ref var a = ref Ꮡa.DerefOrNull();

    var m = maxModulus((nuint)len(a.limbs));
    var aPlusB = @new<global::go.crypto.@internal.bigmod_package.ΔNat>().set(Ꮡa);
    aPlusB.Add(Ꮡb, m);
    var bPlusA = @new<global::go.crypto.@internal.bigmod_package.ΔNat>().set(Ꮡb);
    bPlusA.Add(Ꮡa, m);
    return aPlusB.Equal(bPlusA) == 1;
}

public static void TestModAddCommutative(ж<testing.T> Ꮡt) {
    var err = quick.Check(testModAddCommutative, Ꮡ(new quick.Config(nil)));
    if (err != default!) {
        Ꮡt.Error(err);
    }
}

internal static bool testModSubThenAddIdentity(ж<global::go.crypto.@internal.bigmod_package.ΔNat> Ꮡa, ж<global::go.crypto.@internal.bigmod_package.ΔNat> Ꮡb) {
    ref var a = ref Ꮡa.DerefOrNull();

    var m = maxModulus((nuint)len(a.limbs));
    var original = @new<global::go.crypto.@internal.bigmod_package.ΔNat>().set(Ꮡa);
    Ꮡa.Sub(Ꮡb, m);
    Ꮡa.Add(Ꮡb, m);
    return a.Equal(original) == 1;
}

public static void TestModSubThenAddIdentity(ж<testing.T> Ꮡt) {
    var err = quick.Check(testModSubThenAddIdentity, Ꮡ(new quick.Config(nil)));
    if (err != default!) {
        Ꮡt.Error(err);
    }
}

public static void TestMontgomeryRoundtrip(ж<testing.T> Ꮡt) {
    var err = quick.Check(bool (ж<global::go.crypto.@internal.bigmod_package.ΔNat> a) => {
        var one = Ꮡ(new ΔNat(new slice<nuint>(len((~a).limbs))));
        one.Value.limbs[0] = 1;
        var aPlusOne = @new<bigꓸInt>().SetBytes(natBytes(a));
        aPlusOne.Add(aPlusOne, big.NewInt(1));
        var (m, _) = NewModulusFromBig(aPlusOne);
        var monty = @new<global::go.crypto.@internal.bigmod_package.ΔNat>().set(a);
        monty.montgomeryRepresentation(m);
        var aAgain = @new<global::go.crypto.@internal.bigmod_package.ΔNat>().set(monty);
        aAgain.montgomeryMul(monty, one, m);
        if (a.Equal(aAgain) != 1) {
            Ꮡt.Errorf("%v != %v"u8, a.OrTypedNil(), aAgain.OrTypedNil());
            return false;
        }
        return true;
    }, Ꮡ(new quick.Config(nil)));
    if (err != default!) {
        Ꮡt.Error(err);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object examplesAreOnlyValidIn64ˢ = (@string)"examples are only valid in 64 bit"u8;

[GoType("dyn")] internal partial struct TestShiftIn_examples {
    internal slice<byte> m, x, expected;
    internal uint64 y;
}

public static void TestShiftIn(ж<testing.T> Ꮡt) {
    if (bits.UintSize != 64) {
        Ꮡt.Skip(examplesAreOnlyValidIn64ˢ);
    }
    var examples = new TestShiftIn_examples[]{new(
        m: new byte[]{13}.slice(),
        x: new byte[]{0}.slice(),
        y: 0xFFFF_FFFF_FFFF_FFFFUL,
        expected: new byte[]{2}.slice()
    ), new(
        m: new byte[]{13}.slice(),
        x: new byte[]{7}.slice(),
        y: 0xFFFF_FFFF_FFFF_FFFFUL,
        expected: new byte[]{10}.slice()
    ), new(
        m: new byte[]{0x06, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d}.slice(),
        x: new slice<byte>(9),
        y: 0xFFFF_FFFF_FFFF_FFFFUL,
        expected: new byte[]{0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice()
    ), new(
        m: new byte[]{0x06, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d}.slice(),
        x: new byte[]{0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        y: 0,
        expected: new byte[]{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06}.slice()
    )
    }.slice();
    foreach (var (i, tt) in examples) {
        var m = modulusFromBytes(tt.m);
        var got = natFromBytes(tt.x).ExpandFor(m).shiftIn((nuint)tt.y, m);
        {
            var exp = natFromBytes(tt.expected).ExpandFor(m); if (got.Equal(exp) != 1) {
                Ꮡt.Errorf("%d: got %v, expected %v"u8, i, got.OrTypedNil(), exp.OrTypedNil());
            }
        }
    }
}

public static void TestModulusAndNatSizes(ж<testing.T> Ꮡt) {
    // These are 126 bit (2 * _W on 64-bit architectures) values, serialized as
    // 128 bits worth of bytes. If leading zeroes are stripped, they fit in two
    // limbs, if they are not, they fit in three. This can be a problem because
    // modulus strips leading zeroes and nat does not.
    var m = modulusFromBytes(new byte[]{
        0x3f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice());
    var xb = new byte[]{0x3f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}.slice();
    natFromBytes(xb).ExpandFor(m); // must not panic for shrinking
    NewNat().SetBytes(xb, m);
}

[GoType("dyn")] internal partial struct TestSetBytes_tests {
    internal slice<byte> m, b;
    internal bool fail;
}

public static void TestSetBytes(ж<testing.T> Ꮡt) {
    var tests = new TestSetBytes_tests[]{new(
        m: new byte[]{0xff, 0xff}.slice(),
        b: new byte[]{0x00, 0x01}.slice()
    ), new(
        m: new byte[]{0xff, 0xff}.slice(),
        b: new byte[]{0xff, 0xff}.slice(),
        fail: true
    ), new(
        m: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        b: new byte[]{0x00, 0x01}.slice()
    ), new(
        m: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        b: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}.slice()
    ), new(
        m: new byte[]{0xff, 0xff}.slice(),
        b: new byte[]{0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}.slice(),
        fail: true
    ), new(
        m: new byte[]{0xff, 0xff}.slice(),
        b: new byte[]{0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}.slice(),
        fail: true
    ), new(
        m: new byte[]{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        b: new byte[]{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}.slice()
    ), new(
        m: new byte[]{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        b: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}.slice(),
        fail: true
    ), new(
        m: new byte[]{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        b: new byte[]{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        fail: true
    ), new(
        m: new byte[]{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        b: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}.slice(),
        fail: true
    ), new(
        m: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfd}.slice(),
        b: new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}.slice(),
        fail: true
    )
    }.slice();
    foreach (var (i, tt) in tests) {
        var m = modulusFromBytes(tt.m);
        var (got, errΔ1) = NewNat().SetBytes(tt.b, m);
        if (errΔ1 != default!) {
            if (!tt.fail) {
                Ꮡt.Errorf("%d: unexpected error: %v"u8, i, errΔ1);
            }
            continue;
        }
        if (tt.fail) {
            Ꮡt.Errorf("%d: unexpected success"u8, i);
            continue;
        }
        {
            var expected = natFromBytes(tt.b).ExpandFor(m); if (got.Equal(expected) != yes) {
                Ꮡt.Errorf("%d: got %v, expected %v"u8, i, got.OrTypedNil(), expected.OrTypedNil());
            }
        }
    }
    var f = (slice<byte> xBytes) => {
        var m = maxModulus((nuint)(len(xBytes) * 8 / (nint)_W + 1));
        var (got, errΔ2) = NewNat().SetBytes(xBytes, m);
        if (errΔ2 != default!) {
            return false;
        }
        return got.Equal(natFromBytes(xBytes).ExpandFor(m)) == yes;
    };
    var err = quick.Check(f, Ꮡ(new quick.Config(nil)));
    if (err != default!) {
        Ꮡt.Error(err);
    }
}

[GoType("dyn")] internal partial struct TestExpand_examples {
    internal slice<nuint> @in;
    internal nint n;
    internal slice<nuint> @out;
}

public static void TestExpand(ж<testing.T> Ꮡt) {
    var sliced = new nuint[]{1, 2, 3, 4}.slice();
    var examples = new TestExpand_examples[]{new(
        new nuint[]{1, 2}.slice(),
        4,
        new nuint[]{1, 2, 0, 0}.slice()
    ), new(
        sliced[..2],
        4,
        new nuint[]{1, 2, 0, 0}.slice()
    ), new(
        new nuint[]{1, 2}.slice(),
        2,
        new nuint[]{1, 2}.slice()
    )
    }.slice();
    foreach (var (i, tt) in examples) {
        var got = (Ꮡ(new ΔNat(tt.@in))).expand(tt.n);
        if (len((~got).limbs) != len(tt.@out) || got.Equal(Ꮡ(new ΔNat(tt.@out))) != 1) {
            Ꮡt.Errorf("%d: got %v, expected %v"u8, i, got.OrTypedNil(), tt.@out);
        }
    }
}

public static void TestMod(ж<testing.T> Ꮡt) {
    var m = modulusFromBytes(new byte[]{0x06, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d}.slice());
    var x = natFromBytes(new byte[]{0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01}.slice());
    var @out = @new<global::go.crypto.@internal.bigmod_package.ΔNat>();
    @out.Mod(x, m);
    var expected = natFromBytes(new byte[]{0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09}.slice());
    if (@out.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, @out.OrTypedNil(), expected.OrTypedNil());
    }
}

public static void TestModSub(ж<testing.T> Ꮡt) {
    var m = modulusFromBytes(new byte[]{13}.slice());
    var x = Ꮡ(new ΔNat(new nuint[]{6}.slice()));
    var y = Ꮡ(new ΔNat(new nuint[]{7}.slice()));
    x.Sub(y, m);
    var expected = Ꮡ(new ΔNat(new nuint[]{12}.slice()));
    if (x.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, x.OrTypedNil(), expected.OrTypedNil());
    }
    x.Sub(y, m);
    expected = Ꮡ(new ΔNat(new nuint[]{5}.slice()));
    if (x.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, x.OrTypedNil(), expected.OrTypedNil());
    }
}

public static void TestModAdd(ж<testing.T> Ꮡt) {
    var m = modulusFromBytes(new byte[]{13}.slice());
    var x = Ꮡ(new ΔNat(new nuint[]{6}.slice()));
    var y = Ꮡ(new ΔNat(new nuint[]{7}.slice()));
    x.Add(y, m);
    var expected = Ꮡ(new ΔNat(new nuint[]{0}.slice()));
    if (x.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, x.OrTypedNil(), expected.OrTypedNil());
    }
    x.Add(y, m);
    expected = Ꮡ(new ΔNat(new nuint[]{7}.slice()));
    if (x.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, x.OrTypedNil(), expected.OrTypedNil());
    }
}

public static void TestExp(ж<testing.T> Ꮡt) {
    var m = modulusFromBytes(new byte[]{13}.slice());
    var x = Ꮡ(new ΔNat(new nuint[]{3}.slice()));
    var @out = Ꮡ(new ΔNat(new nuint[]{0}.slice()));
    @out.Exp(x, new byte[]{12}.slice(), m);
    var expected = Ꮡ(new ΔNat(new nuint[]{1}.slice()));
    if (@out.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, @out.OrTypedNil(), expected.OrTypedNil());
    }
}

public static void TestExpShort(ж<testing.T> Ꮡt) {
    var m = modulusFromBytes(new byte[]{13}.slice());
    var x = Ꮡ(new ΔNat(new nuint[]{3}.slice()));
    var @out = Ꮡ(new ΔNat(new nuint[]{0}.slice()));
    @out.ExpShortVarTime(x, 12, m);
    var expected = Ꮡ(new ΔNat(new nuint[]{1}.slice()));
    if (@out.Equal(expected) != 1) {
        Ꮡt.Errorf("%+v != %+v"u8, @out.OrTypedNil(), expected.OrTypedNil());
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object aBModAB0ˢ = (@string)"a * b mod (a * b) != 0"u8;
internal static readonly object aInvAModB1ˢ = (@string)"a * inv(a) mod b != 1"u8;

// TestMulReductions tests that Mul reduces results equal or slightly greater
// than the modulus. Some Montgomery algorithms don't and need extra care to
// return correct results. See https://go.dev/issue/13907.
public static void TestMulReductions(ж<testing.T> Ꮡt) {
    // Two short but multi-limb primes.
    var (a, _) = @new<bigꓸInt>().SetString("773608962677651230850240281261679752031633236267106044359907"u8, 10);
    var (b, _) = @new<bigꓸInt>().SetString("180692823610368451951102211649591374573781973061758082626801"u8, 10);
    var n = @new<bigꓸInt>().Mul(a, b);
    var (N, _) = NewModulusFromBig(n);
    var A = NewNat().setBig(a).ExpandFor(N);
    var B = NewNat().setBig(b).ExpandFor(N);
    if (A.Mul(B, N).IsZero() != 1) {
        Ꮡt.Error(aBModAB0ˢ);
    }
    var i = @new<bigꓸInt>().ModInverse(a, b);
    (N, _) = NewModulusFromBig(b);
    A = NewNat().setBig(a).ExpandFor(N);
    var I = NewNat().setBig(i).ExpandFor(N);
    var one = NewNat().setBig(big.NewInt(1)).ExpandFor(N);
    if (A.Mul(I, N).Equal(one) != 1) {
        Ꮡt.Error(aInvAModB1ˢ);
    }
}

internal static slice<byte> natBytes(ж<global::go.crypto.@internal.bigmod_package.ΔNat> Ꮡn) {
    ref var n = ref Ꮡn.DerefOrNull();

    return n.Bytes(maxModulus((nuint)len(n.limbs)));
}

internal static ж<global::go.crypto.@internal.bigmod_package.ΔNat> natFromBytes(slice<byte> b) {
    // Must not use Nat.SetBytes as it's used in TestSetBytes.
    var bb = @new<bigꓸInt>().SetBytes(b);
    return NewNat().setBig(bb);
}

internal static ж<global::go.crypto.@internal.bigmod_package.Modulus> modulusFromBytes(slice<byte> b) {
    var bb = @new<bigꓸInt>().SetBytes(b);
    var (m, _) = NewModulusFromBig(bb);
    return m;
}

// maxModulus returns the biggest modulus that can fit in n limbs.
internal static ж<global::go.crypto.@internal.bigmod_package.Modulus> maxModulus(nuint n) {
    var b = big.NewInt(1);
    b.Lsh(b, n * (nuint)_W);
    b.Sub(b, big.NewInt(1));
    var (m, _) = NewModulusFromBig(b);
    return m;
}

internal static ж<global::go.crypto.@internal.bigmod_package.Modulus> makeBenchmarkModulus() {
    return maxModulus(32);
}

internal static ж<global::go.crypto.@internal.bigmod_package.ΔNat> makeBenchmarkValue() {
    var x = new slice<nuint>(32);
    for (nint i = 0; i < 32; i++) {
        x[i]--;
    }
    return Ꮡ(new ΔNat(limbs: x));
}

internal static slice<byte> makeBenchmarkExponent() {
    var e = new slice<byte>(256);
    for (nint i = 0; i < 32; i++) {
        e[i] = 0xFF;
    }
    return e;
}

public static void BenchmarkModAdd(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var x = makeBenchmarkValue();
    var y = makeBenchmarkValue();
    var m = makeBenchmarkModulus();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        x.Add(y, m);
    }
}

public static void BenchmarkModSub(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var x = makeBenchmarkValue();
    var y = makeBenchmarkValue();
    var m = makeBenchmarkModulus();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        x.Sub(y, m);
    }
}

public static void BenchmarkMontgomeryRepr(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var x = makeBenchmarkValue();
    var m = makeBenchmarkModulus();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        x.montgomeryRepresentation(m);
    }
}

public static void BenchmarkMontgomeryMul(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var x = makeBenchmarkValue();
    var y = makeBenchmarkValue();
    var @out = makeBenchmarkValue();
    var m = makeBenchmarkModulus();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        @out.montgomeryMul(x, y, m);
    }
}

public static void BenchmarkModMul(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var x = makeBenchmarkValue();
    var y = makeBenchmarkValue();
    var m = makeBenchmarkModulus();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        x.Mul(y, m);
    }
}

public static void BenchmarkExpBig(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var @out = @new<bigꓸInt>();
    var exponentBytes = makeBenchmarkExponent();
    var x = @new<bigꓸInt>().SetBytes(exponentBytes);
    var e = @new<bigꓸInt>().SetBytes(exponentBytes);
    var n = @new<bigꓸInt>().SetBytes(exponentBytes);
    var one = @new<bigꓸInt>().SetUint64(1);
    n.Add(n, one);
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        @out.Exp(x, e, n);
    }
}

public static void BenchmarkExp(ж<testing.B> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var x = makeBenchmarkValue();
    var e = makeBenchmarkExponent();
    var @out = makeBenchmarkValue();
    var m = makeBenchmarkModulus();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        @out.Exp(x, e, m);
    }
}

public static void TestNewModFromBigZero(ж<testing.T> Ꮡt) {
    @string expected = modulusMustBe0ˢ;
    var (_, err) = NewModulusFromBig(big.NewInt(0));
    if (err == default! || err.Error() != expected) {
        Ꮡt.Errorf("NewModulusFromBig(0) got %q, want %q"u8, err, expected);
    }
    expected = modulusMustBeOddˢ;
    (_, err) = NewModulusFromBig(big.NewInt(2));
    if (err == default! || err.Error() != expected) {
        Ꮡt.Errorf("NewModulusFromBig(2) got %q, want %q"u8, err, expected);
    }
}

} // end bigmod_internal_test_package
