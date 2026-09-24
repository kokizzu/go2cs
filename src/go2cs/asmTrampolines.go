// asmTrampolines.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/ast"
	"go/build"
	"go/token"
	"os"
	"path/filepath"
	"regexp"
	"strings"
	"sync"
)

// PURE-JMP ASSEMBLY TRAMPOLINES.
//
// A bodyless Go function is implemented in assembly, and the converter emits it as a `partial`
// declaration that go2cs-gen's PartialStubGenerator completes with a throwing stub. For one shape the
// assembly carries no machine code worth the name: a TEXT block whose ONLY instruction is a jump to
// another Go function, which Go uses to re-export a function under a second name with an identical
// frame. golang.org/x/sys/unix's asm_linux_amd64.s is the case that mattered:
//
//	TEXT ·Syscall(SB),NOSPLIT,$0-56
//		JMP	syscall·Syscall(SB)
//
// go-isatty's ioctl reached that stub inside fatih/color's type initializer, so the README
// walkthrough's app died with a TypeInitializationException on Linux (COORD sizing 2026-09-24, gap 3).
// A JMP trampoline is Go's own statement that the two functions are the same function, so the
// faithful conversion is a FORWARDER: the linkname pull's emission (writeLinknameForwarder), which
// already bridges exactly this frame-identical shape.
//
// SCOPE: packages OUTSIDE the converted standard library. The corpus governs its own assembly with
// hand-owned implementations and a curated stub census; 442 pure-JMP trampolines fall on the corpus
// flavors (internal/runtime/atomic, the hand-owned sync/atomic, darwin's libc trampolines, math/big,
// crypto/x509/internal/macos), and emitting forwarders there would collide with the hand-owned
// partial implementations. A third-party package has no hand-owns: every bodyless function there is
// a throwing stub today, so a forwarder can only replace a certain failure with the target's behavior.
//
// A target must be callable from the trampoline's assembly: an EXPORTED function, or one registered
// in asmTrampolineTargets. The registry exists because assembly may jump to an UNEXPORTED function of
// another package with no `//go:linkname` handle at all (x/sys/unix jumps to syscall·gettimeofday),
// which packageFuncAccess then has to emit `public` so a different assembly can call it. It is
// curated for the same reason linknameForwardTargets is: publicizing every symbol some assembly
// somewhere jumps to would widen the corpus surface for calls that are never emitted.
//
// A trampoline whose body is anything else (x/sys/unix's SyscallNoError issues a raw SYSCALL) is not a
// forwarder and keeps its stub.
var asmTrampolineTargets = map[string]bool{
	// golang.org/x/sys/unix asm_linux_amd64.s: `TEXT ·gettimeofday(SB) ... JMP syscall·gettimeofday(SB)`.
	"syscall.gettimeofday": true,
}

// asmTextRE matches a TEXT directive for a function of THIS package (`·Name`); asmJumpRE the single
// jump that makes a block a trampoline (`JMP` on amd64, `B` or `JMP` on arm64).
var (
	asmTextRE         = regexp.MustCompile(`^TEXT\s+·([A-Za-z_][A-Za-z0-9_]*)\(SB\)`)
	asmJumpRE         = regexp.MustCompile(`^(?:JMP|B)\s+([^\s(]*·[A-Za-z_][A-Za-z0-9_]*)\(SB\)$`)
	asmLabel          = regexp.MustCompile(`^[A-Za-z_][A-Za-z0-9_]*:$`)
	asmBlockCommentRE = regexp.MustCompile(`(?s)/\*.*?\*/`)
)

// asmTrampolineIndexes caches, per package directory and target platform, the local function name of
// every pure-JMP trampoline mapped to its target as `<import path>.<func>` (an empty import path for
// a jump within the same package).
var (
	asmTrampolineIndexes     = map[string]map[string]string{}
	asmTrampolineIndexesLock sync.Mutex
)

// funcAsmTrampolineForward recognizes a bodyless package-level function implemented as a pure-JMP
// assembly trampoline and returns what writeLinknameForwarder needs to call its target: the target
// package's alias ("" for the same package, which calls it unqualified) and the target's C# name.
func (v *Visitor) funcAsmTrampolineForward(funcDecl *ast.FuncDecl) (alias string, targetFunc string, ok bool) {
	if funcDecl.Body != nil || funcDecl.Recv != nil || funcDecl.Name == nil || v.fset == nil || v.pkg == nil {
		return "", "", false
	}

	sourceDir := filepath.Dir(v.fset.Position(funcDecl.Pos()).Filename)

	if sourceDir == "" || sourceDir == "." || isGoRootSourceDir(sourceDir, v.options.goRoot) {
		return "", "", false
	}

	target, found := asmTrampolineIndex(sourceDir, v.options.targetPlatform, v.options.buildTags)[funcDecl.Name.Name]

	if !found {
		return "", "", false
	}

	dot := strings.LastIndex(target, ".")
	pkgPath, name := target[:dot], target[dot+1:]

	if pkgPath == "" || pkgPath == v.pkg.Path() {
		return "", getSanitizedFunctionName(name), true
	}

	if !token.IsExported(name) && !asmTrampolineTargets[target] {
		return "", "", false
	}

	return v.linknameTargetAlias(pkgPath), getSanitizedFunctionName(name), true
}

// isGoRootSourceDir reports whether dir is inside GOROOT's source tree, i.e. a standard-library package.
func isGoRootSourceDir(dir string, goRoot string) bool {
	if goRoot == "" {
		return false
	}

	rel, err := filepath.Rel(filepath.Join(goRoot, "src"), dir)

	return err == nil && rel != ".." && !strings.HasPrefix(rel, ".."+string(filepath.Separator))
}

// asmTrampolineIndex parses the package directory's assembly files that the target platform builds,
// once per (directory, platform).
func asmTrampolineIndex(sourceDir string, targetPlatform string, buildTags []string) map[string]string {
	key := sourceDir + "|" + targetPlatform + "|" + strings.Join(buildTags, ",")

	asmTrampolineIndexesLock.Lock()
	defer asmTrampolineIndexesLock.Unlock()

	if index, ok := asmTrampolineIndexes[key]; ok {
		return index
	}

	index := map[string]string{}
	asmTrampolineIndexes[key] = index

	entries, err := os.ReadDir(sourceDir)

	if err != nil {
		return index
	}

	context := build.Default
	context.GOOS, context.GOARCH, _ = strings.Cut(targetPlatform, "/")
	context.BuildTags = buildTags

	for _, entry := range entries {
		if entry.IsDir() || !strings.EqualFold(filepath.Ext(entry.Name()), ".s") {
			continue
		}

		// go/build's own matcher, not CheckBuildConstraints: that one reads a GO file's name and
		// rejects `asm_linux_amd64.s` even for linux/amd64 (measured), while MatchFile applies the
		// toolchain's rules to assembly exactly as `go build` selects it.
		if included, err := context.MatchFile(sourceDir, entry.Name()); err != nil || !included {
			continue
		}

		content, err := os.ReadFile(filepath.Join(sourceDir, entry.Name()))

		if err != nil {
			continue
		}

		for name, target := range parseAsmTrampolines(string(content)) {
			index[name] = target
		}
	}

	return index
}

// parseAsmTrampolines returns every TEXT block of assembly source whose only instruction is a jump to a
// Go function, as the local function name mapped to `<import path>.<func>`. Comments, blank lines,
// preprocessor lines and labels are not instructions; anything else is, and disqualifies the block.
func parseAsmTrampolines(source string) map[string]string {
	result := map[string]string{}

	source = asmBlockCommentRE.ReplaceAllString(source, "")

	var name string
	var instructions []string

	flush := func() {
		if name != "" && len(instructions) == 1 {
			if match := asmJumpRE.FindStringSubmatch(instructions[0]); match != nil {
				symbol := match[1]
				separator := strings.Index(symbol, "·")

				// Assembly spells an import path's '/' as U+2215 DIVISION SLASH.
				pkgPath := strings.ReplaceAll(symbol[:separator], "∕", "/")
				result[name] = pkgPath + "." + symbol[separator+len("·"):]
			}
		}

		name, instructions = "", nil
	}

	for _, line := range strings.Split(source, "\n") {
		if comment := strings.Index(line, "//"); comment >= 0 {
			line = line[:comment]
		}

		line = strings.TrimSpace(line)

		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}

		if match := asmTextRE.FindStringSubmatch(line); match != nil {
			flush()
			name = match[1]
			continue
		}

		if strings.HasPrefix(line, "TEXT") || strings.HasPrefix(line, "DATA") || strings.HasPrefix(line, "GLOBL") {
			// Another package's or a file-local symbol (`libc_x_trampoline<>`), or a data
			// directive: not an instruction of the current block, so it closes it without opening
			// one.
			flush()
			continue
		}

		if name == "" || asmLabel.MatchString(line) {
			continue
		}

		instructions = append(instructions, line)
	}

	flush()

	return result
}
