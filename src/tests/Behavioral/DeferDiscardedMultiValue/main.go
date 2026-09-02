// DeferDiscardedMultiValue guards a deferred call whose callee returns MULTIPLE results that the
// call site discards -- `defer f(a, b)` where `f` returns `(int, int, error)`. Go drops a deferred
// call's results at unwind; the converter must too, and the ARGUMENTS must still be evaluated at
// DEFER time, not at return.
//
// The defect this guards (measured 2026-09-02 on runtime's memmove_linux_amd64_test.go:44, which
// is `defer syscall.Syscall(SYS_MUNMAP, base+off, 65536, 0)`): the arity-N deferred-call path had
// no equivalent of the arity-0 path's result check, so the emission came out MALFORMED rather than
// merely unwrapped -- the whole call rendered while the argument slots stayed empty:
//
//     defer(syscall.Syscall(syscall.SYS_MUNMAP, @base + off, 65536, 0), , , , , ref ᒐ);
//
// which is CS0839 (argument missing) once per empty slot. It broke runtime's -tests assembly on
// Linux, and no standing gate could see it: runtime is unbanked so nothing builds its test
// assembly, -stdlib emits no test files at all, and the file is filename-constrained to
// linux/amd64 so no Windows lane ever compiles it.
//
// The output below is the CONTROL: what Go itself does. Both deferred calls must run at return in
// LIFO order, both must see the arguments as they were AT THE DEFER STATEMENT (not their mutated
// values), and both results must be discarded without the program noticing.
package main

import "fmt"

// three results, like syscall.Syscall -- the shape the defect needed
func triple(tag string, n int) (int, int, error) {
	fmt.Println("ran:", tag, n)
	return n, n * 2, nil
}

// one result, the shape the arity-0 path already handled correctly
func single(tag string) error {
	fmt.Println("ran:", tag)
	return nil
}

// The failing site's exact shape: uintptr parameters, three results, and a call whose arguments
// are a CONSTANT, a binary expression, and two UNTYPED numeric literals -- syscall.Syscall's
// signature and syscall.SYS_MUNMAP/base+off/65536/0 as its arguments. The untyped literals matter:
// the deferred-call path casts untyped-constant arguments to their default Go type, and that
// handling is conditioned on the very flag this arc changes.
const sysConst uintptr = 11

func quad(a, b, c, d uintptr) (uintptr, uintptr, error) {
	fmt.Println("ran: quad", a, b, c, d)
	return a, b, nil
}

func main() {
	n := 1

	base := uintptr(4)
	off := uintptr(2)

	// Arguments must be captured HERE, at n == 1, not at return where n == 99.
	defer triple("multi-value/eager", n)
	defer single("single-result")
	defer quad(sysConst, base+off, 65536, 0)

	n = 99
	fmt.Println("body done, n =", n)
}
