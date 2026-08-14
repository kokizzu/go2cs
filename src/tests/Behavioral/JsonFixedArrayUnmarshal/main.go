// Guards the Go `switch` lowering for a `default` clause that is NOT the last clause in
// source order. encoding/json's decode.go array() is the witness:
//
//	switch v.Kind() {
//	case reflect.Interface:
//		...
//		fallthrough
//	default:
//		d.saveError(&UnmarshalTypeError{Value: "array", ...})
//		...
//	case reflect.Array, reflect.Slice:
//		break
//	}
//
// Go picks `default` only when NO case matches, wherever it sits in source order. Lowered to a
// C# if-chain in source order, the default's `!match` guard could only see the arms emitted
// BEFORE it, so every Array and Slice target took the error arm and the arms after it were
// dead: `json: cannot unmarshal array into Go value of type [1]interface {}`.
//
// The cases below cover the fixed-size-array target that named the defect (net/rpc/jsonrpc
// passes params as [1]any), Go's truncate/zero-fill semantics for a length mismatch, the slice
// arm that shared the root, and — the other direction — the targets for which the `default`
// arm MUST still fire, so the fix cannot be an over-broad one that simply deletes the guard.
package main

import (
	"encoding/json"
	"fmt"
)

func main() {
	// The net/rpc/jsonrpc params shape: a JSON array into [1]any.
	var a [1]interface{}
	fmt.Println("iface1:", unmarshal("[42]", &a), a)

	// A typed fixed array whose length matches the JSON array exactly.
	var b [2]int
	fmt.Println("int2:", unmarshal("[7,8]", &b), b)

	// Over-length JSON: Go decodes what fits and discards the rest.
	var c [2]int
	fmt.Println("trunc:", unmarshal("[1,2,3,4]", &c), c)

	// Under-length JSON: Go zero-fills the remainder of the array.
	var d [3]string
	fmt.Println("zerofill:", unmarshal(`["x"]`, &d), d)

	// An empty JSON array zero-fills the whole array.
	var e [2]int
	e = [2]int{9, 9}
	fmt.Println("empty:", unmarshal("[]", &e), e)

	// Nested fixed arrays recurse through the same arm.
	var f [2][2]int
	fmt.Println("nested:", unmarshal("[[1,2],[3,4]]", &f), f)

	// A fixed array of structs, so the element arm is the object decoder.
	type point struct {
		X int `json:"x"`
		Y int `json:"y"`
	}
	var g [2]point
	fmt.Println("structs:", unmarshal(`[{"x":1,"y":2},{"x":3,"y":4}]`, &g), g)

	// The slice arm shared the defect's root — it sits after the same `default`.
	var h []int
	fmt.Println("slice:", unmarshal("[3,4,5]", &h), h)

	// A JSON array into a nil interface takes the `case reflect.Interface` arm ahead of the
	// default and never reaches it.
	var i interface{}
	fmt.Println("anyiface:", unmarshal("[1,2]", &i), i)

	// The `default` arm must STILL fire for a target that is neither interface, array nor
	// slice — the error text below is the arm the defect was stealing.
	var j int
	fmt.Println("badtarget:", unmarshal("[1,2]", &j), j)

	var k [2]int
	fmt.Println("badmap:", unmarshal(`{"a":1}`, &k), k)
}

func unmarshal(data string, v any) string {
	if err := json.Unmarshal([]byte(data), v); err != nil {
		return "err(" + err.Error() + ")"
	}
	return "ok"
}
