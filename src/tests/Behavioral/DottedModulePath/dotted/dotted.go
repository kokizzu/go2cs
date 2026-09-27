// Package dotted lives at a module path whose import path is NOT recoverable from its C# emission:
// a '.' inside a segment (example.com), a major-version last segment (/v2), and a package name
// that differs from that last segment.
package dotted

import "runtime"

// T is a defined type, so reflect reports its package path.
type T struct {
	N int
}

// F is a package-level function; FuncForPC names it by import path.
func F() int { return 7 }

// Where reports its own function name as runtime.Caller sees it.
func Where() string {
	pc, _, _, ok := runtime.Caller(0)
	if !ok {
		return "no caller"
	}
	return runtime.FuncForPC(pc).Name()
}
