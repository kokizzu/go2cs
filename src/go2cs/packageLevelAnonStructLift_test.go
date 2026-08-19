// packageLevelAnonStructLift_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Guards the two converter rules internal/reflectlite's test suite was the first thing in the
// corpus to reach, behind the local-value interface-cast fix:
//
//  1. Structurally IDENTICAL anonymous struct types are ONE Go type at PACKAGE level exactly as
//     inside a function. Splitting them per declaration makes the C# types un-unifiable where Go
//     unifies freely — `append(assignableTests, implementsTests...)` could not type (CS9244 +
//     CS8130 ×2 on the range deconstruction).
//
//  2. On the whitebox `-tests` variant, a PRODUCTION-declared type is not a LOCAL operand for a
//     GoImplicitConv record: its C# lives in the closed referenced production assembly, which the
//     generator cannot extend with an operator, so recording the pair declares a PHANTOM partial
//     in the test class whose members do not exist (CS1061).

package main

import (
	"go/build"
	"go/types"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"testing"
)

// TestPackageLevelAnonStructDedup pins that two package-level vars written over one anonymous
// struct type lift to a SINGLE C# type, that a structurally DIFFERENT anonymous struct keeps its
// own lift, and that a function-local identical struct stays in its own scope.
func TestPackageLevelAnonStructDedup(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: runs the real converter over a module fixture")
	}

	root := t.TempDir()
	appDir := filepath.Join(root, "app")

	writeModuleFile(t, filepath.Join(appDir, "go.mod"), "module example.com/anondedup\n\ngo 1.23\n")
	writeModuleFile(t, filepath.Join(appDir, "main.go"), `package main

import "fmt"

// The reflectlite shape: two package vars over ONE written anonymous struct type. Go's structural
// identity makes them one type, so the append below is legal Go and the emission must unify.
var implementsTests = []struct {
	x any
	b bool
}{
	{1, true},
}

var assignableTests = []struct {
	x any
	b bool
}{
	{2, false},
}

// A structurally DIFFERENT anonymous struct must keep its own lifted type.
var otherTests = []struct {
	y int
}{
	{3},
}

func main() {
	// The un-unifiable emission: both operands must be one C# type or neither T nor the
	// deconstruction infers.
	for i, tt := range append(assignableTests, implementsTests...) {
		fmt.Println(i, tt.x, tt.b)
	}

	// Scope separation: a function-local identical struct does NOT unify with the package-level
	// lift (the function-scoped dedup rule keys per function, unchanged).
	local := []struct {
		x any
		b bool
	}{
		{9, true},
	}
	fmt.Println(local[0].x, otherTests[0].y)
}
`)

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

	mainCs := readGenerated(t, filepath.Join(options.go2csPath, "src", "example.com", "anondedup", "main.cs"))

	// ONE lifted declaration for the shared signature: the first declaration's name wins and the
	// second declaration re-uses it (declared once, referenced twice).
	if got := strings.Count(mainCs, "partial struct implementsTests"+TempVarMarker+"1"); got != 1 {
		t.Errorf("the shared anonymous struct must be DECLARED exactly once, got %d:\n%s", got, mainCs)
	}

	if strings.Contains(mainCs, "partial struct assignableTests"+TempVarMarker+"1") {
		t.Errorf("the second var must not lift its own type — its struct is the FIRST var's type:\n%s", mainCs)
	}

	// Both vars type as slices of the ONE lifted type.
	if got := strings.Count(mainCs, "slice<implementsTests"+TempVarMarker+"1>"); got < 2 {
		t.Errorf("both package vars must type by the one lifted struct, found %d references:\n%s", got, mainCs)
	}

	// The structurally different struct keeps its own lift.
	if got := strings.Count(mainCs, "partial struct otherTests"+TempVarMarker+"1"); got != 1 {
		t.Errorf("a structurally DIFFERENT anonymous struct must keep its own lifted type, got %d:\n%s", got, mainCs)
	}

	// The function-local identical struct ADOPTS the package-level lift rather than minting a
	// function-scoped one: Go's struct identity is structural (tags included), so the local
	// composite literal IS the package-level type, and the adoption rule (see the xml `Child_G`
	// entry in ConversionStrategies-Reference) makes the emitted C# unify exactly as Go does.
	// This assertion originally pinned the opposite (a function-scoped lift) — that pin was about
	// the dedup REGISTRY's scope keying, not observable semantics, and the union of the two
	// 2026-08-18 lanes resolved it in favor of Go's structural identity: coordinator ruling at
	// the merge of claude/local-iface-cast x claude/escape-box-copy.
	if strings.Contains(mainCs, "partial struct main_local") {
		t.Errorf("a function-local identical struct must ADOPT the package-level lift, not mint its own:\n%s", mainCs)
	}
	if got := strings.Count(mainCs, "partial struct implementsTests"+TempVarMarker+"1"); got != 1 {
		t.Errorf("adoption must not duplicate the package-level declaration, got %d:\n%s", got, mainCs)
	}

	// ...and the local composite literal builds the adopted package-level struct (the two
	// package vars plus the local = three constructions; the local is `var`-typed so the
	// slice<T> spelling itself appears only on the package vars).
	if got := strings.Count(mainCs, "new implementsTests"+TempVarMarker+"1[]"); got != 3 {
		t.Errorf("the function-local composite must construct the adopted package-level struct, found %d constructions:\n%s", got, mainCs)
	}
}

// TestWhiteboxProductionNumericConvNotRecorded pins that a numeric GoImplicitConv pair whose
// operands are PRODUCTION-declared records nothing on the whitebox internal variant — and that a
// TEST-declared operand still records (the relocatable case recordsRequireProductionMutation's
// rule text names).
func TestWhiteboxProductionNumericConvNotRecorded(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads and converts a test-variant fixture")
	}

	dir := t.TempDir()
	files := map[string]string{
		"go.mod": "module example/wbnum\n\ngo 1.23\n",
		"value.go": "package wbnum\n\ntype flag uintptr\n\ntype Kind uint8\n\n" +
			"func Use(f flag, k Kind) bool { return uintptr(f) == uintptr(k) }\n",
		"export_test.go": "package wbnum\n\ntype testCounter uint32\n\n" +
			"func FlagOf(k Kind) flag { return flag(k) }\n\n" +
			"func CounterOf(k Kind) testCounter { return testCounter(k) }\n",
	}

	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}

	internal, _ := loadBothTestVariantsForDir(t, dir)

	if internal == nil {
		t.Fatal("the internal test variant was not loaded")
	}

	outputPath := t.TempDir()
	options := Options{indentSpaces: 4, preferVarDecl: true, useChannelOperators: true}

	// What convertTestVariants sets for the internal variant under the white-box REFERENCE model —
	// the arm where production C# is a closed referenced assembly.
	options.testClassNameOverride = getSanitizedImport("wbnum_internal_test" + PackageSuffix)
	options.testWhiteboxReference = true
	options.testInlineTypeAccess = true
	options.testProductionPath = "example/wbnum"

	testMethodRenames = make(map[types.Object]bool)
	t.Cleanup(func() { testMethodRenames = nil })

	if _, _, err := convertTestVariant(internal, testFileEntries(internal), outputPath, "go", productionSeed{}, options); err != nil {
		t.Fatal(err)
	}

	// The production×production pair (flag ← Kind) must NOT be recorded: the generator cannot host
	// an operator on either closed type, and the cast site's explicit chain needs none.
	for source, targets := range numericConversions {
		for target := range targets {
			if strings.Contains(source+"|"+target, "flag") {
				t.Errorf("a whitebox-production numeric pair must not record (phantom partial, CS1061): %s -> %s", source, target)
			}
		}
	}

	// The TEST-declared operand still records — the relocatable case: the operator hosts on the
	// test-local type.
	found := false

	for source, targets := range numericConversions {
		for target := range targets {
			if strings.Contains(source, "testCounter") || strings.Contains(target, "testCounter") {
				found = true
			}
		}
	}

	if !found {
		t.Errorf("a numeric pair with a TEST-LOCAL operand must still record (it relocates there); got %v", numericConversions)
	}
}
