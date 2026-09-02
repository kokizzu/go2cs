// DescriptorCarrierFieldName guards the DESCRIPTOR CARRIER at a struct-FIELD position.
//
// A Go DEFINED type over a NAMED interface is emitted as a C# `global using` alias, because it has
// exactly that interface's method set and can carry no methods of its own. A `using` alias is a
// COMPILE-TIME construct that leaves no metadata, so the Go name was erased and
// reflect.Type.Field(i).Type.Name() answered "" for the empty-interface case and the TARGET
// interface's name — a different Go type's — for the non-empty one.
//
// Four fields, and two of them are NEGATIVE controls that must not move:
//
//	E  defined over the EMPTY interface      -> was Name="" / "interface {}"
//	N  defined over a NAMED NON-EMPTY iface  -> was Name="Stringer" / "fmt.Stringer" (WRONG name)
//	R  an INLINE interface definition        -> already a real C# interface; must stay correct
//	A  a true Go type ALIAS                  -> Go reports the TARGET's name, so a carrier here
//	                                            would INVENT a wrong name; must stay fmt.Stringer
package main

import (
	"fmt"
	"reflect"
)

type eface any               // class (i): defined over the empty interface
type namedIface fmt.Stringer // class (ii): defined over a named NON-empty interface
type realIface interface {   // control: an inline definition is a real C# interface already
	Do()
}
type aliasIface = fmt.Stringer // control: a true Go alias must NOT gain a carrier

type holder struct {
	E eface
	N namedIface
	R realIface
	A aliasIface
}

func main() {
	t := reflect.TypeFor[holder]()

	for i := 0; i < t.NumField(); i++ {
		f := t.Field(i)
		fmt.Printf("%s Name=%q String=%q PkgPath=%q Kind=%v\n",
			f.Name, f.Type.Name(), f.Type.String(), f.Type.PkgPath(), f.Type.Kind())
	}
}
