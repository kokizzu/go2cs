// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using fips140 = go.crypto.@internal.fips140_package;
using check = go.crypto.@internal.fips140.check_package;
using godebug = go.@internal.godebug_package;
using go.@internal;
using go.crypto.@internal;
using go.crypto.@internal.fips140;

partial class fips140_package {

internal static ж<godebug.Setting> fips140GODEBUG = godebug.New("fips140"u8);

// Enabled reports whether the cryptography libraries are operating in FIPS
// 140-3 mode.
//
// It can be controlled at runtime using the GODEBUG setting "fips140". If set
// to "on", FIPS 140-3 mode is enabled. If set to "only", non-approved
// cryptography functions will additionally return errors or panic.
//
// This can't be changed after the program has started.
public static bool Enabled() {
    @string godebug = fips140GODEBUG.Value();
    var currentlyEnabled = godebug == "on"u8 || godebug == "only"u8 || godebug == "debug"u8;
    if (currentlyEnabled != fips140.Enabled) {
        throw panic("crypto/fips140: GODEBUG setting changed after program start");
    }
    if (fips140.Enabled && !check.Verified) {
        throw panic("crypto/fips140: FIPS 140-3 mode enabled, but integrity check didn't pass");
    }
    return fips140.Enabled;
}

} // end fips140_package
