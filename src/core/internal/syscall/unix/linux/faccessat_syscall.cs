// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || linux
namespace go.@internal.syscall;

using syscall = syscall_package;

partial class unix_package {

internal static Func<nint, @string, uint32, nint, error> faccessat = syscall.Faccessat;

} // end unix_package
