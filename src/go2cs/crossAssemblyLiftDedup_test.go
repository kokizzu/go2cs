// crossAssemblyLiftDedup_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Guards the one rule the anonymous struct/interface dedup's PRODUCTION-registry arm has to obey
// that its same-pass arm does not: a reuse that crosses an ASSEMBLY boundary is admissible only when
// the reused declaration is REACHABLE from the assembly doing the reusing.
//
// Measured failure (2026-09-01; bisected, first bad commit 5442b402e "Residual pass round 3:
// anonymous struct/interface dedup, cross-variant and within-pass", last good its parent
// a5e3347f5). All four of `errors`' test files are `package errors_test`, so the package has no
// sibling internal test file, so its production .csproj carries no `InternalsVisibleTo`
// (insertFriendAssemblyAccess) and its external suite compiles into a plain referencing assembly.
// join_test.go's `err.(interface{ Unwrap() []error })` sits inside TestJoin — function-local, so the
// old guard's `v.inFunction` disjunct short-circuited unconditionally — and the conversion stopped
// declaring the local `TestJoin_typeᴛ1` it used to emit, binding production's `is_typeᴛ1` instead:
//
//	join_test.cs(49,48): error CS0122: 'errors_package.is_typeᴛ1' is inaccessible due to its
//	protection level
//
// The three arms below are one measurement with its own positive controls. The refusal arm alone
// could pass for the wrong reason — a seeded signature key that matches nothing refuses everything —
// so the two ADOPTION arms use the identical fixture, identical signature key and identical seeded
// registry, varying only the axes the rule reads (the candidate's own accessibility, and which test
// variant is converting). If the key were wrong, those two would fail where the refusal passes.

package main

import (
	"go/types"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"golang.org/x/tools/go/packages"
)

// crossAssemblyUnwrapSignature is the structural signature of `interface{ Unwrap() []error }` —
// errors' own shape, and the key GoDynamicTypeLift publishes / productionDynamicTypeNames is keyed
// by (see seedProductionDynamicTypeLifts). Spelled exactly as types.Type.String() renders it, the
// same contract runtimeScavengeIndexSignature (dynamicTypeGate_test.go) records for the struct side.
const crossAssemblyUnwrapSignature = "interface{Unwrap() []error}"

// crossAssemblyLiftFixture writes the two-variant fixture the arms share: a production package that
// does NOT itself write the anonymous interface (so nothing can register it in the running pass and
// only the SEEDED production registry can supply a candidate — the same isolation
// TestResolvedViaProductionRegistryLeavesTheGateGreen relies on), an INTERNAL test file and an
// EXTERNAL one that each write it inside a function, mirroring join_test.go's TestJoin.
func crossAssemblyLiftFixture(t *testing.T) string {
	t.Helper()

	dir := t.TempDir()

	files := map[string]string{
		"go.mod": "module example/caclift\n\ngo 1.23\n",
		"join.go": "package caclift\n\n" +
			"// Join is the production surface both variants reach. It deliberately does NOT write\n" +
			"// the anonymous interface: the candidate under test must come from the SEEDED\n" +
			"// production registry alone, never from this pass's own registrations.\n" +
			"func Join(errs ...error) error {\n" +
			"\tfor _, err := range errs {\n" +
			"\t\tif err != nil {\n" +
			"\t\t\treturn err\n" +
			"\t\t}\n" +
			"\t}\n\n" +
			"\treturn nil\n" +
			"}\n",
		"export_test.go": "package caclift\n\n" +
			"func UnwrapAll(err error) []error {\n" +
			"\tif x, ok := err.(interface{ Unwrap() []error }); ok {\n" +
			"\t\treturn x.Unwrap()\n" +
			"\t}\n\n" +
			"\treturn nil\n" +
			"}\n",
		"join_test.go": "package caclift_test\n\n" +
			"import (\n\t\"testing\"\n\n\t\"example/caclift\"\n)\n\n" +
			"func TestUnwrapJoined(t *testing.T) {\n" +
			"\terr := caclift.Join(nil)\n\n" +
			"\tif x, ok := err.(interface{ Unwrap() []error }); ok {\n" +
			"\t\t_ = x.Unwrap()\n" +
			"\t}\n" +
			"}\n",
	}

	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}

	return dir
}

// convertCrossAssemblyVariant converts one loaded variant with productionDynamicTypeNames seeded to
// map crossAssemblyUnwrapSignature onto candidate, and returns the emitted C#.
func convertCrossAssemblyVariant(t *testing.T, pkg *packages.Package, candidate string, external bool) string {
	t.Helper()

	outputPath := t.TempDir()

	options := Options{indentSpaces: 4, preferVarDecl: true, useChannelOperators: true}
	options.testExternalVariant = external

	seed := productionSeed{dynamicTypeNames: map[string]string{crossAssemblyUnwrapSignature: candidate}}

	testMethodRenames = make(map[types.Object]bool)
	defer func() { testMethodRenames = nil }()

	if _, _, err := convertTestVariant(pkg, testFileEntries(pkg), outputPath, "go", seed, options); err != nil {
		t.Fatalf("convertTestVariant: %v", err)
	}

	return readConvertedAssembly(t, outputPath)
}

// TestExternalVariantRefusesInternalProductionLift is the regression's own guard. The external
// suite compiles into its own assembly with no view of production's internals, so an INTERNAL
// production candidate must be refused and the lift minted locally — while the same fixture, same
// seeded registry and same signature adopt a PUBLIC candidate, and the INTERNAL variant (which does
// have package-private sight of production, by recompilation or by the IVT grant a package with
// internal test files always emits) adopts an internal one.
func TestExternalVariantRefusesInternalProductionLift(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads and converts a test-variant fixture")
	}

	dir := crossAssemblyLiftFixture(t)
	internal, external := loadBothTestVariantsForDir(t, dir)

	if internal == nil {
		t.Fatal("the internal test variant was not loaded")
	}

	if external == nil {
		t.Fatal("the external test variant was not loaded")
	}

	internalCandidate := "is_type" + TempVarMarker + "1"
	publicCandidate := "Is_type" + TempVarMarker + "1"

	// The seeded names must actually READ the way this test claims, or every arm below measures
	// something else. generatedTypeScope is the same reader the rule itself consults.
	if got := generatedTypeScope(internalCandidate); got != "internal" {
		t.Fatalf("the fixture's internal candidate %q reads %q, not internal", internalCandidate, got)
	}

	if got := generatedTypeScope(publicCandidate); got != "public" {
		t.Fatalf("the fixture's public candidate %q reads %q, not public", publicCandidate, got)
	}

	t.Run("external variant refuses an internal production candidate", func(t *testing.T) {
		withDynamicTypeRegistry(t, func() {
			emitted := convertCrossAssemblyVariant(t, external, internalCandidate, true)

			if strings.Contains(emitted, internalCandidate) {
				t.Errorf("the external suite adopted production's INTERNAL lift %q — it compiles into a separate assembly with no view of it (CS0122, the errors join_test.cs regression):\n%s", internalCandidate, emitted)
			}

			if !strings.Contains(emitted, "partial interface TestUnwrapJoined_type") {
				t.Errorf("refusing the production candidate must MINT a local lift; none was declared:\n%s", emitted)
			}
		})
	})

	t.Run("external variant adopts a public production candidate", func(t *testing.T) {
		withDynamicTypeRegistry(t, func() {
			emitted := convertCrossAssemblyVariant(t, external, publicCandidate, true)

			if !strings.Contains(emitted, publicCandidate) {
				t.Errorf("a PUBLIC production lift is reachable from the external assembly and must still dedup; %q was not adopted:\n%s", publicCandidate, emitted)
			}

			if strings.Contains(emitted, "partial interface TestUnwrapJoined_type") {
				t.Errorf("an adopted candidate must emit NO declaration of its own:\n%s", emitted)
			}
		})
	})

	t.Run("internal variant adopts an internal production candidate", func(t *testing.T) {
		withDynamicTypeRegistry(t, func() {
			emitted := convertCrossAssemblyVariant(t, internal, internalCandidate, false)

			if !strings.Contains(emitted, internalCandidate) {
				t.Errorf("the internal variant has package-private sight of production and must still dedup; %q was not adopted:\n%s", internalCandidate, emitted)
			}

			if strings.Contains(emitted, "partial interface UnwrapAll_type") {
				t.Errorf("an adopted candidate must emit NO declaration of its own:\n%s", emitted)
			}
		})
	})
}

// TestProductionLiftReuseReachable pins the predicate itself, including the arms the fixture-driven
// test above cannot reach cheaply: an empty candidate (no production hit at all) and a PRODUCTION
// conversion, where testExternalVariant is false because there is no test variant — harmless,
// because productionDynamicTypeNames is nil there and the caller never reaches this check.
func TestProductionLiftReuseReachable(t *testing.T) {
	cases := []struct {
		name      string
		candidate string
		external  bool
		want      bool
	}{
		{"no production hit", "", false, false},
		{"no production hit, external", "", true, false},
		{"internal candidate, internal variant", "is_type" + TempVarMarker + "1", false, true},
		{"internal candidate, external variant", "is_type" + TempVarMarker + "1", true, false},
		{"public candidate, external variant", "Is_type" + TempVarMarker + "1", true, true},
		{"public candidate, internal variant", "Is_type" + TempVarMarker + "1", false, true},
	}

	for _, c := range cases {
		t.Run(c.name, func(t *testing.T) {
			if got := productionLiftReuseReachable(c.candidate, Options{testExternalVariant: c.external}); got != c.want {
				t.Errorf("productionLiftReuseReachable(%q, external=%v) = %v, want %v", c.candidate, c.external, got, c.want)
			}
		})
	}
}
