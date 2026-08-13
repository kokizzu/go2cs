// Guards the cross-package Δ-renamed TYPE reference fix.
//
// A DOT import (`. "…/renamedlib"`) makes a foreign type's reference a BARE ident, so there is no
// selector for the qualified-name resolver to rewrite. The type may still be collision-renamed
// inside its own package, and then the raw Go name binds nothing (CS0246).
//
// The TYPE-DRIVEN positions — a declaration, a parameter, a conversion — resolve from types.Type
// and always went through foreignAliasedTypeName, which is why `var mu Mutex` through a dot import
// has worked since DotImportRenamedPackage. The two AST-IDENT type positions did not: a
// TYPE-ASSERTION target and a COMPOSITE-LITERAL type both render through convIdent's isType arm,
// which returned the bare Go name. So `Marker{…}` emitted `new Marker(…)` and `v.(Marker)` emitted
// `v._<Marker>(ᐧ)` against a declaration named ΔMarker — while the consumer's own package_info.cs
// had already minted the correct `renamedlibꓸMarker` alias and left it unused.
//
// Found in internal/types/errors, whose external test file dot-imports go/types: codes_test.cs
// emitted `err._<Error>(ᐧ)` and `new Info(…)` where go/types declares ΔError and ΔInfo.
//
// The QUALIFIED spelling of the same two shapes was always correct, so this project is also the
// assertion that one Go type does not get two spellings depending on how the source named it.
package main

import (
	"fmt"

	. "DotImportRenamedType/renamedlib"
)

// Local reproduces the same collision SHAPE inside this package: it is Δ-renamed too, but locally,
// so its bare reference must keep resolving to the local declaration and never through an imported
// alias. foreignAliasedTypeName is a no-op for a same-package type — this is what says so.
type Local struct {
	Tag string
}

func (l Local) Local() string {
	return "L:" + l.Tag
}

func main() {
	// COMPOSITE LITERAL on a dot-imported, Δ-renamed type.
	m := Marker{Name: "alpha", Size: 3}
	fmt.Println("literal:", m.Marker())

	// Pointer composite literal — the same type through the address-of/boxing path.
	p := &Marker{Name: "beta", Size: 5}
	fmt.Println("ptr-literal:", p.Marker())

	// Composite literal of the type renamed by ANOTHER type's method (the go/types Info shape).
	d := Detail{Label: "gamma", Rank: 7}
	fmt.Println("detail-literal:", d.Show())

	// A renamed type reached as a method RESULT, then re-literal'd — proves the rename is
	// consistent between the type-driven and the ident-driven positions.
	fmt.Println("detail-of:", m.Detail().Show())

	// TYPE ASSERTION, comma-ok form (go/types' `err.(Error)`).
	if got, ok := Describe("delta", 9).(Marker); ok {
		fmt.Println("assert-ok:", got.Marker())
	}

	// TYPE ASSERTION, single-value form.
	one := Wrap("epsilon", 11).(Detail)
	fmt.Println("assert-one:", one.Show())

	// A MISSED comma-ok assertion still discriminates correctly against the renamed type.
	_, notMarker := Wrap("zeta", 13).(Marker)
	fmt.Println("assert-miss:", notMarker)

	// CONTROL: an exported type the rule does NOT rename keeps its bare emission.
	pl := Plain{Note: "eta"}
	fmt.Println("plain:", pl.Note)

	if got, ok := Boxed("theta").(Plain); ok {
		fmt.Println("plain-assert:", got.Note)
	}

	// CONTROL: the SAME-package renamed type resolves locally, not through an alias.
	l := Local{Tag: "iota"}
	fmt.Println("local:", l.Local())

	if got, ok := any(l).(Local); ok {
		fmt.Println("local-assert:", got.Local())
	}
}
