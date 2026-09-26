// stubs2_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// The linux runtime's raw file-descriptor syscalls: read, write1, open and closefd (stubs2.go), plus
// pipe2 and mincore (os_linux.go), whose bodies are assembly in sys_linux_amd64.s.
//
// This file holds the guard's probes only; the bodies follow in the next commit.
//
// Hand-owned (no stubs2_impl.go exists, so a reconvert never regenerates this file).

using System;
using go.golib;
using @unsafe = go.unsafe_package;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    // ---- the guard's view (GolibTests RuntimeFdSyscallTests) ----

    private static string failureOf(Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

    /// <summary>
    /// TestBadOpen's sequence: open a path that does not exist, then read, write and close fd -1.
    /// Go answers -1, -EBADF, -EBADF and -1.
    /// </summary>
    public static (int open, int read, int write, int close, string? failure) GoBadOpenProbe()
    {
        try
        {
            var path = slice<byte>("/notreallyafile\0"u8);
            int fd = (int)open(Ꮡ(path, 0), 0, 0);
            ref var buf = ref heap(new array<byte>(32), out var Ꮡbuf);
            @unsafe.Pointer p = @unsafe.Pointer.FromPinnedBox(Ꮡbuf.at<byte>(0));
            int r = (int)read(-1, p, 32);
            int w = (int)write1(~(uintptr)0, p, 32);
            int c = (int)closefd(-1);
            return (fd, r, w, c, null);
        }
        catch (Exception ex)
        {
            return (0, 0, 0, 0, failureOf(ex));
        }
    }

    /// <summary>
    /// nonblockingPipe's pipe2, then one byte written through write1 and read back through read, then
    /// both ends closed. Returns the errno word, what was read, and the two close results.
    /// </summary>
    public static (int errno, int wrote, int readCount, byte readByte, int closeR, int closeW, string? failure) GoPipe2RoundTripProbe()
    {
        try
        {
            var (rfd, wfd, errno) = pipe2((int32)(_O_NONBLOCK | _O_CLOEXEC));
            if (errno != 0)
                return (errno, 0, 0, 0, 0, 0, null);

            ref var bout = ref heap((byte)42, out var Ꮡout);
            ref var bin = ref heap(new byte(), out var Ꮡin);
            int wrote = (int)write1((uintptr)wfd, @unsafe.Pointer.FromPinnedBox(Ꮡout), 1);
            int got = (int)read(rfd, @unsafe.Pointer.FromPinnedBox(Ꮡin), 1);
            int cr = (int)closefd(rfd);
            int cw = (int)closefd(wfd);
            return (0, wrote, got, bin, cr, cw, null);
        }
        catch (Exception ex)
        {
            return (0, 0, 0, 0, 0, 0, failureOf(ex));
        }
    }

    /// <summary>TestMincoreErrorSign: mincore over a misaligned address. Go answers -EINVAL (-22).</summary>
    public static (int result, string? failure) GoMincoreErrorSignProbe()
    {
        try
        {
            ref var dst = ref heap(new byte(), out var Ꮡdst);
            @unsafe.Pointer misaligned = @unsafe.Add(@unsafe.Pointer.FromPinnedBox(@new<int32>()), 1);
            return ((int)mincore(misaligned, 1, Ꮡdst), null);
        }
        catch (Exception ex)
        {
            return (0, failureOf(ex));
        }
    }
}
