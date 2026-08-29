package main

import (
	"fmt"
	"reflect"
)

// Holder's Done field is a directional channel (`<-chan struct{}`) -- the converter emits its
// zero value as a field initializer (`channel<EmptyStruct> Done = channel<EmptyStruct>.RecvOnly`),
// since the direction is TYPE cargo the field's declared type carries, not something a bare
// `default` can express. A named-field composite literal that omits Done (a) and the
// parameterless `new` path (b) must produce the SAME value -- the shape net/http's
// Request.Cancel broke: the generated named-argument constructor's parameter default silently
// overwrote the field initializer's direction stamp with an unstamped default.
type Holder struct {
	Name string
	Done <-chan struct{}
}

func main() {
	a := Holder{Name: "a"}
	b := new(Holder)
	b.Name = "a"

	fmt.Println(reflect.DeepEqual(a, *b))
	fmt.Printf("%T\n", a.Done)
	fmt.Printf("%T\n", b.Done)
}
