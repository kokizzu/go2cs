// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (darwin && !ios) || dragonfly || freebsd || solaris
namespace go;

using poll = @internal.poll_package;
using Δio = io_package;
using fs = go.io.fs_package;
using syscall = syscall_package;
using @internal;
using go.io;

partial class net_package {

internal const bool supportsSendfile = true;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string sendfileˢ = "sendfile"u8;

[GoType("dyn")] partial interface sendFile_type :
    fs.File,
    Δio.Seeker,
    syscall.Conn
{
}

// sendFile copies the contents of r to c using the sendfile
// system call to minimize copies.
//
// if handled == true, sendFile returns the number (potentially zero) of bytes
// copied and any non-EOF error.
//
// if handled == false, sendFile performed no work.
internal static (int64 written, error err, bool handled) sendFile(ж<netFD> Ꮡc, Δio.Reader r) {
    int64 written = default!;
    error err = default!;
    bool handled = default!;

    // Darwin, FreeBSD, DragonFly and Solaris use 0 as the "until EOF" value.
    // If you pass in more bytes than the file contains, it will
    // loop back to the beginning ad nauseam until it's sent
    // exactly the number of bytes told to. As such, we need to
    // know exactly how many bytes to send.
    int64 remain = 0;
    var (lr, ok) = r._<ж<Δio.LimitedReader>>(ᐧ);
    if (ok) {
        (remain, r) = (lr.Value.N, lr.Value.R);
        if (remain <= 0) {
            return (0, default!, true);
        }
    }
    // r might be an *os.File or an os.fileWithoutWriteTo.
    // Type assert to an interface rather than *os.File directly to handle the latter case.
    (var f, ok) = r._<sendFile_type>(ᐧ);
    if (!ok) {
        return (0, default!, false);
    }
    if (remain == 0) {
        var (fi, errΔ1) = f.Stat();
        if (errΔ1 != default!) {
            return (0, errΔ1, false);
        }
        remain = fi.Size();
    }
    // The other quirk with Darwin/FreeBSD/DragonFly/Solaris's sendfile
    // implementation is that it doesn't use the current position
    // of the file -- if you pass it offset 0, it starts from
    // offset 0. There's no way to tell it "start from current
    // position", so we have to manage that explicitly.
    (var pos, err) = f.Seek(0, Δio.SeekCurrent);
    if (err != default!) {
        return (0, err, false);
    }
    (var sc, err) = f.SyscallConn();
    if (err != default!) {
        return (0, default!, false);
    }
    ref var werr = ref heap<error>(out var Ꮡwerr);
    err = sc.Read((uintptr fd) => {
        (written, Ꮡwerr.ValueSlot, handled) = poll.SendFile(Ꮡc.of(netFD.Ꮡpfd), (nint)fd, pos, remain);
        return true;
    });
    if (err == default!) {
        err = werr;
    }
    if (lr != nil) {
        lr.Value.N = remain - written;
    }
    var (_, err1) = f.Seek(written, Δio.SeekCurrent);
    if (err1 != default! && err == default!) {
        return (written, err1, handled);
    }
    return (written, wrapSyscallError(sendfileˢ, err), handled);
}

} // end net_package
