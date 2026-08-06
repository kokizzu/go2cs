package main

import "fmt"

type relationship string

const (
	equivalent   relationship = "equivalent"
	moreGeneral  relationship = "moreGeneral"
	moreSpecific relationship = "moreSpecific"
)

// The CONVERSION spelling of the same typed const — `= relationship("…")` rather than a declared
// type on the spec. Its value expression is a CallExpr, not a BasicLit, so it never reached the
// literal path that supplies the `u8` form; it emitted a plain C# string, from which the
// [GoType("@string")] wrapper is two user-defined conversions away (string→@string→wrapper), which
// C# forbids — CS0029 (sync map_test's `opLoad = mapOp("Load")` group, ×9). A FOLDED concatenation
// is the same shape for the same reason.
const (
	opLoad   = relationship("opLoad")
	opStore  = relationship("op" + "Store")
	opDelete = relationship(prefix + "Delete")
)

const prefix = "op"

func (r relationship) tag() string { return "rel:" + string(r) }

// Comparisons between values and consts of the named string type must be unambiguous
// (net/http pattern.go shape: typed consts materialized as @string made every `==`
// ambiguous, CS0034).
func describe(r relationship) string {
	if r == equivalent {
		return "same"
	}
	if r == moreGeneral || r == moreSpecific {
		return "related:" + string(r)
	}
	return "other"
}

func main() {
	fmt.Println(describe(equivalent))
	fmt.Println(describe(moreGeneral))
	fmt.Println(describe(relationship("x")))

	// A local typed const keeps the named type too.
	const localRel relationship = "moreSpecific"
	fmt.Println(describe(localRel))

	// Methods still bind on a const of the named type.
	fmt.Println(equivalent.tag())

	// An untyped string const stays a plain string.
	const plain = "plain"
	fmt.Println(plain)

	// The conversion-spelled consts carry the named type: they compare against each other and
	// against a converted value, and bind the named type's method.
	fmt.Println(opLoad == relationship("opLoad"), opStore == opLoad, opStore.tag(), opDelete.tag())
	fmt.Println(describe(opLoad))

	// A function-local conversion-spelled const takes the same form.
	const localOp = relationship("opLocal")
	fmt.Println(localOp.tag(), localOp == relationship("opLocal"))

	// An UNTYPED const folded from the same pieces stays a plain string (no named type, no u8).
	const untypedOp = prefix + "Untyped"
	fmt.Println(untypedOp, len(untypedOp))
}
