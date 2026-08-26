// MultiValueReturnOrder guards the two properties of a MULTI-VALUE `return` that a C# tuple
// literal does not give for free.
//
//  1. ORDER. Go's spec fixes the order of a return statement's CALLS and leaves its plain operands
//     unordered against them; gc resolves that by spilling every call to a temporary first, so a
//     plain operand is read AFTER the calls beside it. A C# tuple literal evaluates strictly left
//     to right, so an operand read before a later call would carry the PRE-mutation value.
//     crypto/x509's ParseOID (`return o, o.unmarshalOIDText(oid)`) returned an EMPTY OID beside a
//     nil error because of this.
//
//  2. IDENTITY. A multi-value call forwarded whole through `return` into an EMPTY-interface result
//     must put the POINTER in the interface, not a copy of its pointee — crypto/x509's
//     parsePublicKey forwarding `ecdh.X25519().NewPublicKey(der)` into `(any, error)` produced an
//     `any` holding a VALUE where Go holds `*ecdh.PublicKey`, so every type assertion, type switch
//     arm and `%T` on the result disagreed with Go.
package main

import "fmt"

// --- 1. ORDER -----------------------------------------------------------------------------------

type oid struct {
	der []byte
}

// fill mutates through a POINTER receiver, so `o.fill()` beside a plain `o` takes o's address.
func (o *oid) fill(text string) error {
	o.der = []byte(text)
	return nil
}

// parseOID is crypto/x509 ParseOID's exact shape: the plain operand is read after the call.
func parseOID(text string) (oid, error) {
	var o oid
	return o, o.fill(text)
}

type counter struct {
	n int
}

func (c *counter) bump() int {
	c.n++
	return c.n
}

// readThenBump reads a FIELD of the value the later call mutates.
func readThenBump() (int, int) {
	var c counter
	return c.n, c.bump()
}

// orderedCalls proves the spilled calls keep their own lexical order — Go DOES fix that — while the
// plain operand is still read last.
func orderedCalls(log *[]string) (int, string, int) {
	var c counter
	return c.n, note(log, "middle"), c.bump()
}

func note(log *[]string, what string) string {
	*log = append(*log, what)
	return what
}

// addressArgument reaches the same storage through an explicit `&` argument rather than a receiver.
func addressArgument() (int, int) {
	n := 0
	return n, raise(&n)
}

func raise(n *int) int {
	*n += 10
	return *n
}

// throughPointer reads a field THROUGH a pointer the later call writes through.
func throughPointer() (int, int) {
	c := &counter{}
	return c.n, c.bump()
}

// --- 1b. NEGATIVE CONTROLS ----------------------------------------------------------------------
// Neither of these can observe the later call, so neither may spill: the goldens hold the emission
// where it is, which is what keeps the rule scoped to the real hazard.

// pointerIdentity returns the POINTER itself, not a copy of anything the call writes — both orders
// yield the same pointer.
func pointerIdentity() (*counter, int) {
	c := &counter{}
	return c, c.bump()
}

// unrelatedOperand's call touches a different variable entirely.
func unrelatedOperand() (int, int) {
	var a counter
	var b counter
	return a.n, b.bump()
}

// valueReceiverCall's method takes its receiver BY VALUE, so it writes nothing the caller can see.
func valueReceiverCall() (int, int) {
	var c counter
	return c.n, c.peek()
}

func (c counter) peek() int {
	c.n = 99
	return c.n
}

// --- 2. IDENTITY --------------------------------------------------------------------------------

type thing struct {
	name string
}

func (t *thing) String() string {
	return "thing(" + t.name + ")"
}

func newThing(name string) (*thing, error) {
	return &thing{name: name}, nil
}

// forwardPointer forwards a multi-value POINTER-returning call whole into an EMPTY-interface result.
func forwardPointer(name string) (any, error) {
	return newThing(name)
}

func describe(v any) string {
	switch t := v.(type) {
	case *thing:
		return "ptr:" + t.String()
	case thing:
		return "value:" + t.name
	default:
		return "other"
	}
}

func main() {
	o, err := parseOID("abc")
	fmt.Println("parseOID:", string(o.der), err)

	a, b := readThenBump()
	fmt.Println("readThenBump:", a, b)

	var log []string
	x, mid, y := orderedCalls(&log)
	fmt.Println("orderedCalls:", x, mid, y, log)

	p, q := addressArgument()
	fmt.Println("addressArgument:", p, q)

	r, s := throughPointer()
	fmt.Println("throughPointer:", r, s)

	pc, pb := pointerIdentity()
	fmt.Println("pointerIdentity:", pc.n, pb)

	ua, ub := unrelatedOperand()
	fmt.Println("unrelatedOperand:", ua, ub)

	va, vb := valueReceiverCall()
	fmt.Println("valueReceiverCall:", va, vb)

	nd := &node{pat: &counter{}}
	fa, fb := pointerFieldUnrelated(nd)
	fmt.Println("pointerFieldUnrelated:", fa, fb)

	nd2 := &node{pat: &counter{}}
	ga, gb := pointerFieldSame(nd2)
	fmt.Println("pointerFieldSame:", ga, gb)

	v, ferr := forwardPointer("one")
	fmt.Println("forwardPointer:", describe(v), ferr)

	tp, ok := v.(*thing)
	fmt.Println("assert *thing:", ok, tp.name)

	_, notValue := v.(thing)
	fmt.Println("assert thing:", notValue)
}

// --- 1c. POINTER-FIELD SHAPES -------------------------------------------------------------------
// The pair the ACCESS-PATH model exists to tell apart. Both reach two hops from `nd`; only the
// second reads storage the call writes.

type node struct {
	handler int
	pat     *counter
}

// CONTROL — net/http's `return n.handler, n.pattern.String(), …` shape. The call writes *(nd.pat),
// which is storage of its own; the read lives in *nd.
func pointerFieldUnrelated(nd *node) (int, int) {
	return nd.handler, nd.pat.bump()
}

// HAZARD — the read now goes THROUGH the same pointer field the call writes through.
func pointerFieldSame(nd *node) (int, int) {
	return nd.pat.n, nd.pat.bump()
}
