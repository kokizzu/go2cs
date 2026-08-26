// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!linux && !freebsd && !darwin) || !cgo
namespace go;

using errors = errors_package;

partial class plugin_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸerrors() {
    builtin.initPackage(typeof(errors_package));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string pluginNotImplementedˢ = "plugin: not implemented"u8;

internal static (Symbol, error) lookup(ref Plugin p, @string symName) {
    return (default!, errors.New(pluginNotImplementedˢ));
}

internal static (ж<Plugin>, error) open(@string name) {
    return (default!, errors.New(pluginNotImplementedˢ));
}

} // end plugin_package
