package main

import "fmt"

type snapshot struct{ addr int }

// `trace` is BOTH a package-level GLOBAL var and a method name → collision-renamed `Δtrace` (the exact
// runtime traceallocfree shape: global `var trace struct{…}` vs the traceLocker method set).
var trace = snapshot{addr: 42}

type acquirer struct{ n int }

func (acquirer) trace() int { return 1 } // method named `trace` (collides with the global var `trace`)

// collisionGlobalShadow reads the GLOBAL `trace` before declaring a same-named LOCAL — runtime
// traceallocfree's `traceSnapshotMemory` (`trace.minPageHeapAddr` read, then `trace := traceAcquire()`).
//
//go:noinline
func collisionGlobalShadow() int {
	a := trace.addr // reads GLOBAL trace.addr (42) BEFORE the local trace decl
	trace := 7      // local `trace` shadows the global (both C# `Δtrace`)
	return a + trace // 42 + 7 = 49
}

var plainCounter = 100 // a plain global (no collision rename)

// plainGlobalShadow is the same shape without a collision rename — the global-vs-local shadow alone
// still produces CS0841 in C# (function-scoped locals).
//
//go:noinline
func plainGlobalShadow() int {
	x := plainCounter * 2 // reads GLOBAL plainCounter (200) before the local decl
	plainCounter := 5     // local shadows the global
	return x + plainCounter // 200 + 5 = 205
}

//go:noinline
func acquire(n int) int { return n * 10 }

// nestedBlockShadow mirrors runtime procresize: a FUNCTION-LEVEL `trace` local, then inside an
// else-block an inner if declares its own `trace` shadow, and — AFTER that inner if closes — a
// SIBLING `trace :=` in the same else block. The sibling declaration follows a CLOSED nested
// block: the shared block-tracker flag used to be cleared by the inner block's exit, so the
// sibling skipped the outer-scope shadow check and kept its name — both emitted `Δtrace`
// (CS0136 against the function-level decl).
//
//go:noinline
func nestedBlockShadow(kind int) int {
	total := 0
	trace := acquire(1) // function-level local (emits Δtrace — the global collision rename)
	total += trace
	if kind == 0 {
		total = -1
	} else {
		if kind > 1 {
			trace := acquire(2) // inner-if shadow (renamed)
			total += trace
		}
		trace := acquire(3) // sibling AFTER the closed inner if — the procresize shape
		total += trace
	}
	total += trace // binds the function-level local again
	return total
}

// hosts mirrors net hosts.go's cache global: a package var whose name a LOCAL shadows in
// the local's OWN tuple initializer — `if hosts, ok := hosts.byAddr[addr]; ok` — where Go
// resolves the initializer's `hosts` to the package var (the local isn't in scope yet) but
// C#'s whole-block scoping bound it to the not-yet-declared local (CS0841/CS8130 ×2). The
// local is shadow-renamed (`hostsΔ1`); the package var keeps the simple name.
var hosts = map[string][]string{"a": {"x", "y"}}

//go:noinline
func tupleInitShadow(key string) int {
	if hosts, ok := hosts[key]; ok {
		return len(hosts)
	}
	return -1
}

// mlkemQ is the package-level CONST half of the same collision — crypto/internal/mlkem768's
// `const q = 3329` against `q := big.NewInt(q)` in TestZetas/TestGammas. Go resolves the
// initializer to the constant (a short var decl's scope begins after its own ValueSpec); C#
// scopes the local to the whole block, so the initializer bound the local it was declaring
// (CS0841 ×2). A const cannot take the LOCAL-rename half of the defence `hosts` above uses:
// the pre-scan feeding it records only objects that are *types.Var, so a const-shadowing local
// is never renamed. The CONST reference is package-qualified instead (`main_package.mlkemQ`)
// and the local keeps its own name.
const mlkemQ = 3329

//go:noinline
func constSelfInitShadow() int {
	mlkemQ := mlkemQ * 2 // initializer reads the package CONST (6658); the local shadows it
	return mlkemQ + 1    // 6659
}

// constNestedInitShadow puts the same shape in an `if` INIT — not a declaration directly in the
// function body, so the function-level map the var arm consults does not see it. The const arm
// consults every variable declared anywhere in the function instead, which is what reaches here.
//
//go:noinline
func constNestedInitShadow() int {
	if mlkemQ := mlkemQ / 1000; mlkemQ > 2 {
		return mlkemQ // 3
	}
	return -1
}

type qbox struct{ v int }

//go:noinline
func newQbox(n int) *qbox { return &qbox{v: n} }

// constVarInitShadow is mlkem768's own `q := big.NewInt(q)` letter for letter: the local's type
// comes from a call, so the declaration emits `var`, and it is `var` that produces the CS0841 the
// package reported. The two shapes above declare an explicit C# type and surface the same defect
// as CS0165 ("use of unassigned local variable") instead — one root, two diagnostics, so the guard
// carries both.
//
//go:noinline
func constVarInitShadow() int {
	mlkemQ := newQbox(mlkemQ) // initializer reads the package CONST; the local shadows it
	return mlkemQ.v           // 3329
}

// constUnshadowedElsewhere proves the qualification is scoped to functions that DECLARE the
// name: this one only reads the const, so it must still emit the bare `mlkemQ`.
//
//go:noinline
func constUnshadowedElsewhere() int { return mlkemQ }

func main() {
	fmt.Println(constSelfInitShadow(), constNestedInitShadow(), constVarInitShadow(), constUnshadowedElsewhere()) // 6659 3 3329 3329
	fmt.Println(collisionGlobalShadow()) // 49
	fmt.Println(plainGlobalShadow())     // 205
	fmt.Println(trace.addr, plainCounter) // 42 100 (globals unchanged)
	fmt.Println(nestedBlockShadow(2), nestedBlockShadow(1)) // 70 50
	fmt.Println(tupleInitShadow("a"), tupleInitShadow("z")) // 2 -1

	bi := buildRec{Main: "mod/a", Path: "p"}
	fmt.Println("main field:", bi.Main, mainField(bi)) // main field: mod/a mod/a
}

// buildRec mirrors runtime/debug's BuildInfo: a struct FIELD named Main. The field access
// must not take the function-name Main→ΔMain special (that reservation is for METHODS —
// C#'s entry point); the declaration is raw `Main`, so the Δ'd access was CS1061 ×2.
type buildRec struct {
	Main string
	Path string
}

func mainField(b buildRec) string { return b.Main }
