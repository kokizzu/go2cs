package main

import "fmt"

// A DEFINED type over string. Go converts an untyped string constant to it implicitly, but the
// emitted C# has to cross two user-defined conversions to get there — C# string -> golib @string ->
// the type's [GoType("@string")] wrapper — and C# admits only one in a conversion. So a bare "" in
// a result position has no conversion to the named type at all.
type Network string

type Label string

// The MULTI-result shape, which is where this first bit: the failed element leaves its tuple
// siblings with no target type either, so `nil` beside it fails as well. This mirrors os's
// getPollFDAndNetwork returning (*poll.FD, poll.String).
func lookup(ok bool) (*int, Network) {
	if !ok {
		return nil, ""
	}
	n := 7
	return &n, "tcp"
}

// The SINGLE-result shape.
func single(ok bool) Label {
	if !ok {
		return ""
	}
	return "named"
}

// A named string type reached through an ALIAS is the same type, so it takes the same route.
type Alias = Network

func viaAlias(ok bool) Alias {
	if !ok {
		return ""
	}
	return "aliased"
}

func main() {
	p0, net0 := lookup(false)
	p1, net1 := lookup(true)

	fmt.Println("zero tuple:", p0 == nil, string(net0) == "", len(net0))
	fmt.Println("set tuple:", *p1, string(net1), len(net1))

	fmt.Println("single zero:", string(single(false)) == "", len(single(false)))
	fmt.Println("single set:", string(single(true)))

	fmt.Println("alias zero:", string(viaAlias(false)) == "")
	fmt.Println("alias set:", string(viaAlias(true)))

	// The named type still behaves as a string: comparison, concatenation and conversion.
	var acc Network
	acc = acc + "a"
	acc += "b"

	fmt.Println("accumulated:", string(acc), acc == "ab", len(acc))
}
