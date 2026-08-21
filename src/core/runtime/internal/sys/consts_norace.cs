// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !race
[assembly: go.GoPositionMap("runtime/internal/sys/consts_norace.go", "consts_norace.cs", "")]

namespace go.runtime.@internal;

partial class sys_package {

internal static UntypedInt isRace => 0;

} // end sys_package
