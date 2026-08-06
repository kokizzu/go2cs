// Package pmslib declares NAMED interfaces together with the only code that ever
// asserts to them. Nothing in this package implements them, and the importing
// package's concrete types are invisible here, so the converter's compile-time
// interface recorders cannot record a single (type, interface) pair — every assert
// below has to resolve through the runtime duck-typing shell.
package pmslib

// Speaker is the method-set probe: exactly one value-receiver-shaped method.
type Speaker interface {
	Speak() string
}

// Getter is the FAIL-SOFT probe. A Go GENERIC type's method signature carries its
// type parameter, so the runtime structural probe can only match it by NAME — a
// gbox[int] whose Get() returns an int matches `Get() string` by that weaker rule.
// Go answers ok=false; the shell must answer the same by refusing to build (a MISS),
// never by escaping as an exception.
type Getter interface {
	Get() string
}

// TrySpeak reports whether v's Go method set satisfies Speaker, calling through the
// interface when it does.
func TrySpeak(v any) (string, bool) {
	if s, ok := v.(Speaker); ok {
		return s.Speak(), true
	}
	return "", false
}

// TryGet is the Getter counterpart.
func TryGet(v any) (string, bool) {
	if g, ok := v.(Getter); ok {
		return g.Get(), true
	}
	return "", false
}
