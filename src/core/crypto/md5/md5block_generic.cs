// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!386 && !amd64 && !arm && !arm64 && !loong64 && !ppc64 && !ppc64le && !riscv64 && !s390x) || purego
namespace go.crypto;

partial class md5_package {

internal const bool haveAsm = false;

internal static void block(ref digest dig, slice<byte> p) {
    blockGeneric(ref dig, p);
}

} // end md5_package
