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

// Box is a GENERIC type and it exists ONLY in this package — dupplain, which declares the same
// package name at a different import path, has no Box at all. That asymmetry is what makes a
// reference to it discriminating: the cross-package INSTANTIATED-GENERIC render used to build its
// qualifier from the declared package NAME ("dup") rather than from the file's alias for this
// package's PATH ("dupmeta"), so `dupmeta.Box[string]` emitted `dup.Box<@string>` — dupplain's
// class, which has no Box — CS0426. The same declared-name-not-import-path root this project
// already guards for the imported-type-alias TABLE, one layer over at the reference site.
type Box[T any] struct{ V T }

func (b Box[T]) Get() T { return b.V }
