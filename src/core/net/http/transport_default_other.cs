// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !wasm
[assembly: go.GoPositionMap("net/http/transport_default_other.go", "transport_default_other.cs", "AA0cgg==")]

namespace go.net;

using context = context_package;
using net = net_package;

partial class http_package {

internal static Func<context.Context, @string, @string, (net.Conn, error)> defaultTransportDialContext(ж<net.Dialer> Ꮡdialer) {
    return Ꮡdialer.DialContext;
}

} // end http_package
