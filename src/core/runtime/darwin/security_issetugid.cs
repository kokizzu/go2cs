// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build darwin || dragonfly || freebsd || illumos || netbsd || openbsd || solaris
namespace go;

partial class runtime_package {

// secureMode is only ever mutated in schedinit, so we don't need to worry about
// synchronization primitives.
internal static bool secureMode;

internal static void initSecureMode() {
    secureMode = issetugid() == 1;
}

internal static bool isSecureMode() {
    return secureMode;
}

} // end runtime_package
