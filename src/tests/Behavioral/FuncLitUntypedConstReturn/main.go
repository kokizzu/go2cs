// FuncLitUntypedConstReturn guards a func literal with a DECLARED single numeric result whose
// return arms reference NAMED untyped constants, in natural-inference (assignment) position —
// strings/bytes TestMap's `maxRune := func(rune) rune { return unicode.MaxRune }`. The const
// reference emits as a golib Untyped* wrapper whose implicit conversions run BOTH ways with
// every numeric type, so C# either infers the wrapper delegate (Func<rune, UntypedInt> —
// CS1503 at the invariant-delegate call site) or, with mixed const/typed arms, finds no best
// common type at all (CS8917). The converter now states the declared return type explicitly
// (`var maxFn = rune (rune _) => maxRune`). A literal passed directly as a call argument is
// target-typed by the delegate parameter and stays unprefixed. Verified vs Go.
//
// EXTENDED 2026-08-15 (crypto/tls) with the LITERAL-only arm set. The comment below used to say
// literal-only arms were already concretely typed and needed no prefix; they are concretely typed,
// but at the LITERAL's natural C# type — `int` for an INT literal, `double` for a FLOAT one — which
// is the declared type only when the Go result is int32 or float64. crypto/tls TestCipherSuites'
// comparator `isBetter := func(a, b uint16) int { …; return -1; …; return +1 }` therefore inferred
// Func<ushort, ushort, int> against a Go `int` (C# nint) result, which every CALL accepted and the
// delegate-valued use rejected: `slices.IsSortedFunc(prefOrder, isBetter)`, CS1503. Both signs are
// literals here — `+1` is stripped exactly like `-1`.
package main

import "fmt"

const maxRune = '\U0010FFFF' // untyped rune; mirrors unicode.MaxRune
const runeSelf = 0x80        // untyped int; mirrors utf8.RuneSelf
const bigConst = 1 << 40     // untyped int, beyond int32

func apply(f func(rune) rune, r rune) rune { return f(r) }

func compareWith(a, b uint16, cmp func(uint16, uint16) int) int { return cmp(a, b) }

func applyInt64(f func(bool) int64, v bool) int64 { return f(v) }

func applyInt32(f func(bool) int32, v bool) int32 { return f(v) }

func applyFloat32(f func() float32) float32 { return f() }

func applyFloat64(f func() float64) float64 { return f() }

func main() {
	// Single arm returning a named untyped constant: without the explicit return type the
	// lambda infers Func<rune, UntypedInt> and the apply call rejects it (CS1503).
	maxFn := func(rune) rune { return maxRune }
	fmt.Println(apply(maxFn, 'a'))

	// Mixed arms: untyped-const refs + the typed parameter — no unique best common type (CS8917).
	encode := func(r rune) rune {
		if r == runeSelf {
			return maxRune
		}
		if r == maxRune {
			return runeSelf
		}
		return r
	}
	fmt.Println(apply(encode, runeSelf))
	fmt.Println(apply(encode, maxRune))
	fmt.Println(apply(encode, 'x'))

	// Mixed const/literal arms over a WIDER declared result (int64).
	pick := func(neg bool) int64 {
		if neg {
			return -1
		}
		return bigConst
	}
	fmt.Println(pick(false), pick(true))

	// Const-EXPRESSION arm containing a named untyped constant — bytes TestMap's
	// `invalidRune := func(r rune) rune { return utf8.MaxRune + 1 }`: the operator result
	// keeps the wrapper type, so inference fails the same way as the bare reference (CS1503).
	invalid := func(r rune) rune { return maxRune + 1 }
	fmt.Println(apply(invalid, 'c'))

	// A CHAR-literal arm is not a numeric literal for this predicate and keeps inferred typing.
	shrink := func(r rune) rune { return 'a' }
	fmt.Println(apply(shrink, maxRune))

	// The crypto/tls comparator: EVERY arm an INT literal, declared result Go `int` (C# nint).
	// Inference gives Func<ushort, ushort, int>, which the delegate-valued use rejects (CS1503).
	// The `+1` arm is signed-positive and must count as a literal like `-1` does.
	isBetter := func(a, b uint16) int {
		if a < b {
			return -1
		} else if a > b {
			return +1
		}
		return 0
	}
	fmt.Println(compareWith(3, 9, isBetter), compareWith(9, 3, isBetter), compareWith(4, 4, isBetter))

	// The same shape over a WIDER declared result: INT literals infer C# `int`, not `long`.
	scale := func(on bool) int64 {
		if on {
			return 9
		}
		return 0
	}
	fmt.Println(applyInt64(scale, true), applyInt64(scale, false))

	// FLOAT literals do NOT misinfer and must keep the plain form: the emitter already writes one
	// at the DECLARED floating width (`0.5F`), so C# infers float32 unaided and a stated return
	// type would be pure churn.
	half := func() float32 { return 0.5 }
	fmt.Println(applyFloat32(half))

	// Nor does an INT literal at a declared FLOATING result: it takes the declared width too
	// (`3D`), so this keeps the plain form as well. Together with `half` and `scale` this pins the
	// rule to the converter's real emission — only a declared INTEGER width leaves the literal
	// bare, which is why the gate is integer-only rather than "any numeric".
	whole := func() float64 { return 3 }
	fmt.Println(applyFloat64(whole))

	// Control — declared int32 IS the INT literal's natural C# type, so inference already
	// yields the right delegate and no return type is stated (no churn on this shape).
	rank := func(hi bool) int32 {
		if hi {
			return 100
		}
		return -100
	}
	fmt.Println(applyInt32(rank, true), applyInt32(rank, false))

	// Argument position is target-typed by the delegate parameter — no inference to fail, no prefix.
	fmt.Println(apply(func(rune) rune { return maxRune }, 'b'))
}
