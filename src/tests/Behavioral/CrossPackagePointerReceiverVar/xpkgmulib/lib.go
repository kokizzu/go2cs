// Package xpkgmu is the library half of the CrossPackagePointerReceiverVar guard: two exported
// package-level VALUE vars of a type whose pointer-receiver methods are box-only in the conversion
// (sync.RWMutex is hand-owned on ж<RWMutex>), in the two shapes the box rule distinguishes.
package xpkgmu

import "sync"

// Boxed mirrors syscall.ForkLock: the package itself calls pointer-receiver methods on it (Touch), so
// the conversion's addressed-globals rule gives it a heap box (ᏑBoxed) and a ref property.
var Boxed sync.RWMutex

// Plain is never touched inside the package: no in-package address-of and no in-package
// pointer-receiver call, so nothing in the package's own text asks for a box.
var Plain sync.RWMutex

// Touch is what makes Boxed boxed. The guard's main package deliberately never calls it before the
// cross-package pairs: the hand-owned RWMutex keeps its state in a lazily created shared object, so a
// COPY taken after the real var's first use shares that state and passes by accident, while a copy
// taken before it fatals — the mask that hid this class from every linux and windows row.
// Counter is a CONVERTED struct with an ordinary pointer-receiver method (Inc takes no field address,
// so it is not capture-mode and keeps a `ref T` primary). Cnt is exported and never touched here: the
// owner arm leaves it unboxed, and an importer's `lib.Cnt.Inc()` must still count on THIS storage.
type Counter struct{ n int }

func (c *Counter) Inc() { c.n++ }

func (c Counter) Value() int { return c.n }

var Cnt Counter

func Touch() {
	Boxed.Lock()
	Boxed.Unlock()
}
