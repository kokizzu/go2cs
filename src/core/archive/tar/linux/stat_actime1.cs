// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || linux || dragonfly || openbsd || solaris
namespace go.archive;

using syscall = syscall_package;
using time = time_package;

partial class tar_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

internal static time.Time statAtime(ж<syscall.Stat_t> Ꮡst) {
    ref var st = ref Ꮡst.DerefOrNull();

    var (ᴛ1, ᴛ2) = st.Atim.Unix();
    return time.Unix(ᴛ1, ᴛ2);
}

internal static time.Time statCtime(ж<syscall.Stat_t> Ꮡst) {
    ref var st = ref Ꮡst.DerefOrNull();

    var (ᴛ3, ᴛ4) = st.Ctim.Unix();
    return time.Unix(ᴛ3, ᴛ4);
}

} // end tar_package
