package main

// An interface-to-interface assignment over a POINTER-sourced interface value stacks TWO generated
// wrappers: an IInterfaceAdapter holding an IжAdapter holding the receiver box. Asserting the
// chained value back to the concrete pointer type has to unwrap both layers, and golib gates both
// adapter tiers behind a single IGoAdapter probe -- so the gate must be re-read after the
// intermediate unwrap, since what an adapter yields need not itself be an adapter.
//
// The value-sourced chain below is the other half: there the inner value is a plain boxed struct,
// the gate must go FALSE after the unwrap, and the pointer assert must miss while the concrete
// assert still resolves.

import "fmt"

type reader interface{ Read() string }

type readCloser interface {
	Read() string
	Close() string
}

type conn struct{ data string }

func (c *conn) Read() string  { return "read:" + c.data }
func (c *conn) Close() string { return "closed:" + c.data }

type valConn struct{ tag string }

func (v valConn) Read() string  { return "vread:" + v.tag }
func (v valConn) Close() string { return "vclosed:" + v.tag }

func main() {
	// pointer-sourced interface value
	var rc readCloser = &conn{data: "init"}

	// interface-to-interface: wraps the pointer-sourced value again
	var r reader = rc

	// assert the CHAINED value back to the concrete pointer type -- both layers must unwrap
	if p, ok := r.(*conn); ok {
		p.data = "mutated"
		fmt.Println("chain assert ok:", p.Read())
	} else {
		fmt.Println("chain assert MISSED")
	}

	// the write through the asserted pointer aliases the original object
	fmt.Println("through chain:", r.Read())
	fmt.Println("through source:", rc.Close())

	// a type switch over the chained value matches the pointer case
	switch v := r.(type) {
	case *conn:
		fmt.Println("switch *conn:", v.data)
	default:
		fmt.Println("switch default")
	}

	// value-sourced chain: the inner value is not a pointer wrapper
	var vrc readCloser = valConn{tag: "v1"}
	var vr reader = vrc

	if _, ok := vr.(*conn); ok {
		fmt.Println("value chain WRONGLY asserted *conn")
	} else {
		fmt.Println("value chain misses *conn:", vr.Read())
	}

	if v, ok := vr.(valConn); ok {
		fmt.Println("value chain asserts valConn:", v.tag)
	} else {
		fmt.Println("value chain MISSED valConn")
	}

	switch v := vr.(type) {
	case *conn:
		fmt.Println("value switch WRONGLY matched *conn:", v.data)
	case valConn:
		fmt.Println("value switch valConn:", v.tag)
	default:
		fmt.Println("value switch default")
	}

	// a nil interface still asserts as a clean miss through the same gate
	var empty reader
	if _, ok := empty.(*conn); ok {
		fmt.Println("nil interface WRONGLY asserted")
	} else {
		fmt.Println("nil interface misses")
	}
}
