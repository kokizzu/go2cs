// Regression test: a conversion whose TARGET is an UNNAMED map type and whose operand is nil —
// `map[string]int(nil)`.
//
// `T(x)` is a call in Go's grammar but a conversion in meaning, and go2cs forks it off in
// convCallExpr via isTypeConversion. For a composite type LITERAL target that claim was gated on
// the target and the argument sharing an underlying type; untyped nil's underlying is itself, so
// the shape was never claimed and fell through to the regular CALL path — which emitted
// `map<@string, nint>(default!)`, i.e. an INVOCATION of a type: CS1955, "non-invocable member
// 'map<TKey, TValue>' cannot be used like a method". The whole package failed to compile.
//
// The NAMED twin `myMap(nil)` was always correct (types.ConvertibleTo holds for it, so the
// general named path claimed it and emitted a cast), which is why only the type-literal spelling
// broke and why the named forms below are the control: both spellings must reach a cast.
//
// The shape is the reflection descriptor idiom `reflect.TypeOf(map[K]V(nil))`, which Go's own
// suites lean on — fmt's `{"%#v", map[int]byte(nil), …}` table row, reflect's TypeOf/DeepEqual
// tables, encoding/json's encode table, internal/reflectlite. It appears in no standard library
// PRODUCTION source, so the corpus never showed it; it is a converted-test and end-user shape.
//
// Sibling nil-able type literals are deliberately present as controls rather than as subjects:
// `[]byte(nil)` binds golib's real `builtin.slice<T>(T[])` conversion helper (the same helper
// `[]byte("…")` is emitted against) and `(chan T)(nil)` already renders as a cast, so both were
// always correct and both must stay on the routes they are on.
package main

import (
	"fmt"
	"reflect"
)

type myMap map[string]int
type mySlice []byte

func main() {
	// The subject: unnamed map type literals converting an untyped nil.
	fmt.Println(reflect.TypeOf(map[string]int(nil)))
	fmt.Println(reflect.TypeOf(map[int]byte(nil)))
	fmt.Println(reflect.TypeOf(map[string][]Header(nil)))
	fmt.Println(reflect.TypeOf(map[Key]Header(nil)))

	// The converted nil must BE nil, not merely typed: an empty map and a nil map are
	// distinguishable in Go and the conversion must land on the latter.
	nilMap := map[string]int(nil)
	fmt.Println("len:", len(nilMap), "isNil:", nilMap == nil)
	fmt.Println("read:", nilMap["absent"])

	// Controls — the NAMED map twin, and the sibling type literals that were never broken.
	fmt.Println(reflect.TypeOf(myMap(nil)))
	fmt.Println(reflect.TypeOf(mySlice(nil)))
	fmt.Println(reflect.TypeOf([]byte(nil)))
	fmt.Println(reflect.TypeOf((chan int)(nil)))
	fmt.Println(reflect.TypeOf((*int)(nil)))

	// A non-nil operand through the same unnamed-map target, which always took the cast route.
	populated := map[string]int{"a": 1}
	fmt.Println("copy len:", len(map[string]int(populated)))
}

type Key struct{ K string }

type Header struct{ N string }
