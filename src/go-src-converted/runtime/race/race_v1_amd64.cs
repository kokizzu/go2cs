// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (linux && !amd64.v3) || darwin || freebsd || netbsd || openbsd || windows
namespace go.runtime;

// blank import: go.runtime.race.@internal.amd64v1_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)

partial class race_package {

// Go runs a blank-imported package's `init` before this package's own; .NET would never
// load an assembly nothing references, so the side effects the import exists for are forced.
[GoInit] internal static void initᴛᴛblankImportꓸruntimeꓸraceꓸinternalꓸamd64v1() {
    builtin.initPackage(typeof(go.runtime.race.@internal.amd64v1_package));
}

} // end race_package
