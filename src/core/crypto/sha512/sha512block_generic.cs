// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!amd64 && !arm64 && !ppc64 && !ppc64le && !riscv64 && !s390x) || purego
[assembly: go.GoPositionMap("crypto/sha512/sha512block_generic.go", "sha512block_generic.cs", "AAoSgg==")]

namespace go.crypto;

partial class sha512_package {

internal static void block(ref digest dig, slice<byte> p) {
    blockGeneric(ref dig, p);
}

} // end sha512_package
