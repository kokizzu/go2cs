// Guards the escape-analysis rule for a POINTER-RECEIVER METHOD VALUE.
//
// `c.split` where split has a pointer receiver and c is an addressable value is Go shorthand
// for `(&c).split` — an implicit address-of that heap-promotes c, so every write the method
// makes through its receiver is visible in c afterwards. The converter used to see no `&` and
// leave c unpromoted, emitting the copy-box `Ꮡ(c).split`: it compiled and ran, but the method
// mutated a COPY and the caller's writes were silently dropped (bufio's `s.Split(c.split)`
// scan counter never decremented — "stopped with N left to process").
//
// Counter-proof cases below pin the boundary: a value-receiver method value must still COPY
// the receiver at evaluation time, a pointer-typed base passes the pointer value with no
// address-of, and a direct pointer-receiver CALL binds `this ref` and must NOT promote.
package main

import "fmt"

type counter struct {
	n     int
	calls int
}

// Pointer receiver: writes must reach the caller's variable.
func (c *counter) dec(step int) int {
	c.n -= step
	c.calls++
	return c.n
}

// Value receiver: Go copies the receiver when the method VALUE is formed.
func (c counter) peek() int { return c.n }

type holder struct {
	c counter
}

type intList []int

// Pointer receiver on an inherently-heap named slice: the copy-box would clone the
// slice HEADER, so appends made through the method value would never reach the caller.
func (l *intList) push(v int) { *l = append(*l, v) }

func applyInt(f func(int) int, a int, b int) int { return f(a) + f(b) }

func applyPush(f func(int)) {
	f(1)
	f(2)
	f(3)
}

// --- the bufio shape: a method value handed to another function ---------------------------

func viaLocal() (int, int) {
	c := counter{n: 100}
	sum := applyInt(c.dec, 5, 7)
	return c.n, sum
}

// --- a value PARAMETER: needs the entry-time box, not a call-site copy ---------------------

func viaParam(c counter) (int, int) {
	sum := applyInt(c.dec, 1, 2)
	return c.n, sum
}

// --- a NAMED RESULT: declared in the signature, never on a `:=` LHS ------------------------

func viaNamedResult() (c counter) {
	c = counter{n: 50}
	applyInt(c.dec, 4, 6)
	return
}

// --- a value-FIELD chain rooted at a local: `&h.c` is taken -------------------------------

func viaFieldChain() int {
	h := holder{c: counter{n: 30}}
	applyInt(h.c.dec, 2, 3)
	return h.c.n
}

// --- an inherently-heap named slice: the box must alias, not clone the header --------------

func viaNamedSlice() (int, int) {
	var l intList
	applyPush(l.push)
	total := 0
	for _, v := range l {
		total += v
	}
	return len(l), total
}

// --- counter-proof: a VALUE-receiver method value copies at evaluation time ----------------

func valueReceiverCopies() int {
	c := counter{n: 7}
	peek := c.peek
	c.n = 999 // must NOT be observed by peek — Go bound a copy
	return peek()
}

// --- counter-proof: a POINTER-typed base passes the pointer, taking no address of it -------

func pointerBaseNoPromotion() int {
	c := &counter{n: 20}
	applyInt(c.dec, 1, 1)
	return c.n
}

// --- counter-proof: a direct CALL is not a method value; `c` stays unpromoted ---------------

func directCallStaysUnboxed() int {
	c := counter{n: 12}
	c.dec(2)
	c.dec(3)
	return c.n
}

// --- a method value formed inside a closure over an enclosing local ------------------------

func viaClosure() int {
	c := counter{n: 60}
	run := func() {
		applyInt(c.dec, 8, 9)
	}
	run()
	return c.n
}

func main() {
	n, sum := viaLocal()
	fmt.Println("viaLocal:", n, sum)

	n, sum = viaParam(counter{n: 40})
	fmt.Println("viaParam:", n, sum)

	r := viaNamedResult()
	fmt.Println("viaNamedResult:", r.n, r.calls)

	fmt.Println("viaFieldChain:", viaFieldChain())

	ln, total := viaNamedSlice()
	fmt.Println("viaNamedSlice:", ln, total)

	fmt.Println("valueReceiverCopies:", valueReceiverCopies())
	fmt.Println("pointerBaseNoPromotion:", pointerBaseNoPromotion())
	fmt.Println("directCallStaysUnboxed:", directCallStaysUnboxed())
	fmt.Println("viaClosure:", viaClosure())
}
