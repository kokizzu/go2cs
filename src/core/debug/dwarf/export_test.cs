// Copyright 2017 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("debug/dwarf/export_test.go", "export_test.cs", "")]

namespace go.debug;

using static go.debug.dwarf_package;

partial class dwarf_internal_test_package {

public static Func<@string, @string, @string> PathJoin = pathJoin;

} // end dwarf_internal_test_package
