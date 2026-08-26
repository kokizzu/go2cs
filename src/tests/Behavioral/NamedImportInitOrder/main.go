// Cross-package initialization-ORDER guard for NAMED imports (the log/slog shape).
//
// Go's spec: an imported package is fully initialized before the package that imports it. go2cs
// maps a Go `init` to `[GoInit]` = .NET `[ModuleInitializer]`, whose guarantee is weaker — a module
// constructor runs at first access to THAT module — so an assembly nothing in the program has
// touched never initializes. golib's `builtin.initPackage` exists to close that gap, and the
// converter emitted it only for BLANK imports; for a NAMED import the ordering was simply absent.
//
// The four packages here are the reduced log/slog shape:
//
//	store   holds the value and initializes nothing of its own   (log/internal)
//	writer  its `init` WRITES store.Value                        (log)
//	reader  its `init` CAPTURES store.Value                      (log/slog)
//	main    touches only reader                                  (the program)
//
// Go prints `written-by-writer-init`. Before the fix the converted C# printed the EMPTY string:
// reader's module constructor ran (main touches reader), writer's did not (nothing touches
// writer), so the capture read the zero value — the same nil capture that made `log/slog`'s
// default handler dereference nil and kill its test host.
//
// Two traps this file is shaped around, both paid for in an earlier attempt at this guard:
//
//  1. reader is reached from a FUNCTION BODY. A package-level `var x = reader.Captured()` here
//     would force reader's module constructor from main's OWN initialization, and — worse, in the
//     reader — a package-level `var x = writer.F()` would force the very package under test,
//     passing the guard for the wrong reason.
//  2. A behavioral project builds TWO executables with the same name, `bin/<cfg>/Go/<Name>.exe`
//     and `bin/<cfg>/net10.0/<Name>.exe`. A hand-check that globs and takes the first compares Go
//     against itself and reports a perfect match. Name the full path.
package main

import (
	"fmt"

	"NamedImportInitOrder/reader"
)

func main() {
	fmt.Println(reader.Captured())
}
