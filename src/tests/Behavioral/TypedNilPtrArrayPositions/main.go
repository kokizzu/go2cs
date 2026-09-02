// TypedNilPtrArrayPositions guards the array-dims cargo at the positions its sibling
// TypedNilPtrArrayDims cannot see. That sibling drives every assertion through a CONVERSION --
// `(*[3]int)(nil)` -- which is the only position the original cargo covered, so its green said
// nothing about a nil that reaches a pointer-to-array target any other way.
//
// The gap was not hypothetical. reflect/all_test.go's TestValue_Cap and TestValue_Len assign nil
// to a local `*[3]int` and then ask reflect for its Cap/Len; Go answers 3 and the erased form
// answered 0, because `*[3]int` renders as `ж<array<nint>>` and `array<E>` does not carry the
// length. Unlike the conversion position nothing downstream can recover it -- by the time the
// value reaches `OrTypedNil()` at the reflection boundary the 3 is already gone -- so the
// dimension has to ride the value from wherever it is still statically known.
//
// FOUR POSITIONS, one per way a bare `nil` can meet such a target, each asserted through reflect
// because reflect is the only observer that can tell a dims-carrying nil from a plain one:
//
//	assign (local)   a = nil          the reflect rows' own shape
//	assign (field)   h.versym = nil   runtime/vdso_linux.go's shape
//	argument         takeArg(nil)     runtime's sigprocmask(_SIG_SETMASK, &m, nil) shape
//	result           return nil       archive/tar readHeader's `return nil, nil, err` shape
//
// The CONVERSION line is kept as an in-guard control: it passed before this change, so if it ever
// fails alongside the others the fault is in the shared cargo rather than in the new positions.
package main

import (
	"fmt"
	"reflect"
)

type Sigset [2]uint32

type holder struct{ versym *[3]uint16 }

func takeArg(p *Sigset) int { return reflect.ValueOf(p).Type().Elem().Len() }

func ret() *[5]byte { return nil }

func main() {
	// assign, local variable -- TestValue_Cap / TestValue_Len's exact shape
	a := &[3]int{1, 2, 3}
	a = nil
	fmt.Println("assign local  Len:", reflect.ValueOf(a).Len(), "Cap:", reflect.ValueOf(a).Cap())

	// assign, struct field
	var h holder
	h.versym = nil
	fmt.Println("assign field  elem len:", reflect.ValueOf(h.versym).Type().Elem().Len())

	// argument -- the callee observes the length through the parameter it was handed
	fmt.Println("argument      elem len:", takeArg(nil))

	// result
	fmt.Println("result        elem len:", reflect.ValueOf(ret()).Type().Elem().Len())

	// CONTROL: the conversion position, which was already covered
	fmt.Println("conversion    Len:", reflect.ValueOf((*[7]int)(nil)).Len())
}
