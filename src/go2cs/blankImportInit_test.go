// blankImportInit_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"strings"
	"testing"
)

// TestBlankImportInitName locks the generated hook name for a blank-imported package: one hook per
// imported package, named from the IMPORT PATH so it is unique by construction within the class
// (two blank imports in one file are two methods — a shared name would be CS0111), and composed
// only of C# identifier characters so a module path's dots and hyphens cannot break it.
func TestBlankImportInitName(t *testing.T) {
	prefix := "init" + TempVarMarker + TempVarMarker + "blankImport"

	cases := []struct {
		importPath string
		want       string
	}{
		{"image/png", prefix + TypeAliasDot + "image" + TypeAliasDot + "png"},
		{"crypto/sha256", prefix + TypeAliasDot + "crypto" + TypeAliasDot + "sha256"},
		{"runtime", prefix + TypeAliasDot + "runtime"},
		{"math/rand/v2", prefix + TypeAliasDot + "math" + TypeAliasDot + "rand" + TypeAliasDot + "v2"},
		// A module path: the dots of a domain and the hyphen of a directory are not identifier
		// characters, so both reduce to '_'.
		{"github.com/mattn/go-isatty", prefix + TypeAliasDot + "github_com" + TypeAliasDot + "mattn" + TypeAliasDot + "go_isatty"},
	}

	for _, tc := range cases {
		if got := blankImportInitName(tc.importPath); got != tc.want {
			t.Errorf("blankImportInitName(%q) = %q, want %q", tc.importPath, got, tc.want)
		}
	}

	// Distinct import paths must never collide, and none may land on the -tests package-init hook's
	// reserved name (both live under the doubled temp marker).
	seen := map[string]string{}

	for _, tc := range cases {
		name := blankImportInitName(tc.importPath)

		if prior, dup := seen[name]; dup {
			t.Errorf("blankImportInitName collision: %q and %q both yield %q", prior, tc.importPath, name)
		}

		seen[name] = tc.importPath

		if name == PackageTestInitHookMethod {
			t.Errorf("blankImportInitName(%q) collides with PackageTestInitHookMethod", tc.importPath)
		}

		if strings.ContainsAny(name, "./-") {
			t.Errorf("blankImportInitName(%q) = %q contains a non-identifier character", tc.importPath, name)
		}
	}
}

// TestNoInitPseudoPackages locks the packages a blank import must NOT force. Go's `unsafe` and
// `builtin` are compiler-provided and have no initialization at all — `import _ "unsafe"` is the
// //go:linkname ritual, present in 67 files of the converted standard library — and `C` is cgo.
// Forcing their module constructors would be guaranteed no-ops, so the emission skips them; every
// real package must still be forced.
func TestNoInitPseudoPackages(t *testing.T) {
	for _, pseudo := range []string{"unsafe", "builtin", "C"} {
		if !noInitPseudoPackages.Contains(pseudo) {
			t.Errorf("noInitPseudoPackages is missing %q", pseudo)
		}
	}

	for _, real := range []string{"image/png", "crypto/sha256", "runtime", "internal/unsafeheader", "cmp"} {
		if noInitPseudoPackages.Contains(real) {
			t.Errorf("noInitPseudoPackages must not contain the real package %q", real)
		}
	}
}
