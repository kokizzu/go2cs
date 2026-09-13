// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !(boringcrypto && linux && (amd64 || arm64) && !android && !msan && cgo)
namespace go.crypto.@internal;

partial class fips140_package {

internal const bool boringEnabled = false;

} // end fips140_package
