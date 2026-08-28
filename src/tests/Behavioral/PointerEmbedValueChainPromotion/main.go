// PointerEmbedValueChainPromotion guards a method promoted through a POINTER embed and then one or
// more VALUE embeds — the two-hop chain that starts at a pointer.
//
// The converter had an arm for a single-level pointer-embed hop and an arm for an all-VALUE chain of
// any depth, and nothing in between: a chain THROUGH a pointer embed fell through both. net/http's
// transport test is the shape — `type breakableConn struct { net.Conn; *brokenState }` over
// `type brokenState struct { sync.Mutex; broken bool }`, so `w.Lock()` inside
// `(*breakableConn).Write` promotes twice. The bare emission binds nothing on the receiver box and
// lets C# overload resolution reach an unrelated same-named extension elsewhere in scope, which is
// why a missing hop always reports as CS1929 naming a type the Go source never mentions.
//
// The first hop needs no address-of machinery precisely BECAUSE it is a pointer embed: the field's
// value already IS the box. Every remaining hop composes as the `.of(Owner.field)` view the
// all-value descent uses, landing on the box of the method's own receiver type.
//
// Both call-site renderings of the base are exercised: the enclosing method's own deref-aliased
// receiver, and a plain pointer LOCAL in main (which renders as the box). The deferred form is here
// too — `defer c.Unlock()` takes the method-VALUE path, not the call path.
package main

import (
	"fmt"
	"sync"
)

type state struct {
	sync.Mutex
	broken bool
}

type conn struct {
	name string
	*state
}

// Lock/Unlock are promoted TWO hops: conn -> *state -> sync.Mutex. The deferred Unlock is the
// method-value spelling of the same chain.
func (c *conn) Write(b []byte) (int, error) {
	c.Lock()
	defer c.Unlock()

	if c.broken {
		return 0, fmt.Errorf("%s is broken", c.name)
	}

	return len(b), nil
}

func main() {
	s := &state{}
	c := &conn{name: "c1", state: s}

	n, err := c.Write([]byte("hello"))
	fmt.Println(n, err == nil)

	s.broken = true

	n, err = c.Write([]byte("hello"))
	fmt.Println(n, err != nil)

	// The same promotion from a pointer LOCAL rather than from inside the method — a different
	// rendering of the base expression through the identical hop chain. Taking and releasing the
	// lock here proves the promotion reached the ORIGINAL mutex: a copy would deadlock nothing and
	// the following Write would still succeed either way, so the write-through assertion is the
	// SHARED broken flag instead.
	c.Lock()
	c.broken = false
	c.Unlock()

	fmt.Println(s.broken)

	n, err = c.Write([]byte("xy"))
	fmt.Println(n, err == nil)
}
