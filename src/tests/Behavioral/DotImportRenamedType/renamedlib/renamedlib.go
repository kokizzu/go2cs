// renamedlib plays go/types in the dot-imported-renamed-type guard: it declares exported types
// that the converter's name-collision rule Δ-RENAMES inside this package, so a consumer that
// spells them by their raw Go name names nothing at all.
//
// The rule (performNameCollisionAnalysis, nameCollisionAnalysisOperations.go): a package-level
// named element whose name is ALSO the name of some package-level FuncDecl in the same package
// collides, and the ELEMENT is Δ-renamed. Every FuncDecl counts, method or free function — and
// since Go forbids a type and a free function sharing a package-scope name, the collision can only
// ever come from a METHOD. Both ways a method can supply it are declared below, because go/types
// happens to use one of each.
package renamedlib

import "fmt"

// Marker is Δ-renamed (ΔMarker) by a method ON ITSELF — go/types' `Error` shape, where
// `func (err Error) Error() string` renames the `Error` struct.
type Marker struct {
	Name string
	Size int
}

// Marker (the METHOD) is what collides with the TYPE name above.
func (m Marker) Marker() string {
	return fmt.Sprintf("%s/%d", m.Name, m.Size)
}

// Detail is Δ-renamed (ΔDetail) by a method on a DIFFERENT type — go/types' `Info` shape, where
// `func (b *Basic) Info() BasicInfo` renames the unrelated `Info` struct. This half is the easier
// one to miss: nothing about Detail's own declaration hints that it moves.
type Detail struct {
	Label string
	Rank  int
}

func (d Detail) Show() string {
	return fmt.Sprintf("%s#%d", d.Label, d.Rank)
}

// Detail (the METHOD, on Marker) is what collides with the Detail TYPE above.
func (m Marker) Detail() Detail {
	return Detail{Label: m.Name, Rank: m.Size}
}

// Plain is the CONTROL: exported, but no FuncDecl shares its name, so it is NOT renamed and must
// keep its bare cross-package emission. It is what proves the fix does not over-reach.
type Plain struct {
	Note string
}

// Describe boxes a Marker in an interface so the consumer must assert back to the renamed type.
func Describe(name string, size int) any {
	return Marker{Name: name, Size: size}
}

// Wrap boxes a Detail the same way.
func Wrap(label string, rank int) any {
	return Detail{Label: label, Rank: rank}
}

// Boxed boxes the non-renamed control type.
func Boxed(note string) any {
	return Plain{Note: note}
}
