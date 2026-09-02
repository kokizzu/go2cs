package main

import "fmt"

// A VALUE-receiver method reached through a POINTER-typed receiver expression is Go's implicit
// dereference: `h.p.label` with `p *frame` and a value-receiver `label` IS `(*h.p).label`. Two
// properties follow from that, and a converter has to render BOTH:
//
//  1. what the method value saves is the POINTEE'S COPY, taken when the method value is
//     EVALUATED — so a later write through the pointer is not visible through it, and repointing
//     the pointer afterwards is not either;
//  2. the method body then operates on that copy, so a mutation inside a value-receiver method
//     cannot reach the pointee — the property an emission that bound the ADDRESS instead would
//     destroy silently.
//
// The receiver-snapshot family (MethodValueReceiverSnapshot) established the evaluate-once rule
// for every receiver KIND; this position is the one shape it deliberately left alone, because
// hoisting the pointer where the value is wanted does not compile (CS1929) and hoisting it
// anyway would defer the dereference to call time.

type frame struct {
	Name string
}

func (f frame) label() string { return f.Name }

// tag exercises the parameter-carrying forwarding lambda: a method value WITH parameters is a
// different emission arm from the nullary one, and it renders the receiver separately.
func (f frame) tag(suffix string) string { return f.Name + suffix }

// touch MUTATES its value receiver. A value receiver operates on a copy, so the write must not
// reach the pointee.
func (f frame) touch() string {
	f.Name = "touched"
	return f.Name
}

// holder gives the receiver expression a PATH: `h.p` is not an ident, so it is the shape the
// method-value hoist has to render, and it is the position this guard was written for.
type holder struct {
	p *frame
}

// ptrCalls counts makePtr's invocations: Go evaluates a call-shaped receiver expression exactly
// once, when the method value is created.
var ptrCalls int

func makePtr() *frame {
	ptrCalls++
	return &frame{Name: "made"}
}

// viaParam returns a method value over a pointer PARAMETER. A pointer parameter renders as a
// deref-aliased value rather than as the box, so a hoist that adds a dereference unconditionally
// would dereference a value here.
func viaParam(p *frame) func() string {
	return p.label
}

func main() {
	// 1. POINTER-TYPED FIELD receiver, assignment position, written THROUGH between creation and
	//    call. The method value reports the copy it saved; a fresh read reports the write.
	h1 := holder{p: &frame{Name: "a"}}
	fieldV := h1.p.label
	h1.p.Name = "A"

	fmt.Println("field   ", fieldV(), h1.p.label())

	// 2. The pointer itself REPOINTED after the method value is created. The saved copy came from
	//    the ORIGINAL pointee; a fresh read follows the new pointer.
	h2 := holder{p: &frame{Name: "b"}}
	repoint := h2.p.label
	h2.p = &frame{Name: "B"}

	fmt.Println("repoint ", repoint(), h2.p.label())

	// 3. POINTER LOCAL ident receiver — the same rule with no path to walk.
	p3 := &frame{Name: "c"}
	identV := p3.label
	p3.Name = "C"

	fmt.Println("ident   ", identV())

	// 4. CALL-ARGUMENT position — the non-assignment arm, which has no statement of its own to
	//    hang a declaration on.
	h4 := holder{p: &frame{Name: "d"}}
	call := func(f func() string) string { return f() }
	got := call(h4.p.label)
	h4.p.Name = "D"

	fmt.Println("argument", got, call(h4.p.label))

	// 5. A CALL-shaped pointer receiver expression: evaluated exactly ONCE, and the pointee copied
	//    at that point. Deferring the expression into the wrapper would count 2 here.
	ptrCalls = 0
	callV := makePtr().label
	_ = callV()
	_ = callV()

	fmt.Println("callOnce", ptrCalls, callV())

	// 6. A method WITH PARAMETERS through the pointer expression.
	h6 := holder{p: &frame{Name: "e"}}
	tag := h6.p.tag
	h6.p.Name = "E"

	fmt.Println("params  ", tag("!"))

	// 7. The value receiver's own COPY semantics: touch writes to its receiver, and the pointee
	//    must not see it.
	h7 := holder{p: &frame{Name: "f"}}
	touch := h7.p.touch

	fmt.Println("copy    ", touch(), h7.p.Name)

	// 8. A pointer PARAMETER as the receiver expression.
	p8 := &frame{Name: "g"}
	paramV := viaParam(p8)
	p8.Name = "G"

	fmt.Println("param   ", paramV())

	// 9. A SLICE ELEMENT of pointer type — a pointer-typed receiver expression that is neither an
	//    ident nor a field path.
	s9 := []*frame{{Name: "h"}}
	elemV := s9[0].label
	s9[0].Name = "H"

	fmt.Println("elem    ", elemV())
}
