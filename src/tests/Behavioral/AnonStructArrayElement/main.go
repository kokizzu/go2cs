package main

import "fmt"

// Anonymous struct as an ARRAY-ELEMENT field type — runtime's MemStats.BySize pattern. The element
// must be lifted to a named type so the field declaration compiles as `array<Stats_BySize>` rather
// than a raw, un-compilable `array<struct{…}>`. (Validated by the Compile/Target phases; its
// zero-value array allocation is a separate generator concern, so its elements are not indexed.)
type Stats struct {
	Total  int
	BySize [3]struct {
		Size  uint32
		Count uint64
	}
}

// Anonymous struct as the element of a package-global array var — runtime's stackpool pattern. The
// declaration must resolve to the lifted element type and be allocated. The byte-array sub-field
// `pad` exercises the element-type rendering (previously mis-rendered as `<4>byte`); it is not
// indexed at runtime (array elements are default-constructed, so a nested array field is unallocated).
var pool [2]struct {
	item int
	pad  [4]byte
}

// Plain fixed-size array globals must be allocated (`new(N)`); indexing an uninitialized array
// global previously NRE'd at runtime.
var nums [3]int
var addr [2]int

// The SAME struct-field arm, one composition level deeper. The arm peeled a field's declared type by
// hand, one container level per kind, so `[N]struct{…}` lifted while `[N]*struct{…}`, `[]*struct{…}`
// and `map[K]struct{…}` fell straight through and the field declaration carried the raw, un-compilable
// Go `struct{…}` text into C#. Every field below is a shape that arm could not see; `Stats.BySize`
// above is the one-level control that must stay byte-identical.
type Composed struct {
	Ptrs  [2]*struct{ Size uint32 }
	Slice []*struct{ Name string }
	ByKey map[string]struct{ Count int }

	// The interface side of the same arm: a map VALUE is not a container kind it peeled either.
	Tagged map[string]interface{ Tag() string }
}

var composed Composed

// Reads every composed field back THROUGH its lifted type, so a lift that produced no named type
// (or the wrong one) cannot pass by compiling alone: `composed.ByKey[…].Count` only binds if
// `Composed_ByKey` is a real C# struct carrying the field.
//
// Deliberately reads the ZERO values rather than populating the fields. Constructing a value of an
// anonymous struct type lifts a SECOND, function- or file-scoped name for the same Go type (the
// recorded cross-context anonymous-lift identity split, unchanged by this arm and present for the
// one-level `[N]struct{…}` shape too), and a container of it — `slice<ж<A>>` to `slice<ж<B>>` — has
// no implicit conversion to bridge them. That residual is its own increment; the field arm's job,
// and all this guards, is that the field DECLARATION resolves to a named type at all.
func composedReads() (bool, int, int, bool) {
	return composed.Ptrs[0] == nil,
		len(composed.Slice),
		composed.ByKey["absent"].Count,
		composed.Tagged["absent"] == nil
}

// The PARENTHESIZED pointer conversion of an anonymous struct — encoding/gob's
// `bootstrapType("_reserved1", (*struct{ r7 int })(nil))`. It reaches its literal through a paren
// hop that no hand-written one-level peel looked for; it is a positive control for the shared
// descent, held here so the whole class has one guard.
func reservedIsNil() bool { return (*struct{ r7 int })(nil) == nil }

// Reference Stats so it (and its lifted Stats_BySize element type) must compile; reads only the
// scalar field, independent of the (separate) zero-value array-element allocation.
func statsTotal() int { s := Stats{Total: 42}; return s.Total }

// An ESCAPING local whose type is an anonymous-struct composite literal (its address is taken, so
// it is heap-boxed). The box declaration `ref var x = ref heap<T>(…)` renders its type explicitly,
// which must resolve to the lifted name rather than a raw `struct{…}` — runtime's mpagealloc
// `firstFree := struct{…}{…}` pattern (the local is used within the function, not passed to a
// distinctly-lifted anonymous-struct parameter — that cross-context identity is a separate concern).
func localHeapAnon() int {
	firstFree := struct {
		base, bound int
	}{
		base:  3,
		bound: 7,
	}
	p := &firstFree
	p.bound = 10
	return firstFree.base + firstFree.bound
}

func main() {
	pool[0].item = 5
	pool[1].item = 6

	nums[0] = 11
	nums[2] = 13

	// Addressed array global — must box a real N-sized array, not the empty default.
	p := &addr
	p[0] = 21
	p[1] = 22

	fmt.Println(pool[0].item, pool[1].item)
	fmt.Println(nums[0], nums[2], addr[0], addr[1])
	fmt.Println(statsTotal())
	fmt.Println(localHeapAnon())

	noPtr, sliceLen, count, noTag := composedReads()
	fmt.Println(noPtr, sliceLen, count, noTag)
	fmt.Println(reservedIsNil())
}
