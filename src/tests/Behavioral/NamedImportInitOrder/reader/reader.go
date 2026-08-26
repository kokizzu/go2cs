// reader plays `log/slog` in the named-import initialization-order guard: its `init` CAPTURES
// store.Value into a package variable, exactly as slog's own `init` captures
// log/internal.DefaultOutput into the default handler. A capture, not a re-read, is what makes the
// ordering observable — a later initialization of the writer cannot repair a value already copied.
//
// The import of `writer` is a NAMED import, which is the whole point: the converter has always
// forced a BLANK import's package initialization, and named imports are the gap.
package reader

import (
	"NamedImportInitOrder/store"
	"NamedImportInitOrder/writer"
)

var captured string

func init() {
	// Reads the package `store`, while the package that must be initialized first is `writer`.
	// That asymmetry is deliberate: it is the shape a read-set heuristic ("force the imports this
	// init references") gets wrong.
	captured = store.Value
}

// Describe is the legal Go reference that keeps the `writer` import used. It is never called
// before the value is read, so calling it cannot be what forces the writer's initialization —
// and reaching a package-level VARIABLE of another package triggers that package's C# TYPE
// constructor, never its module constructor, so even calling it would not stand in for the hook.
func Describe() string {
	return writer.Name
}

// Captured returns the value this package's `init` observed.
func Captured() string {
	return captured
}
