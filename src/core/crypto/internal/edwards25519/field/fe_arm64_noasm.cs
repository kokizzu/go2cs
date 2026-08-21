// Copyright (c) 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !arm64 || purego
[assembly: go.GoPositionMap("crypto/internal/edwards25519/field/fe_arm64_noasm.go", "fe_arm64_noasm.cs", "AAoSgg==")]

namespace go.crypto.@internal.edwards25519;

partial class field_package {

internal static ж<Element> carryPropagate(this ж<Element> Ꮡv) {
    return Ꮡv.carryPropagateGeneric();
}

} // end field_package
