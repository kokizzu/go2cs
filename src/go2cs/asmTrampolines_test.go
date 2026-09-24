// asmTrampolines_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/build"
	"path/filepath"
	"runtime"
	"strings"
	"testing"
)

// asmTrampolineFixtureGo declares one bodyless function per trampoline SHAPE; asm_linux_amd64.s
// (asmTrampolineFixtureAsm) supplies their bodies, exactly as golang.org/x/sys/unix does.
const asmTrampolineFixtureGo = `package main

import (
	"fmt"
	"syscall"
)

// Shape 1: a JMP to an EXPORTED function of another package (x/sys/unix's Syscall).
func Syscall(trap, a1, a2, a3 uintptr) (r1, r2 uintptr, err syscall.Errno)

// Shape 2: a JMP to an UNEXPORTED function registered in asmTrampolineTargets.
func gettimeofday(tv *syscall.Timeval) (err syscall.Errno)

// Shape 3: a JMP to an UNEXPORTED function that is NOT registered: stays a stub.
func rawNoError(trap, a1, a2, a3 uintptr) (r1, r2 uintptr)

// Shape 4: real machine code (a raw SYSCALL), not a trampoline: stays a stub.
func SyscallNoError(trap, a1, a2, a3 uintptr) (r1, r2 uintptr)

// Shape 5: a JMP within the same package.
func localAlias(x int) int

func local(x int) int { return x + 1 }

func main() {
	fmt.Println(localAlias(1))
}
`

const asmTrampolineFixtureAsm = `// Copyright notice.

#include "textflag.h"

/* A block comment
   TEXT ·NotAFunction(SB),NOSPLIT,$0
   JMP syscall·Syscall(SB) */

TEXT ·Syscall(SB),NOSPLIT,$0-56
	JMP	syscall·Syscall(SB)

TEXT ·gettimeofday(SB),NOSPLIT,$0-16
	JMP	syscall·gettimeofday(SB)

TEXT ·rawNoError(SB),NOSPLIT,$0-48
	JMP	syscall·rawSyscallNoError(SB)

TEXT ·SyscallNoError(SB),NOSPLIT,$0-48
	MOVQ	a1+8(FP), DI
	MOVQ	trap+0(FP), AX	// syscall entry
	SYSCALL
	MOVQ	AX, r1+32(FP)
	MOVQ	DX, r2+40(FP)
	RET

TEXT ·localAlias(SB),NOSPLIT,$0-16
	JMP	·local(SB)
`

// convertAsmTrampolineFixture converts the fixture for target and returns its emitted main.cs.
func convertAsmTrampolineFixture(t *testing.T, target string) string {
	t.Helper()

	root := t.TempDir()
	appDir := filepath.Join(root, "app")

	writeModuleFile(t, filepath.Join(appDir, "go.mod"), "module example.com/tramp\n\ngo 1.23\n")
	writeModuleFile(t, filepath.Join(appDir, "main.go"), asmTrampolineFixtureGo)
	writeModuleFile(t, filepath.Join(appDir, "asm_linux_amd64.s"), asmTrampolineFixtureAsm)

	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	options := Options{
		goRoot:              goRoot,
		goPath:              build.Default.GOPATH,
		go2csPath:           filepath.Join(root, "out"),
		recurse:             true,
		targetPlatform:      target,
		indentSpaces:        4,
		preferVarDecl:       true,
		useChannelOperators: true,
	}

	build.Default.GOROOT = options.goRoot
	build.Default.GOPATH = options.goPath

	if err := NewModuleConverter(options).ConvertModule(appDir); err != nil {
		t.Fatalf("ConvertModule (%s): %v", target, err)
	}

	return readGenerated(t, filepath.Join(options.go2csPath, "src", "example.com", "tramp", "main.cs"))
}

// emittedFunction returns the emitted declaration of the named function through the end of its body
// (a bodyless partial is one line), so an assertion about one function cannot be met by another.
func emittedFunction(t *testing.T, mainCs string, name string) string {
	t.Helper()

	lines := strings.Split(strings.ReplaceAll(mainCs, "\r\n", "\n"), "\n")

	for i, line := range lines {
		if !strings.Contains(line, " static ") || !strings.Contains(line, " "+name+"(") {
			continue
		}

		if strings.HasSuffix(strings.TrimSpace(line), ";") {
			return line
		}

		var body strings.Builder

		for _, next := range lines[i:] {
			body.WriteString(next)
			body.WriteString("\n")

			if next == "}" {
				break
			}
		}

		return body.String()
	}

	t.Fatalf("no emitted declaration of %s:\n%s", name, mainCs)

	return ""
}

// TestAsmTrampolinesForwardByShape drives the EMISSION for each trampoline shape. Before
// asmTrampolines.go every one of them was a `partial` stub completed by a throwing
// PartialStubGenerator body; golang.org/x/sys/unix's Syscall is shape 1, and go-isatty reaching it
// inside fatih/color's type initializer is what killed the README walkthrough's app on Linux.
func TestAsmTrampolinesForwardByShape(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the module's closure via go/packages")
	}

	linux := convertAsmTrampolineFixture(t, "linux/amd64")

	forwards := map[string]string{
		"Syscall":      "syscall.Syscall(",
		"gettimeofday": "syscall.gettimeofday(",
		"localAlias":   "local(",
	}

	for name, call := range forwards {
		body := emittedFunction(t, linux, name)

		if strings.Contains(body, " partial ") {
			t.Errorf("shape %s: still a partial stub, not a forwarder:\n%s", name, body)
		}

		if !strings.Contains(body, call) {
			t.Errorf("shape %s: the forwarder does not call %q:\n%s", name, call, body)
		}
	}

	// Shapes that must NOT forward: an unregistered unexported target has no authorization to be
	// reached from another assembly, and real machine code is not a trampoline.
	for _, name := range []string{"rawNoError", "SyscallNoError"} {
		if body := emittedFunction(t, linux, name); !strings.Contains(body, " partial ") {
			t.Errorf("shape %s: must stay a partial stub:\n%s", name, body)
		}
	}

	// CONTROL: the assembly file is linux-only by its name, so a windows conversion has no
	// trampoline to read and every shape keeps its stub.
	windows := convertAsmTrampolineFixture(t, "windows/amd64")

	for _, name := range []string{"Syscall", "gettimeofday", "rawNoError", "SyscallNoError", "localAlias"} {
		if body := emittedFunction(t, windows, name); !strings.Contains(body, " partial ") {
			t.Errorf("windows control: %s forwarded although its assembly is not in the windows build:\n%s", name, body)
		}
	}
}

// TestParseAsmTrampolines pins the parser's reading of the source forms the emission arm does not
// exercise: a division-slash import path, arm64's `B`, a label, and symbols that are not Go
// functions of this package.
func TestParseAsmTrampolines(t *testing.T) {
	source := `#include "textflag.h"
TEXT ·IndexByte(SB),NOSPLIT,$0-40
	JMP	internal∕bytealg·IndexByte(SB)

TEXT ·Load(SB),NOSPLIT,$0-12
	B	·Load32(SB)

TEXT ·Labeled(SB),NOSPLIT,$0
loop:
	JMP	runtime·procyield(SB)

TEXT libc_getpid_trampoline<>(SB),NOSPLIT,$0-0
	JMP	libc_getpid(SB)

TEXT ·Twice(SB),NOSPLIT,$0
	JMP	·a(SB)
	JMP	·b(SB)

TEXT ·Last(SB),NOSPLIT,$0
	JMP	·a(SB)
GLOBL ·data(SB), RODATA, $8
`

	got := parseAsmTrampolines(source)

	want := map[string]string{
		"IndexByte": "internal/bytealg.IndexByte",
		"Load":      ".Load32",
		"Labeled":   "runtime.procyield",
		"Last":      ".a",
	}

	for name, target := range want {
		if got[name] != target {
			t.Errorf("%s: target %q, want %q", name, got[name], target)
		}
	}

	for _, name := range []string{"Twice", "libc_getpid_trampoline"} {
		if target, found := got[name]; found {
			t.Errorf("%s is not a pure-JMP trampoline of this package, yet parsed to %q", name, target)
		}
	}

	if len(got) != len(want) {
		t.Errorf("parsed %d trampolines, want %d: %v", len(got), len(want), got)
	}
}
