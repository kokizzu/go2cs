// Package dup shares its declared name with duprenamed's package (a different import path) but
// publishes no collision renames of its own — nothing here needs one.
package dup

// Marker needs no rename: nothing in this package collides with it. A consumer that imports this
// package bare (as "dup") alongside duprenamed (imported under another name) must still resolve
// THIS Marker, not duprenamed's collision-renamed type of the same name.
func Marker() string { return "marker-from-dupplain" }
