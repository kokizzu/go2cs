// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build go1.8
// +build go1.8

// package logopt -- go2cs converted at 2022 March 13 05:59:00 UTC
// import "cmd/compile/internal/logopt" ==> using logopt = go.cmd.compile.@internal.logopt_package
// Original source: C:\Program Files\Go\src\cmd\compile\internal\logopt\escape.go
namespace go.cmd.compile.@internal;

using url = net.url_package;

public static partial class logopt_package {

private static @string pathEscape(@string s) {
    return url.PathEscape(s);
}

} // end logopt_package
