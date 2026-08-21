// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !unix
[assembly: go.GoPositionMap("runtime/create_file_nounix.go", "create_file_nounix.cs", "AA8WgoI=")]

namespace go;

partial class runtime_package {

internal const bool canCreateFile = false;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string unimplementedˢ = "unimplemented"u8;

internal static int32 create(ref byte name, int32 perm) {
    @throw(unimplementedˢ);
    return -1;
}

} // end runtime_package
