// testConversion_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/build"
	"go/token"
	"go/types"
	"os"
	"path/filepath"
	"reflect"
	"runtime"
	"strings"
	"testing"

	"golang.org/x/tools/go/packages"
)

func TestIsGoTestName(t *testing.T) {
	tests := []struct {
		name string
		want bool
	}{
		{"Test", true},
		{"TestValue", true},
		{"Test_underscore", true},
		{"Testlower", false},
		{"BenchmarkValue", false},
	}

	for _, test := range tests {
		if got := isGoTestName(test.name, "Test"); got != test.want {
			t.Errorf("isGoTestName(%q, Test) = %v, want %v", test.name, got, test.want)
		}
	}
}

func TestManifestEligibility(t *testing.T) {
	manifest := testManifest{Tests: []testDeclaration{
		{Name: "BenchmarkOnly", Kind: "benchmark", Status: "unsupported"},
	}}
	if manifestHasEligibleTests(manifest) {
		t.Fatal("benchmark must not make a manifest test-eligible")
	}
	manifest.Tests = append(manifest.Tests, testDeclaration{Name: "TestValue", Kind: "test", Status: "included"})
	if !manifestHasEligibleTests(manifest) {
		t.Fatal("included TestValue should make a manifest test-eligible")
	}
}

func TestEligibleTerminalResultsExcludeUnsupportedTestKinds(t *testing.T) {
	manifest := testManifest{Tests: []testDeclaration{
		{Name: "TestValue", Kind: "test", Status: "included"},
		{Name: "ExampleValue", Kind: "example", Status: "unsupported"},
		{Name: "BenchmarkValue", Kind: "benchmark", Status: "unsupported"},
	}}
	got := eligibleTerminalTestResults(map[string]string{
		"TestValue":             "pass",
		"TestValue/child":       "pass",
		"ExampleValue":          "pass",
		"BenchmarkValue":        "pass",
		"UnregisteredTestValue": "pass",
	}, manifest)
	want := map[string]string{"TestValue": "pass", "TestValue/child": "pass"}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("eligible results = %#v, want %#v", got, want)
	}
}

// F1 guard: identical terminal statuses on both sides — including skip==skip — are agreement,
// with skips disclosed rather than flagged; any divergence or one-sided result is a mismatch.
func TestSkipParityCountsAsMatched(t *testing.T) {
	goResults := map[string]string{
		"TestPass":    "pass",
		"TestSkip":    "skip",
		"TestDiverge": "pass",
		"TestGoOnly":  "pass",
	}
	csResults := map[string]string{
		"TestPass":    "pass",
		"TestSkip":    "skip",
		"TestDiverge": "fail",
	}

	names := []string{"TestDiverge", "TestGoOnly", "TestPass", "TestSkip"}
	mismatches, skipped, disclosed := matchTerminalStatuses(names, goResults, csResults, nil, nil)

	if !reflect.DeepEqual(skipped, []string{"TestSkip"}) {
		t.Fatalf("skipped = %#v, want [TestSkip]", skipped)
	}
	if len(disclosed) != 0 {
		t.Fatalf("disclosed = %#v, want none — no disclosure manifest means strict comparison", disclosed)
	}
	if len(mismatches) != 2 {
		t.Fatalf("mismatches = %#v, want exactly TestDiverge and TestGoOnly", mismatches)
	}
	for _, mismatch := range mismatches {
		if !strings.HasPrefix(mismatch, "TestDiverge:") && !strings.HasPrefix(mismatch, "TestGoOnly:") {
			t.Fatalf("unexpected mismatch entry %q", mismatch)
		}
	}
}

// Address-pairing guards (X5, reflection-bridge chip): one-sided rows whose names differ only
// by run-varying 0x-hex address tokens pair onto a shared key; ambiguity or collision with an
// exact name leaves rows one-sided (fail loud, never mask); exact-matched names — including
// deterministic hex literals used as names — are never touched.
func TestPairAddressVariantNames(t *testing.T) {
	goResults := map[string]string{
		"TestAsValidation/*string(0xc0000f46b0)": "pass", // pairs 1:1
		"TestHex/0x1234":                         "pass", // exact-matched hex literal — untouched
		"TestAmbiguous/p(0xaaa)":                 "pass", // ambiguous on the C# side — untouched
		"TestPlain":                              "pass",
	}
	csResults := map[string]string{
		"TestAsValidation/*string(0x10d6126)": "pass",
		"TestHex/0x1234":                      "pass",
		"TestAmbiguous/p(0xbbb)":              "fail",
		"TestAmbiguous/p(0xccc)":              "pass",
		"TestPlain":                           "pass",
	}
	csOutputs := map[string]string{
		"TestAsValidation/*string(0x10d6126)": "captured output",
	}

	pairAddressVariantNames(goResults, csResults, csOutputs)

	if _, ok := goResults["TestAsValidation/*string(0x?)"]; !ok {
		t.Fatalf("go side not re-keyed: %#v", goResults)
	}
	if status, ok := csResults["TestAsValidation/*string(0x?)"]; !ok || status != "pass" {
		t.Fatalf("cs side not re-keyed: %#v", csResults)
	}
	if output := csOutputs["TestAsValidation/*string(0x?)"]; output != "captured output" {
		t.Fatalf("csOutputs did not follow the rename: %#v", csOutputs)
	}
	if _, ok := goResults["TestHex/0x1234"]; !ok {
		t.Fatal("exact-matched deterministic hex name was collapsed")
	}
	if _, ok := goResults["TestAmbiguous/p(0xaaa)"]; !ok {
		t.Fatalf("ambiguous row was re-keyed instead of staying one-sided: %#v", goResults)
	}
	if _, ok := csResults["TestAmbiguous/p(0xbbb)"]; !ok {
		t.Fatalf("ambiguous cs rows must keep their originals: %#v", csResults)
	}
}

// Disclosure-oracle guards: a hand-disclosed Go=pass/C#=fail divergence whose captured C#
// failure output carries the pinned signature is reclassified disclosed-divergent, NOT a
// mismatch; the SAME test failing with any other output is still a mismatch (the signature pin
// must catch a regression that changes the failure); and with no disclosure manifest at all the
// comparison stays strict.
func TestDisclosedDivergenceOracle(t *testing.T) {
	goResults := map[string]string{"TestBuilderAllocs": "pass", "TestHealthy": "pass"}
	csResults := map[string]string{"TestBuilderAllocs": "fail", "TestHealthy": "pass"}
	names := []string{"TestBuilderAllocs", "TestHealthy"}
	disclosures := map[string]testDisclosure{
		"TestBuilderAllocs": {Name: "TestBuilderAllocs", Class: "alloc-count-semantics", Signature: "Builder allocs = ", Reason: "AllocsPerRun shim is byte-derived"},
	}

	mismatches, _, disclosed := matchTerminalStatuses(names, goResults, csResults, disclosures,
		map[string]string{"TestBuilderAllocs": "builder_test.go:196: Builder allocs = 648; want 1"})
	if len(mismatches) != 0 || !reflect.DeepEqual(disclosed, []string{"TestBuilderAllocs"}) {
		t.Fatalf("matching signature must disclose, not mismatch: mismatches=%#v disclosed=%#v", mismatches, disclosed)
	}

	mismatches, _, disclosed = matchTerminalStatuses(names, goResults, csResults, disclosures,
		map[string]string{"TestBuilderAllocs": "builder_test.go:189: IndexRune('x') = 3; want 2"})
	if len(disclosed) != 0 || len(mismatches) != 1 || !strings.Contains(mismatches[0], "does not match the disclosed") {
		t.Fatalf("a different failure signature is a regression, not the disclosed divergence: mismatches=%#v disclosed=%#v", mismatches, disclosed)
	}

	mismatches, _, disclosed = matchTerminalStatuses(names, goResults, csResults, nil, nil)
	if len(disclosed) != 0 || len(mismatches) != 1 {
		t.Fatalf("no manifest must mean strict comparison: mismatches=%#v disclosed=%#v", mismatches, disclosed)
	}

	// Direction guard: a disclosure only ever covers Go=pass/C#=fail — never the reverse pair,
	// and never a status other than fail (an infrastructure-error is not the documented shape).
	mismatches, _, disclosed = matchTerminalStatuses([]string{"TestBuilderAllocs"},
		map[string]string{"TestBuilderAllocs": "fail"}, map[string]string{"TestBuilderAllocs": "pass"},
		disclosures, map[string]string{"TestBuilderAllocs": "Builder allocs = 648"})
	if len(disclosed) != 0 || len(mismatches) != 1 {
		t.Fatalf("Go=fail/C#=pass must never be disclosed: mismatches=%#v disclosed=%#v", mismatches, disclosed)
	}
	mismatches, _, disclosed = matchTerminalStatuses([]string{"TestBuilderAllocs"},
		map[string]string{"TestBuilderAllocs": "pass"}, map[string]string{"TestBuilderAllocs": "infrastructure-error"},
		disclosures, map[string]string{"TestBuilderAllocs": "Builder allocs = 648"})
	if len(disclosed) != 0 || len(mismatches) != 1 {
		t.Fatalf("C#=infrastructure-error must never be disclosed: mismatches=%#v disclosed=%#v", mismatches, disclosed)
	}

	// A disclosed test that agrees (pass==pass) is plain agreement — the stale disclosure is inert.
	mismatches, _, disclosed = matchTerminalStatuses([]string{"TestBuilderAllocs"},
		map[string]string{"TestBuilderAllocs": "pass"}, map[string]string{"TestBuilderAllocs": "pass"},
		disclosures, nil)
	if len(disclosed) != 0 || len(mismatches) != 0 {
		t.Fatalf("an agreeing disclosed test is plain agreement: mismatches=%#v disclosed=%#v", mismatches, disclosed)
	}
}

// Loader guards: an absent manifest is the normal no-disclosure case; a present manifest must be
// complete — an empty signature would substring-match ANY failure and defeat the integrity pin,
// and duplicate names would make the pin ambiguous.
func TestDisclosureManifestLoading(t *testing.T) {
	dir := t.TempDir()

	disclosures, err := loadTestDisclosures(dir)
	if err != nil || disclosures != nil {
		t.Fatalf("absent manifest should load as nil, nil — got %#v, %v", disclosures, err)
	}

	writeManifest := func(content string) {
		if err := os.WriteFile(filepath.Join(dir, testDisclosureFileName), []byte(content), 0644); err != nil {
			t.Fatal(err)
		}
	}

	writeManifest(`{"schemaVersion": 1, "disclosures": [
		{"name": "TestBuilderAllocs", "class": "alloc-count-semantics", "signature": "Builder allocs = ", "reason": "shim is byte-derived"}]}`)
	disclosures, err = loadTestDisclosures(dir)
	if err != nil || len(disclosures) != 1 || disclosures["TestBuilderAllocs"].Signature != "Builder allocs = " {
		t.Fatalf("valid manifest should parse — got %#v, %v", disclosures, err)
	}

	writeManifest(`{"schemaVersion": 1, "disclosures": [
		{"name": "TestBuilderAllocs", "class": "alloc-count-semantics", "signature": "", "reason": "shim is byte-derived"}]}`)
	if _, err = loadTestDisclosures(dir); err == nil {
		t.Fatal("an empty signature must be rejected — it would match any failure")
	}

	writeManifest(`{"schemaVersion": 1, "disclosures": [
		{"name": "TestX", "class": "alloc-profile", "signature": "a", "reason": "r"},
		{"name": "TestX", "class": "alloc-profile", "signature": "b", "reason": "r"}]}`)
	if _, err = loadTestDisclosures(dir); err == nil {
		t.Fatal("duplicate disclosure names must be rejected")
	}
}

// F2/F3 guard: every disclosed-unsupported declaration (examples, benchmarks, capability-blocked
// tests, blocked TestMain) appears in the comparison record's exclusion list — never silently
// filtered.
func TestExcludedDeclarationsAreDisclosed(t *testing.T) {
	manifest := testManifest{
		Tests: []testDeclaration{
			{Name: "TestIncluded", Kind: "test", Status: "included"},
			{Name: "ExampleValue", Kind: "example", Status: "unsupported", Reason: "example execution is deferred to Phase 4D"},
			{Name: "BenchmarkValue", Kind: "benchmark", Status: "unsupported", Reason: "benchmark execution is deferred to Phase 4D"},
			{Name: "TestBlocked", Kind: "test", Status: "unsupported", Reason: unsupportedCapabilityReasonPrefix + "T.Deadline"},
		},
		TestMain: &testDeclaration{Name: "TestMain", Kind: "test-main", Status: "unsupported", Reason: unsupportedCapabilityReasonPrefix + "M.SomethingNew"},
	}

	excluded := excludedDeclarations(manifest)
	want := []string{
		"ExampleValue (example): example execution is deferred to Phase 4D",
		"BenchmarkValue (benchmark): benchmark execution is deferred to Phase 4D",
		"TestBlocked (test): " + unsupportedCapabilityReasonPrefix + "T.Deadline",
		"TestMain (test-main): " + unsupportedCapabilityReasonPrefix + "M.SomethingNew",
	}
	if !reflect.DeepEqual(excluded, want) {
		t.Fatalf("excluded = %#v, want %#v", excluded, want)
	}
}

// F4 guard: a capability-blocked TestMain gates the whole package; blocked tests gate the
// package ONLY when no runnable test remains.
func TestManifestCapabilityBlockScope(t *testing.T) {
	blockedTest := testDeclaration{Name: "TestBlocked", Kind: "test", Status: "unsupported", Reason: unsupportedCapabilityReasonPrefix + "T.Deadline"}
	includedTest := testDeclaration{Name: "TestRuns", Kind: "test", Status: "included"}

	if blocked := manifestCapabilityBlock(testManifest{Tests: []testDeclaration{blockedTest, includedTest}}); blocked != nil {
		t.Fatalf("a blocked test among runnable siblings must not block the package, got %v", blocked)
	}

	if blocked := manifestCapabilityBlock(testManifest{Tests: []testDeclaration{blockedTest}}); !reflect.DeepEqual(blocked, []string{"T.Deadline"}) {
		t.Fatalf("all-blocked package should report [T.Deadline], got %v", blocked)
	}

	manifest := testManifest{
		Tests:    []testDeclaration{includedTest},
		TestMain: &testDeclaration{Name: "TestMain", Kind: "test-main", Status: "unsupported", Reason: unsupportedCapabilityReasonPrefix + "M.SomethingNew"},
	}
	if blocked := manifestCapabilityBlock(manifest); !reflect.DeepEqual(blocked, []string{"M.SomethingNew"}) {
		t.Fatalf("blocked TestMain must gate the package, got %v", blocked)
	}
}

// F6 guard: the census gate compares the RAW go test results against the manifest's
// declarations — a name go test ran that the manifest never declared (under ANY status) fails
// the comparison, closing the shared-filter silent-pass channel between discovery and
// comparison. Subtests roll up to their top-level parent; examples/fuzz seed-corpus runs are
// accounted through their disclosed-unsupported declarations.
func TestManifestCensusDetectsUndeclaredTests(t *testing.T) {
	manifest := testManifest{
		Tests: []testDeclaration{
			{Name: "TestKnown", Kind: "test", Status: "included"},
			{Name: "ExampleValue", Kind: "example", Status: "unsupported"},
			{Name: "FuzzSeed", Kind: "fuzz", Status: "unsupported"},
		},
		TestMain: &testDeclaration{Name: "TestMain", Kind: "test-main", Status: "included"},
	}

	goResults := map[string]string{
		"TestKnown":       "pass",
		"TestKnown/child": "pass",
		"ExampleValue":    "pass",
		"FuzzSeed":        "pass",
	}
	if gaps := manifestCensusGaps(goResults, manifest); len(gaps) != 0 {
		t.Fatalf("fully declared results should have no census gaps, got %v", gaps)
	}

	goResults["TestGhost"] = "pass"
	goResults["TestGhost/sub"] = "pass"
	goResults["TestPhantom"] = "fail"
	if gaps := manifestCensusGaps(goResults, manifest); !reflect.DeepEqual(gaps, []string{"TestGhost", "TestPhantom"}) {
		t.Fatalf("census gaps = %v, want [TestGhost TestPhantom]", gaps)
	}
}

// ONE-TREE guard (2026-08-01, replacing the F15/F15b mixed-tree remap guards). The converted
// standard library lives at src/core — the exact path every resolver already emits — so a test
// project's stdlib dependencies are used VERBATIM, with no tree mapping in between. What still
// needs guarding is the invariant that made the remap unnecessary: `testing` must never be
// queued for conversion, or a second [GoPackage("testing")] `testing_package` lands beside the
// hand-owned host and every testing type goes ambiguous (CS0433, reached via internal/testenv).
func TestStdLibConversionSkipsHandOwnedAndToolchainPackages(t *testing.T) {
	for _, skipped := range []string{"unsafe", "builtin", "testing", "cmd", "cmd/compile", "cmd/go/internal/work"} {
		if !isNonConvertedStdLibPackage(skipped) {
			t.Errorf("%q must be excluded from the stdlib conversion queue", skipped)
		}
	}

	// Only `testing` itself is hand-owned — its subpackages are ordinary converted packages,
	// and a prefix test instead of an exact match would silently drop all five of them.
	for _, converted := range []string{
		"bytes", "testing/quick", "testing/fstest", "testing/iotest", "testing/slogtest",
		"testing/internal/testdeps", "internal/testenv", "unsafeptr",
	} {
		if isNonConvertedStdLibPackage(converted) {
			t.Errorf("%q must be converted, not skipped", converted)
		}
	}
}

// Every converted test project carries the shared runtime and the hand-owned testing package as
// fixed references, both rooted in the one converted-standard-library tree — and, since F5, spelled
// with FORWARD slashes like every other emitted reference, so one corpus form serves every host.
func TestTestProjectFixedReferencesRootedInCore(t *testing.T) {
	want := []string{`$(go2csPath)core/golib/golib.csproj`, `$(go2csPath)core/testing/testing.csproj`}

	if !reflect.DeepEqual(testProjectFixedReferences, want) {
		t.Fatalf("test project fixed references = %v, want %v", testProjectFixedReferences, want)
	}
}

// B2b guard: the embedded test project template pins DisableTransitiveProjectReferences so the
// test compilation's reference view is EXACTLY the direct refs the test converter computed — a
// transitive ref contributing a child `go.<pkg>` namespace (internal/testenv -> io/fs, go.io)
// collides with the converter's in-namespace `using io = io_package;` alias emission (CS0576).
func TestTestProjectTemplateDisablesTransitiveProjectReferences(t *testing.T) {
	if !strings.Contains(string(testCsprojTemplate), "<DisableTransitiveProjectReferences>true</DisableTransitiveProjectReferences>") {
		t.Fatal("test-csproj-template.xml must set DisableTransitiveProjectReferences=true")
	}
}

// B3 guard: package_test_info.cs brings the EXTERNAL test package class into `using static`
// scope beside the production class — metadata attributes merged from the test variants can
// reference types declared in <pkg>_test (an errWriter helper cast to io.Writer), which the
// seeded production-only using cannot resolve. Also guards the appended [GoPackage] anchor
// block and re-run idempotence.
func TestAppendExternalTestPackageClassAddsTestUsingAndAnchor(t *testing.T) {
	dir := t.TempDir()
	fileName := filepath.Join(dir, "package_test_info.cs")

	seed := "using go;\r\nusing static go.value_package;\r\n\r\nnamespace go;\r\n\r\n[GoPackage(\"value\")]\r\npublic static partial class value_package\r\n{\r\n}\r\n"
	if err := os.WriteFile(fileName, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	external := &packages.Package{Name: "value_test"}
	if err := appendExternalTestPackageClass(fileName, "go", "value", external); err != nil {
		t.Fatal(err)
	}

	data, err := os.ReadFile(fileName)
	if err != nil {
		t.Fatal(err)
	}

	contents := string(data)
	if !strings.Contains(contents, "using static go.value_package;\r\nusing static go.value_test_package;") {
		t.Fatalf("test package using must follow the production using:\n%s", contents)
	}
	if !strings.Contains(contents, "[GoPackage(\"value_test\")]\r\npublic static partial class value_test_package\r\n{\r\n}") {
		t.Fatalf("external test package anchor class must be appended:\n%s", contents)
	}

	if err := appendExternalTestPackageClass(fileName, "go", "value", external); err != nil {
		t.Fatal(err)
	}
	again, err := os.ReadFile(fileName)
	if err != nil {
		t.Fatal(err)
	}
	if string(again) != contents {
		t.Fatalf("appendExternalTestPackageClass must be idempotent:\nfirst:\n%s\nsecond:\n%s", contents, string(again))
	}
}

// Change C guard: black-box suites reference production directly; suites with an internal
// variant use the white-box reference model.
func TestSelectTestProjectModel(t *testing.T) {
	internal := &packages.Package{Name: "value"}
	external := &packages.Package{Name: "value_test"}

	if got := selectTestProjectModel(nil, external); got != testProjectReference {
		t.Fatalf("black-box-only model = %v, want reference", got)
	}
	if got := selectTestProjectModel(internal, external); got != testProjectWhiteboxReference {
		t.Fatalf("mixed-suite model = %v, want whitebox-reference", got)
	}
	if got := selectTestProjectModel(internal, nil); got != testProjectWhiteboxReference {
		t.Fatalf("internal-only model = %v, want whitebox-reference", got)
	}
}

// loadTestVariantsForDir loads a throwaway module's production, internal-test, and external-test
// variants for exercising the go/types-driven file-exclusion predicate. Either variant may be nil
// (an internal-only or black-box-only suite).
func loadTestVariantsForDir(t *testing.T, dir string) (internal, external *packages.Package) {
	t.Helper()

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: dir, Tests: true}, ".")
	if err != nil {
		t.Fatal(err)
	}
	production := findProductionPackage(loaded, dir)
	if production == nil {
		t.Fatal("production package was not loaded")
	}
	return findTestVariants(loaded, production)
}

// excludedBaseNames reduces the cleaned-path exclusion set to a basename set for readable asserts.
func excludedBaseNames(excluded map[string]bool) map[string]bool {
	names := make(map[string]bool, len(excluded))
	for path := range excluded {
		names[filepath.Base(path)] = true
	}
	return names
}

// Phase-4D file-exclusion positive path (option-a ruling): an EXTERNAL Example-only file and an
// INTERNAL Benchmark-only file are both dropped from the compile set (nothing they declare reaches
// the run registry), while a file carrying a real Test function is retained. This is the go/token
// shape — example_test.go (package token_test, one Example) + position_bench_test.go (one
// Benchmark) — the CS0012 whitebox+blackbox unblock. The predicate is pure go/ast+go/types: a
// text scan would miss that example_test.go's only TOP-LEVEL declaration is the Example (its
// var/const/type/func tokens live inside a raw-string literal).
func TestSelectCompileExcludedTestFilesDropsExampleAndBenchmarkOnly(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/exclude\n\ngo 1.23\n",
		"lib.go": "package exclude\n\nfunc Search(n int) int { return n }\n",
		"lib_test.go": "package exclude\n\nimport \"testing\"\n\n" +
			"func TestSearch(t *testing.T) {\n\tif Search(2) != 2 {\n\t\tt.Fatal(\"bad\")\n\t}\n}\n",
		"bench_test.go": "package exclude\n\nimport \"testing\"\n\n" +
			"func BenchmarkSearch(b *testing.B) {\n\tfor i := 0; i < b.N; i++ {\n\t\tSearch(i)\n\t}\n}\n",
		"example_test.go": "package exclude_test\n\nimport (\n\t\"fmt\"\n\n\t\"example/exclude\"\n)\n\n" +
			"func ExampleSearch() {\n\tfmt.Println(exclude.Search(1))\n\t// Output: 1\n}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)
	if internal == nil || external == nil {
		t.Fatalf("expected both internal and external test variants, got internal=%v external=%v", internal, external)
	}

	got := excludedBaseNames(selectCompileExcludedTestFiles(internal, external))
	want := map[string]bool{"bench_test.go": true, "example_test.go": true}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("excluded files = %v, want %v", got, want)
	}
}

// Condition (1) POSITIVE, widened arm (2026-08-15, crypto/tls): an Example-only file that also
// declares a pure helper TYPE and its METHODS is excluded. crypto/tls's example_test.go is the
// package's only black-box file and every runnable thing in it is an Example, but its Examples need
// an io.Reader to hand Config.Rand, so it declares `type zeroSource struct{}` with a `Read` method —
// and that one helper kept the whole file compiled, which under the recompile model is exactly the
// CS0012 cross-assembly duplication the ruling exists to prevent (3 of the package's 4 build errors).
// A type and its methods run nothing at package init, so admitting them costs no runtime behavior.
func TestSelectCompileExcludedTestFilesDropsExampleWithHelperType(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/helpertype\n\ngo 1.23\n",
		"lib.go": "package helpertype\n\nfunc Read(r interface{ Read([]byte) (int, error) }) int {\n\tn, _ := r.Read(make([]byte, 4))\n\treturn n\n}\n",
		"lib_test.go": "package helpertype\n\nimport \"testing\"\n\n" +
			"func TestPresent(t *testing.T) {}\n",
		"example_test.go": "package helpertype_test\n\nimport (\n\t\"fmt\"\n\n\t\"example/helpertype\"\n)\n\n" +
			"type zeroSource struct{}\n\n" +
			"func (zeroSource) Read(b []byte) (int, error) { return len(b), nil }\n\n" +
			"func ExampleRead() {\n\tfmt.Println(helpertype.Read(zeroSource{}))\n\t// Output: 4\n}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)
	got := excludedBaseNames(selectCompileExcludedTestFiles(internal, external))
	want := map[string]bool{"example_test.go": true}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("excluded files = %v, want %v", got, want)
	}
}

// Condition (2) over the widened arm: the helper TYPE a retained test references pulls its file back
// in. Widening condition (1) without recording the type/method objects in `declared` would have
// silently disarmed condition (2) for exactly the declarations it just admitted — the file would be
// dropped and the retained test's reference left undefined.
func TestSelectCompileExcludedTestFilesKeepsHelperTypeUsedByRetainedTest(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/sharedtype\n\ngo 1.23\n",
		"lib.go": "package sharedtype\n\nfunc Size(b []byte) int { return len(b) }\n",
		"example_test.go": "package sharedtype_test\n\nimport (\n\t\"fmt\"\n\n\t\"example/sharedtype\"\n)\n\n" +
			"type zeroSource struct{}\n\n" +
			"func (zeroSource) Read(b []byte) (int, error) { return sharedtype.Size(b), nil }\n\n" +
			"func ExampleSize() {\n\tfmt.Println(sharedtype.Size(nil))\n\t// Output: 0\n}\n",
		"use_test.go": "package sharedtype_test\n\nimport \"testing\"\n\n" +
			"func TestUsesHelper(t *testing.T) {\n\tvar z zeroSource\n\tif n, _ := z.Read(make([]byte, 3)); n != 3 {\n\t\tt.Fatal(\"bad\")\n\t}\n}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)
	got := excludedBaseNames(selectCompileExcludedTestFiles(internal, external))
	if len(got) != 0 {
		t.Fatalf("a helper type a retained test references must keep its file compiled; got %v", got)
	}
}

// Condition (1) negative: an Example-only-LOOKING file that also declares a top-level var/const
// is NOT excluded — imports do not count and pure type/method declarations are admitted (above), but
// a var/const initializer can carry side effects, so it disqualifies the whole file (conservative by
// design) and its Example still compiles alongside the disqualifying declaration.
func TestSelectCompileExcludedTestFilesKeepsExampleWithTopLevelVar(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/keepvar\n\ngo 1.23\n",
		"lib.go": "package keepvar\n",
		"lib_test.go": "package keepvar\n\nimport \"testing\"\n\n" +
			"func TestPresent(t *testing.T) {}\n",
		"example_test.go": "package keepvar_test\n\nimport \"fmt\"\n\n" +
			"var greeting = \"hi\"\n\n" +
			"func ExampleGreeting() {\n\tfmt.Println(greeting)\n\t// Output: hi\n}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)
	got := excludedBaseNames(selectCompileExcludedTestFiles(internal, external))
	if got["example_test.go"] {
		t.Fatalf("a file with a top-level var must not be excluded; got %v", got)
	}
	if len(got) != 0 {
		t.Fatalf("expected no exclusions, got %v", got)
	}
}

// Condition (2) negative + the promotion fixpoint: an Example-only file whose Example symbol a
// RETAINED test references must stay compiled — dropping it would leave the reference undefined.
// wired_test.go qualifies under condition (1) but registry_test.go (a real Test) takes ExampleWired
// as a value, so object identity (not text) pulls wired_test.go back into the compile set.
func TestSelectCompileExcludedTestFilesKeepsReferencedExample(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/referenced\n\ngo 1.23\n",
		"lib.go": "package referenced\n",
		"wired_test.go": "package referenced\n\nimport \"fmt\"\n\n" +
			"func ExampleWired() {\n\tfmt.Println(\"wired\")\n\t// Output: wired\n}\n",
		"registry_test.go": "package referenced\n\nimport \"testing\"\n\n" +
			"var examples = []func(){ExampleWired}\n\n" +
			"func TestRun(t *testing.T) {\n\tfor _, e := range examples {\n\t\te()\n\t}\n}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)
	got := excludedBaseNames(selectCompileExcludedTestFiles(internal, external))
	if got["wired_test.go"] {
		t.Fatalf("an Example referenced by a retained test must not be excluded; got %v", got)
	}
	if len(got) != 0 {
		t.Fatalf("expected no exclusions, got %v", got)
	}
}

// A TestMain or Fuzz function keeps a file compiled even though both are Phase-4D-deferred at the
// run registry: the ruling scopes the FILE predicate to Example/Benchmark only, so neither a
// TestMain-only nor a Fuzz-only file qualifies (conservative by design — they still compile).
func TestSelectCompileExcludedTestFilesKeepsTestMainAndFuzzOnly(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/nonexcluded\n\ngo 1.23\n",
		"lib.go": "package nonexcluded\n",
		"lib_test.go": "package nonexcluded\n\nimport \"testing\"\n\n" +
			"func TestPresent(t *testing.T) {}\n",
		"main_test.go": "package nonexcluded\n\nimport (\n\t\"os\"\n\t\"testing\"\n)\n\n" +
			"func TestMain(m *testing.M) {\n\tos.Exit(m.Run())\n}\n",
		"fuzz_test.go": "package nonexcluded\n\nimport \"testing\"\n\n" +
			"func FuzzThing(f *testing.F) {\n\tf.Fuzz(func(t *testing.T, b []byte) {})\n}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)
	got := excludedBaseNames(selectCompileExcludedTestFiles(internal, external))
	if len(got) != 0 {
		t.Fatalf("TestMain-only and Fuzz-only files must not be excluded, got %v", got)
	}
}

// Change C fallback gate: a reference-model conversion must fall back to the recompile model
// exactly when the external variant's records demand a PRODUCTION anchor — a partial/adapter
// merged into a production type declaration, impossible across an assembly boundary. Records
// that anchor to the test class (bare test-local impls, adapter-class pairs) must NOT trigger
// the fallback, or every black-box package with any interface cast loses the model.
func TestRecordsRequireProductionAnchorGatesReferenceModel(t *testing.T) {
	resetPackageState(&packages.Package{})
	packageNamespace = "go"

	if recordsRequireProductionAnchor("value_package", "value") {
		t.Fatal("an empty record set must not require a production anchor")
	}

	// A bare (test-package-local) implementer and an adapter-class-marked foreign pair both
	// generate in the test class — no fallback.
	interfaceImplementations["io_package.Writer"] = NewHashSet([]string{"errWriter"})
	adapterClassImplementations.Add("io_package.Writer|strings_package.Builder")
	interfaceImplementations["io_package.Writer"].Add("strings_package.Builder")

	if recordsRequireProductionAnchor("value_package", "value") {
		t.Fatal("test-anchored records (bare impl, adapter-class pair) must not require a production anchor")
	}

	// A production-qualified pointer implementer needs a ж adapter generated on the production
	// class — reference model impossible, fallback required.
	interfaceImplementations["io_package.Writer"].Add(PointerPrefix + "<value_package.Buffer>")

	if !recordsRequireProductionAnchor("value_package", "value") {
		t.Fatal("a production-qualified pointer implementer must require the production anchor")
	}

	// A record rendering a production type through its imported ꓸ alias form hides the
	// production qualifier from the partition predicates — conservatively production-anchored.
	resetPackageState(&packages.Package{})
	packageNamespace = "go"
	implicitConversions["value"+TypeAliasDot+"Kind"] = NewHashSet([]string{"@string"})

	if !recordsRequireProductionAnchor("value_package", "value") {
		t.Fatal("a ꓸ-alias-form production type reference must require the production anchor")
	}
}

// White-box reference fallback: pointer/value adapters can live in the test metadata anchor, but
// conversion operators that would have to extend a referenced production type cannot.
func TestRecordsRequireProductionMutationGatesWhiteboxModel(t *testing.T) {
	resetPackageState(&packages.Package{})
	packageNamespace = "go"

	interfaceImplementations["io_package.Writer"] = NewHashSet([]string{PointerPrefix + "<value_package.Buffer>"})
	indirectSource := ShadowVarMarker + "value.Source"
	indirectImplicitConversions[indirectSource] = NewHashSet([]string{PointerPrefix + "<" + indirectSource + ">"})
	if recordsRequireProductionMutation("value_package", "value") {
		t.Fatal("production adapters and the shared T-to-pointer-box route are relocatable")
	}

	implicitConversions["value_package.Source"] = NewHashSet([]string{"LocalTarget"})
	if !recordsRequireProductionMutation("value_package", "value") {
		t.Fatal("a structural conversion involving a closed production type must fall back")
	}

	resetPackageState(&packages.Package{})
	packageNamespace = "go"
	numericConversions["value_package.Source"] = map[string]string{"LocalTarget": "int64"}
	if recordsRequireProductionMutation("value_package", "value") {
		t.Fatal("a numeric conversion can relocate to its test-local operand")
	}
	numericConversions["value_package.Source"]["value_package.Target"] = "int64"
	if !recordsRequireProductionMutation("value_package", "value") {
		t.Fatal("a numeric conversion between two closed production types must fall back")
	}
}

// Internal test declarations keep their Go package name in the manifest, while the generated host
// targets the separate friend-assembly bridge class that actually owns their converted methods.
func TestWriteTestHostUsesCSharpClassOverride(t *testing.T) {
	dir := t.TempDir()
	declarations := []testDeclaration{{
		Name: "TestInternal", Kind: "test", PackageName: "value", CSharpClassName: "value_internal_test_package",
		Source: "value_test.go", Line: 12, Status: "included",
	}}
	if err := writeTestHost(dir, "go", "example/value", declarations, nil, nil, nil); err != nil {
		t.Fatal(err)
	}
	data, err := os.ReadFile(filepath.Join(dir, testHostFileName))
	if err != nil {
		t.Fatal(err)
	}
	contents := string(data)
	if !strings.Contains(contents, `registry.Add("TestInternal", value_internal_test_package.TestInternal`) {
		t.Fatalf("host must target the white-box bridge class:\n%s", contents)
	}
	if strings.Contains(contents, `value_package.TestInternal`) {
		t.Fatalf("host must not target the referenced production class:\n%s", contents)
	}
}

// The run directory reproduces the package directory's SHAPE, not only its files: a test that asks
// what its working directory contains (os TestReadDir looks for the `exec` subdirectory beside
// read_test.go) must see the same immediate subdirectories `go test` shows it. Names only, sorted,
// one level deep — and carried to the host as its own list so the fixture COPY pass is unaffected.
func TestFixtureDirectoriesStagePackageShape(t *testing.T) {
	dir := t.TempDir()

	for _, name := range []string{"user", "exec", "testdata"} {
		if err := os.MkdirAll(filepath.Join(dir, name, "nested"), 0755); err != nil {
			t.Fatal(err)
		}
	}
	if err := os.WriteFile(filepath.Join(dir, "read_test.go"), []byte("package os\n"), 0644); err != nil {
		t.Fatal(err)
	}

	directories, err := testFixtureDirectories(dir)
	if err != nil {
		t.Fatal(err)
	}
	if got, want := strings.Join(directories, ","), "exec,testdata,user"; got != want {
		t.Fatalf("fixture directories = %q, want the sorted immediate subdirectories %q", got, want)
	}

	out := t.TempDir()
	if err := writeTestHost(out, "go", "os", nil, nil, []string{"read_test.go"}, directories); err != nil {
		t.Fatal(err)
	}
	data, err := os.ReadFile(filepath.Join(out, testHostFileName))
	if err != nil {
		t.Fatal(err)
	}
	contents := string(data)

	// The fixture FILES and the directory names are separate arguments — a directory has no build
	// output to copy, so it must not reach the fixture list the host copies from.
	if !strings.Contains(contents, "\"read_test.go\",") {
		t.Fatalf("host must carry the fixture files:\n%s", contents)
	}
	for _, name := range directories {
		if !strings.Contains(contents, "\""+name+"\",") {
			t.Fatalf("host must carry the %q run directory:\n%s", name, contents)
		}
	}
	if strings.Index(contents, "\"read_test.go\",") > strings.Index(contents, "\"exec\",") {
		t.Fatalf("the fixture list must precede the directory list:\n%s", contents)
	}

	// A package with NO subdirectories omits the argument entirely rather than emitting an empty
	// array, so its host stays byte-identical to one generated before this capability existed —
	// which is what keeps a banked host out of the diff for a run environment that did not change.
	bare := t.TempDir()
	if err := writeTestHost(bare, "go", "cmp", nil, nil, []string{"cmp.go"}, nil); err != nil {
		t.Fatal(err)
	}
	data, err = os.ReadFile(filepath.Join(bare, testHostFileName))
	if err != nil {
		t.Fatal(err)
	}
	if strings.Contains(string(data), "new string[]\r\n        {\r\n        }") || strings.Count(string(data), "new string[]") != 1 {
		t.Fatalf("a package with no subdirectories must emit ONE array:\n%s", data)
	}
}

func TestWhiteboxAdapterAnchoringOnlyRelocatesEmittedPairs(t *testing.T) {
	pairs := [][2]string{{"io_package.PipeWriter", "io_package.Writer"}}
	pair, ok := emittedAdapterPair(pairs, "io_PipeWriter", "io_package.Writer")
	if !ok {
		t.Fatal("the test-owned PipeWriter adapter pair must be recognized across record/cast spellings")
	}
	// The anchored member composes from the RECORD's spelling — the generator's foreign
	// `<pkg>_<Simple>` form — never from the cast site's, whose bare spelling would compose a
	// class name go2cs-gen does not generate (the encoding/csv `ParseErrorжerror` defect).
	if got := anchoredAdapterMemberName(pair, map[string]bool{}); got != "io_PipeWriter"+PointerPrefix+"Writer" {
		t.Fatalf("anchored member = %q, want the generator's io_PipeWriter%sWriter", got, PointerPrefix)
	}
	if _, ok := emittedAdapterPair(pairs, "Δio.LimitedReader", "io_package.Reader"); ok {
		t.Fatal("an imported production adapter must not be redirected into the test metadata anchor")
	}
	// A BARE cast spelling of a recorded qualified pair still matches — and still composes the
	// generator's qualified name from the record side.
	pair, ok = emittedAdapterPair(pairs, "PipeWriter", "io_package.Writer")
	if !ok {
		t.Fatal("a bare cast spelling of a recorded pair must match on the simple name")
	}
	if got := anchoredAdapterMemberName(pair, map[string]bool{}); got != "io_PipeWriter"+PointerPrefix+"Writer" {
		t.Fatalf("bare-spelling anchored member = %q, want io_PipeWriter%sWriter", got, PointerPrefix)
	}
}

// A bare cast spelling is ambiguous the way a bare C# name is: io's tests declare their own
// `Buffer` under `using static io_package` while the record set also carries
// `bytes_package.Buffer`. C# binds the bare name to the variant's own nested type before any
// import, and the generator names that record's adapter BARE (its struct's package class IS the
// anchor class it generates into) — so the resolver must prefer the anchor-local record over the
// first same-simple-name foreign one, and compose the bare member name from it. Taking record
// order instead handed every `*Buffer` cast to `bytes_BufferжReader` (CS1503 ×20 — the io
// build wall's reappearance, 2026-07-31).
func TestBareCastPrefersAnchorLocalRecordOverForeignSimpleNameMatch(t *testing.T) {
	savedAnchors := emittedAdapterPairAnchors
	defer func() { emittedAdapterPairAnchors = savedAnchors }()

	pairs := [][2]string{
		{"bytes_package.Buffer", "io_package.Reader"},      // foreign record, FIRST in record order
		{"go.io_test_package.Buffer", "io_package.Reader"}, // the variant's own type, anchor-qualified
	}
	emittedAdapterPairAnchors = map[string]string{
		adapterGroupKey("bytes_package.Buffer", "io_package.Reader"):      "io_test_package",
		adapterGroupKey("go.io_test_package.Buffer", "io_package.Reader"): "io_test_package",
	}

	pair, ok := emittedAdapterPair(pairs, "Buffer", "io_package.Reader")
	if !ok {
		t.Fatal("a bare cast of the variant's own type must match its anchor-local record")
	}
	if pair[0] != "go.io_test_package.Buffer" {
		t.Fatalf("bare cast resolved to %q, want the anchor-local go.io_test_package.Buffer record", pair[0])
	}
	// The generator's AdapterStructKey sees container == packageClassName for this record and
	// composes the bare name — the converter's member composition must agree.
	if got := anchoredAdapterMemberName(pair, map[string]bool{}); got != "Buffer"+PointerPrefix+"Reader" {
		t.Fatalf("anchored member = %q, want the generator's bare Buffer%sReader", got, PointerPrefix)
	}
	// A QUALIFIED cast of the foreign same-simple-name record still resolves to it.
	pair, ok = emittedAdapterPair(pairs, "bytes_Buffer", "io_package.Reader")
	if !ok || pair[0] != "bytes_package.Buffer" {
		t.Fatalf("qualified foreign cast resolved to %v, want the bytes_package.Buffer record", pair)
	}
	// With NO anchor recorded for the local pair (a production-only resolve), the fallback still
	// returns the first simple-name match — the pre-existing PipeWriter behavior is unchanged.
	emittedAdapterPairAnchors = nil
	pair, ok = emittedAdapterPair(pairs, "Buffer", "io_package.Reader")
	if !ok || pair[0] != "bytes_package.Buffer" {
		t.Fatalf("unanchored bare cast resolved to %v, want first-match bytes_package.Buffer", pair)
	}
}

// The white-box record split of the BRIDGE variant's own records: a BARE record name declared by an
// internal _test.go file anchors to the bridge (its generated partial must merge with the
// bridge-declared type); production-qualified and undeclared bare names anchor to the test class.
func TestSplitWhiteboxVariantRecordsPartitionsByBridgeDeclaredNames(t *testing.T) {
	resetPackageState(&packages.Package{})
	packageNamespace = "go"

	bridgeNames := NewHashSet([]string{"errReader"})

	interfaceImplementations["io_package.Reader"] = NewHashSet([]string{"errReader", "externalHelper", PointerPrefix + "<scanner_package.Scanner>"})

	bridgeAnchored, testAnchored := splitWhiteboxVariantRecords(bridgeNames, true)

	if !bridgeAnchored.interfaceImplements["io_package.Reader"].Contains("errReader") {
		t.Fatal("a bridge-declared implementer must anchor to the bridge unit")
	}
	if bridgeAnchored.interfaceImplements["io_package.Reader"].Contains("externalHelper") {
		t.Fatal("an external-declared bare implementer must stay with the test anchor")
	}
	if !testAnchored.interfaceImplements["io_package.Reader"].Contains("externalHelper") ||
		!testAnchored.interfaceImplements["io_package.Reader"].Contains(PointerPrefix+"<scanner_package.Scanner>") {
		t.Fatal("non-bridge records must anchor to the test class")
	}

	resetPackageState(&packages.Package{})
}

// A BARE record name resolves in the scope of the variant that RECORDED it, so the bridge's
// declared-name set may only be consulted while splitting the BRIDGE's own records. The two
// `-tests` variants are separate Go packages and may declare the same simple type name:
// encoding/gob declares `Point` in codec_test.go (`package gob`) AND in example_interface_test.go
// (`package gob_test`, the one implementing `Pythagoras`). Matching the EXTERNAL suite's record
// against the bridge's set anchored `Point → Pythagoras` in package_info_internal_test.cs, where
// `Pythagoras` — external-only — is not in scope: CS0246, no test host, and all 106 gob verdicts
// read empty. The fixture reproduces the collision through the real go/types scan, so the guard
// fails if either the split's variant gate or the collision itself regresses.
func TestSplitWhiteboxVariantRecordsResolvesBareNamesInTheRecordingVariant(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod":   "module example/variantcollision\n\ngo 1.23\n",
		"value.go": "package variantcollision\n\nfunc Sum(x, y int) int { return x + y }\n",
		"value_test.go": "package variantcollision\n\n" +
			"type Squarer interface{ Square() int }\n\n" +
			"type Point struct{ X, Y int }\n\n" +
			"func (p Point) Square() int { return p.X * p.Y }\n\n" +
			"var _ Squarer = Point{}\n",
		"value_x_test.go": "package variantcollision_test\n\n" +
			"type Pythagoras interface{ Hypotenuse() int }\n\n" +
			"type Point struct{ X, Y int }\n\n" +
			"func (p Point) Hypotenuse() int { return p.X + p.Y }\n\n" +
			"var _ Pythagoras = Point{}\n",
	})

	internal, external := loadTestVariantsForDir(t, dir)

	if internal == nil || external == nil {
		t.Fatal("fixture must load BOTH test variants")
	}

	bridgeNames := collectWhiteboxBridgeTypeNames(internal)

	if !bridgeNames.Contains("Point") {
		t.Fatalf("fixture is inert: the bridge must declare Point, got %v", bridgeNames.Keys())
	}
	if !ambiguousVariantTypeNames(internal, external).Contains("Point") {
		t.Fatal("fixture is inert: both variants must declare Point for the collision to exist")
	}

	// The EXTERNAL suite's pair. Both spellings are bare — `Pythagoras` is declared only there, so
	// the bridge anchor cannot bind it.
	resetPackageState(&packages.Package{})
	packageNamespace = "go"
	interfaceImplementations["Pythagoras"] = NewHashSet([]string{"Point"})

	bridgeAnchored, testAnchored := splitWhiteboxVariantRecords(bridgeNames, false)

	if bridgeAnchored.interfaceImplements["Pythagoras"].Contains("Point") {
		t.Fatal("the external suite's Point must not anchor at the bridge: its interface is out of scope there (CS0246)")
	}
	if !testAnchored.interfaceImplements["Pythagoras"].Contains("Point") {
		t.Fatal("a bare name recorded by the external suite is external-declared and anchors at the test class")
	}

	// The BRIDGE variant's same-spelled pair still anchors at the bridge, where its partial must
	// merge with the internal declaration.
	resetPackageState(&packages.Package{})
	packageNamespace = "go"
	interfaceImplementations["Squarer"] = NewHashSet([]string{"Point"})

	bridgeAnchored, testAnchored = splitWhiteboxVariantRecords(bridgeNames, true)

	if !bridgeAnchored.interfaceImplements["Squarer"].Contains("Point") {
		t.Fatal("the bridge's own Point must anchor at the bridge")
	}
	if testAnchored.interfaceImplements["Squarer"].Contains("Point") {
		t.Fatal("a bridge-declared implementer must not also reach the test anchor")
	}

	resetPackageState(&packages.Package{})
}

// The bridge anchor seed is the bridge's single `static` declaration and single [GoPackage]
// carrier; its first — and only — class is the bridge, so go2cs-gen hosts bridge-anchored
// generated code inside the class the real declarations merge into.
func TestInternalTestPackageInfoSeedAnchorsBridgeClass(t *testing.T) {
	seed := internalTestPackageInfoSeed("go", "value_package", "value_internal_test_package", "value")

	if !strings.Contains(seed, "[GoPackage(\"value\")]\r\npublic static partial class value_internal_test_package\r\n{\r\n}\r\n") {
		t.Fatalf("seed must declare the attributed static bridge class:\n%s", seed)
	}
	if strings.Contains(seed, "class value_package") {
		t.Fatalf("seed must not declare the production class:\n%s", seed)
	}
	if firstClass := strings.Index(seed, "class "); firstClass < 0 || !strings.Contains(seed[firstClass:], "value_internal_test_package") {
		t.Fatalf("the bridge must be the seed's first class:\n%s", seed)
	}
}

// …and that unit is written whether or not the variant contributed any bridge-anchored records,
// because the seed is the bridge class's ONLY `public static partial` declaration: every converted
// SOURCE file opens its package class bare (`partial class X {`) by design, exactly as the
// production and external-test classes do, with the modifier living in the metadata file. Gating
// the unit on having records left a record-less bridge with no static declaration anywhere, and an
// internal test file declaring a method on a production type then emits an EXTENSION method into a
// non-static class — CS1106, with `internal/syscall/windows/registry`'s whole 6-verdict suite
// behind `func (k Key) SetValue(…)` in its export_test.go. Mixed suites that appear to escape it do
// so incidentally: sort/bytes/strings each have a go2cs-gen RecvGenerator file that re-declares the
// class `public static partial`, i.e. a GENERATOR supplying a modifier the emitter owes.
func TestWhiteboxBridgeUnitIsWrittenWithoutBridgeRecords(t *testing.T) {
	dir := t.TempDir()

	resetPackageState(&packages.Package{})
	packageNamespace = "go"

	testInfoPath := filepath.Join(dir, "package_test_info.cs")
	testSeed := referenceModelTestPackageInfoSeed("go", "value_test_package", "value_test", "value_package")

	if err := os.WriteFile(testInfoPath, []byte(testSeed), 0644); err != nil {
		t.Fatal(err)
	}

	// Nothing recorded at all — the record-less bridge registry reproduces.
	unitName, err := writeWhiteboxVariantMetadata(testInfoPath, dir, "value_package", "value_internal_test_package",
		"value", "go.value_internal_test_package", "go.value_test_package", HashSet[string]{}, true)
	if err != nil {
		t.Fatalf("writeWhiteboxVariantMetadata: %v", err)
	}

	if unitName != internalTestPackageInfoFileName {
		t.Fatalf("the bridge unit must be listed as an output file so the .tests.csproj compiles it, got %q", unitName)
	}

	unit, err := os.ReadFile(filepath.Join(dir, internalTestPackageInfoFileName))
	if err != nil {
		t.Fatalf("the bridge unit must be written with no bridge-anchored records: %v", err)
	}

	if !strings.Contains(string(unit), "public static partial class value_internal_test_package") {
		t.Fatalf("the bridge unit must declare the bridge class public static partial:\n%s", unit)
	}

	resetPackageState(&packages.Package{})
}

// A production ALIAS whose right-hand side is an anonymous struct has no C# spelling of its own:
// the production conversion LIFTS it to a real nested type and reaches it through a
// compilation-scoped `global using`. A reference-model test project is a SECOND compilation and
// does not recompile the production sources, so neither the name nor the lift crossed — every
// test-side reference fell through to `t.String()` and emitted raw GO syntax into a C# file
// (internal/fuzz's `Func<struct{Parent string; …}, error>`, CS1031/CS1525/CS1003, with all 52 of
// that package's verdicts behind it). Both halves are seeded from the production package_info the
// test conversion already reads: the TYPE, so every renderer spells the alias, and the alias
// itself, so the name resolves in the test compilation.
func TestSeedProductionAliasLiftsCarriesLiftAndAliasTogether(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/aliaslift\n\ngo 1.23\n",
		// CorpusEntry's shape: an EXPORTED alias to an anonymous struct. Unexported and
		// named-RHS aliases sit beside it as the negative controls.
		"value.go": "package aliaslift\n\n" +
			"type CorpusEntry = struct{ Parent string; Data []byte }\n\n" +
			"type hidden = struct{ N int }\n\n" +
			"type Named = Concrete\n\n" +
			"type Concrete struct{ M int }\n\n" +
			"func Use(e CorpusEntry) int { return len(e.Data) }\n",
		"value_test.go": "package aliaslift\n\n" +
			"func helper(fn func(CorpusEntry) error) error { return fn(CorpusEntry{}) }\n",
	})

	infoPath := filepath.Join(dir, "package_info.cs")
	info := "// <ExportedTypeAliases>\r\n" +
		"[assembly: GoTypeAlias(\"CorpusEntry\", \"go.example.aliaslift_package.CorpusEntryᴛ1\")]\r\n" +
		"[assembly: GoTypeAlias(\"Named\", \"go.example.aliaslift_package.Concrete\")]\r\n" +
		"// </ExportedTypeAliases>\r\n"

	if err := os.WriteFile(infoPath, []byte(info), 0644); err != nil {
		t.Fatal(err)
	}

	internal, _ := loadTestVariantsForDir(t, dir)

	if internal == nil {
		t.Fatal("fixture must load the internal test variant")
	}

	resetPackageState(&packages.Package{})
	packageNamespace = "go"

	seedProductionAliasLifts(internal, infoPath)

	if got := importedTypeAliases["CorpusEntry"]; got != "go.example.aliaslift_package.CorpusEntryᴛ1" {
		t.Fatalf("the alias must be re-emitted into the test compilation, got %q", got)
	}

	// The lift map must name the same alias for the anonymous struct itself, or the renderer
	// still writes Go syntax while the `global using` sits unused.
	obj := internal.Types.Scope().Lookup("CorpusEntry")

	if obj == nil {
		t.Fatal("fixture is inert: CorpusEntry must be in package scope")
	}

	if got := productionAliasLiftedTypes[types.Unalias(obj.Type())]; got != "CorpusEntry" {
		t.Fatalf("the anonymous struct must resolve to its alias name, got %q", got)
	}

	// A NAMED right-hand side already renders through its own qualified name, so seeding an
	// alias for it would put an avoidable global-using name into the test compilation.
	if _, seeded := importedTypeAliases["Named"]; seeded {
		t.Fatal("an alias over a NAMED type must not be seeded — it has its own spelling")
	}

	// An UNEXPORTED alias publishes nothing, so seeding its type would render a name the test
	// compilation cannot resolve.
	if hidden := internal.Types.Scope().Lookup("hidden"); hidden != nil {
		if _, seeded := productionAliasLiftedTypes[types.Unalias(hidden.Type())]; seeded {
			t.Fatal("an unpublished alias must not be seeded — its name would not resolve")
		}
	}

	resetPackageState(&packages.Package{})
}

// Change C project shape: a REFERENCE-model test project binds the production package through a
// colocated ProjectReference and carries NO production compile items; the recompile model keeps
// the original recompiled shape and no production reference.
func TestWriteTestProjectReferenceModelBindsProductionProject(t *testing.T) {
	dir := t.TempDir()
	importPackageDirs = map[string]importedPackageMeta{}
	productionFiles := []string{"value.cs"}
	testFiles := []string{"value_test.cs"}

	projectFile := filepath.Join(dir, "value.tests.csproj")
	if err := writeTestProject(projectFile, "value", "go", testProjectReference, productionFiles, testFiles, nil, nil, Options{go2csPath: dir}); err != nil {
		t.Fatal(err)
	}

	data, err := os.ReadFile(projectFile)
	if err != nil {
		t.Fatal(err)
	}

	contents := string(data)
	if !strings.Contains(contents, `<ProjectReference Include="value.csproj" />`) {
		t.Fatalf("reference model must reference the colocated production csproj:\n%s", contents)
	}
	if strings.Contains(contents, `<Compile Include="value.cs" />`) {
		t.Fatalf("reference model must not recompile production sources:\n%s", contents)
	}
	if !strings.Contains(contents, `<Compile Include="value_test.cs" />`) {
		t.Fatalf("reference model must keep the converted test sources as compile items:\n%s", contents)
	}

	recompileFile := filepath.Join(dir, "value.recompile.tests.csproj")
	if err := writeTestProject(recompileFile, "value", "go", testProjectRecompile, productionFiles, testFiles, nil, nil, Options{go2csPath: dir}); err != nil {
		t.Fatal(err)
	}

	data, err = os.ReadFile(recompileFile)
	if err != nil {
		t.Fatal(err)
	}

	contents = string(data)
	if strings.Contains(contents, `<ProjectReference Include="value.csproj" />`) {
		t.Fatalf("recompile model must not reference the production csproj:\n%s", contents)
	}
	if !strings.Contains(contents, `<Compile Include="value.cs" />`) {
		t.Fatalf("recompile model must recompile production sources:\n%s", contents)
	}
}

// Change C anchor seed: the reference-model package_test_info.cs declares the external test
// package class as its FIRST (and only) class — the go2cs-gen anchor — carrying [GoPackage]
// directly, and must not declare the production package class (the referenced production
// assembly is the single identity for those types).
func TestReferenceModelSeedAnchorsTestClassOnly(t *testing.T) {
	seed := referenceModelTestPackageInfoSeed("go", "value_test_package", "value_test", "value_package")

	if !strings.Contains(seed, "[GoPackage(\"value_test\")]\r\npublic static partial class value_test_package\r\n{\r\n}\r\n") {
		t.Fatalf("seed must declare the attributed external test package class:\n%s", seed)
	}
	if strings.Contains(seed, "class value_package") {
		t.Fatalf("seed must not declare the production package class:\n%s", seed)
	}
	for _, marker := range []string{"<ImportedTypeAliases>", "<ExportedTypeAliases>", "<InterfaceImplementations>", "<ImplicitConversions>"} {
		if !strings.Contains(seed, marker) {
			t.Fatalf("seed is missing the %s writer marker section:\n%s", marker, seed)
		}
	}
}

// Reference closure: the test project's reference set must be CLOSED under the C# DECLARATION
// edges the converter emits — an interface's base interfaces and a struct's field types. Binding
// such a type in C# requires those assemblies, and the edge belongs to the DECLARING package's
// import graph, so it appears in no test import and no alias `using` and only this closure can
// surface it (CS0012 otherwise). It must surface those packages and ONLY those, never a package's
// whole import graph, which would contribute child namespaces that break the CS0576 alias
// machinery — every positive case below is paired with the negative that pins the boundary.
func TestDeclarationClosureImportsSurfacesForeignDeclarationEdges(t *testing.T) {
	// FOREIGN-package shape (hash/maphash, crypto/hmac): the emitted
	// `GoImplement<Hash, hash_package.Hash64>` record binds `hash`, which IS referenced — but
	// hash's `Hash` embeds io.Writer, and neither package's import closure reaches io.
	foreignDir := t.TempDir()
	writeModuleFiles(t, foreignDir, map[string]string{
		"go.mod": "module example/foreign\n\ngo 1.23\n",
		"foreign.go": "package foreign\n" +
			"import \"hash\"\n" +
			"func Size(h hash.Hash64) int { return h.Size() }\n",
	})

	foreign := loadProductionForDir(t, foreignDir)
	if got := declarationClosureImports([]*packages.Package{foreign}, nil, []string{"hash"}, nil, nil); len(got) != 1 || got[0] != "io" {
		t.Fatalf("hash.Hash embeds io.Writer, so referencing hash must surface io; got %v", got)
	}

	// io/fs shape, seeded through the package under test: File STRUCTURALLY satisfies
	// io.ReadCloser (Read + Close) even though Go's fs.File lists the methods explicitly rather
	// than embedding io — the converter emits `File : io.ReadCloser`, and binding File from the
	// REFERENCED production assembly then needs the io assembly.
	fsysDir := t.TempDir()
	writeModuleFiles(t, fsysDir, map[string]string{
		"go.mod": "module example/closure\n\ngo 1.23\n",
		"fs.go": "package closure\n" +
			"import \"io\"\n" +
			"type FileInfo interface{ Name() string }\n" +
			"type File interface {\n" +
			"\tStat() (FileInfo, error)\n" +
			"\tRead([]byte) (int, error)\n" +
			"\tClose() error\n" +
			"}\n" +
			"func ReadAll(f File) ([]byte, error) { return io.ReadAll(f) }\n",
	})

	fsys := loadProductionForDir(t, fsysDir)
	if got := declarationClosureImports([]*packages.Package{fsys}, nil, []string{fsys.PkgPath}, nil, nil); len(got) != 1 || got[0] != "io" {
		t.Fatalf("File structurally implements io.ReadCloser, so the io assembly must be surfaced; got %v", got)
	}

	// TRANSITIVE: a referenced package's base is itself declared in a package with a base of its
	// own (b.B : a.A : io.Writer). A single-step scan surfaces only `a` and the compile still
	// fails CS0012 on io_package.Writer, so the walk must follow every added package in turn.
	chainDir := t.TempDir()
	writeModuleFiles(t, chainDir, map[string]string{
		"go.mod":  "module example/chain\n\ngo 1.23\n",
		"a/a.go":  "package a\nimport \"io\"\ntype A interface {\n\tio.Writer\n\tExtra() int\n}\n",
		"b/b.go":  "package b\nimport \"example/chain/a\"\ntype B interface {\n\ta.A\n\tMore() int\n}\n",
		"main.go": "package chain\nimport \"example/chain/b\"\nfunc Use(x b.B) int { return x.More() }\n",
	})

	chain := loadProductionForDir(t, chainDir)
	got := declarationClosureImports([]*packages.Package{chain}, nil, []string{"example/chain/b"}, nil, nil)

	if len(got) != 2 || got[0] != "example/chain/a" || got[1] != "io" {
		t.Fatalf("the closure must follow b -> a -> io, got %v", got)
	}

	// NARROWING (with its own positive control): the closure follows interfaces the sources
	// actually NAME, never every exported interface of every referenced package. `fmt.State`
	// structurally implements io.Writer, so a package-level walk would hand io to nearly the whole
	// corpus — but a project that merely calls `fmt.Sprintf` never binds `fmt.State` and needs no
	// io reference.
	unnamedDir := t.TempDir()
	writeModuleFiles(t, unnamedDir, map[string]string{
		"go.mod": "module example/unnamed\n\ngo 1.23\n",
		"unnamed.go": "package unnamed\n" +
			"import \"fmt\"\n" +
			"func Greet(name string) string { return fmt.Sprintf(\"hi %s\", name) }\n",
	})

	unnamed := loadProductionForDir(t, unnamedDir)
	if got := declarationClosureImports([]*packages.Package{unnamed}, nil, []string{"fmt", unnamed.PkgPath}, nil, nil); len(got) != 0 {
		t.Fatalf("fmt is referenced but fmt.State is never named, so nothing must be surfaced; got %v", got)
	}

	// The control: naming fmt.State DOES bind an interface whose base is io.Writer.
	namedDir := t.TempDir()
	writeModuleFiles(t, namedDir, map[string]string{
		"go.mod": "module example/named\n\ngo 1.23\n",
		"named.go": "package named\n" +
			"import \"fmt\"\n" +
			"func Width(s fmt.State) int { w, _ := s.Width(); return w }\n",
	})

	namedPkg := loadProductionForDir(t, namedDir)
	if got := declarationClosureImports([]*packages.Package{namedPkg}, nil, []string{"fmt", namedPkg.PkgPath}, nil, nil); len(got) != 1 || got[0] != "io" {
		t.Fatalf("naming fmt.State binds an interface whose base is io.Writer; got %v", got)
	}

	// Negative: an exported interface matching no imported interface, alongside an imported
	// package used only inside a function body, surfaces NOTHING — the closure must stay minimal
	// and never widen to the seeded packages' plain import graphs.
	plainDir := t.TempDir()
	writeModuleFiles(t, plainDir, map[string]string{
		"go.mod": "module example/plain\n\ngo 1.23\n",
		"plain.go": "package plain\n" +
			"import \"strings\"\n" +
			"type Greeter interface{ Greet() string }\n" +
			"func Upper(s string) string { return strings.ToUpper(s) }\n",
	})

	plain := loadProductionForDir(t, plainDir)
	if got := declarationClosureImports([]*packages.Package{plain}, nil, []string{plain.PkgPath}, nil, nil); len(got) != 0 {
		t.Fatalf("no exported interface matches an imported interface, so the closure must be empty; got %v", got)
	}

	// STRUCT FIELDS AT A COMPOSITE LITERAL (image/draw): the compilation CONSTRUCTS testing/quick's
	// `Config`, and the converter renders that as `new Config(…)` — go2cs-gen's fieldwise
	// constructor, whose parameter list names every field type, including `Rand *rand.Rand`. Rand is
	// a STRUCT, so no interface closure can ever reach it, and math/rand is in no import list.
	// Paired below with the negative that pins the boundary at CONSTRUCTION, not at value use.
	fieldDir := t.TempDir()
	fieldFiles := map[string]string{
		"go.mod": "module example/field\n\ngo 1.23\n",
		"cfg/cfg.go": "package cfg\n" +
			"import \"math/rand\"\n" +
			"type Config struct {\n" +
			"\tMaxCount int\n" +
			"\tRand     *rand.Rand\n" +
			"\tValues   func(rand *rand.Rand)\n" +
			"}\n" +
			"func Default() Config { return Config{} }\n",
		// CONSTRUCTS the foreign struct.
		"build/main.go": "package build\nimport \"example/field/cfg\"\nfunc Use() int { return cfg.Config{MaxCount: 3}.MaxCount }\n",
		// Only USES it by value — binds one from a constructor function and reads a field, never
		// writing a literal. This is the shape eleven banked packages take with
		// sync.Once/reflect.Value/sync.Map, and they compile clean today with no reference to
		// those field types' assemblies.
		"use/main.go": "package use\nimport \"example/field/cfg\"\nfunc Use() int {\n\tc := cfg.Default()\n\treturn c.MaxCount\n}\n",
		// Constructs it with the EMPTY (zero-value) literal, which converts to go2cs-gen's nil
		// constructor — `new Config(nil)` — naming no field. The FIELDWISE overload is `internal`
		// (Config's package is not this assembly nor its friend), so it is not even a resolution
		// candidate here. mime's `once = sync.Once{}` and testing/quick's
		// `return reflect.Value{}, false` are the corpus instances.
		"zero/main.go": "package zero\nimport \"example/field/cfg\"\nfunc Use() cfg.Config { return cfg.Config{} }\n",
		// The ROOT-package counterpart: the struct is declared in the package under conversion, so
		// its `internal` fieldwise constructor IS a candidate (same assembly under recompile, a
		// friend assembly under white-box) and resolving `new Holder(nil)` binds every parameter
		// type. math/rand/v2's `*p = ChaCha8{}` is the corpus instance.
		"root/root.go": "package root\n" +
			"import \"math/rand\"\n" +
			"type Holder struct{ R *rand.Rand }\n" +
			"func Zero() Holder { return Holder{} }\n",
	}
	writeModuleFiles(t, fieldDir, fieldFiles)

	built := loadProductionForDir(t, filepath.Join(fieldDir, "build"))
	if got := declarationClosureImports([]*packages.Package{built}, nil, []string{"example/field/cfg"}, nil, nil); len(got) != 1 || got[0] != "math/rand" {
		t.Fatalf("constructing Config must surface its *rand.Rand field's math/rand; got %v", got)
	}

	used := loadProductionForDir(t, filepath.Join(fieldDir, "use"))
	if got := declarationClosureImports([]*packages.Package{used}, nil, []string{"example/field/cfg"}, nil, nil); len(got) != 0 {
		t.Fatalf("holding a Config VALUE demands no field assembly — only the constructor does; got %v", got)
	}

	zero := loadProductionForDir(t, filepath.Join(fieldDir, "zero"))
	if got := declarationClosureImports([]*packages.Package{zero}, nil, []string{"example/field/cfg"}, nil, nil); len(got) != 0 {
		t.Fatalf("a FOREIGN struct's EMPTY literal resolves against no accessible fieldwise constructor; got %v", got)
	}

	rootZero := loadProductionForDir(t, filepath.Join(fieldDir, "root"))
	if got := declarationClosureImports([]*packages.Package{rootZero}, nil, nil, nil, nil); len(got) != 1 || got[0] != "math/rand" {
		t.Fatalf("a ROOT struct's EMPTY literal still resolves against its accessible fieldwise constructor; got %v", got)
	}

	// A field's FUNC signature names types just as a plain field does: the fieldwise constructor's
	// parameter for it is a delegate spelling every parameter and result type out. Pinned by
	// dropping the pointer field, so math/rand can only come through `Values`.
	funcFieldDir := t.TempDir()
	writeModuleFiles(t, funcFieldDir, map[string]string{
		"go.mod": "module example/funcfield\n\ngo 1.23\n",
		"cfg/cfg.go": "package cfg\n" +
			"import \"math/rand\"\n" +
			"type Config struct{ Values func(rand *rand.Rand) }\n",
		"main.go": "package funcfield\nimport \"example/funcfield/cfg\"\nfunc Use() cfg.Config { return cfg.Config{Values: nil} }\n",
	})

	funcField := loadProductionForDir(t, funcFieldDir)
	if got := declarationClosureImports([]*packages.Package{funcField}, nil, []string{"example/funcfield/cfg"}, nil, nil); len(got) != 1 || got[0] != "math/rand" {
		t.Fatalf("a func-typed field's signature must surface math/rand; got %v", got)
	}

	// ONE LEVEL, and no layout recursion. Constructing mid.Outer needs deep.Deep's ASSEMBLY (the
	// constructor names the parameter type) but NOT deep.Deep's own field types: the parameter
	// defaults rather than being constructed, and a nested literal would be its own seed. Without
	// this boundary `os.File`'s single `*file` field would drag internal/poll, syscall and the rest
	// of os's private graph into every project that constructs one.
	layoutDir := t.TempDir()
	writeModuleFiles(t, layoutDir, map[string]string{
		"go.mod":       "module example/layout\n\ngo 1.23\n",
		"deep/deep.go": "package deep\nimport \"math/rand\"\ntype Deep struct{ R *rand.Rand }\n",
		"mid/mid.go":   "package mid\nimport \"example/layout/deep\"\ntype Outer struct {\n\tN int\n\tD deep.Deep\n}\n",
		"main.go":      "package layout\nimport \"example/layout/mid\"\nfunc Use() mid.Outer { return mid.Outer{N: 1} }\n",
	})

	layout := loadProductionForDir(t, layoutDir)
	got = declarationClosureImports([]*packages.Package{layout}, nil, []string{"example/layout/mid"}, nil, nil)

	if len(got) != 1 || got[0] != "example/layout/deep" {
		t.Fatalf("the field edge is one level — deep's assembly, not deep's own field types; got %v", got)
	}

	// COMPILE-EXCLUDED FILES SEED NOTHING (compress/gzip): a Phase-4D Example-only file is analyzed
	// but never emitted, so the types it names are not in the compilation. Seeding from one handed
	// gzip five references reached through `http.Request`'s fields. The positive control is the same
	// file with the exclusion NOT applied.
	excludedDir := t.TempDir()
	writeModuleFiles(t, excludedDir, map[string]string{
		"go.mod": "module example/excluded\n\ngo 1.23\n",
		"cfg/cfg.go": "package cfg\n" +
			"import \"math/rand\"\n" +
			"type Config struct{ Rand *rand.Rand }\n",
		"real.go": "package excluded\nfunc Real() int { return 1 }\n",
		"example_test.go": "package excluded_test\n" +
			"import \"example/excluded/cfg\"\n" +
			"func ExampleWired() { _ = cfg.Config{Rand: nil} }\n",
	})

	excludedProduction := loadProductionForDir(t, excludedDir)
	_, excludedExternal := loadTestVariantsForDir(t, excludedDir)

	if excludedExternal == nil {
		t.Fatal("the external Example-only variant was not loaded")
	}

	roots := []*packages.Package{excludedProduction, excludedExternal}
	control := declarationClosureImports(roots, nil, []string{"example/excluded/cfg"}, nil, nil)

	if len(control) != 1 || control[0] != "math/rand" {
		t.Fatalf("control: with the file compiled, its literal must surface math/rand; got %v", control)
	}

	excluded := selectCompileExcludedTestFiles(nil, excludedExternal)
	if len(excluded) != 1 {
		t.Fatalf("the Example-only file must be compile-excluded; got %v", excluded)
	}

	if got := declarationClosureImports(roots, excluded, []string{"example/excluded/cfg"}, nil, nil); len(got) != 0 {
		t.Fatalf("a compile-excluded file names nothing in the compilation; got %v", got)
	}

	// A ROOT's own package is never an addition: its types compile into the test assembly (or bind
	// through the production project reference the template already carries). For the EXTERNAL
	// variant this is load-bearing rather than theoretical — go/packages names it `<pkg>_test`,
	// which resolves to no importable package, so an external-test struct literal whose field type
	// is declared beside it would fail the whole conversion with "package bytes_test is not in std".
	selfDir := t.TempDir()
	writeModuleFiles(t, selfDir, map[string]string{
		"go.mod":  "module example/self\n\ngo 1.23\n",
		"self.go": "package self\nfunc Value() int { return 1 }\n",
		"self_test.go": "package self_test\n" +
			"import (\n\t\"testing\"\n\n\t\"example/self\"\n)\n" +
			"type inner struct{ v int }\n" +
			"type harness struct{ i inner }\n" +
			"func TestValue(t *testing.T) {\n" +
			"\th := harness{i: inner{v: self.Value()}}\n" +
			"\tif h.i.v != 1 {\n\t\tt.Fatal(\"bad\")\n\t}\n}\n",
	})

	selfProduction := loadProductionForDir(t, selfDir)
	_, selfExternal := loadTestVariantsForDir(t, selfDir)

	if selfExternal == nil {
		t.Fatal("the external test variant was not loaded")
	}

	if got := declarationClosureImports([]*packages.Package{selfProduction, selfExternal}, nil, []string{selfProduction.PkgPath}, nil, nil); len(got) != 0 {
		t.Fatalf("a root's own package is never a project reference; got %v", got)
	}

	// `testing` is never a walk SOURCE: it binds to the hand-owned core/testing shim, whose C#
	// declarations share only names with Go's. Go's testing.T embeds a `common` holding io.Writer,
	// time.Time, sync.RWMutex and more — walking it would hand those to EVERY test project, none of
	// which the shim's two-field T names. Every -tests compilation names testing.T, so this is the
	// widest over-inclusion the struct rule could possibly cause.
	testingDir := t.TempDir()
	writeModuleFiles(t, testingDir, map[string]string{
		"go.mod": "module example/testingsource\n\ngo 1.23\n",
		"src.go": "package testingsource\n" +
			"import \"testing\"\n" +
			"func Helper(t *testing.T) { t.Helper(); _ = testing.T{} }\n",
	})

	testingSource := loadProductionForDir(t, testingDir)
	if got := declarationClosureImports([]*packages.Package{testingSource}, nil, []string{"testing", testingSource.PkgPath}, nil, nil); len(got) != 0 {
		t.Fatalf("testing binds to the hand-owned shim and must never be walked; got %v", got)
	}
}

// The MEMBER-ACCESS edge (unique): resolving `x.M` requires binding x's TYPE, and when x is declared
// in another package that type is spelled nowhere in this compilation — not in an import, not in an
// alias `using`. unique's white-box suite calls `cleanupMu.Lock()` on the production package's
// `var cleanupMu sync.Mutex`; the reference model does not inherit the production assembly's own
// references, so the test compile died `CS0012 … 'sync_package.Mutex'` twice with no host ever linking.
// Every positive below is paired with the negative that pins the boundary — the two the banked roster
// measured are the receiver restriction (naming a declaration is not accessing a member of it) and the
// test-file scoping (production sources are not in this compilation under the reference model).
func TestDeclarationClosureImportsSurfacesMemberAccessEdges(t *testing.T) {
	// The unique shape: a member access on a foreign package-level VAR whose type is declared in a
	// third package the test never imports.
	varDir := t.TempDir()
	writeModuleFiles(t, varDir, map[string]string{
		"go.mod":       "module example/valdecl\n\ngo 1.23\n",
		"lib/lib.go":   "package lib\nimport \"sync\"\nvar Mu sync.Mutex\n",
		"main.go":      "package valdecl\nfunc Use() int { return 1 }\n",
		"main_test.go": "package valdecl\nimport (\n\t\"testing\"\n\n\t\"example/valdecl/lib\"\n)\nfunc TestLock(t *testing.T) { lib.Mu.Lock(); lib.Mu.Unlock() }\n",
	})

	varProduction := loadProductionForDir(t, varDir)
	varInternal, varExternal := loadTestVariantsForDir(t, varDir)

	got := declarationClosureImports([]*packages.Package{varProduction, varInternal, varExternal}, nil,
		[]string{varProduction.PkgPath, "example/valdecl/lib", "testing"}, nil, nil)

	if len(got) != 1 || got[0] != "sync" {
		t.Fatalf("a member access on a foreign var of type sync.Mutex must surface sync; got %v", got)
	}

	// NEGATIVE (the receiver restriction). The same lib, the same var — but the test only NAMES it,
	// passing it along without accessing a member. Naming a declaration does not force its signature
	// to be materialized, and widening the edge to every named declaration drifts 23 of the 73 banked
	// projects.
	namedOnlyDir := t.TempDir()
	writeModuleFiles(t, namedOnlyDir, map[string]string{
		"go.mod":       "module example/valnamed\n\ngo 1.23\n",
		"lib/lib.go":   "package lib\nimport \"sync\"\nvar Mu sync.Mutex\nfunc Take(m *sync.Mutex) {}\n",
		"main.go":      "package valnamed\nfunc Use() int { return 1 }\n",
		"main_test.go": "package valnamed\nimport (\n\t\"testing\"\n\n\t\"example/valnamed/lib\"\n)\nfunc TestTake(t *testing.T) { lib.Take(&lib.Mu) }\n",
	})

	namedProduction := loadProductionForDir(t, namedOnlyDir)
	namedInternal, namedExternal := loadTestVariantsForDir(t, namedOnlyDir)

	if got := declarationClosureImports([]*packages.Package{namedProduction, namedInternal, namedExternal}, nil,
		[]string{namedProduction.PkgPath, "example/valnamed/lib", "testing"}, nil, nil); len(got) != 0 {
		t.Fatalf("naming a declaration is not accessing a member of it; got %v", got)
	}

	// NEGATIVE (the test-file scoping). The member access lives in the PRODUCTION source, which under
	// the reference model is compiled into the referenced assembly — not into this compilation — and
	// that assembly carries its own `sync` reference. Seeding from production too drifts 13 banked
	// projects (crc32's `castagnoliOnce.Do`, math's `cpu.X86`, …).
	prodOnlyDir := t.TempDir()
	writeModuleFiles(t, prodOnlyDir, map[string]string{
		"go.mod":       "module example/valprod\n\ngo 1.23\n",
		"lib/lib.go":   "package lib\nimport \"sync\"\nvar Mu sync.Mutex\n",
		"main.go":      "package valprod\nimport \"example/valprod/lib\"\nfunc Lock() { lib.Mu.Lock() }\n",
		"main_test.go": "package valprod\nimport \"testing\"\nfunc TestLock(t *testing.T) { Lock() }\n",
	})

	prodProduction := loadProductionForDir(t, prodOnlyDir)
	prodInternal, prodExternal := loadTestVariantsForDir(t, prodOnlyDir)

	if got := declarationClosureImports([]*packages.Package{prodProduction, prodInternal, prodExternal}, nil,
		[]string{prodProduction.PkgPath, "example/valprod/lib", "testing"}, nil, nil); len(got) != 0 {
		t.Fatalf("a member access in PRODUCTION source is not in the test compilation; got %v", got)
	}

	// NEGATIVE — a package-QUALIFIED selector is not a member access on a value: its base is a
	// PkgName, which has no type at all, and the import that spells it already carries the reference.
	// Pinned with a foreign-typed const so a base-type edge would have something to surface.
	qualifiedDir := t.TempDir()
	writeModuleFiles(t, qualifiedDir, map[string]string{
		"go.mod":       "module example/valqual\n\ngo 1.23\n",
		"kind/kind.go": "package kind\ntype Kind int\n",
		"lib/lib.go":   "package lib\nimport \"example/valqual/kind\"\nconst First kind.Kind = 1\n",
		"main.go":      "package valqual\nfunc Use() int { return 1 }\n",
		"main_test.go": "package valqual\nimport (\n\t\"testing\"\n\n\t\"example/valqual/lib\"\n)\nfunc TestConst(t *testing.T) { _ = int(lib.First) }\n",
	})

	qualProduction := loadProductionForDir(t, qualifiedDir)
	qualInternal, qualExternal := loadTestVariantsForDir(t, qualifiedDir)

	if got := declarationClosureImports([]*packages.Package{qualProduction, qualInternal, qualExternal}, nil,
		[]string{qualProduction.PkgPath, "example/valqual/lib", "testing"}, nil, nil); len(got) != 0 {
		t.Fatalf("a package-qualified selector has no receiver type to surface; got %v", got)
	}
}

// The ZERO-VALUE DECLARATION form of the constructor edge (log): `var l Logger` writes no composite
// literal anywhere, yet the converter renders Go's zero value as a CONSTRUCTOR CALL, so overload
// resolution must still materialize the accessible fieldwise constructor's parameter types. log's
// white-box TestNonNewLogger died `CS0012 … 'atomic_package.Pointer<>'` on it. Each positive is
// paired with the negative that keeps the edge exactly where the empty-literal form already sits:
// accessible constructors only (ROOT-declared), and `_test.go` sources only.
func TestDeclarationClosureImportsSurfacesZeroValueVarDeclarations(t *testing.T) {
	// The log shape: a root struct declared at its zero value in a white-box test, with a field
	// type from a package neither half imports.
	rootDir := t.TempDir()
	writeModuleFiles(t, rootDir, map[string]string{
		"go.mod":       "module example/zerovar\n\ngo 1.23\n",
		"main.go":      "package zerovar\nimport \"math/rand\"\ntype Holder struct{ r *rand.Rand }\nfunc Use() int { return 1 }\n",
		"main_test.go": "package zerovar\nimport \"testing\"\nfunc TestZero(t *testing.T) {\n\tvar h Holder\n\t_ = h\n}\n",
	})

	rootProduction := loadProductionForDir(t, rootDir)
	rootInternal, rootExternal := loadTestVariantsForDir(t, rootDir)

	got := declarationClosureImports([]*packages.Package{rootProduction, rootInternal, rootExternal}, nil,
		[]string{rootProduction.PkgPath, "testing"}, nil, nil)

	if len(got) != 1 || got[0] != "math/rand" {
		t.Fatalf("a ROOT struct's zero-value var declaration constructs it; got %v", got)
	}

	// NEGATIVE (accessibility, the same gate the empty literal carries). A FOREIGN struct's
	// fieldwise constructor is `internal` for any struct with an unexported field, so it is not a
	// resolution candidate here at all and the declaration demands nothing.
	foreignDir := t.TempDir()
	writeModuleFiles(t, foreignDir, map[string]string{
		"go.mod":     "module example/zeroforeign\n\ngo 1.23\n",
		"lib/lib.go": "package lib\nimport \"math/rand\"\ntype Holder struct{ r *rand.Rand }\n",
		"main.go":    "package zeroforeign\nfunc Use() int { return 1 }\n",
		"main_test.go": "package zeroforeign\nimport (\n\t\"testing\"\n\n\t\"example/zeroforeign/lib\"\n)\n" +
			"func TestZero(t *testing.T) {\n\tvar h lib.Holder\n\t_ = h\n}\n",
	})

	foreignProduction := loadProductionForDir(t, foreignDir)
	foreignInternal, foreignExternal := loadTestVariantsForDir(t, foreignDir)

	if got := declarationClosureImports([]*packages.Package{foreignProduction, foreignInternal, foreignExternal}, nil,
		[]string{foreignProduction.PkgPath, "example/zeroforeign/lib", "testing"}, nil, nil); len(got) != 0 {
		t.Fatalf("a FOREIGN struct's zero-value declaration resolves against no accessible constructor; got %v", got)
	}

	// NEGATIVE (the test-file scoping). The same declaration in PRODUCTION source is compiled into
	// the referenced assembly, not into this compilation, and that assembly carries its own
	// references.
	prodDir := t.TempDir()
	writeModuleFiles(t, prodDir, map[string]string{
		"go.mod":       "module example/zeroprod\n\ngo 1.23\n",
		"main.go":      "package zeroprod\nimport \"math/rand\"\ntype Holder struct{ r *rand.Rand }\nfunc Use() Holder {\n\tvar h Holder\n\treturn h\n}\n",
		"main_test.go": "package zeroprod\nimport \"testing\"\nfunc TestUse(t *testing.T) { _ = Use() }\n",
	})

	prodProduction := loadProductionForDir(t, prodDir)
	prodInternal, prodExternal := loadTestVariantsForDir(t, prodDir)

	if got := declarationClosureImports([]*packages.Package{prodProduction, prodInternal, prodExternal}, nil,
		[]string{prodProduction.PkgPath, "testing"}, nil, nil); len(got) != 0 {
		t.Fatalf("a zero-value declaration in PRODUCTION source is not in the test compilation; got %v", got)
	}
}

// The IMPLEMENTED-INTERFACE edge (go/scanner): a converted concrete type carries its interfaces in
// its package's `[assembly: GoImplement<T, I>]` records, which go2cs-gen realizes as a partial base
// list INSIDE THE DECLARING ASSEMBLY — so the metadata type declares them and binding any member on
// it resolves that list. `ErrorList`'s `sort.Interface` base failed `CS0012` ×13 across the suite and
// the generated `error` adapter's own `m_value.Equals(…)`.
func TestDeclarationClosureImportsSurfacesImplementedInterfaceBases(t *testing.T) {
	// The scanner shape: a named slice whose value method set satisfies sort.Interface, with sort in
	// the PRODUCTION package's imports and in neither test import list.
	implDir := t.TempDir()
	writeModuleFiles(t, implDir, map[string]string{
		"go.mod": "module example/implbase\n\ngo 1.23\n",
		"main.go": "package implbase\n" +
			"import \"sort\"\n" +
			"type Rows []int\n" +
			"func (r Rows) Len() int           { return len(r) }\n" +
			"func (r Rows) Less(i, j int) bool { return r[i] < r[j] }\n" +
			"func (r Rows) Swap(i, j int)      { r[i], r[j] = r[j], r[i] }\n" +
			"func (r Rows) Total() int         { return len(r) }\n" +
			"func Order(r Rows)                { sort.Sort(r) }\n",
		"main_test.go": "package implbase\nimport \"testing\"\nfunc TestTotal(t *testing.T) {\n\tvar r Rows\n\tif r.Total() != 0 {\n\t\tt.Fatal(\"bad\")\n\t}\n}\n",
	})

	implProduction := loadProductionForDir(t, implDir)
	implInternal, implExternal := loadTestVariantsForDir(t, implDir)

	// The record set the production half emitted, as packageImplementBases parses it out of
	// package_info.cs: `[assembly: GoImplement<Rows, sort_package.Interface>]`.
	recorded := map[string][]string{"Rows": {"sort_package"}}

	got := declarationClosureImports([]*packages.Package{implProduction, implInternal, implExternal}, nil,
		[]string{implProduction.PkgPath, "testing"}, recorded, nil)

	if len(got) != 1 || got[0] != "sort" {
		t.Fatalf("binding a member on Rows must surface the sort.Interface base its declaration carries; got %v", got)
	}

	// NEGATIVE, and the one that matters most — THE RECORDS ARE THE GATE, not Go satisfaction. The
	// same package, the same member access, the same `types.Implements` answer; only the record set
	// is empty, as it is for a type the converter never converted a cast of. Go satisfaction alone
	// drifts 16 of the 96 banked projects (os.File's syscall.Conn into thirteen, bytes.Buffer's io
	// into sort and unicode/utf8, buildcfg's fmt.Stringer), every one of which compiles clean with
	// none of it.
	if got := declarationClosureImports([]*packages.Package{implProduction, implInternal, implExternal}, nil,
		[]string{implProduction.PkgPath, "testing"}, nil, nil); len(got) != 0 {
		t.Fatalf("satisfying an interface is not carrying it as a base — only a record does; got %v", got)
	}

	// NEGATIVE (a record for ANOTHER type answers for nothing). os records `rawConn` against
	// syscall.RawConn and thirteen projects bind `os.File` members with no syscall reference; the
	// per-TYPE key is what keeps those apart.
	if got := declarationClosureImports([]*packages.Package{implProduction, implInternal, implExternal}, nil,
		[]string{implProduction.PkgPath, "testing"}, map[string][]string{"Other": {"sort_package"}}, nil); len(got) != 0 {
		t.Fatalf("a record keyed to another type must not surface a base here; got %v", got)
	}

	// NEGATIVE (the receiver restriction carries here too). The same type, the same production
	// import — but the test only NAMES a value of it and never binds a member, so no declaration has
	// to be resolved.
	valueDir := t.TempDir()
	writeModuleFiles(t, valueDir, map[string]string{
		"go.mod": "module example/implvalue\n\ngo 1.23\n",
		"main.go": "package implvalue\n" +
			"import \"sort\"\n" +
			"type Rows []int\n" +
			"func (r Rows) Len() int           { return len(r) }\n" +
			"func (r Rows) Less(i, j int) bool { return r[i] < r[j] }\n" +
			"func (r Rows) Swap(i, j int)      { r[i], r[j] = r[j], r[i] }\n" +
			"func Order(r Rows)                { sort.Sort(r) }\n",
		"main_test.go": "package implvalue\nimport \"testing\"\nfunc TestPass(t *testing.T) {\n\tvar r Rows\n\tOrder(r)\n}\n",
	})

	valueProduction := loadProductionForDir(t, valueDir)
	valueInternal, valueExternal := loadTestVariantsForDir(t, valueDir)

	if got := declarationClosureImports([]*packages.Package{valueProduction, valueInternal, valueExternal}, nil,
		[]string{valueProduction.PkgPath, "testing"}, map[string][]string{"Rows": {"sort_package"}}, nil); len(got) != 0 {
		t.Fatalf("passing a value along binds no member and resolves no base list; got %v", got)
	}

	// NEGATIVE (the candidate match is real satisfaction, not mere presence of the import). The
	// production package still imports sort and the test still binds a member — but the type's
	// method set does not satisfy sort.Interface, so its declaration carries no such base.
	noneDir := t.TempDir()
	writeModuleFiles(t, noneDir, map[string]string{
		"go.mod": "module example/implnone\n\ngo 1.23\n",
		"main.go": "package implnone\n" +
			"import \"sort\"\n" +
			"type Rows []int\n" +
			"func (r Rows) Total() int { return len(r) }\n" +
			"func Order(s []int)       { sort.Ints(s) }\n",
		"main_test.go": "package implnone\nimport \"testing\"\nfunc TestTotal(t *testing.T) {\n\tvar r Rows\n\tif r.Total() != 0 {\n\t\tt.Fatal(\"bad\")\n\t}\n}\n",
	})

	noneProduction := loadProductionForDir(t, noneDir)
	noneInternal, noneExternal := loadTestVariantsForDir(t, noneDir)

	if got := declarationClosureImports([]*packages.Package{noneProduction, noneInternal, noneExternal}, nil,
		[]string{noneProduction.PkgPath, "testing"}, map[string][]string{"Rows": {"sort_package"}}, nil); len(got) != 0 {
		t.Fatalf("a type that satisfies nothing carries no interface base; got %v", got)
	}
}

// The implemented-interface edge across the PACKAGE BOUNDARY (go/types). The type whose realized
// base list must resolve is declared in a FOREIGN package, so its records live in that package's own
// package_info.cs, not in the package-under-test's — and the member that binds it spells no selector
// at all. go/types' check_test.go asserts `err.(scanner.ErrorList)` and then calls `len(list)` on the
// result; `scanner.ErrorList`'s `sort.Interface` base comes from go/scanner's record set, `sort` is in
// the PRODUCTION project's references and in no test import, and the test host failed to build with a
// single `CS0012 … 'sort_package.Interface'` at that `len`.
func TestDeclarationClosureImportsSurfacesForeignImplementedBases(t *testing.T) {
	// The scanner shape, one package over: `Rows` is declared and recorded in example/fimpl/rows,
	// the suite imports rows (so Rows BINDS) but never sort, and the only use is `len(r)`.
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/fimpl\n\ngo 1.23\n",
		"rows/rows.go": "package rows\n" +
			"import \"sort\"\n" +
			"type Rows []int\n" +
			"func (r Rows) Len() int           { return len(r) }\n" +
			"func (r Rows) Less(i, j int) bool { return r[i] < r[j] }\n" +
			"func (r Rows) Swap(i, j int)      { r[i], r[j] = r[j], r[i] }\n" +
			"func Order(r Rows)                { sort.Sort(r) }\n",
		"main.go":      "package fimpl\nimport \"example/fimpl/rows\"\nfunc Make() rows.Rows { return nil }\n",
		"main_test.go": "package fimpl\nimport (\n\t\"testing\"\n\n\t\"example/fimpl/rows\"\n)\nfunc TestLen(t *testing.T) {\n\tvar r rows.Rows\n\tif len(r) != 0 {\n\t\tt.Fatal(\"bad\")\n\t}\n}\n",
	})

	production := loadProductionForDir(t, dir)
	internal, external := loadTestVariantsForDir(t, dir)
	referenced := []string{production.PkgPath, "example/fimpl/rows", "testing"}

	// The record set go/scanner's OWN package_info.cs carries, as packageImplementBases parses it.
	foreign := func(importPath string) map[string][]string {
		if importPath == "example/fimpl/rows" {
			return map[string][]string{"Rows": {"sort_package"}}
		}
		return nil
	}

	roots := []*packages.Package{production, internal, external}

	if got := declarationClosureImports(roots, nil, referenced, nil, foreign); len(got) != 1 || got[0] != "sort" {
		t.Fatalf("len() on a foreign type binds its realized base list, so sort must be surfaced; got %v", got)
	}

	// NEGATIVE — the RECORDS remain the gate across the boundary. Same package, same `len`, same
	// `types.Implements` answer; the declaring package simply recorded no value-form cast, exactly as
	// for a type the converter never converted a cast of. This is the same gate the same-package edge
	// is measured on (satisfaction alone drifts 16 of 96 banked projects).
	if got := declarationClosureImports(roots, nil, referenced, nil, nil); len(got) != 0 {
		t.Fatalf("with no foreign record set, satisfying sort.Interface must surface nothing; got %v", got)
	}

	if got := declarationClosureImports(roots, nil, referenced, nil, func(string) map[string][]string { return nil }); len(got) != 0 {
		t.Fatalf("an empty foreign record set must surface nothing; got %v", got)
	}

	// NEGATIVE — a record keyed to ANOTHER type in that package answers for nothing.
	other := func(string) map[string][]string { return map[string][]string{"Other": {"sort_package"}} }

	if got := declarationClosureImports(roots, nil, referenced, nil, other); len(got) != 0 {
		t.Fatalf("a record keyed to another type must not surface a base here; got %v", got)
	}

	// NEGATIVE, and the boundary that keeps this edge from becoming "every named type": the suite
	// NAMES a value of the foreign type and PASSES IT ALONG, binding no member on it. Nothing may be
	// surfaced — the same negative the same-package edge pins for `Order(r)`.
	passDir := t.TempDir()
	writeModuleFiles(t, passDir, map[string]string{
		"go.mod": "module example/fpass\n\ngo 1.23\n",
		"rows/rows.go": "package rows\n" +
			"import \"sort\"\n" +
			"type Rows []int\n" +
			"func (r Rows) Len() int           { return len(r) }\n" +
			"func (r Rows) Less(i, j int) bool { return r[i] < r[j] }\n" +
			"func (r Rows) Swap(i, j int)      { r[i], r[j] = r[j], r[i] }\n" +
			"func Order(r Rows)                { sort.Sort(r) }\n",
		"main.go":      "package fpass\nimport \"example/fpass/rows\"\nfunc Make() rows.Rows { return nil }\n",
		"main_test.go": "package fpass\nimport (\n\t\"testing\"\n\n\t\"example/fpass/rows\"\n)\nfunc TestPass(t *testing.T) {\n\tvar r rows.Rows\n\trows.Order(r)\n\t_ = r\n}\n",
	})

	passProduction := loadProductionForDir(t, passDir)
	passInternal, passExternal := loadTestVariantsForDir(t, passDir)
	passForeign := func(importPath string) map[string][]string {
		if importPath == "example/fpass/rows" {
			return map[string][]string{"Rows": {"sort_package"}}
		}
		return nil
	}

	if got := declarationClosureImports([]*packages.Package{passProduction, passInternal, passExternal}, nil,
		[]string{passProduction.PkgPath, "example/fpass/rows", "testing"}, nil, passForeign); len(got) != 0 {
		t.Fatalf("naming a foreign value and passing it along binds no member; got %v", got)
	}

	// The other two non-selector member bindings the same lowering rule covers: RANGE and INDEX.
	for name, body := range map[string]string{
		"range": "\tvar r rows.Rows\n\tfor _, v := range r {\n\t\t_ = v\n\t}\n",
		"index": "\tr := rows.Rows{1}\n\tif r[0] != 1 {\n\t\tt.Fatal(\"bad\")\n\t}\n",
	} {
		formDir := t.TempDir()
		writeModuleFiles(t, formDir, map[string]string{
			"go.mod": "module example/fform\n\ngo 1.23\n",
			"rows/rows.go": "package rows\n" +
				"import \"sort\"\n" +
				"type Rows []int\n" +
				"func (r Rows) Len() int           { return len(r) }\n" +
				"func (r Rows) Less(i, j int) bool { return r[i] < r[j] }\n" +
				"func (r Rows) Swap(i, j int)      { r[i], r[j] = r[j], r[i] }\n" +
				"func Order(r Rows)                { sort.Sort(r) }\n",
			"main.go":      "package fform\nimport \"example/fform/rows\"\nfunc Make() rows.Rows { return nil }\n",
			"main_test.go": "package fform\nimport (\n\t\"testing\"\n\n\t\"example/fform/rows\"\n)\nfunc TestForm(t *testing.T) {\n" + body + "}\n",
		})

		formProduction := loadProductionForDir(t, formDir)
		formInternal, formExternal := loadTestVariantsForDir(t, formDir)
		formForeign := func(importPath string) map[string][]string {
			if importPath == "example/fform/rows" {
				return map[string][]string{"Rows": {"sort_package"}}
			}
			return nil
		}

		got := declarationClosureImports([]*packages.Package{formProduction, formInternal, formExternal}, nil,
			[]string{formProduction.PkgPath, "example/fform/rows", "testing"}, nil, formForeign)

		if len(got) != 1 || got[0] != "sort" {
			t.Fatalf("a %s over a foreign type binds a member on it, so sort must be surfaced; got %v", name, got)
		}
	}
}

// The foreign half of the edge reads the DECLARING package's emitted package_info.cs out of the
// runtime root, by the same getImportPackageInfo route the `<ImportedTypeAliases>` block already
// resolves a dependency's metadata by. A root that does not hold the file yields no edge rather than
// an error — the same silent-nothing an unreadable root produces for the package under test.
func TestForeignImplementBasesResolverReadsDeclaringPackageInfo(t *testing.T) {
	root := t.TempDir()
	sortDir := filepath.Join(root, "core", "sort")

	if err := os.MkdirAll(sortDir, 0755); err != nil {
		t.Fatal(err)
	}

	if err := os.WriteFile(filepath.Join(sortDir, PackageInfoFileName),
		[]byte("// <InterfaceImplementations>\r\n"+
			"[assembly: GoImplement<Rows, sort_package.Interface>(Pointer = true)]\r\n"+
			"[assembly: GoImplement<Rows, other_package.Thing>]\r\n"+
			"// </InterfaceImplementations>\r\n"), 0644); err != nil {
		t.Fatal(err)
	}

	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	resolve := foreignImplementBasesResolver(Options{
		go2csPath:      root,
		goRoot:         goRoot,
		goPath:         build.Default.GOPATH,
		targetPlatform: runtime.GOOS + "/" + runtime.GOARCH,
	})
	bases := resolve("sort")

	// The VALUE-form record is the only one that lands a base on the type; the POINTER form generates
	// an adapter class instead and places no demand on a member binding.
	if len(bases["Rows"]) != 1 || bases["Rows"][0] != "other_package" {
		t.Fatalf("the resolver must read the declaring package's VALUE-form records only; got %v", bases["Rows"])
	}

	// Memoized, and an unreadable package yields an empty map rather than an error.
	if got := resolve("strconv"); len(got) != 0 {
		t.Fatalf("a package with no readable package_info.cs must yield no records; got %v", got)
	}

	if got := resolve("sort"); len(got["Rows"]) != 1 {
		t.Fatalf("the memoized second read must return the same records; got %v", got)
	}
}

// F14b guard: a dependency that fails to resolve fails the test-project emission loudly, naming
// the dependency — never a silent reference drop.
func TestWriteTestProjectFailsLoudlyOnDependencyError(t *testing.T) {
	dir := t.TempDir()
	importPackageDirs = map[string]importedPackageMeta{}

	err := writeTestProject(filepath.Join(dir, "broken.tests.csproj"), "broken", "go", testProjectRecompile, nil, nil, nil,
		[]string{"go2cs.invalid/definitely/not/resolvable"}, Options{go2csPath: dir})

	if err == nil {
		t.Fatal("expected a loud dependency resolution error, got nil")
	}
	if !strings.Contains(err.Error(), "go2cs.invalid/definitely/not/resolvable") {
		t.Fatalf("error must name the unresolvable dependency, got %q", err)
	}
}

// F7 guard: the converter revision is the RUNNING executable's content hash — a stale binary
// self-identifies (the source-directory digest reported fresh revisions for stale binaries).
func TestConverterRevisionIsStableExecutableDigest(t *testing.T) {
	first := converterRevision()
	second := converterRevision()
	if first != second || !strings.HasPrefix(first, "exe-") {
		t.Fatalf("converter revision is not a stable executable digest: %q, %q", first, second)
	}
}

func TestInputDigestDetectsSourceChanges(t *testing.T) {
	dir := t.TempDir()
	source := filepath.Join(dir, "value.go")
	if err := os.WriteFile(source, []byte("package value\n"), 0644); err != nil {
		t.Fatal(err)
	}
	options := Options{targetPlatform: "windows/amd64"}
	first, err := testInputDigest(dir, dir, options, "converter")
	if err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(source, []byte("package value\nvar changed = true\n"), 0644); err != nil {
		t.Fatal(err)
	}
	second, err := testInputDigest(dir, dir, options, "converter")
	if err != nil {
		t.Fatal(err)
	}
	if first == second {
		t.Fatal("input digest did not change after source content changed")
	}
}

// F7 guard: a NEWLY ADDED testdata fixture invalidates the digest — the fixture set is globbed
// fresh at digest time, never taken from the manifest's recorded list.
func TestInputDigestDetectsNewTestdataFixture(t *testing.T) {
	dir := t.TempDir()
	if err := os.WriteFile(filepath.Join(dir, "value.go"), []byte("package value\n"), 0644); err != nil {
		t.Fatal(err)
	}
	options := Options{targetPlatform: "windows/amd64"}
	first, err := testInputDigest(dir, dir, options, "converter")
	if err != nil {
		t.Fatal(err)
	}

	if err := os.MkdirAll(filepath.Join(dir, "testdata"), 0755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(dir, "testdata", "new-fixture.txt"), []byte("fixture\n"), 0644); err != nil {
		t.Fatal(err)
	}

	second, err := testInputDigest(dir, dir, options, "converter")
	if err != nil {
		t.Fatal(err)
	}
	if first == second {
		t.Fatal("input digest did not change after a testdata fixture was added")
	}
}

// F7 guard: output-affecting conversion options are part of the digest.
func TestInputDigestDetectsConversionOptionChanges(t *testing.T) {
	dir := t.TempDir()
	if err := os.WriteFile(filepath.Join(dir, "value.go"), []byte("package value\n"), 0644); err != nil {
		t.Fatal(err)
	}

	first, err := testInputDigest(dir, dir, Options{targetPlatform: "windows/amd64", useChannelOperators: true}, "converter")
	if err != nil {
		t.Fatal(err)
	}
	second, err := testInputDigest(dir, dir, Options{targetPlatform: "windows/amd64", useChannelOperators: false}, "converter")
	if err != nil {
		t.Fatal(err)
	}
	if first == second {
		t.Fatal("input digest did not change after an output-affecting option changed")
	}
}

func loadTestVariantForDir(t *testing.T, dir string) (*packages.Package, *packages.Package) {
	t.Helper()

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: dir, Tests: true}, ".")
	if err != nil {
		t.Fatal(err)
	}
	production := findProductionPackage(loaded, dir)
	if production == nil {
		t.Fatal("production package was not loaded")
	}
	internal, _ := findTestVariants(loaded, production)
	if internal == nil {
		t.Fatal("same-package test variant was not loaded")
	}
	return production, internal
}

// writeModuleFiles materializes a throwaway Go module (name -> contents) into dir.
func writeModuleFiles(t *testing.T, dir string, files map[string]string) {
	t.Helper()

	for name, contents := range files {
		path := filepath.Join(dir, name)

		// A sub-PACKAGE fixture ("a/a.go") needs its directory created first.
		if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil {
			t.Fatal(err)
		}
		if err := os.WriteFile(path, []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
}

// loadProductionForDir loads just the production package for a module dir (no test variant
// required) with full type information, for exercising the go/types-driven reference helpers.
func loadProductionForDir(t *testing.T, dir string) *packages.Package {
	t.Helper()

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: dir, Tests: true}, ".")
	if err != nil {
		t.Fatal(err)
	}
	production := findProductionPackage(loaded, dir)
	if production == nil {
		t.Fatal("production package was not loaded")
	}
	if len(production.Errors) > 0 {
		t.Fatalf("production package load failed: %v", production.Errors)
	}
	return production
}

func TestUnsupportedTestingCapabilityIsDiscovered(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":        "module example/capability\n\ngo 1.23\n",
		"value.go":      "package capability\n",
		"value_test.go": "package capability\nimport \"testing\"\nfunc TestBlocked(t *testing.T) { testing.Benchmark(func(b *testing.B) { b.ReportAllocs() }) }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	if len(declarations) != 1 || declarations[0].Name != "TestBlocked" {
		t.Fatalf("declarations = %#v, want just TestBlocked", declarations)
	}
	declaration := declarations[0]
	if declaration.Status != "unsupported" || !strings.Contains(declaration.Reason, "B.ReportAllocs") {
		t.Fatalf("TestBlocked should be capability-blocked naming B.ReportAllocs, got status %q reason %q", declaration.Status, declaration.Reason)
	}
	if !NewHashSet(declaration.RequiredCapabilities).Contains("B.ReportAllocs") {
		t.Fatalf("required capabilities %v do not contain B.ReportAllocs", declaration.RequiredCapabilities)
	}
	if NewHashSet(supportedTestCapabilities()).Contains("B.ReportAllocs") {
		t.Fatal("B.ReportAllocs unexpectedly appears in the runtime capability list")
	}

	// The exemplar moved (this test used to use T.Deadline, which is now supported), so pin the
	// direction of that change too: T.Deadline must BE supported, or context's six cancellation
	// tests silently drop out of the run set again.
	if !NewHashSet(supportedTestCapabilities()).Contains("T.Deadline") {
		t.Fatal("T.Deadline must be a supported capability — core/testing implements it")
	}
}

// A helper declared `func h(t testing.TB)` records its calls under the TB receiver, NOT under T, so
// the two spellings are separate capability names over one implementation. The whole TB surface must
// stay listed: it was the absence of every TB member that excluded os/exec's 26 process-spawn tests
// wholesale, through the single helper `exePath(t testing.TB)`. Pins both the attribution (a TB
// receiver yields TB.*) and the roster (all 18 of Go 1.23's public TB members are supported).
func TestTestingTBCapabilitiesAreSupported(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/tbsurface\n\ngo 1.23\n",
		"value.go": "package tbsurface\n",
		"value_test.go": "package tbsurface\n" +
			"import \"testing\"\n" +
			"func exePath(tb testing.TB) string { tb.Helper(); tb.Fatal(\"boom\"); return \"\" }\n" +
			"func TestViaTB(t *testing.T) { _ = exePath(t) }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	if len(declarations) != 1 || declarations[0].Name != "TestViaTB" {
		t.Fatalf("declarations = %#v, want just TestViaTB", declarations)
	}

	// Attribution: the requirement is recorded against the TB receiver the helper declares, and
	// reaches the test transitively — the shape that gated os/exec.
	required := NewHashSet(declarations[0].RequiredCapabilities)
	for _, capability := range []string{"TB.Helper", "TB.Fatal"} {
		if !required.Contains(capability) {
			t.Fatalf("required capabilities %v do not contain %q — a testing.TB receiver must attribute to TB.*", declarations[0].RequiredCapabilities, capability)
		}
	}
	if required.Contains("T.Fatal") {
		t.Fatalf("a testing.TB receiver must not attribute to T.*, got %v", declarations[0].RequiredCapabilities)
	}

	// Roster: with the surface supported the test is included, not disclosed-unsupported.
	if declarations[0].Status != "included" {
		t.Fatalf("TestViaTB status = %q reason %q, want included — the TB surface is supported", declarations[0].Status, declarations[0].Reason)
	}

	// And the whole surface stays listed: Go 1.23's testing.TB, whose every member core/testing
	// implements on T and the generated ж-adapter forwards. A member dropped here silently excludes
	// every test in the corpus that reaches a helper calling it.
	supported := NewHashSet(supportedTestCapabilities())
	for _, member := range []string{
		"Cleanup", "Error", "Errorf", "Fail", "FailNow", "Failed", "Fatal", "Fatalf", "Helper",
		"Log", "Logf", "Name", "Setenv", "Skip", "SkipNow", "Skipf", "Skipped", "TempDir",
	} {
		if !supported.Contains("TB." + member) {
			t.Fatalf("TB.%s must be a supported capability — core/testing's TB declares it and T implements it", member)
		}
	}
}

// The unsupported-RUNTIME-capability gate maps a SYMBOL to the CAPABILITY it requires, and what the
// report shows is the capability. Guards all three properties the mechanism turns on: the lookup
// answers with the capability rather than the symbol, it stays package-scope only, and runtime.Goexit
// — its first entry — is no longer gated statically, its remaining unimplemented case (Goexit from the
// MAIN goroutine) being undecidable from a call graph and therefore gated at runtime instead.
func TestUnsupportedRuntimeCapabilityGate(t *testing.T) {
	runtimePkg := types.NewPackage("runtime", "runtime")
	signature := types.NewSignatureType(nil, nil, nil, nil, nil, false)
	goexit := types.NewFunc(token.NoPos, runtimePkg, "Goexit", signature)

	if capability, blocked := unsupportedRuntimeCapability(goexit); blocked {
		t.Fatalf("runtime.Goexit must no longer be statically capability-gated (matched %q)", capability)
	}

	// The lookup reports the CAPABILITY, never the key: several symbols can want one capability, and
	// a host capability names no symbol a test calls at all.
	unsupportedRuntimeCapabilities["runtime.Goexit"] = "cooperative goroutine unwind"
	defer delete(unsupportedRuntimeCapabilities, "runtime.Goexit")

	if capability, blocked := unsupportedRuntimeCapability(goexit); !blocked || capability != "cooperative goroutine unwind" {
		t.Fatalf("gate did not fire for a listed symbol: capability %q, blocked %v", capability, blocked)
	}

	// And it stays package-scope only — a METHOD named Goexit is not runtime.Goexit.
	receiver := types.NewVar(token.NoPos, runtimePkg, "g", types.Typ[types.Int])
	method := types.NewFunc(token.NoPos, runtimePkg, "Goexit", types.NewSignatureType(receiver, nil, nil, nil, nil, false))

	if _, blocked := unsupportedRuntimeCapability(method); blocked {
		t.Fatal("a method named Goexit must not match the package-scope runtime.Goexit entry")
	}

	// The standing entries must each name a capability, so nothing can be added as a bare symbol and
	// silently surface in the report as one.
	for symbol, capability := range unsupportedRuntimeCapabilities {
		if capability == "" {
			t.Fatalf("entry %q has no capability name — the report would show the bare symbol", symbol)
		}
	}
}

// A capability entry may name the TEST DECLARATION itself, for an impossibility that belongs to the
// host rather than to anything the test calls (os's TestRemoveAllWithExecutedProcess, which assumes
// the test binary is a relocatable single file). Nothing NAMES a test, so the caller-side arm of the
// attribution can never record such a requirement — requiredFor gates a listed function on its own
// account, and this is the control for that arm.
func TestUnsupportedRuntimeCapabilityGatesTheDeclarationItself(t *testing.T) {
	pkg := types.NewPackage("example_test", "example_test")
	signature := types.NewSignatureType(nil, nil, nil, nil, nil, false)
	subject := types.NewFunc(token.NoPos, pkg, "TestHostBound", signature)
	bystander := types.NewFunc(token.NoPos, pkg, "TestOrdinary", signature)

	unsupportedRuntimeCapabilities["example_test.TestHostBound"] = "relocatable single-file test executable"
	defer delete(unsupportedRuntimeCapabilities, "example_test.TestHostBound")

	analysis := testCapabilityAnalysis{
		direct:   map[*types.Func]HashSet[string]{subject: {}, bystander: {}},
		referees: map[*types.Func]map[*types.Func]bool{subject: {}, bystander: {}},
	}

	if required := analysis.requiredFor(subject); !required.Contains("relocatable single-file test executable") {
		t.Fatalf("a listed declaration must require its own capability, got %v", required.Keys())
	}

	// Negative control: an unlisted declaration in the same package stays clean.
	if required := analysis.requiredFor(bystander); !required.IsEmpty() {
		t.Fatalf("an unlisted declaration must require nothing, got %v", required.Keys())
	}
}

// A declaration-keyed entry must be keyed on the EXTERNAL TEST package's import path, which is the
// package path with "_test" appended: os/exec's helper-copying tests live in `package exec_test`,
// whose types.Package path is os/exec_test — not os/exec, and not exec_test. Getting it wrong is
// SILENT, because the map lookup simply misses: every gated test runs, the package reports its old
// failing count, and nothing anywhere names the cause. So the shape is pinned for every entry that
// names a test, and the one standing entry is pinned by exact key.
func TestDeclarationKeyedCapabilityEntries(t *testing.T) {
	const standing = "os_test.TestRemoveAllWithExecutedProcess"

	if capability := unsupportedRuntimeCapabilities[standing]; capability != "relocatable single-file test executable" {
		t.Fatalf("standing declaration entry %q names capability %q", standing, capability)
	}

	for key := range unsupportedRuntimeCapabilities {
		packagePath, name, split := strings.Cut(key, ".")

		// Only entries naming a Test DECLARATION are in scope. The rest key on a symbol a test
		// calls (syscall.CommandLineToArgv), where no such convention applies.
		if !split || !isGoTestName(name, "Test") {
			continue
		}

		if !strings.HasSuffix(packagePath, "_test") {
			t.Fatalf("entry %q names a test but is not keyed on an external test package path "+
				"(want <import path>_test.%s) — a mis-keyed gate never fires and never says so", key, name)
		}
	}
}

// A DECLARATION-keyed gate is not a declaration-sized omission: eligibleTerminalTestResults cuts a
// verdict row at its first "/", so gating one table-driven test withdraws every subtest with it.
// This is the guard that those rows are enumerated for the proof page rather than absorbed — os/exec
// measured 40 rows withdrawn by 2 entries where only 27 were failing, and the page must name them.
func TestCapabilityGatedDeclarationsEnumerateSubtestRows(t *testing.T) {
	manifest := testManifest{Tests: []testDeclaration{
		{Name: "TestCommand", Kind: "test", Status: "unsupported",
			Reason: unsupportedCapabilityReasonPrefix + "relocatable single-file test executable"},
		{Name: "TestOrdinary", Kind: "test", Status: "included"},
		{Name: "TestDeferred", Kind: "example", Status: "unsupported", Reason: "example execution is deferred to Phase 4D"},
	}}

	goResults := map[string]string{
		"TestCommand":          "pass",
		"TestCommand/relative": "pass",
		"TestCommand/absolute": "pass",
		"TestCommandOther":     "pass",
		"TestOrdinary":         "pass",
		"TestOrdinary/subtest": "pass",
		"TestDeferred":         "pass",
	}

	gated := capabilityGatedDeclarations(goResults, manifest)

	if len(gated) != 1 {
		t.Fatalf("expected exactly the capability-gated declaration, got %d: %+v", len(gated), gated)
	}

	if gated[0].Name != "TestCommand" || gated[0].Capabilities != "relocatable single-file test executable" {
		t.Fatalf("gated declaration is %+v", gated[0])
	}

	// Sorted, and prefix matching is on the WHOLE first segment — TestCommandOther is a different
	// declaration that merely shares a prefix, and an included test's subtests stay claimed.
	want := []string{"TestCommand", "TestCommand/absolute", "TestCommand/relative"}

	if !reflect.DeepEqual(gated[0].Rows, want) {
		t.Fatalf("withdrawn rows are %v, want %v", gated[0].Rows, want)
	}

	// Nothing gated ⇒ nothing published: the page section must not appear for the ordinary package.
	clean := testManifest{Tests: []testDeclaration{{Name: "TestOrdinary", Kind: "test", Status: "included"}}}

	if enumerated := capabilityGatedDeclarations(goResults, clean); enumerated != nil {
		t.Fatalf("a package with no capability gate must enumerate nothing, got %+v", enumerated)
	}
}

// AllocsPerRun support guard: the shim implements testing.AllocsPerRun (byte-derived — see
// core/testing/testing.cs), so a test requiring it must convert as INCLUDED while the
// requirement still appears in the manifest's per-test attribution.
func TestAllocsPerRunCapabilityIsSupported(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/allocs\n\ngo 1.23\n",
		"value.go": "package allocs\n",
		"value_test.go": "package allocs\n" +
			"import \"testing\"\n" +
			"func TestNoAllocs(t *testing.T) {\n" +
			"\tif allocs := testing.AllocsPerRun(100, func() {}); allocs != 0 {\n" +
			"\t\tt.Errorf(\"allocs = %v\", allocs)\n" +
			"\t}\n" +
			"}\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	if len(declarations) != 1 || declarations[0].Name != "TestNoAllocs" {
		t.Fatalf("declarations = %#v, want just TestNoAllocs", declarations)
	}
	declaration := declarations[0]
	if declaration.Status != "included" {
		t.Fatalf("TestNoAllocs should be included (testing.AllocsPerRun is supported), got status %q reason %q", declaration.Status, declaration.Reason)
	}
	if !NewHashSet(declaration.RequiredCapabilities).Contains("testing.AllocsPerRun") {
		t.Fatalf("required capabilities %v do not contain testing.AllocsPerRun", declaration.RequiredCapabilities)
	}
}

// The shim's CoverMode() returns "" (Go's exact coverage-off value), so tests branching on it —
// strings' TestIndexRune is the sole stdlib caller in the #3-4 packages — must census as included.
func TestCoverModeCapabilityIsSupported(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/covermode\n\ngo 1.23\n",
		"value.go": "package covermode\n",
		"value_test.go": "package covermode\n" +
			"import \"testing\"\n" +
			"func TestCoverageOffPath(t *testing.T) {\n" +
			"\tif testing.CoverMode() != \"\" {\n" +
			"\t\tt.Skip(\"coverage instrumentation active\")\n" +
			"\t}\n" +
			"}\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	if len(declarations) != 1 || declarations[0].Name != "TestCoverageOffPath" {
		t.Fatalf("declarations = %#v, want just TestCoverageOffPath", declarations)
	}
	declaration := declarations[0]
	if declaration.Status != "included" {
		t.Fatalf("TestCoverageOffPath should be included (testing.CoverMode is supported), got status %q reason %q", declaration.Status, declaration.Reason)
	}
	if !NewHashSet(declaration.RequiredCapabilities).Contains("testing.CoverMode") {
		t.Fatalf("required capabilities %v do not contain testing.CoverMode", declaration.RequiredCapabilities)
	}
}

// A Test function may drive an in-process benchmark itself: testing.Benchmark runs a func(*B)
// closure (reading b.N) and returns a BenchmarkResult whose NsPerOp() the test inspects —
// unicode's TestCalibrate is the stdlib case. The host implements Benchmark/B.N/BenchmarkResult
// (core/testing/testing.cs), so such a test must census as included rather than being
// disclosed-unsupported. (Top-level BenchmarkXxx DECLARATIONS remain unsupported by their kind;
// see TestManifestEligibility — this only covers Test functions that CALL testing.Benchmark.)
func TestBenchmarkCapabilityIsSupported(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/benchcap\n\ngo 1.23\n",
		"value.go": "package benchcap\n",
		"value_test.go": "package benchcap\n" +
			"import \"testing\"\n" +
			"func TestCalibrateLike(t *testing.T) {\n" +
			"\tr := testing.Benchmark(func(b *testing.B) {\n" +
			"\t\tfor i := 0; i < b.N; i++ {\n" +
			"\t\t}\n" +
			"\t})\n" +
			"\tif r.NsPerOp() < 0 {\n" +
			"\t\tt.Fatalf(\"ns/op = %d\", r.NsPerOp())\n" +
			"\t}\n" +
			"}\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	if len(declarations) != 1 || declarations[0].Name != "TestCalibrateLike" {
		t.Fatalf("declarations = %#v, want just TestCalibrateLike", declarations)
	}
	declaration := declarations[0]
	if declaration.Status != "included" {
		t.Fatalf("TestCalibrateLike should be included (testing.Benchmark/B.N/BenchmarkResult.NsPerOp are supported), got status %q reason %q", declaration.Status, declaration.Reason)
	}
	required := NewHashSet(declaration.RequiredCapabilities)
	for _, capability := range []string{"testing.Benchmark", "B.N", "BenchmarkResult.NsPerOp"} {
		if !required.Contains(capability) {
			t.Fatalf("required capabilities %v do not contain %q", declaration.RequiredCapabilities, capability)
		}
	}
}

// F4 guard: capability attribution is per test THROUGH its helper closure — one blocked test
// (via a helper it calls) blocks itself; its supported sibling stays included.
func TestPerTestCapabilityAttributionBlocksOnlyOffendingTest(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/attribution\n\ngo 1.23\n",
		"value.go": "package attribution\n",
		"value_test.go": "package attribution\n" +
			"import \"testing\"\n" +
			// T.Deadline landed with the one-tree consolidation, so the example unsupported
			// capability is now a B member reached from a Test through testing.Benchmark —
			// still attributed through a helper, which is what this test exists to prove.
			"func helperBench(t *testing.T) { testing.Benchmark(func(b *testing.B) { b.ResetTimer() }) }\n" +
			"func TestBlocked(t *testing.T) { helperBench(t) }\n" +
			"func TestSupported(t *testing.T) { t.Log(\"fine\") }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	byName := map[string]testDeclaration{}
	for _, declaration := range declarations {
		byName[declaration.Name] = declaration
	}

	blocked, ok := byName["TestBlocked"]
	if !ok || blocked.Status != "unsupported" || !strings.Contains(blocked.Reason, "B.ResetTimer") {
		t.Fatalf("TestBlocked should be capability-blocked through its helper, got %#v", blocked)
	}

	supported, ok := byName["TestSupported"]
	if !ok || supported.Status != "included" {
		t.Fatalf("TestSupported should stay included, got %#v", supported)
	}
}

// F2 guard: Example declarations (zero parameters) are discovered and disclosed as unsupported;
// an invalid lowercase-suffix name stays out (matching `go test`, which would not run it).
func TestExampleDeclarationsAreDiscoveredAndDisclosed(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/examples\n\ngo 1.23\n",
		"value.go": "package examples\n",
		"value_test.go": "package examples\n" +
			"import \"fmt\"\n" +
			"func ExampleValue() { fmt.Println(\"value\") }\n" +
			"func Example() { fmt.Println(\"bare\") }\n" +
			"func Examplelower() { fmt.Println(\"not an example\") }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, internal := loadTestVariantForDir(t, dir)

	analysis := analyzeTestingCapabilities(internal)
	declarations, _ := discoverTestDeclarations(internal, testFileEntries(internal), dir, analysis, NewHashSet(supportedTestCapabilities()))

	byName := map[string]testDeclaration{}
	for _, declaration := range declarations {
		byName[declaration.Name] = declaration
	}

	for _, name := range []string{"Example", "ExampleValue"} {
		declaration, ok := byName[name]
		if !ok || declaration.Kind != "example" || declaration.Status != "unsupported" {
			t.Fatalf("%s should be a disclosed-unsupported example declaration, got %#v", name, declaration)
		}
	}

	if _, ok := byName["Examplelower"]; ok {
		t.Fatal("Examplelower is not a valid example name and must not be discovered")
	}
}

// Shared-writer guard: writePackageInfoFile with mergeExisting preserves a section's existing
// entries, merges additions from the current package-scoped globals, dedupes, and sorts —
// identical emission semantics for production package_info.cs and the test path's
// package_test_info.cs (which is why the test path has no private metadata writer to drift).
func TestWritePackageInfoFileMergesExistingSections(t *testing.T) {
	dir := t.TempDir()
	fileName := filepath.Join(dir, "package_test_info.cs")

	seed := strings.Join([]string{
		"namespace go;",
		"",
		"// <ImportedTypeAliases>",
		"global using zeta = go.zeta_package.Zeta;",
		"// </ImportedTypeAliases>",
		"",
		"// <ExportedTypeAliases>",
		"// </ExportedTypeAliases>",
		"",
		"// <InterfaceImplementations>",
		"// </InterfaceImplementations>",
		"",
		"// <ImplicitConversions>",
		"// </ImplicitConversions>",
		"",
		"[GoPackage(\"value\")]",
		"public static partial class value_package",
		"{",
		"}",
	}, "\r\n")

	if err := os.WriteFile(fileName, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	packageName = "value"
	packageNamespace = "go"
	importedTypeAliases["alpha"] = "go.alpha_package.Alpha"
	importedTypeAliases["zeta"] = "go.zeta_package.Zeta"

	writePackageInfoFile(fileName, true)

	data, err := os.ReadFile(fileName)
	if err != nil {
		t.Fatal(err)
	}

	contents := string(data)
	alphaIndex := strings.Index(contents, "global using alpha = go.alpha_package.Alpha;")
	zetaIndex := strings.Index(contents, "global using zeta = go.zeta_package.Zeta;")

	if alphaIndex < 0 || zetaIndex < 0 {
		t.Fatalf("merged file must contain both the existing and the added alias:\n%s", contents)
	}
	if alphaIndex > zetaIndex {
		t.Fatal("merged aliases must be sorted (alpha before zeta)")
	}
	if strings.Count(contents, "global using zeta = go.zeta_package.Zeta;") != 1 {
		t.Fatal("merging must not duplicate an existing alias entry")
	}
	if !strings.Contains(contents, "public static partial class value_package") {
		t.Fatal("content outside the sections must be preserved")
	}
}

// Stale-spelling merge guard (container/heap): a GoImplement record persisted by an EARLIER
// converter run under a now-stale spelling must NOT survive a re-merge as a SECOND record for the
// same (impl, interface) pair. A NESTED package-under-test's own interface was once emitted fully
// qualified (`go.container.heap_package.Interface`); stripLocalTypeQualifier (the math/rand/v2
// collapse fix) now canonicalizes it to the bare local `Interface`. The -tests external-variant
// write MERGES the committed package_info_external_test.cs, so the stale qualified line and the freshly
// rendered bare line both reached the emitting HashSet and go2cs-gen composed the adapter twice —
// CS0102 + CS0111 + CS8646 on IntHeapжInterface. writePackageInfoFile now normalizes each merged-in
// GoImplement line through the same qualifyLocalTypeRef canonicalization the fresh render applies,
// so the two collapse to ONE. Guards the container/heap -tests re-validation from a fresh converter.
func TestMergedStaleGoImplementSpellingCollapses(t *testing.T) {
	dir := t.TempDir()
	fileName := filepath.Join(dir, externalTestPackageInfoFileName)

	// The committed (stale) external-test metadata: the interface is spelled fully qualified, as an
	// older converter emitted it before stripLocalTypeQualifier reduced it to the bare local form.
	seed := strings.Join([]string{
		"// <ImportedTypeAliases>",
		"// </ImportedTypeAliases>",
		"",
		"using go;",
		"using static go.container.heap_package;",
		"using static go.container.heap_test_package;",
		"",
		"// <ExportedTypeAliases>",
		"// </ExportedTypeAliases>",
		"",
		"// <InterfaceImplementations>",
		"[assembly: GoImplement<IntHeap, go.container.heap_package.Interface>(Pointer = true)]",
		"// </InterfaceImplementations>",
		"",
		"// <ImplicitConversions>",
		"// </ImplicitConversions>",
		"",
		"namespace go.container;",
		"",
		"public static partial class heap_test_package",
		"{",
		"}",
	}, "\r\n")

	if err := os.WriteFile(fileName, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	packageName = "heap_test"
	packageNamespace = "go.container"

	// What convertTestVariant installs for a -tests run of a NESTED package under test, and the
	// interface record the external variant's `heap.Init(&h)` cast rediscovers by import path.
	previous := testLocalTypePrefixes
	t.Cleanup(func() { testLocalTypePrefixes = previous })
	testLocalTypePrefixes = []string{"go.container.heap_package"}
	interfaceImplementations["container.heap_package.Interface"] = NewHashSet([]string{PointerPrefix + "<IntHeap>"})

	writePackageInfoFile(fileName, true)

	data, err := os.ReadFile(fileName)
	if err != nil {
		t.Fatal(err)
	}

	contents := string(data)

	if got := strings.Count(contents, "GoImplement<IntHeap,"); got != 1 {
		t.Fatalf("IntHeap must map to the interface through exactly ONE GoImplement record (a duplicate composes a duplicate adapter, CS0102/CS0111); got %d:\n%s", got, contents)
	}
	if strings.Contains(contents, "go.container.heap_package.Interface") {
		t.Fatalf("the merged record must canonicalize to the bare generator-resolvable spelling, not the stale qualified one:\n%s", contents)
	}
	if !strings.Contains(contents, "[assembly: GoImplement<IntHeap, Interface>(Pointer = true)]") {
		t.Fatalf("the surviving record must be the bare-spelling pointer form:\n%s", contents)
	}
}

// B4/B5 guard (partition predicate): an EXTERNAL variant GoImplement record anchors to the test
// package unit when its generated code must live in the test package class — bare (test-local)
// impls, every non-production ж pointer adapter, and adapter-class-marked (interface-sourced /
// foreign-value ᴠ) pairs; production-qualified records keep the production anchor, where the
// partial-struct/adapter they generate merges with the production declaration.
func TestExternalVariantRecordPartitionAnchors(t *testing.T) {
	resetPackageState(&packages.Package{})
	packageNamespace = "go"
	adapterClassImplementations.Add("io.Writer|value_package.WriterTo")

	cases := []struct {
		iface string
		impl  string
		want  bool
	}{
		{"value_package.Interface", "ByAge", true},                                       // bare value ⇒ test partial struct
		{"value_package.Interface", PointerPrefix + "<multiSorter>", true},               // bare pointer ⇒ test-hosted adapter
		{"rand_package.Source", PointerPrefix + "<go.math.rand.rand_package.PCG>", true}, // true-foreign pointer adapter
		{"io.Writer", PointerPrefix + "<value_package.Builder>", false},                  // production pointer ⇒ production-hosted adapter
		{"io.Writer", PointerPrefix + "<go.value_package.Builder>", false},               // rooted production form
		{"value_package.Interface", "value_package.IntSlice", false},                     // production value ⇒ production partial struct
		{"io.Writer", "value_package.WriterTo", true},                                    // adapter-class-marked ⇒ consumer-hosted ᴠ class
	}

	for _, testCase := range cases {
		if got := isTestAnchoredImplementRecord(testCase.iface, testCase.impl, "value_package"); got != testCase.want {
			t.Errorf("isTestAnchoredImplementRecord(%q, %q) = %v, want %v", testCase.iface, testCase.impl, got, testCase.want)
		}
	}

	if !isTestAnchoredConversionRecord("localData", "value_package.Person") {
		t.Error("a conversion pair involving a bare (test-local) type must anchor to the test unit")
	}
	if isTestAnchoredConversionRecord("value_package.Data", "go.io_package.Writer") {
		t.Error("a conversion pair between qualified types must keep the production anchor")
	}
}

// B4/B5 guard (split write): the external variant's test-anchored records land in
// package_info_external_test.cs — whose FIRST class is the test package class (the generator's anchor)
// and which carries NO [GoPackage] (the attribute-bearing partial stays in
// package_test_info.cs, CS0579) — while production-anchored records and the alias globals merge
// into package_test_info.cs. A variant with NO test-anchored records writes no unit at all
// (utf8-class packages keep their single-file shape).
func TestWriteExternalVariantMetadataSplitsAnchors(t *testing.T) {
	dir := t.TempDir()
	testInfoPath := filepath.Join(dir, testPackageInfoFileName)

	seed := strings.Join([]string{
		"// <ImportedTypeAliases>",
		"// </ImportedTypeAliases>",
		"",
		"using go;",
		"using static go.value_package;",
		"",
		"// <ExportedTypeAliases>",
		"// </ExportedTypeAliases>",
		"",
		"// <InterfaceImplementations>",
		"// </InterfaceImplementations>",
		"",
		"// <ImplicitConversions>",
		"// </ImplicitConversions>",
		"",
		"namespace go;",
		"",
		"[GoPackage(\"value\")]",
		"public static partial class value_package",
		"{",
		"}",
	}, "\r\n")

	if err := os.WriteFile(testInfoPath, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	packageName = "value_test"
	packageNamespace = "go"
	interfaceImplementations["value_package.Interface"] = NewHashSet([]string{
		"localSorter",            // bare ⇒ test anchor
		"value_package.IntSlice", // production-qualified ⇒ production anchor
	})
	importedTypeAliases["alpha"] = "go.alpha_package.Alpha"

	unitName, err := writeExternalVariantMetadata(testInfoPath, dir, "value", metadataClassPrefix("go", "value"), metadataClassPrefix("go", "value_test"))
	if err != nil {
		t.Fatal(err)
	}
	if unitName != externalTestPackageInfoFileName {
		t.Fatalf("unit name = %q, want %q", unitName, externalTestPackageInfoFileName)
	}

	unitData, err := os.ReadFile(filepath.Join(dir, externalTestPackageInfoFileName))
	if err != nil {
		t.Fatal(err)
	}
	unit := string(unitData)

	if !strings.Contains(unit, "[assembly: GoImplement<localSorter, value_package.Interface>]") {
		t.Fatalf("test-anchored record must land in the external unit:\n%s", unit)
	}
	if strings.Contains(unit, "IntSlice") {
		t.Fatalf("production-qualified record must not land in the external unit:\n%s", unit)
	}
	if strings.Contains(unit, "GoPackage") {
		t.Fatalf("the external unit must not duplicate the [GoPackage] attribute (CS0579):\n%s", unit)
	}
	if strings.Contains(unit, "global using alpha") {
		t.Fatalf("global using aliases must stay in package_test_info.cs (CS1537):\n%s", unit)
	}

	classIndex := strings.Index(unit, "public static partial class value_test_package")
	if classIndex < 0 || strings.Contains(unit[:classIndex], "partial class value_package") {
		t.Fatalf("the external unit's FIRST class must be the test package class:\n%s", unit)
	}

	infoData, err := os.ReadFile(testInfoPath)
	if err != nil {
		t.Fatal(err)
	}
	info := string(infoData)

	if !strings.Contains(info, "[assembly: GoImplement<value_package.IntSlice, value_package.Interface>]") {
		t.Fatalf("production-qualified record must merge into package_test_info.cs:\n%s", info)
	}
	if strings.Contains(info, "localSorter") {
		t.Fatalf("test-anchored record must not merge into package_test_info.cs:\n%s", info)
	}
	if !strings.Contains(info, "global using alpha = go.alpha_package.Alpha;") {
		t.Fatalf("alias globals must merge into package_test_info.cs:\n%s", info)
	}

	// A variant with no test-anchored records writes no unit.
	unitOnlyDir := t.TempDir()
	secondInfoPath := filepath.Join(unitOnlyDir, testPackageInfoFileName)
	if err := os.WriteFile(secondInfoPath, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	packageName = "value_test"
	packageNamespace = "go"
	interfaceImplementations["value_package.Interface"] = NewHashSet([]string{"value_package.IntSlice"})

	unitName, err = writeExternalVariantMetadata(secondInfoPath, unitOnlyDir, "value", metadataClassPrefix("go", "value"), metadataClassPrefix("go", "value_test"))
	if err != nil {
		t.Fatal(err)
	}
	if unitName != "" {
		t.Fatalf("no unit expected for a production-only record set, got %q", unitName)
	}
	if _, err := os.Stat(filepath.Join(unitOnlyDir, externalTestPackageInfoFileName)); !os.IsNotExist(err) {
		t.Fatal("no external unit file may be written when the variant has no test-anchored records")
	}
}

// B2c guard: a `using` ALIAS in the test metadata targeting an assembly the package reaches only
// TRANSITIVELY yields a direct project-reference import for that assembly — matched through the
// same namespace rendering the alias emission uses; direct dependencies, the production package,
// `using static` lines, and comment lines contribute nothing.
func TestAliasReferenceImportsAddsTransitiveAliasTargets(t *testing.T) {
	dir := t.TempDir()
	infoPath := filepath.Join(dir, testPackageInfoFileName)

	contents := strings.Join([]string{
		"// Example: global using mypkgꓸTable = go.map<go.@string, nint>;",
		"// <ImportedTypeAliases>",
		"global using reflectliteꓸKind = go.@internal.abi_package.ΔKind;",
		"global using reflectliteꓸType = go.@internal.reflectlite_package.ΔType;",
		"using conv = go.@internal.convalias_package;",
		"// </ImportedTypeAliases>",
		"using go;",
		"using static go.value_package;",
	}, "\r\n")

	if err := os.WriteFile(infoPath, []byte(contents), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	importPackageDirs = map[string]importedPackageMeta{
		"internal/abi":         {Dir: dir, Name: "abi"},
		"internal/reflectlite": {Dir: dir, Name: "reflectlite"},
		"internal/convalias":   {Dir: dir, Name: "convalias"},
		"internal/unrelated":   {Dir: dir, Name: "unrelated"},
	}

	got := aliasReferenceImports([]string{infoPath, filepath.Join(dir, "missing.cs")}, "sort", []string{"internal/reflectlite"})
	want := []string{"internal/abi", "internal/convalias"}

	if !reflect.DeepEqual(got, want) {
		t.Fatalf("alias reference imports = %v, want %v", got, want)
	}
}

func loadBothTestVariantsForDir(t *testing.T, dir string) (internal, external *packages.Package) {
	t.Helper()

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: dir, Tests: true}, ".")
	if err != nil {
		t.Fatal(err)
	}
	production := findProductionPackage(loaded, dir)
	if production == nil {
		t.Fatal("production package was not loaded")
	}
	return findTestVariants(loaded, production)
}

func readConvertedTestFile(t *testing.T, dir, name string) string {
	t.Helper()

	data, err := os.ReadFile(filepath.Join(dir, name))
	if err != nil {
		t.Fatal(err)
	}
	return string(data)
}

// B2 guard (production-name pinning): a `_test.go` METHOD declared over a production TYPE's name
// must Δ-rename the TEST-side declarator — the production .cs on disk keeps the bare name, so the
// pre-fix element rename split one assembly into two disagreeing halves (strings' export_test.go
// `func (r *Replacer) Replacer()`: CS0102 `strings_package` already contains `Replacer` + CS0246
// `ΔReplacer`). The rename must hold at the declaration, at internal-variant call sites, AND at
// EXTERNAL-variant call sites (the export_test pattern — both variants share one load, so the
// session-scoped object-keyed registry carries the internal pass's rename into the external one).
func TestTestVariantPinsProductionTypeAgainstTestMethodCollision(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/pinned\n\ngo 1.23\n",
		"value.go": "package pinned\n\ntype Replacer struct{ n int }\n\nfunc NewReplacer(n int) *Replacer { return &Replacer{n: n} }\n",
		"export_test.go": "package pinned\n\nfunc (r *Replacer) Replacer() int { return r.n }\n\n" +
			"func replacerProbe(r *Replacer) int { return r.Replacer() }\n",
		"external_test.go": "package pinned_test\n\nimport \"example/pinned\"\n\n" +
			"func externalProbe(r *pinned.Replacer) int { return r.Replacer() }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	internal, external := loadBothTestVariantsForDir(t, dir)
	if internal == nil || external == nil {
		t.Fatal("both test variants must load")
	}

	outputPath := t.TempDir()
	options := Options{indentSpaces: 4, preferVarDecl: true, useChannelOperators: true}

	// Session scope mirrors processTestConversion: ONE registry across both variant passes.
	testMethodRenames = make(map[types.Object]bool)
	t.Cleanup(func() { testMethodRenames = nil })

	if _, _, err := convertTestVariant(internal, testFileEntries(internal), outputPath, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	if nameCollisions["Replacer"] {
		t.Fatal("the production type name must stay pinned — nameCollisions must not Δ-rename `Replacer` in a test variant")
	}

	exportCs := readConvertedTestFile(t, outputPath, "export_test.cs")

	if !strings.Contains(exportCs, ShadowVarMarker+"Replacer(") {
		t.Fatalf("the test-side method declarator must be Δ-renamed:\n%s", exportCs)
	}
	if !strings.Contains(exportCs, "."+ShadowVarMarker+"Replacer()") {
		t.Fatalf("the internal-variant call site must follow the Δ-renamed method:\n%s", exportCs)
	}
	if strings.Contains(exportCs, "ref "+ShadowVarMarker+"Replacer") || strings.Contains(exportCs, PointerPrefix+"<"+ShadowVarMarker+"Replacer>") {
		t.Fatalf("the production TYPE must keep its bare name in every reference:\n%s", exportCs)
	}

	if _, _, err := convertTestVariant(external, testFileEntries(external), outputPath, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	externalCs := readConvertedTestFile(t, outputPath, "external_test.cs")

	if !strings.Contains(externalCs, "."+ShadowVarMarker+"Replacer()") {
		t.Fatalf("an EXTERNAL-variant call site must follow the internal variant's Δ-renamed method:\n%s", externalCs)
	}
}

// B9 guard (dot-import shadowing): a TEST-declared method named like a dot-imported production
// FUNCTION the variant references unqualified must Δ-rename the TEST-side declarator (C# member
// lookup binds the enclosing class's method group before `using static` — sort_test's `Sort(data)`
// bound example_keys_test.go's `By.Sort`, CS1501 ×14), while the dot-imported call keeps its bare
// emission. Discrimination both ways: a same-named method whose production function is never
// referenced (Stable), or referenced only QUALIFIED (Reverse), keeps its plain name.
func TestTestVariantRenamesTestMethodShadowingDotImportedFunction(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/value\n\ngo 1.23\n",
		"value.go": "package value\n\nfunc Sort(x []int) {}\n\nfunc Stable(x []int) {}\n\nfunc Reverse(x []int) {}\n",
		"value_test.go": "package value_test\n\nimport . \"example/value\"\n\n" +
			"type By []int\n\n" +
			"func (by By) Sort() { Sort(by) }\n\n" +
			"func (by By) Stable() {}\n\n" +
			"func probe(by By) {\n\tby.Sort()\n\tby.Stable()\n}\n",
		"qualified_test.go": "package value_test\n\nimport value \"example/value\"\n\n" +
			"type QB []int\n\nfunc (qb QB) Reverse() { value.Reverse(qb) }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	_, external := loadBothTestVariantsForDir(t, dir)
	if external == nil {
		t.Fatal("external test variant was not loaded")
	}

	outputPath := t.TempDir()
	testMethodRenames = make(map[types.Object]bool)
	t.Cleanup(func() { testMethodRenames = nil })

	options := Options{indentSpaces: 4, preferVarDecl: true, useChannelOperators: true}
	if _, _, err := convertTestVariant(external, testFileEntries(external), outputPath, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	valueCs := readConvertedTestFile(t, outputPath, "value_test.cs")

	if !strings.Contains(valueCs, ShadowVarMarker+"Sort(") {
		t.Fatalf("the shadowing test method declarator must be Δ-renamed:\n%s", valueCs)
	}
	if !strings.Contains(valueCs, "."+ShadowVarMarker+"Sort()") {
		t.Fatalf("the test method's call sites must follow the Δ-rename:\n%s", valueCs)
	}

	// After removing every Δ-renamed occurrence, a bare `Sort(` must remain — the dot-imported
	// production call keeps its unqualified emission (bound through `using static`).
	if !strings.Contains(strings.ReplaceAll(valueCs, ShadowVarMarker+"Sort", ""), "Sort(") {
		t.Fatalf("the dot-imported production call must keep its bare emission:\n%s", valueCs)
	}

	if strings.Contains(valueCs, ShadowVarMarker+"Stable") {
		t.Fatalf("a same-named method whose production function is never referenced unqualified must keep its plain name:\n%s", valueCs)
	}

	qualifiedCs := readConvertedTestFile(t, outputPath, "qualified_test.cs")

	if strings.Contains(qualifiedCs, ShadowVarMarker+"Reverse") {
		t.Fatalf("a QUALIFIED production reference must not trigger the rename (Sel exclusion):\n%s", qualifiedCs)
	}
}

// Receiver/first-parameter guard: a `_test.go` FREE FUNCTION whose emitted C# signature matches a
// production METHOD's — because the method's receiver becomes the extension method's leading `this`
// parameter — must Δ-rename the TEST-side declarator. Go keeps method names and package-scope
// function names in separate namespaces, so math/big's `func (z nat) norm() nat` (nat.go) and
// `func norm(x nat) nat` (int_test.go) coexist legally, but both emit as `norm(nat)` and `this` does
// not participate in C# signature identity (CS0111 `big_package` already defines `norm`).
// Discrimination: a same-named free function whose parameters do NOT line up with the receiver
// (different type, or an extra parameter) emits distinctly and must keep its plain name.
func TestTestVariantRenamesTestFuncCollidingWithProductionMethodReceiver(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod": "module example/limbs\n\ngo 1.23\n",
		"value.go": "package limbs\n\ntype nat []uint\n\n" +
			"func (z nat) norm() nat { return z }\n\n" +
			"func (z nat) trim() nat { return z }\n\n" +
			"func (z nat) keep() nat { return z }\n",
		"value_test.go": "package limbs\n\n" +
			// Collides: receiver nat + no params vs one nat param.
			"func norm(x nat) nat { return x.norm() }\n\n" +
			// Does NOT collide: the free function takes an extra parameter.
			"func trim(x nat, n int) nat { return x.trim() }\n\n" +
			// Does NOT collide: the free function's first parameter is a different type.
			"func keep(n int) nat { return nil }\n\n" +
			"func probe(x nat) nat { return norm(trim(keep(0), 1)) }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}
	internal, _ := loadBothTestVariantsForDir(t, dir)
	if internal == nil {
		t.Fatal("internal test variant was not loaded")
	}

	outputPath := t.TempDir()
	testMethodRenames = make(map[types.Object]bool)
	t.Cleanup(func() { testMethodRenames = nil })

	options := Options{indentSpaces: 4, preferVarDecl: true, useChannelOperators: true}
	if _, _, err := convertTestVariant(internal, testFileEntries(internal), outputPath, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	valueCs := readConvertedTestFile(t, outputPath, "value_test.cs")

	if !strings.Contains(valueCs, ShadowVarMarker+"norm(") {
		t.Fatalf("the colliding test-side FREE FUNCTION declarator must be Δ-renamed:\n%s", valueCs)
	}
	if !strings.Contains(valueCs, ShadowVarMarker+"norm(trim(") {
		t.Fatalf("the test function's call sites must follow the Δ-rename:\n%s", valueCs)
	}

	// The production method keeps its bare name — it is pinned, and only the free function moved.
	if !strings.Contains(valueCs, ".norm()") {
		t.Fatalf("the production METHOD must keep its bare name at the call site:\n%s", valueCs)
	}

	if strings.Contains(valueCs, ShadowVarMarker+"trim") {
		t.Fatalf("an extra-parameter free function does not collide and must keep its plain name:\n%s", valueCs)
	}
	if strings.Contains(valueCs, ShadowVarMarker+"keep") {
		t.Fatalf("a different-first-parameter free function does not collide and must keep its plain name:\n%s", valueCs)
	}
}

// G4 guard (production-pinned LIFTED TYPE names): a `_test.go` anonymous struct lifts to a nested
// type of the SAME `<pkg>_package` class the production `.cs` on disk already declare into, and
// those files are not regenerated by a `-tests` run — so their lifted names are immutable and the
// test-side lift must step around them. While the uniquing set was per-FILE, encoding/gob's
// encoder_test.cs re-claimed type.cs's `Δtype`/`Δtypeᴛ1` and the class declared each twice: CS0579
// on the doubled `[GoType]` attribute plus CS0111/CS0557 on every member go2cs-gen generated for
// the duplicate definition (32 errors). Both directions are pinned here — UNSEEDED the test lift
// legitimately takes the base name (its own class is the only scope), SEEDED it must not.
func TestTestVariantPinsProductionLiftedTypeNames(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod": "module example/lifted\n\ngo 1.23\n",
		// Production: a package-level anonymous struct with no name of its own — lifted under the
		// generic `type` fallback, exactly like gob's `(*struct{ r7 int })(nil)` bootstrap types.
		"value.go": "package lifted\n\nvar reserved = (*struct{ r7 int })(nil)\n\n" +
			"func Reserved() bool { return reserved == nil }\n",
		// A DIFFERENTLY shaped anonymous struct in the internal test variant, reaching for the
		// same fallback name.
		"value_test.go": "package lifted\n\nvar probeValue = (*struct{ a int })(nil)\n\n" +
			"func probe() bool { return probeValue == nil }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}

	internal, _ := loadBothTestVariantsForDir(t, dir)
	if internal == nil {
		t.Fatal("internal test variant was not loaded")
	}

	options := Options{indentSpaces: 4, preferVarDecl: true, useChannelOperators: true}
	baseName := getCoreSanitizedIdentifier("type")
	steppedName := baseName + TempVarMarker + "1"

	// Unseeded (what an EXTERNAL variant gets — its own `<pkg>_test_package` class is a separate
	// scope): the lift takes the base name. This half is what makes the seeded half meaningful.
	unseeded := t.TempDir()
	if _, _, err := convertTestVariant(internal, testFileEntries(internal), unseeded, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	unseededCs := readConvertedTestFile(t, unseeded, "value_test.cs")

	if !strings.Contains(unseededCs, "partial struct "+baseName+" {") {
		t.Fatalf("an unseeded lift must claim the base name %q:\n%s", baseName, unseededCs)
	}

	// Seeded with the production conversion's claims (what convertTestVariants hands the INTERNAL
	// variant): the very same lift must move off the pinned name.
	seeded := t.TempDir()
	seed := NewHashSet([]string{baseName})

	if _, _, err := convertTestVariant(internal, testFileEntries(internal), seeded, "go", seed, nil, options); err != nil {
		t.Fatal(err)
	}

	seededCs := readConvertedTestFile(t, seeded, "value_test.cs")

	if strings.Contains(seededCs, "partial struct "+baseName+" {") {
		t.Fatalf("a test-side lift must not re-declare the production-pinned name %q:\n%s", baseName, seededCs)
	}
	if !strings.Contains(seededCs, "partial struct "+steppedName+" {") {
		t.Fatalf("the test-side lift must step to %q:\n%s", steppedName, seededCs)
	}
}

// G5 guard (a name BOTH test-variant classes declare emits class-qualified): the merged test
// metadata carries a `using static` for the package under test AND for the external `<pkg>_test`
// class, so a bare reference to a name both declare cannot bind — CS0104 on the
// `[assembly: GoImplement<…>]` arguments (encoding/gob declares Point and Vector in codec_test.go
// and again in example_encdec_test.go / example_interface_test.go: 3 errors that blocked the whole
// package build). The reference is qualified with the class the metadata FILE anchors to.
func TestAmbiguousVariantTypeNamesAreClassQualified(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/ambig\n\ngo 1.23\n",
		"value.go": "package ambig\n\ntype Squarer interface{ Square() int }\n\nfunc Probe(s Squarer) int { return s.Square() }\n",
		// The package's own Point is declared by an INTERNAL test file, exactly as gob's is
		// (codec_test.go) — it lands in the production package class all the same.
		"value_test.go": "package ambig\n\ntype Point struct{ X, Y int }\n\n" +
			"func (p Point) Square() int { return p.X*p.X + p.Y*p.Y }\n\n" +
			"func internalProbe() int { var s Squarer = Point{1, 2}; return s.Square() }\n",
		// The EXTERNAL suite declares its OWN Point — the same simple name, a different type.
		"example_test.go": "package ambig_test\n\nimport \"example/ambig\"\n\n" +
			"type Point struct{ X, Y int }\n\n" +
			"type Pythagoras interface{ Hypot() int }\n\n" +
			"func (p Point) Hypot() int { return p.X + p.Y }\n\n" +
			"func externalProbe() int { var h Pythagoras = Point{1, 2}; return h.Hypot() + ambig.Probe(nil) }\n",
	}
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0644); err != nil {
			t.Fatal(err)
		}
	}

	internal, external := loadBothTestVariantsForDir(t, dir)
	if internal == nil || external == nil {
		t.Fatal("both test variants must load")
	}

	ambiguous := ambiguousVariantTypeNames(internal, external)

	if !ambiguous.Contains("Point") {
		t.Fatalf("a type name declared by BOTH variants must be recorded: %v", ambiguous.Keys())
	}
	for _, unique := range []string{"Squarer", "Pythagoras"} {
		if ambiguous.Contains(unique) {
			t.Fatalf("a type name declared by only ONE variant must stay bare: %q in %v", unique, ambiguous.Keys())
		}
	}

	testAmbiguousLocalTypeNames = ambiguous
	t.Cleanup(func() { testAmbiguousLocalTypeNames = nil })

	productionAnchor := metadataClassPrefix("go", "ambig")
	testAnchor := metadataClassPrefix("go", "ambig_test")

	// package_test_info.cs anchors to the PRODUCTION class; package_info_external_test.cs to the
	// external test class. The same bare record therefore renders differently per file — which is
	// the whole point: each names the type its own file's first class declares.
	if got, want := qualifyAmbiguousTestTypeRefs("[assembly: GoImplement<Point, Squarer>]", productionAnchor),
		"[assembly: GoImplement<go.ambig_package.Point, Squarer>]"; got != want {
		t.Fatalf("production-anchored render:\n got %s\nwant %s", got, want)
	}
	if got, want := qualifyAmbiguousTestTypeRefs("[assembly: GoImplement<Point, Pythagoras>]", testAnchor),
		"[assembly: GoImplement<go.ambig_test_package.Point, Pythagoras>]"; got != want {
		t.Fatalf("test-anchored render:\n got %s\nwant %s", got, want)
	}

	// An already-qualified reference names its class and is left alone; so is every conversion
	// outside -tests, where the name set is empty.
	qualified := "[assembly: GoImplement<go.ambig_package.Point, Squarer>]"
	if got := qualifyAmbiguousTestTypeRefs(qualified, testAnchor); got != qualified {
		t.Fatalf("an already-qualified reference must not be re-qualified: %s", got)
	}

	testAmbiguousLocalTypeNames = nil
	bare := "[assembly: GoImplement<Point, Squarer>]"
	if got := qualifyAmbiguousTestTypeRefs(bare, productionAnchor); got != bare {
		t.Fatalf("outside -tests nothing may change: %s", got)
	}
}

// TestIsSelfProjectReference pins the base-name comparison: a raw suffix test dropped any
// dependency whose project file name merely ends with the target's — converting "time" lost
// its runtime reference because "runtime.csproj" ends with "time.csproj" (B7).
//
// BOTH separators are cases, and that is the point. Emission is forward-slash since F5, but a
// reference still arrives backslashed from a pre-F5 corpus, a deployed tree or a hand-authored
// project — and filepath.Base off Windows does not split on a backslash, so on Linux and macOS
// every `\` case returned the whole string and matched nothing.
func TestIsSelfProjectReference(t *testing.T) {
	cases := []struct {
		reference   string
		projectName string
		want        bool
	}{
		{`$(go2csPath)core/time/time.csproj`, "time", true},
		{`$(go2csPath)core/runtime/runtime.csproj`, "time", false},
		{`$(go2csPath)core/runtime/internal/math/runtime.internal.math.csproj`, "math", false},
		{`$(go2csPath)core/math/math.csproj`, "math", true},
		{`$(go2csPath)core/time/TIME.CSPROJ`, "time", true},
		{`$(go2csPath)core\time\time.csproj`, "time", true},
		{`$(go2csPath)core\runtime\runtime.csproj`, "time", false},
		{`$(go2csPath)core\runtime\internal\math\runtime.internal.math.csproj`, "math", false},
		{`$(go2csPath)core\math\math.csproj`, "math", true},
		{`$(go2csPath)core\time\TIME.CSPROJ`, "time", true},
	}

	for _, c := range cases {
		if got := isSelfProjectReference(c.reference, c.projectName); got != c.want {
			t.Errorf("isSelfProjectReference(%q, %q) = %v, want %v", c.reference, c.projectName, got, c.want)
		}
	}
}

// TypeAccessibility guard: writePackageInfoFile INSERTS the section (prose + markers) into the
// FIRST package class's body when the file predates it, renders the live packageEmittedTypeAccess
// entries there sorted and indented, and — under mergeExisting — preserves the entries already
// present. The insertion path is what lets a tree converted by an older go2cs (and the -tests seed
// files, which compose their own contents rather than using package_info-template.txt) pick the
// section up without a migration step; the merge path is what carries a seeded production
// package_info.cs's entries through each test variant's additions.
func TestWritePackageInfoFileInsertsTypeAccessibilitySection(t *testing.T) {
	dir := t.TempDir()
	fileName := filepath.Join(dir, PackageInfoFileName)

	// A package info file with NO TypeAccessibility section — the pre-2026-07-25 shape.
	seed := strings.Join([]string{
		"namespace go;",
		"",
		"// <ImportedTypeAliases>",
		"// </ImportedTypeAliases>",
		"",
		"// <ExportedTypeAliases>",
		"// </ExportedTypeAliases>",
		"",
		"// <InterfaceImplementations>",
		"// </InterfaceImplementations>",
		"",
		"// <ImplicitConversions>",
		"// </ImplicitConversions>",
		"",
		"[GoPackage(\"value\")]",
		"public static partial class value_package",
		"{",
		"}",
	}, "\r\n")

	if err := os.WriteFile(fileName, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	packageName = "value"
	packageNamespace = "go"
	packageEmittedTypeAccess.Add("public partial interface Closer {}")
	packageEmittedTypeAccess.Add("internal partial struct dirEntry {}")

	writePackageInfoFile(fileName, false)

	data, err := os.ReadFile(fileName)
	if err != nil {
		t.Fatal(err)
	}

	contents := string(data)

	for _, want := range []string{
		"// <" + TypeAccessibilitySection + ">",
		"// </" + TypeAccessibilitySection + ">",
		typeAccessibilityIndent + "internal partial struct dirEntry {}",
		typeAccessibilityIndent + "public partial interface Closer {}",
	} {
		if !strings.Contains(contents, want) {
			t.Fatalf("emitted package info must contain %q:\n%s", want, contents)
		}
	}

	// The section — and its entries — must land INSIDE the package class body, since the types
	// they declare are nested in that class. Anything after the class's closing brace would
	// declare a namespace-scoped type of the same simple name instead.
	classIndex := strings.Index(contents, "public static partial class value_package")
	sectionIndex := strings.Index(contents, "// <"+TypeAccessibilitySection+">")
	closeIndex := strings.LastIndex(contents, "\r\n}")

	if classIndex < 0 || sectionIndex < classIndex || sectionIndex > closeIndex {
		t.Fatalf("TypeAccessibility section must sit inside the package class body:\n%s", contents)
	}

	// Entries are sorted on the rendered line, so `internal …` precedes `public …`. Compare the
	// INDENTED forms: the section's own prose quotes both spellings, so a bare substring search
	// would find the explanatory text rather than the entries.
	if strings.Index(contents, typeAccessibilityIndent+"internal partial struct dirEntry {}") > strings.Index(contents, typeAccessibilityIndent+"public partial interface Closer {}") {
		t.Fatal("TypeAccessibility entries must be sorted")
	}

	// Re-writing with mergeExisting must preserve what is already there and add the new entry
	// exactly once — the -tests seeded-file path.
	resetPackageState(&packages.Package{})
	packageName = "value"
	packageNamespace = "go"
	packageEmittedTypeAccess.Add("internal partial class dequeueNil {}")
	packageEmittedTypeAccess.Add("public partial interface Closer {}")

	writePackageInfoFile(fileName, true)

	if data, err = os.ReadFile(fileName); err != nil {
		t.Fatal(err)
	}

	contents = string(data)

	if !strings.Contains(contents, typeAccessibilityIndent+"internal partial struct dirEntry {}") {
		t.Fatalf("merge must preserve an existing entry:\n%s", contents)
	}
	if !strings.Contains(contents, typeAccessibilityIndent+"internal partial class dequeueNil {}") {
		t.Fatalf("merge must add the new entry:\n%s", contents)
	}
	if got := strings.Count(contents, typeAccessibilityIndent+"public partial interface Closer {}"); got != 1 {
		t.Fatalf("merge must not duplicate an entry, got %d occurrences:\n%s", got, contents)
	}
	if got := strings.Count(contents, "// <"+TypeAccessibilitySection+">"); got != 1 {
		t.Fatalf("the section must not be inserted twice, got %d:\n%s", got, contents)
	}
}

// Movable-attribute guard: recordTypeAccessibility relocates a declaration's movable stamps onto
// the `<TypeAccessibility>` record and hands the caller back an EMPTY inline prefix — and, on the
// two paths where no record is written, hands the stamps back verbatim so the declaration keeps
// them. The attributes and the access modifier must travel together: the record is the only
// declaration that survives to carry either, so a stamp left behind when the record is skipped
// would be lost, and one relocated onto a record that is never written would be lost too.
func TestRecordTypeAccessibilityRelocatesMovableAttributes(t *testing.T) {
	const attrs = "[GoLocalName(\"span\")] [GoValueClone(\"buf\")] "

	// The recording path: the stamps land on the record, the declaration is left bare.
	resetPackageState(&packages.Package{})

	v := &Visitor{}

	if got := v.recordTypeAccessibility("struct", "holder", "", "", attrs); got != "" {
		t.Fatalf("a recorded type must emit no inline attributes, got %q", got)
	}

	want := attrs + "internal partial struct holder {}"

	if !packageEmittedTypeAccess.Contains(want) {
		t.Fatalf("record must carry the attributes, got %v", packageEmittedTypeAccess.Keys())
	}

	// A HAND-OWNED file gets no record (its compiled declarations are the author's), so its
	// `.cs.auto` review sibling must keep the stamps where they have always been.
	resetPackageState(&packages.Package{})

	v = &Visitor{manualConversion: true}

	if got := v.recordTypeAccessibility("struct", "holder", "", "", attrs); got != attrs {
		t.Fatalf("a hand-owned conversion must keep its attributes inline, got %q", got)
	}

	if len(packageEmittedTypeAccess.Keys()) != 0 {
		t.Fatalf("a hand-owned conversion must record nothing, got %v", packageEmittedTypeAccess.Keys())
	}

	// A -tests bridge unit writes its accessibility inline for its own reason (its metadata anchor
	// can be a different test class); the attributes follow the same route.
	resetPackageState(&packages.Package{})

	v = &Visitor{}
	v.options.testInlineTypeAccess = true

	if got := v.recordTypeAccessibility("struct", "holder", "", "", attrs); got != attrs {
		t.Fatalf("a bridge unit must keep its attributes inline, got %q", got)
	}

	if len(packageEmittedTypeAccess.Keys()) != 0 {
		t.Fatalf("a bridge unit must record nothing, got %v", packageEmittedTypeAccess.Keys())
	}
}

// Sort guard: the `<TypeAccessibility>` section orders entries by the DECLARATION, so a stamped
// type sits with its accessibility/kind/name peers. Sorting the raw line instead would let the
// leading '[' pull every stamped entry into a block of its own ahead of the unstamped ones —
// legal, but it scrambles a section whose whole value is being readable at a glance.
func TestTypeAccessibilitySortIgnoresAttributePrefix(t *testing.T) {
	for _, c := range []struct {
		line string
		want string
	}{
		{"internal partial struct row {}", "internal partial struct row {}"},
		{"[GoValueClone(\"b\")] internal partial struct inner {}", "internal partial struct inner {}"},
		{"[GoLocalName(\"span\")] [GoValueClone(\"buf\")] public partial struct holder {}", "public partial struct holder {}"},
		// A bracket inside a quoted argument is argument text, not structure.
		{"[GoLocalName(\"a]b\")] internal partial struct odd {}", "internal partial struct odd {}"},
		// An unterminated group has no declaration to find; the line stands as its own key rather
		// than being truncated to nothing.
		{"[GoValueClone(\"x\" internal partial struct broken {}", "[GoValueClone(\"x\" internal partial struct broken {}"},
	} {
		if got := typeAccessibilityKey(c.line); got != c.want {
			t.Errorf("typeAccessibilityKey(%q) = %q, want %q", c.line, got, c.want)
		}
	}

	// End to end: a stamped entry keeps the place its declaration earns, not the one its '[' would.
	dir := t.TempDir()
	fileName := filepath.Join(dir, PackageInfoFileName)

	seed := strings.Join([]string{
		"namespace go;",
		"",
		"// <ImportedTypeAliases>",
		"// </ImportedTypeAliases>",
		"",
		"// <ExportedTypeAliases>",
		"// </ExportedTypeAliases>",
		"",
		"// <InterfaceImplementations>",
		"// </InterfaceImplementations>",
		"",
		"// <ImplicitConversions>",
		"// </ImplicitConversions>",
		"",
		"[GoPackage(\"value\")]",
		"public static partial class value_package",
		"{",
		"}",
	}, "\r\n")

	if err := os.WriteFile(fileName, []byte(seed), 0644); err != nil {
		t.Fatal(err)
	}

	resetPackageState(&packages.Package{})
	packageName = "value"
	packageNamespace = "go"
	packageEmittedTypeAccess.Add("internal partial struct alpha {}")
	packageEmittedTypeAccess.Add("[GoValueClone(\"b\")] internal partial struct beta {}")
	packageEmittedTypeAccess.Add("internal partial struct gamma {}")

	writePackageInfoFile(fileName, false)

	data, err := os.ReadFile(fileName)
	if err != nil {
		t.Fatal(err)
	}

	contents := string(data)

	alpha := strings.Index(contents, typeAccessibilityIndent+"internal partial struct alpha {}")
	beta := strings.Index(contents, typeAccessibilityIndent+"[GoValueClone(\"b\")] internal partial struct beta {}")
	gamma := strings.Index(contents, typeAccessibilityIndent+"internal partial struct gamma {}")

	if alpha < 0 || beta < 0 || gamma < 0 {
		t.Fatalf("every entry must be rendered:\n%s", contents)
	}

	if !(alpha < beta && beta < gamma) {
		t.Fatalf("a stamped entry must sort with its peers, got alpha=%d beta=%d gamma=%d:\n%s", alpha, beta, gamma, contents)
	}
}

// generatedTypeScope MUST agree with go2cs-gen's Common.GetScope — the two decide the access
// modifier of two partial declarations of ONE type, and C# rejects conflicting accessibility
// (CS0262). GetScope reads the C# identifier VERBATIM, so the Δ collision-rename prefix (a Greek
// capital) reads as exported and a C#-keyword escape (`@decimal`) reads as unexported; getAccess
// strips both first and would disagree on each.
func TestGeneratedTypeScopeMirrorsGeneratorRule(t *testing.T) {
	cases := []struct {
		identifier string
		want       string
	}{
		{"Closer", "public"},
		{"dirEntry", "internal"},
		{"_", "public"},
		{"_func", "internal"},
		{"@decimal", "internal"},
		{ShadowVarMarker + "Month", "public"},
		{ShadowVarMarker + "guintptr", "public"},
		{PointerPrefix + "Elem", "internal"},
		{"", "internal"},
	}

	for _, c := range cases {
		if got := generatedTypeScope(c.identifier); got != c.want {
			t.Errorf("generatedTypeScope(%q) = %q, want %q", c.identifier, got, c.want)
		}
	}
}

// The bridge class HIDES a same-named production member from the `using static <pkg>_package` it
// binds production through — C# member lookup stops at the first enclosing type carrying the name.
// container/heap is the corpus instance: heap_test.go declares `func (h *myHeap) Pop() any`, which
// hid `heap_package.Pop(Interface)` so every `Pop(h)` in the suite bound the extension by value
// (CS1620 x8). The collection must therefore reach METHODS (they emit as static extension members
// of the same class), not just package-level declarations, and must not reach production names or
// function-LOCAL declarations, which shadow nothing at class scope.
func TestWhiteboxBridgeDeclaredNamesCoverMethodsAndPackageDecls(t *testing.T) {
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/bridgenames\n\ngo 1.23\n",
		"heapish.go": "package bridgenames\n" +
			"type Iface interface{ Len() int }\n" +
			"func Pop(h Iface) int { return h.Len() }\n" +
			"func Production() int { return 1 }\n",
		"heapish_test.go": "package bridgenames\n" +
			"import \"testing\"\n" +
			"type myHeap []int\n" +
			"func (h *myHeap) Len() int { return len(*h) }\n" +
			"func (h *myHeap) Pop() int { return 0 }\n" +
			"var seed = 3\n" +
			"const limit = 4\n" +
			"func TestUse(t *testing.T) {\n" +
			"\tlocalOnly := 1\n" +
			"\t_ = localOnly\n" +
			"\th := &myHeap{}\n" +
			"\t_ = Pop(h)\n" +
			"\t_ = Production()\n" +
			"\t_ = seed + limit\n" +
			"}\n",
	})

	internal, _ := loadTestVariantsForDir(t, dir)
	if internal == nil {
		t.Fatal("the internal test variant was not loaded")
	}

	names := collectWhiteboxBridgeDeclaredNames(internal)

	for _, want := range []string{"Pop", "Len", "myHeap", "seed", "limit", "TestUse"} {
		if !names.Contains(want) {
			t.Errorf("the bridge declares %q, so it hides a same-named production member; got %v", want, names.Keys())
		}
	}

	// Production declarations are not bridge members, and a function-local name shadows nothing
	// at class scope — either would qualify a reference that needs no qualification.
	for _, unwanted := range []string{"Production", "Iface", "localOnly"} {
		if names.Contains(unwanted) {
			t.Errorf("%q is not a bridge-class member; got %v", unwanted, names.Keys())
		}
	}
}

// A box-field accessor (`Type.Ꮡfield`, used by `receiver.of(Type.Ꮡfield)`) qualifies its owner
// type with a package static class whenever a bare name could be shadowed — and under the
// white-box test model that class must be the BRIDGE, not the production package, for a type an
// internal `_test.go` declares. database/sql's `fakedb_test.go` is the corpus instance:
// `type table struct { mu sync.Mutex; … }` sits beside `func (db *fakeDB) table(string)`, so the
// TYPE is Δ-renamed and therefore always qualified — and all six `t.mu.Lock()` sites spelled
// `sql_package.Δtable.Ꮡmu`, a class with no such member (CS0117 ×6).
//
// Both directions are asserted: the bridge class appears, and the production class does not.
func TestTestVariantBoxAccessorNamesBridgeDeclaringClass(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod":   "module example/boxacc\n\ngo 1.23\n",
		"value.go": "package boxacc\n\ntype Holder struct{ n int }\n\nfunc NewHolder(n int) *Holder { return &Holder{n: n} }\n",
		// `probe` the TYPE collides with `probe` the METHOD, which Δ-renames the type — exactly
		// fakedb_test.go's `table`/`(*fakeDB).table` pairing.
		"export_test.go": "package boxacc\n\ntype probe struct{ n int }\n\n" +
			"func (h *Holder) probe() int { return h.n }\n\n" +
			"func probeAddr(p *probe) *int { return &p.n }\n",
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
	// What convertTestVariants sets for the internal variant under the white-box model.
	options.testClassNameOverride = getSanitizedImport("boxacc_internal_test" + PackageSuffix)

	testMethodRenames = make(map[types.Object]bool)
	t.Cleanup(func() { testMethodRenames = nil })

	if _, _, err := convertTestVariant(internal, testFileEntries(internal), outputPath, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	exportCs := readConvertedTestFile(t, outputPath, "export_test.cs")

	accessor := ShadowVarMarker + "probe." + AddressPrefix + "n"

	if !strings.Contains(exportCs, "boxacc_internal_test"+PackageSuffix+"."+accessor) {
		t.Fatalf("the box accessor must name the BRIDGE class that declares the test type:\n%s", exportCs)
	}
	if strings.Contains(exportCs, "boxacc"+PackageSuffix+"."+accessor) {
		t.Fatalf("the production class does not declare a test-file type — it must not be the qualifier:\n%s", exportCs)
	}
}

// A type declared INSIDE a function body has no Go exportedness. The export convention governs
// PACKAGE-LEVEL identifiers, so a function-local `S8` is exactly as unreachable from outside its
// function as `embed2` is — Go draws no distinction between them. go2cs hoists both to package scope
// as `<Func>_<name>`, and deriving an access modifier from a name at that point invents a split Go
// never had, by either of two routes: the bridge arm reads the LOCAL name (so siblings of one
// function land on opposite sides), and a lifted ANONYMOUS struct carries no modifier at all, so
// go2cs-gen's own rule reads the HOISTED name and inherits the case of the enclosing function.
//
// C#'s accessibility-consistency rule then rejects the mixture, and it was the ENTIRE compile wall of
// encoding/json's suite: TestUnmarshalEmbeddedUnexported makes `embed2` a field of `S8`, and a public
// S8 over an internal embed2 is CS0053 — 76 errors across CS0050/51/52/53, four codes, one cause.
// All three shapes are pinned here: the uppercase local, the lowercase local, and the anonymous lift
// (whose fields reach a package-level unexported production type — the CS0052 member of the family).
func TestFunctionLocalTypesShareOneAccessibility(t *testing.T) {
	dir := t.TempDir()
	files := map[string]string{
		"go.mod": "module example/localaccess\n\ngo 1.23\n",
		// A package-level unexported PRODUCTION type — emitted `internal`, and the operand that
		// makes an anonymous lift's exported field CS0052 when the lift is scoped from its name.
		"value.go": "package localaccess\n\ntype hidden struct{ N int }\n",
		"value_test.go": "package localaccess\n\nimport \"testing\"\n\n" +
			"func TestLocals(t *testing.T) {\n" +
			"\ttype embed2 struct{ Q int }\n" +
			"\ttype S8 struct {\n\t\tembed2\n\t\tR int\n\t}\n" +
			"\tanon := struct{ H hidden }{}\n" +
			"\t_ = S8{}\n" +
			"\t_ = anon\n" +
			"}\n",
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
	// What convertTestVariants sets for the internal variant under the white-box model — the arm
	// that writes accessibility inline, and therefore the one this rule lives in.
	options.testClassNameOverride = getSanitizedImport("localaccess_internal_test" + PackageSuffix)
	options.testInlineTypeAccess = true

	testMethodRenames = make(map[types.Object]bool)
	t.Cleanup(func() { testMethodRenames = nil })

	if _, _, err := convertTestVariant(internal, testFileEntries(internal), outputPath, "go", nil, nil, options); err != nil {
		t.Fatal(err)
	}

	valueCs := readConvertedTestFile(t, outputPath, "value_test.cs")

	// Both named locals are internal — the uppercase one is the half that used to read `public`.
	for _, want := range []string{
		"internal partial struct TestLocals_S8",
		"internal partial struct TestLocals_embed2",
	} {
		if !strings.Contains(valueCs, want) {
			t.Errorf("a function-local type has no Go exportedness to read a modifier from; want %q:\n%s", want, valueCs)
		}
	}

	// No local type is public, whatever the case of its Go name.
	if strings.Contains(valueCs, "public partial struct TestLocals_") {
		t.Errorf("no function-local type may be public — its Go name's case carries no export meaning:\n%s", valueCs)
	}

	// And none is left BARE: a declaration with nothing between `]` and `partial` hands the decision
	// to go2cs-gen, which scopes it from the hoisted name and lands back on public.
	if strings.Contains(valueCs, "] partial struct TestLocals_") {
		t.Errorf("a bare local declaration lets the generator scope it from the hoisted name:\n%s", valueCs)
	}
}
