// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("crypto/rsa/rsa_export_test.go", "rsa_export_test.cs", "")]

namespace go.crypto;

using hash = hash_package;
using io = io_package;
using static go.crypto.rsa_package;

partial class rsa_internal_test_package {

public static Func<slice<byte>, io.Reader, error> NonZeroRandomBytes = nonZeroRandomBytes;

public static Func<slice<byte>, nint, slice<byte>, hash.Hash, (slice<byte>, error)> EMSAPSSEncode;
internal static void initᴛEMSAPSSEncode() { EMSAPSSEncode = emsaPSSEncode; }

public static Func<slice<byte>, slice<byte>, nint, nint, hash.Hash, error> EMSAPSSVerify;
internal static void initᴛEMSAPSSVerify() { EMSAPSSVerify = emsaPSSVerify; }

public static error InvalidSaltLenErr;
internal static void initᴛInvalidSaltLenErr() { InvalidSaltLenErr = invalidSaltLenErr; }

} // end rsa_internal_test_package
