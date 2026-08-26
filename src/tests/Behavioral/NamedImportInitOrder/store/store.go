// store plays log/internal in the named-import initialization-order guard: it OWNS the value and
// initializes NOTHING of its own. That matters twice over.
//
// First, it is what makes the guard defeat the read-set heuristic the board warns about: the
// reader's `init` names THIS package, but the package whose `init` supplies the value is `writer`.
// A rule that forced only the imports an `init` textually reads would force `store` — which has
// nothing to run — and leave the one that matters unforced.
//
// Second, it is the selectivity half of the trigger: having neither a `func init()` nor a
// package-level variable with a non-constant initializer, `store` initializes nothing
// transitively, so a consumer must emit NO forcing hook for it. Forcing an empty module
// constructor would be a guaranteed no-op, and the emission says so by not emitting it.
package store

// Value is written by the writer package's `init` and read by the reader package's `init`.
// Declared WITHOUT an initializer, so this package contributes no initialization at all.
var Value string
