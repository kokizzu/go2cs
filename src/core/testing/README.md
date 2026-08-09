# go.testing

> Hand-implemented C# counterpart of the Go standard library's `testing` package, by [go2cs](https://github.com/ritchiecarroll/go2cs).

[![Tests](https://img.shields.io/badge/Tests-not_yet_validated-orange?logo=go)](https://go2cs.net/ValidatedTestPackages.html) [![Docs](https://img.shields.io/badge/Docs-@1.23.1-00ADD8?logo=go)](https://pkg.go.dev/testing@go1.23.1) [![Source](https://img.shields.io/badge/Source-@1.23.1-00ADD8?logo=go)](https://github.com/golang/go/tree/go1.23.1/src/testing) [![Source](https://img.shields.io/badge/Source-@1.23.1.5-512BD4?logo=dotnet)](https://github.com/ritchiecarroll/go2cs/tree/nuget-1.23.1.5/src/core/testing)

Package testing provides support for automated testing of Go packages. This is the go2cs Phase-4 test host: a hand-maintained implementation of the `testing` API — `T`, `B`, `F`, `TB`, subtests, parallelism, `TempDir` with Go-faithful `os.RemoveAll` cleanup semantics, `Setenv`, package deadlines — that runs converted `_test.go` suites and compares their verdicts one-for-one against a clean `go test -json` baseline. Every validated package's proof page on [go2cs.net/validation](https://go2cs.net/validation/index.html) was produced under this host.

---

This package is a hand-maintained implementation of the Go standard library's `testing` API rather than converted Go source — hand-owning the test host is what keeps one `testing` package shared by every converted test project. The go2cs implementation is distributed under the MIT license.
