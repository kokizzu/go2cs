package main

import (
	"fmt"
	"time"
)

// describe takes `any`, so a func-literal argument is natural-typed by C# — the emission must
// state the literal's declared Go result type explicitly or inference picks the arms' literal
// type (`return 0` → C# int = Go int32), collapsing distinct Go func types under reflection
// (testing/quick's TestFailure #3).
func describe(f any) {
	_ = f
	fmt.Println("ok")
}

func main() {
	// Structurally identical anonymous struct types are ONE Go type: both occurrences must
	// lift to a single C# type or the assignment below cannot compile, and reflect.Type
	// identity splits per occurrence (encoding/binary's TestSizeStructCache cache counting).
	a := struct{ X int }{X: 1}
	var b struct{ X int }
	b = a
	fmt.Println(a == b)

	// A function-local named type keeps its Go source name for reflection: %T must print
	// main.point, never the lifted C# identifier (encoding/binary's TestNoFixedSize asserts
	// the exact error text this feeds). Printing it is what pins the [GoLocalName] stamp —
	// the stamp itself rides package_info.cs, which has no golden, so an emission-level pin
	// would guard nothing.
	type point struct{ X, Y int }
	p := point{X: 1, Y: 2}
	fmt.Println(p.X + p.Y)
	fmt.Printf("%T %T\n", p, &p)

	describe(func(x int) int { return 0 })

	embeddedLocalTypes()
	foreignUnderlyingLocalTypes()
	foreignUnderlyingLocalTypesAgain()
	localInterfaceEmbed()
}

// embeddedLocalTypes pins the NAME of an EMBEDDED field whose type is a function-local one.
// Go names an embedded field after the unqualified type name — `myInt`, `embed1` — while the
// converter HOISTS the local type to package scope under a mangled name. Naming the C# member
// after the hoisted type left the declaration (and go2cs-gen's generated constructor and
// promotion, which are both read off it) spelling `TestX_myIntᴛ1` while every use site spelled
// the Go field name: `s.myInt` was CS1061 and `S3{embed1: …}` was CS1739. It also flipped the
// field's EXPORTEDNESS, since the mangled name begins with the enclosing function's capital.
// These are encoding/json's TestAnonymousFields / TestUnmarshalEmbeddedUnexported shapes.
func embeddedLocalTypes() {
	type (
		myInt int
		MyInt int
		embed struct{ Q int }

		// Embeds of all three: a local named non-struct, its exported twin, and a local
		// struct — by value and by pointer.
		holder struct {
			myInt
			MyInt
			embed
		}
		ptrHolder struct {
			*myInt
			*embed
		}
	)

	// Positional construction, then field access BY THE GO FIELD NAME.
	h := holder{1, 2, embed{Q: 3}}
	fmt.Println(h.myInt, h.MyInt, h.embed.Q)

	// Promotion through the embedded struct reads the same field without naming it.
	fmt.Println(h.Q)

	// Keyed construction names the fields explicitly.
	k := holder{myInt: 4, MyInt: 5, embed: embed{Q: 6}}
	fmt.Println(k.myInt, k.MyInt, k.Q)

	// Writing through an embedded field, and through a pointer embed.
	k.myInt = 7
	fmt.Println(k.myInt)

	i := myInt(8)
	e := embed{Q: 9}
	pp := ptrHolder{myInt: &i, embed: &e}
	*pp.myInt = 10
	pp.embed.Q = 11
	fmt.Println(*pp.myInt, pp.embed.Q, pp.Q)

	// The local STRUCT type's Go name survives for reflection when it is reached through an
	// embedded field, exactly as it does for a plain local (the [GoLocalName] stamp above).
	// Its non-struct sibling — `%T` of h.myInt — still prints the hoisted identifier, because
	// only lifted STRUCT types are stamped; that is a separate, boarded gap, not this one.
	fmt.Printf("%T\n", h.embed)
}

// foreignUnderlyingLocalTypes pins the lift for a function-local type whose underlying is a
// FOREIGN (cross-package) NAMED type — `type myTime time.Time`. Every other local
// type-declaration kind hoists to member level; this one alone wrote its `[GoType] partial
// struct` INLINE into the method body, which C# forbids. reflect's set_test.go declares
// `type MyBuffer bytes.Buffer` inside TestImplicitMapConversion, and that ONE site produced 73
// parse diagnostics — the whole file, and the whole package's suite behind it.
func foreignUnderlyingLocalTypes() {
	// A STRUCT underlying, used exactly as reflect uses it: through a pointer, as a map key.
	type myTime time.Time
	m := make(map[*myTime]string)
	t := new(myTime)
	m[t] = "seven"
	v, ok := m[t]
	fmt.Println(v, ok, len(m))

	// A BASIC underlying reaches the SAME emission branch (flag's package-level
	// `type durationValue time.Duration`), so the lift must cover that shape too. Nothing here
	// CONVERTS a number into the wrapper: `myDur(1500)` is legal Go, but the [GoType] wrapper
	// over a foreign NAMED type carries conversions to and from that named type only, so an
	// untyped-constant conversion is CS0030 — at package scope exactly as here (A/B-controlled).
	// That gap is real, pre-existing and independent of the lift; a guard that leaned on it
	// would go red for someone else's defect.
	type myDur time.Duration
	var d myDur
	pd := new(myDur)
	fmt.Println(d == *pd, pd != nil)

	// A nested BLOCK is where reflect's declaration actually sits: the lift must reach out to
	// member level, not merely out of the enclosing statement list.
	{
		type inner time.Time
		pi := new(inner)
		fmt.Println(pi != nil)
	}
}

// foreignUnderlyingLocalTypesAgain re-declares the SAME local names in a SECOND function. The
// lift's `<Func>_<name>` prefix and its ᴛN disambiguation are what keep two functions' identically
// named local types from claiming one C# name (archive/tar's `type testFnc any` shape, one
// declaration kind over).
func foreignUnderlyingLocalTypesAgain() {
	type myTime time.Time
	type myDur time.Duration

	t := new(myTime)
	var d, d2 myDur
	fmt.Println(t != nil, d == d2)
}

// localInterfaceEmbed pins the lift RENAME's propagation into an EMBEDDED interface base. Both
// interfaces hoist to member level under `<Func>_<name>`, but the embed rendered the bare Go name,
// which exists nowhere after the hoist — reflect's TestMethodPkgPath declares `type I interface{…}`
// and then `type i interface{ I; … }`, and that one CS0246 took all_test.cs with it. The sibling
// shape (a self-referential local SLICE, gob's `type recursiveSlice []recursiveSlice`) has been
// guarded in visitArrayType since it was found; this is the interface member of the family.
func localInterfaceEmbed() {
	type I interface {
		x() int
	}
	type i interface {
		I
		y() int
	}

	var v i = embedImpl{}
	fmt.Println(v.x(), v.y(), v.x()+v.y())
}

// embedImpl satisfies the local interfaces above. It must live at package scope: Go allows no
// methods on a function-local type.
type embedImpl struct{}

func (embedImpl) x() int { return 3 }
func (embedImpl) y() int { return 4 }
