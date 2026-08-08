// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || dragonfly || freebsd || (js && wasm) || wasip1 || linux || netbsd || openbsd || solaris
namespace go;

using byteorder = @internal.byteorder_package;
using goarch = @internal.goarch_package;
using Δio = io_package;
using Δruntime = runtime_package;
using Δsync = sync_package;
using syscall = syscall_package;
using @unsafe = unsafe_package;
using @internal;

partial class os_package {

// Auxiliary information if the File describes a directory
[GoType] partial struct dirInfo {
    internal Δsync.Mutex mu;
    internal ж<slice<byte>> buf; // buffer for directory I/O
    internal nint nbuf;    // length of buf; return value from Getdirentries
    internal nint bufp;    // location of next record in buf.
}

internal static UntypedInt blockSize => 8192;

// The buffer must be at least a block long.
internal static ж<Δsync.Pool> ᏑdirBufPool = new(new Δsync.Pool(
    New: () => {
        var buf = new slice<byte>(blockSize);
        return Ꮡ(buf);
    }
));
internal static ref Δsync.Pool dirBufPool => ref ᏑdirBufPool.Value;

[GoRecv] internal static void close(this ref dirInfo d) {
    if (d.buf != nil) {
        ᏑdirBufPool.Put(d.buf.OrTypedNil());
        d.buf = default!;
    }
}

// go2cs generated this placeholder — func readdir is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// If this file has no dirInfo, create one.
// Change the meaning of n for the implementation below.
//
// The n above was for the public interface of "if n <= 0,
// Readdir returns all the FileInfo from the directory in a
// single slice".
//
// But below, we use only negative to mean looping until the
// end and positive to mean bounded, with positive
// terminating at 0.
// Refill the buffer if necessary
// Optimization: we can return the buffer to the pool, there is nothing else to read.
// EOF
// Drain the buffer
// When building to wasip1, the host runtime might be running on Windows
// or might expose a remote file system which does not have the concept
// of inodes. Therefore, we cannot make the assumption that it is safe
// to skip entries with zero inodes.
// Check for useless names before allocating a string.
// see 'n == 0' comment above
// File disappeared between readdir and stat.
// Treat as if it didn't exist.
// File disappeared between readdir + stat.
// Treat as if it didn't exist.

// readInt returns the size-bytes unsigned integer in native byte order at offset off.
internal static (uint64 u, bool ok) readInt(slice<byte> b, uintptr off, uintptr size) {
    if (len(b) < (nint)(off + size)) {
        return (0, false);
    }
    if (goarch.BigEndian) {
        return (readIntBE(b[(int)(off)..], size), true);
    }
    return (readIntLE(b[(int)(off)..], size), true);
}

internal static uint64 readIntBE(slice<byte> b, uintptr size) {
    var exprᴛ1 = size;
    if (exprᴛ1 == 1) {
        return (uint64)b[0];
    }
    if (exprᴛ1 == 2) {
        return (uint64)byteorder.BeUint16(b);
    }
    if (exprᴛ1 == 4) {
        return (uint64)byteorder.BeUint32(b);
    }
    if (exprᴛ1 == 8) {
        return (uint64)byteorder.BeUint64(b);
    }
    { /* default: */
        throw panic("syscall: readInt with unsupported size");
    }

}

internal static uint64 readIntLE(slice<byte> b, uintptr size) {
    var exprᴛ1 = size;
    if (exprᴛ1 == 1) {
        return (uint64)b[0];
    }
    if (exprᴛ1 == 2) {
        return (uint64)byteorder.LeUint16(b);
    }
    if (exprᴛ1 == 4) {
        return (uint64)byteorder.LeUint32(b);
    }
    if (exprᴛ1 == 8) {
        return (uint64)byteorder.LeUint64(b);
    }
    { /* default: */
        throw panic("syscall: readInt with unsupported size");
    }

}

} // end os_package
