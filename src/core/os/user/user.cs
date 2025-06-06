// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

/*
Package user allows user account lookups by name or id.

For most Unix systems, this package has two internal implementations of
resolving user and group ids to names, and listing supplementary group IDs.
One is written in pure Go and parses /etc/passwd and /etc/group. The other
is cgo-based and relies on the standard C library (libc) routines such as
getpwuid_r, getgrnam_r, and getgrouplist.

When cgo is available, and the required routines are implemented in libc
for a particular platform, cgo-based (libc-backed) code is used.
This can be overridden by using osusergo build tag, which enforces
the pure Go implementation.
*/
namespace go.os;

using strconv = strconv_package;

partial class user_package {

// These may be set to false in init() for a particular platform and/or
// build flags to let the tests know to skip tests of some features.
internal static bool userImplemented = true;

internal static bool groupImplemented = true;

internal static bool groupListImplemented = true;

// User represents a user account.
[GoType] partial struct User {
    // Uid is the user ID.
    // On POSIX systems, this is a decimal number representing the uid.
    // On Windows, this is a security identifier (SID) in a string format.
    // On Plan 9, this is the contents of /dev/user.
    public @string Uid;
    // Gid is the primary group ID.
    // On POSIX systems, this is a decimal number representing the gid.
    // On Windows, this is a SID in a string format.
    // On Plan 9, this is the contents of /dev/user.
    public @string Gid;
    // Username is the login name.
    public @string Username;
    // Name is the user's real or display name.
    // It might be blank.
    // On POSIX systems, this is the first (or only) entry in the GECOS field
    // list.
    // On Windows, this is the user's display name.
    // On Plan 9, this is the contents of /dev/user.
    public @string Name;
    // HomeDir is the path to the user's home directory (if they have one).
    public @string HomeDir;
}

// Group represents a grouping of users.
//
// On POSIX systems Gid contains a decimal number representing the group ID.
[GoType] partial struct Group {
    public @string Gid; // group ID
    public @string Name; // group name
}

[GoType("num:nint")] partial struct UnknownUserIdError;

public static @string Error(this UnknownUserIdError e) {
    return "user: unknown userid "u8 + strconv.Itoa(((nint)e));
}

[GoType("@string")] partial struct UnknownUserError;

public static @string Error(this UnknownUserError e) {
    return "user: unknown user "u8 + ((@string)e);
}

[GoType("@string")] partial struct UnknownGroupIdError;

public static @string Error(this UnknownGroupIdError e) {
    return "group: unknown groupid "u8 + ((@string)e);
}

[GoType("@string")] partial struct UnknownGroupError;

public static @string Error(this UnknownGroupError e) {
    return "group: unknown group "u8 + ((@string)e);
}

} // end user_package
