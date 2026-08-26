// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix
// Unix cryptographically secure pseudorandom number
// generator.
namespace go.crypto;

using boring = go.crypto.@internal.boring_package;
using errors = errors_package;
using io = io_package;
using os = os_package;
using sync = sync_package;
using atomic = go.sync.atomic_package;
using syscall = syscall_package;
using time = time_package;
using go.crypto.@internal;
using go.sync;

partial class rand_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸcryptoꓸinternalꓸboring() {
    builtin.initPackage(typeof(go.crypto.@internal.boring_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸerrors() {
    builtin.initPackage(typeof(errors_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸos() {
    builtin.initPackage(typeof(os_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsync() {
    builtin.initPackage(typeof(sync_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyncꓸatomic() {
    builtin.initPackage(typeof(go.sync.atomic_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸtime() {
    builtin.initPackage(typeof(time_package));
}

internal static readonly @string urandomDevice = "/dev/urandom"u8;

[GoInit] internal static void initΔ1() {
    if (boring.Enabled) {
        Reader = new boring_randReaderᴠReader(boring.RandReader);
        return;
    }
    Reader = new readerжReader(Ꮡ(new reader(nil)));
}

// A reader satisfies reads by reading from urandomDevice
[GoType] partial struct reader {
    internal io.Reader f;
    internal sync.Mutex mu;
    internal atomic.Uint32 used; // Atomic: 0 - never used, 1 - used, but f == nil, 2 - used, and f != nil
}

// altGetRandom if non-nil specifies an OS-specific function to get
// urandom-style randomness.
internal static Func<slice<byte>, error> altGetRandom;

internal static void warnBlocked() {
    println((@string)"crypto/rand: blocked for 60 seconds waiting to read random data from the kernel"u8);
}

internal static (nint n, error err) Read(this ж<reader> Ꮡr, slice<byte> b) {
    nint n = default!;
    error err = default!;
    GoFrame ᒐ = default;
    try {
        ref var r = ref Ꮡr.DerefOrNull();

        boring.Unreachable();
        if (Ꮡr.of(reader.Ꮡused).CompareAndSwap(0, 1)) {
            // First use of randomness. Start timer to warn about
            // being blocked on entropy not being available.
            var t = time.AfterFunc(time.ΔMinute, warnBlocked);
            var tʗ1 = t;
            defer(() => tʗ1.Stop(), ref ᒐ);
        }
        if (altGetRandom != default! && altGetRandom(b) == default!) {
            (n, err) = (len(b), default!); goto ᒐdone;
        }
        if (Ꮡr.of(reader.Ꮡused).Load() != 2) {
            Ꮡr.of(reader.Ꮡmu).Lock();
            if (Ꮡr.of(reader.Ꮡused).Load() != 2) {
                var (f, errΔ1) = os.Open(urandomDevice);
                if (errΔ1 != default!) {
                    Ꮡr.of(reader.Ꮡmu).Unlock();
                    (n, err) = (0, errΔ1); goto ᒐdone;
                }
                r.f = new hideAgainReader(new os_FileжReader(f));
                Ꮡr.of(reader.Ꮡused).Store(2);
            }
            Ꮡr.of(reader.Ꮡmu).Unlock();
        }
        (n, err) = io.ReadFull(r.f, b);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    ᒐdone: return (n, err);
}

// hideAgainReader masks EAGAIN reads from /dev/urandom.
// See golang.org/issue/9205
[GoType] partial struct hideAgainReader {
    internal io.Reader r;
}

internal static (nint n, error err) Read(this hideAgainReader hr, slice<byte> p) {
    nint n = default!;
    error err = default!;

    (n, err) = hr.r.Read(p);
    if (errors.Is(err, syscall.EAGAIN)) {
        err = default!;
    }
    return (n, err);
}

} // end rand_package
