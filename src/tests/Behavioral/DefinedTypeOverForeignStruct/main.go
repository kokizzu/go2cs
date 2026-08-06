// A defined type whose underlying is a struct in ANOTHER package — `type alias ptlike.Outer`,
// the shape index/suffixarray's `type index Index` takes once the package under test and its
// white-box test files land in separate assemblies. Go gives the defined type the underlying
// struct's field set, so every selection below is legal Go; in C# the type is a `[GoType(...)]`
// wrapper whose members go2cs-gen forwards onto the wrapped value.
//
// Two things this guards, both of which were broken:
//   - the underlying struct is reached only as compiled METADATA (a <ProjectReference> is not a
//     CompilationReference), so the syntax-based resolution found nothing and NO members were
//     forwarded at all — every selection was CS1061;
//   - a forwarded member must be a VARIABLE, not a value: `a.In.Len()` binds a `this ref`
//     receiver and `a.In.Push` takes its address, which a get/set property cannot satisfy (CS0206).
package main

import (
	"fmt"

	"DefinedTypeOverForeignStruct/ptlike"
)

type alias ptlike.Outer

// A method on the defined type reads the forwarded members through its own receiver.
func (a *alias) describe() string {
	return fmt.Sprintf("%s/%d", a.Name, a.In.Len())
}

func main() {
	a := alias(ptlike.New("first", 1, 2, 3))

	// Read a forwarded plain field, and a method on a forwarded named-struct field.
	fmt.Println(a.Name, a.In.Len())
	fmt.Println(a.describe())

	// Write through a forwarded plain field.
	a.Name = "second"
	fmt.Println(a.Name)

	// Pointer-receiver method through a forwarded field: the mutation must persist in the
	// wrapper's own storage, not in a copy of it.
	a.In.Push(4)
	a.In.Push(5)
	fmt.Println(a.In.Len(), a.In.Vals)
	fmt.Println(a.describe())

	// Direct write to a nested field of a forwarded member.
	a.In.Vals[0] = 99
	fmt.Println(a.In.Vals)

	// Conversion back to the underlying type carries the mutations.
	o := ptlike.Outer(a)
	fmt.Println(o.Name, o.In.Vals)

	// The zero value of the defined type is the underlying's zero value.
	var z alias
	fmt.Println(z.Name == "", z.In.Len())
}
