// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !windows && !plan9
namespace go;

using syscall = syscall_package;
using time = time_package;

partial class os_package {

// A fileStat is the implementation of FileInfo returned by Stat and Lstat.
[GoType] [GoValueClone("sys")] partial struct fileStat {
    internal @string name;
    internal int64 size;
    internal FileMode mode;
    internal time.Time modTime;
    internal syscall.Stat_t sys;
}

[GoRecv] internal static int64 Size(this ref fileStat fs) {
    return fs.size;
}

[GoRecv] internal static FileMode Mode(this ref fileStat fs) {
    return fs.mode;
}

[GoRecv] internal static time.Time ModTime(this ref fileStat fs) {
    return fs.modTime;
}

internal static any Sys(this ж<fileStat> Ꮡfs) {
    return Ꮡfs.of(fileStat.Ꮡsys);
}

internal static bool sameFile(ж<fileStat> Ꮡfs1, ж<fileStat> Ꮡfs2) {
    ref var fs1 = ref Ꮡfs1.DerefOrNull();
    ref var fs2 = ref Ꮡfs2.DerefOrNull();

    return fs1.sys.Dev == fs2.sys.Dev && fs1.sys.Ino == fs2.sys.Ino;
}

} // end os_package
