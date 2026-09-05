// Struct-field METADATA through reflect, three roots measured by reflect's own suite and pinned here
// against `go run` (increment E2 of the reflect tail):
//
//   1. Go's flag.ro(): an EMBEDDED read-only field's elements stay read-only through Index and
//      Slice, so CanSet answers false two hops down (TestIssue22031).
//   2. FieldByName's MULTIPLICITY rule: an embedded type reached twice at one depth annihilates
//      itself -- its field is absent, not found once (TestFieldByName's S3.B / S10.X / S14.X).
//   3. StructField.PkgPath is the package that DECLARED the field, even when the struct is a defined
//      type over another package's struct (TestFieldPkgPath's localOtherPkgFields row).
//
// The row this guard deliberately does NOT carry: Anonymous for an embedded BUILTIN (`struct{ int }`),
// which needs an emission marker the converter does not yet place (E2b, sized by census). That half of
// TestFieldPkgPath stays red until it lands; adding its row here now would seat a red guard.
//
// A second shape withheld for a different reason: a struct EMBEDDING `twice` (`type deeper struct{ twice }`,
// the clause that an annihilation at one depth inhibits every deeper match) does not compile through
// go2cs-gen today. TypeGenerator's transitive promotion walk (StructTypeTemplate.getStructMembers) guards
// against cycles with ONE `seenTypes` set shared across sibling branches, so `base` -- reached through
// `viaX` AND `*viaY` at the same depth -- is walked once and its `B` counted once: `deeper` promotes
// `B => ref twice.B` while `twice`'s own shell (each embed walked afresh) correctly promotes no `B`, and
// the forwarder binds a nonexistent hop (CS1061 x2 in go.main_package.deeper.g.cs). It is the same
// visited-vs-multiplicity error E2 fixes in reflect's promotedFieldByName, on the generator's side; the
// shape returns here when the walk's guard is scoped to the current PATH (its own increment, route #7 gates).
package main

import (
	"fmt"
	"reflect"

	"ReflectFieldMetadata/fieldlib"
)

// --- root 1: flag.ro() ---
type sElem []struct{ C int }
type embeds struct{ sElem }   // the slice type is EMBEDDED and unexported: flagEmbedRO
type holds struct{ f sElem }  // a plain unexported field: flagStickyRO

// --- root 2: multiplicity ---
type base struct{ B int }
type viaX struct{ base }
type viaY struct{ base }
type twice struct { // base is reached through viaX AND *viaY at depth 2: B annihilates
	viaX
	*viaY
	D int
}
type once struct { // base reached ONCE: B is found at depth 2
	viaX
	D int
}

// --- root 3: PkgPath through a defined type over a foreign struct ---
type local fieldlib.Outer

func main() {
	// 1. CanSet through an embedded read-only slice's element's field.
	fmt.Println("CanSet via embedded  :", reflect.ValueOf(embeds{sElem{{}}}).Field(0).Index(0).Field(0).CanSet())
	fmt.Println("CanSet via unexported:", reflect.ValueOf(holds{sElem{{}}}).Field(0).Index(0).Field(0).CanSet())
	// and the same read-only survives a Slice window
	fmt.Println("CanSet via Slice     :", reflect.ValueOf(embeds{sElem{{}}}).Field(0).Slice(0, 1).Index(0).Field(0).CanSet())

	// 2. FieldByName: found / not found, and the index path when found.
	_, foundTwice := reflect.TypeOf(twice{}).FieldByName("B")
	fOnce, foundOnce := reflect.TypeOf(once{}).FieldByName("B")
	fD, foundD := reflect.TypeOf(twice{}).FieldByName("D")
	fmt.Println("twice.B  found:", foundTwice)
	fmt.Println("once.B   found:", foundOnce, "index:", fOnce.Index)
	fmt.Println("twice.D  found:", foundD, "index:", fD.Index)
	// the Value side agrees with the Type side
	fmt.Println("twice.B  value valid:", reflect.ValueOf(twice{}).FieldByName("B").IsValid())

	// 3. PkgPath of a field declared in another package, reached through a defined local type. The
	// rows compare PATHS rather than print them, so the guard measures E2's root -- the declaring
	// package survives the defined-type hop -- and not the spelling of a sub-library's import path.
	lt := reflect.TypeOf(local{})
	ot := reflect.TypeOf(fieldlib.Outer{})
	mine := reflect.TypeOf(struct{ u int }{}).Field(0).PkgPath
	fmt.Println("local.Field(0) exported, PkgPath empty:", lt.Field(0).IsExported(), lt.Field(0).PkgPath == "")
	fmt.Println("local.Field(1) exported:", lt.Field(1).IsExported())
	fmt.Println("local.Field(1) PkgPath == Outer.Field(1) PkgPath:", lt.Field(1).PkgPath == ot.Field(1).PkgPath)
	fmt.Println("local.Field(1) PkgPath is foreign (not this package, not empty):", lt.Field(1).PkgPath != mine, lt.Field(1).PkgPath != "")
	fmt.Println("anon unexported PkgPath is this package:", mine == "main")
}
