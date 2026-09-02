package main

import "fmt"

// A VALUE-receiver method value binds a COPY of its receiver at the moment the method value is
// EVALUATED, not when the resulting func is called. A closure over the same variable binds the
// VARIABLE and observes later writes. The two therefore disagree on purpose, and a converter that
// makes them share one receiver rendering is wrong whichever way it collapses them.

type frame struct {
	Name    string
	Inlined bool
}

func (f frame) label() string { return f.Name }

// A POINTER receiver binds the ADDRESS of the original storage at evaluation, so writes through the
// method value are visible at that storage — the opposite property to the value receiver's copy, and
// the one a shape-first "snapshot the receiver" would destroy by binding the address of a temp copy.
func (f *frame) bump() { f.Name += "!" }

type namer interface{ label() string }

// The base for the field-chain positions: the receiver expression is a path, not an ident, so every
// call re-reads it unless it is evaluated once.
type holder struct {
	f frame
	i namer
}

// makeFrame counts its own invocations. Go evaluates a call-shaped receiver EXACTLY ONCE, when the
// method value is created; re-evaluating it per call is a side-effect defect, not a timing nicety.
var frameCalls int

func makeFrame() frame {
	frameCalls++
	return frame{Name: "made"}
}

// makePtr is makeFrame's pointer-returning twin: a call-shaped receiver expression is only legal
// under a POINTER receiver when the call itself yields a pointer (a value result is not addressable).
func makePtr() *frame {
	frameCalls++
	return &frame{Name: "madeptr"}
}

func main() {
	// 1. Composite literal holding a method value AND a sibling closure over the same variable,
	//    in ONE statement, with the variable written afterwards. The method value must report the
	//    pre-write receiver; the closure must report the post-write one.
	a := frame{Name: "a"}

	typed := []func() string{
		a.label,
		func() string { return a.Name },
	}

	a.Name = "A"

	fmt.Println("typed   ", typed[0](), typed[1]())

	// 2. The same shape through an EMPTY-interface element. This is the spelling that renders the
	//    receiver as a bare ref-local alias, which a lambda cannot capture at all.
	b := frame{Name: "b", Inlined: true}

	boxed := []any{
		b.label,
		func() bool { return b.Inlined },
	}

	b.Name = "B"

	fmt.Println("boxed   ", boxed[0].(func() string)(), boxed[1].(func() bool)())

	// 3. Two method values over one variable in DIFFERENT statements, each with its own write
	//    between them. They must NOT converge on a shared snapshot: each binds the receiver as it
	//    stood when that method value was evaluated.
	c := frame{Name: "p"}
	first := c.label
	c.Name = "q"
	second := c.label
	c.Name = "r"

	fmt.Println("distinct", first(), second(), c.label())

	// 4. A method value passed as a call ARGUMENT, the non-assignment position that has no
	//    statement of its own to hang a declaration on.
	d := frame{Name: "d"}
	call := func(f func() string) string { return f() }
	got := call(d.label)
	d.Name = "D"

	fmt.Println("argument", got, call(d.label))

	// 5. The ASSIGNMENT position, where the receiver is ALSO captured by a sibling closure — which
	//    heap-boxes it. The capture machinery declines to snapshot a heap-boxed variable, which is
	//    right for the closure (it has to observe the write through the shared box) and wrong for
	//    the method value (it must not), so the receiver was read at call time and reported the
	//    POST-write value.
	e := frame{Name: "e"}
	watch := func() string { return e.Name }
	bound := e.label
	e.Name = "E"

	fmt.Println("assign  ", bound(), watch())

	// ---- commit 3: the receiver EXPRESSION is evaluated exactly once, every kind and shape ----

	// 6. FIELD CHAIN x VALUE receiver — the path is re-read per call unless evaluated once.
	h6 := holder{f: frame{Name: "f6"}}
	chainV := h6.f.label
	h6.f.Name = "F6"

	fmt.Println("chainV  ", chainV())

	// 7. FIELD CHAIN x POINTER receiver — binds &h7.f once; the write must land in h7's storage.
	h7 := holder{f: frame{Name: "f7"}}
	chainP := h7.f.bump
	chainP()

	fmt.Println("chainP  ", h7.f.Name)

	// 8. FIELD CHAIN x INTERFACE receiver — the interface VALUE is saved at evaluation, so replacing
	//    the field afterwards must not be visible through the method value.
	h8 := holder{i: frame{Name: "i8"}}
	chainI := h8.i.label
	h8.i = frame{Name: "I8"}

	fmt.Println("chainI  ", chainI())

	// 9. CALL receiver x VALUE — the loud one. Go calls makeFrame ONCE, at creation.
	frameCalls = 0
	callRecv := makeFrame().label
	_ = callRecv()
	_ = callRecv()

	fmt.Println("callOnce", frameCalls)

	// 10. INDEX (slice) x VALUE — the element is COPIED at evaluation, so a later append that
	//     reallocates, plus a write through the new header, must not be visible.
	s10 := make([]frame, 1, 1)
	s10[0] = frame{Name: "s10"}
	idxV := s10[0].label
	s10 = append(s10, frame{Name: "grown"})
	s10[0].Name = "S10"

	fmt.Println("idxV    ", idxV())

	// 11. INDEX (slice) x POINTER — binds &s11[0] in the ORIGINAL backing array. After a
	//     reallocating append the slice points elsewhere, so the write is not visible through it.
	s11 := make([]frame, 1, 1)
	s11[0] = frame{Name: "s11"}
	idxP := s11[0].bump
	s11 = append(s11, frame{Name: "grown"})
	idxP()

	fmt.Println("idxP    ", s11[0].Name)

	// 12. IDENT x POINTER in CALL-ARGUMENT position, with a whole-variable reassignment between —
	//     the cell the family has never measured. The address is the variable's storage, so the
	//     write lands on whatever the variable holds at call time.
	p12 := frame{Name: "p12"}
	hold := func(f func()) func() { return f }
	argP := hold(p12.bump)
	p12 = frame{Name: "P12"}
	argP()

	fmt.Println("argP    ", p12.Name)

	// 13. INDEX (map) x VALUE — legal Go (map elements are not addressable, so the POINTER cell of
	//     this shape does not exist); untested anywhere in the corpus today.
	m13 := map[string]frame{"k": {Name: "m13"}}
	idxMapV := m13["k"].label
	m13["k"] = frame{Name: "M13"}

	fmt.Println("idxMapV ", idxMapV())

	// 14. REGRESSION guard for the pointer-receiver address binding: a plain ident, pointer
	//     receiver, called twice. This is the position that fails if commit 3 is written
	//     shape-first and snapshots a value copy instead of binding the address.
	c14 := frame{Name: "c14"}
	thru := c14.bump
	thru()
	thru()

	fmt.Println("ptrThru ", c14.Name)

	// 15. FIELD CHAIN over a POINTER base. The root of the receiver expression is an ident, so the
	//     capture machinery's root-ident snapshot fires — but copying a POINTER copies the
	//     reference, so the chain still reads the SAME holder. The value-semantics bases above
	//     (struct, slice header) are saved by that copy; a reference-semantics base is not. This is
	//     the cell that decides whether the corpus's `c.hash.New` sites are safe.
	p15 := &holder{f: frame{Name: "p15"}}
	chainPtrBase := p15.f.label
	p15.f.Name = "P15"

	fmt.Println("chainPtr", chainPtrBase())

	// 16. FIELD CHAIN over an INTERFACE-typed base holding a pointer — the same aliasing question
	//     one level further out, and the shape net/http's `sc.handler.ServeHTTP` actually has.
	var n16 namer = &frame{Name: "n16"}
	h16 := holder{i: n16}
	chainIfacePtr := h16.i.label
	n16.(*frame).Name = "N16"

	fmt.Println("chainIfP", chainIfacePtr())

	// 17. CALL-returning-POINTER x POINTER receiver — the ONLY legal cell in which a pointer
	//     receiver's expression can have a side effect (a value-returning call is not addressable,
	//     so `f().P` does not compile). If the pointer path also defers its receiver expression,
	//     this counts 2 where Go counts 1.
	frameCalls = 0
	callPtr := makePtr().bump
	callPtr()
	callPtr()

	fmt.Println("callPtr ", frameCalls)
}
