# go.internal.saferio

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).
> Go version: 1.23.1

[![Tests](https://img.shields.io/badge/Tests-17%2F17_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.3/internal.saferio.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/internal/saferio@go1.23.1)

Package saferio provides I/O functions that avoid allocating large amounts of memory unnecessarily. This is intended for packages that read data from an [io.Reader](/io#Reader) where the size is part of the input data but the input may be corrupt, or may be provided by an untrustworthy attacker.

---
Part of the go2cs converted Go standard library. See the [repository](https://github.com/ritchiecarroll/go2cs) for usage and details.

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
