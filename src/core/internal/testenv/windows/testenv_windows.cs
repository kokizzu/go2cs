// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using os = os_package;
using filepath = path.filepath_package;
using sync = sync_package;
using Δsyscall = syscall_package;
using path;

partial class testenv_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

internal static ж<sync.Once> ᏑsymlinkOnce = new StandardBox<sync.Once>(default(sync.Once));
internal static ref sync.Once symlinkOnce => ref ᏑsymlinkOnce.Value;

internal static error winSymlinkErr;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string symtestˢ = "symtest"u8;
private static readonly @string targetˢ = "target"u8;
private static readonly @string symlinkˢ = "symlink"u8;

internal static void initWinHasSymlink() {
    GoFrame ᒐ = default;
    try {
        var (tmpdir, err) = os.MkdirTemp(""u8, symtestˢ);
        if (err != default!) {
            throw panic("failed to create temp directory: " + err.Error());
        }
        defer(os.RemoveAll, tmpdir, ref ᒐ);
        err = os.Symlink(targetˢ, filepath.Join(tmpdir, symlinkˢ));
        if (err != default!) {
            err = err._<ж<os.LinkError>>().Value.Err;
            var exprᴛ1 = err;
            if (AreEqual(exprᴛ1, Δsyscall.EWINDOWS) || AreEqual(exprᴛ1, Δsyscall.ERROR_PRIVILEGE_NOT_HELD)) {
                winSymlinkErr = err;
            }

        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string symlinksAreNotSupportedˢ = ": symlinks are not supported on your version of Windows"u8;
private static readonly @string youDonTHaveEnoughˢ = ": you don't have enough privileges to create symlinks"u8;

internal static (bool ok, @string reason) hasSymlink() {
    ᏑsymlinkOnce.Do(initWinHasSymlink);
    var exprᴛ1 = winSymlinkErr;
    if (AreEqual(exprᴛ1, default!)) {
        return (true, "");
    }
    if (AreEqual(exprᴛ1, Δsyscall.EWINDOWS)) {
        return (false, symlinksAreNotSupportedˢ);
    }
    if (AreEqual(exprᴛ1, Δsyscall.ERROR_PRIVILEGE_NOT_HELD)) {
        return (false, youDonTHaveEnoughˢ);
    }

    return (false, "");
}

} // end testenv_package
