// WindowsNewCallback guards syscall.NewCallback -- the managed callback seam
// (docs/phase4/DESIGN-managed-newcallback.md), which is bucket-3 FRONTIER rather than wiring: Go's
// own implementation is assembly-backed (compileCallback walks an abiDesc into `callbackasm`, and
// `callbackasm`/`callbackasm1` are bodyless partials on every target here), so the linkname push was
// DECLINED and the remedy is a managed body on the syscall side.
//
// WHAT IS GUARDED. Three properties, printed as four lines, each of which the seam can lose
// independently:
//
//   1. A REAL NATIVE CALLER CAN CALL BACK INTO GO. This is the whole capability. It exercises the
//      ruled mechanism end to end -- the non-generic per-arity shim, the reinterpret of each native
//      word's low sizeof(T) bytes, and the marshalled function pointer.
//   2. THE SAME FUNC VALUE YIELDS THE SAME POINTER, AND A DIFFERENT ONE DOES NOT. Go caches on the
//      funcval pointer (runtime/syscall_windows.go's cbs.index), so two NewCallback calls on one
//      func value return one address. Ours gets that from the table, which is ALSO the rooting --
//      pointer identity is per delegate INSTANCE, so without the table the two calls would return
//      different pointers. The first line is what fails if the table is ever "optimised away"; the
//      SECOND line is the discriminating complement, without which a body returning one CONSTANT
//      pointer for every func would pass. Two distinct top-level funcs have distinct funcvals, so
//      Go's table appends two entries and the addresses differ -- guaranteed, not incidental.
//   3. A NON-CONFORMING FUNC TYPE PANICS WITH GO'S OWN TEXT. Go permits only uintptr-sized arguments
//      and exactly ONE uintptr-sized result and panics outside that; the refusal must be a PANIC
//      carrying Go's message, not a managed exception, or a caller's recover() cannot see it.
//
//      ⚠ THIS LINE PRINTS THE TEXT, NOT A BOOLEAN, AND THAT IS THE WHOLE POINT. A `== wantRefusal`
//      boolean can read false on BOTH sides for OPPOSITE reasons -- Go because the runtime reworded
//      its panic in some release, ours because the body raises something else -- and two arms equal
//      for opposite reasons are a vacuous pass that a golden would then bank as the contract.
//      Printing the recovered value compares the strings themselves, so the golden records whatever
//      the pinned toolchain says and our body has to match THAT. It also means this file carries no
//      assumption about the corpus pin's exact wording: at go1.24.13 the branch taken here is
//      syscall_windows.go:288 (`len(ft.OutSlice()) != 1`), text "compileCallback: expected function
//      with one uintptr-sized result" -- read, not remembered -- but nothing here depends on it.
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
// one would be a golden about the machine. Three lines are booleans about the seam and the fourth is
// a compile-time constant of the pinned runtime; none of the four can move with the host.
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

// A SECOND conforming callback, distinct from onLocale. Never invoked -- it exists only so the
// identity property has its discriminating complement (see (2) in the header). Deliberately it does
// NOT touch `invoked`: a later edit that enumerated with this one instead would otherwise make
// property (1) read true without the seam having called anything. Its body also differs from
// onLocale's, so the two funcvals cannot be one however identical bodies are treated.
func onLocaleOther(lpLocaleString uintptr) uintptr {
	return 0 // never enumerated with, so the value is inert; it only has to differ from onLocale's
}

// A shape Go REFUSES: no result at all -- zero results is not one uintptr-sized result.
func nonConforming() {}

func main() {
	kernel32 := syscall.NewLazyDLL("kernel32.dll")
	enumSystemLocalesW := kernel32.NewProc("EnumSystemLocalesW")

	// (2) IDENTITY, taken BEFORE the call so a failure here is not confused with a failure to invoke.
	// The same func value, twice: Go returns one address and so must we.
	cb1 := syscall.NewCallback(onLocale)
	cb2 := syscall.NewCallback(onLocale)
	fmt.Println("same-func-same-pointer:", cb1 == cb2)

	// The complement: a DIFFERENT func value must not share cb1's address.
	cbOther := syscall.NewCallback(onLocaleOther)
	fmt.Println("different-func-different-pointer:", cbOther != cb1)

	// (1) A REAL NATIVE CALLER. LCID_INSTALLED = 0x1.
	enumSystemLocalesW.Call(cb1, uintptr(0x1))
	fmt.Println("callback-invoked:", invoked)

	// (3) THE REFUSAL. The TEXT, so the two sides compare strings rather than a boolean that can be
	// false on both sides for different reasons.
	fmt.Println("nonconforming-refusal:", refusalText())
}

// refusalText returns the recovered panic value of a NewCallback that must be refused. The two
// non-string outcomes get their own markers so neither can be mistaken for a text mismatch: a seam
// that FAILED TO REFUSE and one that refused with a managed exception are different defects.
func refusalText() (out string) {
	defer func() {
		switch r := recover().(type) {
		case nil:
			out = "<no-panic>"
		case string:
			out = r
		default:
			out = fmt.Sprintf("<non-string-panic:%T>", r)
		}
	}()
	syscall.NewCallback(nonConforming)
	return "<no-panic>" // not reached: Go panics above
}
