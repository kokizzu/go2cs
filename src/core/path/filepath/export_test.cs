// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("path/filepath/export_test.go", "export_test.cs", "")]

namespace go.path;

using fs = io.fs_package;
using static go.path.filepath_package;

partial class filepath_internal_test_package {

public static ж<Func<@string, (fs.FileInfo, error)>> LstatP;
internal static void initᴛLstatP() { LstatP = Ꮡlstat; }

} // end filepath_internal_test_package
