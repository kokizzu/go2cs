// NamedMapRvalueIndexWrite guards INDEX ASSIGNMENT through a named MAP type that arrives as an
// RVALUE — the value a method or function just returned.
//
// A Go named map type IS a reference, so `w.Header()[k] = v` is ordinary Go, and net/http's entire
// handler surface is written on it. go2cs emits a named map type as a struct wrapping golib's
// `map<K,V>`, and a C# struct's indexer SET on an rvalue receiver is CS1612 — "cannot modify the
// return value of 'X' because it is not a variable" — unless the member is declared readonly. The
// compiler's assumption (that the mutation would be lost to the temporary) is false here: the
// setter writes through the wrapped map, which the copy shares exactly as Go's map header does.
// Six of net/http's converted-test diagnostics were this one missing word.
//
// Reads through the same rvalue, comma-ok reads, len() and delete() were never affected: a method
// call on an rvalue struct is legal C#. They are exercised anyway so a remedy that reached the
// indexer by changing the TYPE's shape could not pass quietly.
package main

import "fmt"

type Header map[string][]string

type Counts map[string]int

type response struct {
	hdr Header
}

func (r *response) Header() Header { return r.hdr }

func newResponse() *response { return &response{hdr: Header{}} }

func main() {
	r := newResponse()

	// The defect's exact shape: index-assign through a METHOD-call rvalue.
	r.Header()["Content-Type"] = []string{"text/plain"}
	r.Header()["X-Trace"] = []string{"a", "b"}

	// The writes landed on the receiver's own map, not on a copy of the header.
	fmt.Println(r.hdr["Content-Type"], len(r.hdr["X-Trace"]))

	// A FUNC-call rvalue of the same shape, observed through the map the closure captured.
	c := Counts{}
	get := func() Counts { return c }

	get()["hits"] = 3
	get()["hits"] = get()["hits"] + 1

	fmt.Println(c["hits"], len(c))

	// Reads through an rvalue named map, comma-ok included.
	fmt.Println(r.Header()["Content-Type"][0])

	absent, ok := r.Header()["absent"]
	fmt.Println(absent == nil, ok)

	// len() and delete() over an rvalue named map.
	delete(r.Header(), "X-Trace")
	fmt.Println(len(r.Header()), len(r.hdr))
}
