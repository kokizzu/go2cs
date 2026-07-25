// Package identlib probes what a runtime-resolved interface value IS, not just what
// it can do. Go keeps the DYNAMIC value when an interface value is copied, re-asserted
// or type-switched, so a duck-typing shell has to be transparent: %T must name the
// wrapped Go type, a re-assert must succeed, a type switch must take the same arm,
// and interface equality against the original value must hold.
package identlib

import "fmt"

type Greeter interface {
	Greet() string
}

func TryGreet(v any) (Greeter, bool) {
	g, ok := v.(Greeter)
	return g, ok
}

// Describe round-trips a runtime-resolved Greeter back through `any`.
func Describe(g Greeter, original any) string {
	var v any = g

	_, again := v.(Greeter)

	kind := "other"
	switch v.(type) {
	case string:
		kind = "string"
	case Greeter:
		kind = "greeter"
	}

	return fmt.Sprintf("%T again=%v kind=%s equal=%v text=%s", v, again, kind, v == original, g.Greet())
}
