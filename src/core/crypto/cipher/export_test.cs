// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto;

using static go.crypto.cipher_package;

partial class cipher_internal_test_package {

// Export internal functions for testing.
public static Func<global::go.crypto.cipher_package.Block, slice<byte>, global::go.crypto.cipher_package.BlockMode> NewCBCGenericEncrypter = newCBCGenericEncrypter;

public static Func<global::go.crypto.cipher_package.Block, slice<byte>, global::go.crypto.cipher_package.BlockMode> NewCBCGenericDecrypter = newCBCGenericDecrypter;

} // end cipher_internal_test_package
