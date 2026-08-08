// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (cgo || darwin) && !osusergo && unix && !android
namespace go.os;

using fmt = fmt_package;
using runtime = runtime_package;
using strconv = strconv_package;
using strings = strings_package;
using syscall = syscall_package;
using @unsafe = unsafe_package;

partial class user_package {

internal static (ж<User>, error) current() {
    return lookupUnixUid(syscall.Getuid());
}

internal static (ж<User>, error) lookupUser(@string username) {
    ref var pwd = ref heap(new _C_struct_passwd(), out var Ꮡpwd);
    bool found = default!;
    var nameC = new slice<byte>(len(username) + 1);
    copy(nameC, username);
    var nameCʗ1 = nameC;
    var err = retryWithBuffer(userBuffer, (slice<byte> buf) => {
        syscall.Errno errno = default!;
        (Ꮡpwd.Value, found, errno) = _C_getpwnam_r(Ꮡ(nameCʗ1, 0),
            Ꮡ(buf, 0), ((_C_size_t)len(buf)));
        return errno;
    });
    if (AreEqual(err, syscall.ENOENT) || (err == default! && !found)) {
        return (default!, ((UnknownUserError)username));
    }
    if (err != default!) {
        return (default!, fmt.Errorf("user: lookup username %s: %v"u8, username, err));
    }
    return (buildUser(Ꮡpwd), err);
}

internal static (ж<User>, error) lookupUserId(@string uid) {
    var (i, e) = strconv.Atoi(uid);
    if (e != default!) {
        return (default!, e);
    }
    return lookupUnixUid(i);
}

internal static (ж<User>, error) lookupUnixUid(nint uid) {
    ref var pwd = ref heap(new _C_struct_passwd(), out var Ꮡpwd);
    bool found = default!;
    var err = retryWithBuffer(userBuffer, (slice<byte> buf) => {
        syscall.Errno errno = default!;
        (Ꮡpwd.Value, found, errno) = _C_getpwuid_r(((_C_uid_t)uid),
            Ꮡ(buf, 0), ((_C_size_t)len(buf)));
        return errno;
    });
    if (AreEqual(err, syscall.ENOENT) || (err == default! && !found)) {
        return (default!, ((UnknownUserIdError)uid));
    }
    if (err != default!) {
        return (default!, fmt.Errorf("user: lookup userid %d: %v"u8, uid, err));
    }
    return (buildUser(Ꮡpwd), default!);
}

internal static ж<User> buildUser(ж<_C_struct_passwd> Ꮡpwd) {
    var u = Ꮡ(new User(
        Uid: strconv.FormatUint((uint64)_C_pw_uid(Ꮡpwd), 10),
        Gid: strconv.FormatUint((uint64)_C_pw_gid(Ꮡpwd), 10),
        Username: _C_GoString(_C_pw_name(Ꮡpwd)),
        Name: _C_GoString(_C_pw_gecos(Ꮡpwd)),
        HomeDir: _C_GoString(_C_pw_dir(Ꮡpwd))
    ));
    // The pw_gecos field isn't quite standardized. Some docs
    // say: "It is expected to be a comma separated list of
    // personal data where the first item is the full name of the
    // user."
    (u.Value.Name, _, _) = strings.Cut((~u).Name, ","u8);
    return u;
}

internal static (ж<Group>, error) lookupGroup(@string groupname) {
    ref var grp = ref heap(new _C_struct_group(), out var Ꮡgrp);
    bool found = default!;
    var cname = new slice<byte>(len(groupname) + 1);
    copy(cname, groupname);
    var cnameʗ1 = cname;
    var err = retryWithBuffer(groupBuffer, (slice<byte> buf) => {
        syscall.Errno errno = default!;
        (Ꮡgrp.Value, found, errno) = _C_getgrnam_r(Ꮡ(cnameʗ1, 0),
            Ꮡ(buf, 0), ((_C_size_t)len(buf)));
        return errno;
    });
    if (AreEqual(err, syscall.ENOENT) || (err == default! && !found)) {
        return (default!, ((UnknownGroupError)groupname));
    }
    if (err != default!) {
        return (default!, fmt.Errorf("user: lookup groupname %s: %v"u8, groupname, err));
    }
    return (buildGroup(Ꮡgrp), default!);
}

internal static (ж<Group>, error) lookupGroupId(@string gid) {
    var (i, e) = strconv.Atoi(gid);
    if (e != default!) {
        return (default!, e);
    }
    return lookupUnixGid(i);
}

internal static (ж<Group>, error) lookupUnixGid(nint gid) {
    ref var grp = ref heap(new _C_struct_group(), out var Ꮡgrp);
    bool found = default!;
    var err = retryWithBuffer(groupBuffer, (slice<byte> buf) => {
        syscall.Errno errno = default!;
        (Ꮡgrp.Value, found, errno) = _C_getgrgid_r(((_C_gid_t)gid),
            Ꮡ(buf, 0), ((_C_size_t)len(buf)));
        return errno;
    });
    if (AreEqual(err, syscall.ENOENT) || (err == default! && !found)) {
        return (default!, ((UnknownGroupIdError)strconv.Itoa(gid)));
    }
    if (err != default!) {
        return (default!, fmt.Errorf("user: lookup groupid %d: %v"u8, gid, err));
    }
    return (buildGroup(Ꮡgrp), default!);
}

internal static ж<Group> buildGroup(ж<_C_struct_group> Ꮡgrp) {
    var g = Ꮡ(new Group(
        Gid: strconv.Itoa((nint)_C_gr_gid(Ꮡgrp)),
        Name: _C_GoString(_C_gr_name(Ꮡgrp))
    ));
    return g;
}

[GoType("num:int32")] partial struct bufferKind;

internal static bufferKind userBuffer = ((bufferKind)_C__SC_GETPW_R_SIZE_MAX);
internal static bufferKind groupBuffer = ((bufferKind)_C__SC_GETGR_R_SIZE_MAX);

internal static _C_size_t initialSize(this bufferKind k) {
    var sz = _C_sysconf(((_C_int)k));
    if (sz == -1) {
        // DragonFly and FreeBSD do not have _SC_GETPW_R_SIZE_MAX.
        // Additionally, not all Linux systems have it, either. For
        // example, the musl libc returns -1.
        return 1024;
    }
    if (!isSizeReasonable((int64)sz)) {
        // Truncate.  If this truly isn't enough, retryWithBuffer will error on the first run.
        return maxBufferSize;
    }
    return ((_C_size_t)sz);
}

// retryWithBuffer repeatedly calls f(), increasing the size of the
// buffer each time, until f succeeds, fails with a non-ERANGE error,
// or the buffer exceeds a reasonable limit.
internal static error retryWithBuffer(bufferKind kind, Func<slice<byte>, syscall.Errno> f) {
    var buf = new slice<byte>((nint)(kind.initialSize()));
    while (ᐧ) {
        var errno = f(buf);
        if (errno == 0){
            return default!;
        } else 
        if (runtime.GOOS == "aix"u8 && errno + 1 == 0){
        } else 
        if (errno != syscall.ERANGE) {
            // On AIX getpwuid_r appears to return -1,
            // not ERANGE, on buffer overflow.
            return errno;
        }
        nint newSize = len(buf) * 2;
        if (!isSizeReasonable((int64)newSize)) {
            return fmt.Errorf("internal buffer exceeds %d bytes"u8, (nint)(maxBufferSize));
        }
        buf = new slice<byte>(newSize);
    }
}

internal static UntypedInt maxBufferSize => /* 1 << 20 */ 1048576;

internal static bool isSizeReasonable(int64 sz) {
    return sz > 0 && sz <= maxBufferSize;
}

// Because we can't use cgo in tests:
internal static _C_struct_passwd structPasswdForNegativeTest() {
    ref var sp = ref heap<_C_struct_passwd>(out var Ꮡsp);
    sp = new _C_struct_passwd();
    _C_pw_uidp(Ꮡsp).Value = 4294967296L - 2;
    _C_pw_gidp(Ꮡsp).Value = 4294967296L - 3;
    return sp;
}

} // end user_package
