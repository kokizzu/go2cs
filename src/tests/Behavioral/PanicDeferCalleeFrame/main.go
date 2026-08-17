// Regression test: a function CALLED from a deferred call while a panic is unwinding must not
// re-raise that panic when its OWN defers finish.
//
// A Go function that defers is emitted with its body inline in try/catch/finally beside a GoFrame
// (docs/Phase4/DESIGN-closure-emission.md §4): the catch parks the panic and the finally re-raises
// an unrecovered one. Read as "re-raise whatever is parked", that makes every ordinary callee a
// re-raise site — it caught nothing, but it leaves through the same finally, so the CALLER's
// in-flight panic escapes from the middle of the caller's deferred cleanup and every statement
// after that call is skipped. The panic must resume only when the frame that CAUGHT it is done.
//
// database/sql's Conn.Raw is the shape that found it. Its deferred cleanup calls release, which
// calls Conn.close, whose `c.dc = nil` sits after a chain ending in withLock — a two-line helper
// holding one defer and panicking nothing. The connection was therefore left open, and TestConnRaw
// blocked on a five-second poll that could never become true (it consumed the package's entire
// deadline: 3,418 s against Go's 0.005 s).
//
// The cases below are that shape reduced, plus the two negatives a fix must not break: a panic
// raised BY a deferred call in a frame that caught nothing still has to escape, and depth must not
// matter.
package main

import "fmt"

var trace []string

func note(s string) { trace = append(trace, s) }

func show(label string, open bool) {
	fmt.Println(label, "open =", open, trace)
	trace = nil
}

// ---------------------------------------------------------------------------------------------
// The reduced shape: a resource whose cleanup runs several calls deep, each level deferring.

var open bool

// withLock is the exact shape that found it: it defers, it panics nothing, and it is called from
// inside a deferred cleanup while a panic is in flight.
func withLock(fn func()) {
	note("lock")
	defer note("unlock")
	fn()
}

func release() {
	withLock(func() { note("release") })
	// The statement the spurious re-raise skipped.
	open = false
	note("closed")
}

// raw mirrors (*sql.Conn).Raw: the callback may panic, and the deferred cleanup must complete.
func raw(f func()) {
	note("acquire")
	defer func() {
		note("cleanup begin")
		release()
		note("cleanup end")
	}()
	f()
}

// ---------------------------------------------------------------------------------------------
// Depth: three deferring callees stacked below one deferred call.

func deep3() { defer note("deep3 defer"); note("deep3 body") }
func deep2() { defer note("deep2 defer"); deep3(); note("deep2 after") }
func deep1() { defer note("deep1 defer"); deep2(); note("deep1 after") }

func deepCleanup() {
	defer func() {
		note("deep cleanup begin")
		deep1()
		note("deep cleanup end")
	}()
	panic("deep boom")
}

// ---------------------------------------------------------------------------------------------
// The negatives. A panic raised by a frame's OWN deferred call is that frame's to continue, even
// when the frame caught nothing itself — so it must still escape.

func calleeDeferPanics() {
	defer func() {
		note("callee defer")
		panic("callee boom")
	}()
	note("callee body")
}

// ...including when that callee is itself invoked from a deferred cleanup during another panic:
// the newer panic replaces the one unwinding, exactly as in Go.
func calleeDeferPanicsDuringPanic() {
	defer func() {
		note("outer cleanup")
		calleeDeferPanics()
		note("unreachable")
	}()
	panic("outer boom")
}

func main() {
	// 1. Control: no panic. The cleanup runs to completion on the ordinary return path.
	open = true
	raw(func() { note("callback ok") })
	show("normal:", open)

	// 2. The defect: the callback panics. The cleanup must still reach `open = false`, and the
	//    panic must still escape raw exactly once.
	open = true
	func() {
		defer func() { fmt.Println("recovered:", recover()) }()
		raw(func() { panic("callback boom") })
		note("unreachable")
	}()
	show("panicked:", open)

	// 3. Depth is not special: every level's defer runs, and nothing below the deferred call
	//    re-raises.
	func() {
		defer func() { fmt.Println("recovered:", recover()) }()
		deepCleanup()
	}()
	show("deep:", open)

	// 4. Negative: a panic from a frame's own deferred call escapes that frame.
	func() {
		defer func() { fmt.Println("recovered:", recover()) }()
		calleeDeferPanics()
		note("unreachable")
	}()
	show("callee panic:", open)

	// 5. Negative: the same, raised while another panic is already unwinding — the new panic wins.
	func() {
		defer func() { fmt.Println("recovered:", recover()) }()
		calleeDeferPanicsDuringPanic()
	}()
	show("callee panic during panic:", open)
}
