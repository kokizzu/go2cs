// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build darwin || (openbsd && !mips64)
namespace go.@internal.syscall;

using syscall = syscall_package;
// blank import: unsafe_package (side effects only; no using emitted — a `using _` alias hijacks C# discards) // for linkname

partial class unix_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

public static error Unlinkat(nint dirfd, @string path, nint flags) {
    return unlinkat(dirfd, path, flags);
}

public static (nint, error) Openat(nint dirfd, @string path, nint flags, uint32 perm) {
    return openat(dirfd, path, flags, perm);
}

public static error Fstatat(nint dirfd, @string path, ж<syscall.Stat_t> Ꮡstat, nint flags) {
    return fstatat(dirfd, path, Ꮡstat, flags);
}

//go:linkname unlinkat syscall.unlinkat
internal static partial error unlinkat(nint dirfd, @string path, nint flags);

//go:linkname openat syscall.openat
internal static partial (nint, error) openat(nint dirfd, @string path, nint flags, uint32 perm);

//go:linkname fstatat syscall.fstatat
internal static partial error fstatat(nint dirfd, @string path, ж<syscall.Stat_t> stat, nint flags);

} // end unix_package
