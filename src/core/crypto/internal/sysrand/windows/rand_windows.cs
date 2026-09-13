// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto.@internal;

using windows = go.@internal.syscall.windows_package;
using go.@internal.syscall;

partial class sysrand_package {

internal static error read(slice<byte> b) {
    return windows.ProcessPrng(b);
}

} // end sysrand_package
