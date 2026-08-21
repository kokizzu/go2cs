// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("runtime/typekind.go", "typekind.cs", "AAwUkg==")]

namespace go;

using abi = @internal.abi_package;
using @internal;

partial class runtime_package {

// isDirectIface reports whether t is stored directly in an interface value.
internal static bool isDirectIface(ref _type t) {
    return (abiꓸKind)(t.Kind_ & abi.KindDirectIface) != 0;
}

} // end runtime_package
