// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !osusergo && darwin
namespace go.os;

using unix = @internal.syscall.unix_package;
using @internal.syscall;

partial class user_package {

internal static _C_int getGroupList(ж<_C_char> Ꮡname, _C_gid_t userGID, ж<_C_gid_t> Ꮡgids, ж<_C_int> Ꮡn) {
    var err = unix.Getgrouplist(Ꮡname, userGID, Ꮡgids, Ꮡn);
    if (err != default!) {
        return -1;
    }
    return 0;
}

} // end user_package
