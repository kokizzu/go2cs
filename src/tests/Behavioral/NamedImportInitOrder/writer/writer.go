// writer plays `log` in the named-import initialization-order guard: its `init` is the ONLY thing
// that ever writes store.Value, and nothing in the program references this package before that
// value is read.
//
// In Go that is enough — the spec initializes an imported package before its importer, so the
// reader's `init` always observes the written value. A .NET module constructor runs at first
// access to its OWN module, so an assembly nothing has touched yet has not initialized: without a
// forcing hook in the importer, this `init` never runs and the reader captures the zero value.
package writer

import "NamedImportInitOrder/store"

// Name exists solely so the reader's import of this package is a legal Go reference (Go rejects an
// unused import, and a BLANK import would be forced by the converter already — which is the case
// this guard must NOT be). It is a constant initializer, so it contributes no initialization of
// its own; the `func init()` below is what makes this package one that initializes.
var Name = "writer"

func init() {
	store.Value = "written-by-writer-init"
}
