// Copyright (c) 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !amd64 || purego
[assembly: go.GoPositionMap("crypto/internal/edwards25519/field/fe_amd64_noasm.go", "fe_amd64_noasm.cs", "AAoSgKSA")]

namespace go.crypto.@internal.edwards25519;

partial class field_package {

internal static void feMul(ж<Element> Ꮡv, ref Element x, ref Element y) {
    feMulGeneric(Ꮡv, ref x, ref y);
}

internal static void feSquare(ж<Element> Ꮡv, ref Element x) {
    feSquareGeneric(Ꮡv, ref x);
}

} // end field_package
