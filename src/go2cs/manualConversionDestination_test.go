// manualConversionDestination_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The BOTH-SIDES guard for manualConversionFuncs: a registration displaces a generated body, and the
// displacement must have a DESTINATION.
//
// Why this exists, stated as the defect it locks out rather than as a rule. `manualConversionFuncs`
// only ever says "do not emit this body" — the hand-owned replacement lives in a separate `*_impl.cs`
// that nothing mechanically ties to the registration. So the two halves can part company at a merge,
// silently and with no conflict: on 2026-08-29 master carried the `Uname` registration and the
// generated placeholder pointing at it, while the `_impl.cs` body it named existed nowhere. Clean
// merge, no warning, `-p:GoTargetOS=linux` red at `kernel_version_linux.cs(21,27) CS0117` and the
// whole Linux corpus behind it.
//
// It got past the seam check that was supposed to catch exactly this, and the reason is the point of
// this file. That check asserted every registered name has ZERO generated bodies and EXACTLY ONE
// placeholder — which verifies the wrapper was DISPLACED and never that the displacement ARRIVES
// anywhere. A placeholder aimed at a body that does not exist passes it cleanly. The fold-bound
// lesson: a displacement property must assert its destination; every seam check carries both sides of
// the ledger.
//
// SCOPE, deliberately narrow. This walks the real corpus and asks one question per registration: does
// SOME hand-owned file in the package define this name? It does not type-check, does not resolve
// overloads, and does not know which platform folder a body belongs in (layout L3 routes hand-owns by
// their principal's platform set — platformHandOwn_test.go owns that invariant). A cheap, mechanical
// yes/no is what the merge seam needs; anything richer would duplicate the compiler, which the corpus
// build already runs.

package main

import (
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"testing"
)

// TestManualConversionRegistrationsHaveBodies is the forward direction: registration => a hand-owned
// definition exists. This is the direction the Uname subtraction broke.
func TestManualConversionRegistrationsHaveBodies(t *testing.T) {
	coreDir := filepath.Join("..", "core")

	if _, err := os.Stat(coreDir); err != nil {
		t.Skip("src/core is not beside the converter; nothing to walk")
	}

	var missing []string

	for pkg, funcs := range manualConversionFuncs {
		bodies := handOwnedDefinitions(t, filepath.Join(coreDir, filepath.FromSlash(pkg)))

		for name := range funcs {
			// A method registration ("g.guintptr", "SockaddrInet4.sockaddr") names the RECEIVER
			// type and the member; the member is what a hand-own defines, so match on the tail.
			member := name
			if dot := strings.LastIndex(member, "."); dot >= 0 {
				member = member[dot+1:]
			}

			if !bodies[member] {
				missing = append(missing, pkg+"."+name)
			}
		}
	}

	sort.Strings(missing)

	for _, entry := range missing {
		t.Errorf("manualConversionFuncs registers %s, but no hand-owned file in that package defines it — "+
			"the generated body is displaced and the displacement has no destination, which is a build "+
			"failure on the scoped platform (CS0117 at the first consumer), not a converter warning", entry)
	}
}

// handOwnedDefinitions returns the member names DEFINED by the hand-owned files of one package —
// every `*_impl.cs` plus every file carrying the whole-file `[module: GoManualConversion]` marker,
// which are the two shapes a hand-own takes (platformHandOwn_test.go's own framing). Walks nested
// per-GOOS folders, because layout L3 routes hand-owns into them.
func handOwnedDefinitions(t *testing.T, packageDir string) map[string]bool {
	t.Helper()

	defined := map[string]bool{}

	if _, err := os.Stat(packageDir); err != nil {
		return defined
	}

	err := filepath.Walk(packageDir, func(path string, info os.FileInfo, err error) error {
		if err != nil || info.IsDir() || !strings.HasSuffix(path, ".cs") {
			return nil
		}

		// `.cs.auto` review siblings are the converter's own output, never a hand-own.
		if strings.HasSuffix(path, ".cs.auto") {
			return nil
		}

		content, readErr := os.ReadFile(path)
		if readErr != nil {
			return nil
		}

		text := string(content)

		// `_impl` anywhere in the base name, not just as the suffix: the hand-owned TEST surface
		// uses `<name>_impl_test.cs` (internal/reflectlite's export_impl_test.cs defines Field,
		// TField and Zero, all three registered). Calibrated against the corpus rather than
		// assumed — a `_impl.cs`-only suffix check reported those three as missing bodies.
		isImpl := strings.Contains(filepath.Base(path), "_impl")

		if !isImpl && !manualConversionMarker.MatchString(text) {
			return nil
		}

		for _, line := range csharpDeclarationLine.FindAllString(text, -1) {
			for _, match := range csharpCallableName.FindAllStringSubmatch(line, -1) {
				defined[match[1]] = true
			}
		}

		return nil
	})
	if err != nil {
		t.Fatalf("walking %s: %v", packageDir, err)
	}

	return defined
}

// The whole-file hand-own marker, line-anchored for the reason the corpus census records: `reflect`
// and `internal/reflectlite` MENTION the marker inside bodyless-partial placeholder comments, and an
// unanchored match counts those as hand-owns.
var manualConversionMarker = regexp.MustCompile(`(?m)^\s*\[module:\s*(go\.)?GoManualConversion\]`)

// A line that DECLARES something: an access modifier at the start, after any attributes. Selecting
// the line first and harvesting names second is what makes TUPLE RETURN TYPES work —
// `internal static unsafe (nint wpid, error err) wait4(…)` puts parentheses in the RETURN, so any
// pattern that walks from the modifier to the first `(` stops in the wrong place. Measured, not
// guessed: the first cut of this guard did exactly that and reported 22 false failures, including
// `wait4`, `Select` and `Adjtimex` — bodies in this very branch.
var csharpDeclarationLine = regexp.MustCompile(`(?m)^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|private|protected)\b[^\n;]*\([^\n]*$`)

// Every `Name(` on such a line. Deliberately over-collecting: a tuple return contributes its field
// names too, and that is fine. Over-collection can only cost a false PASS on a name nothing defines,
// which the corpus build then catches at the first consumer; under-collection costs a false FAIL,
// which is the failure mode that makes a merge-seam guard worthless — nobody believes a check that
// cries wolf, and this one exists to be believed.
var csharpCallableName = regexp.MustCompile(`\b([A-Za-z_][A-Za-z0-9_]*)\s*\(`)
