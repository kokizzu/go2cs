package main

import (
	"fmt"
	"reflect"
)

// The reflection bridge's CLOSURE arc: the facts encoding/json and fmt read out of reflect that the
// managed bridge answered wrongly, each reduced to the smallest Go program that observes it. Every
// line below is compared against `go run`, so a bridge answer that merely LOOKS plausible fails.

// ---- 1. an EMBEDDED field is visible to reflect AS an embed, tag included ----------------------
//
// StructField.Anonymous is the whole Go embedding contract: encoding/json, encoding/xml,
// encoding/gob and text/template all flatten a field's own fields into the enclosing object exactly
// when it is set and no name tag overrides it. Reported false, every embed became an ordinary field
// named after its type. The TAG is the second half and lives at a different declaration site — the
// converter stamps it on the emitted partial PROPERTY while the backing field is generated — so an
// embed's tag came back empty for every converted struct.

type inner struct {
	X int
}

type tagged struct {
	Y int
}

// A plain unexported field is the read-only CONTROL for the promoted-through-an-embed case.
type nested struct {
	hidden int
}

// Two embeds carrying the same name at the same depth ANNIHILATE: Go reports the name absent
// rather than picking one, which is why a promoted search counts matches per level.
type clashA struct{ Y int }
type clashB struct{ Y int }
type clash struct {
	clashA
	clashB
}

type host struct {
	Plain  int
	inner             // an embed, unexported type, exported field
	tagged `json:"t"` // an embed WITH a tag: named by the tag, not flattened
	Named  inner      `json:"n"` // NOT an embed: same type, same name shape, declared field
}

// ---- 2. an UNNAMED func type renders STRUCTURALLY -----------------------------------------------
//
// GoReflect.TypeNaming had no delegate handling at all, so a Go func type printed the CLR delegate
// family standing in for it (`Action`1`). A DEFINED func type keeps its own name, and the pair below
// is what separates the two rules.

// A defined func type keeps its Go name only when it has a MANAGED identity, and it acquires one
// exactly when it carries a method: a METHODLESS defined func type is rendered inline as its base
// delegate family and is indistinguishable from an unnamed one here — the residual this rule states
// rather than hides.
type handler func(int) error

func (h handler) call(n int) error { return h(n) }

func noArgs()                             {}
func oneIn(int)                           {}
func oneOut() error                       { return nil }
func twoOut() (int, error)                { return 0, nil }
func variadic(string, ...int)             {}
func mixed(a int, b string) (bool, error) { return false, nil }

// ---- 3. Value.Bytes / SetBytes over a DEFINED byte element --------------------------------------
//
// Go decides on the element KIND, not the element TYPE, and it ALIASES the storage. The bridge held
// `slice<definedByte>` of wrapper structs, an unrelated instantiation with no conversion to
// `slice<byte>`, so Bytes() threw out of a core reflect API; SetBytes stored through the Go data
// word the bridge never populates and wrote nowhere at all — for EVERY byte slice, plain []byte
// included.

type definedByte byte

type definedBytes []definedByte

type namedPlainBytes []byte

// A struct FIELD holding a pointer slice with a nil hole — encoding/json's `All.SliceP` shape.
type holder struct {
	Ps []*inner
}

// encoding/json's slice cycle key, verbatim: the data pointer paired with the length.
type sliceKey struct {
	ptr any
	len int
}

// ---- 4. new(T) is Go's ZERO value -----------------------------------------------------------------
//
// golib's container structs each declare a parameterless constructor that ALLOCATES, and `new(T)`
// ran it — so `new([]int)` and `new(map[string]int)` pointed at a non-nil EMPTY container where Go
// points at nil. Invisible until the two zero-fabrication paths met: reflect.New builds its zero by
// a different rule, so DeepEqual of the two was false. That comparison is the precondition
// encoding/json's whole TestUnmarshal table checks before every subtest.

func zeroAgrees[T any]() (bool, bool) {
	p := new(T)
	v := reflect.New(reflect.TypeOf(p).Elem())
	return reflect.DeepEqual(p, v.Interface()), reflect.DeepEqual(*p, v.Elem().Interface())
}

func main() {
	// ---- 1 ----
	// Looked up BY NAME rather than walked in index order, deliberately: a promoted embed's backing
	// field is emitted by go2cs-gen AFTER the declaring part's plain fields, so the projected field
	// ORDER puts every embed last where Go keeps declaration order. That is a separate, recorded gap
	// with no measured consumer (json's dominance rules read depth and tag, never order), and a guard
	// that asserted the current order would pin it as a contract.
	ht := reflect.TypeOf(host{})
	fmt.Println("fields:", ht.NumField())
	for _, name := range []string{"Plain", "inner", "tagged", "Named"} {
		f, ok := ht.FieldByName(name)
		fmt.Printf("field %s found=%v anonymous=%v tag=%q exported=%v type=%s\n", name, ok, f.Anonymous, string(f.Tag), f.IsExported(), f.Type)
	}

	// ---- 2 ----
	// A DEFINED func type is written as a declared value, not as a `handler(nil)` conversion: that
	// conversion is a SEPARATE, still-open converter gap (it emits `new Func<nint, error>(default!)`,
	// which does not compile) and this test must fail on the naming rule alone.
	var named handler = func(int) error { return nil }
	for _, f := range []any{noArgs, oneIn, oneOut, twoOut, variadic, mixed, named} {
		t := reflect.TypeOf(f)
		fmt.Printf("func %-34s name=%q variadic=%v\n", t.String(), t.Name(), t.IsVariadic())
	}
	fmt.Printf("%T | %T\n", mixed, named)

	// ---- 3 ----
	// A defined ELEMENT type, read through Bytes() and written back THROUGH the alias.
	custom := definedBytes{'h', 'i', '!'}
	cv := reflect.ValueOf(custom)
	got := cv.Bytes()
	fmt.Printf("bytes(definedBytes)=%q len=%d cap=%d\n", string(got), len(got), cap(got))
	got[2] = '?'
	fmt.Printf("alias write visible in source: %q\n", string([]byte{byte(custom[0]), byte(custom[1]), byte(custom[2])}))

	// A defined SLICE type over plain byte — the shape that already worked, kept as the control.
	plain := namedPlainBytes("ok")
	fmt.Printf("bytes(namedPlainBytes)=%q\n", string(reflect.ValueOf(plain).Bytes()))

	// An addressable byte ARRAY is Go's other Bytes() arm.
	arr := [3]byte{'a', 'b', 'c'}
	fmt.Printf("bytes([3]byte)=%q\n", string(reflect.ValueOf(&arr).Elem().Bytes()))

	// SetBytes into each slot shape, including a plain []byte (which wrote nowhere before).
	var dstPlain []byte
	reflect.ValueOf(&dstPlain).Elem().SetBytes([]byte("plain"))
	var dstNamed namedPlainBytes
	reflect.ValueOf(&dstNamed).Elem().SetBytes([]byte("named"))
	var dstCustom definedBytes
	reflect.ValueOf(&dstCustom).Elem().SetBytes([]byte("custom"))
	fmt.Printf("setbytes %q %q %d\n", string(dstPlain), string(dstNamed), len(dstCustom))

	// ---- 3b. an exported field reached THROUGH an unexported embed is SETTABLE ----
	//
	// Go's two read-only bits are not interchangeable: an unexported EMBED takes flagEmbedRO, an
	// unexported plain field flagStickyRO, and only the sticky one propagates to a child. Marking
	// an embed sticky made every promoted field read-only, and a decoder writing one panicked in
	// mustBeAssignable instead of filling it.
	var h host
	hv := reflect.ValueOf(&h).Elem()
	hv.FieldByName("X").SetInt(7)
	// FieldByName must SEARCH the embeds to find `X` at all — Go's breadth-first promoted lookup —
	// and the index it reports is a PATH, not a single field. Its length is asserted rather than its
	// contents, because the projected order of an embed is the recorded gap above.
	promoted, promotedOK := reflect.TypeOf(h).FieldByName("X")
	_, ambiguous := reflect.TypeOf(clash{}).FieldByName("Y")
	fmt.Println("promoted through an unexported embed:", h.inner.X, hv.FieldByName("X").CanSet(), promotedOK, len(promoted.Index), promoted.Name, ambiguous)
	// ...while the embed itself, and a plain unexported field, stay read-only exactly as in Go.
	fmt.Println("embed and plain unexported are read-only:", hv.FieldByName("inner").CanSet(), reflect.ValueOf(&nested{}).Elem().Field(0).CanSet())

	// ---- 3c. a nil pointer ELEMENT is one nil, however the slice was built ----
	//
	// A slice literal's `nil` element and the zero element `reflect.MakeSlice` fabricates must be
	// the same nil — Go has one — or DeepEqual separates two slices that print alike and every
	// decoded-vs-expected comparison over a `[]*T` with a nil hole fails.
	lit := []*inner{nil}
	built := reflect.MakeSlice(reflect.TypeOf(lit), 1, 1)
	fmt.Printf("nil element: %v %v deepequal=%v\n", lit, built.Interface(), reflect.DeepEqual(built.Interface(), lit))

	// ...and the shape a DECODER actually produces: a nil hole beside a real element, inside a
	// struct field written through reflect, compared against the same value written as a literal.
	want := holder{Ps: []*inner{{X: 1}, nil}}
	var target holder
	tv := reflect.ValueOf(&target).Elem().Field(0)
	grown := reflect.MakeSlice(tv.Type(), 2, 2)
	grown.Index(0).Set(reflect.ValueOf(&inner{X: 1}))
	tv.Set(grown)
	fmt.Printf("decoded vs literal: %v %v %v\n", reflect.DeepEqual(target, want), want.Ps[1], target.Ps[1])

	// ---- 4 ----
	sliceEq, sliceElemEq := zeroAgrees[[]any]()
	mapEq, mapElemEq := zeroAgrees[map[string]int]()
	arrEq, arrElemEq := zeroAgrees[[3]int]()
	strEq, strElemEq := zeroAgrees[inner]()
	fmt.Println("new==reflect.New:", sliceEq, sliceElemEq, mapEq, mapElemEq, arrEq, arrElemEq, strEq, strElemEq)

	ps := new([]int)
	pm := new(map[string]int)
	fmt.Println("new(T) is nil:", *ps == nil, *pm == nil, len(*ps), len(*pm))

	// The array LENGTH must survive TypeOf(*[N]T).Elem(), or reflect.New allocates the wrong one.
	pa := new([5]int)
	at := reflect.TypeOf(pa).Elem()
	fmt.Println("array type through a pointer:", at.String(), at.Len(), reflect.New(at).Elem().Len())

	// ---- 5. a NaN map key is never equal to anything, including itself ----
	//
	// Dictionary's default comparer reports NaN equal to NaN so a stored NaN can be found again;
	// Go applies `==` unchanged, so each insert makes a NEW entry and none can be read or deleted.
	nan := nanValue()
	nm := map[float64]int{}
	nm[nan] = 1
	nm[nan] = 2
	_, found := nm[nan]
	delete(nm, nan)
	fmt.Println("nan keys:", len(nm), found)

	// ---- 6. an unsafe.Pointer is compared BY ADDRESS ----
	//
	// The converter mints a fresh unsafe.Pointer box on every uintptr conversion, and a heap box
	// compares by reference — so a pointer used as a map key never found the entry it had itself
	// stored. That is exactly how Go's own cycle detectors are written (encoding/json's encoder
	// keys e.ptrSeen on v.UnsafePointer()), and without it a self-referential value recursed until
	// the process died instead of reporting a cycle.
	seen := map[any]struct{}{}
	backing := map[string]int{"a": 1}
	mv := reflect.ValueOf(backing)
	seen[mv.UnsafePointer()] = struct{}{}
	_, again := seen[reflect.ValueOf(backing).UnsafePointer()]
	sl := []int{1, 2, 3}
	sv := reflect.ValueOf(sl)
	seen[sv.UnsafePointer()] = struct{}{}
	_, slAgain := seen[reflect.ValueOf(sl).UnsafePointer()]
	_, other := seen[reflect.ValueOf(map[string]int{"b": 2}).UnsafePointer()]
	fmt.Println("pointer identity:", again, slAgain, other, len(seen))

	// ...and the SLICE flavour of the same key, which encoding/json's cycle detector uses verbatim:
	// a struct pairing the data pointer with the length, stored in a map[any]struct{}. It is a
	// separate shape because the pointer lands in an `any` FIELD rather than being the key itself.
	boxed := map[any]struct{}{}
	boxed[sliceKey{sv.UnsafePointer(), sv.Len()}] = struct{}{}
	_, keyAgain := boxed[sliceKey{reflect.ValueOf(sl).UnsafePointer(), len(sl)}]
	_, keyOther := boxed[sliceKey{reflect.ValueOf(sl).UnsafePointer(), len(sl) - 1}]
	fmt.Println("slice key identity:", keyAgain, keyOther, len(boxed))
}

// nanValue keeps the NaN out of a constant expression: Go rejects a NaN constant, and computing it
// here also keeps the two sides' arithmetic identical.
func nanValue() float64 {
	zero := 0.0
	return zero / zero
}
