// Copyright 2017 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix || (js && wasm) || wasip1
namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// CloseFunc is used to hook the close call.
public static Func<nint, error> CloseFunc = Δsyscall.Close;

// AcceptFunc is used to hook the accept call.
public static Func<nint, (nint, Δsyscall.Sockaddr, error)> AcceptFunc = Δsyscall.Accept;

} // end poll_package
