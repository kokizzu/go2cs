// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix
namespace go.@internal.syscall;

using syscall = syscall_package;

partial class unix_package {

public static UntypedInt R_OK => 0x4;
public static UntypedInt W_OK => 0x2;
public static UntypedInt X_OK => 0x1;
public static syscall.Errno NoFollowErrno => /* noFollowErrno */ 40;

} // end unix_package
