// CaptureHoistThroughConversion guards the capture-copy prologue's STATEMENT position.
//
// A func literal that captures a variable needing a snapshot (slice/map/chan/struct/array)
// emits `var xʗ1 = x;` before the enclosing statement. Two positions used to have no hoist
// target and wrote that declaration INSIDE an argument list — invalid C#:
//
//  1. A capturing literal wrapped in a NAMED-FUNC-TYPE conversion. The conversion renders as
//     C# delegate creation (`new Handler(<literal>)`) and rendered its operand with no
//     contexts at all, so the wrapper hid the literal from the `go`/`defer`/`return` hoist.
//  2. A channel SEND, which supplied no statement hoist sink of any kind — so even a BARE
//     capturing literal sent to a channel emitted its snapshot inline.
//
// Both shapes are net/http's: `go Serve(ls, HandlerFunc(func(rw, req){ … conn … }))`
// (serve_test) and `handlerc <- HandlerFunc(func(w, r){ … ts … })` (client_test). Between
// them they were the whole 28-diagnostic parse wall that kept net/http's 1,352-verdict suite
// from ever running.
package main

import "fmt"

// Handler is a NAMED FUNC TYPE — its C# emission is a delegate declaration, so a conversion
// to it renders as delegate creation rather than a cast.
type Handler func(int)

// Do makes Handler satisfy Iface, so a converted value can also cross an interface parameter —
// net/http's Serve(l, HandlerFunc(...)) shape exactly.
func (h Handler) Do(i int) { h(i) }

type Iface interface{ Do(int) }

var out = make(chan string, 16)

func serve(i Iface, n int) { i.Do(n) }

func run(h Handler, n int) { h(n) }

func runBare(f func(int), n int) { f(n) }

func makeHandler(g []string) Handler {
	return Handler(func(i int) { out <- fmt.Sprint(g[0], i) })
}

func main() {
	// (a) `go` + named-func-type conversion wrapping a CAPTURING literal, interface param.
	a := []string{"a="}
	go serve(Handler(func(i int) { out <- fmt.Sprint(a[0], i) }), 10)
	fmt.Println(<-out)

	// (b) `go` + named-func-type conversion, concrete named-func param.
	b := []string{"b="}
	go run(Handler(func(i int) { out <- fmt.Sprint(b[0], i) }), 20)
	fmt.Println(<-out)

	// (c) channel SEND of a named-func-type-wrapped capturing literal.
	hc := make(chan Handler, 1)
	c := []string{"c="}
	hc <- Handler(func(i int) { out <- fmt.Sprint(c[0], i) })
	(<-hc)(30)
	fmt.Println(<-out)

	// (d) channel SEND of a BARE capturing literal — no conversion wrapper at all.
	fc := make(chan func(int), 1)
	d := []string{"d="}
	fc <- func(i int) { out <- fmt.Sprint(d[0], i) }
	(<-fc)(40)
	fmt.Println(<-out)

	// (e) channel SEND into an INTERFACE-element channel of a wrapped capturing literal.
	ic := make(chan Iface, 1)
	e := []string{"e="}
	ic <- Handler(func(i int) { out <- fmt.Sprint(e[0], i) })
	(<-ic).Do(50)
	fmt.Println(<-out)

	// (f) PLAIN call statement with a wrapped capturing literal (already-working control).
	f := []string{"f="}
	run(Handler(func(i int) { out <- fmt.Sprint(f[0], i) }), 60)
	fmt.Println(<-out)

	// (g) RETURN position, wrapped capturing literal (see makeHandler).
	makeHandler([]string{"g="})(70)
	fmt.Println(<-out)

	// (h) `defer` + named-func-type conversion wrapping a capturing literal — NESTED, so the
	// snapshot is of the enclosing lambda's own capture name.
	h := []string{"h="}
	func() {
		defer run(Handler(func(i int) { out <- fmt.Sprint(h[0], i) }), 80)
	}()
	fmt.Println(<-out)

	// (i) ASSIGNMENT of a wrapped capturing literal (already-working control).
	i9 := []string{"i="}
	hv := Handler(func(i int) { out <- fmt.Sprint(i9[0], i) })
	hv(90)
	fmt.Println(<-out)

	// (j) `go` with a BARE capturing literal argument (already-working control).
	j := []string{"j="}
	go runBare(func(i int) { out <- fmt.Sprint(j[0], i) }, 100)
	fmt.Println(<-out)
}
