// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix || windows
[assembly: go.GoPositionMap("net/tcpsockopt_posix.go", "tcpsockopt_posix.cs", "AA4cgoKC")]

namespace go;

using Δruntime = runtime_package;
using syscall = syscall_package;
using @internal;

partial class net_package {

internal static error setNoDelay(ж<netFD> Ꮡfd, bool noDelay) {
    var err = Ꮡfd.of(netFD.Ꮡpfd).SetsockoptInt(syscall.IPPROTO_TCP, syscall.TCP_NODELAY, boolint(noDelay));
    Δruntime.KeepAlive(Ꮡfd.OrTypedNil());
    return wrapSyscallError(setsockoptˢ, err);
}

} // end net_package
