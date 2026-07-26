package main

// z_impl.go sorts LAST in conversion order, which is what makes the two cross-file cases real.

// describe backs a_decls.go's package-level `derivedAtInit`. Its literal hoists to a field
// declared HERE — in a later file, i.e. a different C# partial-class part — so the initializer
// that calls it must be relocated into the ordered static constructor (§4.4's init-order rule).
func describe() string {
	return "derived through a later file"
}

// reuseShared consumes a literal whose FIRST package-wide use is in main.go: the package-scoped
// dedupe must reference that one field from here rather than declare a second.
func reuseShared() string {
	return "shared across files"
}

// A `func init()` body runs exactly once by construction, so nothing in it is hoisted.
func init() {
	if reuseShared() != "shared across files" {
		panic("init literal must not be hoisted")
	}
}
