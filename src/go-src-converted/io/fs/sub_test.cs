// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.io;

using errors = errors_package;
using static go.io.fs_package;
using testing = testing_package;
using fs = go.io.fs_package;
using go.io;

partial class fs_test_package {

[GoType] partial struct subOnly {
    public go.io.fs_package.SubFS SubFS;
}

internal static (fs.File, error) Open(this subOnly _, @string name) {
    return (default!, ErrNotExist);
}

public static void TestSub(ж<testing.T> Ꮡt) {
    var check = (@string desc, fs.FS subΔ1, error errΔ1) => {
        Ꮡt.Helper();
        if (errΔ1 != default!) {
            Ꮡt.Errorf("Sub(sub): %v"u8, errΔ1);
            return;
        }
        (var data, errΔ1) = ReadFile(subΔ1, "goodbye.txt"u8);
        if (((sstring)data) != "goodbye, world"u8 || errΔ1 != default!) {
            Ꮡt.Errorf(@"ReadFile(%s, ""goodbye.txt"" = %q, %v, want %q, nil"u8, desc, ((@string)data), errΔ1, (@string)"goodbye, world");
        }
        (var dirs, errΔ1) = ReadDir(subΔ1, "."u8);
        if (errΔ1 != default! || len(dirs) != 1 || dirs[0].Name() != "goodbye.txt"u8) {
            slice<@string> names = default!;
            foreach (var (_, d) in dirs) {
                names = append(names, d.Name());
            }
            Ꮡt.Errorf(@"ReadDir(%s, ""."") = %v, %v, want %v, nil"u8, desc, names, errΔ1, new @string[]{"goodbye.txt"}.slice());
        }
    };
    // Test that Sub uses the method when present.
    var (sub, err) = Sub(new subOnly(new fstest_MapFSᴠSubFS(testFsys)), "sub"u8);
    check("subOnly"u8, sub, err);
    // Test that Sub uses Open when the method is not present.
    (sub, err) = Sub(new openOnly(testFsys), "sub"u8);
    check("openOnly"u8, sub, err);
    (_, err) = sub.Open("nonexist"u8);
    if (err == default!) {
        Ꮡt.Fatal((@string)"Open(nonexist): succeeded");
    }
    var (pe, ok) = err._<ж<fs.PathError>>(ᐧ);
    if (!ok) {
        Ꮡt.Fatalf("Open(nonexist): error is %T, want *PathError"u8, err);
    }
    if ((~pe).Path != "nonexist"u8) {
        Ꮡt.Fatalf("Open(nonexist): err.Path = %q, want %q"u8, (~pe).Path, (@string)"nonexist");
    }
    (_, err) = sub.Open("./"u8);
    if (!errors.Is(err, ErrInvalid)) {
        Ꮡt.Fatalf("Open(./): error is %v, want %v"u8, err, ErrInvalid);
    }
}

} // end fs_test_package
