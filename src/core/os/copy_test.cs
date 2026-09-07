// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using bytes = bytes_package;
using errors = errors_package;
using Δio = io_package;
using rand = math.rand.rand_package;
using Δnet = net_package;
using Δos = os_package;
using Δruntime = runtime_package;
using Δsync = sync_package;
using Δtesting = testing_package;
using nettest = vendor.golang.org.x.net.nettest_package;
using math.rand;
using static go.os_internal_test_package;
using time = time_package;
using vendor.golang.org.x.net;

partial class os_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string tcpˢ = "tcp"u8;

// Exercise sendfile/splice fast paths with a moderately large file.
//
// https://go.dev/issue/70000
public static void TestLargeCopyViaNetwork(ж<Δtesting.T> Ꮡt) {
    GoFrame ᒐ = default;
    try {
        UntypedInt size = /* 10 * 1024 * 1024 */ 10485760;
        @string dir = Ꮡt.TempDir();
        var (src, err) = Δos.Create(dir + "/src"u8);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        var srcʗ1 = src;
        defer(() => srcʗ1.Close(), ref ᒐ);
        {
            var (_, errΔ1) = Δio.CopyN(new Δos.FileжWriter(src), new os_test_package.randReaderжReader(newRandReader()), size); if (errΔ1 != default!) {
                Ꮡt.Fatal(errΔ1);
            }
        }
        {
            var (_, errΔ2) = src.Seek(0, 0); if (errΔ2 != default!) {
                Ꮡt.Fatal(errΔ2);
            }
        }
        (var dst, err) = Δos.Create(dir + "/dst"u8);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        var dstʗ1 = dst;
        defer(() => dstʗ1.Close(), ref ᒐ);
        var (client, server) = createSocketPair(Ꮡt, tcpˢ);
        ref var wg = ref heap(new Δsync.WaitGroup(), out var Ꮡwg);
        Ꮡwg.Add(2);
        var dstʗ2 = dst;
        var serverʗ1 = server;
        goǃ(() => {
            GoFrame ᒐ = default;
            try {
                defer(Ꮡwg.Done, ref ᒐ);
                {
                    var (n, errΔ3) = Δio.Copy(new Δos.FileжWriter(dstʗ2), new os_test_package.net_ConnᴠReader(serverʗ1)); if (n != size || errΔ3 != default!) {
                        Ꮡt.Errorf("copy to destination = %v, %v; want %v, nil"u8, n, errΔ3, (nint)(size));
                    }
                }
            }
            catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
            finally { ᒐ.Run(); }
        });
        var clientʗ1 = client;
        var srcʗ2 = src;
        goǃ(() => {
            GoFrame ᒐ = default;
            try {
                defer(Ꮡwg.Done, ref ᒐ);
                var clientʗ2 = clientʗ1;
                defer(() => clientʗ2.Close(), ref ᒐ);
                {
                    var (n, errΔ4) = Δio.Copy(new os_test_package.net_ConnᴠWriter(clientʗ1), new os_test_package.os_FileжReader(srcʗ2)); if (n != size || errΔ4 != default!) {
                        Ꮡt.Errorf("copy from source = %v, %v; want %v, nil"u8, n, errΔ4, (nint)(size));
                    }
                }
            }
            catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
            finally { ᒐ.Run(); }
        });
        Ꮡwg.Wait();
        {
            var (_, errΔ5) = dst.Seek(0, 0); if (errΔ5 != default!) {
                Ꮡt.Fatal(errΔ5);
            }
        }
        {
            var errΔ6 = compareReaders(new os_test_package.os_FileжReader(dst), Δio.LimitReader(new os_test_package.randReaderжReader(newRandReader()), size)); if (errΔ6 != default!) {
                Ꮡt.Fatal(errΔ6);
            }
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string contentsMismatchˢ = "contents mismatch"u8;

internal static error compareReaders(Δio.Reader a, Δio.Reader b) {
    var bufa = new slice<byte>(4096);
    var bufb = new slice<byte>(4096);
    while (ᐧ) {
        var (na, erra) = Δio.ReadFull(a, bufa);
        if (erra != default! && !AreEqual(erra, Δio.EOF)) {
            return erra;
        }
        var (nb, errb) = Δio.ReadFull(b, bufb);
        if (errb != default! && !AreEqual(errb, Δio.EOF)) {
            return errb;
        }
        if (!bytes.Equal(bufa[..(int)(na)], bufb[..(int)(nb)])) {
            return errors.New(contentsMismatchˢ);
        }
        if (AreEqual(erra, Δio.EOF) && AreEqual(errb, Δio.EOF)) {
            break;
        }
    }
    return default!;
}

[GoType] partial struct randReader {
    internal ж<rand.Rand> rand;
}

internal static ж<randReader> newRandReader() {
    return Ꮡ(new randReader(rand.New(new rand.PCGжSource(rand.NewPCG(0, 0)))));
}

[GoRecv] internal static (nint, error) Read(this ref randReader r, slice<byte> p) {
    uint64 v = default!;
    nint n = default!;
    foreach (var (i, _) in p) {
        if (n == 0) {
            v = r.rand.Uint64();
            n = 8;
        }
        p[i] = (byte)((uint64)(v & 0xff));
        v >>= (int)(8);
        n--;
    }
    return (len(p), default!);
}

internal static (Δnet.Conn client, Δnet.Conn server) createSocketPair(ж<Δtesting.T> Ꮡt, @string proto) {
    Δnet.Conn client = default!;
    Δnet.Conn server = default!;

    Ꮡt.Helper();
    if (!nettest.TestableNetwork(proto)) {
        Ꮡt.Skipf("%s does not support %q"u8, Δruntime.GOOS, proto);
    }
    var (ln, err) = nettest.NewLocalListener(proto);
    if (err != default!) {
        Ꮡt.Fatalf("NewLocalListener error: %v"u8, err);
    }
    var lnʗ1 = ln;
    Ꮡt.Cleanup(() => {
        if (lnʗ1 != default!) {
            lnʗ1.Close();
        }
        if (client != default!) {
            client.Close();
        }
        if (server != default!) {
            server.Close();
        }
    });
    var ch = new channel<EmptyStruct>(0);
    var chʗ1 = ch;
    var lnʗ2 = ln;
    goǃ(() => {
        error errΔ1 = default!;
        (server, errΔ1) = lnʗ2.Accept();
        if (errΔ1 != default!) {
            Ꮡt.Errorf("Accept new connection error: %v"u8, errΔ1);
        }
        chʗ1.ᐸꟷ(new EmptyStruct());
    });
    (client, err) = Δnet.Dial(proto, ln.Addr().String());
    ᐸꟷ(ch);
    if (err != default!) {
        Ꮡt.Fatalf("Dial new connection error: %v"u8, err);
    }
    return (client, server);
}

} // end os_test_package
