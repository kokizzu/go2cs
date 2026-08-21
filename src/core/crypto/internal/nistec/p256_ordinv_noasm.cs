// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build (!amd64 && !arm64) || purego
[assembly: go.GoPositionMap("crypto/internal/nistec/p256_ordinv_noasm.go", "p256_ordinv_noasm.cs", "AA8Wgg==")]

namespace go.crypto.@internal;

using errors = errors_package;

partial class nistec_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string unimplementedˢ = "unimplemented"u8;

public static (slice<byte>, error) P256OrdInverse(slice<byte> k) {
    return (default!, errors.New(unimplementedˢ));
}

} // end nistec_package
