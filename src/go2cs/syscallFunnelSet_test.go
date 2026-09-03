// syscallFunnelSet_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Pins the SET syscallKeepAliveAnalysis.go intercepts — the membership question, not the emission
// shape (deferredSyscallFunnel_test.go's CONTROL B already pins the shape for syscall.Syscall).
//
// The defect this guards (measured 2026-09-02, the pin-LIFETIME census): the funnel set carried
// Syscall/Syscall6/9/12/15/18/N and omitted RawSyscall/RawSyscall6, which Go marks with the SAME
// //go:uintptrkeepalive directive (GOROOT go1.23.12, syscall/syscall_linux.go:50,58,69,91 — those
// four declarations are the whole of the directive outside cmd/). The omission is invisible to
// every standing gate: an unprotected `(uintptr)Ꮡx` argument is valid C# and compiles clean. It
// left eleven generated Linux wrappers handing the kernel a managed address with nothing holding
// the box that pins it — syscall.pipe2, EpollCtl, Getrusage, prlimit1, Settimeofday, Times,
// Getrlimit/setrlimit, getgroups, socketpair, getpeername/getsockname, Capget — reachable from
// os.Pipe, os/exec's ProcessState.SysUsage, os/user's getgroups and net's socketpair paths.
//
// Why the fixture converts for linux/amd64 REGARDLESS of host: RawSyscall/RawSyscall6 are declared
// on the unix platforms only (syscall/dll_windows.go has no such member), so a host-platform
// fixture would not type-check on Windows and the guard would silently degrade to a skip on the
// machine class that runs the corpus gates. The converter's own loader sets GOOS/GOARCH from
// targetPlatform (moduleConverter.go:168), so pinning the target makes this guard's verdict
// host-INDEPENDENT — the opposite trade from deferredSyscallFunnel_test.go, whose shape genuinely
// differs per platform (syscall.Syscall's arity) and so must be derived per host.
//
// The other half of the widening — internal/runtime/syscall.Syscall6, the Linux boundary's own
// bottom — cannot be reached by a fixture at all (an `internal/` path is not importable from a
// test module), so it is pinned by the corpus footprint instead: the two-seeded A/B for the linux
// target must move internal/runtime/syscall/linux/syscall_linux.cs's EpollCtl.

package main

import (
	"fmt"
	"go/build"
	"path/filepath"
	"regexp"
	"runtime"
	"strings"
	"testing"
)

// rawSyscallFunnelFixture is the Go source the guard converts. Each call carries its OWN trap
// variable so the assertions can name a single emitted statement without depending on argument
// order, arity or the converter's spelling of the pointer expression.
const rawSyscallFunnelFixture = `package main

import (
	"fmt"
	"syscall"
	"unsafe"
)

var (
	trapRaw6  uintptr
	trapRaw   uintptr
	trapPlain uintptr
)

// POSITIVE - RawSyscall6 with a pointer-derived argument. Go marks RawSyscall6
// //go:uintptrkeepalive; the CLR heap moves, so the box that pins the buffer must be held across
// the call or the kernel may write through storage the GC has since relocated.
func raw6PointerArg(buf []byte) uintptr {
	r1, _, _ := syscall.RawSyscall6(trapRaw6, 0, uintptr(unsafe.Pointer(&buf[0])), uintptr(len(buf)), 0, 0, 0)
	return r1
}

// POSITIVE - RawSyscall, the 3-argument member of the same directive set.
func rawPointerArg(p *byte) uintptr {
	r1, _, _ := syscall.RawSyscall(trapRaw, 0, uintptr(unsafe.Pointer(p)), 0)
	return r1
}

// CONTROL - a RawSyscall call whose arguments are all integers takes NO temp. The widening is
// about pointer-derived arguments, not about the callee's name: capturing here would be pure
// noise in the emission and would mean pointerDerivedArgSource had widened too.
func rawNoPointer(fd uintptr) uintptr {
	r1, _, _ := syscall.RawSyscall(trapPlain, fd, 1, 0)
	return r1
}

func main() {
	buf := make([]byte, 8)
	fmt.Println(raw6PointerArg(buf), rawPointerArg(&buf[0]), rawNoPointer(0))
}
`

// convertRawSyscallFunnelFixture converts the fixture for linux/amd64 and returns its emitted C#.
func convertRawSyscallFunnelFixture(t *testing.T) string {
	t.Helper()

	root := t.TempDir()
	appDir := filepath.Join(root, "app")

	writeModuleFile(t, filepath.Join(appDir, "go.mod"), "module example.com/rawfunnel\n\ngo 1.23\n")
	writeModuleFile(t, filepath.Join(appDir, "main.go"), rawSyscallFunnelFixture)

	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	options := Options{
		goRoot:              goRoot,
		goPath:              build.Default.GOPATH,
		go2csPath:           filepath.Join(root, "out"),
		recurse:             true,
		targetPlatform:      "linux/amd64",
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

	return readGenerated(t, filepath.Join(options.go2csPath, "src", "example.com", "rawfunnel", "main.cs"))
}

// funnelTempAtCallSite is the emitted cast of a captured box at the call site: `(uintptr)ᴋ7`.
var funnelTempAtCallSite = regexp.MustCompile(`\(uintptr\)(ᴋ\d+)`)

// callStatementLine returns the one emitted line carrying `marker`, and the lines around it.
func callStatementLine(t *testing.T, mainCs, marker string) (line string, index int, lines []string) {
	t.Helper()

	lines = strings.Split(mainCs, "\n")

	for i, candidate := range lines {
		if strings.Contains(candidate, marker) {
			return strings.TrimSpace(candidate), i, lines
		}
	}

	t.Fatalf("no emitted statement carrying %q:\n%s", marker, mainCs)

	return "", -1, nil
}

// assertKeepAlivePair requires the statement carrying `marker` to cast a captured temp at the call
// site, to have that temp DECLARED above it, and to be followed by the temp's GC.KeepAlive — the
// three halves of the uintptrkeepalive emission, asserted independently so a partial regression
// (a temp with no KeepAlive, or a KeepAlive whose temp was never hoisted) names which half moved.
func assertKeepAlivePair(t *testing.T, mainCs, marker string) {
	t.Helper()

	line, index, lines := callStatementLine(t, mainCs, marker)
	matches := funnelTempAtCallSite.FindAllStringSubmatch(line, -1)

	if len(matches) == 0 {
		t.Errorf("the pointer-derived argument of %s is not routed through a captured box temp — the uintptrkeepalive contract is not applied to this callee:\n    %s", marker, line)
		return
	}

	for _, match := range matches {
		temp := match[1]
		declaration := fmt.Sprintf("var %s = ", temp)
		keepAlive := fmt.Sprintf("System.GC.KeepAlive(%s);", temp)

		if !strings.Contains(strings.Join(lines[:index], "\n"), declaration) {
			t.Errorf("temp %s is cast at the %s call site but never declared above it:\n    %s", temp, marker, line)
		}

		if !strings.Contains(strings.Join(lines[index+1:], "\n"), keepAlive) {
			t.Errorf("temp %s is cast at the %s call site but never kept alive after the statement — the box is unreachable the instant the argument is evaluated:\n    %s", temp, marker, line)
		}
	}
}

// TestRawSyscallFunnelKeepsItsPointerArgumentAlive is the positive: Go's //go:uintptrkeepalive set
// is RawSyscall, RawSyscall6, Syscall and Syscall6, and the converter's interception must cover all
// four. Red before the widening — RawSyscall/RawSyscall6 fell through to the general call path,
// which renders the argument as a bare `(uintptr)Ꮡ(buf, 0)` with nothing left referencing the box.
func TestRawSyscallFunnelKeepsItsPointerArgumentAlive(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: runs the real converter over a module fixture")
	}

	mainCs := convertRawSyscallFunnelFixture(t)

	assertKeepAlivePair(t, mainCs, "RawSyscall6(trapRaw6")
	assertKeepAlivePair(t, mainCs, "RawSyscall(trapRaw,")
}

// TestRawSyscallFunnelLeavesIntegerArgumentsAlone is the SCOPE half. The interception is keyed on
// the ARGUMENT shape (pointerDerivedArgSource) as much as on the callee, so an all-integer call
// into a now-intercepted callee must emit exactly what it emitted before: no temp, no KeepAlive.
// Without this, a widening of pointerDerivedArgSource would ride in unnoticed behind the set
// change — the two are separate predicates and this guard measures only one of them.
func TestRawSyscallFunnelLeavesIntegerArgumentsAlone(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: runs the real converter over a module fixture")
	}

	mainCs := convertRawSyscallFunnelFixture(t)
	line, _, _ := callStatementLine(t, mainCs, "RawSyscall(trapPlain")

	if strings.Contains(line, "ᴋ") {
		t.Errorf("an all-integer RawSyscall call captured a temp — the interception widened past pointer-derived arguments:\n    %s", line)
	}
}
