// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!amd64 && !386 && !arm && !ppc64le && !ppc64 && !s390x && !arm64) || purego
[assembly: go.GoPositionMap("crypto/md5/md5block_generic.go", "md5block_generic.cs", "AAwWgg==")]

namespace go.crypto;

partial class md5_package {

internal const bool haveAsm = false;

internal static void block(ref digest dig, slice<byte> p) {
    blockGeneric(ref dig, p);
}

} // end md5_package
