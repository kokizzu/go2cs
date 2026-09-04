// PrintfFormatCommaParen pins the emitted form of a Printf/Sprintf FORMAT STRING whose text holds
// parentheses -- with a comma inside them, nested, empty, or unbalanced -- and of the argument
// shapes that were named alongside it. It exists because a routed report (2026-09-03) diagnosed a
// converter defect here twice, in two incompatible ways: first "a Printf whose FIRST variadic
// argument is a .String() call is mangled", then, corrected by elimination, "the trigger is the
// FORMAT's comma inside parentheses". Neither reproduces: every row below converts clean at the
// converter that raised the report AND at its branch base, byte-identically, and the corpus already
// carries thousands of such formats (bufio's "first ReadSlice(,) = %q, %v", archive/tar's
// "formatPAXTime(%ds, %dns): got %q, want %q") compiling green.
//
// So this guard banks a NEGATIVE result rather than a fix. The format literal is emitted verbatim as
// a `"…"u8` UTF-8 span whatever its text contains: the converter never tokenizes a literal's
// CONTENTS as call syntax, and the golden beside this file is what keeps that true. The value is in
// the next elimination -- a report of this shape is now a filtered behavioral run, not an
// investigation.
package main

import "fmt"

// stringer is the "first variadic argument is a method call returning a string" arm -- the report's
// FIRST diagnosis, which the second one replaced. Both arms are kept: a guard that only covers the
// surviving diagnosis cannot show the other was eliminated.
type stringer struct{ s string }

func (b stringer) String() string { return b.s }

func main() {
	name := "x"
	n := 3
	b := stringer{"bx"}

	// The reported format text, verbatim, single-line call form.
	fmt.Printf("constructed row: ChanOf(BothDir, ArrayOf(3,int)) String()=%s Elem().Len()=%d\n", name, n)

	// The same format text, multi-line call form (the shape the report was measured in).
	fmt.Printf("constructed row: ChanOf(BothDir, ArrayOf(3,int)) String()=%s Elem().Len()=%d\n",
		name, n)

	// A comma inside parentheses, without and with a following space.
	fmt.Printf("f(a,b) s=%s d=%d\n", name, n)
	fmt.Printf("f(a, b) s=%s d=%d\n", name, n)

	// Nested parentheses, comma in the inner group.
	fmt.Printf("f(g(1,2)) s=%s d=%d\n", name, n)

	// CONTROL: a comma with no parentheses at all -- the row that must already have worked.
	fmt.Printf("a,b s=%s d=%d\n", name, n)

	// Empty parentheses, and a verb immediately after a closing one.
	fmt.Printf("String() s=%s d=%d\n", name, n)
	fmt.Printf("Elem().Len()=%d\n", n)

	// UNBALANCED parentheses each way -- the rows a paren-depth scan over the literal's contents
	// would desynchronize on, and the ones the report's elimination could not have reached.
	fmt.Printf("open( s=%s d=%d\n", name, n)
	fmt.Printf("close) s=%s d=%d\n", name, n)

	// An escaped double quote beside a comma inside parentheses: the literal's own quoting must not
	// end the emitted C# literal early.
	fmt.Printf("q(\"a\",b) s=%s\n", name)

	// Argument-count arms: one argument, none at all, and three across a multi-line call.
	fmt.Printf("one(a,b) s=%s\n", name)
	fmt.Printf("none(a,b)\n")
	fmt.Printf("three(a,b) %s %d %s\n",
		name, n, name)

	// A percent escape beside parentheses -- `%%` must survive as `%%`, not be consumed as a verb.
	fmt.Printf("pct(a,b) 100%% s=%s\n", name)

	// No trailing newline in the format.
	fmt.Printf("noNL f(a,b) s=%s d=%d", name, n)
	fmt.Println()

	// The report's FIRST diagnosis: a method call returning a string as the FIRST variadic argument,
	// with and without a comma inside parentheses in the format.
	fmt.Printf("m1 f(a,b) s=%s d=%d\n", b.String(), n)
	fmt.Printf("m2 s=%s d=%d\n", b.String(), n)
	fmt.Printf("m3 f(a,b) s=%s d=%d\n",
		b.String(), n)

	// The Stringer VALUE rather than the method call, and the method call in second position.
	fmt.Printf("m4 f(a,b) s=%s\n", b)
	fmt.Printf("m5 f(a,b) %d %s\n", n, b.String())

	// Sprintf carries the same format text, single-line and multi-line.
	fmt.Println(fmt.Sprintf("sp1 ChanOf(BothDir, ArrayOf(3,int)) String()=%s Elem().Len()=%d", name, n))
	fmt.Println(fmt.Sprintf("sp2 f(a,b) s=%s d=%d",
		name, n))

	// CONTROL: Println with the same text -- the sidestep the report's own guard took.
	fmt.Println("pl ChanOf(BothDir, ArrayOf(3,int)) String() Elem().Len()")
}
