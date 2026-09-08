// WindowsNewCallback guards syscall.NewCallback -- the managed callback seam
// (docs/phase4/DESIGN-managed-newcallback.md), which is bucket-3 FRONTIER rather than wiring: Go's
// own implementation is assembly-backed (compileCallback walks an abiDesc into `callbackasm`, and
// `callbackasm`/`callbackasm1` are bodyless partials on every target here), so the linkname push was
// DECLINED and the remedy is a managed body on the syscall side.
//
// WHAT IS GUARDED. Three properties, each of which the seam can lose independently:
//
//   1. A REAL NATIVE CALLER CAN CALL BACK INTO GO. This is the whole capability. It exercises the
//      ruled mechanism end to end -- the non-generic per-arity shim, the reinterpret of each native
//      word's low sizeof(T) bytes, and the marshalled function pointer.
//   2. THE SAME FUNC VALUE YIELDS THE SAME POINTER. Go caches on the funcval pointer
//      (runtime/syscall_windows.go's cbs.index), so two NewCallback calls on one func value return
//      one address. Ours gets that from the table, which is ALSO the rooting -- pointer identity is
//      per delegate INSTANCE, so without the table the two calls would return different pointers.
//      This line is what fails if the table is ever "optimised away".
//   3. A NON-CONFORMING FUNC TYPE PANICS WITH GO'S OWN TEXT. Go permits only uintptr-sized arguments
//      and exactly ONE uintptr-sized result and panics outside that; the refusal must be a PANIC
//      carrying Go's message, not a managed exception, or a caller's recover() cannot see it.
//
// ⚠ WHY EnumSystemLocalesW AND NOT EnumWindows. The dispatch suggested EnumWindows or
// EnumThreadWindows. Both enumerate TOP-LEVEL WINDOWS, and a headless or service-session host can
// legitimately have ZERO of them -- which would make "the callback ran" host-dependent, i.e. exactly
// the property this file exists to avoid. EnumSystemLocalesW is kernel32, needs no window station,
// and always yields entries, so the assertion is a property of the seam rather than of the machine.
// Its callback is arity 1 returning BOOL, which is inside the implemented set.
//
// ⚠ EVERYTHING PRINTED IS COUNT-INDEPENDENT. Not one line carries how many locales exist, how many
// windows are open, or any address: those are properties of the host, and a golden that captured
// one would be a golden about the machine. Each line is a boolean about the seam.
//
// This package is WINDOWS-ONLY by construction -- syscall.NewCallback does not exist elsewhere -- so
// its package_info.cs carries [GoPlatformExclusive("windows")] and every harness skips it BY NAME on
// another host (F8). Its golden is captured on Windows.

//go:build windows

package main

import (
	"fmt"
	"syscall"
)

// The enumeration callback: one uintptr-sized argument, one uintptr-sized result -- the shape Go's
// contract permits, and the arity-1 shim on our side.
var invoked bool

func onLocale(lpLocaleString uintptr) uintptr {
	invoked = true
	return 1 // keep enumerating; the count is deliberately not observed
}

// A shape Go REFUSES: no result at all. Go's compileCallback panics with
// "compileCallback: expected function with one uintptr-sized result".
func nonConforming() {}

const wantRefusal = "compileCallback: expected function with one uintptr-sized result"

func main() {
	kernel32 := syscall.NewLazyDLL("kernel32.dll")
	enumSystemLocalesW := kernel32.NewProc("EnumSystemLocalesW")

	// (2) IDENTITY, taken BEFORE the call so a failure here is not confused with a failure to invoke.
	// The same func value, twice: Go returns one address and so must we.
	cb1 := syscall.NewCallback(onLocale)
	cb2 := syscall.NewCallback(onLocale)
	fmt.Println("same-func-same-pointer:", cb1 == cb2)

	// (1) A REAL NATIVE CALLER. LCID_INSTALLED = 0x1.
	enumSystemLocalesW.Call(cb1, uintptr(0x1))
	fmt.Println("callback-invoked:", invoked)

	// (3) THE REFUSAL, and that it is a PANIC carrying Go's text rather than any other failure.
	fmt.Println("nonconforming-panics-with-go-text:", refusalMatches())
}

func refusalMatches() (matched bool) {
	defer func() {
		if r := recover(); r != nil {
			s, ok := r.(string)
			matched = ok && s == wantRefusal
		}
	}()
	syscall.NewCallback(nonConforming)
	return false // not reached: Go panics above
}
