// subtreeLoadGoRoot_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

// Guard for processConversion's load-pattern branch (loadsPackageSubtree).
//
// A GOPATH input loads its whole subtree ("./..."); a GOROOT input never does, because the standard
// library's sub-packages are the convert set's business and are driven in dependency order.
//
// The case this guard exists for is the third row below: THE TWO ROOTS ARE NOT DISJOINT. A
// GOTOOLCHAIN-installed toolchain lives at $GOPATH/pkg/mod/golang.org/toolchain@<version>, so on
// such a box a GOROOT input is also a GOPATH input, every stdlib package loaded its subtree, and
// that converted runtime/cgo — a package `go list std` drops at CGO_ENABLED=0 and the queue
// therefore never had, emitted out of dependency order with nothing in the run log naming it. On a
// box with an ordinary GOROOT install the branch never fired, so the same command emitted a
// different corpus on the two boxes. (C2 mailbox 1257a20bad; COORD ruling 3d0c7cd5d.)
//
// Roots are built under t.TempDir() rather than spelled, so the paths are real and absolute and the
// predicate meets the same filepath.Clean handling it meets in a run.

package main

import (
	"os"
	"path/filepath"
	"testing"
)

func TestSubtreeLoadExcludesGoRootInputs(t *testing.T) {
	tempDir := t.TempDir()

	goPath := filepath.Join(tempDir, "gopath")
	separateGoRoot := filepath.Join(tempDir, "usr", "local", "go")

	// The defect's own shape: a GOTOOLCHAIN-installed toolchain inside the module cache, so GOROOT
	// is BOTH a GOROOT input and a GOPATH input.
	nestedGoRoot := filepath.Join(goPath, "pkg", "mod", "golang.org", "toolchain@v0.0.1-go1.24.13")

	// A SIBLING of that nested GOROOT whose name merely begins with the GOROOT spelling. It is under
	// GOPATH but not under GOROOT by path element, so it must keep the subtree behavior it has
	// always had. This is the arm that goes red if the guard is ever rewritten as a string prefix:
	// a prefix test would swallow it and silently narrow a GOPATH tree's conversion.
	goRootNamePrefixSibling := nestedGoRoot + "-tools"

	for _, dir := range []string{goPath, separateGoRoot, nestedGoRoot, goRootNamePrefixSibling} {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			t.Fatalf("could not create fixture root %q: %v", dir, err)
		}
	}

	tests := []struct {
		name      string
		inputPath string
		goRoot    string
		recurse   bool
		want      bool
	}{
		{
			name:      "a GOPATH package loads its subtree",
			inputPath: filepath.Join(goPath, "src", "example.com", "app"),
			goRoot:    separateGoRoot,
			want:      true,
		},
		{
			name:      "a GOROOT package outside GOPATH does not",
			inputPath: filepath.Join(separateGoRoot, "src", "runtime"),
			goRoot:    separateGoRoot,
			want:      false,
		},
		{
			// ⚠ The regression this guard exists for.
			name:      "a GOROOT package NESTED under GOPATH does not",
			inputPath: filepath.Join(nestedGoRoot, "src", "runtime"),
			goRoot:    nestedGoRoot,
			want:      false,
		},
		{
			name:      "GOROOT itself does not",
			inputPath: nestedGoRoot,
			goRoot:    nestedGoRoot,
			want:      false,
		},
		{
			// isPathUnder compares path ELEMENTS, so a name-prefix sibling is NOT under GOROOT and
			// keeps its GOPATH behavior. Red under a string-prefix guard.
			name:      "a GOPATH sibling whose name begins with the GOROOT spelling still does",
			inputPath: filepath.Join(goRootNamePrefixSibling, "pkg"),
			goRoot:    nestedGoRoot,
			want:      true,
		},
		{
			name:      "-recurse never loads a subtree",
			inputPath: filepath.Join(goPath, "src", "example.com", "app"),
			goRoot:    separateGoRoot,
			recurse:   true,
			want:      false,
		},
		{
			name:      "an input under neither root does not",
			inputPath: filepath.Join(tempDir, "scratch", "pkg"),
			goRoot:    separateGoRoot,
			want:      false,
		},
	}

	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			options := Options{goRoot: test.goRoot, goPath: goPath, recurse: test.recurse}

			if got := loadsPackageSubtree(test.inputPath, options); got != test.want {
				t.Errorf("loadsPackageSubtree(%q) = %v, want %v\n  GOROOT: %s\n  GOPATH: %s",
					test.inputPath, got, test.want, test.goRoot, goPath)
			}
		})
	}
}
