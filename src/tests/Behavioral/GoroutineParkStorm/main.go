// A thousand goroutines that are all PARKED AT ONCE — the shape that says whether the runtime owns
// goroutine capacity or rents it from a host heuristic.
//
// The barrier is the whole point. Every goroutine announces itself and then parks, and main cannot
// pass its drain loop until all n have announced, so all n must be RUNNING SIMULTANEOUSLY before
// anything is released. A goroutine that has not started cannot announce, and a parked one never
// finishes to free what it holds — so under an executor where goroutines share a fixed pool of
// threads, the barrier can only be crossed as fast as that pool grows (hill-climbing injection,
// roughly a thread per second), and this program takes minutes. With a dedicated thread per
// goroutine, capacity equals demand by construction and it takes milliseconds. See
// docs/phase4/DESIGN-cooperative-scheduler.md §1.
//
// Deterministic by construction: the ids are summed, never printed in arrival order, and every
// cross-goroutine ordering is established by the WaitGroup and the channel operations themselves —
// no sleeps and no timing anywhere.
package main

import (
	"fmt"
	"sync"
)

const n = 1000

func main() {
	// The release gate: main holds it until every goroutine has arrived.
	var start sync.WaitGroup
	start.Add(1)

	var done sync.WaitGroup
	done.Add(n)

	// Buffered to n, so announcing arrival can never itself block — the only parks in this program
	// are the ones being measured.
	arrived := make(chan int, n)

	// Unbuffered: every send below is a real rendezvous with main's receive.
	results := make(chan int)

	for i := 0; i < n; i++ {
		go func(id int) {
			defer done.Done()
			arrived <- id
			start.Wait()  // park #1: the sync runtime semaphore
			results <- id // park #2: an unbuffered channel rendezvous
		}(i)
	}

	// Cannot complete until all n goroutines exist and have run.
	sum := 0
	for i := 0; i < n; i++ {
		sum += <-arrived
	}
	fmt.Println("arrived:", n, "sum:", sum)

	// n goroutines are now parked on start.Wait(); release them all at once.
	start.Done()

	total := 0
	for i := 0; i < n; i++ {
		total += <-results
	}
	done.Wait()

	fmt.Println("rendezvous:", n, "total:", total)
	fmt.Println("match:", sum == total)
}
