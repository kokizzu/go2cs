// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto.@internal;

using bytes = bytes_package;
using elliptic = go.crypto.elliptic_package;
using Δnistec = go.crypto.@internal.nistec_package;
using fmt = fmt_package;
using testenv = go.@internal.testenv_package;
using big = math.big_package;
using rand = math.rand_package;
using testing = testing_package;
using go.@internal;
using go.crypto;
using go.crypto.@internal;
using math;

partial class nistec_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸbytes() {
    builtin.initPackage(typeof(bytes_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸcryptoꓸelliptic() {
    builtin.initPackage(typeof(go.crypto.elliptic_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸcryptoꓸinternalꓸnistec() {
    builtin.initPackage(typeof(go.crypto.@internal.nistec_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸfmt() {
    builtin.initPackage(typeof(fmt_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸtestenv() {
    builtin.initPackage(typeof(go.@internal.testenv_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸmathꓸbig() {
    builtin.initPackage(typeof(math.big_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸmathꓸrand() {
    builtin.initPackage(typeof(math.rand_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸtesting() {
    builtin.initPackage(typeof(testing_package));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string p224ˢ = "P224"u8;
private static readonly @string p256ˢ = "P256"u8;
private static readonly @string p384ˢ = "P384"u8;
private static readonly @string p521ˢ = "P521"u8;

public static void TestAllocations(ж<testing.T> Ꮡt) {
    testenv.SkipIfOptimizationOff(new testing_TжTB(Ꮡt));
    Ꮡt.Run(p224ˢ, (ж<testing.T> tΔ1) => {
        {
            var allocs = testing.AllocsPerRun(10, () => {
                var p = Δnistec.NewP224Point().SetGenerator();
                var scalar = new slice<byte>(28);
                rand.Read(scalar);
                p.ScalarBaseMult(scalar);
                p.ScalarMult(p, scalar);
                var @out = p.Bytes();
                {
                    var (_, err) = Δnistec.NewP224Point().SetBytes(@out); if (err != default!) {
                        tΔ1.Fatal(err);
                    }
                }
                @out = p.BytesCompressed();
                {
                    var (_, err) = p.SetBytes(@out); if (err != default!) {
                        tΔ1.Fatal(err);
                    }
                }
            }); if (allocs > 0D) {
                tΔ1.Errorf("expected zero allocations, got %0.1f"u8, allocs);
            }
        }
    });
    Ꮡt.Run(p256ˢ, (ж<testing.T> tΔ2) => {
        {
            var allocs = testing.AllocsPerRun(10, () => {
                var p = Δnistec.NewP256Point().SetGenerator();
                var scalar = new slice<byte>(32);
                rand.Read(scalar);
                p.ScalarBaseMult(scalar);
                p.ScalarMult(p, scalar);
                var @out = p.Bytes();
                {
                    var (_, err) = Δnistec.NewP256Point().SetBytes(@out); if (err != default!) {
                        tΔ2.Fatal(err);
                    }
                }
                @out = p.BytesCompressed();
                {
                    var (_, err) = p.SetBytes(@out); if (err != default!) {
                        tΔ2.Fatal(err);
                    }
                }
            }); if (allocs > 0D) {
                tΔ2.Errorf("expected zero allocations, got %0.1f"u8, allocs);
            }
        }
    });
    Ꮡt.Run(p384ˢ, (ж<testing.T> tΔ3) => {
        {
            var allocs = testing.AllocsPerRun(10, () => {
                var p = Δnistec.NewP384Point().SetGenerator();
                var scalar = new slice<byte>(48);
                rand.Read(scalar);
                p.ScalarBaseMult(scalar);
                p.ScalarMult(p, scalar);
                var @out = p.Bytes();
                {
                    var (_, err) = Δnistec.NewP384Point().SetBytes(@out); if (err != default!) {
                        tΔ3.Fatal(err);
                    }
                }
                @out = p.BytesCompressed();
                {
                    var (_, err) = p.SetBytes(@out); if (err != default!) {
                        tΔ3.Fatal(err);
                    }
                }
            }); if (allocs > 0D) {
                tΔ3.Errorf("expected zero allocations, got %0.1f"u8, allocs);
            }
        }
    });
    Ꮡt.Run(p521ˢ, (ж<testing.T> tΔ4) => {
        {
            var allocs = testing.AllocsPerRun(10, () => {
                var p = Δnistec.NewP521Point().SetGenerator();
                var scalar = new slice<byte>(66);
                rand.Read(scalar);
                p.ScalarBaseMult(scalar);
                p.ScalarMult(p, scalar);
                var @out = p.Bytes();
                {
                    var (_, err) = Δnistec.NewP521Point().SetBytes(@out); if (err != default!) {
                        tΔ4.Fatal(err);
                    }
                }
                @out = p.BytesCompressed();
                {
                    var (_, err) = p.SetBytes(@out); if (err != default!) {
                        tΔ4.Fatal(err);
                    }
                }
            }); if (allocs > 0D) {
                tΔ4.Errorf("expected zero allocations, got %0.1f"u8, allocs);
            }
        }
    });
}

[GoType] partial interface nistPoint<T> {
    slice<byte> Bytes();
    T SetGenerator();
    (T, error) SetBytes(slice<byte> _Δp0);
    T Add(T _Δp0, T _Δp1);
    T Double(T _Δp0);
    (T, error) ScalarMult(T _Δp0, slice<byte> _Δp1);
    (T, error) ScalarBaseMult(slice<byte> _Δp0);
}

public static void TestEquivalents(ж<testing.T> Ꮡt) {
    Ꮡt.Run(p224ˢ, (ж<testing.T> tΔ1) => {
        testEquivalents<P224PointжnistPoint>(tΔ1, () => Δnistec.NewP224Point(), elliptic.P224());
    });
    Ꮡt.Run(p256ˢ, (ж<testing.T> tΔ2) => {
        testEquivalents<P256PointжnistPoint>(tΔ2, () => Δnistec.NewP256Point(), elliptic.P256());
    });
    Ꮡt.Run(p384ˢ, (ж<testing.T> tΔ3) => {
        testEquivalents<P384PointжnistPoint>(tΔ3, () => Δnistec.NewP384Point(), elliptic.P384());
    });
    Ꮡt.Run(p521ˢ, (ж<testing.T> tΔ4) => {
        testEquivalents<P521PointжnistPoint>(tΔ4, () => Δnistec.NewP521Point(), elliptic.P521());
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object pP2Pˢ = (@string)"P+P != 2*P"u8;
private static readonly object pP2Pˢ2 = (@string)"P+P != [2]P"u8;
private static readonly object gG2Gˢ = (@string)"G+G != [2]G"u8;
private static readonly object pPN2Pˢ = (@string)"P+P != [N+2]P"u8;
private static readonly object gGN2Gˢ = (@string)"G+G != [N+2]G"u8;

internal static void testEquivalents<P>(ж<testing.T> Ꮡt, Func<P> newPoint, elliptic.Curve c)
    where P : nistPoint<P>
{
    var p = newPoint().SetGenerator();
    nint elementSize = ((~c.Params()).BitSize + 7) / 8;
    var two = new slice<byte>(elementSize);
    two[len(two) - 1] = 2;
    var nPlusTwo = new slice<byte>(elementSize);
    @new<bigꓸInt>().Add((~c.Params()).N, big.NewInt(2)).FillBytes(nPlusTwo);
    var p1 = newPoint().Double(p);
    var p2 = newPoint().Add(p, p);
    var (p3, err) = newPoint().ScalarMult(p, two);
    fatalIfErr(Ꮡt, err);
    (var p4, err) = newPoint().ScalarBaseMult(two);
    fatalIfErr(Ꮡt, err);
    (var p5, err) = newPoint().ScalarMult(p, nPlusTwo);
    fatalIfErr(Ꮡt, err);
    (var p6, err) = newPoint().ScalarBaseMult(nPlusTwo);
    fatalIfErr(Ꮡt, err);
    if (!bytes.Equal(p1.Bytes(), p2.Bytes())) {
        Ꮡt.Error(pP2Pˢ);
    }
    if (!bytes.Equal(p1.Bytes(), p3.Bytes())) {
        Ꮡt.Error(pP2Pˢ2);
    }
    if (!bytes.Equal(p1.Bytes(), p4.Bytes())) {
        Ꮡt.Error(gG2Gˢ);
    }
    if (!bytes.Equal(p1.Bytes(), p5.Bytes())) {
        Ꮡt.Error(pPN2Pˢ);
    }
    if (!bytes.Equal(p1.Bytes(), p6.Bytes())) {
        Ꮡt.Error(gGN2Gˢ);
    }
}

public static void TestScalarMult(ж<testing.T> Ꮡt) {
    Ꮡt.Run(p224ˢ, (ж<testing.T> tΔ1) => {
        testScalarMult<P224PointжnistPoint>(tΔ1, () => Δnistec.NewP224Point(), elliptic.P224());
    });
    Ꮡt.Run(p256ˢ, (ж<testing.T> tΔ2) => {
        testScalarMult<P256PointжnistPoint>(tΔ2, () => Δnistec.NewP256Point(), elliptic.P256());
    });
    Ꮡt.Run(p384ˢ, (ж<testing.T> tΔ3) => {
        testScalarMult<P384PointжnistPoint>(tΔ3, () => Δnistec.NewP384Point(), elliptic.P384());
    });
    Ꮡt.Run(p521ˢ, (ж<testing.T> tΔ4) => {
        testScalarMult<P521PointжnistPoint>(tΔ4, () => Δnistec.NewP521Point(), elliptic.P521());
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object kGScalarBaseMultKˢ = (@string)"[k]G != ScalarBaseMult(k)"u8;
private static readonly object scalarBaseMultKˢ = (@string)"ScalarBaseMult(k) != ∞"u8;
private static readonly object scalarBaseMultKˢ2 = (@string)"ScalarBaseMult(k) == ∞"u8;
private static readonly object nKGKGˢ = (@string)"[N - k]G + [k]G != ∞"u8;
private static readonly @string all1sˢ = "all1s"u8;

internal static void testScalarMult<P>(ж<testing.T> Ꮡt, Func<P> newPoint, elliptic.Curve c)
    where P : nistPoint<P>
{
    var G = newPoint().SetGenerator();
    var Gʗ1 = G;
    void checkScalar(ж<testing.T> tΔ1, slice<byte> scalar) {
        var (p1, err) = newPoint().ScalarBaseMult(scalar);
        fatalIfErr(tΔ1, err);
        (var p2, err) = newPoint().ScalarMult(Gʗ1, scalar);
        fatalIfErr(tΔ1, err);
        if (!bytes.Equal(p1.Bytes(), p2.Bytes())) {
            tΔ1.Error(kGScalarBaseMultKˢ);
        }
        var expectInfinity = @new<bigꓸInt>().Mod(@new<bigꓸInt>().SetBytes(scalar), (~c.Params()).N).Sign() == 0;
        if (expectInfinity){
            if (!bytes.Equal(p1.Bytes(), newPoint().Bytes())) {
                tΔ1.Error(scalarBaseMultKˢ);
            }
            if (!bytes.Equal(p2.Bytes(), newPoint().Bytes())) {
                tΔ1.Error((@string)"[k]G != ∞"u8);
            }
        } else {
            if (bytes.Equal(p1.Bytes(), newPoint().Bytes())) {
                tΔ1.Error(scalarBaseMultKˢ2);
            }
            if (bytes.Equal(p2.Bytes(), newPoint().Bytes())) {
                tΔ1.Error((@string)"[k]G == ∞"u8);
            }
        }
        var d = @new<bigꓸInt>().SetBytes(scalar);
        d.Sub((~c.Params()).N, d);
        d.Mod(d, (~c.Params()).N);
        (var g1, err) = newPoint().ScalarBaseMult(d.FillBytes(new slice<byte>(len(scalar))));
        fatalIfErr(tΔ1, err);
        g1.Add(g1, p1);
        if (!bytes.Equal(g1.Bytes(), newPoint().Bytes())) {
            tΔ1.Error(nKGKGˢ);
        }
    }
    nint byteLen = len((~c.Params()).N.Bytes());
    nint bitLen = (~c.Params()).N.BitLen();
    var checkScalarʗ1 = checkScalar;
    Ꮡt.Run("0"u8, (ж<testing.T> tΔ2) => {
        checkScalarʗ1(tΔ2, new slice<byte>(byteLen));
    });
    var checkScalarʗ2 = checkScalar;
    Ꮡt.Run("1"u8, (ж<testing.T> tΔ3) => {
        checkScalarʗ2(tΔ3, big.NewInt(1).FillBytes(new slice<byte>(byteLen)));
    });
    var checkScalarʗ3 = checkScalar;
    Ꮡt.Run("N-1"u8, (ж<testing.T> tΔ4) => {
        checkScalarʗ3(tΔ4, @new<bigꓸInt>().Sub((~c.Params()).N, big.NewInt(1)).Bytes());
    });
    var checkScalarʗ4 = checkScalar;
    Ꮡt.Run("N"u8, (ж<testing.T> tΔ5) => {
        checkScalarʗ4(tΔ5, (~c.Params()).N.Bytes());
    });
    var checkScalarʗ5 = checkScalar;
    Ꮡt.Run("N+1"u8, (ж<testing.T> tΔ6) => {
        checkScalarʗ5(tΔ6, @new<bigꓸInt>().Add((~c.Params()).N, big.NewInt(1)).Bytes());
    });
    var checkScalarʗ6 = checkScalar;
    Ꮡt.Run(all1sˢ, (ж<testing.T> tΔ7) => {
        var s = @new<bigꓸInt>().Lsh(big.NewInt(1), (nuint)bitLen);
        s.Sub(s, big.NewInt(1));
        checkScalarʗ6(tΔ7, s.Bytes());
    });
    if (testing.Short()) {
        return;
    }
    for (nint iᴛ1 = 0; iᴛ1 < bitLen; iᴛ1++) {
        var i = iᴛ1;
        var checkScalarʗ7 = checkScalar;
        Ꮡt.Run(fmt.Sprintf("1<<%d"u8, i), (ж<testing.T> tΔ8) => {
            var s = @new<bigꓸInt>().Lsh(big.NewInt(1), (nuint)i);
            checkScalarʗ7(tΔ8, s.FillBytes(new slice<byte>(byteLen)));
        });
    }
    for (nint iᴛ2 = 0; iᴛ2 <= 64; iᴛ2++) {
        var i = iᴛ2;
        var checkScalarʗ8 = checkScalar;
        Ꮡt.Run(fmt.Sprintf("%d"u8, i), (ж<testing.T> tΔ9) => {
            checkScalarʗ8(tΔ9, big.NewInt((int64)i).FillBytes(new slice<byte>(byteLen)));
        });
    }
    // Test N-64...N+64 since they risk overlapping with precomputed table values
    // in the final additions.
    for (var iᴛ3 = (int64)(-64); iᴛ3 <= 64; iᴛ3++) {
        var i = iᴛ3;
        var checkScalarʗ9 = checkScalar;
        Ꮡt.Run(fmt.Sprintf("N%+d"u8, i), (ж<testing.T> tΔ10) => {
            checkScalarʗ9(tΔ10, @new<bigꓸInt>().Add((~c.Params()).N, big.NewInt(i)).Bytes());
        });
    }
}

internal static void fatalIfErr(ж<testing.T> Ꮡt, error err) {
    Ꮡt.Helper();
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
}

public static void BenchmarkScalarMult(ж<testing.B> Ꮡb) {
    Ꮡb.Run(p224ˢ, (ж<testing.B> bΔ1) => {
        benchmarkScalarMult<P224PointжnistPoint>(bΔ1, Δnistec.NewP224Point().SetGenerator(), 28);
    });
    Ꮡb.Run(p256ˢ, (ж<testing.B> bΔ2) => {
        benchmarkScalarMult<P256PointжnistPoint>(bΔ2, Δnistec.NewP256Point().SetGenerator(), 32);
    });
    Ꮡb.Run(p384ˢ, (ж<testing.B> bΔ3) => {
        benchmarkScalarMult<P384PointжnistPoint>(bΔ3, Δnistec.NewP384Point().SetGenerator(), 48);
    });
    Ꮡb.Run(p521ˢ, (ж<testing.B> bΔ4) => {
        benchmarkScalarMult<P521PointжnistPoint>(bΔ4, Δnistec.NewP521Point().SetGenerator(), 66);
    });
}

internal static void benchmarkScalarMult<P>(ж<testing.B> Ꮡb, P p, nint scalarSize)
    where P : nistPoint<P>
{
    ref var b = ref Ꮡb.DerefOrNull();

    var scalar = new slice<byte>(scalarSize);
    rand.Read(scalar);
    b.ReportAllocs();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        p.ScalarMult(p, scalar);
    }
}

public static void BenchmarkScalarBaseMult(ж<testing.B> Ꮡb) {
    Ꮡb.Run(p224ˢ, (ж<testing.B> bΔ1) => {
        benchmarkScalarBaseMult<P224PointжnistPoint>(bΔ1, Δnistec.NewP224Point().SetGenerator(), 28);
    });
    Ꮡb.Run(p256ˢ, (ж<testing.B> bΔ2) => {
        benchmarkScalarBaseMult<P256PointжnistPoint>(bΔ2, Δnistec.NewP256Point().SetGenerator(), 32);
    });
    Ꮡb.Run(p384ˢ, (ж<testing.B> bΔ3) => {
        benchmarkScalarBaseMult<P384PointжnistPoint>(bΔ3, Δnistec.NewP384Point().SetGenerator(), 48);
    });
    Ꮡb.Run(p521ˢ, (ж<testing.B> bΔ4) => {
        benchmarkScalarBaseMult<P521PointжnistPoint>(bΔ4, Δnistec.NewP521Point().SetGenerator(), 66);
    });
}

internal static void benchmarkScalarBaseMult<P>(ж<testing.B> Ꮡb, P p, nint scalarSize)
    where P : nistPoint<P>
{
    ref var b = ref Ꮡb.DerefOrNull();

    var scalar = new slice<byte>(scalarSize);
    rand.Read(scalar);
    b.ReportAllocs();
    b.ResetTimer();
    for (nint i = 0; i < b.N; i++) {
        p.ScalarBaseMult(scalar);
    }
}

} // end nistec_test_package
