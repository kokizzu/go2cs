package main

import "fmt"

// Guards the defer/go-of-variadic-callee class: a deferred (or spawned) ZERO-ARG call of a
// variadic function must not collapse back into a method group — the C# form of a variadic
// callee always carries the params parameter, so its group converts to no Action and the
// emission is CS1503 (found on os/signal's `defer Reset()`, 2026-08-27). The with-args forms
// ride the temp-parameter ladder and are pinned here so the two routes stay in agreement, and
// the pointer-receiver variant covers the box-method-group path, which hands over the same
// inconvertible group.

var order []string

func note(tags ...string) {
	if len(tags) == 0 {
		order = append(order, "note()")
		return
	}
	for _, t := range tags {
		order = append(order, "note("+t+")")
	}
}

type counter struct{ n int }

func (c *counter) bump(deltas ...int) {
	if len(deltas) == 0 {
		c.n++
		return
	}
	for _, d := range deltas {
		c.n += d
	}
}

func run() *counter {
	c := &counter{}
	// The defect shape: zero-arg call of a variadic func.
	defer note()
	// The temp-parameter ladder shape, pinned alongside.
	defer note("a", "b")
	// The box-method-group shape: zero-arg variadic method on a pointer receiver.
	defer c.bump()
	defer c.bump(2, 3)
	order = append(order, "body")
	return c
}

var defaultDone = make(chan struct{})

func signalDone(chans ...chan struct{}) {
	if len(chans) == 0 {
		close(defaultDone)
		return
	}
	for _, ch := range chans {
		close(ch)
	}
}

func main() {
	c := run()
	for _, s := range order {
		fmt.Println(s)
	}
	fmt.Println("counter:", c.n)

	// The same class at the go statement, both arities.
	go signalDone()
	<-defaultDone
	fmt.Println("go variadic zero-arg done")

	ch := make(chan struct{})
	go signalDone(ch)
	<-ch
	fmt.Println("go variadic with-arg done")
}
