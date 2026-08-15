// SliceElementFieldAddress guards taking the address of a FIELD of a slice or array
// ELEMENT — `&s[i].f` — and WRITING through it. The address must be built on the
// element-aliasing form (`Ꮡ(s, i)` for a slice, `.at<E>(i)` for an array) and then
// field-ref'd, `Ꮡ(s, i).of(T.Ꮡf)`. The regression it locks out is the `Ꮡ(s[i]).of(T.Ꮡf)`
// fallback, which boxes a COPY of the element: the field ref then aliases the copy and
// every write through the pointer is silently dropped, while all the reads still look
// right — the container simply never changes.
//
// It is the deliberate MIRROR of SliceFieldElementAddress, and the two are easy to
// confuse, so: that one takes the address of an ELEMENT of a slice FIELD (`&g.rows[i]`,
// tabwriter's empty lines); this one takes the address of a FIELD of an ELEMENT
// (`&p.Inst[pc].Out`).
//
// Live original: regexp's onePassCopy, which patches a compiled program in place through
// `p_A_Other := &p.Inst[pc].Out` / `p_B_Alt := &p.Inst[*p_A_Alt].Out`, swaps the two
// pointers, and then assigns through them. With the writes going into copies, two of the
// rewrites that make a program one-pass never landed and TestCompileOnePass reported
// `isOnePass=false` for `^(?:(?:a+)*)$` and `^(?:(?:(?:a*)+))$`.
//
// The PROMOTED case (the field belongs to an embedded struct) was masked until go2cs-gen
// made an embed an inline field rather than a shared `ж<T>` box: while the embed carried
// reference semantics, a copied element still pointed at the origin's embedded storage,
// so the write reached the real element by accident. Both cases are exercised here, along
// with an ordinary (non-embedded) field, which was never masked.
package main

import "fmt"

// header is embedded, so Out and Arg reach inst PROMOTED.
type header struct {
	Out uint32
	Arg uint32
}

type inst struct {
	header
	Name string // ordinary, non-promoted field of the same element
}

type prog struct {
	Inst []inst
}

type cell struct{ n, m int }

//go:noinline
func setU32(p *uint32, v uint32) { *p = v }

//go:noinline
func setInt(p *int, v int) { *p = v }

func main() {
	// 1. Ordinary field of an element of a slice LOCAL.
	cells := make([]cell, 3)
	p := &cells[1].n
	*p = 7
	setInt(&cells[2].m, 9)
	fmt.Println("local slice:", cells[1].n, cells[2].m)

	// 2. Ordinary field of an element of an ARRAY local.
	var arr [3]cell
	q := &arr[2].n
	*q = 11
	setInt(&arr[0].m, 13)
	fmt.Println("local array:", arr[2].n, arr[0].m)

	// 3. PROMOTED field of an element of a slice FIELD, reached through a pointer —
	//    regexp's exact shape.
	pr := &prog{Inst: make([]inst, 4)}
	for i := range pr.Inst {
		pr.Inst[i].Name = fmt.Sprintf("i%d", i)
	}
	a := &pr.Inst[0].Out
	b := &pr.Inst[0].Arg
	*a = 100
	*b = 200
	setU32(&pr.Inst[1].Out, 300)
	fmt.Println("promoted:", pr.Inst[0].Out, pr.Inst[0].Arg, pr.Inst[1].Out)

	// 4. The onePassCopy idiom: two pointers into the SAME element, swapped, then
	//    assigned through. Both must still name the live element after the swap.
	x := &pr.Inst[2].Out
	y := &pr.Inst[2].Arg
	x, y = y, x
	*x = 41
	*y = 42
	fmt.Println("swapped:", pr.Inst[2].Out, pr.Inst[2].Arg)

	// 5. Cross-element assignment through pointers, the way onePassCopy patches one
	//    instruction from another (`*p_B_Alt = *p_A_Other`).
	pr.Inst[3].Out = 55
	src := &pr.Inst[3].Out
	dst := &pr.Inst[1].Arg
	*dst = *src
	fmt.Println("patched:", pr.Inst[1].Arg, pr.Inst[3].Out)

	// 6. Ordinary field of the same element, to prove the promoted and non-promoted
	//    projections both land in one storage.
	n := &pr.Inst[3].Name
	*n = "patched"
	fmt.Println("name:", pr.Inst[3].Name)
}
