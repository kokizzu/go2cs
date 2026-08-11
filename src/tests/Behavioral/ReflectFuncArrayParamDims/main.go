package main

import (
	"fmt"
	"reflect"
)

// A Go array's LENGTH is part of its type, and a func PARAMETER is the one position where the
// managed emission cannot carry it: `[32]byte` renders as golib array<byte> (C# has no const
// generic to hold the 32), and the delegate type is a bare Func<array<byte>, bool> shared by
// func([32]byte) bool and func([64]byte) bool alike. Every other position recovers the length from
// a live source — a value reveals its own, a struct field reads the declaring type's zero instance,
// because the converter emits the dimension as a field initializer — but a type-only parameter
// position has neither, so reflect.TypeOf(f).In(0) answered a DIMS-LESS array: Len() 0 and String()
// "[]uint8", which does not even read as an array. reflect.New/Zero of it then built a ZERO-length
// array, so testing/quick generated the empty value for every property test over a fixed-size array
// (edwards25519's TestScalarSetCanonicalBytes indexed in[len(in)-1] and panicked with index -1).
//
// The converter now stamps the dimension on the parameter as [GoArrayDims(32)] and the bridge reads
// it back off the delegate INSTANCE, stamping it as descriptor cargo in abi.TypeOf. The last block
// below is quick's generation loop in miniature — New(In(0)).Elem(), fill through Index(i).Set,
// Call — which is the shape that has to work, not merely the reported length.

type wrap struct {
	Buf [8]byte
}

func declared(in [16]byte) int { return len(in) }

func main() {
	f32 := func(in [32]byte) bool { return len(in) == 32 }
	f64 := func(in [64]byte, w wrap) int { return len(in) + len(w.Buf) }
	nested := func(in [2][3]int) int { return len(in) * len(in[0]) }
	plain := func(a int, s []byte) int { return a + len(s) }

	report("f32", reflect.TypeOf(f32))
	report("f64", reflect.TypeOf(f64))
	report("nested", reflect.TypeOf(nested))
	report("plain", reflect.TypeOf(plain))
	report("declared", reflect.TypeOf(declared))

	// [32]byte and [64]byte are DISTINCT Go types over ONE managed delegate shape, so the two
	// descriptors must not intern together — otherwise whichever arrived first would answer Len()
	// for both.
	fmt.Println("distinct in0 types:", reflect.TypeOf(f32).In(0) != reflect.TypeOf(f64).In(0))
	fmt.Println("same as itself:    ", reflect.TypeOf(f32).In(0) == reflect.TypeOf(f32).In(0))

	// The element of a nested array parameter keeps the INNER dimension.
	inner := reflect.TypeOf(nested).In(0).Elem()
	fmt.Println("nested elem:", inner, inner.Len())

	// A struct parameter's array FIELD already worked (the zero-instance route) and still does.
	field := reflect.TypeOf(f64).In(1).Field(0)
	fmt.Println("struct field:", field.Name, field.Type, field.Type.Len())

	// testing/quick's generation loop, in miniature.
	fmt.Println("generated call:", generateAndCall(reflect.ValueOf(f32)))
	fmt.Println("generated call:", generateAndCall(reflect.ValueOf(declared)))
}

func report(name string, t reflect.Type) {
	in0 := t.In(0)
	line := fmt.Sprintf("%-9s in0=%-10v kind=%-7v len=%d", name, in0, in0.Kind(), lenOf(in0))

	if in0.Kind() == reflect.Array {
		line += fmt.Sprintf(" new=%d zero=%d", reflect.New(in0).Elem().Len(), reflect.Zero(in0).Len())
	}

	fmt.Println(line)
}

// lenOf reports an array type's length and 0 for every other kind (reflect.Type.Len panics off an
// array/chan/map/slice, so the probe has to gate on the kind, exactly as quick's switch does).
func lenOf(t reflect.Type) int {
	if t.Kind() == reflect.Array {
		return t.Len()
	}
	return 0
}

// generateAndCall mirrors testing/quick's Value for an Array parameter: allocate the argument from
// the PARAMETER TYPE alone, fill every element, and invoke. It returns the callee's own answer, so
// a zero-length synthesis shows up as the wrong result rather than as a silent pass.
func generateAndCall(fn reflect.Value) []any {
	argType := fn.Type().In(0)
	arg := reflect.New(argType).Elem()

	for i := 0; i < arg.Len(); i++ {
		arg.Index(i).SetUint(uint64(i % 251))
	}

	out := fn.Call([]reflect.Value{arg})
	results := make([]any, len(out))

	for i, r := range out {
		results[i] = r.Interface()
	}

	return results
}
