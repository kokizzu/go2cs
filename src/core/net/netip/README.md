# go.net.netip

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-210%2F267_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.12.2/net.netip.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.12-00ADD8?logo=go)](https://pkg.go.dev/net/netip@go1.23.12)\
[![Source](https://img.shields.io/badge/Source-@1.23.12-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.12/src/net/netip) [![Source](https://img.shields.io/badge/Source-@1.23.12.2-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.12.2/src/core/net/netip)

Package netip defines an IP address type that's a small value type. Building on that \[Addr] type, the package also defines \[AddrPort] (an IP address and a port) and \[Prefix] (an IP address and a bit length prefix).

Compared to the [net.IP](https://pkg.go.dev/net@go1.23.12#IP) type, \[Addr] type takes less memory, is immutable, and is comparable (supports == and being a map key).

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
