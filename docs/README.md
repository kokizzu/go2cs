![go2cs](images/go2cs-small.png)

# go2cs — Go to C# Converter

[![golib NuGet package](https://img.shields.io/nuget/dt/go.lib?label=go.lib%20NuGet%20package)
](https://www.nuget.org/packages/go.lib)

Browse all: [Go Standard Library NuGet packages](https://www.nuget.org/packages?q=go2cs%20ritchiecarroll)

---

## 📰 NEWS — Go's own standard-library test suites pass in C#

**69 of the 215 converted standard-library packages that define `Test` functions run their own Go 1.23.1
test suites in C# and agree with `go test -json` verdict for verdict — 32.1%.** That is **2,376 matching
test results**, with **48** divergences disclosed by exact failure signature rather than skipped. The set
reaches well past leaf packages: `sync`'s own concurrency suite, the full RE2 engine in `regexp`,
`strconv`'s float formatting at full precision, and the reflection-driven `errors`, `encoding/binary` and
`go/token` — the last round-tripping a `FileSet` through the real converted `encoding/gob` engines.
Per-package counts, and a [one-command reproduction](#try-it-yourself--validate-a-converted-test-suite)
from a clone, are in the [validated-package table](ValidatedTestPackages.md).

**➡ All announcements can be found in the [go2cs News Archive](NEWS.md).**

## go2cs Purpose

Convert source code written in the [Go programming language](https://golang.org/ref/spec) into
[C#](https://learn.microsoft.com/dotnet/csharp/). The generated C# is designed to be both *behaviorally*
and *visually* similar to the original Go — so a Go developer can read the converted code and follow it
easily, and a .NET developer can use Go code directly within the .NET ecosystem.

* Browse transpiled code: [Converted Go Standard Library](https://github.com/ritchiecarroll/go2cs/tree/master/src/core)
* Explore Go and generated C# side by side: [Tour of go2cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/tour/README.md)
* Learn how it works: [Go to C# Conversion Strategies](ConversionStrategies.md)
* Walk through an example: [Converting a real-world module](#converting-a-real-world-module)
* Compile in Visual Studio: [Go Standard Library Solution](https://github.com/ritchiecarroll/go2cs/blob/master/src/go2cs-stdlib.slnx)
* Run converted Go test validation: [Try it yourself](#try-it-yourself--validate-a-converted-test-suite)
* Track which stdlib test suites pass in C#: [Validated Test Packages](ValidatedTestPackages.md)
* View example converted test: [`utf8_test.cs`](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/unicode/utf8/utf8_test.cs)
* See current project [status](#status) and [milestones](#milestones)

[![Tour of go2cs showing Go and generated C# side by side](images/tour-of-go2cs.png)](images/tour-of-go2cs.png)

### Frequently asked questions

* Why is a Go to C# transpiler needed? _[Integration opportunities](Background.md#background)._
* Won't converted C# code be slower? _[Yes, slower is expected](#performance)._

## Transpiler Goals

Go leans on its compiler and runtime for slices, maps, channels, goroutines, `defer`/`panic`/`recover`,
multiple returns, struct embedding and interface duck-typing. go2cs maps each onto idiomatic C#, keeping
the machinery out of sight — in a small runtime library and compile-time source generators — so the
converted code stays close to the original Go.

- **Reads like Go.** Receiver methods become extension methods, multiple returns become tuples, struct
  embedding becomes promoted fields — the shape of the code is preserved.
- **Runs like Go.** Conversions prioritize behavioral equivalence first (e.g. a `goroutine` runs on the
  thread pool rather than being rewritten into `async`).
- **Managed first.** Output targets portable managed C#; native interop is a last resort, not the default.

## Example

Given this Go:

```go
type Person struct {
    name string
    age  int32
}

func (p Person) IsAdult() bool {
    return p.age >= 18
}
```

go2cs produces this C#:

```csharp
[GoType] partial struct Person {
    internal @string name;
    internal int32 age;
}

public static bool IsAdult(this Person p) {
    return p.age >= 18;
}
```

### Real standard-library conversions, side by side

The goal — *reads like Go* — is easiest to judge on real code. Below are converted standard-library files
next to their original **Go 1.23.1** source, in order of increasing richness:

| Package | Go 1.23.1 source | Converted C# | What it shows |
|:--|:--|:--|:--|
| `errors` | [errors.go](https://github.com/golang/go/blob/go1.23.1/src/errors/errors.go) | [errors.cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/errors/errors.cs) | Error values and an unexported type satisfying the `error` interface. |
| `cmp` | [cmp.go](https://github.com/golang/go/blob/go1.23.1/src/cmp/cmp.go) | [cmp.cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/cmp/cmp.cs) | Generics with an ordered-type constraint. |
| `unicode/utf8` | [utf8.go](https://github.com/golang/go/blob/go1.23.1/src/unicode/utf8/utf8.go) | [utf8.cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/unicode/utf8/utf8.cs) | Constants keeping Go's hex/binary literal formatting; arrays and structs. |
| `sort` | [search.go](https://github.com/golang/go/blob/go1.23.1/src/sort/search.go) | [search.cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/sort/search.cs) | Binary search driven by a `func(int) bool` closure. |
| `strings` | [reader.go](https://github.com/golang/go/blob/go1.23.1/src/strings/reader.go) | [reader.cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/strings/reader.cs) | A struct with receiver methods, tuple returns, and interface implementation. |
| `container/list` | [list.go](https://github.com/golang/go/blob/go1.23.1/src/container/list/list.go) | [list.cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/container/list/list.cs) | A doubly-linked list — pointers and receiver methods. |

Browse the whole set under [`src/core`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core).

## Features

go2cs converts the full Go language surface — the same converter that emits the 302 packages above:

**Types & values**

- Slices and arrays (backing-array aliasing preserved, as in Go), maps, and UTF-8-backed `@string`
- `int` / `uint` as platform-width native integers; named numeric types and untyped-constant semantics
- Constants and `iota`, preserving Go's numeric literal formatting (hex, binary, underscores, exponents)
- Pointers with automatic heap-boxing driven by escape analysis; `nil`; `unsafe.Pointer`
- Type definitions and aliases — including exported aliases that resolve across assembly boundaries

**Functions & methods**

- Multiple return values and named results, as tuples
- Receiver methods as extension methods, with distinct pointer- and value-receiver overloads
- Function values, closures (honoring Go's shared-storage capture semantics), and variadic functions
- `defer` / `panic` / `recover`, including named results observed and mutated by deferred closures

**Concurrency**

- Goroutines, run on the thread pool (behavioral equivalence first — not rewritten into `async`)
- Channels with channel-operator (`<-`) lowering, and `select`-statement lowering

**Composition & polymorphism**

- Struct embedding with promoted fields and methods (multi-hop and cross-package)
- Interfaces, satisfied structurally in Go and realized as nominal C# glue via Roslyn source generators
- Type assertions and type switches
- Generics: type parameters and constraints (unions, `comparable`, `~`-underlying, method sets)

**Control flow & packaging**

- `for` / `range`, labeled `break` / `continue`, expression and type switches, Go 1.22 loop variables
- The built-ins: `append`, `len`, `cap`, `make`, `new`, `copy`, `delete`, `close`, …
- Packages mapped to namespaces; cross-package imports compiled as separate, referenced assemblies
- Build-tag and `GOOS` / `GOARCH` platform file selection, and deterministic, byte-stable output

See [`ConversionStrategies.md`](ConversionStrategies.md) for an example-driven tour of how each construct
maps to C# (with [`ConversionStrategies-Reference.md`](ConversionStrategies-Reference.md) for the full detail).

![GopherDotNetBotFrisbee](images/GopherDotNetBotFrisbee.png)

## Requirements

- **[.NET 9.0 SDK](https://dotnet.microsoft.com/download)** — to build and run the converted C#.
- **[Go 1.23+](https://go.dev/dl/)** — the converter is a Go program, and it uses the Go toolchain to load
  and type-check the source being converted. Make sure your Go environment is set up (`GOROOT`/`GOPATH`)
  and the source you want to convert already builds with `go build`.

## Installing the converter

Build the `go2cs` executable from source and place it on your `PATH`. The simplest way is `go install`,
which compiles it and drops the binary into `%GOBIN%` (or `%GOPATH%\bin`) — already on your `PATH` in a
standard Go setup — in one step:

```shell
cd src/go2cs
go install .
```

Go produces a self-contained native binary. To target another platform, use Go's standard cross-compilation
(`GOOS`/`GOARCH`).

## Usage

```shell
go2cs [options] <input_dir> [output_dir]
```

Examples:

```shell
go2cs example.go                       # convert a single file
go2cs package_dir                      # convert a package
go2cs -indent 2 -var=false example.go conv/example.cs
go2cs -stdlib                          # convert the entire Go standard library
go2cs -stdlib fmt strings io           # convert specific standard library packages
go2cs -recurse=nuget module_dir out    # convert a module + its third-party deps, stdlib from NuGet
go2cs -recurse module_dir              # same, referencing a locally-staged standard library
go2cs -recurse module_dir output_root  # ...with the generated app/dep trees under output_root
go2cs -recurse=module module_dir out   # convert only the module's own packages (deps referenced, not converted)
go2cs -tests package_dir               # convert a package plus its Go test suite
go2cs -tests -test-action all goroot_pkg_dir converted_pkg_dir   # ...and build, run, and diff vs go test
```

### Common options

| Option | Description |
|:--|:--|
| `-stdlib` | Convert the Go standard library (optionally followed by specific package names). |
| `-recurse` | Recursively convert a downloaded module **and its third-party dependencies** in dependency order, referencing (not reconverting) the pre-converted standard library through `$(go2csPath)`. An optional second positional output root isolates the generated `src\` app and `pkg\` dependency trees; references inside that graph are relative. A package that fails to load or convert is reported and skipped, and the run continues with the rest. See [Converting a real-world module](#converting-a-real-world-module). |
| `-recurse=module` | Same recursion, narrower **scope**: convert the input module's own packages (every package under its `go.mod`, in dependency order) and **stop there** — each third-party package is still referenced into the `pkg\` tree, but none of them is converted, so a dependency closure that go2cs cannot yet convert can no longer hold up the module's own code. The referenced-but-unconverted packages are listed at the end of the run; converting them into the same output root later resolves those references. See [Converting a real-world module](#converting-a-real-world-module). |
| `-recurse=nuget` | Same, but the standard library, the `golib` runtime and the analyzer come from NuGet — [`go.<pkg>`](https://www.nuget.org/packages?q=go2cs%20ritchiecarroll) + [`go.lib`](https://www.nuget.org/packages/go.lib) + [`go.gen`](https://www.nuget.org/packages/go.gen) — so nothing is staged locally. The app's own and third-party converted packages stay project references. A reference style and a scope are independent, so the values combine: `-recurse=module,nuget`. |
| `-tests` | Also convert the package's eligible `_test.go` suite and emit a runnable C# test-host project (default off; cannot be combined with `-recurse`). Forces `-comments` on and self-locates `$(go2csPath)` by walking up from the output directory, so the two-argument form works from a bare clone with no flags or environment setup. See [Try it yourself](#try-it-yourself--validate-a-converted-test-suite). |
| `-test-action <action>` | With `-tests`: one of `convert` (default), `build`, `run`, `compare`, or `all`. `convert` and `all` convert the package and its tests; `build` / `run` / `compare` act on the **existing** converted artifacts — validated against the test manifest's recorded input digest — without reconverting. `compare` (and `all`) runs both `go test -json -count=1` and the converted C# test host and diffs the terminal results by test name. |
| `-test-timeout <duration>` | Package deadline for a converted-test action, in Go duration syntax (default `2m`). For `run`/`compare` it is handed to **both** sides — `go test -timeout` and the converted host's own `-timeout` — so they agree. A suite that legitimately runs long needs a value above both defaults: `hash/maphash` takes ~15 minutes in C# where Go's takes 7.6 seconds, so it is validated with `-test-timeout 30m`. |
| `-go2cspath <dir>` | Runtime/stdlib root (env `GO2CSPATH`; default `~/go2cs`) used by generated `$(go2csPath)…` references, and the output root for `-stdlib`. For a single-package/file conversion, C# output goes to optional `[output_dir]` (in place by default). |
| `-goroot` / `-gopath` | Override the detected Go root / path. |
| `-platforms <os/arch>` | Target platform for build-tagged files (defaults to the host). |
| `-tags <list>` | Build tags applied when loading packages. `-stdlib` and `-tests` apply `purego` by default; an explicit value replaces it. |
| `-indent <n>` | Spaces per indent level (default 4). |
| `-var` | Prefer `var` declarations where the type is obvious (default on). |
| `-uco` | Emit channel operators instead of method calls (default on). |
| `-comments` | Carry source comments into the output (best effort, see [go/ast comment status](https://github.com/golang/go/issues/20744)). |
| `-csproj <file>` | Generate project files from a custom `.csproj` template instead of the embedded one. |
| `-tree` | Print each file's Go parse tree (`go/ast`) to stdout during conversion — a diagnostic aid. |
| `-debug` | Disable the converter's per-file panic recovery, so a conversion failure crashes with a full stack trace instead of being reported as a warning. |
| ~~`-cgo`~~ | ~~Also convert cgo-targeted files.~~ |

All converted C# code references a hand-written runtime library (`golib`, published as the [`go.lib`](https://www.nuget.org/packages/go.lib)
NuGet package) plus a set of Roslyn source generators that supply Go semantics at compile time (published as
[`go.gen`](https://www.nuget.org/packages/go.gen)). A `-recurse=nuget` conversion wires both up for you.

### Converting a real-world module

The `-recurse` option converts a **whole downloaded application together with every third-party dependency
package** in its transitive import closure — in dependency order (least-dependencies-first) — while
**referencing** (not reconverting) the pre-converted standard library. The result is a C# solution you can
open and build. With `-recurse=nuget` the standard library, the `golib` runtime and the `go2cs-gen`
analyzer come from [nuget.org](https://www.nuget.org/packages?q=go2cs%20ritchiecarroll), so nothing has to
be staged on the machine beforehand. (Prefer the standard library as local C# source? See
[building against a local standard library](#optional-build-against-a-local-standard-library) below.)

Here is the full round-trip for a small CLI that uses [`github.com/fatih/color`](https://github.com/fatih/color),
which itself pulls in `github.com/mattn/go-colorable`, `github.com/mattn/go-isatty`, and `golang.org/x/sys` —
a genuine dependency graph:

> **NOTE:** _these steps are tested on Windows only — they assume a `cmd.exe`-type shell._

**1 — Go: get the app and confirm it builds as Go.**

```bat
mkdir colordemo && cd colordemo
go mod init example.com/colordemo
```

Create `main.go` (`go mod tidy` needs a real source file — with none present it reports
`warning: "all" matched no packages`):

```go
package main

import "github.com/fatih/color"

func main() {
	color.New(color.FgGreen, color.Bold).Println("hello from fatih/color")
}
```

Next, pin the app to a **Go 1.23-compatible** dependency set and confirm it builds as Go.

> **NOTE:** _go2cs is built with **Go 1.23**, so its type-checker only reads modules whose `go` directive — and their dependencies' — is **≤ 1.23**. `fatih/color` v1.19+ and current `golang.org/x/sys` require Go 1.25, which would fail step 2 with_ `package requires newer Go version go1.25`_; pin as shown._

```bat
set GOTOOLCHAIN=local
go get github.com/fatih/color@v1.18.0     & :: a Go 1.23-era release (v1.19+ requires Go 1.25)
go mod tidy                               & :: download color + its (Go 1.23-era) dependencies
go build ./...                            & :: baseline: confirm it compiles as Go first
```

**2 — go2cs: recurse-convert the app.** `go2cs` is the converter you put on your `PATH` in *Installing the
converter* above, so it runs from anywhere. Point it at the **app** directory, and give it an output root
to write the generated C# into:

```bat
cd path\to\colordemo
go2cs -recurse=nuget . csharp
```

`go2cs` discovers the imports and converts each package, least-dependencies-first
(`go-isatty` and `x/sys` → `go-colorable` → `color` → the app), into a parallel tree under `csharp\`,
leaving your original Go source untouched. The converted app lands under `csharp\src\<import-path>`, and
every third-party library under `csharp\pkg\<import-path>`. The standard library is referenced as
`go.<pkg>` packages, and the generated `csharp\Directory.Build.props` supplies the version they resolve —
so the projects restore and build with no further configuration. A per-project `.slnx` sits next to every
generated `.csproj`, each with that project plus its converted dependencies.

_Code converted from `main.go` should look like the following in `main.cs`:_
```c#
namespace go.example.com;

using color = github.com.fatih.color_package;
using github.com.fatih;

partial class main_package {

internal static void Main() {
    color.New(color.FgGreen, color.Bold).Println("hello from fatih/color");
}

} // end main_package
```

**3 — C#: build the generated solution.** The app's per-project `.slnx` builds the app and its whole
converted dependency tree, restoring the go2cs packages on the way; opening it in Visual Studio makes the
app the startup project (F5 runs it):

```bat
cd "csharp\src\example.com\colordemo\"
dotnet build example.com.colordemo.slnx -c Debug
```

**4 — C#: run the converted app.** Navigate into the default .NET 9.0 debug build folder, and run demo:
```bat
cd "bin\Debug\net9.0\"
colordemo.exe
```
_Expected output:_

![colorapp-output](images/colorapp-output.png)

> **NOTE:** this `fatih/color` example **compiles clean** — app plus all four dependency projects — **and runs**. Bigger programs are a deeper milestone: the referenced standard library compiles in full, and making it **operational** package by package is the [Phase-4](Roadmap.md#phase-4--convert-and-run-go-package-tests) work that [Validated Test Packages](ValidatedTestPackages.md) tracks.

#### Optional: convert the module only, and deal with its dependencies later

A dependency closure is not always convertible today — a large third-party SDK can hit a converter defect,
or pull in a package go2cs cannot yet handle — and under plain `-recurse` that blocks the packages you
actually came for. `-recurse=module` narrows the **scope** to the input module's own packages:

```bat
cd path\to\myapp
go2cs -recurse=module . csharp
```

Every package under the module's own `go.mod` converts, in dependency order, exactly as it would under the
full `-recurse`; every third-party package is *referenced* — into `csharp\pkg\<import-path>`, the same place
the full run would have converted it — but never converted, so nothing about it can fail the run. The
converter prints the referenced-but-unconverted list when it finishes:

```text
Closure: 214 packages discovered — converting 9 app, referencing 118 third-party + 87 stdlib (0 skipped)
...
Third-party packages referenced but NOT converted (-recurse=module): 118
  google.golang.org/api/googleapi
  ...
```

Those references are unresolved until something is written at those paths, so the generated solution does
**not** build yet — the mode's deliberate trade. Re-running the same conversion **without** `=module` (once
the dependencies convert) writes them at exactly those paths and resolves the references; the app's own
converted `.cs` and `.csproj` come out byte-identical either way, so nothing you have done to them is lost.

#### Optional: build against a local standard library

Some work wants the standard library on disk as C# source instead — to step into it in the debugger, or to
change it and rebuild. `deploy-core` is a build script in the go2cs repo's **`src/`** folder (it is *not* on
your `PATH`), so run it from there. It stages the standard library, runtime and analyzer at
`%GOPATH%\src\go2cs` — the "deploy root" a converted project resolves through `$(go2csPath)`:

```bat
cd path\to\go2cs\src
deploy-core
```

Staging is **per-machine**, not per-app; redo it when you pull a new go2cs version. Then convert with plain
`-recurse`, pointing at the deploy root:

```bat
cd path\to\colordemo
go2cs -recurse . -go2cspath %GOPATH%\src\go2cs
```

The converted app lands under `%GOPATH%\src\go2cs\src\<import-path>` and its converted third-party
dependencies under `%GOPATH%\src\go2cs\pkg\<import-path>`, with the standard library referenced at
`%GOPATH%\src\go2cs\core\`; build and run it exactly as in steps 3 and 4 from there. The converted C# is
the same either way — only the reference style in the generated projects differs.

## Project layout

| Path | Contents |
|:--|:--|
| `src/go2cs/` | The converter (written in Go, using `go/ast` + `go/types`). |
| `src/core/` | The converted Go standard library — 302 packages, with `unsafe` and `testing` hand-written rather than converted. Everything (tests, tour, NuGet) builds against this one tree. |
| `src/core/golib/` | The C# runtime library (`slice`, `map`, `channel`, `@string`, built-ins, type aliases). |
| `src/core/go2cs/` | Shared `Symbols` project — the canonical marker glyphs used by the runtime and the generators. |
| `src/gen/go2cs-gen/` | Roslyn source generators (interface implementation, receiver overloads, struct embedding). |
| `src/tour/` | [Tour of go2cs](https://github.com/ritchiecarroll/go2cs/blob/master/src/tour/README.md) — the Tour of Go beside a live Go-to-C# workspace. |
| `src/tests/Behavioral/` | Per-feature Go↔C# equivalence tests (transpile, compile, run-and-compare). |
| `src/tests/Performance/` | Go vs transpiled C# runtime benchmarks (JIT and Native AOT) — see the [performance comparison](Performance.md) for current numbers. |

Contributors: see [`CLAUDE.md`](../CLAUDE.md) for an architecture overview and
[`Architecture.md`](Architecture.md), [`ConversionStrategies.md`](ConversionStrategies.md), and
[`Roadmap.md`](Roadmap.md) for details. There's lots of low hanging fruit to be had here, jump in if you'd like to help...

## Status

The converter builds idiomatic C# for the full range of Go language features, gated by 519 Go-vs-C#
behavioral regression projects — each transpiled, compiled, byte-compared against a committed golden and,
where it is a runnable program, executed with its stdout compared against the Go original's. The entire Go
standard library (302 packages, Go 1.23.1) compiles cleanly as .NET assemblies.

The converted standard library reproduces **Go built with `-tags purego`** — a managed runtime cannot
execute Go's hand-written `.s` assembly, so the portable pure-Go variants of the asm-backed crypto and hash
functions are the faithful target (`-stdlib` and `-tests` apply the tag by default; see
[Conversion Strategies](ConversionStrategies.md#the-standard-library-reproduces-go--tags-purego)).

Compiling is not runtime parity. Making the library **operational** is the ongoing
[Phase 4](Roadmap.md#phase-4--convert-and-run-go-package-tests) work: each package's own `_test.go` suite
is converted to C#, built against the converted standard library, run under a Go-semantics test host, and
compared verdict-for-verdict against a clean `go test -json` baseline.
[Validated Test Packages](ValidatedTestPackages.md) tracks the set — reproducible via
[Try it yourself](#try-it-yourself--validate-a-converted-test-suite).

### Try it yourself — validate a converted test suite

Every validated package ships its **converted C# test sources** next to the production code under
[`src/core`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core) (for example,
[`unicode/utf8/utf8_test.cs`](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/unicode/utf8/utf8_test.cs)),
so you can read the exact C# that runs — and re-run the validation yourself. You need
**[Go 1.23.1](https://go.dev/dl/)** (for the reference `go test` run), the
**[.NET 9 SDK](https://dotnet.microsoft.com/download)**, and `go2cs` on your `PATH` (see
[installing the converter](#installing-the-converter)):

```sh
# 1. Convert unicode/utf8's test suite, build + run the C# host, and diff it against `go test`.
#    The second argument is the package's home in the converted tree; the converter locates the
#    runtime and its stdlib dependencies from there — no flags or environment setup required.
#    (On Windows, Go's source lives under "C:\Program Files\Go\src"; elsewhere use "$(go env GOROOT)/src".)
go2cs.exe -tests -test-action all \
    "C:\Program Files\Go\src\unicode\utf8" \
    src/core/unicode/utf8
```

Expected final line:

```text
Validated 14 tests against go test (0 skipped identically on both sides, 37 disclosed-unsupported declarations excluded).
```

The command converts the `_test.go` files to C#, generates a test host, builds it against the converted
standard library, runs it in an isolated process, captures a clean `go test -json -count=1` baseline, and
compares terminal results by full Go test name — reporting `validated` only when every test agrees on both
sides and every unsupported declaration (benchmarks, examples) is accounted for. It regenerates the local
converted `.cs` in place; the Go source copies and run manifests it stages are git-ignored. The same
command validates every other package on the table — substitute its GOROOT source path and its
`src/core/<pkg>` path in the two arguments.

A few packages carry a **disclosed divergence**: a Go test asserting something a managed runtime provably
cannot satisfy — an exact allocation count (Go's `testing.AllocsPerRun`, reached through compiler escape
analysis), or a collectibility check Go answers from per-safepoint liveness maps. Rather than skip those
tests, each affected package pins the divergence in a hand-owned, committed
[`go2cs_test_disclosures.json`](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/bytes/go2cs_test_disclosures.json)
that the differential oracle matches by *exact failure signature* — any other failure is still a hard
mismatch — and reports as **disclosed-divergent** in the summary. Packages without a manifest compare
strictly.

### Performance

_Everyone asks:_ how fast is the transpiled C# compared to the original Go — including startup time,
memory, and Native AOT builds? See the [performance comparison](Performance.md) — **`TL;DR`**: _not as fast
as native Go, [nor is that an expected outcome](Background.md#converted-code)._ Save for initial work on a
[ref struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct)
based [stack string](ConversionStrategies.md#strings-string-and-sstring) and
[stack slice](ConversionStrategies.md#slices-and-arrays), optimization is targeted for _after_ Phase 4.

Newer Go and .NET versions are planned; a validated baseline comes first.

## Milestones

High level timeline of the project's major turning points.

| Date | Milestone | Commit / Tag | Notes |
|:--|:--|:--|:--|
| 2018-05-21 | Project inception | `929d1457f` | A C#/.NET converter built on an ANTLR4 Go grammar with T4 templates. |
| 2020-07-09 | Runtime library + hand-converted stub | `9792eeea2` | The `golib` Go-semantics runtime and a curated hand-finished stdlib stub. |
| 2022-03-13 | [`v0.1.2` release](NEWS.md#march-13-2022--v012-release) | [`v0.1.2`](https://github.com/ritchiecarroll/go2cs/releases/tag/v0.1.2) | Tagged release of the mature ANTLR4-era converter. |
| 2025-01-12 | [Rewrite as "go2cs2" — Go-based converter](NEWS.md#january-12-2025--the-converter-is-rewritten-in-go-go2cs-version-2) | `87465f5f5` | Converter re-implemented in Go on `go/ast` + `go/types`; Roslyn source generators supply ancillary Go semantics. |
| 2025-05-05 | [First full standard-library auto-conversion](NEWS.md#may-5-2025--first-full-standard-library-auto-conversion) | `6ca1c45b7` · [`full-conversion-2025-05`](https://github.com/ritchiecarroll/go2cs/releases/tag/full-conversion-2025-05) (`cc14584c7`) | Every Go file got a C# file — the transpiler did not crash; it did not mean the emitted C# compiled. |
| 2026-06-25 | Baseline ↔ full-conversion separation | `3c8b3a848` | A compiling curated baseline and the WIP full conversion split apart, restoring a green build and the converter-improvement loop. |
| 2026-06-26 | First full-conversion package promoted | `05a53e8c0` | `sync/atomic` migrated into the baseline (`atomic.Pointer[T]` backed by a managed slot). |
| 2026-06-27 | [`math` package compiles clean](NEWS.md#june-27-2026--the-math-package-compiles-clean) | [`math-green-2026-06-27`](https://github.com/ritchiecarroll/go2cs/releases/tag/math-green-2026-06-27) (`914d4bd72`) | Nine packages greened via 19 behaviorally-tested converter fixes, including widely-imported `math`. |
| 2026-07-10 | [**First clean full-standard-library compile**](NEWS.md#july-10-2026--the-entire-go-standard-library-compiles-in-net) | `51ba5d9cf` · [`stdlib-green-2026-07-10`](https://github.com/ritchiecarroll/go2cs/releases/tag/stdlib-green-2026-07-10) | All **302** packages (Go 1.23.1) compile with zero errors — `runtime`, `reflect`, `net/http`, `go/types`, `crypto/tls` included ([details](StdLibCompileMilestone.md)). |
| 2026-07-14 | [Standard library on NuGet + NuGet-referencing conversion](NEWS.md#july-14-2026--the-converted-go-standard-library-is-on-nuget) | `2363af0e6` · `dd821a556` · [`nuget-stdlib-2026-07-14`](https://github.com/ritchiecarroll/go2cs/releases/tag/nuget-stdlib-2026-07-14) | `go.<pkg>` / `go.lib` / `go.gen` published to nuget.org; `-recurse=nuget` emits matching `<PackageReference>` entries, so a converted app needs no local go2cs checkout. |
| 2026-07-17 | [**First Go standard-library test suite passing in C#**](NEWS.md#july-17-2026--gos-own-tests-now-pass-in-c) | `337a928df` · [`utf8-tests-green-2026-07-17`](https://github.com/ritchiecarroll/go2cs/releases/tag/utf8-tests-green-2026-07-17) | `unicode/utf8` validates **14/14 against `go test -json`** under the hand-owned `go.testing` host; the differential pipeline goes live end to end. |
| 2026-07-18 | [**Phase-4 test suites expand — disclosed-divergence mechanism**](NEWS.md#july-18-2026--bytes-and-strings-tests-pass-with-disclosed-divergence) | `40f39d2be` · [`bytes-strings-tests-green-2026-07-18`](https://github.com/ritchiecarroll/go2cs/releases/tag/bytes-strings-tests-green-2026-07-18) · [`sort`](https://github.com/ritchiecarroll/go2cs/releases/tag/sort-tests-green-2026-07-18) · [`utf16`](https://github.com/ritchiecarroll/go2cs/releases/tag/utf16-tests-green-2026-07-18) | `bytes` (81), `strings` (68), `sort` (63) and `unicode/utf16` (8) validate, introducing the committed per-package disclosure manifest matched by exact failure signature. |
| 2026-07-26 | [**More than a quarter of the standard library's test suites pass in C#**](NEWS.md#july-26-2026--more-than-a-quarter-of-the-standard-librarys-test-suites-pass-in-c) | `44fcc4f04` | The validated set moves past leaf packages into `sync`, `regexp`/`regexp/syntax`, `strconv`, `bufio`, `compress/gzip`, the `crypto/sha*` family and the reflection-driven `errors` / `encoding/binary` / `go/token`. |
| 2026-08-01 | One tree — the converted standard library comes home to `src/core` | `2e8066da6` | Baseline and full conversion consolidate into a single tree with one `$(go2csPath)core\<pkg>` path scheme; no reference rewriting anywhere. |

## C# to Go?

A full code-based conversion from C# to Go is not offered (it would require so many restrictions as to be
impractical). To call compiled .NET code *from* Go instead, see
[go-dotnet](https://github.com/matiasinsaurralde/go-dotnet) (CLR hosting for .NET Core) or
[embedding Mono via cgo](https://www.mono-project.com/docs/advanced/embedding/) for traditional .NET.

## License

go2cs is licensed under the [MIT License](https://opensource.org/licenses/MIT). See the `LICENSE` and
`NOTICE` files. For more background, see [`Background.md`](Background.md).
