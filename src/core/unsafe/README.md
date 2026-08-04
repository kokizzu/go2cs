# go.unsafe

> Hand-implemented C# counterpart of the Go standard library's `unsafe` package, by [go2cs](https://github.com/ritchiecarroll/go2cs).
> Go version: 1.23.1

[![Tests](https://img.shields.io/badge/Tests-none_to_validate-lightgrey?logo=go)](https://go2cs.net/ValidatedTestPackages.html)

Package unsafe contains operations that step around the type safety of Go programs. In Go it is a compiler intrinsic with no Go source to convert; this package is its hand-maintained managed implementation — pointer reinterpretation, `Sizeof`/`Alignof`/`Offsetof`, and the `Slice`/`String`/`SliceData`/`StringData` constructors — built over the go2cs runtime's `ж<T>` box and pinned-buffer machinery.

---
Part of the go2cs converted Go standard library. See the [repository](https://github.com/ritchiecarroll/go2cs) for usage and details.

This package is a hand-maintained implementation of the Go standard library's `unsafe` API rather than converted Go source (the original is a compiler intrinsic). The go2cs implementation is distributed under the MIT license.
