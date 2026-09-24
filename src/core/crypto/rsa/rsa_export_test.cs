// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using io = io_package;
using static go.crypto.rsa_package;

partial class rsa_internal_test_package {

public static Func<slice<byte>, io.Reader, error> NonZeroRandomBytes = nonZeroRandomBytes;

} // end rsa_internal_test_package
