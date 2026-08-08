// Copyright 2015 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux || netbsd || openbsd || solaris
namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// Accept4Func is used to hook the accept4 call.
public static Func<nint, nint, (nint, Δsyscall.Sockaddr, error)> Accept4Func = Δsyscall.Accept4;

} // end poll_package
