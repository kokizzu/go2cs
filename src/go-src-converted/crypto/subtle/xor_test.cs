// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using bytes = bytes_package;
using rand = go.crypto.rand_package;
using static go.crypto.subtle_package;
using fmt = fmt_package;
using io = io_package;
using testing = testing_package;
using go.crypto;

partial class subtle_test_package {

public static void TestXORBytes(ж<testing.T> Ꮡt) {
    for (nint n = 1; n <= 1024; n++) {
        if (n > 16 && testing.Short()) {
            n += (n >> (int)(3));
        }
        for (nint alignP = 0; alignP < 8; alignP++) {
            for (nint alignQ = 0; alignQ < 8; alignQ++) {
                for (nint alignD = 0; alignD < 8; alignD++) {
                    var p = new slice<byte>(alignP + n, alignP + n + 10)[(int)(alignP)..];
                    var q = new slice<byte>(alignQ + n, alignQ + n + 10)[(int)(alignQ)..];
                    if ((nint)(n & 1) != 0){
                        p = p[..(int)(n)];
                    } else {
                        q = q[..(int)(n)];
                    }
                    {
                        var (_, err) = io.ReadFull(rand.Reader, p); if (err != default!) {
                            Ꮡt.Fatal(err);
                        }
                    }
                    {
                        var (_, err) = io.ReadFull(rand.Reader, q); if (err != default!) {
                            Ꮡt.Fatal(err);
                        }
                    }
                    var d = new slice<byte>(alignD + n, alignD + n + 10);
                    foreach (var (i, _) in d) {
                        d[i] = 0xdd;
                    }
                    var want = new slice<byte>(len(d), cap(d));
                    copy(want[..(int)(cap(want))], d[..(int)(cap(d))]);
                    for (nint i = 0; i < n; i++) {
                        want[alignD + i] = (byte)(p[i] ^ q[i]);
                    }
                    {
                        XORBytes(d[(int)(alignD)..], p, q); if (!bytes.Equal(d, want)) {
                            Ꮡt.Fatalf("n=%d alignP=%d alignQ=%d alignD=%d:\n\tp = %x\n\tq = %x\n\td = %x\n\twant %x\n"u8, n, alignP, alignQ, alignD, p, q, d, want);
                        }
                    }
                }
            }
        }
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string subtleXORBytesDstTooˢ = "subtle.XORBytes: dst too short"u8;

public static void TestXorBytesPanic(ж<testing.T> Ꮡt) {
    mustPanic(Ꮡt, subtleXORBytesDstTooˢ, () => {
        XORBytes(default!, new slice<byte>(1), new slice<byte>(1));
    });
    mustPanic(Ꮡt, subtleXORBytesDstTooˢ, () => {
        XORBytes(new slice<byte>(1), new slice<byte>(2), new slice<byte>(3));
    });
}

public static void BenchmarkXORBytes(ж<testing.B> Ꮡb) {
    var dst = new slice<byte>((1 << (int)(15)));
    var data0 = new slice<byte>((1 << (int)(15)));
    var data1 = new slice<byte>((1 << (int)(15)));
    var sizes = new int64[]{((int64)1 << (int)(3)), ((int64)1 << (int)(7)), ((int64)1 << (int)(11)), ((int64)1 << (int)(15))}.slice();
    foreach (var (_, size) in sizes) {
        var data0ʗ1 = data0;
        var data1ʗ1 = data1;
        var dstʗ1 = dst;
        Ꮡb.Run(fmt.Sprintf("%dBytes"u8, size), (ж<testing.B> bΔ1) => {
            var s0 = data0ʗ1[..(int)(size)];
            var s1 = data1ʗ1[..(int)(size)];
            bΔ1.SetBytes((int64)size);
            for (nint i = 0; i < (~bΔ1).N; i++) {
                XORBytes(dstʗ1, s0, s1);
            }
        });
    }
}

internal static void mustPanic(ж<testing.T> Ꮡt, @string expected, Action f) => func((defer, recover) => {
    Ꮡt.Helper();
    defer(() => {
        var switchᴛ1 = recover();
        switch (switchᴛ1.type()) {
        case null: {
            Ꮡt.Errorf("expected panic(%q), but did not panic"u8, expected);
            break;
        }
        case @string msg: {
            if (msg != expected) {
                Ꮡt.Errorf("expected panic(%q), but got panic(%q)"u8, expected, msg);
            }
            break;
        }
        default: {
            var msg = switchᴛ1;
            Ꮡt.Errorf("expected panic(%q), but got panic(%T%v)"u8, expected, msg, msg);
            break;
        }}
    });
    f();
});

} // end subtle_test_package
