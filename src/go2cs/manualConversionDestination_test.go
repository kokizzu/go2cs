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
	"go/ast"
	"go/build"
	"go/parser"
	"go/token"
	"os"
	"path/filepath"
	"regexp"
	"runtime"
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

// TestManualConversionRegistrationsDisplaceSomething is the SOURCE direction of the same ledger: a
// registration must actually DISPLACE a generated body. The guard above asks whether the
// displacement ARRIVES; this one asks whether it ever DEPARTS, and the two fail apart.
//
// The defect, stated as the defect. `manualConversionFuncs` is keyed by NAME, and a Go METHOD's key
// carries its receiver — "Value.extendSlice", never the bare "extendSlice". The bare form matches no
// declaration, so the converter displaces nothing, the generated body survives, the hand-owned
// `_impl.cs` body beside it becomes a duplicate, and the package dies CS0111. The guard above passes
// cleanly on exactly that mistake, because the `_impl.cs` really does define the member — which is
// how the trap has been paid three separate times, most recently by reflect's `extendSlice`
// (2026-09-01). Worse than an ordinary build failure: a `-tests` build that fails leaves the PREVIOUS
// comparison record in place, so the run reports the old verdicts and reads as "the fix does not
// work" rather than as a compile error.
//
// The registry's OTHER field has had this guard for a while, with the reasoning already written
// down. TestEveryManualConversionScopeNamesAKnownGOOS exists because a scope naming "win" "matches no
// target at all, which silently turns the entry off everywhere — the auto body is emitted and
// compiles, and the hand-own it was protecting is simply gone." That sentence is true word for word
// of a mistyped NAME. Only the name field lacked the check.
//
// WITNESS. The converter writes one fixed line wherever it displaces a func body (visitFuncDecl.go's
// placeholder), so this is a filesystem scan of the same cost class as the guard above: no
// type-checking, no overload resolution, no Go toolchain.
//
// IN SYNC BY CONSTRUCTION, not by luck. A hand-own bank must ship its regenerated package with it, or
// the committed corpus carries BOTH the generated body and the new `_impl.cs` body and fails to
// compile. Registration and placeholder therefore land in the same commit, and this guard cannot red
// merely because the corpus lags the converter.
//
// WHAT IT CANNOT SEE, stated because a guard's blind spot is worth more written down than
// rediscovered. The placeholder names funcDecl.Name.Name — the member alone — so this test strips a
// key's receiver before matching and therefore cannot tell "Value.extendSlice" from a bare
// "extendSlice" while a placeholder for that member already sits in the corpus. That is not the
// trap's actual shape: a NEW hand-own keyed bare produces no placeholder at all (the same mechanism
// that makes reflect.methodName visible here — a key matching no declaration displaces nothing), and
// isManualFuncDecl is the single decision behind both the displacement and the placeholder, so
// witness and displacement cannot disagree. What survives is the narrow case of editing an ALREADY
// CORRECT key down to its bare form without regenerating, where the stale placeholder answers for
// the new key until the next regen — and the CS0111 that follows is caught by the corpus build, one
// layer out, exactly as the over-collection in handOwnedDefinitions above is.
//
// NO EXEMPTION LIST, and that was measured rather than assumed. The three `runtime` entries declared
// in runtime2.go look structurally unwitnessable — `runtime/runtime2.cs` is a whole-file hand-own, so
// the converter never emits that file — but it emits their placeholders into `runtime2.cs.auto`, the
// review sibling it writes for exactly that case, and searching the siblings takes the residual set
// to zero. The sibling is the right place to look, not a loophole: it is the converter's own record
// of what it WOULD emit, which is precisely this test's question. Note the asymmetry with
// handOwnedDefinitions above, which SKIPS `.cs.auto` — its question is "does a HAND-OWN define
// this?", and a sibling is not a hand-own. Same files, opposite treatment, both correct; do not
// "unify" them.
//
// manualConversionTypes is deliberately NOT covered. All three of its entries (guintptr, puintptr,
// muintptr) are witnessed in that same sibling, so a types arm would restate this one over three
// names and add no independent signal.
func TestManualConversionRegistrationsDisplaceSomething(t *testing.T) {
	coreDir := filepath.Join("..", "core")

	if _, err := os.Stat(coreDir); err != nil {
		t.Skip("src/core is not beside the converter; nothing to walk")
	}

	// The GOROOT the corpus was converted from — the source of the weaker, test-side witness below.
	goRoot := build.Default.GOROOT
	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	var undisplaced []string
	witnessed := 0
	testWitnessed := 0

	for pkg, funcs := range manualConversionFuncs {
		placeholders := generatedFuncPlaceholders(t, filepath.Join(coreDir, filepath.FromSlash(pkg)))

		// Lazily parsed on the first entry that misses its production placeholder — a hand-own of a
		// GOROOT `_test.go` declaration (reflect's export_test.go IsExported) has no on-disk
		// placeholder until reflect `-tests` has run in THIS tree, so a clone that has never run it
		// would report the entry undisplaced. Its Go declaration is in the package's own test files,
		// which every clone has, so that is the witness. Nil until needed; empty map means "parsed,
		// none found" (distinct from "not yet parsed").
		var testFuncs map[string]bool

		for name := range funcs {
			// A method registration names the RECEIVER type and the member; the placeholder names the
			// member alone, because it is written from funcDecl.Name.Name.
			member := name
			if dot := strings.LastIndex(member, "."); dot >= 0 {
				member = member[dot+1:]
			}

			if placeholders[member] {
				witnessed++
				continue
			}

			// Weaker witness: the production body was not displaced on disk, but the name IS a
			// declaration in the package's GOROOT test files — a test-only hand-own (export_test.go).
			// Tallied separately; the production arm above stays first and decides the common case.
			if testFuncs == nil {
				testFuncs = testDeclaredFuncs(goRoot, pkg)
			}
			if testFuncs[member] {
				testWitnessed++
				continue
			}

			undisplaced = append(undisplaced, pkg+"."+name)
		}
	}

	sort.Strings(undisplaced)

	// The weaker witness is tallied separately AND surfaced: an entry that relies on it has no
	// on-disk production placeholder, so a reviewer who sees this count non-zero can confirm each
	// such entry is a genuine GOROOT-test-file hand-own (IsExported; GCBits when it lands) rather
	// than a production displacement the strong arm should have caught.
	if testWitnessed > 0 {
		t.Logf("%d registration(s) witnessed only by their GOROOT _test.go declaration (no on-disk "+
			"production placeholder); this is the test-side hand-own case (e.g. reflect.IsExported)", testWitnessed)
	}

	for _, entry := range undisplaced {
		t.Errorf("manualConversionFuncs registers %s, but the converter displaced no body for it — the "+
			"entry matches no Go declaration in that package. A method key needs its receiver "+
			"(\"Value.extendSlice\", not \"extendSlice\"); a renamed or removed upstream declaration needs "+
			"the entry retired. Either way the generated body survives, a hand-owned one beside it is a "+
			"duplicate, and the package fails CS0111 — reported through a -tests run that reuses the "+
			"previous comparison record and so reads as a failed fix", entry)
	}

	// A census that finds nothing reports every registration as undisplaced, which reads as a
	// catastrophic registry rather than as a broken instrument. Anchor it, the way the scope guard
	// anchors its own.
	if witnessed == 0 {
		t.Error("no registration matched a generated placeholder anywhere in the corpus; the placeholder " +
			"census is broken, not the registry")
	}
}

// testDeclaredFuncs returns the set of top-level function and method NAMES declared in the GOROOT
// package's own `_test.go` files (both the in-package and the external `<pkg>_test` test files sit
// in the same directory). It is the weaker, test-side witness for a registration whose production
// body was not displaced on disk — a hand-own of a GOROOT test declaration
// (reflect/export_test.go's IsExported), whose generated placeholder exists only where the package's
// `-tests` conversion has run. Keyed by the member name (funcDecl.Name.Name), matching the
// production arm's receiver-stripped `member`. An unreadable/absent GOROOT package yields an empty
// set, which correctly leaves the entry undisplaced rather than silently witnessing it.
func testDeclaredFuncs(goRoot, pkg string) map[string]bool {
	names := map[string]bool{}

	if goRoot == "" {
		return names
	}

	dir := filepath.Join(goRoot, "src", filepath.FromSlash(pkg))

	entries, err := os.ReadDir(dir)
	if err != nil {
		return names
	}

	fset := token.NewFileSet()

	for _, entry := range entries {
		name := entry.Name()

		if entry.IsDir() || !strings.HasSuffix(name, "_test.go") {
			continue
		}

		file, parseErr := parser.ParseFile(fset, filepath.Join(dir, name), nil, 0)
		if parseErr != nil {
			continue
		}

		for _, decl := range file.Decls {
			if funcDecl, ok := decl.(*ast.FuncDecl); ok && funcDecl.Name != nil {
				names[funcDecl.Name.Name] = true
			}
		}
	}

	return names
}

// generatedFuncPlaceholders returns the member names the converter displaced a func body for in ONE
// package: the generated `.cs` at the package root, the per-GOOS folders layout L3 routes a
// platform-scoped declaration into, and the `.cs.auto` review siblings (see the caller's note on why
// the siblings count here and not in handOwnedDefinitions).
//
// Scope is the package's OWN files — root plus GOOS folders — not a full recursive walk. A converted
// package's subdirectories are usually OTHER packages (net/http holds cgi, httptest, …), and counting
// a child's placeholder for its parent would be a false PASS on exactly the question this test asks.
func generatedFuncPlaceholders(t *testing.T, packageDir string) map[string]bool {
	t.Helper()

	witnessed := map[string]bool{}

	dirs := []string{packageDir}

	// Layout L3's platform folders, and ONLY those: a GOOS-named subdirectory can also be a package
	// (`internal/syscall/windows` is the corpus's only one), but a package's placeholders sit in its
	// OWN platform folder — depth 2 from here — and this walk stops at depth 1, so a child package's
	// placeholders cannot answer for its parent. Measured 2026-09-01 rather than assumed:
	// `internal/syscall/windows/*.cs` carries no placeholder at all; all NINE of them are in
	// `internal/syscall/windows/windows/`, its own platform folder. A csproj test was written here
	// first and then removed: it could not be made to fire, because the depth rule already closes the
	// case, and an unexercisable branch in a guard is exactly what this file's neighbours refuse to
	// carry.
	if entries, err := os.ReadDir(packageDir); err == nil {
		for _, entry := range entries {
			if entry.IsDir() && isKnownGOOS(entry.Name()) {
				dirs = append(dirs, filepath.Join(packageDir, entry.Name()))
			}
		}
	}

	for _, dir := range dirs {
		entries, err := os.ReadDir(dir)
		if err != nil {
			continue
		}

		for _, entry := range entries {
			name := entry.Name()

			if entry.IsDir() || !(strings.HasSuffix(name, ".cs") || strings.HasSuffix(name, ".cs.auto")) {
				continue
			}

			content, readErr := os.ReadFile(filepath.Join(dir, name))
			if readErr != nil {
				continue
			}

			for _, match := range generatedFuncPlaceholder.FindAllStringSubmatch(string(content), -1) {
				witnessed[match[1]] = true
			}
		}
	}

	return witnessed
}

// The displacement witness, anchored on the converter's own prefix rather than on the trailing prose.
// `runtime/runtime2.cs` carries 13 HAND-WRITTEN lines ending in the same words ("func set is
// hand-converted with managed semantics — see the package's *_impl.cs"), a wording the converter emits
// nowhere; a pattern loose enough to match those would let a hand-own satisfy a guard about generated
// output, and this test would report clean while measuring nothing.
var generatedFuncPlaceholder = regexp.MustCompile(`(?m)^// go2cs generated this placeholder — func ([A-Za-z_][A-Za-z0-9_]*) is hand-converted\b`)
