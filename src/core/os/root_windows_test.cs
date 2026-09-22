// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build windows
namespace go;

using errors = errors_package;
using Δos = os_package;
using filepath = go.path.filepath_package;
using Δtesting = testing_package;
using fs = go.io.fs_package;
using go.path;
using static go.os_internal_test_package;

partial class os_test_package {

// Verify that Root.Open rejects Windows reserved names.
public static void TestRootWindowsDeviceNames(ж<Δtesting.T> Ꮡt) {
    GoFrame ᒐ = default;
    try {
        var (r, err) = Δos.OpenRoot(Ꮡt.TempDir());
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        var rʗ1 = r;
        defer(() => rʗ1.Close(), ref ᒐ);
        {
            var (f, errΔ1) = r.Open(nulˢ4); if (errΔ1 == default!) {
                Ꮡt.Errorf(@"r.Open(""NUL"") succeeded; want error"""u8);
                f.Close();
            }
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string fileˢ3 = "FILE"u8;

// Verify that Root.Open is case-insensitive.
// (The wrong options to NtOpenFile could make operations case-sensitive,
// so this is worth checking.)
public static void TestRootWindowsCaseInsensitivity(ж<Δtesting.T> Ꮡt) {
    GoFrame ᒐ = default;
    try {
        @string dir = Ꮡt.TempDir();
        {
            var errΔ1 = Δos.WriteFile(filepath.Join(dir, fileˢ2), default!, 438); if (errΔ1 != default!) {
                Ꮡt.Fatal(errΔ1);
            }
        }
        var (r, err) = Δos.OpenRoot(dir);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        var rʗ1 = r;
        defer(() => rʗ1.Close(), ref ᒐ);
        (var f, err) = r.Open(fileˢ3);
        if (err != default!) {
            Ꮡt.Fatal(err);
        }
        f.Close();
        {
            var errΔ2 = r.Remove(fileˢ3); if (errΔ2 != default!) {
                Ꮡt.Fatal(errΔ2);
            }
        }
        {
            var (_, errΔ3) = Δos.Stat(filepath.Join(dir, fileˢ2)); if (!errors.Is(errΔ3, Δos.ErrNotExist)) {
                Ꮡt.Fatalf("os.Stat(file) after deletion: %v, want ErrNotFound"u8, errΔ3);
            }
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end os_test_package
