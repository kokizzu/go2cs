// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !netcgo
[assembly: go.GoPositionMap("net/netcgo_off.go", "netcgo_off.cs", "")]

namespace go;

partial class net_package {

internal const bool netCgoBuildTag = false;

} // end net_package
