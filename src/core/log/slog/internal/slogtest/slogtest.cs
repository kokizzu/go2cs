// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package slogtest contains support functions for testing slog.
namespace go.log.slog.@internal;

using slog = go.log.slog_package;
using go.log;

partial class slogtest_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸlogꓸslog() {
    builtin.initPackage(typeof(go.log.slog_package));
}

// RemoveTime removes the top-level time attribute.
// It is intended to be used as a ReplaceAttr function,
// to make example output deterministic.
public static slog.Attr RemoveTime(slice<@string> groups, slog.Attr a) {
    if (a.Key == slog.TimeKey && len(groups) == 0) {
        return new slog.Attr(nil);
    }
    return a;
}

} // end slogtest_package
