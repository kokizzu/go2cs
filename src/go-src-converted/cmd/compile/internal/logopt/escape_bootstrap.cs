// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build !go1.8
// +build !go1.8

// package logopt -- go2cs converted at 2022 March 13 05:59:02 UTC
// import "cmd/compile/internal/logopt" ==> using logopt = go.cmd.compile.@internal.logopt_package
// Original source: C:\Program Files\Go\src\cmd\compile\internal\logopt\escape_bootstrap.go
namespace go.cmd.compile.@internal;

public static partial class logopt_package {

// For bootstrapping with an early version of Go
private static @string pathEscape(@string s) => func((_, panic, _) => {
    panic("This should never be called; the compiler is not fully bootstrapped if it is.");
});

} // end logopt_package
