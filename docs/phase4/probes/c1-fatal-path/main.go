// Command c1-fatal-path is the falsifier for C1's fatal-path sizing (MAILBOX, 2026-09-08).
//
// IT IS EXPECTED TO FAIL. Its whole job is to make the fatal path's death point OBSERVABLE, on
// each flavour, so the increment that severs `fatalthrow`/`fatalpanic`/`traceback` is written
// against a measurement rather than against a reading of the code.
//
// WHY THIS TRIGGER, and why NOT the one the dispatch suggested. COORD proposed reaching the fatal
// path through `sync: unlock of unlocked mutex` via `sync.Mutex.Unlock` as the cheapest route.
// MEASURED, it reaches nothing: `src/core/sync/mutex.cs` is a whole-file `[module:
// GoManualConversion]` hand-own that declares its OWN `throw` and `fatal`, each
// `=> throw new InvalidOperationException($"fatal error: {s}")`, and `:112` calls that LOCAL
// `fatal`. So an unlocked-mutex unlock raises a .NET exception and never enters runtime's
// `fatal` -> `fatalthrow` at all. That holds at master AND after C1's `01a5c803d`, which moves
// those hooks to `sync/runtime_impl.cs` but keeps the same `InvalidOperationException` shape.
//
// `runtime.SetFinalizer(nil, f)` is the trigger that DOES reach it, and it reaches it at MASTER
// with no dependency on any unlanded branch:
//
//   - Ours: `mfinal.cs:443` (master) -- `if (obj is null or NilType) @throw("runtime.SetFinalizer:
//     first argument is nil")`. `runtime` declares no local `throw` shim (checked), so that is
//     runtime's REAL `@throw` at `panic.cs:1090`, which prints and then calls
//     `fatalthrow(throwTypeRuntime)`, whose FIRST statement is the `getcallerpc()` stub.
//   - Go's oracle: `mfinal.go:438` at go1.24.13 -- `throw("runtime.SetFinalizer: first argument is
//     nil")`, i.e. a genuine fatal, exit 2. The message is present in BOTH, which is why this
//     message was chosen over "second argument is not a function" (absent from Go 1.24.13's source).
//
// The markers go through `fmt`, NOT through `println`. That is deliberate: Go's `println` builtin
// lowers into runtime's own print path (`gwrite` -> `writeErr` -> `write` -> `write1`), which is
// the very path predicted to be dead on linux -- using it for the markers would conflate the
// probe's own output with the thing under test.
package main

import (
	"fmt"
	"runtime"
)

func main() {
	fmt.Println("PROBE-MARK-1: reached main")

	// Expected to be fatal on every flavour, in Go and in the conversion alike.
	runtime.SetFinalizer(nil, func(p *int) {})

	fmt.Println("PROBE-MARK-2: MUST NOT BE REACHED")
}
