// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.hash;

using bytes = bytes_package;
using fmt = fmt_package;
using hash = hash_package;
using testing = testing_package;
using static go.hash.maphash_package;

partial class maphash_internal_test_package {

public static void TestUnseededHash(ж<testing.T> Ꮡt) {
    var m = new map<uint64, EmptyStruct>{};
    for (nint i = 0; i < 1000; i++) {
        var h = @new<global::go.hash.maphash_package.Hash>();
        m[h.Sum64()] = new EmptyStruct();
    }
    if (len(m) < 900) {
        Ꮡt.Errorf("empty hash not sufficiently random: got %d, want 1000"u8, len(m));
    }
}

public static void TestSeededHash(ж<testing.T> Ꮡt) {
    var s = MakeSeed();
    var m = new map<uint64, EmptyStruct>{};
    for (nint i = 0; i < 1000; i++) {
        var h = @new<global::go.hash.maphash_package.Hash>();
        h.SetSeed(s);
        m[h.Sum64()] = new EmptyStruct();
    }
    if (len(m) != 1) {
        Ꮡt.Errorf("seeded hash is random: got %d, want 1"u8, len(m));
    }
}

public static void TestHashGrouping(ж<testing.T> Ꮡt) {
    var b = bytes.Repeat(slice<byte>("foo"u8), 100);
    var hh = new slice<ж<global::go.hash.maphash_package.Hash>>(7);
    foreach (var (i, _) in hh) {
        hh[i] = @new<global::go.hash.maphash_package.Hash>();
    }
    foreach (var (_, h) in hh[1..]) {
        h.SetSeed(hh[0].Seed());
    }
    hh[0].Write(b);
    hh[1].WriteString(((@string)b));
    var writeByte = (ж<global::go.hash.maphash_package.Hash> h, byte bΔ1) => {
        var err = h.WriteByte(bΔ1);
        if (err != default!) {
            Ꮡt.Fatalf("WriteByte: %v"u8, err);
        }
    };
    var writeSingleByte = (ж<global::go.hash.maphash_package.Hash> h, byte bΔ2) => {
        var (_, err) = h.Write(new byte[]{bΔ2}.slice());
        if (err != default!) {
            Ꮡt.Fatalf("Write single byte: %v"u8, err);
        }
    };
    var writeStringSingleByte = (ж<global::go.hash.maphash_package.Hash> h, byte bΔ3) => {
        var (_, err) = h.WriteString(((@string)new byte[]{bΔ3}.slice()));
        if (err != default!) {
            Ꮡt.Fatalf("WriteString single byte: %v"u8, err);
        }
    };
    foreach (var (i, x) in b) {
        writeByte(hh[2], x);
        writeSingleByte(hh[3], x);
        if (i == 0){
            writeByte(hh[4], x);
        } else {
            writeSingleByte(hh[4], x);
        }
        writeStringSingleByte(hh[5], x);
        if (i == 0){
            writeByte(hh[6], x);
        } else {
            writeStringSingleByte(hh[6], x);
        }
    }
    var sum = hh[0].Sum64();
    foreach (var (i, h) in hh) {
        if (sum != h.Sum64()) {
            Ꮡt.Errorf("hash %d not identical to a single Write"u8, i);
        }
    }
    {
        var sum1 = Bytes(hh[0].Seed(), b); if (sum1 != hh[0].Sum64()) {
            Ꮡt.Errorf("hash using Bytes not identical to a single Write"u8);
        }
    }
    {
        var sum1 = String(hh[0].Seed(), ((@string)b)); if (sum1 != hh[0].Sum64()) {
            Ꮡt.Errorf("hash using String not identical to a single Write"u8);
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string fooˢ = "foo"u8;

public static void TestHashBytesVsString(ж<testing.T> Ꮡt) {
    @string s = fooˢ;
    var b = slice<byte>(s);
    var h1 = @new<global::go.hash.maphash_package.Hash>();
    var h2 = @new<global::go.hash.maphash_package.Hash>();
    h2.SetSeed(h1.Seed());
    var (n1, err1) = h1.WriteString(s);
    if (n1 != len(s) || err1 != default!) {
        Ꮡt.Fatalf("WriteString(s) = %d, %v, want %d, nil"u8, n1, err1, len(s));
    }
    var (n2, err2) = h2.Write(b);
    if (n2 != len(b) || err2 != default!) {
        Ꮡt.Fatalf("Write(b) = %d, %v, want %d, nil"u8, n2, err2, len(b));
    }
    if (h1.Sum64() != h2.Sum64()) {
        Ꮡt.Errorf("hash of string and bytes not identical"u8);
    }
}

public static void TestHashHighBytes(ж<testing.T> Ꮡt) {
    // See issue 34925.
    UntypedInt N = 10;
    var m = new map<uint64, EmptyStruct>{};
    for (nint i = 0; i < N; i++) {
        var h = @new<global::go.hash.maphash_package.Hash>();
        h.WriteString(fooˢ);
        m[(h.Sum64() >> (int)(32))] = new EmptyStruct();
    }
    if (len(m) < N / 2) {
        Ꮡt.Errorf("from %d seeds, wanted at least %d different hashes; got %d"u8, (nint)(N), (nint)(N / 2), len(m));
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string testingˢ = "testing"u8;

public static void TestRepeat(ж<testing.T> Ꮡt) {
    var h1 = @new<global::go.hash.maphash_package.Hash>();
    h1.WriteString(testingˢ);
    var sum1 = h1.Sum64();
    h1.Reset();
    h1.WriteString(testingˢ);
    var sum2 = h1.Sum64();
    if (sum1 != sum2) {
        Ꮡt.Errorf("different sum after resetting: %#x != %#x"u8, sum1, sum2);
    }
    var h2 = @new<global::go.hash.maphash_package.Hash>();
    h2.SetSeed(h1.Seed());
    h2.WriteString(testingˢ);
    var sum3 = h2.Sum64();
    if (sum1 != sum3) {
        Ꮡt.Errorf("different sum on the same seed: %#x != %#x"u8, sum1, sum3);
    }
}

public static void TestSeedFromSum64(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.Value;

    var h1 = @new<global::go.hash.maphash_package.Hash>();
    h1.WriteString(fooˢ);
    var x = h1.Sum64();
    // seed generated here
    var h2 = @new<global::go.hash.maphash_package.Hash>();
    h2.SetSeed(h1.Seed());
    h2.WriteString(fooˢ);
    var y = h2.Sum64();
    if (x != y) {
        Ꮡt.Errorf("hashes don't match: want %x, got %x"u8, x, y);
    }
}

public static void TestSeedFromSeed(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.Value;

    var h1 = @new<global::go.hash.maphash_package.Hash>();
    h1.WriteString(fooˢ);
    _ = h1.Seed();
    // seed generated here
    var x = h1.Sum64();
    var h2 = @new<global::go.hash.maphash_package.Hash>();
    h2.SetSeed(h1.Seed());
    h2.WriteString(fooˢ);
    var y = h2.Sum64();
    if (x != y) {
        Ꮡt.Errorf("hashes don't match: want %x, got %x"u8, x, y);
    }
}

public static void TestSeedFromFlush(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.Value;

    var b = new slice<byte>(65);
    var h1 = @new<global::go.hash.maphash_package.Hash>();
    h1.Write(b);
    // seed generated here
    var x = h1.Sum64();
    var h2 = @new<global::go.hash.maphash_package.Hash>();
    h2.SetSeed(h1.Seed());
    h2.Write(b);
    var y = h2.Sum64();
    if (x != y) {
        Ꮡt.Errorf("hashes don't match: want %x, got %x"u8, x, y);
    }
}

public static void TestSeedFromReset(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.Value;

    var h1 = @new<global::go.hash.maphash_package.Hash>();
    h1.WriteString(fooˢ);
    h1.Reset();
    // seed generated here
    h1.WriteString(fooˢ);
    var x = h1.Sum64();
    var h2 = @new<global::go.hash.maphash_package.Hash>();
    h2.SetSeed(h1.Seed());
    h2.WriteString(fooˢ);
    var y = h2.Sum64();
    if (x != y) {
        Ꮡt.Errorf("hashes don't match: want %x, got %x"u8, x, y);
    }
}

// Make sure a Hash implements the hash.Hash and hash.Hash64 interfaces.
internal static hash.Hash _ᴛ1ʗ = new maphash_test_package.maphash_HashжHash(Ꮡ(new Hash(nil)));

internal static hash.Hash64 _ᴛ2ʗ = new maphash_test_package.maphash_HashжHash64(Ꮡ(new Hash(nil)));

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string writeˢ = "Write"u8;
internal static readonly @string bytesˢ = "Bytes"u8;
internal static readonly @string stringˢ = "String"u8;

internal static void benchmarkSize(ж<testing.B> Ꮡb, nint size) {
    var h = Ꮡ(new Hash(nil));
    var buf = new slice<byte>(size);
    @string s = ((@string)buf);
    var bufʗ1 = buf;
    var hʗ1 = h;
    Ꮡb.Run(writeˢ, (ж<testing.B> bΔ1) => {
        bΔ1.SetBytes((int64)size);
        for (nint i = 0; i < (~bΔ1).N; i++) {
            hʗ1.Reset();
            hʗ1.Write(bufʗ1);
            hʗ1.Sum64();
        }
    });
    var bufʗ3 = buf;
    var hʗ3 = h;
    Ꮡb.Run(bytesˢ, (ж<testing.B> bΔ2) => {
        bΔ2.SetBytes((int64)size);
        ref var seed = ref heap<global::go.hash.maphash_package.ΔSeed>(out var Ꮡseed);
        seed = hʗ3.Seed();
        for (nint i = 0; i < (~bΔ2).N; i++) {
            Bytes(seed, bufʗ3);
        }
    });
    var hʗ5 = h;
    Ꮡb.Run(stringˢ, (ж<testing.B> bΔ3) => {
        bΔ3.SetBytes((int64)size);
        ref var seed = ref heap<global::go.hash.maphash_package.ΔSeed>(out var Ꮡseed);
        seed = hʗ5.Seed();
        for (nint i = 0; i < (~bΔ3).N; i++) {
            String(seed, s);
        }
    });
}

public static void BenchmarkHash(ж<testing.B> Ꮡb) {
    var sizes = new nint[]{4, 8, 16, 32, 64, 256, 320, 1024, 4096, 16384}.slice();
    foreach (var (_, size) in sizes) {
        Ꮡb.Run(fmt.Sprint((@string)"n="u8, size), (ж<testing.B> bΔ1) => {
            benchmarkSize(bΔ1, size);
        });
    }
}

} // end maphash_internal_test_package
