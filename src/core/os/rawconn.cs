// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !plan9
namespace go;

using Δruntime = runtime_package;
using @internal;

partial class os_package {

// rawConn implements syscall.RawConn.
[GoType] partial struct rawConn {
    internal ж<File> @file;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string syscallConnControlˢ = "SyscallConn.Control"u8;

[GoRecv] internal static error Control(this ref rawConn c, Action<uintptr> f) {
    {
        var errΔ1 = c.@file.checkValid(syscallConnControlˢ); if (errΔ1 != default!) {
            return errΔ1;
        }
    }
    var err = c.@file.of(File.Ꮡpfd).RawControl(f);
    Δruntime.KeepAlive(c.@file);
    return err;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string syscallConnReadˢ = "SyscallConn.Read"u8;

[GoRecv] internal static error Read(this ref rawConn c, Func<uintptr, bool> f) {
    {
        var errΔ1 = c.@file.checkValid(syscallConnReadˢ); if (errΔ1 != default!) {
            return errΔ1;
        }
    }
    var err = c.@file.of(File.Ꮡpfd).RawRead(f);
    Δruntime.KeepAlive(c.@file);
    return err;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string syscallConnWriteˢ = "SyscallConn.Write"u8;

[GoRecv] internal static error Write(this ref rawConn c, Func<uintptr, bool> f) {
    {
        var errΔ1 = c.@file.checkValid(syscallConnWriteˢ); if (errΔ1 != default!) {
            return errΔ1;
        }
    }
    var err = c.@file.of(File.Ꮡpfd).RawWrite(f);
    Δruntime.KeepAlive(c.@file);
    return err;
}

internal static (ж<rawConn>, error) newRawConn(ж<File> Ꮡfile) {
    ref var @file = ref Ꮡfile.Value;

    return (Ꮡ(new rawConn(@file: Ꮡfile)), default!);
}

} // end os_package
