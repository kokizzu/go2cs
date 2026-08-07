package main

import "fmt"

// Guards the LOCAL FUNCTION emission mode (localFunctionDefine / LambdaContext.localFuncName).
//
// A `name := func(…){…}` whose variable is ONLY ever CALLED emits as a C# local function
// (`ret name(params) { … }`) instead of a lambda bound to a variable. A capturing lambda allocates
// a display class AND a delegate on every evaluation of the lambda expression — 88 bytes for the
// worked case, charged per call of the enclosing function; a local function that is never converted
// to a delegate captures through a by-ref STRUCT closure and allocates nothing. That is the whole
// of `time`'s parseRFC3339 half of TestUnmarshalTextAllocations (`want 0 allocs`).
//
// The change must be SEMANTICS-PRESERVING: the struct closure shares the ONE captured variable
// exactly as the display class did, so every probe below prints what `go run` prints. The GOLDEN
// pins the emitted FORM (revert the fix and TargetComparison diverges); the OUTPUT comparison pins
// the capture semantics (get sharing wrong and it diverges too).
//
// P4, P5, P7 and P8 are the negative controls — shapes that must STAY lambdas, one per disqualifying
// reason. P6 was one too, and is not any more: see its comment.

type tally struct {
	n   int
	log string
}

// P1 — the parseRFC3339 shape: only ever called, captures and MUTATES `ok` and `calls`, and has a
// NAMED result (which keeps the body a block).
func p1() {
	ok := true
	calls := 0
	parseUint := func(s string, min, max int) (x int) {
		calls++
		for _, c := range []byte(s) {
			if c < '0' || '9' < c {
				ok = false
				return min
			}
			x = x*10 + int(c) - '0'
		}
		if x < min || max < x {
			ok = false
			return min
		}
		return x
	}
	a := parseUint("42", 0, 99)
	b := parseUint("7x", 1, 99)
	c := parseUint("98", 0, 50)
	fmt.Println("P1:", a, b, c, ok, calls)
}

// P2 — a single-return literal collapses to an EXPRESSION-BODIED local function. The write to
// `base` AFTER the declaration is the sharing probe: a snapshot would print 13/17, sharing 23/27.
func p2() {
	base := 10
	add := func(a, b int) int { return a + b + base }
	base = 20
	fmt.Println("P2:", add(1, 2), add(3, 4))
}

// P3 — a struct and an array captured and MUTATED by the local function, then read by the body.
func p3() {
	t := tally{1, "a"}
	arr := [3]int{1, 2, 3}
	bump := func(k int) {
		t.n += k
		t.log += "!"
		arr[0] += k
	}
	bump(2)
	bump(3)
	t.n++
	fmt.Println("P3:", t.n, t.log, arr)
}

// P4 control — the name is read as a VALUE (assigned to another variable), so it must stay a
// lambda: a local function has no value form, and converting one to a delegate is exactly what
// makes Roslyn fall back to a heap display class.
func p4() {
	c := 0
	next := func() int { c++; return c }
	next()
	f := next
	fmt.Println("P4:", f(), next(), c)
}

// P5 control — the name is REASSIGNED. A local function cannot be rebound.
func p5() {
	step := func(n int) int { return n + 1 }
	fmt.Println("P5a:", step(1))
	step = func(n int) int { return n + 2 }
	fmt.Println("P5b:", step(1))
}

// P6 — NO LONGER A CONTROL. The literal DEFERS and RECOVERS, and it was excluded because its body
// was emitted inside a `func((defer, recover) => …)` execution context whose cost dominated the 88
// bytes this rule removes. The ref-struct frame made that shape free — the body is an inline
// try/catch/finally beside a `GoFrame` local, which is an ordinary local of a local function — so
// the exclusion is gone and this probe now pins the POSITIVE case: a deferring, recovering,
// named-result literal that is only ever called emits as a local function carrying its own frame,
// with Go's recover-sets-the-named-result semantics intact (which is what the values check).
func p6() {
	guard := func(n int) (r int) {
		defer func() {
			if e := recover(); e != nil {
				r = -1
			}
		}()
		if n < 0 {
			panic("negative")
		}
		return n * 2
	}
	fmt.Println("P6:", guard(3), guard(-1))
}

// P7 control — recursion needs Go's two-step `var f func(…); f = func(…){…}`, which is an ASSIGN
// and not a DEFINE, so it is not this rule's shape at all.
func p7() {
	var fact func(int) int
	fact = func(n int) int {
		if n <= 1 {
			return 1
		}
		return n * fact(n-1)
	}
	fmt.Println("P7:", fact(5))
}

func apply(f func(int) int, v int) int { return f(v) }

// P8 control — the name is PASSED as an argument, a value use.
func p8() {
	m := 3
	scale := func(v int) int { return v * m }
	fmt.Println("P8:", apply(scale, 5), scale(2))
}

// P9 — NESTED only-called literals: the inner one captures a variable of the OUTER one's frame,
// which itself captures the enclosing function's, so both levels convert and both must share.
func p9() {
	total := 0
	outer := func(k int) int {
		inner := func(v int) int {
			total += v
			return total
		}
		return inner(k) + inner(k)
	}
	a := outer(2)
	b := outer(3)
	fmt.Println("P9:", a, b, total)
}

// P10 — one local function capturing nothing, one capturing, both called from a loop.
func p10() {
	pure := func(a, b int) int { return a * b }
	sum := 0
	acc := func(v int) { sum += v }
	for i := 1; i <= 3; i++ {
		acc(pure(i, i))
	}
	fmt.Println("P10:", sum)
}

func main() {
	p1()
	p2()
	p3()
	p4()
	p5()
	p6()
	p7()
	p8()
	p9()
	p10()
}
