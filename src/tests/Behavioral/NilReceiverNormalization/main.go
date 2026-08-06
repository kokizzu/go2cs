package main

import "fmt"

// Go's nil-receiver NORMALIZATION idiom: a method tolerates a nil receiver not by testing it, but
// by re-pointing it through a helper that does. time's `func (l *Location) lookup(…) { l = l.get() }`
// is the canonical case — Time{} carries a nil *Location meaning UTC, and `get` maps nil to &utcLoc.
//
// The converted method aliases its receiver's pointee on entry (`ref var l = ref Ꮡl.Value;`) so the
// body can read fields through it. Establishing that alias eagerly DEREFERENCES the box before the
// re-pointing assignment can run, so a nil receiver faulted at entry — before a single field was
// read, and where Go returns normally. Nothing in the body ever reads the entry alias (the
// assignment replaces it), so the deref is pure loss.

type loc struct {
	name   string
	offset int
}

var utcLoc = loc{name: "UTC", offset: 0}

func (l *loc) get() *loc {
	if l == nil {
		return &utcLoc
	}
	return l
}

// lookup re-points its receiver before reading anything through it.
func (l *loc) lookup(sec int) (name string, offset int) {
	l = l.get()
	name = l.name
	offset = l.offset + sec
	return
}

// zoneName re-points and then writes back through the alias, so the re-alias must address the real
// storage rather than a throwaway slot.
func (l *loc) rename(suffix string) string {
	l = l.get()
	l.name += suffix
	return l.name
}

// span takes a nil-legal pointer PARAMETER through the same shape, not a receiver.
func span(l *loc, width int) string {
	l = l.get()
	return fmt.Sprintf("%s/%d", l.name, l.offset+width)
}

// firstUseReads keeps the ordinary emission: the receiver is read BEFORE it is re-pointed, so the
// entry alias is live and a nil receiver must still panic exactly as Go's does.
func (l *loc) firstUseReads() string {
	n := l.name
	l = l.get()
	return n + "/" + l.name
}

func main() {
	var nilLoc *loc
	real := &loc{name: "MST", offset: -7}

	fmt.Println(nilLoc.lookup(3))
	fmt.Println(real.lookup(3))

	fmt.Println(span(nilLoc, 10))
	fmt.Println(span(real, 10))

	// Written through the re-aliased receiver, then read back through the same pointer.
	fmt.Println(nilLoc.rename("!"))
	fmt.Println(utcLoc.name)
	fmt.Println(real.rename("?"))
	fmt.Println(real.name)

	fmt.Println(real.firstUseReads())

	// A nil receiver whose body reads before normalizing panics in Go; recover proves the
	// converted form still does, rather than silently reading a zero value.
	func() {
		defer func() {
			if r := recover(); r != nil {
				fmt.Println("recovered")
			}
		}()
		fmt.Println(nilLoc.firstUseReads())
	}()
}
