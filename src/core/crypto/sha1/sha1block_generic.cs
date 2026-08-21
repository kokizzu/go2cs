// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!amd64 && !386 && !arm && !s390x && !arm64) || purego
[assembly: go.GoPositionMap("crypto/sha1/sha1block_generic.go", "sha1block_generic.cs", "AAoSgg==")]

namespace go.crypto;

partial class sha1_package {

internal static void block(ref digest dig, slice<byte> p) {
    blockGeneric(ref dig, p);
}

} // end sha1_package
