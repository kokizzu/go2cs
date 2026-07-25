package main

import (
	"fmt"
	"time"
)

// Runtime duck-typed interface satisfaction — the ONE path in the converted runtime that has no
// compile-time answer, and (until this benchmark) the only shared mechanism never executed under
// Native AOT.
//
// Go resolves interface satisfaction structurally at run time; C# resolves it nominally at compile
// time. go2cs answers most asserts nominally (a generated adapter recorded at conversion time), but
// some pairs cannot be recorded even in principle — the concrete type may live in a package
// converted AFTER the interface's own — so go2cs-gen emits two duck-typing SHELLS beside every
// eligible interface and golib's AdapterBinder builds one at run time. See
// docs/Phase4/DESIGN-named-interface-wrappers.md.
//
// The two shells have very different cost models, and this benchmark exercises BOTH in the same
// loop, deliberately:
//
//   - a VALUE-typed dynamic value (`counter`, a named int with a value-receiver method) takes the
//     reflective OBJECT shell: it holds the value as `object` and forwards through a cached
//     MethodInvoker, so it needs no generic instantiation at all. That is what makes it the tier
//     that is unconditionally available under Native AOT, where a MakeGenericType driven by a
//     run-time GetType() over a value type can only succeed for an instantiation ILC already
//     rooted — which a value-type close reached from GetType() never is.
//   - a POINTER-sourced dynamic value (`&box`, a struct with a pointer-receiver method) takes the
//     delegate-bound GENERIC shell, closed over the POINTEE: each interface method is a pre-bound
//     delegate, so a forwarded call costs a delegate invocation rather than a reflective one. The
//     pointee is a struct here, so this is also the AOT-GRACEFUL case — if ILC did not root the
//     instantiation, the binder belts to the object shell rather than to a miss, and the checksum
//     below still matches Go.
//
// An ANONYMOUS interface is used as the assert target because it reaches the shell tier by
// construction in a single package: neither compile-time recorder covers an interface literal (the
// declaration-site scan skips anonymous interfaces, and the assertion-site recorder is restricted
// to named targets), so no adapter can exist for either pair. The runtime mechanism reached is the
// SAME one a named interface uses — same emitted shells, same binder, same tier choice; only the
// conversion-time recorder gating differs, and that has no run-time cost.
//
// What the number means: one iteration is two asserts (each a memoized shell construction) plus two
// forwarded one-method calls, one per tier. It is the cost of the DUCK-TYPED FALLBACK, not of
// ordinary Go interface dispatch: an assertion the converter could record resolves through a
// generated nominal adapter and never comes near this path, which is why the ratio here is nothing
// like the other rows'. Go's side is a cached itab lookup and is essentially free, so this row
// measures how expensive it is for a managed runtime to answer a question C# has no answer for at
// all — the honest price of the capability, not a regression.
type counter int

func (c counter) Len() int { return int(c) }

type box struct{ n int }

func (b *box) Len() int { return b.n }

func run(n int) int {
	// Held as `any` so the assert below sees no concrete type at conversion time.
	values := []any{counter(3), &box{n: 5}}
	total := 0

	for i := 0; i < n; i++ {
		if v, ok := values[0].(interface{ Len() int }); ok {
			total += v.Len()
		}

		if v, ok := values[1].(interface{ Len() int }); ok {
			total += v.Len()
		}
	}

	return total
}

func main() {
	start := time.Now().UnixNano()

	total := run(5000000)

	elapsed := time.Now().UnixNano() - start
	fmt.Println("checksum:", total)
	fmt.Println("elapsed_ns:", elapsed)
}
