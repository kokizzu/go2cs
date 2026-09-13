// Same-declared-package-name guard: duprenamed and dupplain are two different import paths that
// both declare `package dup`. go2cs's imported-type-alias table is keyed by declared package name,
// not import path, so duprenamed's collision-renamed Marker type used to leak into any reference
// to dupplain's unrelated, unrenamed Marker function that happened to share the key (CS1955) —
// exactly the shape that broke runtime's crash_test.go, where runtime/trace.Log picked up
// internal/trace's unrelated Log rename because both packages declare `package trace`.
//
// The Box rows guard the SAME declared-name-not-import-path root at a second site: the
// cross-package INSTANTIATED-GENERIC render built its qualifier from the declared package NAME
// instead of this file's alias for the import PATH, so `dupmeta.Box[string]` emitted
// `dup.Box<@string>` against dupplain's class, which has no Box (CS0426). Only the GENERIC
// reference was affected — `dupmeta.Greeting()` above is the control that already resolved
// correctly, which is why the corpus compiled with six such same-name sites at go1.23.12 and
// only go1.24's `unique` (isync "internal/sync" beside "sync", both declaring `package sync`,
// referencing the generic HashTrieMap) ever reached it.
package main

import (
	"fmt"

	dupmeta "collidea/dup"

	"collideb/dup"
)

func main() {
	fmt.Println(dupmeta.Greeting())
	fmt.Println(dup.Marker())

	// The discriminating row: a GENERIC type in a DECLARATION's type position, reached through the
	// ALIASED import of the package whose declared name the other import also carries. Pre-fix this
	// rendered `dup.Box<@string>` — dupplain's class, which has no Box (CS0426).
	//
	// The declaration form is load-bearing and was MEASURED: a composite literal
	// (`b := dupmeta.Box[string]{...}`) renders through a different path and resolved correctly even
	// pre-fix, so a guard written that way is green against the defect it is meant to catch.
	var b dupmeta.Box[string]
	b.V = "boxed-in-duprenamed"
	fmt.Println(b.Get())

	// Control on the same import: a NON-generic type through the same alias already resolved
	// correctly, so a red on this line would mean the fix reached further than the generic arm.
	var w dupmeta.Widget
	fmt.Println(w.Marker())
}
