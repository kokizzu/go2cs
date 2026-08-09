// Regression test: a native-sized integer constant (nint/nuint, incl. the uintptr alias) whose
// value does not fit a C# constant of that type — `const MaxU = ^uintptr(0)` = 0xFFFF...FFFF, a
// ulong literal needing a non-constant nuint conversion — must be emitted as `static readonly`
// with an unchecked cast, not `const` (which fails CS0133/CS0266). This is the
// runtime/internal/math `MaxUintptr` pattern.
package main

import "fmt"

const MaxU = ^uintptr(0) // max uintptr (0xFFFFFFFFFFFFFFFF on 64-bit)

const ptrSize = 8 // goarch.PtrSize on a 64-bit target

func main() {
	var zero uintptr = 0
	var one uintptr = 1
	fmt.Println(MaxU > zero)      // true
	fmt.Println(MaxU+one == zero) // true (uintptr wraps)
	fmt.Println(MaxU-one < MaxU)  // true

	// An UNTYPED-constant shift whose type comes from the CONTEXT (uintptr) rather than from the
	// Go source, with a value past int32. uintptr is a golib STRUCT, not a C# primitive, so it was
	// the one wide unsigned type whose shift retype cast the RESULT — `(uintptr)(1 << (int)(32))` —
	// and C# masks an int shift count to five bits, so the value was 1. The operand must carry the
	// width cast instead: `((uintptr)1 << (int)(32))`. This is runtime/internal/math MulUintptr's
	// overflow fast path (`a|b < 1<<(4*goarch.PtrSize)`), and the same shape sits in runtime's page
	// allocator as `1 << heapAddrBits`.
	var big uintptr = 1 << (4 * ptrSize)
	fmt.Println(big)     // 4294967296, not 1
	fmt.Println(big > 1) // true

	// A literal shift count, same context-typed shape.
	var wide uintptr = 1 << 40
	fmt.Println(wide) // 1099511627776

	// The composite-literal position MulUintptr's own test table uses. The `- 1` row was always
	// right — there the shift is an INNER node still typed `untyped int`, which the signed
	// constant fold handles whole — so only the bare row moves.
	rows := []struct{ a, b uintptr }{
		{1 << (4 * ptrSize), 1 << (4 * ptrSize)},
		{1<<(4*ptrSize) - 1, 1<<(4*ptrSize) - 1},
	}
	fmt.Println(rows[0].a, rows[0].b) // 4294967296 4294967296
	fmt.Println(rows[1].a, rows[1].b) // 4294967295 4294967295
}
