// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build 386 || amd64
[assembly: go.GoPositionMap("internal/cpu/export_x86_test.go", "export_x86_test.cs", "")]

namespace go.@internal;

using static go.@internal.cpu_package;

partial class cpu_internal_test_package {

public static Func<int32> GetGOAMD64level = getGOAMD64level;

} // end cpu_internal_test_package
