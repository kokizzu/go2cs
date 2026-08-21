// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("path/filepath/export_windows_test.go", "export_windows_test.cs", "")]

namespace go.path;

using static go.path.filepath_package;

partial class filepath_internal_test_package {

public static Func<@string, Func<@string, (@string, error)>, (@string, error)> ToNorm = toNorm;
public static Func<@string, (@string, error)> NormBase = normBase;

} // end filepath_internal_test_package
