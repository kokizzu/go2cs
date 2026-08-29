package main

import (
	"fmt"
	"net"
	"reflect"
)

// net's TestResolveIPAddr, reduced to the exact rows that fail, against the REAL package.
//
// The divergence is ONE property: reflect.DeepEqual(err, want) is true in Go and false in the
// converted build, while ==, the error text, and both type NAMES all agree. DeepEqual's first act
// is `if v1.Type() != v2.Type() { return false }`, and comparing type NAMES is not comparing type
// IDENTITY -- so these rows separate the two.
func main() {
	rows := []struct {
		network string
		addr    string
		want    error
	}{
		{"l2tp", "127.0.0.1", net.UnknownNetworkError("l2tp")},
		{"l2tp:gre", "127.0.0.1", net.UnknownNetworkError("l2tp:gre")},
		{"tcp", "1.2.3.4:123", net.UnknownNetworkError("tcp")},
	}
	for _, r := range rows {
		addr, err := net.ResolveIPAddr(r.network, r.addr)
		fmt.Printf("ResolveIPAddr(%q):\n", r.network)
		fmt.Printf("   addr nil:                  %v\n", addr == nil)
		fmt.Printf("   err text equal:            %v\n", err != nil && err.Error() == r.want.Error())
		fmt.Printf("   err == want:               %v\n", err == r.want)
		fmt.Printf("   DeepEqual(err,want):       %v\n", reflect.DeepEqual(err, r.want))
		fmt.Printf("   TypeOf(err).String():      %v\n", reflect.TypeOf(err).String())
		fmt.Printf("   TypeOf(want).String():     %v\n", reflect.TypeOf(r.want).String())
		fmt.Printf("   TypeOf(err)==TypeOf(want): %v\n", reflect.TypeOf(err) == reflect.TypeOf(r.want))
		fmt.Printf("   ValueOf(err).Kind():       %v\n", reflect.ValueOf(err).Kind().String())
		fmt.Printf("   ValueOf(want).Kind():      %v\n", reflect.ValueOf(r.want).Kind().String())
		fmt.Printf("   TypeOf(want).Kind():       %v\n", reflect.TypeOf(r.want).Kind().String())
		fmt.Printf("   ValueOf(err).String():     %q\n", reflect.ValueOf(err).String())
		fmt.Printf("   ValueOf(want).String():    %q\n", reflect.ValueOf(r.want).String())
	}
}
