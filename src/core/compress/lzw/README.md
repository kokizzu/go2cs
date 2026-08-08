# go.compress.lzw

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).
> Go version: 1.23.1

[![Tests](https://img.shields.io/badge/Tests-17%2F17_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.4/compress.lzw.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/compress/lzw@go1.23.1) [![Source](https://img.shields.io/badge/Source-Go_@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/compress/lzw) [![Source](https://img.shields.io/badge/Source-C%23_@1.23.1.4-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.4/src/core/compress/lzw)

Package lzw implements the Lempel-Ziv-Welch compressed data format, described in T. A. Welch, “A Technique for High-Performance Data Compression”, Computer, 17(6) (June 1984), pp 8-19.

In particular, it implements LZW as used by the GIF and PDF file formats, which means variable-width codes up to 12 bits and the first two non-literal codes are a clear code and an EOF code.

The TIFF file format uses a similar but incompatible version of the LZW algorithm. See the golang.org/x/image/tiff/lzw package for an implementation.

---
Part of the go2cs converted Go standard library. See the [repository](https://github.com/ritchiecarroll/go2cs) for usage and details.

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
