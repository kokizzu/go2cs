// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using unix = @internal.syscall.unix_package;
using @internal.syscall;

partial class net_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸsyscallꓸunix() {
    builtin.initPackage(typeof(@internal.syscall.unix_package));
}

internal static UntypedInt cgoAddrInfoFlags => /* (unix.AI_CANONNAME | unix.AI_V4MAPPED | unix.AI_ALL) & unix.AI_MASK */ 2;

} // end net_package
