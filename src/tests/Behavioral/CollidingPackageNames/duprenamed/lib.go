// Package dup deliberately shares its declared name with dupplain's package, at a different
// import path, and deliberately collision-renames an exported type — the two ingredients the
// CollidingPackageNames regression guards.
package dup

// Widget's Marker method collides with the exported Marker type below, so go2cs collision-renames
// the type (Marker -> ΔMarker) and publishes that rename in this package's package_info.cs, keyed
// by this package's own declared name "dup".
type Widget struct{}

func (Widget) Marker() string { return "widget-marker" }

// Marker collides with Widget.Marker above.
type Marker struct{ Value string }

// Greeting has no collision; it is the plain, correctly-resolving reference the test also
// exercises through this package's import.
func Greeting() string { return "hello-from-duprenamed" }
