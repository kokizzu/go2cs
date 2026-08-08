// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// This file provides the Accept function used on all systems
// other than arm. See syscall_linux_accept.go for why.
//go:build linux && !arm
namespace go;

partial class syscall_package {

public static (nint nfd, Sockaddr sa, error err) Accept(nint fd) {
    nint nfd = default!;
    Sockaddr sa = default!;
    error err = default!;

    ref var rsa = ref heap(new RawSockaddrAny(), out var Ꮡrsa);
    ref var len = ref heap(new _Socklen(), out var Ꮡlen);
    len = SizeofSockaddrAny;
    (nfd, err) = accept4(fd, Ꮡrsa, Ꮡlen, 0);
    if (err != default!) {
        return (nfd, sa, err);
    }
    (sa, err) = anyToSockaddr(Ꮡrsa);
    if (err != default!) {
        Close(nfd);
        nfd = 0;
    }
    return (nfd, sa, err);
}

} // end syscall_package
