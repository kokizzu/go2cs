# go.text.scanner

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-18%2F18_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.6/text.scanner.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/text/scanner@go1.23.1)\
[![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/text/scanner) [![Source](https://img.shields.io/badge/Source-@1.23.1.6-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.6/src/core/text/scanner)

Package scanner provides a scanner and tokenizer for UTF-8-encoded text. It takes an io.Reader providing the source, which then can be tokenized through repeated calls to the Scan function. For compatibility with existing tools, the NUL character is not allowed. If the first character in the source is a UTF-8 encoded byte order mark (BOM), it is discarded.

By default, a \[Scanner] skips white space and Go comments and recognizes all literals as defined by the Go language specification. It may be customized to recognize only a subset of those literals and to recognize different identifier and white space characters.

---

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
