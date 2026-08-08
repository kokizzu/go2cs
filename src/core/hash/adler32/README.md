# go.hash.adler32

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-2%2F2_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.4/hash.adler32.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/hash/adler32@go1.23.1) [![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/hash/adler32) [![Source](https://img.shields.io/badge/Source-@1.23.1.4-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.4/src/core/hash/adler32)

Package adler32 implements the Adler-32 checksum.

It is defined in RFC 1950:

	Adler-32 is composed of two sums accumulated per byte: s1 is
	the sum of all bytes, s2 is the sum of all s1 values. Both sums
	are done modulo 65521. s1 is initialized to 1, s2 to zero.  The
	Adler-32 checksum is stored as s2*65536 + s1 in most-
	significant-byte first (network) order.

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
