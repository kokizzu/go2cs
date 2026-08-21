// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("internal/cpu/export_test.go", "export_test.cs", "")]

namespace go.@internal;

using static go.@internal.cpu_package;

partial class cpu_internal_test_package {

internal static slice<global::go.@internal.cpu_package.option> Options;
internal static void initᴛOptions() { Options = options; }

} // end cpu_internal_test_package
