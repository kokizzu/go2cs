// PackageVarFuncLitPointerParam guards the pointer DEREF in a func literal that is a PACKAGE-LEVEL
// var initializer — the one place the converter has no enclosing function to take a signature from.
//
// A DECLARATION's pointer parameter is emitted as the box `T p` plus an entry alias
// `ref var p = ref p.DerefOrNull()`, so the Go name is already a value and a field read on it needs
// no deref. A func LITERAL's pointer parameter has no such alias: the name IS the raw box, and a
// field read must deref. The converter distinguished the two by asking the CURRENT function
// signature whether the identifier is one of its parameters — and a package-level initializer has no
// enclosing function, so the literal converter seeds that field with the LITERAL'S OWN signature for
// nil-safety. The literal's parameter then matched, the deref was dropped, and net/http's
// `var hostPortHandler = HandlerFunc(func(w ResponseWriter, r *Request){ … })` emitted `r.Close` and
// `r.RemoteAddr` straight on the box (CS1061 x2) while every sibling handler literal INSIDE a
// function body emitted the deref correctly.
//
// The trigger needs the literal to be the first one converted in its file, so `describe` is declared
// ahead of every func declaration deliberately. Method CALLS on the same parameter were never
// affected — they ask an object-identity predicate instead — and are exercised alongside so a remedy
// cannot fix the field read by breaking them.
package main

import "fmt"

type request struct {
	path   string
	closed bool
	depth  int
}

// FIRST in the file, ahead of EVERY func declaration — including the method below, which is
// deliberately placed after it. The visitor walks a file's declarations in source order and never
// clears the seeded signature, so only a literal converted before any declaration meets the defect;
// putting `label` above this line would silently disarm the guard.
var describe = func(r *request, tag string) string {
	// Field READS on the literal's own pointer parameter, and a METHOD call on it — the calls take
	// an object-identity predicate and were never affected, so a remedy cannot fix the field read by
	// breaking them.
	return fmt.Sprint(tag, ":", r.path, "/", r.closed, "/", r.depth, r.label())
}

// A field WRITE through the same shape — the assignment twin of the deref, which takes a different
// emission arm.
var deepen = func(r *request) {
	r.depth = r.depth + 2
	r.closed = !r.closed
}

// A second package-level literal proves the seed does not make only the FIRST one special.
var pathOf = func(r *request) string { return r.path }

func (r *request) label() string { return "[" + r.path + "]" }

func inside() string {
	// The CONTROL: the same literal shape inside a function body, where the enclosing declaration
	// supplies the signature. This spelling always emitted the deref.
	f := func(r *request) string { return r.path + "!" }

	return f(&request{path: "/ctl"})
}

func main() {
	r := &request{path: "/a", closed: true, depth: 1}

	fmt.Println(describe(r, "first"))

	deepen(r)

	fmt.Println(describe(r, "second"))
	fmt.Println(pathOf(r), inside())
}
