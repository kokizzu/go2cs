// deferredSyscallFunnel_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Guards the seam between the syscall-funnel interception (syscallKeepAliveAnalysis.go) and the
// defer/go temp-parameter form (visitDeferStmt/visitGoStmt), and — as with returnOperandOrder's
// controls — the SCOPE of that seam matters as much as the positive: the interception still owns
// every ORDINARY funnel call, including the uintptrkeepalive machinery it exists for.
//
// The defect this pins (measured 2026-09-02): convCallExpr intercepted EVERY funnel call before
// the general path threaded `callArgs`, and convSyscallFunnelCall takes no LambdaContext, so a
// DEFERRED funnel call rendered its arguments inside the THUNK BODY while every eager slot stayed
// empty —
//
//	defer((ᴛ1, ᴛ2, ᴛ3, ᴛ4) => syscall.Syscall(syscall.SYS_CLOSE, (uintptr)fds[i], 0, 0), , , , , ref ᒐ)
//
// CS0839 ×4, and a SEMANTIC defect underneath the compile error: `fds[i]` inside the thunk is read
// at UNWIND, where Go reads it at the defer statement. The corpus instance is runtime's
// memmove_linux_amd64_test.go:44, which is why runtime's Linux `-tests` build is the row-level gate.
//
// This lives in the CONVERTER suite rather than in tests/Behavioral because the shape cannot be
// written portably in Go: `syscall.Syscall` is `(trap, a1, a2, a3)` on unix and
// `(trap, nargs, a1, a2, a3)` on Windows, so one behavioral source cannot compile on both, and a
// build-tag split would give the project a per-platform golden set — manufacturing the very
// captured-on-one-OS drift EnvironBlockWalk already costs us. The fixture below is written by the
// test, so each host writes the form its own syscall package declares and the assertions are
// derived from that arity.

package main

import (
	"fmt"
	"go/build"
	"path/filepath"
	"runtime"
	"strings"
	"testing"
)

// syscallFunnelFixtureArity is how many arguments `syscall.Syscall` takes on THIS host: Windows
// carries an explicit `nargs` ahead of the three argument slots, every other platform does not.
func syscallFunnelFixtureArity() int {
	if runtime.GOOS == "windows" {
		return 5
	}

	return 4
}

// deferredSyscallFunnelFixture is the Go source both tests convert. `trap` is a variable rather
// than a platform constant so the one fixture type-checks everywhere (no host needs to declare
// SYS_CLOSE), and the deferred call's argument is read from a slice the loop MUTATES on the very
// next line — the mutation is what makes eager-vs-lazy argument evaluation observable at all.
func deferredSyscallFunnelFixture() string {
	deferArgs := "trap, uintptr(fds[i]), 0, 0"
	plainArgs := "trap, uintptr(fd), 1, 0"
	pointerArgs := "trap, 0, uintptr(unsafe.Pointer(&buf[0])), uintptr(len(buf))"

	if runtime.GOOS == "windows" {
		deferArgs = "trap, 3, uintptr(fds[i]), 0, 0"
		plainArgs = "trap, 3, uintptr(fd), 1, 0"
		pointerArgs = "trap, 3, 0, uintptr(unsafe.Pointer(&buf[0])), uintptr(len(buf))"
	}

	return `package main

import (
	"fmt"
	"syscall"
	"unsafe"
)

var trap uintptr

// POSITIVE - a deferred funnel call inside a loop whose argument CHANGES per iteration, with the
// slice mutated immediately after. Go evaluates ` + "`fds[i]`" + ` at the defer statement.
func deferredInLoop(fds []int) {
	for i := 0; i < len(fds); i++ {
		defer syscall.Syscall(` + deferArgs + `)
		fds[i] = -1
	}
}

// CONTROL A - an ORDINARY funnel call still renders its arguments inline: the interception is
// unchanged for every shape but defer/go.
func plainCall(fd int) uintptr {
	r1, _, _ := syscall.Syscall(` + plainArgs + `)
	return r1
}

// CONTROL B - an ordinary funnel call with a POINTER-DERIVED argument still routes through the
// uintptrkeepalive machinery the interception exists for: a temp holding the box, cast to uintptr
// at the call site, kept alive after the statement.
func pointerArg(buf []byte) uintptr {
	r1, _, _ := syscall.Syscall(` + pointerArgs + `)
	return r1
}

func main() {
	fds := []int{3, 4}
	deferredInLoop(fds)
	fmt.Println(plainCall(0), pointerArg(make([]byte, 8)))
}
`
}

// convertDeferredSyscallFunnelFixture converts the fixture and returns its emitted C#.
func convertDeferredSyscallFunnelFixture(t *testing.T) string {
	t.Helper()

	root := t.TempDir()
	appDir := filepath.Join(root, "app")

	writeModuleFile(t, filepath.Join(appDir, "go.mod"), "module example.com/deferfunnel\n\ngo 1.23\n")
	writeModuleFile(t, filepath.Join(appDir, "main.go"), deferredSyscallFunnelFixture())

	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	options := Options{
		goRoot:              goRoot,
		goPath:              build.Default.GOPATH,
		go2csPath:           filepath.Join(root, "out"),
		recurse:             true,
		targetPlatform:      runtime.GOOS + "/" + runtime.GOARCH,
		indentSpaces:        4,
		preferVarDecl:       true,
		useChannelOperators: true,
	}

	build.Default.GOROOT = options.goRoot
	build.Default.GOPATH = options.goPath

	converter := NewModuleConverter(options)

	if err := converter.ConvertModule(appDir); err != nil {
		t.Fatalf("ConvertModule: %v", err)
	}

	return readGenerated(t, filepath.Join(options.go2csPath, "src", "example.com", "deferfunnel", "main.cs"))
}

// deferStatementLine returns the one emitted line carrying the deferred funnel call.
func deferStatementLine(t *testing.T, mainCs string) string {
	t.Helper()

	for _, line := range strings.Split(mainCs, "\n") {
		if strings.Contains(line, "defer(") && strings.Contains(line, "syscall.Syscall") {
			return strings.TrimSpace(line)
		}
	}

	t.Fatalf("no deferred syscall.Syscall statement in emission:\n%s", mainCs)

	return ""
}

// TestDeferredSyscallFunnelCallFillsItsEagerArgumentSlots pins the positive property and both
// controls. Proven against a neuter: restoring the unconditional interception (dropping the
// `context.callArgs == nil` guard in convCallExpr) makes the positive report the malformed
// `syscall.Syscall(trap, (uintptr)fds[i], 0, 0), , , ,` form and leaves both controls passing —
// so the controls are measuring the scope, not carrying the verdict.
func TestDeferredSyscallFunnelCallFillsItsEagerArgumentSlots(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: runs the real converter over a module fixture")
	}

	mainCs := convertDeferredSyscallFunnelFixture(t)
	deferLine := deferStatementLine(t, mainCs)

	arity := syscallFunnelFixtureArity()
	temps := make([]string, arity)

	for i := range temps {
		temps[i] = fmt.Sprintf("%s%d", TempVarMarker, i+1)
	}

	joined := strings.Join(temps, ", ")

	// The thunk takes one parameter per argument and its BODY references nothing but those
	// parameters — every argument is therefore evaluated at the defer statement, as Go does, and
	// the callee's results are dropped by the statement body, as Go drops them.
	thunk := fmt.Sprintf("(%s) => syscall.Syscall(%s)", joined, joined)

	if !strings.Contains(deferLine, thunk) {
		t.Errorf("deferred funnel call does not render the temp-parameter thunk %q:\n    %s", thunk, deferLine)
	}

	// The per-iteration argument is an EAGER slot, not a thunk-body read. Its absence is the
	// CS0839 shape; its presence INSIDE the thunk would be the semantic defect (read at unwind).
	eager := deferLine[strings.Index(deferLine, thunk)+len(thunk):]

	if !strings.Contains(eager, "fds[i]") {
		t.Errorf("the per-iteration argument is not in an eager slot:\n    %s", deferLine)
	}

	// An empty argument slot is the defect's signature, and it survives a `strings.Contains` of
	// the thunk on its own — assert against it directly.
	if strings.Contains(deferLine, ", ,") {
		t.Errorf("deferred funnel call has an EMPTY argument slot (CS0839):\n    %s", deferLine)
	}

	if !strings.HasSuffix(deferLine, "ref ᒐ);") {
		t.Errorf("deferred funnel call does not close on the frame argument:\n    %s", deferLine)
	}
}

// TestOrdinarySyscallFunnelCallKeepsItsInterception is the SCOPE half: the fall-through added for
// defer/go must not have widened. An ordinary funnel call still renders its arguments inline, and
// a pointer-derived one still gets the box temp and the GC.KeepAlive that reproduce Go's
// uintptrkeepalive contract — the whole reason the interception exists.
func TestOrdinarySyscallFunnelCallKeepsItsInterception(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: runs the real converter over a module fixture")
	}

	mainCs := convertDeferredSyscallFunnelFixture(t)

	// CONTROL A - arguments inline, no temp parameters anywhere near the ordinary call.
	if !strings.Contains(mainCs, "syscall.Syscall(trap, ") {
		t.Errorf("an ordinary funnel call no longer renders its arguments inline:\n%s", mainCs)
	}

	// CONTROL B - the pointer-derived argument's box is held by a temp, cast at the call site,
	// and kept alive after the statement.
	if !strings.Contains(mainCs, "System.GC.KeepAlive(ᴋ") {
		t.Errorf("the uintptrkeepalive contract is gone from an ordinary funnel call:\n%s", mainCs)
	}

	if !strings.Contains(mainCs, "(uintptr)ᴋ") {
		t.Errorf("the pointer-derived argument no longer casts its box temp at the call site:\n%s", mainCs)
	}
}
