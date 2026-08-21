// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !boringcrypto
[assembly: go.GoPositionMap("crypto/x509/notboring.go", "notboring.cs", "AAoSgA==")]

namespace go.crypto;

partial class x509_package {

internal static bool boringAllowCert(ref Certificate c) {
    return true;
}

} // end x509_package
