package main

import "fmt"

// DeferFinallyLowering — capability 4's defer→finally lowering.
//
// A deferred call on a RECEIVER FIELD (`defer x.a.done()`) normally costs a ж-box per call: the
// registration has to hold the receiver, and holding `x.a` boxes it. Where the deferred call would
// run on exactly the paths a C# `finally` runs on, the converter emits it into the frame's finally
// instead and allocates nothing.
//
// The rows below split in two. The BEHAVIOR rows are compared against `go run` — they pin LIFO
// order, the panic path, and the reached-flag. The REFUSAL rows exist for the golden: each carries
// a shape the lowering must decline, and the `.cs.target` is where that refusal is asserted, since
// a refusal is invisible in stdout by construction.

type tracer struct {
	id string
}

func (t *tracer) touch() {
	fmt.Println("touch", t.id)
}

func (t *tracer) done() {
	fmt.Println("done", t.id)
}

type box struct {
	a tracer
	b tracer
}

// BEHAVIOR — LIFO. Both defers lower; the finally emits them in REVERSE source order, which is
// what Go's LIFO means for two unconditional top-level defers. Expect: done b, then done a.
func (x *box) two() {
	x.a.touch()
	x.b.touch()
	defer x.a.done()
	defer x.b.done()
	fmt.Println("body two")
}

// BEHAVIOR — the panic path. The lowered call runs BEFORE the frame re-throws, so `done a` must
// appear ahead of the recovered value. `boom` itself has no recover, so it still qualifies; the
// recover lives in the caller.
func (x *box) boom() {
	x.a.touch()
	defer x.a.done()
	panic("boom")
}

// BEHAVIOR — the reached-flag. The early return happens BEFORE the defer, so Go registers nothing
// and prints no `done`. An unflagged finally would print one.
func (x *box) early(skip bool) {
	x.a.touch()

	if skip {
		fmt.Println("early out")
		return
	}

	defer x.a.done()
	fmt.Println("body early")
}

// REFUSAL — all-or-nothing. The second defer is nested in a conditional, so NEITHER lowers and both
// keep their registration. Asserted in the golden.
func (x *box) mixed(f bool) {
	x.a.touch()
	x.b.touch()
	defer x.a.done()

	if f {
		defer x.b.done()
	}

	fmt.Println("body mixed")
}

// REFUSAL — the prefix gate, the row measured at THREE sites in Go's std. Nothing ahead of the
// defer dereferences `x.b`, so a nil receiver would panic at the defer in Go and at the FUNCTION'S
// EXIT if this were lowered — the body's output surviving into a panic reported in the wrong place.
// Registration is kept. Asserted in the golden.
func (x *box) unguarded() {
	x.a.touch()
	defer x.b.done()
	fmt.Println("body unguarded")
}

// BEHAVIOR — a nested func literal that RECOVERS but does not defer. The literal owns its own
// defer/recover scope, so it gets a frame of its own — and a frame emits the lowered calls into its
// `finally`. Nothing about the enclosing function disqualifies it (the recover is not in ITS scope,
// and the literal contributes no defer to the all-or-nothing count), so the lowering stays on and
// the literal's frame must NOT re-emit the enclosing function's calls.
//
// The failure this pins is silent and it compiles: the flag is an ordinary local the lambda can
// capture, so a leaked emission would print `done a` TWICE — once at the literal's exit and once at
// the function's. Go prints it once.
func (x *box) withLit() {
	x.a.touch()
	defer x.a.done()

	f := func() {
		if r := recover(); r != nil {
			fmt.Println("inner recovered", r)
		}
	}

	f()
	fmt.Println("body withLit")
}

// REFUSAL — the prefix is dereferenced only CONDITIONALLY before the defer. `x.a.touch()` runs
// unconditionally but says nothing about `x.b`, and the `x.b.touch()` that would is inside an `if`
// that may not execute — so it witnesses nothing, and the defer's own evaluation could still be the
// first dereference of `x.b`. FOUR sites in Go 1.23.12's std lean on exactly such a conditional
// match; the first form of the gate accepted them. Asserted in the golden.
func (x *box) condPrefix(f bool) {
	x.a.touch()

	if f {
		x.b.touch()
	}

	defer x.b.done()
	fmt.Println("body condPrefix")
}

// BEHAVIOR — B2's SECOND widening: a method on the RECEIVER ITSELF. Its registration allocates a
// delegate rather than a FieldRefBox (the receiver's box is the method's own parameter), which is
// why B refused it — and refusing it is what made all-or-nothing reject every function pairing one
// with a receiver-field defer.
func (x *box) finish() {
	fmt.Println("finish", x.a.id)
}

func (x *box) methodShape() {
	x.a.touch()
	defer x.finish()
	fmt.Println("body methodShape")
}

// BEHAVIOR — `FD.Write`'s exact shape, the row B2 exists to reach: a receiver-METHOD defer whose
// witness lives in an `if` INIT (which always executes), paired with a CONDITIONAL receiver-FIELD
// defer whose witness is its sibling inside that branch. Under B neither lowered, because the two
// failed different gates and all-or-nothing then refused the function outright.
//
// LIFO across the mixed pair is the assertion: Go registers `finish` then, if the branch is taken,
// `b.done` — so it prints `done b` before `finish`. Reverse SOURCE order gives the same, and the
// flag makes the untaken branch a no-op.
func (x *box) writeShape(isFile bool) {
	if id := x.a.id; id == "" {
		return
	}

	defer x.finish()

	if isFile {
		x.b.touch()
		defer x.b.done()
	}

	fmt.Println("body writeShape", isFile)
}

// REFUSAL — the receiver is REASSIGNED after the defer. Go binds the deferred call's receiver at
// registration, so this must report the ORIGINAL receiver's id; a lowered finally would bind at
// unwind and report the other one. Both refused and behavioral: the printed id is the assertion.
func (x *box) rebound(other *box) {
	x.a.touch()
	defer x.a.done()
	x = other
	fmt.Println("body rebound", x.a.id)
}

func main() {
	x := &box{a: tracer{id: "a"}, b: tracer{id: "b"}}

	x.two()

	func() {
		defer func() {
			if r := recover(); r != nil {
				fmt.Println("recovered", r)
			}
		}()

		x.boom()
	}()

	x.early(true)
	x.early(false)
	x.mixed(true)
	x.unguarded()
	x.condPrefix(false)
	x.methodShape()
	x.writeShape(true)
	x.writeShape(false)
	x.withLit()

	other := &box{a: tracer{id: "other-a"}, b: tracer{id: "other-b"}}
	x.rebound(other)
}
