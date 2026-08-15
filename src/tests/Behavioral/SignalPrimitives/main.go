package main

import (
	"fmt"
	"os"
	"os/signal"
)

// Exercises the five os/signal runtime primitives that runtime/sigqueue.go pushes across the
// //go:linkname boundary, plus signalWaitUntilIdle, WITHOUT needing a console control event:
// every transition below is observable through Ignored alone.
//
// Before the pushes were honored each of these calls was a PartialStubGenerator stub and the very
// first line threw. Stop is the deepest of them: it calls signalWaitUntilIdle, which spins until
// the watcher goroutine's signal_recv has reached the sigReceiving state — so reaching the last
// line at all proves the blocking receive really parks rather than throwing or spinning forever.
func main() {
	fmt.Println("initially ignored:", signal.Ignored(os.Interrupt))

	signal.Ignore(os.Interrupt)
	fmt.Println("after Ignore:", signal.Ignored(os.Interrupt))

	c := make(chan os.Signal, 1)
	signal.Notify(c, os.Interrupt)
	fmt.Println("after Notify:", signal.Ignored(os.Interrupt))

	signal.Stop(c)
	fmt.Println("after Stop:", signal.Ignored(os.Interrupt))

	signal.Ignore(os.Interrupt)
	fmt.Println("after Ignore again:", signal.Ignored(os.Interrupt))

	signal.Reset(os.Interrupt)
	fmt.Println("after Reset:", signal.Ignored(os.Interrupt))
}
