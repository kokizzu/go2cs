// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using Δio = io_package;
using Δruntime = runtime_package;
using syscall = syscall_package;
using @unsafe = unsafe_package;
using fs = go.io.fs_package;

partial class os_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸruntime() {
    builtin.initPackage(typeof(runtime_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

// Auxiliary information if the File describes a directory
[GoType] partial struct dirInfo {
    internal uintptr dir; // Pointer to DIR structure from dirent.h
}

[GoRecv] internal static void close(this ref dirInfo d) {
    if (d.dir == 0) {
        return;
    }
    closedir(d.dir);
    d.dir = 0;
}

// go2cs generated this placeholder — func readdir is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// We lost the race: try again.
// EOF
// Darwin may return a zero inode when a directory entry has been
// deleted but not yet removed from the directory. The man page for
// getdirentries(2) states that programs are responsible for skipping
// those entries:
//
//   Users of getdirentries() should skip entries with d_fileno = 0,
//   as such entries represent files which have been deleted but not
//   yet removed from the directory entry.
//
// Check for useless names before allocating a string.
// File disappeared between readdir and stat.
// Treat as if it didn't exist.
// File disappeared between readdir + stat.
// Treat as if it didn't exist.
internal static FileMode dtToType(uint8 typ) {
    var exprᴛ1 = typ;
    if (exprᴛ1 == syscall.DT_BLK) {
        return ModeDevice;
    }
    if (exprᴛ1 == syscall.DT_CHR) {
        return (fs.FileMode)(ModeDevice | ModeCharDevice);
    }
    if (exprᴛ1 == syscall.DT_DIR) {
        return ModeDir;
    }
    if (exprᴛ1 == syscall.DT_FIFO) {
        return ModeNamedPipe;
    }
    if (exprᴛ1 == syscall.DT_LNK) {
        return ModeSymlink;
    }
    if (exprᴛ1 == syscall.DT_REG) {
        return 0;
    }
    if (exprᴛ1 == syscall.DT_SOCK) {
        return ModeSocket;
    }

    return ~((fs.FileMode)((fs.FileMode)0));
}

// Implemented in syscall/syscall_darwin.go.

//go:linkname closedir syscall.closedir
internal static partial error /*err*/ closedir(uintptr dir);

//go:linkname readdir_r syscall.readdir_r
internal static partial syscall.Errno /*res*/ readdir_r(uintptr dir, ж<syscall.Dirent> entry, ж<ж<syscall.Dirent>> result);

} // end os_package
