# go2cs converter

This directory holds the Go sources of the **go2cs converter** — the program that translates Go
source code into C#. It is written in Go and built on the official `go/ast` + `go/types`
toolchain (`golang.org/x/tools/go/packages`), so it type-checks the code it converts with the
same front end the Go compiler uses.

> The file `readme.go` here is a *component of the converter* — it emits the per-package
> `README.md` (description, license attribution, validation badges) for each converted
> standard-library package. It is not this directory's readme; you are reading that.

## Build

```bash
go build
```

The behavioral and validation harnesses rebuild the converter automatically when any `*.go`
source here is newer than the binary; `go test ./...` runs the converter's own test suite,
including the shared-project registration and hand-own routing integrity gates.

## Usage

```bash
go2cs [options] <input_dir> [output_dir]
```

See [`main.go`](main.go) for the authoritative flag set (`-stdlib`, `-recurse`, `-tests`,
`-platforms`, `-go2cspath`, …) and the repository's
[`CLAUDE.md`](../../CLAUDE.md) / [`docs/Architecture.md`](../../docs/Architecture.md) for how
each mode is used in practice.

## Layout

- `main.go` — entry point and flag handling; `stdLibConverter.go` — the standard-library
  conversion driver (dependency graph, conversion queue, multi-platform emission).
- `visit*.go` — AST statement/declaration visitors (functions, range, defer, select, …).
- `conv*.go` — expression and type conversion (calls, slices, pointers, composite literals, …).
- Analysis passes — escape analysis, variable shadowing, name collisions, generic constraints,
  imports (`*Operations.go`).
- `testConversion*.go` — the Phase-4 `-tests` pipeline: converts a package's `_test.go` suite,
  builds the runnable test host, and differentially compares results against `go test -json`.

The conversion strategy — how each Go construct maps to C# and why — is documented in
[`docs/ConversionStrategies.md`](../../docs/ConversionStrategies.md) (summary) and
[`docs/ConversionStrategies-Reference.md`](../../docs/ConversionStrategies-Reference.md)
(exhaustive reference).
