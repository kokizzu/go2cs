# go.time.tzdata

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).
> Go version: 1.23.1

[![Tests](https://img.shields.io/badge/Tests-none_to_validate-lightgrey?logo=go)](https://go2cs.net/ValidatedTestPackages.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/time/tzdata@go1.23.1) [![Source](https://img.shields.io/badge/Source-Go_@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/time/tzdata) [![Source](https://img.shields.io/badge/Source-C%23_@1.23.1.4-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.4/src/core/time/tzdata)

Package tzdata provides an embedded copy of the timezone database. If this package is imported anywhere in the program, then if the time package cannot find tzdata files on the system, it will use this embedded information.

Importing this package will increase the size of a program by about 450 KB.

This package should normally be imported by a program's main package, not by a library. Libraries normally shouldn't decide whether to include the timezone database in a program.

This package will be automatically imported if you build with -tags timetzdata.

---
Part of the go2cs converted Go standard library. See the [repository](https://github.com/ritchiecarroll/go2cs) for usage and details.

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
