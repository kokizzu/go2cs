// Guards runtime.Caller's frame accounting.
//
// The managed traceback answers Caller through the hand-owned runtime.callers funnel, which
// walks the CLR stack and projects it to Go-logical frames; every hop between the public API
// and the walker is itself a Go-source frame that has to be skipped. An off-by-one in either
// direction still returns plausible-looking file/line values, so the guard has to be a
// RELATION between frames rather than an absolute position: the converted program's source is
// C#, so no absolute file name or line number is comparable against Go in the first place.
package main

import (
	"fmt"
	"runtime"
)

// selfLine reports the line of its OWN runtime.Caller statement: Caller(0) names the caller of
// Caller, which is selfLine itself, so every call site gets the same answer.
func selfLine() int {
	_, _, line, _ := runtime.Caller(0)
	return line
}

// callerLine reports the line of the call site that invoked it.
func callerLine() int {
	_, _, line, _ := runtime.Caller(1)
	return line
}

// callerFile reports the file of the call site that invoked it.
func callerFile() string {
	_, file, _, _ := runtime.Caller(1)
	return file
}

// grandLine reports the line of the call site one level further up.
func grandLine() int {
	_, _, line, _ := runtime.Caller(2)
	return line
}

// wrapGrand exists only to put exactly one frame between grandLine and the site under test.
func wrapGrand() int {
	return grandLine()
}

// sameSite calls both forms on ONE source line, so a correct implementation must report the
// SAME line for both. This is the exact invariant an off-by-one in either direction breaks:
// skip one frame too few and callerLine reports its own body; one too many and wrapGrand's
// answer walks past sameSite.
func sameSite() (int, int) {
	return callerLine(), wrapGrand()
}

func siteA() int { return callerLine() }
func siteB() int { return callerLine() }

func okAt(skip int) bool {
	_, _, _, ok := runtime.Caller(skip)
	return ok
}

// deepOK asks for two levels above okAt — okAt, deepOK, then deepOK's caller — so the answer
// never depends on what sits below main.
func deepOK() bool {
	return okAt(2)
}

func depth() int {
	pc := make([]uintptr, 256)
	return runtime.Callers(0, pc)
}

func depthPlus1() int {
	return depth()
}

func depthPlus2() int {
	return depthPlus1()
}

func main() {
	x, y := sameSite()
	fmt.Println("same-line agreement:", x == y)

	fmt.Println("self line constant:", selfLine() == selfLine())
	fmt.Println("self line differs from call site:", selfLine() != callerLine())

	fmt.Println("distinct call sites:", siteA() != siteB())

	_, here, _, _ := runtime.Caller(0)
	fmt.Println("same file:", here == callerFile())
	fmt.Println("file reported:", len(here) > 0)

	fmt.Println("ok at 0:", okAt(0))
	fmt.Println("ok at 1:", okAt(1))
	fmt.Println("ok two levels up:", deepOK())
	fmt.Println("ok past the stack:", okAt(1000))

	fmt.Println("callers depth delta:", depthPlus2()-depth())

	callerFwd, callerBack := callerSeparators()
	fmt.Println("caller file uses forward slash:", callerFwd)
	fmt.Println("caller file uses host separator:", callerBack)

	framesFwd, framesBack := framesSeparators()
	fmt.Println("frames files use forward slash:", framesFwd)
	fmt.Println("frames files use host separator:", framesBack)

	fmt.Println("traceback uses host separator:", stackHasBackslash())
}

// hasByte reports whether s contains b. Hand-rolled rather than strings.Contains so this guard
// adds no package reference of its own — the project's reference set is converter-emitted.
func hasByte(s string, b byte) bool {
	for i := 0; i < len(s); i++ {
		if s[i] == b {
			return true
		}
	}
	return false
}

// callerSeparators reports how runtime.Caller SPELLS the file path it answers with. Go records
// source paths with forward slashes on every platform, Windows included: runtime.Caller there
// answers `C:/Program Files/Go/src/runtime/proc.go`, never the host's native spelling. That is
// observable, not cosmetic — flag's TestDefineAfterSet matches `.*/flag_test.go:.*` against
// exactly this string — so a converted program handing back the host separator diverges from Go
// on a value the program can read. On a forward-slash host both answers are trivially true; the
// guard bites on Windows, which is where the two spellings differ.
func callerSeparators() (fwd bool, back bool) {
	_, file, _, _ := runtime.Caller(0)
	return hasByte(file, '/'), hasByte(file, '\\')
}

// framesSeparators asks the same question of every frame a Callers/CallersFrames walk yields —
// the other surface the file string reaches a program through.
func framesSeparators() (fwd bool, back bool) {
	pc := make([]uintptr, 64)
	n := runtime.Callers(0, pc)
	frames := runtime.CallersFrames(pc[:n])
	for {
		frame, more := frames.Next()
		if len(frame.File) > 0 {
			fwd = fwd || hasByte(frame.File, '/')
			back = back || hasByte(frame.File, '\\')
		}
		if !more {
			break
		}
	}
	return fwd, back
}

// stackHasBackslash reports whether a rendered traceback spells any path the host's way. Go's
// traceback prints the same forward-slash file strings, so the answer is false on every platform.
func stackHasBackslash() bool {
	buf := make([]byte, 8192)
	n := runtime.Stack(buf, false)
	return hasByte(string(buf[:n]), '\\')
}
