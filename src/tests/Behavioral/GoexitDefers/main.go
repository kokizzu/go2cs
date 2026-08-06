// Regression test: runtime.Goexit terminates ONLY the calling goroutine, and it does so the way
// Go specifies — every deferred call of that goroutine runs, across frames, and recover() inside
// those defers returns nil because a Goexit is not a panic and a defer cannot cancel it.
//
// The managed shape is a golib GoexitException: deliberately NOT a PanicException, so recover()
// (whose implementation keys on PanicException) is blind to it BY CONSTRUCTION, while GoFunc's
// finally-based defer machinery still pops the defer stack during the unwind. The goroutine root
// (golib.Goroutine) swallows it, ending that goroutine and no other — a PanicException reaching
// the same point keeps its process-killing path (guarded separately by GoroutinePanicExitCode).
//
// Deterministic by construction: no sleeps and no timing. Every cross-goroutine hand-off is
// ordered by the channel operations themselves, and the exiting goroutine's log lines are drained
// only after its last defer (close(done)) has run.
package main

import (
	"fmt"
	"runtime"
)

// exitFromHelper proves the unwind crosses FRAMES: this frame's own defer runs on the way out,
// and control never returns to the caller.
func exitFromHelper(log chan<- string) {
	defer func() { log <- "helper defer ran" }()

	log <- "helper calling runtime.Goexit"
	runtime.Goexit()
	log <- "helper resumed (UNREACHABLE)"
}

func main() {
	log := make(chan string, 16)
	done := make(chan struct{})

	go func() {
		// Registered first, so it runs LAST — and it is what releases main.
		defer close(done)

		defer func() {
			log <- "goroutine defer 2 ran"
		}()

		defer func() {
			// The load-bearing property: a Goexit is invisible to recover().
			if recover() == nil {
				log <- "recover() during Goexit returned nil"
			} else {
				log <- "recover() during Goexit returned a value (WRONG)"
			}
			log <- "goroutine defer 1 ran"
		}()

		exitFromHelper(log)
		log <- "goroutine resumed (UNREACHABLE)"
	}()

	<-done

	close(log)
	for line := range log {
		fmt.Println(line)
	}

	// No other goroutine was affected: the runtime is still running and still schedules them.
	after := make(chan string)
	go func() { after <- "a later goroutine still runs" }()
	fmt.Println(<-after)

	fmt.Println("main continues past the exited goroutine")
}
