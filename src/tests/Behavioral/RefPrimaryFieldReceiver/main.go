// RefPrimaryFieldReceiver guards the cross-package ref-primary CALL-SITE rule (increment I3).
//
// sync.Mutex's Lock/TryLock/Unlock are hand-declared `[GoRecv] this ref Mutex` primaries and
// PUBLISHED as GoRefPrimary records in sync's package_info.cs. A consuming package that holds its
// receiver as a ref may therefore bind the plain member chain — `c.mu.Lock()` — instead of minting
// a field-address box for the receiver (`Ꮡc.of(counter.Ꮡmu).Lock()`).
//
// What this program pins, and why each row is here rather than being obvious:
//
//   1. WRITE-THROUGH under contention. The rewritten chain must be a genuine ref into the
//      receiver's own storage. If it ever bound a COPY, each goroutine would lock a different gate,
//      the increments would interleave, and the total would come out SHORT — silently, since a
//      per-copy lock still "works". Enough workers and iterations that a copy-bound build fails
//      essentially every run rather than occasionally.
//
//   2. ONE gate, not per-call state. TryLock on a mutex this goroutine already holds must fail. If
//      the plain chain and the box form reached different gates, it would succeed.
//
//   3. Unlock from ANOTHER goroutine. Go permits it, which is only meaningful if the gate is shared
//      storage rather than anything bound to the calling frame.
//
//   4. The NEGATIVE CONTROL. A base that is NOT ref-addressable in the emission — a plain local
//      pointer variable — must keep the box form and must still work. The row exists so the rule's
//      boundary is exercised rather than assumed; neutering the converter's published-record check
//      makes this row the one that breaks.
package main

import (
	"fmt"
	"sync"
)

type counter struct {
	mu sync.Mutex
	n  int
}

// A POINTER receiver: the emission gives it a deref-aliased receiver, so `c.mu` is a ref lvalue and
// the published primary binds the plain chain.
func (c *counter) bump(times int) {
	for i := 0; i < times; i++ {
		c.mu.Lock()
		c.n++
		c.mu.Unlock()
	}
}

func (c *counter) get() int {
	c.mu.Lock()
	defer c.mu.Unlock()
	return c.n
}

// nBeforeTouch guards the MINTED ENTRY ALIAS. Where the base lacks a deref alias the rule mints
// one — `ref var c = ref Ꮡc.DerefOrNull()` — at method ENTRY, which is earlier than the first field
// touch. A nil receiver must still fault where GO faults: at the first field touch, not at entry.
// This method returns before touching any field, so a nil receiver must reach the early return.
// If minting the alias moved the fault forward, this row panics instead of printing -1.
func (c *counter) nBeforeTouch() int {
	if c == nil {
		return -1
	}
	c.mu.Lock()
	defer c.mu.Unlock()
	return c.n
}

// bumpAsync guards the THIRD emission shape. Inside a closure the receiver is reached through the
// raw box rather than the method's deref alias, which emits `Ꮡc.Value.mu.Lock()`. It is exercised
// CONTENDED and its increments are counted, because a by-value `.Value` would be a COPY-lock: it
// compiles, it never contends, and the total silently comes out short.
func (c *counter) bumpAsync(times int, wg *sync.WaitGroup) {
	go func() {
		defer wg.Done()
		for i := 0; i < times; i++ {
			c.mu.Lock()
			c.n++
			c.mu.Unlock()
		}
	}()
}

// tryWhileHeld reports whether TryLock succeeds on a mutex this goroutine already holds. It must
// not: one mutex is one gate.
func (c *counter) tryWhileHeld() bool {
	c.mu.Lock()
	got := c.mu.TryLock()
	if got {
		c.mu.Unlock()
	}
	c.mu.Unlock()
	return got
}

func main() {
	const workers, each = 8, 2000

	// 1. write-through under contention, through the deref-aliased receiver (the plain chain)
	//    AND through the closure-captured receiver (the `.Value` third shape), contending with
	//    each other on the SAME mutex. A copy-lock on either side loses increments.
	c := &counter{}
	var wg sync.WaitGroup
	for i := 0; i < workers; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			c.bump(each)
		}()
	}
	for i := 0; i < workers; i++ {
		wg.Add(1)
		c.bumpAsync(each, &wg)
	}
	wg.Wait()
	fmt.Println("total:", c.get(), "want:", 2*workers*each)

	// 2. one gate, not per-call state
	fmt.Println("trylock while held:", c.tryWhileHeld())

	// 3. unlock from another goroutine
	c.mu.Lock()
	released := make(chan bool)
	go func() {
		c.mu.Unlock()
		released <- true
	}()
	<-released
	c.mu.Lock()
	c.mu.Unlock()
	fmt.Println("cross-goroutine unlock: ok")

	// 4. negative control: a plain local pointer base keeps the box form
	p := &counter{}
	q := p
	q.mu.Lock()
	q.n = 42
	q.mu.Unlock()
	fmt.Println("via local pointer:", p.n)

	// 5. the minted entry alias must not move a nil receiver's fault earlier than Go's
	var nilc *counter
	fmt.Println("nil receiver, early return:", nilc.nBeforeTouch())
}
