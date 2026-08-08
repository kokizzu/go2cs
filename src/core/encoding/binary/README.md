# go.encoding.binary

> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).
> Go version: 1.23.1

[![Tests](https://img.shields.io/badge/Tests-137%2F146_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.4/encoding.binary.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/encoding/binary@go1.23.1) [![Source](https://img.shields.io/badge/Source-Go_@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/encoding/binary) [![Source](https://img.shields.io/badge/Source-C%23_@1.23.1.4-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.4/src/core/encoding/binary)

Package binary implements simple translation between numbers and byte sequences and encoding and decoding of varints.

Numbers are translated by reading and writing fixed-size values. A fixed-size value is either a fixed-size arithmetic type (bool, int8, uint8, int16, float32, complex64, ...) or an array or struct containing only fixed-size values.

The varint functions encode and decode single integer values using a variable-length encoding; smaller values require fewer bytes. For a specification, see [https://developers.google.com/protocol-buffers/docs/encoding](https://developers.google.com/protocol-buffers/docs/encoding).

This package favors simplicity over efficiency. Clients that require high-performance serialization, especially for large data structures, should look at more advanced solutions such as the [encoding/gob](/encoding/gob) package or [google.golang.org/protobuf](/google.golang.org/protobuf) for protocol buffers.

---
Part of the go2cs converted Go standard library. See the [repository](https://github.com/ritchiecarroll/go2cs) for usage and details.

Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.
