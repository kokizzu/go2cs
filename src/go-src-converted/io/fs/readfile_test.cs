// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.io;

using static go.io.fs_package;
using os = os_package;
using testing = testing_package;
using fstest = go.testing.fstest_package;
using time = time_package;
using fs = go.io.fs_package;
using go.testing;

partial class fs_test_package {

internal static fstest.MapFS testFsys = new fstest.MapFS(new map<@string, ж<fstest.MapFile>>{
    ["hello.txt"u8] = Ꮡ(new fstest.MapFile(
        Data: slice<byte>("hello, world"u8),
        Mode: 302,
        ModTime: time.Now(),
        Sys: ᏑsysValue)),
    ["sub/goodbye.txt"u8] = Ꮡ(new fstest.MapFile(
        Data: slice<byte>("goodbye, world"u8),
        Mode: 302,
        ModTime: time.Now(),
        Sys: ᏑsysValue))
});

internal static ж<nint> ᏑsysValue = new(default(nint));
internal static ref nint sysValue => ref ᏑsysValue.Value;

[GoType] partial struct readFileOnly {
    public go.io.fs_package.ReadFileFS ReadFileFS;
}

internal static (fs.File, error) Open(this readFileOnly _, @string name) {
    return (default!, ErrNotExist);
}

[GoType] partial struct openOnly {
    public go.io.fs_package.FS FS;
}

public static void TestReadFile(ж<testing.T> Ꮡt) {
    // Test that ReadFile uses the method when present.
    var (data, err) = ReadFile(new readFileOnly(new fstest_MapFSᴠReadFileFS(testFsys)), "hello.txt"u8);
    if (((sstring)data) != "hello, world"u8 || err != default!) {
        Ꮡt.Fatalf(@"ReadFile(readFileOnly, ""hello.txt"") = %q, %v, want %q, nil"u8, data, err, (@string)"hello, world");
    }
    // Test that ReadFile uses Open when the method is not present.
    (data, err) = ReadFile(new openOnly(testFsys), "hello.txt"u8);
    if (((sstring)data) != "hello, world"u8 || err != default!) {
        Ꮡt.Fatalf(@"ReadFile(openOnly, ""hello.txt"") = %q, %v, want %q, nil"u8, data, err, (@string)"hello, world");
    }
    // Test that ReadFile on Sub of . works (sub_test checks non-trivial subs).
    (var sub, err) = Sub(testFsys, "."u8);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    (data, err) = ReadFile(sub, "hello.txt"u8);
    if (((sstring)data) != "hello, world"u8 || err != default!) {
        Ꮡt.Fatalf(@"ReadFile(sub(.), ""hello.txt"") = %q, %v, want %q, nil"u8, data, err, (@string)"hello, world");
    }
}

[GoType("dyn")] partial struct TestReadFilePath_fsys {
    public go.io.fs_package.FS FS;
}

public static void TestReadFilePath(ж<testing.T> Ꮡt) {
    var fsys = os.DirFS(Ꮡt.TempDir());
    var (_, err1) = ReadFile(fsys, "non-existent"u8);
    var (_, err2) = ReadFile(new TestReadFilePath_fsys(fsys), "non-existent"u8);
    {
        @string s1 = errorPath(err1);
        @string s2 = errorPath(err2); if (s1 != s2) {
            Ꮡt.Fatalf("s1: %s != s2: %s"u8, s1, s2);
        }
    }
}

} // end fs_test_package
