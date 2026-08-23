# go.net.netip

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-not_yet_validated-orange?logo=go)](https://go2cs.net/ValidatedTestPackages.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/net/netip@go1.23.1)\
[![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/net/netip) [![Source](https://img.shields.io/badge/Source-@1.23.1.7-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.7/src/core/net/netip)

Package netip defines an IP address type that's a small value type. Building on that \[Addr] type, the package also defines \[AddrPort] (an IP address and a port) and \[Prefix] (an IP address and a bit length prefix).

Compared to the [net.IP](https://pkg.go.dev/net@go1.23.1#IP) type, \[Addr] type takes less memory, is immutable, and is comparable (supports == and being a map key).

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
