# go.compress.zlib

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).
> Go version: 1.23.1

[![Tests](https://img.shields.io/badge/Tests-6%2F6_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.4/compress.zlib.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/compress/zlib@go1.23.1) [![Source](https://img.shields.io/badge/Source-Go_@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/compress/zlib) [![Source](https://img.shields.io/badge/Source-C%23_@1.23.1.4-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.4/src/core/compress/zlib)

Package zlib implements reading and writing of zlib format compressed data, as specified in RFC 1950.

The implementation provides filters that uncompress during reading and compress during writing.  For example, to write compressed data to a buffer:

	var b bytes.Buffer
	w := zlib.NewWriter(&b)
	w.Write([]byte("hello, world\n"))
	w.Close()

and to read that data back:

	r, err := zlib.NewReader(&b)
	io.Copy(os.Stdout, r)
	r.Close()

---
Part of the go2cs converted Go standard library. See the [repository](https://github.com/ritchiecarroll/go2cs) for usage and details.

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
