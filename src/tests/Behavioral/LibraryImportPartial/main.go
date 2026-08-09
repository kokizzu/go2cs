// A guard for the hand-owned FFI shape rather than for a Go language feature.
//
// Beside this file sits ffi_impl.cs — a hand-owned C# file of exactly the kind the converted
// standard library carries at every kernel boundary (syscall/windows, time, internal/runtime/
// syscall on linux). It declares a [LibraryImport] partial, and that one declaration only reaches a
// running program if TWO separate mechanisms hold:
//
//   1. The .csproj the converter REGENERATES for this project must set AllowUnsafeBlocks, which it
//      can only learn from the [module: go.GoRequiresUnsafe] declaration in ffi_impl.cs — the
//      converter's own usesUnsafeCode predicate sees nothing unsafe in this package. Without it:
//      SYSLIB1062.
//
//   2. go2cs-gen's PartialStubGenerator must leave the declaration alone. It stubs bodyless partial
//      methods (Go's asm/cgo functions), and source generators cannot see each other's output, so a
//      [LibraryImport] partial looks exactly like an unimplemented asm function from there. Stubbing
//      it produces two implementing parts: CS0757.
//
// Both failures are COMPILE failures, which is why the program itself is small — the compile phase
// is the assertion. The one line it prints is still evidence of a third thing: ffi_impl.cs runs the
// P/Invoke from a module initializer and throws if the kernel disagrees, so a program that prints
// at all is one whose source-generated marshalling stub actually worked.
package main

import "fmt"

func main() {
	fmt.Println("library import partial:", describe())
}

func describe() string {
	return "compiled, initialized and ran"
}
