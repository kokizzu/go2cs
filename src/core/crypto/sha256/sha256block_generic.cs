// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!amd64 && !386 && !s390x && !ppc64le && !ppc64 && !arm64) || purego
[assembly: go.GoPositionMap("crypto/sha256/sha256block_generic.go", "sha256block_generic.cs", "AAoSgg==")]

namespace go.crypto;

partial class sha256_package {

internal static void block(ref digest dig, slice<byte> p) {
    blockGeneric(ref dig, p);
}

} // end sha256_package
