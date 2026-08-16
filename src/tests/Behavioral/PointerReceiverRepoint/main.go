// A pointer receiver is an ordinary local variable in Go, so a method may REPOINT it
// (`n = n.next`) to walk a linked structure; the rebind is local to the callee and the
// caller's pointer is untouched.
//
// The converter deref-aliases a pointer receiver to a value var (`ref var n = ref Ꮡn.Value`),
// and a value alias cannot be repointed — the repoint needs the direct-ж receiver form, where
// the box `Ꮡn` IS the parameter and the assignment repoints the box before re-aliasing the
// value (`Ꮡn = n.next; n = ref Ꮡn.DerefOrNull();`).
//
// That emission arm already existed but was reachable only for a method some OTHER direct-ж
// trigger had marked. Every method here is written to avoid all of them — the receiver is never
// returned, never compared to nil, never address-taken, never captured in a closure and never
// passed as a pointer argument — so the repoint itself is the only reason the direct-ж form can
// be chosen. Without the `bodyReassignsReceiver` trigger these emit `this ref node n` and the
// repoint fails to compile (CS0029: cannot convert `ж<node>` to `node`).
//
// Guards the root censused from database/sql's `fakedb_test.go`, whose
// `func (s *fakeStmt) QueryContext` walks `s = s.next` in exactly this shape.
package main

import "fmt"

type node struct {
	val  int
	next *node
}

// sum walks the whole list by repointing the receiver. The loop tests the FIELD for nil, never
// the receiver itself, so bodyUsesReceiverAsPointerValue does not fire.
func (n *node) sum() int {
	total := 0
	for {
		total += n.val
		if n.next == nil {
			break
		}
		n = n.next
	}
	return total
}

// advance repoints the receiver a bounded number of times and reads a field afterward, proving
// the value alias follows the box to the NEW element rather than staying pinned to the first.
func (n *node) advance(steps int) int {
	for i := 0; i < steps; i++ {
		if n.next == nil {
			break
		}
		n = n.next
	}
	return n.val
}

// scale repoints the receiver while WRITING through it, so each write must land in the element
// the box currently points at — a stale alias would scale the first element repeatedly.
func (n *node) scale(factor int) {
	for {
		n.val = n.val * factor
		if n.next == nil {
			break
		}
		n = n.next
	}
}

func main() {
	c := &node{val: 3}
	b := &node{val: 2, next: c}
	a := &node{val: 1, next: b}

	fmt.Println(a.sum())

	fmt.Println(a.advance(0), a.advance(1), a.advance(2), a.advance(99))

	a.scale(10)
	fmt.Println(a.val, b.val, c.val)
	fmt.Println(a.sum())

	// The callee's repoint never rebinds the CALLER's pointer: `a` still names the head.
	fmt.Println(a.val, a.next.val, a.next.next.val)
}
