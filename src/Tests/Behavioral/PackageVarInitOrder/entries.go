package main

// CROSS-FILE dependency: registry is declared in registry.go. This file sorts
// alphabetically BEFORE registry.go, so a naive C# conversion would run this
// static field initializer while registry is still nil (C# executes static
// field initializers per file in compiler input order) — a NullReference at
// type initialization. Mirrors syscall's procSetFilePointerEx <- modkernel32.
var entryName = registry.add("alpha")

// Depends on a RELOCATED var (entryName): must relocate too, even though the
// reference is same-file and backward — the dependency's value is only
// assigned in the ordered ctor, after every field initializer has run.
var entryUpper = entryName + "!"

// TRANSITIVE cross-file dependency through a package function: describe
// (main.go) reads names (registry.go).
var stdinName = describe(0)

// Func-literal (IIFE) initializer: the literal's body reads base (main.go) —
// cross-file through the literal, and the literal EXECUTES at init time.
var computed = func() int { return base * 2 }()

// A Go CONSTANT has no initialization at all, so a var declared ahead of it —
// here CROSS-FILE (tableSize/chunkBits live in registry.go, which the compiler
// sees later) — still sees its real value. A `static readonly` field for the
// constant reintroduces order and hands back the type's DEFAULT instead:
// compress/flate's `chunks [huffmanNumChunks]uint32` allocated length 0 that
// way, and every later index panicked.
var sizedTable = newTable(tableSize)

// Same shape THROUGH a struct's fixed-array field initializer, which is where
// flate's decoder table was silently zero-length.
var chunkHolder holder
