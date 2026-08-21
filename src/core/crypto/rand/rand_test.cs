// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bytes = bytes_package;
using flate = compress.flate_package;
using io = io_package;
using testing = testing_package;
using compress;
using static go.crypto.rand_package;

partial class rand_internal_test_package {

public static void TestRead(ж<testing.T> Ꮡt) {
    nint n = 4000000;
    if (testing.Short()) {
        n = 100000;
    }
    var b = new slice<byte>(n);
    (n, var err) = io.ReadFull(Reader, b);
    if (n != len(b) || err != default!) {
        Ꮡt.Fatalf("ReadFull(buf) = %d, %s"u8, n, err);
    }
    ref var z = ref heap(new bytes.Buffer(), out var Ꮡz);
    var (f, _) = flate.NewWriter(new rand_test_package.bytes_BufferжWriter(Ꮡz), 5);
    f.Write(b);
    f.Close();
    if (z.Len() < len(b) * 99 / 100) {
        Ꮡt.Fatalf("Compressed %d -> %d"u8, len(b), z.Len());
    }
}

public static void TestReadEmpty(ж<testing.T> Ꮡt) {
    var (n, err) = Reader.Read(new slice<byte>(0));
    if (n != 0 || err != default!) {
        Ꮡt.Fatalf("Read(make([]byte, 0)) = %d, %v"u8, n, err);
    }
    (n, err) = Reader.Read(default!);
    if (n != 0 || err != default!) {
        Ꮡt.Fatalf("Read(nil) = %d, %v"u8, n, err);
    }
}

public static void BenchmarkRead(ж<testing.B> Ꮡb) {
    Ꮡb.Run("32"u8, (ж<testing.B> bΔ1) => {
        benchmarkRead(bΔ1, 32);
    });
    Ꮡb.Run("4K"u8, (ж<testing.B> bΔ2) => {
        benchmarkRead(bΔ2, (4 << (int)(10)));
    });
}

internal static void benchmarkRead(ж<testing.B> Ꮡb, nint size) {
    ref var b = ref Ꮡb.DerefOrNull();

    b.SetBytes((int64)size);
    var buf = new slice<byte>(size);
    for (nint i = 0; i < b.N; i++) {
        {
            var (_, err) = Read(buf); if (err != default!) {
                Ꮡb.Fatal(err);
            }
        }
    }
}

} // end rand_internal_test_package
