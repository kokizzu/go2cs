// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.io;

using fmt = fmt_package;
using static go.io.fs_package;
using testing = testing_package;
using fs = go.io.fs_package;
using go.io;

partial class fs_test_package {

[GoType] partial struct statOnly {
    public go.io.fs_package.StatFS StatFS;
}

internal static (fs.File, error) Open(this statOnly _, @string name) {
    return (default!, ErrNotExist);
}

public static void TestStat(ж<testing.T> Ꮡt) {
    var check = (@string desc, fs.FileInfo infoΔ1, error errΔ1) => {
        Ꮡt.Helper();
        if (errΔ1 != default! || infoΔ1 == default! || infoΔ1.Mode() != 302) {
            @string infoStr = "<nil>"u8;
            if (infoΔ1 != default!) {
                infoStr = fmt.Sprintf("FileInfo(Mode: %#o)"u8, infoΔ1.Mode());
            }
            Ꮡt.Fatalf("Stat(%s) = %v, %v, want Mode:0456, nil"u8, desc, infoStr, errΔ1);
        }
    };
    // Test that Stat uses the method when present.
    var (info, err) = Stat(new statOnly(testFsys), "hello.txt"u8);
    check("statOnly"u8, info, err);
    // Test that Stat uses Open when the method is not present.
    (info, err) = Stat(new openOnly(new fstest_MapFSᴠFS(testFsys)), "hello.txt"u8);
    check("openOnly"u8, info, err);
}

} // end fs_test_package
