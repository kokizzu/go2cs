// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || darwin || dragonfly || freebsd || (js && wasm) || netbsd || openbsd || solaris || wasip1
namespace go;

partial class os_package {

// According to sticky(8), neither open(2) nor mkdir(2) will create
// a file with the sticky bit set.
internal const bool supportsCreateWithStickyBit = false;

} // end os_package
