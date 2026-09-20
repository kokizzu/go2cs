// testOnlyPackageEmission_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).
//
// Guards what a TEST-ONLY package's `-tests` emission may NAME.
//
// MEASURED at Go 1.24.13 on crypto/internal/fips140test — thirteen files, every one a `_test.go`,
// directory `fips140test`, package clause `fipstest`. Its production half converts nothing:
// conversionDriver reaches `unmarkedFileCount == 0` with an empty file list and reports
// "Skipping conversion: no target Go source files found", so no `fipstest_package` class and no
// production `.csproj` are emitted. The TEST half named that class anyway — a file-scoped
// `using static` in each of the twelve emitted test files, plus package_test_info.cs's own
// `global using static` and the `initPackage(typeof(...))` forcing hook — and the tests project
// failed to build with thirteen of:
//
//	package_test_info.cs(7,49): error CS0234: The type or namespace name 'fipstest_package' does
//	not exist in the namespace 'go.crypto.@internal' (are you missing an assembly reference?)
//
// The two arms are ONE measurement with its own control. They vary a single axis — whether the
// package has a production file — and select the same project model, which the runner asserts
// rather than assumes. The CONTROL is what keeps the refusal arm from passing vacuously: an
// emission that produced nothing, or that spelled the production class differently than this test
// looks for, would satisfy "no line names it" for the wrong reason and fails in the control
// instead.

package main

import (
	"os"
	"path/filepath"
	"runtime"
	"sort"
	"strings"
	"testing"

	"golang.org/x/tools/go/packages"
)

// The fixture's package DIRECTORY and its package CLAUSE differ deliberately: fips140test/fipstest
// does, the emitted class is named from the package clause, and a fixture whose two spellings
// agreed could not tell which one the emission used.
const (
	testOnlyFixtureDir     = "shapetest"
	testOnlyFixturePackage = "shapes"
)

// writeTestOnlyFixture writes the fixture module and returns its package directory.
//
// withProduction is the ONE axis. Without it every file in the package is a `_test.go` — the
// fips140test shape. With it the same `area` moves into a production file the test calls, so a
// production class exists and both directives are load-bearing rather than decorative.
func writeTestOnlyFixture(t *testing.T, withProduction bool) string {
	t.Helper()

	dir := t.TempDir()

	testSource := "package " + testOnlyFixturePackage + "\n\n" +
		"import \"testing\"\n\n" +
		"func TestArea(t *testing.T) {\n" +
		"\tif area(2, 3) != 6 {\n" +
		"\t\tt.Fatal(\"area\")\n" +
		"\t}\n" +
		"}\n"

	files := map[string]string{"go.mod": "module example/toponly\n\ngo 1.23\n"}

	if withProduction {
		files[testOnlyFixtureDir+"/shape.go"] = "package " + testOnlyFixturePackage + "\n\n" +
			"func area(w, h int) int { return w * h }\n"
	} else {
		testSource += "\nfunc area(w, h int) int { return w * h }\n"
	}

	files[testOnlyFixtureDir+"/shape_test.go"] = testSource

	writeModuleFiles(t, dir, files)

	return filepath.Join(dir, testOnlyFixtureDir)
}

// convertTestOnlyFixture runs the REAL `-tests` wiring over the fixture — the same Tests:true load,
// production/variant discovery, model selection and convertTestVariants call processTestConversion
// makes — and returns every emitted `.cs` keyed by base name. Going through the production path is
// what makes this a guard on the WIRING and not on a hand-set option field.
func convertTestOnlyFixture(t *testing.T, inputPath string) map[string]string {
	t.Helper()

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: inputPath, Tests: true}, ".")
	if err != nil {
		t.Fatal(err)
	}

	production := findProductionPackage(loaded, inputPath)
	if production == nil {
		t.Fatal("production package was not loaded")
	}

	if production.Name != testOnlyFixturePackage {
		t.Fatalf("fixture package clause reads %q, not %q — the emitted class is named from it", production.Name, testOnlyFixturePackage)
	}

	internal, external := findTestVariants(loaded, production)
	if internal == nil {
		t.Fatal("the fixture must load an INTERNAL test variant — its suite is what selects the model both arms share")
	}
	if external != nil {
		t.Fatal("the fixture must load no EXTERNAL test variant, matching the fips140test shape")
	}

	// Asserted rather than assumed: the arms are comparable only while they select the same model,
	// and the production file the control adds is exactly the kind of edit that could move it.
	if model := selectTestProjectModel(internal, external); model != testProjectWhiteboxReference {
		t.Fatalf("fixture model = %v, want whitebox-reference", model)
	}

	outputPath := t.TempDir()

	resetPackageState(&packages.Package{})
	packageNamespace = "go"

	options := Options{
		indentSpaces:        4,
		preferVarDecl:       true,
		useChannelOperators: true,
		convertTests:        true,
		targetPlatform:      runtime.GOOS + "/" + runtime.GOARCH,
	}

	// The self-import binding processTestConversion establishes before handing off.
	options.testPackagePath = production.PkgPath
	options.testPackageName = production.Name

	if _, err = convertTestVariants(testProjectWhiteboxReference, production, internal, external,
		selectCompileExcludedTestFiles(internal, external), inputPath, outputPath, "go",
		NewHashSet(supportedTestCapabilities()), options); err != nil {
		t.Fatalf("convertTestVariants: %v", err)
	}

	emitted, err := filepath.Glob(filepath.Join(outputPath, "*.cs"))
	if err != nil {
		t.Fatal(err)
	}

	if len(emitted) == 0 {
		t.Fatalf("no converted file was emitted into %s", outputPath)
	}

	files := make(map[string]string, len(emitted))

	for _, path := range emitted {
		data, readErr := os.ReadFile(path)
		if readErr != nil {
			t.Fatal(readErr)
		}
		files[filepath.Base(path)] = string(data)
	}

	return files
}

// productionClassLines returns every emitted line naming the production class, partitioned into the
// `using static` directives and the `initPackage(typeof(...))` hooks, plus all of them together.
// Reported as "<file>: <line>" so a failure reads as the CS0234 list it stands for rather than as a
// bare count.
//
// Matching on the class NAME rather than on a fully-spelled directive is deliberate: the namespace
// a fixture lands in and whether the reference is `global::`-rooted are both emission details this
// guard has no business pinning, while "no emitted line may name a class that does not exist" is
// the property itself. `shapes_internal_test_package` — the class the test files DO emit into —
// does not contain `shapes_package`, so the bridge is not a false positive.
func productionClassLines(files map[string]string) (usings, inits, all []string) {
	className := getSanitizedImport(testOnlyFixturePackage + PackageSuffix)

	names := make([]string, 0, len(files))
	for name := range files {
		names = append(names, name)
	}

	sort.Strings(names)

	for _, name := range names {
		for _, line := range strings.Split(files[name], "\n") {
			line = strings.TrimSpace(strings.TrimSuffix(line, "\r"))

			if !strings.Contains(line, className) {
				continue
			}

			record := name + ": " + line
			all = append(all, record)

			switch {
			case strings.Contains(line, "using static"):
				usings = append(usings, record)
			case strings.Contains(line, "initPackage(typeof("):
				inits = append(inits, record)
			}
		}
	}

	return usings, inits, all
}

// TestTestOnlyPackageNamesNoProductionClass is the fips140test refusal: every file a `_test.go`, so
// the production conversion emitted no class, so nothing may import or initialize one.
func TestTestOnlyPackageNamesNoProductionClass(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads and converts a test-variant fixture")
	}

	_, _, all := productionClassLines(convertTestOnlyFixture(t, writeTestOnlyFixture(t, false)))

	if len(all) > 0 {
		t.Fatalf("a TEST-ONLY package has no %s class for the test assembly to name — the production half emitted none — yet %d emitted line(s) name it (one CS0234 each):\n\t%s",
			getSanitizedImport(testOnlyFixturePackage+PackageSuffix), len(all), strings.Join(all, "\n\t"))
	}
}

// TestPackageWithProductionFileNamesTheProductionClass is the control for the refusal above: the
// same fixture WITH a production file must still emit both directives, so the refusal cannot be
// satisfied by an emission that names the class nowhere under any circumstances.
func TestPackageWithProductionFileNamesTheProductionClass(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads and converts a test-variant fixture")
	}

	className := getSanitizedImport(testOnlyFixturePackage + PackageSuffix)
	usings, inits, _ := productionClassLines(convertTestOnlyFixture(t, writeTestOnlyFixture(t, true)))

	if len(usings) == 0 {
		t.Errorf("a package WITH production files must import its %s class — the test files reference production declarations through it", className)
	}

	if len(inits) == 0 {
		t.Errorf("a package WITH production files must force its %s class's init — the referenced production assembly's module constructor runs only when touched", className)
	}
}
