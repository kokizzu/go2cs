package main

import (
	"fmt"
	"reflect"
)

// makeSwap is the reflect.MakeFunc documentation example: fptr points at a nil
// func variable; the made func returns its two arguments swapped.
func makeSwap(fptr any) {
	swap := func(in []reflect.Value) []reflect.Value {
		return []reflect.Value{in[1], in[0]}
	}
	fn := reflect.ValueOf(fptr).Elem()
	v := reflect.MakeFunc(fn.Type(), swap)
	fn.Set(v)
}

// traceHooks mirrors the net/http/httptrace ClientTrace shape whose compose()
// was reflect.MakeFunc's first operational consumer: func-typed struct fields
// composed pairwise via MakeFunc + Call.
type traceHooks struct {
	OnEvent func(string)
	Sum     func(int, int) (int, string)
}

func main() {
	// The docs swap example — made funcs invoked DIRECTLY as typed Go funcs.
	var intSwap func(int, int) (int, int)
	makeSwap(&intSwap)
	a, b := intSwap(3, 7)
	fmt.Println("intSwap:", a, b)

	var stringSwap func(string, string) (string, string)
	makeSwap(&stringSwap)
	s1, s2 := stringSwap("hello", "world")
	fmt.Println("stringSwap:", s1, s2)

	// The httptrace compose shape: both hooks must run, in order, through one
	// composed func value set back into the struct field.
	var events []string
	t1 := traceHooks{OnEvent: func(s string) { events = append(events, "new:"+s) }}
	t2 := traceHooks{OnEvent: func(s string) { events = append(events, "old:"+s) }}
	tv := reflect.ValueOf(&t1).Elem()
	ov := reflect.ValueOf(&t2).Elem()
	tf := tv.Field(0)
	of := ov.Field(0)
	tfCopy := reflect.ValueOf(tf.Interface())
	newFunc := reflect.MakeFunc(tf.Type(), func(args []reflect.Value) []reflect.Value {
		tfCopy.Call(args)
		return of.Call(args)
	})
	tv.Field(0).Set(newFunc)
	t1.OnEvent("dns")
	t1.OnEvent("connect")
	fmt.Println("events:", events)

	// Multi-return through a struct field's func type, called directly.
	h := traceHooks{}
	hv := reflect.ValueOf(&h).Elem()
	sumField := hv.Field(1)
	made := reflect.MakeFunc(sumField.Type(), func(args []reflect.Value) []reflect.Value {
		total := args[0].Int() + args[1].Int()
		return []reflect.Value{reflect.ValueOf(int(total)), reflect.ValueOf(fmt.Sprintf("sum=%d", total))}
	})
	sumField.Set(made)
	n, msg := h.Sum(4, 5)
	fmt.Println("sum:", n, msg)

	// The made Value's Type is CANONICALLY the type it was asked for.
	fmt.Println("type match:", made.Type() == sumField.Type())

	// Calling a made func through reflect.Call as well (the DynamicInvoke path
	// over the compiled delegate).
	out := made.Call([]reflect.Value{reflect.ValueOf(10), reflect.ValueOf(20)})
	fmt.Println("call:", out[0].Int(), out[1].String())

	// An interface-typed parameter arrives as a Kind Interface Value (Go's
	// static-slot rule), with the dynamic value behind Elem().
	var describe func(any) string
	dv := reflect.ValueOf(&describe).Elem()
	dv.Set(reflect.MakeFunc(dv.Type(), func(args []reflect.Value) []reflect.Value {
		arg := args[0]
		return []reflect.Value{reflect.ValueOf(fmt.Sprintf("%v/%v", arg.Kind(), arg.Elem().Kind()))}
	}))
	fmt.Println("describe:", describe(42))

	// A nil pointer argument is a VALID typed-nil Value, never the zero Value.
	var isNil func(*int) bool
	nv := reflect.ValueOf(&isNil).Elem()
	nv.Set(reflect.MakeFunc(nv.Type(), func(args []reflect.Value) []reflect.Value {
		return []reflect.Value{reflect.ValueOf(args[0].IsNil())}
	}))
	x := 5
	fmt.Println("isNil:", isNil(nil), isNil(&x))

	// A fixed-size array parameter keeps its length: [4]byte emits as a bare
	// array<byte>, so the length reaches the made func's argument only through
	// the descriptor's per-parameter dims cargo.
	var sum4 func([4]byte) int
	sv := reflect.ValueOf(&sum4).Elem()
	sv.Set(reflect.MakeFunc(sv.Type(), func(args []reflect.Value) []reflect.Value {
		total := 0
		for i := 0; i < args[0].Len(); i++ {
			total += int(args[0].Index(i).Uint())
		}
		return []reflect.Value{reflect.ValueOf(total)}
	}))
	fmt.Println("sum4:", sum4([4]byte{1, 2, 3, 4}))
}
