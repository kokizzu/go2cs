// Guards Go's `defer f(g())` / `go f(g())` shape, where g returns MULTIPLE values that
// spread into f's parameter list. Go evaluates g() ONCE, at the defer/go STATEMENT, and
// its results become the deferred call's arguments; f itself runs at unwind (defer) or on
// the new goroutine (go). C# has no splat, so the emission must hoist g()'s results into
// statement-time temps and pass them expanded.
package main

import "fmt"

var counter int

var done = make(chan bool)

// two is the canonical multi-value source: it announces its own evaluation so the
// statement-time-vs-run-time ordering is visible in the output.
func two() (int, string) {
	fmt.Println("  two() evaluated")
	return 7, "seven"
}

func three() (int, string, bool) {
	fmt.Println("  three() evaluated")
	return 3, "three", true
}

// next mutates a global so a captured result can be told apart from a re-read one.
func next() (int, int) {
	counter++
	return counter, counter * 10
}

func one() int {
	fmt.Println("  one() evaluated")
	return 1
}

func show(n int, s string) {
	fmt.Println("  show:", n, s)
}

func show3(n int, s string, b bool) {
	fmt.Println("  show3:", n, s, b)
}

func showOne(n int) {
	fmt.Println("  showOne:", n)
}

func report(a int, b int) {
	fmt.Println("  report:", a, b, "counter now", counter)
}

func goShow(n int, s string) {
	fmt.Println("  goShow:", n, s)
	done <- true
}

type sink struct {
	tag string
}

func (s *sink) take(n int, msg string) {
	fmt.Println("  sink", s.tag, "took", n, msg)
}

// deferSpread proves the split: two() runs at the DEFER STATEMENT (before the body's
// trailing line), show runs at unwind (after it).
func deferSpread() {
	fmt.Println("deferSpread: enter")
	defer show(two())
	fmt.Println("deferSpread: body")
}

// deferThree is the same shape at arity 3 — the expansion is not special-cased to pairs.
func deferThree() {
	fmt.Println("deferThree: enter")
	defer show3(three())
	fmt.Println("deferThree: body")
}

// deferCapture proves the results are CAPTURED, not re-derived: report must see the pair
// next() produced at the defer statement (1, 10) even though counter has moved on to 3.
func deferCapture() {
	fmt.Println("deferCapture: enter")
	defer report(next())
	next()
	next()
	fmt.Println("deferCapture: counter is now", counter)
}

// deferOrder proves the spread form takes its place in the ordinary LIFO defer stack, and
// that each deferred call's arguments were evaluated in STATEMENT order at defer time.
func deferOrder() {
	fmt.Println("deferOrder: enter")
	defer show(two())
	defer show3(three())
	fmt.Println("deferOrder: body")
}

// deferMethodSpread spreads into a POINTER-RECEIVER method — the receiver binds at defer
// time exactly as the arguments do.
func deferMethodSpread() {
	fmt.Println("deferMethodSpread: enter")
	s := &sink{tag: "A"}
	defer s.take(two())
	s.tag = "B"
	fmt.Println("deferMethodSpread: body")
}

// deferLoopSpread evaluates the source once PER ITERATION at each defer statement; the
// three deferred calls then unwind LIFO with their own captured pairs.
func deferLoopSpread() {
	fmt.Println("deferLoopSpread: enter")
	for i := 0; i < 3; i++ {
		defer report(next())
	}
	fmt.Println("deferLoopSpread: body, counter", counter)
}

// deferControlPlain is the CONTROL for the arity-matching plain-argument form — it must
// keep the ordinary emission and must not be routed through the spread machinery.
func deferControlPlain() {
	fmt.Println("deferControlPlain: enter")
	defer show(11, "eleven")
	fmt.Println("deferControlPlain: body")
}

// deferControlSingleValueCall is the near-miss CONTROL: the sole argument IS a call, but it
// yields ONE value, so no expansion is owed. Go still evaluates it at the defer statement.
func deferControlSingleValueCall() {
	fmt.Println("deferControlSingleValueCall: enter")
	defer showOne(one())
	fmt.Println("deferControlSingleValueCall: body")
}

// goSpread is the `go` half. Go's rule is identical: the multi-value call is evaluated on
// the CURRENT goroutine at the `go` statement, and only f runs on the new one.
func goSpread() {
	fmt.Println("goSpread: enter")
	go goShow(two())
	<-done
	fmt.Println("goSpread: done")
}

// goCaptureSpread proves the `go` form captures too: the goroutine must report the pair
// next() produced at the `go` statement, not a later one.
func goCaptureSpread() {
	fmt.Println("goCaptureSpread: enter")
	go goShow(pair())
	<-done
	fmt.Println("goCaptureSpread: done")
}

func pair() (int, string) {
	counter++
	return counter, "captured"
}

func main() {
	deferSpread()
	deferThree()
	deferCapture()
	deferOrder()
	deferMethodSpread()
	deferLoopSpread()
	deferControlPlain()
	deferControlSingleValueCall()
	goSpread()
	goCaptureSpread()
	deferFuncLitSpread()
	deferVariadicSpread()
	deferResultReturningSpread()
	fmt.Println("final counter", counter, "regs", regs)
}

func showAll(parts ...any) {
	fmt.Println("  showAll:", parts)
}

var regs = [3]int{1, 2, 3}

// setRegs is the save-and-restore shape: it both TAKES and RETURNS the same arity, so
// `defer setRegs(setRegs(a, b, c))` restores the previous values at unwind. This is the
// idiom the Go standard library's own test hooks use (reflect's SetArgRegs), and its callee
// returns results — the deferred thunk is a result-discarding Func, not an Action.
func setRegs(a int, b int, c int) (int, int, int) {
	old := regs
	regs = [3]int{a, b, c}
	fmt.Println("  setRegs: now", regs, "was", old)
	return old[0], old[1], old[2]
}

// deferResultReturningSpread defers the restore with the save's own three results.
func deferResultReturningSpread() {
	fmt.Println("deferResultReturningSpread: enter")
	defer setRegs(setRegs(7, 8, 9))
	fmt.Println("deferResultReturningSpread: body, regs", regs)
}

// deferFuncLitSpread spreads into a FUNC-LITERAL callee. This is the one defer/go shape whose
// literal is rendered as an INVOCATION rather than handed to the registration as a delegate,
// so it additionally needs the immediately-invoked-literal delegate cast.
func deferFuncLitSpread() {
	fmt.Println("deferFuncLitSpread: enter")
	defer func(n int, s string) {
		fmt.Println("  lit:", n, s)
	}(two())
	fmt.Println("deferFuncLitSpread: body")
}

// deferVariadicSpread spreads into a VARIADIC callee — Go allows the multi-value call to feed
// variadic parameters, and each component crosses into `any` on its own.
func deferVariadicSpread() {
	fmt.Println("deferVariadicSpread: enter")
	defer showAll(two())
	fmt.Println("deferVariadicSpread: body")
}
