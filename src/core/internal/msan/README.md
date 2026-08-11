# go.internal.msan

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-none_to_validate-lightgrey?logo=go)](https://go2cs.net/ValidatedTestPackages.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/internal/msan@go1.23.1) [![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/internal/msan) [![Source](https://img.shields.io/badge/Source-@1.23.1.6-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.6/src/core/internal/msan)

Package msan contains helper functions for manually instrumenting code for the memory sanitizer. This package exports the private msan routines in runtime unconditionally but without the "msan" build tag they are no-ops.

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
