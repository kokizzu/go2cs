// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build windows
namespace go.os.exec.@internal;

partial class fdtest_package {

// Exists is not implemented on windows and panics.
public static bool Exists(uintptr fd) {
    throw panic("unimplemented");
}

} // end fdtest_package
