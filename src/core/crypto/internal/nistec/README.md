# go.crypto.internal.nistec

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-not_yet_validated-orange?logo=go)](https://go2cs.net/ValidatedTestPackages.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/crypto/internal/nistec@go1.23.1) [![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/crypto/internal/nistec) [![Source](https://img.shields.io/badge/Source-@1.23.1.6-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.6/src/core/crypto/internal/nistec)

Package nistec implements the NIST P elliptic curves from FIPS 186-4.

This package uses fiat-crypto or specialized assembly and Go code for its backend field arithmetic (not math/big) and exposes constant-time, heap allocation-free, byte slice-based safe APIs. Group operations use modern and safe complete addition formulas where possible. The point at infinity is handled and encoded according to SEC 1, Version 2.0, and invalid curve points can't be represented.

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
