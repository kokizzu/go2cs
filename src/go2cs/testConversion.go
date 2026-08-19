// testConversion.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"io/fs"
	"os"
	"os/exec"
	"path"
	"path/filepath"
	"regexp"
	"runtime"
	"runtime/debug"
	"sort"
	"strings"
	"time"
	"unicode"
	"unicode/utf8"

	"golang.org/x/tools/go/packages"
)

// Phase-4 test conversion: converts a package's _test.go variants (in-package and external
// package_test) into a runnable, self-registering C# test project driven by the hand-owned
// go.testing runtime (src/core/testing), plus a machine-readable manifest and a `go test -json`
// differential oracle. Ported from the codex/testing-infrastructure branch (097c94d70) onto the
// shared per-package helpers in packageStateOperations.go and the shared writePackageInfoFile —
// the branch's private copies of that machinery are gone by design (they drifted; see the port
// review in docs/phase4/BranchReview-codex-testing-infrastructure.md).

const (
	testPackageInfoFileName = "package_test_info.cs"
	testHostFileName        = "go2cs_test_host.cs"
	testManifestFileName    = "go2cs_test_manifest.json"

	// The package's HAND-OWNED disclosed-divergence manifest (see testDisclosure). Unlike the
	// go2cs_test_* artifacts above, this file is never generated: it is authored by hand,
	// committed beside the converted package, and reviewed like source.
	testDisclosureFileName = "go2cs_test_disclosures.json"

	// The EXTERNAL test package's metadata anchor (B4/B5) — the compilation unit hosting the
	// GoImplement/GoImplicitConv attributes whose generated adapters/partials must anchor to
	// the <name>_test package class. "external test package" is Go's own term for `package
	// <name>_test`, matching the vocabulary used throughout this file.
	//
	// ⚠ The `_test.cs` SUFFIX IS LOAD-BEARING — it is the exclusion mechanism: the production
	// csproj's committed `*_test.cs` Compile Remove and productionCSFiles both skip this file by
	// that glob alone, WITHOUT a shared-csproj-template edit (which would churn every behavioral
	// csproj on re-transpile). Any future rename must keep the suffix or pay that churn.
	//
	// Renamed from the original `package_info_test.cs` (2026-07-21): a near-anagram of
	// testPackageInfoFileName above, the two sorted adjacent to `package_info.cs` in every
	// converted package directory, and nothing in either name said which class it anchors to.
	externalTestPackageInfoFileName = "package_info_external_test.cs"
	internalTestPackageInfoFileName = "package_info_internal_test.cs"
)

// Markers substituted into test-csproj-template.xml by writeTestProject (embedded-resource
// template, following the csproj-template.xml precedent — never a hardcoded csproj string).
const (
	TestRootNamespaceMarker     = ">>MARKER:TEST_ROOT_NAMESPACE<<"
	TestAssemblyNameMarker      = ">>MARKER:TEST_ASSEMBLY_NAME<<"
	TestGo2CSRelativePathMarker = ">>MARKER:TEST_GO2CS_RELATIVE_PATH<<"
	TestCompileItemsMarker      = ">>MARKER:TEST_COMPILE_ITEMS<<"
	TestFixtureItemsMarker      = ">>MARKER:TEST_FIXTURE_ITEMS<<"
	TestProjectReferencesMarker = ">>MARKER:TEST_PROJECT_REFERENCES<<"
)

const unsupportedCapabilityReasonPrefix = "requires unsupported testing capabilities: "

// testProjectModel selects how the generated test project binds the PRODUCTION package.
type testProjectModel int

const (
	// testProjectRecompile compiles the production .cs INTO the test assembly alongside the
	// converted test sources (the original -tests model). Retained as a fallback when converted
	// test metadata would have to add operators to a closed production type.
	testProjectRecompile testProjectModel = iota

	// testProjectWhiteboxReference references the production project while internal test files
	// emit into a friend-assembly bridge class. Production remains the sole identity for its types.
	testProjectWhiteboxReference

	// testProjectReference references the colocated production csproj instead of recompiling
	// its sources, so the production ASSEMBLY stays the single identity for the production
	// types. A black-box (external-only) suite touches only the package's exported API, which
	// resolves cross-assembly exactly as it does for every other converted consumer — while a
	// recompile there DUPLICATES the production types: a referenced stdlib assembly whose API
	// mentions a production type (strings.ToLowerSpecial(unicode.SpecialCase, …)) names the
	// type in the PRODUCTION assembly, and the test assembly's recompiled copy is a distinct
	// type — CS0012 (unicode's letter_test). Applies to black-box-only packages
	// (unicode, unicode/utf8, path, …); mixed/internal suites use whitebox-reference.
	testProjectReference
)

func (m testProjectModel) String() string {
	switch m {
	case testProjectWhiteboxReference:
		return "whitebox-reference"
	case testProjectReference:
		return "reference"
	default:
		return "recompile"
	}
}

func (m testProjectModel) referencesProduction() bool {
	return m == testProjectReference || m == testProjectWhiteboxReference
}

// selectTestProjectModel references production for both suite shapes: black-box-only suites use
// the ordinary reference model; a suite with an internal variant uses the friend-assembly bridge.
// Either reference model can still fall back when converted records require a real mutation of a
// closed production type (errProductionAnchoredRecords — see processTestConversion).
func selectTestProjectModel(internal, external *packages.Package) testProjectModel {
	if internal != nil {
		return testProjectWhiteboxReference
	}
	if external != nil {
		return testProjectReference
	}

	return testProjectRecompile
}

// errProductionAnchoredRecords signals that a reference-model conversion attempt collected
// GoImplement/GoImplicitConv records whose GENERATED code must anchor to the production
// package class (a partial struct merged into a production type declaration, or conversion
// operators on one) — impossible across an assembly boundary, where the referenced production
// types are closed. The caller falls back to the recompile model, which reconverts with the
// production types local.
var errProductionAnchoredRecords = errors.New("test variant records production-anchored metadata")

// recordsRequireProductionAnchor reports whether the LIVE record globals — the just-converted
// external variant's collected records — contain any entry that must anchor to the production
// class, evaluated with the recompile-model partition predicates (isTestAnchoredImplementRecord /
// isTestAnchoredConversionRecord). Under the reference model nothing is seeded and
// testLocalTypePrefixes stays empty, so every production type renders package-qualified, and any
// record landing in the production partition is one whose generated partial/adapter/operator
// would need to merge with a production declaration. A record that renders a production type
// through its imported ꓸ type-alias form (`<pkg>ꓸ<Type>`, TypeAliasDot) is likewise treated as
// production-anchored — conservatively, since the partition predicates cannot see the production
// qualifier inside the alias identifier.
func recordsRequireProductionAnchor(productionClassName, productionPackageName string) bool {
	_, productionAnchored := splitExternalVariantRecords(productionClassName)

	if !productionAnchored.isEmpty() {
		return true
	}

	aliasPrefix := getSanitizedIdentifier(productionPackageName) + TypeAliasDot
	names := make([]string, 0)

	for ifaceName, implementations := range interfaceImplementations {
		names = append(names, ifaceName)
		names = append(names, implementations.Keys()...)
	}

	for ifaceName, implementations := range promotedInterfaceImplementations {
		names = append(names, ifaceName)
		names = append(names, implementations.Keys()...)
	}

	for _, proxy := range constraintProxies {
		names = append(names, proxy[0], proxy[1])
	}

	for _, conversions := range []map[string]HashSet[string]{implicitConversions, invertedImplicitConversions, indirectImplicitConversions} {
		for sourceType, targetTypes := range conversions {
			names = append(names, sourceType)
			names = append(names, targetTypes.Keys()...)
		}
	}

	for _, conversions := range []map[string]map[string]string{numericConversions, indirectNumericConversions} {
		for sourceType, targetTypes := range conversions {
			names = append(names, sourceType)

			for targetType := range targetTypes {
				names = append(names, targetType)
			}
		}
	}

	for _, name := range names {
		if strings.Contains(name, aliasPrefix) {
			return true
		}
	}

	return false
}

// recordsRequireProductionMutation reports records that a white-box reference project cannot
// relocate into its test-owned metadata anchor. Interface implementation records are relocatable:
// qualified production structs are foreign to the test compilation, so go2cs-gen emits value or
// pointer adapter classes in the test anchor instead of partial production structs. Structural
// conversions involving a production type still require a partial conversion operator on that
// closed type. Numeric conversions can relocate to the test-local operand, but not when both
// operands belong to production.
func recordsRequireProductionMutation(productionClassName, productionPackageName string) bool {
	aliasPrefix := getSanitizedIdentifier(productionPackageName) + TypeAliasDot
	shadowAliasPrefix := ShadowVarMarker + getSanitizedIdentifier(productionPackageName) + "."
	normalize := func(name string) string {
		return strings.TrimPrefix(name, "global::")
	}
	isProductionType := func(name string) bool {
		if trimmed, ok := strings.CutPrefix(name, PointerPrefix+"<"); ok {
			name = strings.TrimSuffix(trimmed, ">")
		}
		name = normalize(name)
		return strings.Contains(name, productionClassName+".") || strings.Contains(name, aliasPrefix) ||
			strings.Contains(name, shadowAliasPrefix)
	}

	for _, conversions := range []map[string]HashSet[string]{implicitConversions, invertedImplicitConversions} {
		for sourceType, targetTypes := range conversions {
			for targetType := range targetTypes {
				if isProductionType(sourceType) || isProductionType(targetType) {
					return true
				}
			}
		}
	}

	for sourceType, targetTypes := range indirectImplicitConversions {
		for targetType := range targetTypes {
			if inner, ok := strings.CutPrefix(targetType, PointerPrefix+"<"); ok && normalize(sourceType) == normalize(strings.TrimSuffix(inner, ">")) {
				// T -> ж<T> is the shared Go pointer-boxing route. The generator intentionally
				// emits no type-owned operator for a foreign T, so it does not mutate production.
				continue
			}
			if isProductionType(sourceType) || isProductionType(targetType) {
				return true
			}
		}
	}

	for _, conversions := range []map[string]map[string]string{numericConversions, indirectNumericConversions} {
		for sourceType, targetTypes := range conversions {
			for targetType := range targetTypes {
				if isProductionType(sourceType) && isProductionType(targetType) {
					return true
				}
			}
		}
	}

	return false
}

// isGo2CSRoot reports whether dir is a go2cs project-reference root — the directory the
// $(go2csPath) MSBuild property points at, identified by the shared runtime living at
// core\golib\golib.csproj beneath it.
func isGo2CSRoot(dir string) bool {
	if dir == "" {
		return false
	}

	_, err := os.Stat(filepath.Join(dir, "core", "golib", "golib.csproj"))
	return err == nil
}

// findGo2CSRootAbove walks dir's ancestor chain (inclusive) and returns the first go2cs
// project-reference root, or "" when none exists above dir.
func findGo2CSRootAbove(dir string) string {
	for current := dir; ; {
		if isGo2CSRoot(current) {
			return current
		}

		parent := filepath.Dir(current)

		if parent == current {
			return ""
		}

		current = parent
	}
}

type testDeclaration struct {
	Name                 string   `json:"name"`
	Kind                 string   `json:"kind"`
	PackageName          string   `json:"packageName"`
	CSharpClassName      string   `json:"-"`
	Source               string   `json:"source"`
	Line                 int      `json:"line"`
	Status               string   `json:"status"`
	Reason               string   `json:"reason,omitempty"`
	RequiredCapabilities []string `json:"requiredCapabilities,omitempty"`
}

type testSource struct {
	Path   string `json:"path"`
	Kind   string `json:"kind"`
	Status string `json:"status"`
	Reason string `json:"reason,omitempty"`
}

type testManifest struct {
	SchemaVersion           int               `json:"schemaVersion"`
	CapabilitiesVersion     int               `json:"capabilitiesVersion"`
	PackageImportPath       string            `json:"packageImportPath"`
	ProjectName             string            `json:"projectName"`
	TestProject             string            `json:"testProject"`
	GoVersion               string            `json:"goVersion"`
	TargetGOOS              string            `json:"targetGOOS"`
	TargetGOARCH            string            `json:"targetGOARCH"`
	SourceRevision          string            `json:"sourceRevision,omitempty"`
	ConverterRevision       string            `json:"converterRevision"`
	InputDigest             string            `json:"inputDigest"`
	TestProjectModel        string            `json:"testProjectModel,omitempty"`
	ProductionFiles         []string          `json:"productionFiles"`
	TestSources             []testSource      `json:"testSources"`
	Fixtures                []string          `json:"fixtures"`
	FixtureDirectories      []string          `json:"fixtureDirectories"`
	Tests                   []testDeclaration `json:"tests"`
	TestMain                *testDeclaration  `json:"testMain,omitempty"`
	Dependencies            []string          `json:"dependencies"`
	Capabilities            []string          `json:"capabilities"`
	RequiredCapabilities    []string          `json:"requiredCapabilities"`
	UnsupportedCapabilities []string          `json:"unsupportedCapabilities"`
}

func processTestConversion(inputPath, outputPath string, options Options) error {
	// The sibling declarator names steer the PRODUCTION pass only (see siblingTestFuncMethodNames);
	// that pass is complete by the time this runs. Each variant's own analysis then computes the
	// shadow set from its own universe — the in-package variant already contains these names, and
	// the external variant's declarations live in a different C# class, so leaving them set would
	// only over-qualify the external half's package idents.
	siblingTestFuncMethodNames = nil

	// Likewise for the addressed-global seed: each variant's universe already CONTAINS the
	// `_test.go` files, so its own collectAddressedGlobals sees `&g` directly. The seed exists only
	// for the production pass, which cannot.
	siblingTestAddressedGlobalNames = nil

	inputPath, err := filepath.Abs(inputPath)
	if err != nil {
		return err
	}

	outputPath, err = filepath.Abs(outputPath)
	if err != nil {
		return err
	}

	targetParts := strings.Split(options.targetPlatform, "/")
	if len(targetParts) != 2 {
		return fmt.Errorf("invalid target platform format %q", options.targetPlatform)
	}

	cfg := &packages.Config{
		Mode:       packages.LoadAllSyntax,
		Dir:        inputPath,
		Tests:      true,
		BuildFlags: options.loaderBuildFlags(),
		Env: append(os.Environ(),
			fmt.Sprintf("GOOS=%s", targetParts[0]),
			fmt.Sprintf("GOARCH=%s", targetParts[1])),
	}

	loaded, err := packages.Load(cfg, ".")
	if err != nil {
		return fmt.Errorf("load test package variants: %w", err)
	}

	production := findProductionPackage(loaded, inputPath)
	if production == nil {
		return fmt.Errorf("go/packages did not return a production package for %q", inputPath)
	}

	if len(production.Errors) > 0 {
		return fmt.Errorf("production package load failed: %v", production.Errors)
	}

	// External package tests import the package under test. Bind that import to the
	// production partial class compiled into the same test assembly, never to a
	// project reference back to the production DLL.
	options.testPackagePath = production.PkgPath
	options.testPackageName = production.Name

	internal, external := findTestVariants(loaded, production)
	if internal == nil && external == nil {
		return writeNoTestsManifest(production, inputPath, outputPath, targetParts, options)
	}

	// Phase-4D file exclusion (option-a ruling): drop Example/Benchmark-only test files from the
	// compile set (both models honor it below). Computed once from both variants — a cross-variant
	// reference keeps a file compiled — and reused across the reference→recompile fallback.
	compileExcluded := selectCompileExcludedTestFiles(internal, external)

	projectName, projectNamespace := getProjectName(inputPath, options)
	supported := NewHashSet(supportedTestCapabilities())
	testInfoPath := filepath.Join(outputPath, testPackageInfoFileName)

	model := selectTestProjectModel(internal, external)
	conversion, err := convertTestVariants(model, production, internal, external, compileExcluded, inputPath, outputPath, projectNamespace, supported, options)

	if errors.Is(err, errProductionAnchoredRecords) {
		// The suite records metadata that must mutate a production type — only a same-assembly
		// recompile can host it. Reconvert under the recompile model: conversion is deterministic
		// and the expensive go/packages load above is reused, so fallback costs one emission pass.
		model = testProjectRecompile
		conversion, err = convertTestVariants(model, production, internal, external, compileExcluded, inputPath, outputPath, projectNamespace, supported, options)
	}

	if err != nil {
		return err
	}

	declarations := conversion.declarations
	testMain := conversion.testMain
	outputFiles := conversion.outputFiles
	allImports := conversion.allImports
	requiredCapabilities := conversion.requiredCapabilities
	includedSources := conversion.includedSources

	sort.Slice(declarations, func(i, j int) bool {
		if declarations[i].Name == declarations[j].Name {
			return declarations[i].PackageName < declarations[j].PackageName
		}
		return declarations[i].Name < declarations[j].Name
	})

	// Hand-owned companions of TEST-file conversions — `*_impl_test.cs`
	// (internal/reflectlite's export_impl_test.cs is the pattern's first instance). They are
	// committed beside the package exactly like the production `*_impl.cs` companions and are
	// compiled into the TEST project: the `_test.cs` suffix keeps them under the production
	// side's existing test-artifact exclusion (csproj template and productionCSFiles both),
	// so no production emission changes. Globbed FRESH (F7) so a companion appearing or
	// disappearing re-shapes the project without a recorded list; testInputDigest globs the
	// same pattern so editing one invalidates a prior comparison.
	testImplCompanions, err := filepath.Glob(filepath.Join(outputPath, "*_impl_test.cs"))
	if err != nil {
		return err
	}
	for _, companion := range testImplCompanions {
		name := filepath.Base(companion)
		if !containsString(outputFiles, name) {
			outputFiles = append(outputFiles, name)
		}
	}

	sort.Strings(outputFiles)

	fixtures, err := copyTestFixtures(inputPath, outputPath)
	if err != nil {
		return err
	}

	fixtureDirectories, err := testFixtureDirectories(inputPath)
	if err != nil {
		return err
	}

	productionFiles, err := productionCSFiles(outputPath, goosOfTarget(options.targetPlatform))
	if err != nil {
		return err
	}

	if err := writeTestHost(outputPath, projectNamespace, production.PkgPath, declarations, testMain, fixtures, fixtureDirectories); err != nil {
		return err
	}

	dependencies := allImports.Keys()
	dependencies = removeString(dependencies, production.PkgPath)
	dependencies = removeString(dependencies, "testing")
	sort.Strings(dependencies)

	referenceImports := append(append([]string{}, dependencies...), aliasReferenceImports(
		testProjectAliasScanFiles(model, outputPath, testInfoPath, outputFiles, productionFiles),
		production.PkgPath, dependencies)...)

	// Close the reference set under the C# DECLARATION edges the converter emits
	// (declarationClosureImports): binding a type the compilation NAMES needs the assemblies its
	// own declaration names — an interface's base interfaces (hash's `Hash : io.Writer` reached by
	// every `GoImplement<…, hash_package.Hash64>` record, io/fs's `File : io.ReadCloser`) and a
	// struct's field types (testing/quick's `Config` holds a `*rand.Rand`) — and those edges belong
	// to the DECLARING package's import graph, so no test import and no alias `using` names them
	// (CS0012). Both models feed the same walk; the already-referenced set it subtracts carries the
	// production package's own path so an edge landing there never becomes a self-reference.
	//
	// The package's OWN package_info.cs — written moments ago by the production half of this same
	// run — supplies the fourth edge's gate: the VALUE-form `[assembly: GoImplement<T, I>]` records
	// go2cs-gen realizes as base lists on the production types the test half binds members on.
	referenceImports = append(referenceImports, declarationClosureImports(
		[]*packages.Package{production, internal, external}, compileExcluded,
		append([]string{production.PkgPath}, referenceImports...),
		packageImplementBases(platformPackageInfoPath(outputPath, goosOfTarget(options.targetPlatform))),
		foreignImplementBasesResolver(options))...)

	testProjectName := projectFileBaseName(projectName) + ".tests.csproj"
	if err := writeTestProject(filepath.Join(outputPath, testProjectName), projectName, projectNamespace, model, productionFiles, outputFiles, fixtures, referenceImports, options); err != nil {
		return err
	}

	sources, err := classifyTestSources(inputPath, includedSources, compileExcluded, external)
	if err != nil {
		return err
	}

	capabilities := supportedTestCapabilities()
	required := requiredCapabilities.Keys()
	sort.Strings(required)
	unsupported := NewHashSet(required)
	unsupported.ExceptWith(capabilities)
	unsupportedList := unsupported.Keys()
	sort.Strings(unsupportedList)

	manifest := testManifest{
		SchemaVersion:           1,
		CapabilitiesVersion:     1,
		PackageImportPath:       production.PkgPath,
		ProjectName:             projectName,
		TestProject:             testProjectName,
		GoVersion:               runtime.Version(),
		TargetGOOS:              targetParts[0],
		TargetGOARCH:            targetParts[1],
		SourceRevision:          gitRevision(inputPath),
		ConverterRevision:       converterRevision(),
		TestProjectModel:        model.String(),
		ProductionFiles:         productionFiles,
		TestSources:             sources,
		Fixtures:                fixtures,
		FixtureDirectories:      fixtureDirectories,
		Tests:                   declarations,
		TestMain:                testMain,
		Dependencies:            dependencies,
		Capabilities:            capabilities,
		RequiredCapabilities:    required,
		UnsupportedCapabilities: unsupportedList,
	}

	manifest.InputDigest, err = testInputDigest(inputPath, outputPath, options, manifest.ConverterRevision)
	if err != nil {
		return err
	}

	return writeJSONFile(filepath.Join(outputPath, testManifestFileName), manifest)
}

// testVariantConversionResult carries everything one convertTestVariants pass produced — the
// model-dependent conversion state a reference→recompile fallback re-run rebuilds from scratch.
type testVariantConversionResult struct {
	declarations         []testDeclaration
	testMain             *testDeclaration
	outputFiles          []string
	allImports           HashSet[string]
	requiredCapabilities HashSet[string]
	includedSources      HashSet[string]
}

// convertTestVariants converts the package's test variants under the given test-project model:
// seeds the package_test_info.cs anchor, discovers and converts each variant, and merges the
// collected metadata into the model's anchor file(s). A reference model returns
// errProductionAnchoredRecords when records require a closed production-type mutation; the caller
// then re-runs the pass under testProjectRecompile (the go/packages load remains shared).
func convertTestVariants(model testProjectModel, production, internal, external *packages.Package, compileExcluded map[string]bool, inputPath, outputPath, projectNamespace string, supported HashSet[string], options Options) (testVariantConversionResult, error) {
	internalUnitListed := false

	result := testVariantConversionResult{
		declarations:         make([]testDeclaration, 0),
		outputFiles:          make([]string, 0),
		allImports:           HashSet[string]{},
		requiredCapabilities: HashSet[string]{},
		includedSources:      HashSet[string]{},
	}

	// Collected across BOTH variants (result.outputFiles carries csproj <Compile> names, not
	// paths) so the deferred adapter names can be resolved once, after the merged metadata file
	// makes the record set final.
	testAdapterResolveNames = nil
	emittedAdapterPairAnchors = nil

	// A model change between runs (or a recompile fallback) must not leave a stale bridge anchor
	// on disk: it is merge-preserving, and a superseded record set would silently resurrect.
	// The models that need it re-seed it below; everything else keeps the directory clean.
	_ = os.Remove(filepath.Join(outputPath, internalTestPackageInfoFileName))

	internalBridgeName := getSanitizedImport(production.Name + "_internal_test" + PackageSuffix)
	testClassName := internalBridgeName
	testPackageName := production.Name
	if external != nil {
		testClassName = getSanitizedImport(external.Name + PackageSuffix)
		testPackageName = external.Name
	}

	if model.referencesProduction() {
		// The recompile model's external-test anchor is superseded under a reference model; a
		// copy left by a previous recompile conversion is merge-preserving and would resurrect
		// stale records on the next fallback re-run.
		_ = os.Remove(filepath.Join(outputPath, externalTestPackageInfoFileName))
	}

	// The bridge's declared-name set drives the white-box record split: a BARE record name in
	// this set is a bridge-declared type whose generated partial must merge inside the bridge.
	whiteboxBridgeTypeNames = HashSet[string]{}
	if model == testProjectWhiteboxReference {
		whiteboxBridgeTypeNames = collectWhiteboxBridgeTypeNames(internal)
	}

	if model.referencesProduction() {
		options.testProductionPath = options.testPackagePath
		options.testProductionName = options.testPackageName
		options.testMetadataAnchorName = testClassName
		if model == testProjectWhiteboxReference {
			options.testWhiteboxReference = true
			options.testInternalBridgeName = internalBridgeName
		}

		// The production package binds as an ORDINARY imported package: its exported metadata
		// (type aliases, implements) loads from the colocated package_info.cs like any other
		// dependency's, its types render package-qualified, and isSameAssemblyPkg answers false
		// so cast sites compose the same foreign adapter names go2cs-gen generates for a
		// project-referenced package. Clearing the self-import binding is what flips all of it
		// (visitImportSpec's isPackageUnderTest, convertTestVariant's testLocalTypePrefixes and
		// loadPackageImplements are each gated on these fields).
		options.testPackagePath = ""
		options.testPackageName = ""
	}

	// Session-scoped, not per-variant (B2/B9): both variants come from the ONE load the caller
	// performed, so the external variant's references to an internal-variant-renamed method (the
	// export_test pattern) resolve by object identity to entries registered during the internal
	// pass — resetPackageState deliberately does not clear this map.
	testMethodRenames = make(map[types.Object]bool)
	whiteboxInternalTestObjects = collectWhiteboxInternalTestObjects(internal)

	whiteboxBridgeDeclaredNames = HashSet[string]{}
	if model == testProjectWhiteboxReference {
		whiteboxBridgeDeclaredNames = collectWhiteboxBridgeDeclaredNames(internal)
	}

	// The naming state the PRODUCTION conversion of this package left standing. It ran in this same
	// process moments ago (processConversion converts the production sources, then calls
	// processTestConversion), so its live claims are still here — captured BEFORE the first
	// variant's resetPackageState clears them. Only the INTERNAL variant is seeded with it: its
	// test files emit into the production package class, where those names are already taken.
	// See productionSeed for what each member pins and why. Captured by REFERENCE deliberately:
	// resetPackageState replaces each of these globals with a fresh instance rather than clearing
	// the one in place, so the captured production state stays pristine while the variant claims
	// into its own.
	internalSeed := productionSeed{
		liftedTypeNames:      packageLiftedTypeNames,
		hoistedConstOrdinals: packageHoistedConstOrdinals,
		globalTempVarCounts:  globalTempVarCount,
		blankImportForces:    packageBlankImportForces,
		initFuncs:            initFuncCounter,
	}

	// The simple type names BOTH variant classes declare (see testAmbiguousLocalTypeNames). Both
	// `using static` directives are in scope in the merged metadata, so these must emit
	// class-qualified. Computed from the loaded variants before either converts, and session-scoped
	// — resetPackageState does not clear it.
	testAmbiguousLocalTypeNames = ambiguousVariantTypeNames(internal, external)

	productionAnchor := metadataClassPrefix(projectNamespace, production.Name)
	internalAnchor := projectNamespace + "." + internalBridgeName
	testAnchor := projectNamespace + "." + testClassName

	testInfoPath := filepath.Join(outputPath, testPackageInfoFileName)

	if model.referencesProduction() {
		// The reference model must NOT declare the production package class: the production
		// types' single identity is the referenced production assembly, and a local partial
		// declaration (or generated code anchored to one) would re-introduce exactly the
		// duplicate-type shadow the model exists to eliminate. Seed a test-class-only anchor
		// instead of the production package_info.cs.
		seedArgs := []string{}
		if model == testProjectWhiteboxReference {
			seedArgs = append(seedArgs, internalBridgeName)
		}
		seed := referenceModelTestPackageInfoSeed(projectNamespace, testClassName, testPackageName, getSanitizedImport(production.Name+PackageSuffix), seedArgs...)

		if err := os.WriteFile(testInfoPath, []byte(seed), 0644); err != nil {
			return result, fmt.Errorf("seed test package metadata: %w", err)
		}
	} else {
		// Seed package_test_info.cs from the production package_info.cs so the production
		// assembly-level metadata carries over verbatim; each converted variant's ADDITIONS are
		// then merged in by the shared writePackageInfoFile (identical emission semantics to
		// production — pointer-form unwrapping, dedup, pruning — because it IS the production
		// writer).
		// Layout L3: an L3 package's production package_info.cs lives in its per-GOOS folder, and it
		// is the SEED this test conversion's own metadata is merged into — asking flat would fail
		// the "convert the package itself before its tests" check on a package that HAS been
		// converted (design §4.3).
		productionInfoPath := platformPackageInfoPath(outputPath, goosOfTarget(options.targetPlatform))
		productionInfo, err := os.ReadFile(productionInfoPath)
		if err != nil {
			return result, fmt.Errorf("read production package metadata (convert the package itself before its tests): %w", err)
		}

		if err := os.WriteFile(testInfoPath, productionInfo, 0644); err != nil {
			return result, fmt.Errorf("seed test package metadata: %w", err)
		}

		// The production sources recompile into the test assembly, so their imports are test
		// project references too. Under the reference model the production ASSEMBLY carries its
		// own dependencies, and the test project references only what the test files import
		// (plus the alias-scan additions — B2c).
		for importPath := range production.Imports {
			result.allImports.Add(importPath)
		}
	}

	for _, variant := range []*packages.Package{internal, external} {
		if variant == nil {
			continue
		}

		if len(variant.Errors) > 0 {
			return result, fmt.Errorf("test package variant %q failed to load: %v", variant.ID, variant.Errors)
		}

		entries := testFileEntries(variant)
		if len(entries) == 0 {
			continue
		}

		for _, entry := range entries {
			result.includedSources.Add(filepath.Clean(entry.filePath))
		}

		// DISCOVERY runs over EVERY test file (below), so an excluded file's Example/Benchmark
		// declarations still reach the manifest under their disclosed-unsupported status. EMISSION
		// runs over the non-excluded files only — the excluded file's C# is never written and it is
		// never a csproj compile item (Phase-4D file-exclusion ruling, selectCompileExcludedTestFiles).
		emitEntries := make([]FileEntry, 0, len(entries))
		for _, entry := range entries {
			if compileExcluded[filepath.Clean(entry.filePath)] {
				continue
			}
			emitEntries = append(emitEntries, entry)
		}

		capabilities := analyzeTestingCapabilities(variant)
		found, foundMain := discoverTestDeclarations(variant, entries, inputPath, capabilities, supported)
		if model == testProjectWhiteboxReference && variant == internal {
			for i := range found {
				found[i].CSharpClassName = internalBridgeName
			}
			if foundMain != nil {
				foundMain.CSharpClassName = internalBridgeName
			}
		}
		result.declarations = append(result.declarations, found...)

		// Package-level capability reporting aggregates over RUNNABLE declaration kinds only
		// (tests + TestMain) — benchmark/fuzz/example requirements must not block the package,
		// they are excluded-disclosed by their own status (F4: attribution is per-test).
		for _, declaration := range found {
			if declaration.Kind == "test" {
				result.requiredCapabilities.UnionWith(declaration.RequiredCapabilities)
			}
		}

		if foundMain != nil {
			if result.testMain != nil {
				return result, fmt.Errorf("multiple valid TestMain declarations: %s and %s", result.testMain.Source, foundMain.Source)
			}
			result.testMain = foundMain
			result.requiredCapabilities.UnionWith(foundMain.RequiredCapabilities)
		}

		// The seed is the INTERNAL variant's under the RECOMPILE model alone — that is exactly the
		// case where the production `.cs` are compile items of this assembly and share the emitted
		// class. Under the reference model production is a separate assembly; the external variant
		// has a class of its own. Both may reuse every seeded name.
		var seed productionSeed

		if variant == internal && !model.referencesProduction() {
			seed = internalSeed
		}

		variantOptions := options
		if model == testProjectWhiteboxReference {
			variantOptions.testExternalVariant = variant == external
			if variant == internal {
				variantOptions.testClassNameOverride = internalBridgeName
				variantOptions.testInlineTypeAccess = true
			}
		}

		variantOutputs, imports, err := convertTestVariant(variant, emitEntries, outputPath, projectNamespace, seed, variantOptions)
		if err != nil {
			return result, err
		}

		// A FUNCTION-LOCAL type declared by an internal _test.go emits under its LIFTED
		// package-level name (`TestEncoderDecoder_r`), which the go/types declared-name scan above
		// cannot know: it sees the Go-source name (`r`). The record split keys on the EMITTED
		// name, so union the internal variant's live lift claims in before the split runs — they
		// are still standing here, the next variant's resetPackageState is what clears them.
		// Without this the record anchors in the EXTERNAL test class and the generator declares a
		// PHANTOM empty type there instead of merging with the bridge's real declaration
		// (encoding/hex: CS0103 on the phantom's missing embed, CS0034 from the phantom's missing
		// TypeGenerator `==`, and CS1503 at the cast site — see splitWhiteboxVariantRecords).
		if model == testProjectWhiteboxReference && variant == internal {
			whiteboxBridgeTypeNames.UnionWithSet(packageLiftedTypeNames)
		}

		// Merge this variant's collected metadata globals while they are still live (the next
		// variant's conversion resets them). Under the RECOMPILE model the EXTERNAL variant's
		// records are split across TWO anchor files (B4/B5): records whose generated code must
		// live in the test package class go to package_info_external_test.cs; production-
		// anchored records stay in package_test_info.cs. Under the REFERENCE model there is a
		// single anchor — the test package class — and a record that would need the production
		// anchor triggers the recompile fallback instead.
		if model == testProjectWhiteboxReference && recordsRequireProductionMutation(getSanitizedImport(production.Name+PackageSuffix), production.Name) {
			return result, errProductionAnchoredRecords
		}

		if model == testProjectWhiteboxReference && external != nil {
			// A MIXED white-box suite has two owning classes in one assembly; each variant's
			// records split between the bridge anchor and the test anchor by declared-name set.
			unitName, err := writeWhiteboxVariantMetadata(testInfoPath, outputPath,
				getSanitizedImport(production.Name+PackageSuffix), internalBridgeName,
				production.Name, internalAnchor, testAnchor, whiteboxBridgeTypeNames, variant == internal)
			if err != nil {
				return result, err
			}

			if unitName != "" && !internalUnitListed {
				result.outputFiles = append(result.outputFiles, unitName)
				internalUnitListed = true
			}
		} else if variant == external {
			if model.referencesProduction() {
				if model == testProjectReference && recordsRequireProductionAnchor(getSanitizedImport(production.Name+PackageSuffix), production.Name) {
					return result, errProductionAnchoredRecords
				}

				// Reference model: the seeded package_test_info.cs declares the TEST class as its
				// first — and only — class, so that is its anchor.
				metadataAnchorClassPrefix = testAnchor
				metadataAnchorLocalTypes = true
				writePackageInfoFile(testInfoPath, true)
			} else {
				unitName, err := writeExternalVariantMetadata(testInfoPath, outputPath, production.Name, productionAnchor, testAnchor)
				if err != nil {
					return result, err
				}

				if unitName != "" {
					result.outputFiles = append(result.outputFiles, unitName)
				}
			}
		} else {
			// An INTERNAL-ONLY white-box suite has one owning class — the bridge, which is also
			// the seeded first class of package_test_info.cs — so a single anchored write suffices.
			metadataAnchorClassPrefix = productionAnchor
			metadataAnchorLocalTypes = false
			if model == testProjectWhiteboxReference {
				metadataAnchorClassPrefix = internalAnchor
				metadataAnchorLocalTypes = true
			}
			writePackageInfoFile(testInfoPath, true)
		}

		result.outputFiles = append(result.outputFiles, variantOutputs...)
		result.allImports.UnionWithSet(imports)
	}

	// The reference-model seed already declares the attribute-bearing test package class as its
	// first — and only — class; the append is a recompile-model concern (the production-seeded
	// file needs the test class and its widened `using static` scope added).
	if !model.referencesProduction() {
		if err := appendExternalTestPackageClass(testInfoPath, projectNamespace, production.Name, external); err != nil {
			return result, err
		}
	}

	// Resolve the deferred pointer-adapter names for the test outputs against the FINISHED
	// metadata file — read back rather than taken from the last writePackageInfoFile capture,
	// because the variants reach it by different paths (the recompile model's external variant
	// goes through writeExternalVariantMetadata instead) and only the file on disk is what
	// go2cs-gen will actually read. The test closure is where collisions surface at all: its
	// extra casts are what let one struct reach two same-simple-named interfaces.
	if model == testProjectWhiteboxReference {
		// TWO anchor files can exist under white-box; capture both so the pair set is the
		// assembly's full record set and each pair remembers which class its adapter lives in.
		unitPath := filepath.Join(outputPath, internalTestPackageInfoFileName)
		if _, err := os.Stat(unitPath); err == nil {
			captureAdapterPairsFromInfoFile(unitPath, internalBridgeName)
		}
		captureAdapterPairsFromInfoFile(testInfoPath, testClassName)
		resolveAdapterNameMarkers(testAdapterResolveNames, options.testMetadataAnchorName)
	} else {
		captureAdapterPairsFromInfoFile(testInfoPath)
		resolveAdapterNameMarkers(testAdapterResolveNames)
	}

	return result, nil
}

// metadataClassPrefix renders the fully-qualified C# class a converted Go package emits into —
// the anchor a metadata file's bare local type references bind to.
func metadataClassPrefix(namespace, goPackageName string) string {
	return namespace + "." + getSanitizedImport(goPackageName+PackageSuffix)
}

// ambiguousVariantTypeNames returns the simple type names declared by BOTH `-tests` variants: the
// package under test (production + its internal `_test.go` files) and the external `<pkg>_test`
// suite. Both classes are `using static`-imported by the merged metadata, so a bare reference to
// one of these names cannot bind (CS0104) — see testAmbiguousLocalTypeNames. The Go name and its
// core-sanitized C# spelling are both recorded: membership is tested against an EMITTED name, and
// an entry that can never be emitted is inert. Empty unless BOTH variants exist.
func ambiguousVariantTypeNames(internal, external *packages.Package) HashSet[string] {
	ambiguous := HashSet[string]{}

	if internal == nil || external == nil || internal.Types == nil || external.Types == nil {
		return ambiguous
	}

	externalTypeNames := HashSet[string]{}

	for _, name := range external.Types.Scope().Names() {
		if _, ok := external.Types.Scope().Lookup(name).(*types.TypeName); ok {
			externalTypeNames.Add(name)
		}
	}

	for _, name := range internal.Types.Scope().Names() {
		if _, ok := internal.Types.Scope().Lookup(name).(*types.TypeName); !ok {
			continue
		}

		if !externalTypeNames.Contains(name) {
			continue
		}

		ambiguous.Add(name)
		ambiguous.Add(getCoreSanitizedIdentifier(name))
	}

	return ambiguous
}

// referenceModelTestPackageInfoSeed composes package_test_info.cs for a production-reference test
// project. The structure mirrors package_info-template.txt (the shared writer requires all four
// marker sections); the FIRST — and only — class declaration is the test metadata anchor,
// which is where go2cs-gen anchors generated adapters and partials
// (GetFirstClassName), carrying [GoPackage] directly (no second partial exists to make that a
// CS0579). Deliberately absent, versus the recompile model's production-seeded file: the
// production class declaration and every production-anchored record — the referenced production
// assembly already owns them, and a local shadow would duplicate its types.
func referenceModelTestPackageInfoSeed(projectNamespace, testClassName, goPackageName, productionClassName string, additionalStaticClasses ...string) string {
	var b strings.Builder

	b.WriteString("// go2cs metadata anchor for a production-reference test project: the test assembly\r\n")
	b.WriteString("// REFERENCES the colocated production project instead of\r\n")
	b.WriteString("// recompiling its sources, so the production assembly is the single identity for the\r\n")
	b.WriteString("// production types and no production class partial may be declared here. The first —\r\n")
	b.WriteString("// and only — class is the test metadata class the go2cs-gen generators anchor\r\n")
	b.WriteString("// generated adapters and partials to.\r\n")
	b.WriteString(fmt.Sprintf("global using static global::%s.%s;\r\n", projectNamespace, productionClassName))
	for _, className := range additionalStaticClasses {
		// An internal-only suite names the bridge as BOTH the test class and the additional
		// class — the file-scoped `using static` below already imports it, and a second,
		// global import of the same class is CS8933.
		if className != "" && className != productionClassName && className != testClassName {
			b.WriteString(fmt.Sprintf("global using static global::%s.%s;\r\n", projectNamespace, className))
		}
	}
	b.WriteString("\r\n")
	b.WriteString("// <ImportedTypeAliases>\r\n")
	b.WriteString("// </ImportedTypeAliases>\r\n")
	b.WriteString("\r\n")
	b.WriteString("using go;\r\n")

	staticClasses := []string{testClassName}
	seenStatic := HashSet[string]{}
	for _, className := range staticClasses {
		if className == "" || seenStatic.Contains(className) {
			continue
		}
		seenStatic.Add(className)
		b.WriteString(fmt.Sprintf("using static global::%s.%s;\r\n", projectNamespace, className))
	}
	b.WriteString("\r\n")
	b.WriteString("// <ExportedTypeAliases>\r\n")
	b.WriteString("// </ExportedTypeAliases>\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <InterfaceImplementations>\r\n")
	b.WriteString("// </InterfaceImplementations>\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <ImplicitConversions>\r\n")
	b.WriteString("// </ImplicitConversions>\r\n")
	b.WriteString("\r\n")
	b.WriteString(fmt.Sprintf("namespace %s;\r\n", projectNamespace))
	b.WriteString("\r\n")
	b.WriteString(fmt.Sprintf("[GoPackage(\"%s\")]\r\n", goPackageName))
	b.WriteString(fmt.Sprintf("public static partial class %s\r\n{\r\n}\r\n", testClassName))

	return b.String()
}

// collectSiblingTestClosure populates siblingClosureImportPaths with the transitive import closure
// of the package's _test.go variants so package-wide declaration analysis sees the complete test
// assembly. The closure also supplies the production half when mutation forces recompile fallback.
// Declarator names are collected separately and cheaply per package by
// collectSiblingTestFuncMethodNames, including for ordinary conversion, so reference spelling does
// not depend on whether -tests was requested. Metadata load only (no syntax/types for dependencies),
// so it costs a fraction of the LoadAllSyntax pass processTestConversion does later. Best-effort: a
// load failure leaves the closure empty and the production conversion behaves exactly as before —
// processTestConversion reports the real error moments later.
func collectSiblingTestClosure(inputPath string, options Options) {
	siblingClosureImportPaths = nil
	targetParts := strings.Split(options.targetPlatform, "/")
	if len(targetParts) != 2 {
		return
	}

	absolute, err := filepath.Abs(inputPath)
	if err != nil {
		return
	}

	loaded, err := packages.Load(&packages.Config{
		Mode:       packages.NeedName | packages.NeedImports | packages.NeedDeps | packages.NeedCompiledGoFiles,
		Dir:        absolute,
		Tests:      true,
		BuildFlags: options.loaderBuildFlags(),
		Env: append(os.Environ(),
			fmt.Sprintf("GOOS=%s", targetParts[0]),
			fmt.Sprintf("GOARCH=%s", targetParts[1])),
	}, ".")

	if err != nil {
		return
	}

	closure := HashSet[string]{}

	var walk func(pkg *packages.Package)

	walk = func(pkg *packages.Package) {
		for path, imported := range pkg.Imports {
			if closure.Contains(path) {
				continue
			}

			closure.Add(path)
			walk(imported)
		}
	}

	// Only the TEST variants contribute: the production package's own closure is already walked by
	// computeImportAliasRenames from the loaded types, and the synthetic `<pkg>.test` main package
	// is not part of the emitted assembly.
	for _, pkg := range loaded {
		if !strings.Contains(pkg.ID, "[") {
			continue
		}

		walk(pkg)
	}

	siblingClosureImportPaths = closure.Keys()
	sort.Strings(siblingClosureImportPaths)
}

func findProductionPackage(pkgs []*packages.Package, inputPath string) *packages.Package {
	for _, pkg := range pkgs {
		if pkg.Name == "main" || strings.Contains(pkg.ID, "[") {
			continue
		}

		if samePath(pkg.Dir, inputPath) {
			return pkg
		}
	}

	return nil
}

func findTestVariants(pkgs []*packages.Package, production *packages.Package) (internal, external *packages.Package) {
	testID := production.PkgPath + ".test]"

	for _, pkg := range pkgs {
		if !strings.HasSuffix(pkg.ID, testID) || !strings.Contains(pkg.ID, "[") {
			continue
		}

		switch {
		case pkg.PkgPath == production.PkgPath && pkg.Name == production.Name:
			internal = pkg
		case pkg.Name == production.Name+"_test":
			external = pkg
		}
	}

	return internal, external
}

func testFileEntries(pkg *packages.Package) []FileEntry {
	entries := make([]FileEntry, 0)

	for i, file := range pkg.Syntax {
		if i >= len(pkg.CompiledGoFiles) {
			break
		}

		path := pkg.CompiledGoFiles[i]
		if strings.HasSuffix(strings.ToLower(filepath.Base(path)), "_test.go") {
			entries = append(entries, newFileEntry(file, path, false))
		}
	}

	return entries
}

// The manifest TestSources status/reason for a _test.go file dropped from the compile set by the
// Phase-4D Example/Benchmark-only file-exclusion policy (selectCompileExcludedTestFiles). Distinct
// from "platform-excluded" (a file build-constraints deselect): this file WAS selected for the
// target but declares nothing the run registry admits, so its compilation is deferred alongside
// its declarations' execution.
const (
	compileExcludedSourceStatus = "example-benchmark-only"
	compileExcludedSourceReason = "file declares only Phase-4D-deferred Example/Benchmark functions; its compilation is deferred to Phase 4D along with their execution"
)

// isPhase4DExcludedTestFunc reports whether a top-level function declaration is a Phase-4D-deferred
// Example or Benchmark — the EXACT classification discoverTestDeclarations applies for its
// "example"/"benchmark" (status "unsupported") cases: no receiver, no results, no type params, and
// either a zero-parameter func Example* or a single-*testing.B-parameter func Benchmark*. A
// Test/TestMain/Fuzz func, a method, or a mis-signatured Example/Benchmark returns false. TestMain
// and Fuzz are DELIBERATELY not treated as excluded here — the ruling scopes the file predicate to
// Example/Benchmark (conservative by design), so a file declaring either stays in the compile set.
func isPhase4DExcludedTestFunc(fn *ast.FuncDecl, info *types.Info) bool {
	if fn.Recv != nil || fn.Name == nil {
		return false
	}

	obj, ok := info.Defs[fn.Name].(*types.Func)
	if !ok {
		return false
	}

	sig, ok := obj.Type().(*types.Signature)
	if !ok || sig.TypeParams().Len() != 0 || sig.Results().Len() != 0 {
		return false
	}

	name := fn.Name.Name

	if sig.Params().Len() == 0 {
		return isGoTestName(name, "Example")
	}

	return sig.Params().Len() == 1 && isGoTestName(name, "Benchmark") && isTestingPointer(sig.Params().At(0).Type(), "B")
}

// testFileExclusionInfo holds the go/types facts the Phase-4D file-exclusion predicate needs for
// one _test.go file: whether every top-level declaration it contributes is a Phase-4D-deferred
// Example/Benchmark function (condition 1), the objects it declares (the reference targets of
// condition 2), and every object it references (so a candidate promoted back to RETAINED can, in
// turn, pull a file IT references back into the compile set — the condition-2 fixpoint).
type testFileExclusionInfo struct {
	path      string
	qualifies bool                  // condition (1): declares only Example/Benchmark functions
	declared  []types.Object        // top-level objects the file declares
	used      map[types.Object]bool // objects the file references (go/types Uses)
}

// classifyTestFileForExclusion evaluates condition (1) for one test file and captures the go/types
// objects condition (2) needs. A file qualifies when every RUNNABLE declaration it contributes is a
// Phase-4D-deferred Example/Benchmark function, plus — since the crypto/tls measurement, 2026-08-15 —
// the pure TYPE declarations and METHODS such a function needs to express itself.
//
// Why types and methods joined, and why nothing else did. The original predicate accepted a file
// whose declarations were EXCLUSIVELY Example/Benchmark funcs, which is the shape go/token's
// example_test.go happens to have. crypto/tls's is the same file in every way that matters — it is
// the package's ONLY black-box file and every runnable thing in it is an Example — except that its
// Examples need an io.Reader to hand `Config.Rand`, so it declares `type zeroSource struct{}` and one
// `Read` method on it. That one helper type kept the whole file compiled, and under the recompile
// model a compiled black-box Example is exactly what the ruling exists to prevent: `http.Transport`'s
// `TLSClientConfig` field names `tls_package.Config` in the PRODUCTION assembly while the test
// assembly recompiles its own, so the field is unnameable — CS0012 ×3 at example_test.cs 88/99/198,
// two of `crypto/tls`'s four build errors. Adding the production reference cannot fix it (the two
// `Config`s stay distinct types and CS0012 merely becomes CS0029), so the file must not be compiled.
//
// A type declaration and its methods are admissible because they have no RUN-TIME behavior of their
// own: nothing executes at package init, and any use by a retained file is a reference condition (2)
// already resolves by go/types object identity — which is why the type and method objects are now
// recorded in `declared`, without which widening condition (1) would silently disarm condition (2).
// Everything else stays disqualifying, deliberately: a `var`/`const` initializer can carry side
// effects, and a plain helper func can be `init()`, neither of which any reference edge would reveal.
func classifyTestFileForExclusion(file *ast.File, info *types.Info, path string) *testFileExclusionInfo {
	result := &testFileExclusionInfo{path: path, used: make(map[types.Object]bool)}

	qualifies := true
	hasExcludedFunc := false

	declare := func(name *ast.Ident) {
		if name == nil {
			return
		}

		if object := info.Defs[name]; object != nil {
			result.declared = append(result.declared, object)
		}
	}

	for _, decl := range file.Decls {
		switch typed := decl.(type) {
		case *ast.GenDecl:
			if typed.Tok == token.IMPORT {
				continue // imports are not declarations for this predicate
			}

			if typed.Tok != token.TYPE {
				qualifies = false // a top-level var/const disqualifies the file
				continue
			}

			// A pure type declaration is admissible; record it so condition (2) can see a
			// retained file's reference to it.
			for _, spec := range typed.Specs {
				if typeSpec, ok := spec.(*ast.TypeSpec); ok {
					declare(typeSpec.Name)
				}
			}
		case *ast.FuncDecl:
			switch {
			case isPhase4DExcludedTestFunc(typed, info):
				hasExcludedFunc = true
				declare(typed.Name)
			case typed.Recv != nil:
				// A method on a file-local type: admissible with its receiver type, and
				// recorded for the same condition-(2) reason.
				declare(typed.Name)
			default:
				qualifies = false // a Test/TestMain/Fuzz func, an init, or a plain helper disqualifies
			}
		default:
			qualifies = false
		}
	}

	result.qualifies = qualifies && hasExcludedFunc

	// Every referenced object, for the condition-(2) fixpoint. Collected for ALL files: a retained
	// file's references are the exclusion driver, and a candidate's are needed once it is promoted.
	// Defining idents resolve through Defs (not Uses) and are skipped, so a file's own Example name
	// does not count as a reference to itself.
	ast.Inspect(file, func(node ast.Node) bool {
		if ident, ok := node.(*ast.Ident); ok {
			if object := info.Uses[ident]; object != nil {
				result.used[object] = true
			}
		}
		return true
	})

	return result
}

// selectCompileExcludedTestFiles applies the user-approved Phase-4D file-exclusion ruling
// ("option a", 2026-07-24): a _test.go file is dropped from the -tests conversion/compile set iff
//
//	(1) every RUNNABLE declaration it contributes is a Phase-4D-deferred declaration — the file
//	    declares at least one func Example* / func Benchmark* and, apart from those, only pure TYPE
//	    declarations and METHODS (imports do not count as declarations; any var/const, or any other
//	    plain func — a Test/TestMain/Fuzz func, an init, or a mis-signatured Example/Benchmark —
//	    disqualifies the file, conservative by design; see classifyTestFileForExclusion for why the
//	    type/method admission is safe and why nothing beyond it is), AND
//	(2) no RETAINED test file references any object the file declares (resolved by go/types object
//	    identity across the loaded variant set, never by filename or text).
//
// Phase-4D already excludes Example/Benchmark DECLARATIONS from the run registry uniformly, so a
// file that contributes nothing to the run contributes nothing to the compile. This unblocks the
// compile-poisoning external Example-only files (go/token's example_test.go, whose whitebox+blackbox
// recompile drags cross-assembly type identity into CS0012) WITHOUT touching the differential
// oracle: discovery is left intact, so the excluded file's declarations still appear in the manifest
// under their existing disclosed-unsupported status — the F6 census gate stays truthful and every
// already-filtered Example/Benchmark stays filtered. Only the file's EMISSION and csproj
// compile-membership are dropped. Returns the set of cleaned file paths to exclude.
func selectCompileExcludedTestFiles(variants ...*packages.Package) map[string]bool {
	infos := make([]*testFileExclusionInfo, 0)
	byPath := make(map[string]*testFileExclusionInfo)

	for _, variant := range variants {
		if variant == nil {
			continue
		}

		for i, file := range variant.Syntax {
			if i >= len(variant.CompiledGoFiles) {
				break
			}

			path := variant.CompiledGoFiles[i]
			if !strings.HasSuffix(strings.ToLower(filepath.Base(path)), "_test.go") {
				continue
			}

			cleaned := filepath.Clean(path)
			if _, seen := byPath[cleaned]; seen {
				continue
			}

			info := classifyTestFileForExclusion(file, variant.TypesInfo, cleaned)
			infos = append(infos, info)
			byPath[cleaned] = info
		}
	}

	// Seed the excluded set with every condition-(1) qualifier, then relax it: a qualifier a
	// RETAINED file references must stay compiled (condition 2). Promotion is a fixpoint — a
	// newly-retained file's own references can pull further qualifiers back in — over a set that
	// only ever shrinks, so it converges.
	excluded := make(map[string]bool)
	for _, info := range infos {
		if info.qualifies {
			excluded[info.path] = true
		}
	}

	for changed := true; changed; {
		changed = false

		usedByRetained := make(map[types.Object]bool)
		for _, info := range infos {
			if excluded[info.path] {
				continue
			}
			for object := range info.used {
				usedByRetained[object] = true
			}
		}

		for path := range excluded {
			for _, object := range byPath[path].declared {
				if usedByRetained[object] {
					delete(excluded, path)
					changed = true
					break
				}
			}
		}
	}

	return excluded
}

// seedProductionAliasLifts makes the production conversion's package-scope ALIAS LIFTS reachable
// from the test compilation — both halves of "reachable", which is why they are seeded together.
//
// Go's `type CorpusEntry = struct{Parent string; Path string; …}` (internal/fuzz's fuzz.go) has no
// C# spelling of its own, so the production conversion LIFTS the anonymous struct to a real nested
// type and reaches it through a compilation-scoped `global using CorpusEntry = …CorpusEntryᴛ1;`.
// `global using` is scoped to ONE compilation and a reference-model test project is a second one,
// so neither the name nor the lift crossed: every test-side reference fell through to `t.String()`
// and emitted raw GO syntax into a C# file — `Func<struct{Parent string; …}, error>` in
// minimize_test.cs and worker_test.cs, CS1031/CS1525/CS1003, with all 52 of internal/fuzz's
// verdicts behind it.
//
// The production package's OWN package_info.cs is the authority for both halves: it publishes
// `[assembly: GoTypeAlias("CorpusEntry", "go.@internal.fuzz_package.CorpusEntryᴛ1")]`, which gives
// the exact target the production compilation uses. Recording it in importedTypeAliases re-emits
// the `global using` into the test metadata file, and recording the TYPE in
// productionAliasLiftedTypes makes every renderer spell the alias (see liftedNameFor).
//
// Deliberately NARROW on both axes. Only an alias whose right-hand side is an anonymous
// struct/interface is seeded — that is exactly the set with no other spelling; a named RHS already
// renders through its own qualified name, and adding aliases for it would put avoidable
// `global using` names into the test compilation where a test-local type could collide. And only
// an alias the production package_info PUBLISHES is seeded, so a type is never rendered under a
// name the test compilation cannot resolve; an unexported alias to an anonymous struct publishes
// nothing and keeps the pre-existing route.
func seedProductionAliasLifts(pkg *packages.Package, productionInfoPath string) {
	if pkg == nil || pkg.Types == nil {
		return
	}

	published, err := parseExportedTypeAliases(productionInfoPath)

	if err != nil || len(published) == 0 {
		return
	}

	targets := make(map[string]string, len(published))

	for _, entry := range published {
		targets[entry[0]] = entry[1]
	}

	scope := pkg.Types.Scope()

	for _, name := range scope.Names() {
		typeName, ok := scope.Lookup(name).(*types.TypeName)

		if !ok || !typeName.IsAlias() {
			continue
		}

		target, published := targets[name]

		if !published {
			continue
		}

		// A test file's own alias lifts normally in THIS conversion; only the production
		// declarations are missing one.
		if strings.HasSuffix(pkg.Fset.Position(typeName.Pos()).Filename, "_test.go") {
			continue
		}

		resolved := types.Unalias(typeName.Type())

		switch underlying := resolved.(type) {
		case *types.Struct:
			if isEmptyStructType(underlying) {
				continue
			}
		case *types.Interface:
			if underlying.Empty() {
				continue
			}
		default:
			continue
		}

		packageLock.Lock()

		if productionAliasLiftedTypes == nil {
			productionAliasLiftedTypes = map[types.Type]string{}
		}

		productionAliasLiftedTypes[resolved] = name
		importedTypeAliases[name] = target
		packageLock.Unlock()
	}
}

// seedProductionInterfaceAliases makes the production conversion's DEFINED-OVER-INTERFACE types
// reachable from the test compilation — the second kind of package-level declaration that has a
// `global using` and no class member, alongside the anonymous-RHS alias lifts above.
//
// `type Token any` (encoding/xml) and `type Reader io.Reader` are DEFINED types in Go: each has its
// own identity and its own name. But each also has EXACTLY the right-hand interface's method set and
// can carry no methods of its own, so visitTypeSpec emits it as a compilation-scoped
// `global using ΔToken = object;` rather than as a member of the `<pkg>_package` class (see the
// definedOverInterface arm — a struct wrapper over `any` admits no implicit conversion from a
// concrete value, so the wrapper form was CS0029 at every assignment).
//
// A `-tests` conversion under a REFERENCE model is a SECOND compilation that declares no such alias,
// and its renderers reach the type as an ordinary production named type:
// `global::go.encoding.xml_package.ΔToken`, which qualifies an assembly-scoped alias as a type
// member. That is CS0426 — 36 of them in encoding/xml, its ONLY build error, with all 386 of the
// package's verdicts behind it. The production conversion never produces that spelling because it
// references the alias BARE, which is why the defect is invisible outside a `-tests` run.
//
// Both halves are seeded, exactly as seedProductionAliasLifts seeds them: the NAME into
// productionAliasLiftedTypes so every renderer spells the alias (liftedNameFor is consulted ahead of
// the white-box class qualifiers), and the TARGET into importedTypeAliases so the `global using` is
// re-emitted into the test metadata file and the name resolves there. Recording one without the
// other would render a name the test compilation cannot bind.
//
// The production package_info.cs is the authority for both, and its TWO-HOP chain is followed to the
// end — `GoTypeAlias("Token", "ΔToken")` then `GoTypeAlias("ΔToken", "object")`, because a type whose
// name collides with a method name is Δ-renamed and the alias the production compilation actually
// declares is the renamed one. This is the same chain a cross-package consumer already follows
// (loadImportedTypeAliases' localAliases), so the two readers of one published record agree.
//
// Excluded: the RECOMPILE model, for the one reason that matters — there the production `.cs` ARE
// compile items of the test assembly, so the alias is already declared in this compilation and
// re-declaring it would be the defect rather than the fix. Gated on testProductionPath, which is set
// only by the models that REFERENCE production.
func seedProductionInterfaceAliases(pkg *packages.Package, productionInfoPath string, options Options) {
	if pkg == nil || pkg.Types == nil || options.testProductionPath == "" {
		return
	}

	names := definedOverInterfaceTypeNames(pkg)

	if len(names) == 0 {
		return
	}

	published, err := parseExportedTypeAliases(productionInfoPath)

	if err != nil || len(published) == 0 {
		return
	}

	targets := make(map[string]string, len(published))

	for _, entry := range published {
		targets[entry[0]] = entry[1]
	}

	scope := pkg.Types.Scope()

	for _, name := range names {
		typeName, ok := scope.Lookup(name).(*types.TypeName)

		if !ok || typeName.IsAlias() {
			continue
		}

		named, isNamed := typeName.Type().(*types.Named)

		if !isNamed {
			continue
		}

		aliasName, target, resolved := followPublishedAliasChain(targets, name)

		if !resolved {
			continue
		}

		packageLock.Lock()

		if productionAliasLiftedTypes == nil {
			productionAliasLiftedTypes = map[types.Type]string{}
		}

		productionAliasLiftedTypes[named] = aliasName
		importedTypeAliases[aliasName] = target
		packageLock.Unlock()
	}
}

// definedOverInterfaceTypeNames returns the package-level type names the PRODUCTION files declare as
// a defined type over a NAMED interface — visitTypeSpec's definedOverInterface predicate, read from
// the same syntax that pass reads it from.
//
// The predicate needs the AST and cannot be recovered from go/types: `type X any` and
// `type X interface{}` are the same *types.Named over the same empty *types.Interface, yet the first
// emits a `global using` and the second emits a C# interface that IS a class member. Only the
// right-hand SYNTAX separates them, and convertTestVariant's package carries every production file's
// syntax because the whole variant feeds the package-wide analyses.
//
// `_test.go` declarations are excluded: a test file's own alias emits its `global using` into THIS
// compilation and needs no seeding.
func definedOverInterfaceTypeNames(pkg *packages.Package) []string {
	if pkg == nil || pkg.Types == nil || pkg.Fset == nil {
		return nil
	}

	scope := pkg.Types.Scope()
	names := []string{}

	for _, file := range pkg.Syntax {
		if strings.HasSuffix(strings.ToLower(pkg.Fset.Position(file.Pos()).Filename), "_test.go") {
			continue
		}

		for _, decl := range file.Decls {
			genDecl, isGen := decl.(*ast.GenDecl)

			if !isGen || genDecl.Tok != token.TYPE {
				continue
			}

			for _, spec := range genDecl.Specs {
				typeSpec, isType := spec.(*ast.TypeSpec)

				if !isType || typeSpec.Assign.IsValid() {
					continue
				}

				switch typeSpec.Type.(type) {
				case *ast.Ident, *ast.SelectorExpr:
				default:
					continue
				}

				obj, isTypeName := scope.Lookup(typeSpec.Name.Name).(*types.TypeName)

				if !isTypeName || obj.Type() == nil {
					continue
				}

				if _, isInterface := obj.Type().Underlying().(*types.Interface); isInterface {
					names = append(names, typeSpec.Name.Name)
				}
			}
		}
	}

	return names
}

// followPublishedAliasChain resolves a Go type name through the production package_info.cs's
// exported-alias records to the alias name that compilation DECLARES and that alias's target.
//
// A type whose name collides with a method name publishes TWO records — the rename
// (`"Token"` → `"ΔToken"`) and the alias itself (`"ΔToken"` → `"object"`) — so the name to spell is
// the last key in the chain, never the first. A type with no collision publishes one record and the
// chain ends immediately. The visited set bounds a malformed or self-referential published set,
// which is read from a file this run did not necessarily write.
func followPublishedAliasChain(targets map[string]string, name string) (string, string, bool) {
	target, published := targets[name]

	if !published {
		return "", "", false
	}

	visited := NewHashSet([]string{name})

	for {
		next, chained := targets[target]

		if !chained || visited.Contains(target) {
			return name, target, true
		}

		visited.Add(target)
		name, target = target, next
	}
}

// productionSeed carries the naming state the PRODUCTION conversion of this package left standing
// when it finished, so a `-tests` variant that emits into the SAME `<pkg>_package` class can
// continue from it rather than start over.
//
// Every member exists for one reason: under the RECOMPILE model the production `.cs` on disk are
// compile items of the test assembly and are NOT regenerated, so every package-scope name they
// already declare is immutable. A converter counter or claim set that restarts for the test
// emission pass re-mints one of those names and the class declares it twice. The seed is therefore
// the INTERNAL variant's alone (its files land in the production class); the EXTERNAL variant's
// `<pkg>_test_package` is a separate scope that may reuse every one of these names freely, and
// seeding it would only churn names for no compile-level reason.
//
// A zero value is the "no production half to continue from" case — an external variant, the
// reference model (production is a separate ASSEMBLY there, so nothing it declares can collide),
// and every direct unit-test call.
type productionSeed struct {
	// liftedTypeNames — the anonymous-struct/interface lifts already nested in the class.
	liftedTypeNames HashSet[string]

	// hoistedConstOrdinals — per Go const name, the `<name>ᶜ[ordinal]` big-constant fields claimed.
	hoistedConstOrdinals map[string]int

	// globalTempVarCounts — the package-scope generated-name counters (getGlobalTempVarName): the
	// blank identifier `_` (a blank package var, const or func becomes `_ᴛNʗ` / `_ᴛN`, since C#
	// has no package-scope discard) and the hidden `tupleᴛNʗ` holders. crypto/x509's pem_decrypt.cs
	// declares `_ᴛ1ʗ` for a blank const in an iota block, and oid_test.cs's
	// `var _ encoding.BinaryMarshaler = OID{}` re-minted the very same name into the very same
	// class — CS0102, one of the three roots that stood between that package and any verdict at all.
	globalTempVarCounts map[string]int

	// blankImportForces — the imported paths a `[GoInit] initᴛᴛblankImportꓸ…` hook was already
	// emitted for. The hook forces a blank-imported package's module constructor and is idempotent
	// by construction: exactly one per (assembly, imported package). A test file repeating a
	// production blank import is the ordinary case, not an exotic one — `x509.go` and `x509_test.go`
	// both blank-import `crypto/sha256` and `crypto/sha512`, and each half emitted the same hook
	// into the same partial class (CS0111 ×2). The PRODUCTION half owns the hook whenever its file
	// is in the compilation, because that file is the one this run cannot rewrite.
	blankImportForces HashSet[string]

	// initFuncs — how many Go `func init()` declarations the class already carries. Go allows any
	// number per package and C# needs a distinct name for each, so the first takes `init` and the
	// rest `initΔN`. The counter restarting for the test emission pass gives the test half's own
	// `func init()` the bare `init` a production file already declares: `crypto/x509`'s
	// windows/root_windows.cs and x509_test.go, CS0111 again. Same shape as globalTempVarCounts —
	// a per-class name supply that one emission pass must not restart.
	initFuncs int
}

// convertTestVariant converts one test package variant's _test.go files into C# in outputPath.
// The whole variant (production + test files) feeds the package-wide analyses so the test files
// convert with complete state, but only the test files are EMITTED here. The production .cs already
// exist from normal conversion and are either referenced or included later by recompile fallback.
//
// Files convert SEQUENTIALLY in pkg.Syntax order for byte-reproducible output, mirroring
// processConversion (the per-file visitors share package-level state claimed at visit time; the
// branch's concurrent goroutines reproduced exactly the nondeterminism master removed).
func convertTestVariant(pkg *packages.Package, testEntries []FileEntry, outputPath, projectNamespace string, seed productionSeed, options Options) ([]string, HashSet[string], error) {
	resetPackageState(pkg)
	packageNamespace = projectNamespace

	// The lifted type names the production conversion already claimed (see
	// productionLiftedTypeNames). Non-nil for the INTERNAL variant only — its test files emit into
	// the production `<pkg>_package` class, whose on-disk `.cs` are not regenerated here, so a lift
	// that reuses one of those names declares the nested type twice.
	productionLiftedTypeNames = seed.liftedTypeNames

	// Same production-pinned seeding for the hoisted big-constant field ordinals (see
	// productionHoistedConstOrdinals); claimHoistedConstFieldName folds it in on first claim.
	productionHoistedConstOrdinals = seed.hoistedConstOrdinals

	// The counters and claim sets that have no production-pinned mirror of their own are seeded
	// straight into the live state resetPackageState just cleared: nothing else reads them, so a
	// second global would carry no information the live set cannot. All three say the same thing —
	// this emission pass CONTINUES the production one rather than restarting it (see productionSeed).
	for prefix, count := range seed.globalTempVarCounts {
		globalTempVarCount[prefix] = count
	}

	for importPath := range seed.blankImportForces {
		packageBlankImportForces.Add(importPath)
	}

	initFuncCounter = seed.initFuncs

	// The package under test is RECOMPILED into this assembly, so a record naming one of its types
	// through its fully-qualified class (how an external `<name>_test` variant renders it, having
	// reached it by import path) is naming a LOCAL type — and must emit in the same bare form the
	// seeded production metadata uses, or the two spellings of one resolved pair survive as two
	// GoImplement records and go2cs-gen defines the adapter twice. See stripLocalTypeQualifier.
	if options.testPackageName != "" {
		testLocalTypePrefixes = []string{packageNamespace + "." + getSanitizedImport(options.testPackageName+PackageSuffix)}
	}

	// Load the PRODUCTION package's own GoImplement pairs from its (colocated, already-seeded)
	// package_info.cs (B4/B5): visitImportSpec skips the package-under-test alias load — its
	// types bind locally — which also skipped these sets, so an EXTERNAL test file's cast of a
	// production type could not see the seeded adapters and re-recorded the pair. Must run per
	// variant: resetPackageState above just cleared the sets.
	//
	// A REFERENCE model clears testPackageName/Path (that is what makes production bind as an
	// ordinary import), so the name comes from testProductionName there. The INTERNAL white-box
	// bridge is why this cannot be left to visitImportSpec: it is the SAME Go package, so it never
	// imports production, yet production is a referenced assembly whose own partials already
	// realize its records. Without them the bridge re-records every production value cast and
	// constructs a redundant ᴠ adapter — a DIFFERENT identity than the value production's own code
	// returns, which encoding/hex catches at `err != tt.err`. The external variant loads these
	// through its import too; the sets are idempotent.
	productionName := options.testPackageName

	if productionName == "" {
		productionName = options.testProductionName
	}

	if productionName != "" {
		productionInfoPath := platformPackageInfoPath(outputPath, goosOfTarget(options.targetPlatform))
		loadPackageImplements(productionInfoPath, productionName)
		seedProductionAliasLifts(pkg, productionInfoPath)
		seedProductionInterfaceAliases(pkg, productionInfoPath, options)
	}

	allEntries := make([]FileEntry, 0, len(pkg.Syntax))
	entryByPath := make(map[string]*FileEntry, len(pkg.Syntax))

	for i, file := range pkg.Syntax {
		if i >= len(pkg.CompiledGoFiles) {
			break
		}

		allEntries = append(allEntries, newFileEntry(file, pkg.CompiledGoFiles[i], false))
		entryByPath[filepath.Clean(pkg.CompiledGoFiles[i])] = &allEntries[len(allEntries)-1]
	}

	selected := make([]FileEntry, 0, len(testEntries))
	for _, requested := range testEntries {
		entry := entryByPath[filepath.Clean(requested.filePath)]
		if entry == nil {
			continue
		}

		outputFile := filepath.Join(outputPath, strings.TrimSuffix(filepath.Base(entry.filePath), ".go")+".cs")
		manual, err := containsManualConversionMarker(outputFile)
		if err != nil {
			return nil, nil, err
		}

		// A hand-owned (GoManualConversion-marked) test `.cs` is never overwritten, but its
		// source stays in the convert set — its visit feeds package-wide emission state that
		// sibling files depend on; only its EMISSION redirects, to the non-compiled `.cs.auto`
		// review sibling. Same semantics as processConversion's marked-file flow — dropping the
		// visit corrupts sibling emission.
		// The copy shares the analysis maps with the allEntries element (maps are references).
		selectedEntry := *entry
		selectedEntry.manualConversion = manual
		selected = append(selected, selectedEntry)
	}

	if len(selected) == 0 {
		return nil, HashSet[string]{}, nil
	}

	// A `_test.go` in the variant's syntax that the caller did NOT select for emission is a
	// Phase-4D compile-excluded file (Example/Benchmark-only, selectCompileExcludedTestFiles):
	// it participates in analysis but renders no C#, so it must never claim a hoisted literal
	// field (see FileEntry.emissionExcluded). Production files are already claim-fenced by the
	// hoist collector's seeded-mode emitted check.
	selectedPaths := make(map[string]bool, len(selected))
	for _, entry := range selected {
		selectedPaths[filepath.Clean(entry.filePath)] = true
	}

	for i := range allEntries {
		path := filepath.Clean(allEntries[i].filePath)

		if strings.HasSuffix(strings.ToLower(path), "_test.go") && !selectedPaths[path] {
			allEntries[i].emissionExcluded = true
		}
	}

	globalIdentNames := make(map[*ast.Ident]string)
	globalScope := map[string]*types.Var{}

	// Mirror processConversion's package-wide analysis sequence — a test file is an ordinary Go
	// file and needs the same emission inputs (collectMovedInitVars runs below, after the hoist
	// collection whose reader set feeds its dependency graph — same ordering as processConversion).
	performNameCollisionAnalysis(pkg)

	for _, entry := range allEntries {
		performGlobalVariableAnalysis(entry.file.Decls, pkg.TypesInfo, globalIdentNames, globalScope)
	}

	collectCaptureModeMethods(pkg)
	collectTypeSpecRHS(pkg)

	// The production-file entry list (manual-conversion flags resolved against the production
	// `.cs` on disk) — input to the ref-lowering classification, the hoist seed, and every other
	// production-only sub-pass below.
	prodEntries := make([]FileEntry, 0, len(allEntries))

	for _, entry := range allEntries {
		if strings.HasSuffix(strings.ToLower(entry.filePath), "_test.go") {
			continue
		}

		prodEntry := entry
		prodOutput := filepath.Join(outputPath, strings.TrimSuffix(filepath.Base(entry.filePath), ".go")+".cs")

		if manual, err := containsManualConversionMarker(prodOutput); err == nil {
			prodEntry.manualConversion = manual
		}

		prodEntries = append(prodEntries, prodEntry)
	}

	// ж-box A2 (three-driver rule, DESIGN-zh-box-reduction §3.5): the ref-lowering classification
	// runs in the -tests driver too — over the PRODUCTION files only (prodEntries; the entry point
	// additionally filters `_test.go` structurally), so the merged white-box package's test-side
	// func-value aliases can never desynchronize this classification from the -stdlib emission's
	// (§3.5's determinism invariant). The EXTERNAL variant carries no production files and records
	// an empty result. Runs BEFORE escape analysis, which consults the reversion verdicts; the
	// signature/call-site emission reads the lowered sets during the visits.
	performRefLoweringAnalysis(prodEntries, pkg.Types, pkg.TypesInfo, options)

	performEscapeAnalysis(allEntries, pkg.Fset, pkg.Types, pkg.TypesInfo)
	collectAddressedGlobals(allEntries, pkg.Types, pkg.TypesInfo)
	computeImportAliasRenames(allEntries, pkg.Types, packageNamespace)
	collectPublicizedTypes(pkg.Types)
	preloadImportedTypeAliases(allEntries, options)

	// Tier C hoisted string literals (§4.4's `-tests` invariants). The INTERNAL test variant
	// recompiles the package under test into this assembly and emits into the SAME package class,
	// so its test files must REFERENCE the fields the production `.cs` on disk already declares —
	// never re-declare them (a `_test.go` can sort BEFORE its production owner, so this, not name
	// luck, is what prevents CS0102). The production map is recomputed from the production files
	// exactly as processConversion computed it (same collector, same order, same manual-conversion
	// flags), then handed to the real pass as a seed; only `_test.go` files may claim a NEW field.
	// The EXTERNAL variant carries no production files, so its seed is empty and its own class
	// (`<pkg>_test_package`) claims freely — which is required, since a production field is
	// `private` to a different class.

	// The seed run SIMULATES processConversion, which does relocate an out-of-order initializer
	// (initOrderRelocated=true), so it reproduces the production `.cs` on disk exactly. The real
	// run STILL passes false even though relocation now runs here (collectMovedInitVars below):
	// suppressing test-file hoists in initializer-reachable functions is a pure (and tiny)
	// allocation pessimization, never a correctness risk, and flipping the flag would drift the
	// hoist claims of every banked *_test.cs for no behavioral gain.
	collectHoistedLiterals(prodEntries, pkg.Types, pkg.TypesInfo, goosOfTarget(options.targetPlatform), nil, true)
	productionHoistSeed := packageHoistNames
	collectHoistedLiterals(allEntries, pkg.Types, pkg.TypesInfo, goosOfTarget(options.targetPlatform), productionHoistSeed, false)

	// Find test-file package-level var initializers whose Go dependency order C#'s
	// static-field-initializer order cannot reproduce — the same pass processConversion runs
	// (three-drivers rule), over the whole variant package so an internal-variant test var that
	// reads a production var cross-file (gob's basicTypes over type.go's tBool…) relocates too.
	// Production vars it flags are never re-emitted here (only test files convert below), so
	// only test-file relocations reach packageMovedInitMethods; the ordered assignments are
	// emitted by writeTestVariantInitFile after the convert loop. First demonstrated consumer:
	// internal/fmtsort's sort_test.go (compareTests reads chans/ints declared later in the file
	// — every test died in the class cctor on the default slice).
	collectMovedInitVars(pkg.Fset, pkg.Types, pkg.TypesInfo, pkg.Syntax)

	var compileNames []string // emitted test .cs basenames — the csproj's compile items
	var resolveNames []string // every emission (incl. .cs.auto review siblings) for marker resolution

	convert := func(entry FileEntry) (err error) {
		if !options.debugMode {
			defer func() {
				if r := recover(); r != nil {
					err = fmt.Errorf("convert test file %q: %v", entry.filePath, r)
				}
			}()
		}

		visitor := newFileVisitor(pkg.Fset, pkg.Types, pkg.TypesInfo, options, globalIdentNames, globalScope, entry)
		visitor.visitFile(entry.file)

		baseName := strings.TrimSuffix(filepath.Base(entry.filePath), ".go")

		if entry.manualConversion {
			// Hand-owned destination: the visit above already fed this file's package-wide state;
			// emit the auto conversion to the `.cs.auto` review sibling, leaving the marked `.cs`
			// untouched. The HAND-OWNED `.cs` is the compile item; the `.cs.auto` sibling never is.
			outputName := filepath.Join(outputPath, baseName+".cs.auto")
			if writeErr := writeAutoConversionSibling(outputName, baseName, visitor.outputBuilder.String()); writeErr != nil {
				showWarning("%s", writeErr)
			}

			projectImports.UnionWithSet(visitor.importQueue)
			compileNames = append(compileNames, baseName+".cs")
			resolveNames = append(resolveNames, outputName)
			return nil
		}

		outputName := filepath.Join(outputPath, baseName+".cs")
		if writeErr := visitor.writeOutputFile(outputName); writeErr != nil {
			return writeErr
		}

		projectImports.UnionWithSet(visitor.importQueue)
		compileNames = append(compileNames, filepath.Base(outputName))
		resolveNames = append(resolveNames, outputName)
		return nil
	}

	for _, entry := range selected {
		if err := convert(entry); err != nil {
			return nil, nil, err
		}
	}

	// Emit the variant's ordered relocated-initializer file (no-op unless the convert loop
	// recorded any relocation). The internal variant shares the production `<pkg>_package`
	// class: when the production package_init.cs exists it owns the single static-ctor slot,
	// so the test side implements its erasable partial hook instead of a second ctor. The
	// `_test.cs` suffix keeps the file out of the production csproj's compile set (the IP-4
	// test-artifact exclusion) — it compiles only into the test assembly.
	if len(packageMovedInitMethods) > 0 {
		isExternalVariant := strings.HasSuffix(pkg.Name, "_test")
		variantKind := "internal"

		if isExternalVariant {
			variantKind = "external"
		}

		initFileName := fmt.Sprintf("package_init_%s_test.cs", variantKind)
		variantClassName := getSanitizedImport(pkg.Name + PackageSuffix)
		implementHook := false

		if options.testClassNameOverride != "" {
			variantClassName = options.testClassNameOverride
		} else if !isExternalVariant {
			// Layout L3 puts the production package_init.cs in the package's per-GOOS folder (Go's
			// InitOrder differs when the file set does — conversionDriver.go), so this must ask
			// where the tree actually keeps it. A flat-only probe answered "no production ctor" for
			// every L3 package and emitted a SECOND `static <pkg>_package()` beside the real one:
			// CS0111 on the constructor itself, once crypto/x509's platform folder started
			// compiling into its test assembly at all.
			_, statErr := os.Stat(platformLayoutPath(outputPath, goosOfTarget(options.targetPlatform), PackageInitFileName))
			implementHook = statErr == nil
		}

		if err := writeTestVariantInitFile(outputPath, initFileName, packageNamespace, variantClassName, implementHook); err != nil {
			return nil, nil, err
		}

		compileNames = append(compileNames, initFileName)
	}

	resolveDynamicTypeMarkers(resolveNames)

	// Adapter names cannot resolve here: this variant's GoImplement records are not merged into
	// package_test_info.cs until the caller writes it, and a name depends on the FINAL set. Hand
	// the emission PATHS up (compileNames are csproj-relative) for the caller's single pass.
	testAdapterResolveNames = append(testAdapterResolveNames, resolveNames...)

	return compileNames, NewHashSet(projectImports.Keys()), nil
}

// appendExternalTestPackageClass appends the external test package's [GoPackage] partial class
// declaration to package_test_info.cs — converted external-test files declare partial pieces of
// <name>_test_package, and this block is the attribute-bearing anchor the production
// package_info.cs provides for the production class. It also widens the file's `using static`
// scope to that class (B3): metadata attributes merged from the test variants (GoImplement /
// GoImplicitConv) can reference types DECLARED in the external test package (e.g. an errWriter
// helper cast to io.Writer), which the seeded production-only `using static <ns>.<pkg>_package;`
// cannot resolve — CS0246 on every such attribute argument.
func appendExternalTestPackageClass(testInfoPath, packageNamespace, productionPackageName string, external *packages.Package) error {
	if external == nil {
		return nil
	}

	data, err := os.ReadFile(testInfoPath)
	if err != nil {
		return fmt.Errorf("read test package metadata: %w", err)
	}

	// EOL-agnostic: package_test_info.cs is READ BACK off disk, and for a validated package it is a
	// COMMITTED file, so its line endings are the checkout's rather than the converter's. Every test
	// below is CRLF-shaped — the `block` literal and the using-directive insert — so an LF copy makes
	// `strings.Contains(contents, block)` miss and the [GoPackage] class is appended AGAIN on every
	// run, accumulating duplicate declarations. writePackageInfoFile emits this file uniformly CRLF
	// (one "\r\n" per line, no exceptions), which is why normalizing to CRLF is the faithful
	// reconstruction rather than a guess, and why it is inert on a Windows checkout: the content is
	// already CRLF, so `contents == string(data)` still short-circuits the write below.
	// F3 in docs/PLAN-linux-operation.md.
	contents := normalizeToCRLF(string(data))
	className := getSanitizedImport(external.Name + PackageSuffix)

	productionUsing := fmt.Sprintf("using static %s.%s;", packageNamespace, getSanitizedImport(productionPackageName+PackageSuffix))
	testUsing := fmt.Sprintf("using static %s.%s;", packageNamespace, className)

	if !strings.Contains(contents, testUsing) {
		if !strings.Contains(contents, productionUsing) {
			return fmt.Errorf("seeded test package metadata %q is missing the production using directive %q", testInfoPath, productionUsing)
		}
		contents = strings.Replace(contents, productionUsing, productionUsing+"\r\n"+testUsing, 1)
	}

	block := fmt.Sprintf("\r\n[GoPackage(\"%s\")]\r\npublic static partial class %s\r\n{\r\n}\r\n", external.Name, className)

	if !strings.Contains(contents, block) {
		contents += block
	}

	if contents == string(data) {
		return nil
	}

	return os.WriteFile(testInfoPath, []byte(contents), 0644)
}

// conversionRecordSet snapshots the package-scoped GoImplement/GoImplicitConv record globals so
// the external test variant's records can be written through the shared writePackageInfoFile in
// TWO passes with different anchors (B4/B5) — the writer reads the live globals, so each pass
// installs its partition.
type conversionRecordSet struct {
	interfaceImplements map[string]HashSet[string]
	promotedImplements  map[string]HashSet[string]
	proxies             map[string][2]string
	implicitConvs       map[string]HashSet[string]
	invertedConvs       map[string]HashSet[string]
	indirectConvs       map[string]HashSet[string]
	numericConvs        map[string]map[string]string
	indirectNumerics    map[string]map[string]string
}

func newConversionRecordSet() conversionRecordSet {
	return conversionRecordSet{
		interfaceImplements: make(map[string]HashSet[string]),
		promotedImplements:  make(map[string]HashSet[string]),
		proxies:             make(map[string][2]string),
		implicitConvs:       make(map[string]HashSet[string]),
		invertedConvs:       make(map[string]HashSet[string]),
		indirectConvs:       make(map[string]HashSet[string]),
		numericConvs:        make(map[string]map[string]string),
		indirectNumerics:    make(map[string]map[string]string),
	}
}

func (r conversionRecordSet) install() {
	interfaceImplementations = r.interfaceImplements
	promotedInterfaceImplementations = r.promotedImplements
	constraintProxies = r.proxies
	implicitConversions = r.implicitConvs
	invertedImplicitConversions = r.invertedConvs
	indirectImplicitConversions = r.indirectConvs
	numericConversions = r.numericConvs
	indirectNumericConversions = r.indirectNumerics
}

func (r conversionRecordSet) isEmpty() bool {
	return len(r.interfaceImplements) == 0 && len(r.promotedImplements) == 0 &&
		len(r.proxies) == 0 && len(r.implicitConvs) == 0 && len(r.invertedConvs) == 0 &&
		len(r.indirectConvs) == 0 && len(r.numericConvs) == 0 && len(r.indirectNumerics) == 0
}

// isTestAnchoredImplementRecord decides which -tests metadata anchor hosts an EXTERNAL variant
// GoImplement record (B4/B5). The go2cs-gen generators host generated code in the FIRST class
// of the attribute-bearing file, so anchoring is dictated by where each record's generated form
// must land:
//   - an adapter-CLASS record (interface-sourced or foreign-value ᴠ adapters, per
//     adapterClassImplementations; and every ж pointer adapter for a non-production type) is
//     referenced BARE from test-file cast sites, which are partial pieces of the test package
//     class — the adapter must be its member;
//   - a BARE impl name is a type declared in the external test package itself — its generated
//     partial struct must merge with that declaration in the test package class;
//   - a PRODUCTION-qualified record (`sort_package.IntSlice`, its rooted form, or its
//     namespace-relative form `math.rand_package.Rand`) generates a partial/adapter on the
//     production class — it stays with the production-anchored package_test_info.cs, whose first
//     class is the production class.
func isTestAnchoredImplementRecord(ifaceName, implName, productionClassName string) bool {
	if adapterClassImplementations.Contains(ifaceName + "|" + implName) {
		return true
	}

	inner := implName
	pointerForm := false

	if trimmed, ok := strings.CutPrefix(inner, PointerPrefix+"<"); ok {
		inner = strings.TrimSuffix(trimmed, ">")
		pointerForm = true
	}

	if !strings.Contains(inner, ".") {
		return true
	}

	if pointerForm {
		inner = strings.TrimPrefix(inner, "global::")

		if strings.HasPrefix(inner, productionClassName+".") ||
			strings.HasPrefix(inner, packageNamespace+"."+productionClassName+".") {
			return false
		}

		// The live records qualify the implementer NAMESPACE-RELATIVE — without the `go.` root —
		// so a NESTED package's production type arrives as `math.rand_package.Rand`, matching
		// neither form above and landing in the wrong anchor. (A TOP-LEVEL package worked by
		// accident: its relative qualifier IS the bare `sort_package.` form.) Recognize the
		// relative qualifier so nested packages keep production types production-anchored.
		if relative, ok := strings.CutPrefix(packageNamespace, RootNamespace+"."); ok {
			return !strings.HasPrefix(inner, relative+"."+productionClassName+".")
		}

		return true
	}

	return false
}

// isTestAnchoredConversionRecord decides the anchor for an EXTERNAL variant GoImplicitConv
// record: the generated conversion operators live inside a partial declaration of one of the
// two types, so a pair involving ANY test-package-local (bare) type must anchor to the test
// package class; a pair between qualified (production/foreign) types keeps the production
// anchor, matching the pre-split emission.
func isTestAnchoredConversionRecord(sourceType, targetType string) bool {
	isBare := func(name string) bool {
		if trimmed, ok := strings.CutPrefix(name, PointerPrefix+"<"); ok {
			name = strings.TrimSuffix(trimmed, ">")
		}

		return !strings.Contains(name, ".")
	}

	return isBare(sourceType) || isBare(targetType)
}

// splitExternalVariantRecords partitions the LIVE record globals (the external variant's
// collected records) into the test-anchored and production-anchored sets (B4/B5).
func splitExternalVariantRecords(productionClassName string) (testAnchored, productionAnchored conversionRecordSet) {
	testAnchored = newConversionRecordSet()
	productionAnchored = newConversionRecordSet()

	splitImplements := func(source map[string]HashSet[string], test, production map[string]HashSet[string]) {
		for ifaceName, implementations := range source {
			for implementation := range implementations {
				target := production

				if isTestAnchoredImplementRecord(ifaceName, implementation, productionClassName) {
					target = test
				}

				if existing, ok := target[ifaceName]; ok {
					existing.Add(implementation)
				} else {
					target[ifaceName] = NewHashSet([]string{implementation})
				}
			}
		}
	}

	splitImplements(interfaceImplementations, testAnchored.interfaceImplements, productionAnchored.interfaceImplements)
	splitImplements(promotedInterfaceImplementations, testAnchored.promotedImplements, productionAnchored.promotedImplements)

	for key, proxy := range constraintProxies {
		if isTestAnchoredConversionRecord(proxy[0], proxy[1]) {
			testAnchored.proxies[key] = proxy
		} else {
			productionAnchored.proxies[key] = proxy
		}
	}

	splitConversions := func(source map[string]HashSet[string], test, production map[string]HashSet[string]) {
		for sourceType, targetTypes := range source {
			for targetType := range targetTypes {
				target := production

				if isTestAnchoredConversionRecord(sourceType, targetType) {
					target = test
				}

				if existing, ok := target[sourceType]; ok {
					existing.Add(targetType)
				} else {
					target[sourceType] = NewHashSet([]string{targetType})
				}
			}
		}
	}

	splitConversions(implicitConversions, testAnchored.implicitConvs, productionAnchored.implicitConvs)
	splitConversions(invertedImplicitConversions, testAnchored.invertedConvs, productionAnchored.invertedConvs)
	splitConversions(indirectImplicitConversions, testAnchored.indirectConvs, productionAnchored.indirectConvs)

	splitNumerics := func(source map[string]map[string]string, test, production map[string]map[string]string) {
		for sourceType, targetTypes := range source {
			for targetType, valueType := range targetTypes {
				target := production

				if isTestAnchoredConversionRecord(sourceType, targetType) {
					target = test
				}

				if existing, ok := target[sourceType]; ok {
					existing[targetType] = valueType
				} else {
					target[sourceType] = map[string]string{targetType: valueType}
				}
			}
		}
	}

	splitNumerics(numericConversions, testAnchored.numericConvs, productionAnchored.numericConvs)
	splitNumerics(indirectNumericConversions, testAnchored.indirectNumerics, productionAnchored.indirectNumerics)

	return testAnchored, productionAnchored
}

// externalTestPackageInfoSeed composes the initial contents of package_info_external_test.cs. The
// structure mirrors package_info-template.txt (the shared writer requires all four marker
// sections); the FIRST — and only — class declaration is the external test package class, which
// is what the go2cs-gen generators anchor generated adapters and partials to
// (GetFirstClassName). The class is declared WITHOUT [GoPackage]: the attribute-bearing partial
// lives in package_test_info.cs (appendExternalTestPackageClass), and duplicating the attribute
// on a second partial declaration is CS0579. Both `using static` scopes are included so
// attribute arguments resolve exactly as they do in package_test_info.cs.
// internalTestPackageInfoSeed composes the initial contents of package_info_internal_test.cs —
// the WHITE-BOX bridge's metadata anchor. A mixed suite's test compilation carries TWO classes
// that generated code must merge into: the external test class (package_test_info.cs) and the
// internal bridge. The generators host output in the FIRST class of the attribute-bearing file,
// so a record whose generated partial must merge with a bridge-declared type needs a file whose
// first class IS the bridge — anchoring it in the external class would declare a phantom empty
// type there instead (the same B4/B5 reasoning that gives the recompile model its two files).
// This is also the bridge's ONE `static` declaration (its .cs parts are bare `partial class`),
// and its ONE `[GoPackage]` carrier — no other partial declares the attribute, so no CS0579.
func internalTestPackageInfoSeed(projectNamespace, productionClassName, bridgeClassName, goPackageName string) string {
	var b strings.Builder

	b.WriteString("// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /\r\n")
	b.WriteString("// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type\r\n")
	b.WriteString("// anchor here — the source generators host output in the first class of the\r\n")
	b.WriteString("// attribute-bearing file, and only this file's first class is the bridge. Records for\r\n")
	b.WriteString("// production and external-test types stay in package_test_info.cs.\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <ImportedTypeAliases>\r\n")
	b.WriteString("// </ImportedTypeAliases>\r\n")
	b.WriteString("\r\n")
	b.WriteString("using go;\r\n")
	b.WriteString(fmt.Sprintf("using static %s.%s;\r\n", projectNamespace, productionClassName))
	b.WriteString(fmt.Sprintf("using static %s.%s;\r\n", projectNamespace, bridgeClassName))
	b.WriteString("\r\n")
	b.WriteString("// <ExportedTypeAliases>\r\n")
	b.WriteString("// </ExportedTypeAliases>\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <InterfaceImplementations>\r\n")
	b.WriteString("// </InterfaceImplementations>\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <ImplicitConversions>\r\n")
	b.WriteString("// </ImplicitConversions>\r\n")
	b.WriteString("\r\n")
	b.WriteString(fmt.Sprintf("namespace %s;\r\n", projectNamespace))
	b.WriteString("\r\n")
	b.WriteString(fmt.Sprintf("[GoPackage(\"%s\")]\r\n", goPackageName))
	b.WriteString(fmt.Sprintf("public static partial class %s\r\n{\r\n}\r\n", bridgeClassName))

	return b.String()
}

// splitWhiteboxVariantRecords partitions the LIVE record globals between the bridge anchor and
// the test anchor. The discriminator is the record participant's spelling plus the bridge's
// declared-name set: a BARE name declared by an internal _test.go file is a bridge type, whose
// generated partial must merge inside the bridge class; every other record — production-qualified,
// foreign, or bare-but-external-declared — anchors to the test class as before.
//
// ⚠ A BARE name resolves in the scope of the variant that RECORDED it, so the declared-name set
// may only be consulted while splitting the BRIDGE variant's own records (bridgeVariant). Each
// variant's records are split as they are collected, and every cross-variant reference is routed
// by go/types.Object identity to a CLASS-QUALIFIED spelling (whiteboxBridgeNamedType renders an
// internal-test type the external suite names as `global::<ns>.<pkg>_internal_test_package.T`;
// whiteboxProductionObject does the mirror while the bridge converts) — so a bare name recorded by
// the external suite is external-declared by construction, whatever the bridge happens to declare
// under the same simple name. Matching it against the bridge's set anchors the record at the
// bridge, where the OTHER participant is out of scope: encoding/gob declares `Point` in both
// variants (codec_test.go and example_interface_test.go), and the external pair
// `Point → Pythagoras` landed in package_info_internal_test.cs with `Pythagoras` — external-only —
// unqualified, CS0246 with no test host and all 106 verdicts empty. (Write-time qualification
// cannot repair it: qualifyAmbiguousTestTypeRefs roots an ambiguous bare name at the file it is
// ALREADY being written into, so a mis-anchored record is merely qualified to the wrong variant.)
func splitWhiteboxVariantRecords(bridgeTypeNames HashSet[string], bridgeVariant bool) (bridgeAnchored, testAnchored conversionRecordSet) {
	bridgeAnchored = newConversionRecordSet()
	testAnchored = newConversionRecordSet()

	isBridgeName := func(name string) bool {
		if !bridgeVariant {
			return false
		}

		if trimmed, ok := strings.CutPrefix(name, PointerPrefix+"<"); ok {
			name = strings.TrimSuffix(trimmed, ">")
		}

		return !strings.Contains(name, ".") && bridgeTypeNames.Contains(strings.TrimPrefix(name, ShadowVarMarker))
	}

	splitImplements := func(source map[string]HashSet[string], bridge, test map[string]HashSet[string]) {
		for ifaceName, implementations := range source {
			for implementation := range implementations {
				target := test

				// EITHER side being bridge-declared anchors the record at the bridge: a bridge
				// implementer needs its partial-struct there, and a bridge INTERFACE with a
				// foreign implementer needs its conversion operator on the interface partial —
				// encoding/binary's `TestByteOrder_byteOrder` ← `binary_package.bigEndian`.
				if isBridgeName(implementation) || isBridgeName(ifaceName) {
					target = bridge
				}

				if existing, ok := target[ifaceName]; ok {
					existing.Add(implementation)
				} else {
					target[ifaceName] = NewHashSet([]string{implementation})
				}
			}
		}
	}

	splitImplements(interfaceImplementations, bridgeAnchored.interfaceImplements, testAnchored.interfaceImplements)
	splitImplements(promotedInterfaceImplementations, bridgeAnchored.promotedImplements, testAnchored.promotedImplements)

	for key, proxy := range constraintProxies {
		if isBridgeName(proxy[0]) || isBridgeName(proxy[1]) {
			bridgeAnchored.proxies[key] = proxy
		} else {
			testAnchored.proxies[key] = proxy
		}
	}

	splitConversions := func(source map[string]HashSet[string], bridge, test map[string]HashSet[string]) {
		for sourceType, targetTypes := range source {
			for targetType := range targetTypes {
				target := test

				if isBridgeName(sourceType) || isBridgeName(targetType) {
					target = bridge
				}

				if existing, ok := target[sourceType]; ok {
					existing.Add(targetType)
				} else {
					target[sourceType] = NewHashSet([]string{targetType})
				}
			}
		}
	}

	splitConversions(implicitConversions, bridgeAnchored.implicitConvs, testAnchored.implicitConvs)
	splitConversions(invertedImplicitConversions, bridgeAnchored.invertedConvs, testAnchored.invertedConvs)
	splitConversions(indirectImplicitConversions, bridgeAnchored.indirectConvs, testAnchored.indirectConvs)

	splitNumerics := func(source map[string]map[string]string, bridge, test map[string]map[string]string) {
		for sourceType, targetTypes := range source {
			for targetType, valueType := range targetTypes {
				target := test

				if isBridgeName(sourceType) || isBridgeName(targetType) {
					target = bridge
				}

				if existing, ok := target[sourceType]; ok {
					existing[targetType] = valueType
				} else {
					target[sourceType] = map[string]string{targetType: valueType}
				}
			}
		}
	}

	splitNumerics(numericConversions, bridgeAnchored.numericConvs, testAnchored.numericConvs)
	splitNumerics(indirectNumericConversions, bridgeAnchored.indirectNumerics, testAnchored.indirectNumerics)

	return bridgeAnchored, testAnchored
}

// writeWhiteboxVariantMetadata merges a WHITE-BOX variant's live metadata globals into the two
// -tests anchor files: bridge-anchored records into package_info_internal_test.cs (first class:
// the bridge), everything else into package_test_info.cs (first class: the external test class).
// Alias globals are stashed around the bridge-unit write for the same CS1537 reason
// writeExternalVariantMetadata stashes them, and the accessibility section never reaches the
// bridge unit — bridge-declared types carry their accessibility inline (testInlineTypeAccess).
// Returns the unit's file name when it was written, or "" when this variant contributed no
// bridge-anchored records. bridgeVariant states which variant collected the live records — the
// bridge's declared-name set only resolves ITS own bare spellings (splitWhiteboxVariantRecords).
func writeWhiteboxVariantMetadata(testInfoPath, outputPath, productionClassName, bridgeClassName, goPackageName, internalAnchor, testAnchor string, bridgeTypeNames HashSet[string], bridgeVariant bool) (string, error) {
	bridgeAnchored, testAnchored := splitWhiteboxVariantRecords(bridgeTypeNames, bridgeVariant)

	// Both anchored writes below are reference-model files: their anchor class IS the local
	// type scope, and the production class is a referenced assembly.
	metadataAnchorLocalTypes = true

	unitName := ""

	// The bridge unit is written whether or not this variant contributed records, because the
	// file is not only a metadata anchor — it is the ONLY place `<pkg>_internal_test_package` is
	// declared `public static partial`. Every converted SOURCE file opens its package class bare
	// (`partial class X {`) by design; the modifier lives in the metadata file, exactly as it does
	// for the production and external-test classes. A record-less bridge therefore had no static
	// declaration at all, and an internal test file declaring an extension method is then CS1106 —
	// `internal/syscall/windows/registry`'s `export_test.go`, whose whole 6-verdict suite sat
	// behind `func (k Key) SetValue(…)`. Banked mixed suites only appear to escape it: `sort`,
	// `bytes` and `strings` each happen to have a go2cs-gen RecvGenerator file that re-declares
	// the class `public static partial`, i.e. a GENERATOR supplying a modifier the emitter owes.
	// A bridge with no records writes an anchor whose sections are all empty, which is what the
	// production and external-test seeds already do in the same situation.
	{
		unitPath := filepath.Join(outputPath, internalTestPackageInfoFileName)

		if _, err := os.Stat(unitPath); os.IsNotExist(err) {
			seed := internalTestPackageInfoSeed(packageNamespace, productionClassName, bridgeClassName, goPackageName)

			if err := os.WriteFile(unitPath, []byte(seed), 0644); err != nil {
				return "", fmt.Errorf("seed internal test package metadata: %w", err)
			}
		}

		savedImported, savedExported := importedTypeAliases, exportedTypeAliases
		savedAccess := packageEmittedTypeAccess
		importedTypeAliases = map[string]string{}
		exportedTypeAliases = map[string]string{}
		packageEmittedTypeAccess = HashSet[string]{}

		bridgeAnchored.install()
		metadataAnchorClassPrefix = internalAnchor
		writePackageInfoFile(unitPath, true)

		importedTypeAliases, exportedTypeAliases = savedImported, savedExported
		packageEmittedTypeAccess = savedAccess
		unitName = internalTestPackageInfoFileName
	}

	testAnchored.install()
	metadataAnchorClassPrefix = testAnchor
	writePackageInfoFile(testInfoPath, true)

	return unitName, nil
}

func externalTestPackageInfoSeed(projectNamespace, productionClassName, testClassName string) string {
	var b strings.Builder

	b.WriteString("// go2cs metadata anchor for the EXTERNAL test package (<name>_test): GoImplement /\r\n")
	b.WriteString("// GoImplicitConv attributes recorded by its converted _test files whose GENERATED code\r\n")
	b.WriteString("// (adapter classes, partial-struct implementations, conversion operators) must anchor to\r\n")
	b.WriteString("// the test package class — the source generators host output in the first class of the\r\n")
	b.WriteString("// attribute-bearing file, and test-file cast sites reference the adapters as members of\r\n")
	b.WriteString("// the test package class. Production-anchored records stay in package_test_info.cs.\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <ImportedTypeAliases>\r\n")
	b.WriteString("// </ImportedTypeAliases>\r\n")
	b.WriteString("\r\n")
	b.WriteString("using go;\r\n")
	b.WriteString(fmt.Sprintf("using static %s.%s;\r\n", projectNamespace, productionClassName))
	b.WriteString(fmt.Sprintf("using static %s.%s;\r\n", projectNamespace, testClassName))
	b.WriteString("\r\n")
	b.WriteString("// <ExportedTypeAliases>\r\n")
	b.WriteString("// </ExportedTypeAliases>\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <InterfaceImplementations>\r\n")
	b.WriteString("// </InterfaceImplementations>\r\n")
	b.WriteString("\r\n")
	b.WriteString("// <ImplicitConversions>\r\n")
	b.WriteString("// </ImplicitConversions>\r\n")
	b.WriteString("\r\n")
	b.WriteString(fmt.Sprintf("namespace %s;\r\n", projectNamespace))
	b.WriteString("\r\n")
	b.WriteString(fmt.Sprintf("public static partial class %s\r\n{\r\n}\r\n", testClassName))

	return b.String()
}

// writeExternalVariantMetadata merges the EXTERNAL test variant's live metadata globals into
// the -tests anchor files (B4/B5). Test-anchored records are written into
// package_info_external_test.cs — a separate compilation unit whose first class is the test package
// class — through the SAME shared writer (with the alias globals stashed: `global using`
// aliases must live in exactly one file, CS1537, and GoTypeAlias attributes stay with the
// production-anchored metadata). Production-anchored records and every alias then merge into
// package_test_info.cs as before. Returns the unit's file name when it was written (the caller
// adds it to the test project's compile items), or "" when the variant introduced no
// test-anchored records — utf8-class packages keep their single-file shape byte-identical.
func writeExternalVariantMetadata(testInfoPath, outputPath, productionPackageName, productionAnchor, testAnchor string) (string, error) {
	productionClassName := getSanitizedImport(productionPackageName + PackageSuffix)
	testAnchored, productionAnchored := splitExternalVariantRecords(productionClassName)

	// RECOMPILE-model anchored writes: the production class is compiled into this assembly, so
	// the historical production-local type qualification stays in force (see writePackageInfoFile).
	metadataAnchorLocalTypes = false

	unitName := ""

	// The external variant's `[GoType]` types are declared in the TEST package class, so their
	// accessibility section belongs to the test-anchored unit — never to package_test_info.cs,
	// whose section sits inside the PRODUCTION package class (a stray entry there would declare a
	// second, phantom type of the same simple name in the wrong class). A variant that declares
	// types therefore needs the unit even when it recorded no test-anchored GoImplement /
	// GoImplicitConv attributes.
	if !testAnchored.isEmpty() || len(packageEmittedTypeAccess) > 0 {
		unitPath := filepath.Join(outputPath, externalTestPackageInfoFileName)

		if _, err := os.Stat(unitPath); os.IsNotExist(err) {
			seed := externalTestPackageInfoSeed(packageNamespace, productionClassName, getSanitizedImport(packageName+PackageSuffix))

			if err := os.WriteFile(unitPath, []byte(seed), 0644); err != nil {
				return "", fmt.Errorf("seed external test package metadata: %w", err)
			}
		}

		savedImported, savedExported := importedTypeAliases, exportedTypeAliases
		importedTypeAliases = map[string]string{}
		exportedTypeAliases = map[string]string{}

		testAnchored.install()
		metadataAnchorClassPrefix = testAnchor
		writePackageInfoFile(unitPath, true)

		importedTypeAliases, exportedTypeAliases = savedImported, savedExported
		unitName = externalTestPackageInfoFileName
	}

	// The production-anchored partition (plus the full alias globals) merges into
	// package_test_info.cs; the split partitions stay installed afterward — the external
	// variant is the last one converted, and nothing downstream reads these globals. The
	// accessibility entries were written to the test-anchored unit above and must NOT reach this
	// file's production-class section; clearing the set leaves the merge to preserve exactly the
	// production + internal-variant entries already there.
	packageEmittedTypeAccess = HashSet[string]{}

	productionAnchored.install()

	// package_test_info.cs anchors to the PRODUCTION class even though the EXTERNAL variant is the
	// one merging into it here — the anchor is a property of the file (its first class), not of the
	// variant writing it.
	metadataAnchorClassPrefix = productionAnchor
	writePackageInfoFile(testInfoPath, true)

	return unitName, nil
}

// discoverTestDeclarations finds every go-test-shaped top-level declaration in the variant's
// selected test files and classifies it. Disclosure is total by design (req §2.7): every
// discovered Test*/Benchmark*/Fuzz*/Example*/TestMain declaration lands in the manifest with an
// explicit status — nothing is silently absent. Capability gating is PER TEST (F4): a test whose
// transitive call closure requires capabilities outside the supported list blocks itself
// (status "unsupported" + reason), not its package.
func discoverTestDeclarations(pkg *packages.Package, entries []FileEntry, inputPath string, capabilities testCapabilityAnalysis, supported HashSet[string]) ([]testDeclaration, *testDeclaration) {
	selected := make(map[*ast.File]string, len(entries))
	for _, entry := range entries {
		selected[entry.file] = entry.filePath
	}

	result := make([]testDeclaration, 0)
	var testMain *testDeclaration

	for _, file := range pkg.Syntax {
		path, ok := selected[file]
		if !ok {
			continue
		}

		relPath, _ := filepath.Rel(inputPath, path)
		for _, decl := range file.Decls {
			fn, ok := decl.(*ast.FuncDecl)
			if !ok || fn.Recv != nil || fn.Name == nil {
				continue
			}

			obj, ok := pkg.TypesInfo.Defs[fn.Name].(*types.Func)
			if !ok {
				continue
			}

			sig, ok := obj.Type().(*types.Signature)
			if !ok || sig.TypeParams().Len() != 0 || sig.Results().Len() != 0 {
				continue
			}

			name := fn.Name.Name
			position := pkg.Fset.Position(fn.Pos())
			entry := testDeclaration{Name: name, PackageName: file.Name.Name, Source: filepath.ToSlash(relPath), Line: position.Line}

			requirements := capabilities.requiredFor(obj)
			required := requirements.Keys()
			sort.Strings(required)
			entry.RequiredCapabilities = required

			// Example functions take no parameters (F2): `go test` runs them with Output:
			// comparison, so they MUST appear in the manifest — disclosed-unsupported until
			// Phase 4D — or the differential oracle would silently under-compare.
			if sig.Params().Len() == 0 {
				if isGoTestName(name, "Example") {
					entry.Kind, entry.Status, entry.Reason = "example", "unsupported", "example execution is deferred to Phase 4D"
					result = append(result, entry)
				}
				continue
			}

			if sig.Params().Len() != 1 {
				continue
			}

			switch {
			case name == "TestMain" && isTestingPointer(sig.Params().At(0).Type(), "M"):
				entry.Kind = "test-main"
				entry.Status = "included"
				applyCapabilityGate(&entry, requirements, supported)
				declarationCopy := entry
				testMain = &declarationCopy
			case isGoTestName(name, "Test") && isTestingPointer(sig.Params().At(0).Type(), "T"):
				entry.Kind = "test"
				entry.Status = "included"
				applyCapabilityGate(&entry, requirements, supported)
				result = append(result, entry)
			case isGoTestName(name, "Benchmark") && isTestingPointer(sig.Params().At(0).Type(), "B"):
				entry.Kind, entry.Status, entry.Reason = "benchmark", "unsupported", "benchmark execution is deferred to Phase 4D"
				result = append(result, entry)
			case isGoTestName(name, "Fuzz") && isTestingPointer(sig.Params().At(0).Type(), "F"):
				entry.Kind, entry.Status, entry.Reason = "fuzz", "unsupported", "fuzz execution is deferred to Phase 4D"
				result = append(result, entry)
			}
		}
	}

	return result, testMain
}

// applyCapabilityGate downgrades an included declaration to disclosed-unsupported when its
// transitive capability requirements exceed the supported list.
func applyCapabilityGate(entry *testDeclaration, requirements HashSet[string], supported HashSet[string]) {
	unsupported := NewHashSet(requirements.Keys())
	unsupported.ExceptWith(supported.Keys())

	if unsupported.IsEmpty() {
		return
	}

	blocked := unsupported.Keys()
	sort.Strings(blocked)
	entry.Status = "unsupported"
	entry.Reason = unsupportedCapabilityReasonPrefix + strings.Join(blocked, ", ")
}

func isTestingPointer(t types.Type, typeName string) bool {
	pointer, ok := t.(*types.Pointer)
	if !ok {
		return false
	}
	named, ok := pointer.Elem().(*types.Named)
	if !ok || named.Obj() == nil || named.Obj().Pkg() == nil {
		return false
	}
	return named.Obj().Pkg().Path() == "testing" && named.Obj().Name() == typeName
}

func isGoTestName(name, prefix string) bool {
	if !strings.HasPrefix(name, prefix) {
		return false
	}
	if len(name) == len(prefix) {
		return true
	}
	r, _ := utf8.DecodeRuneInString(name[len(prefix):])
	return !unicode.IsLower(r)
}

func supportedTestCapabilities() []string {
	capabilities := []string{
		"T.Cleanup", "T.Error", "T.Errorf", "T.Fail", "T.FailNow", "T.Failed",
		"T.Fatal", "T.Fatalf", "T.Helper", "T.Log", "T.Logf", "T.Name", "T.Parallel",
		"T.Run", "T.Setenv", "T.Skip", "T.SkipNow", "T.Skipf", "T.Skipped", "T.TempDir", "M.Run",
		// T.Deadline reports when the package deadline (-timeout) expires. It was the LAST
		// unsupported member of Go 1.23's *testing.T surface, blocked only because a shim that
		// could not name a converted `time.Time` had nothing to return; the one-tree consolidation
		// (2026-08-01) gave core/testing a real `time` reference and the member landed with it
		// (core/testing/testing.cs Deadline + TestHost.PackageDeadlineUtc) — the capability list
		// simply was not widened to match. Six of context's cancellation tests were excluded for
		// want of it, including the whole tree-cancellation family. Roster impact measured before
		// widening (charter §9): the only validated package whose _test.go calls it is os/signal,
		// and both call sites are in `//go:build unix` files this platform never builds.
		"T.Deadline",
		// The testing.TB surface. A capability name is keyed on the RECEIVER's named type
		// (analyzeTestingCapabilities), so a helper declared `func h(t testing.TB)` records TB.Fatal
		// where the identical call on a *testing.T records T.Fatal — two rosters over ONE
		// implementation. Listing only the T spelling therefore excluded every test that funnels
		// through a same-package TB-typed helper, whole: os/exec's tests all reach
		// `exePath(t testing.TB)`, and 26 of them — every process-spawn shape the package has —
		// were gated out and had never run.
		//
		// What makes these honest is that nothing here is new behavior. "Supported" means three
		// things hold, and for TB all three already did: core/testing's TB interface declares the
		// member (Go 1.23's full 18, minus the unexported private()); go2cs-gen's ImplementGenerator
		// mints the `testing_TжTB` adapter the converter's `[assembly: GoImplement<T, TB>(Pointer =
		// true)]` record asks for, forwarding EVERY member to the package-scope T implementation
		// (`TB.Fatal(Span<object>) => testing_package.Fatal(m_box, args)` — verified against the
		// generated file, not assumed); and that implementation is the same TestExecution-backed one
		// T.Fatal has always answered, so a TB.FailNow throws the same TestAbortException and aborts
		// the same way. No member of Go's TB is absent: TB has no Run, Parallel or Deadline to want.
		//
		// The one declared limit, and it is a property of B rather than of TB: an adapter built from
		// a *testing.B forwards to B's compile-only no-ops. Benchmarks are never registered or run,
		// so the only path that puts a live B behind a TB parameter is a Test that calls
		// testing.Benchmark itself and passes the b onward — no suite does, and if one appears its
		// failure reports would be silently swallowed. That is a benchmark-execution question
		// (Phase 4D), not a reason to withhold the T-backed surface from every test that has one.
		"TB.Cleanup", "TB.Error", "TB.Errorf", "TB.Fail", "TB.FailNow", "TB.Failed",
		"TB.Fatal", "TB.Fatalf", "TB.Helper", "TB.Log", "TB.Logf", "TB.Name", "TB.Setenv",
		"TB.Skip", "TB.SkipNow", "TB.Skipf", "TB.Skipped", "TB.TempDir",
		"testing.AllocsPerRun", "testing.CoverMode", "testing.Short", "testing.Verbose",
		// In-process benchmarking driven from a Test function: testing.Benchmark runs a
		// func(*B) closure and returns a BenchmarkResult, setting B.N and exposing NsPerOp
		// (unicode's TestCalibrate uses this to pick a linear-vs-binary search cutoff). The
		// host implements these (core/testing/testing.cs: Benchmark, B.N, BenchmarkResult).
		// Top-level BenchmarkXxx DECLARATIONS remain unsupported by their kind (they are never
		// registered — see the "benchmark" case in discoverTestDeclarations), so supporting
		// these members only unblocks Test functions that call testing.Benchmark themselves.
		"testing.Benchmark", "B.N", "BenchmarkResult.NsPerOp",
	}
	sort.Strings(capabilities)
	return capabilities
}

// unsupportedRuntimeCapabilities maps a SYMBOL — "<import path>.<func>" — to the NAME of the
// capability that symbol requires and that the managed runtime provably cannot provide. A test whose
// transitive closure reaches a listed symbol, or which IS one, is gated to `unsupported` by the SAME
// mechanism that gates an unsupported testing.* member: the capability name becomes a REQUIREMENT
// that supportedTestCapabilities deliberately does not list.
//
// The key is the symbol and the value is the capability because the two are not the same thing and
// the report needs the second. Several symbols can want one capability, and a capability that is a
// property of the HOST rather than of anything the test calls has no symbol to name at all — so a key
// may also name the TEST DECLARATION itself, which requiredFor honors by gating a listed function on
// its own account and not only on its callers'. What the manifest, the comparison and the proof page
// then show is the capability ("relocatable single-file test executable"), never the bare symbol.
//
// Why this exists as a gate rather than a runtime failure: an unimplemented assembly primitive
// throws a .NET NotImplementedException, and when the reaching path runs on a goroutine (a managed
// thread) that exception is unhandled and TERMINATES THE HOST — every test after it reports no
// result and the whole package reads as a mass infrastructure wall. sync's TestOnceFuncGoexit did
// exactly that: runtime.Goexit → getcallerpc, taking 28 of sync's 51 tests down with it. Declaring
// the capability unsupported is both more honest and more useful — the one test is excluded and
// disclosed by name, and the rest of the package is measurable.
//
// runtime.Goexit, the map's first and for a while only entry, graduated when the managed shape landed
// — an unwinding golib GoexitException that the defer machinery runs defers for, recover() cannot
// see, and the goroutine root swallows (docs/phase4/DESIGN-goexit.md, §2 + option C). Goexit from the
// MAIN goroutine is still unimplemented, but that case cannot be distinguished statically — a
// function's call graph says nothing about which goroutine will run it — so it is gated where the
// distinction actually exists, at runtime, by runtime/managed_impl.cs (a loud NotSupportedException,
// never a silent no-op).
//
// Add an entry ONLY for something provably unavailable, never for something merely unimplemented;
// and before adding one, scan every VALIDATED package for the symbol, since gating it removes those
// tests from the run set (the mirror of the widening trap in the charter's §9). Guarded by
// TestUnsupportedRuntimeCapabilityGate.
var unsupportedRuntimeCapabilities = map[string]string{
	// The block returned by CommandLineToArgv is OS-allocated and CALLER-FREED — every Go caller ends
	// `defer syscall.LocalFree(syscall.Handle(uintptr(unsafe.Pointer(argv))))`. Walking it needs a
	// managed materialization of the pointer array; taking the address of one hands back the GC-heap
	// data address (ж's pinnedArrayData path), so the deferred LocalFree would be asked to free GC
	// memory — a STATUS_HEAP_CORRUPTION process kill in place of a contained failure. Reading the
	// native block WITHOUT materializing it is the snapshot-pointer flavor golib does not have, and
	// which the 2026-08-02 ruling deferred to net's DNS work rather than mint for one test.
	"syscall.CommandLineToArgv": "native output block with caller-side LocalFree",

	// createMountPoint lays a *windows.MountPointReparseBuffer over a managed []byte and writes four
	// uint16 fields through it, then indexes &buf.PathBuffer[0] — a Go `[1]uint16` inline tail standing
	// in for however many kernel bytes follow. The conversion of that tail is an 8-byte MANAGED
	// REFERENCE, so no managed array reference can be laid over inline OS bytes: this is the raw-metal
	// arm of the S1 fork, whose remedy everywhere else is to hand-own the file — unavailable here
	// because the code is a TEST helper, which the converter regenerates by definition.
	"os_test.createMountPoint": "raw-metal struct overlay on managed bytes",

	// The one entry that names a TEST rather than a symbol, because the impossibility is a property of
	// the host: the test copies os.Executable() — ONE file — into a temp directory 100 times and runs
	// each copy. os.Executable() is correct (it returns the apphost, os.tests.exe), but an apphost is a
	// stub bound at build time to a managed assembly of the same base name that must sit beside it, so
	// a single-file copy can never run: hostfxr answers 0x8000809a LibHostAppRootFindFailure, which is
	// byte-for-byte the code the test reports. Go's test binary is statically linked, which is the only
	// reason its premise holds there. Satisfying it means publishing every converted test host
	// self-contained single-file — ~70 MB and a publish rather than a build, per package.
	"os_test.TestRemoveAllWithExecutedProcess": "relocatable single-file test executable",

	// os/exec's TestCommand and TestLookPathWindows want this SAME capability from the other
	// direction — installExe (lp_windows_test.go) copies the running test executable into a
	// t.TempDir() tree and runs the copy — and they are NOT listed here. They are DISCLOSED
	// instead, under the host-limit class ruled 2026-08-15: src/core/os/exec's committed
	// go2cs_test_disclosures.json pins 25 leaf rows on `exit status 0x8000809a`, their 2 parents
	// ride the disclosed-parent aggregation, and os/exec banks at 74 matched + 27 disclosed. The
	// class and the bar an entry must clear are in docs/ConversionStrategies-Reference.md,
	// "host-limit — the third disclosed-divergence class". Gating them was measured FIRST and is
	// worse on three counts — the two below, plus that a gate hides the very rows whose future
	// passing is the only signal the limit has lifted
	// (docs/phase4/BOARD-next-validation-candidates.md, lane claude/os-exec-gate-bank):
	//
	//  1. A gate is DECLARATION-keyed and eligibleTerminalTestResults cuts a verdict row at its
	//     first "/", so 2 entries withdraw 40 rows rather than the 27 that were failing. The other
	//     13 are agreeing passes, and os/exec drops from 74 agreeing rows to 61.
	//  2. Gating the failures is what BREAKS it. os/exec's TestMain runs a helper-registry census
	//     guarded by `code == 0`, and the only callers of maySkipHelperCommand("printpath") are the
	//     two tests a gate removes — their file's init() still registers the helper. Green the
	//     suite and the census fires: `helper command unused: "printpath"`, exit 1, and the package
	//     validates at no count at all.
	//
	// os_test.TestRemoveAllWithExecutedProcess never showed (2) only because os's TestMain is a bare
	// Exit(m.Run()), and os is not yet on the roster — its disposition is decided when it banks.
	// The underlying hazard is unfixed and QUEUED rather than closed: a gate is invisible to the
	// running host, since nothing publishes the fact that a SUBSET ran where Go's own vocabulary for
	// it is a non-empty test.run. So any suite asserting that the whole suite ran will mis-answer
	// while a gate is active. Nothing is broken today (the only gated declarations live in os, whose
	// TestMain asserts nothing), but CHECK FOR SUCH A TestMain before adding a declaration-keyed
	// entry — and prefer a disclosure whenever the tests can still run.
}

// unsupportedRuntimeCapability reports whether fn requires a listed unsupported runtime capability,
// returning the capability name used in the requirement set.
func unsupportedRuntimeCapability(fn *types.Func) (string, bool) {
	if fn == nil || fn.Pkg() == nil || fn.Type() == nil {
		return "", false
	}

	// Package-scope functions only — a method named Goexit on some type is not runtime.Goexit.
	if sig, ok := fn.Type().(*types.Signature); ok && sig.Recv() != nil {
		return "", false
	}

	capability, blocked := unsupportedRuntimeCapabilities[fn.Pkg().Path()+"."+fn.Name()]

	return capability, blocked
}

// testCapabilityAnalysis is the per-variant capability attribution input (F4): the testing.*
// members each function uses DIRECTLY, and the static same-package reference graph used to close
// over helpers. References are collected conservatively (any use of a same-package function, not
// just direct calls), so a capability reached through a stored function value still gates the
// test that stores it; cross-package helpers (e.g. internal/testenv) are outside the graph and
// gate through their own package's conversion instead.
type testCapabilityAnalysis struct {
	direct   map[*types.Func]HashSet[string]
	referees map[*types.Func]map[*types.Func]bool
}

// analyzeTestingCapabilities walks every function declaration in the variant (production and
// test files alike — helpers can live in either) recording direct testing.* usage and the
// same-package reference graph. The receiver filter is deliberately absent (F5): a helper taking
// *testing.B contributes B.* requirements, so a supported-kind test calling it is gated instead
// of sailing through.
func analyzeTestingCapabilities(pkg *packages.Package) testCapabilityAnalysis {
	analysis := testCapabilityAnalysis{
		direct:   make(map[*types.Func]HashSet[string]),
		referees: make(map[*types.Func]map[*types.Func]bool),
	}

	for _, file := range pkg.Syntax {
		for _, decl := range file.Decls {
			fn, ok := decl.(*ast.FuncDecl)
			if !ok || fn.Name == nil {
				continue
			}

			obj, ok := pkg.TypesInfo.Defs[fn.Name].(*types.Func)
			if !ok {
				continue
			}

			direct := HashSet[string]{}
			referees := make(map[*types.Func]bool)

			if fn.Body != nil {
				ast.Inspect(fn.Body, func(node ast.Node) bool {
					switch expr := node.(type) {
					case *ast.SelectorExpr:
						if selection := pkg.TypesInfo.Selections[expr]; selection != nil {
							member := selection.Obj()
							if member == nil || member.Pkg() == nil || member.Pkg().Path() != "testing" {
								return true
							}

							receiver := selection.Recv()
							if pointer, ok := receiver.(*types.Pointer); ok {
								receiver = pointer.Elem()
							}
							if named, ok := receiver.(*types.Named); ok && named.Obj().Pkg() != nil && named.Obj().Pkg().Path() == "testing" {
								direct.Add(named.Obj().Name() + "." + member.Name())
							}
						} else if member := pkg.TypesInfo.Uses[expr.Sel]; member != nil && member.Pkg() != nil && member.Pkg().Path() == "testing" {
							if _, ok := member.(*types.Func); ok {
								direct.Add("testing." + member.Name())
							}
						}
					case *ast.Ident:
						used, ok := pkg.TypesInfo.Uses[expr].(*types.Func)

						if !ok {
							return true
						}

						if used.Pkg() == pkg.Types {
							referees[used] = true
						}

						// A RUNTIME capability the managed model cannot provide is recorded the
						// same way a testing.* member is, and gates the same way (it is absent
						// from supportedTestCapabilities). This is keyed on the resolved OBJECT,
						// so it catches the call however it is spelled or aliased.
						if capability, blocked := unsupportedRuntimeCapability(used); blocked {
							direct.Add(capability)
						}
					}
					return true
				})
			}

			analysis.direct[obj] = direct
			analysis.referees[obj] = referees
		}
	}

	return analysis
}

// requiredFor returns the transitive testing.* capability requirements of fn — its own direct
// usage plus that of every same-package function reachable through the reference graph.
func (a testCapabilityAnalysis) requiredFor(fn *types.Func) HashSet[string] {
	required := HashSet[string]{}
	visited := make(map[*types.Func]bool)

	var walk func(current *types.Func)
	walk = func(current *types.Func) {
		if visited[current] {
			return
		}
		visited[current] = true

		// A listed unsupported capability gates the function that REQUIRES it on its own account,
		// not only its callers'. The caller-side arm (analyzeTestingCapabilities, which records the
		// requirement at every ident that names a listed symbol) cannot reach the case where the
		// requirement belongs to the test itself: nothing names a test, so nothing records it. That
		// is the shape a HOST capability takes — the test calls no impossible function, it merely
		// assumes something of the binary it runs in.
		if capability, blocked := unsupportedRuntimeCapability(current); blocked {
			required.Add(capability)
		}

		if direct, ok := a.direct[current]; ok {
			required.UnionWithSet(direct)
		}
		for referee := range a.referees[current] {
			walk(referee)
		}
	}

	walk(fn)
	return required
}

func writeTestHost(outputPath, namespace, importPath string, declarations []testDeclaration, testMain *testDeclaration, fixtures, fixtureDirectories []string) error {
	var b strings.Builder
	b.WriteString("// Code generated by go2cs test conversion. DO NOT EDIT.\r\n")
	b.WriteString(fmt.Sprintf("namespace %s;\r\n\r\n", namespace))
	// Emitted INSIDE `namespace go.<pkg>;`, so the leading `go` re-binds to a `go.go` namespace
	// whenever the test closure pulls in a go/* package (math/rand/v2's regress_test.go imports
	// go/format) — CS0234. packageChildNamespaces still holds the last converted test variant's
	// closure here, which is the closure of the assembly this host compiles into.
	b.WriteString(fmt.Sprintf("using %s;\r\n\r\n", globalQualifyRooted(RootNamespace+".testing_runtime")))
	b.WriteString("internal static class Go2CsTestHost\r\n{\r\n")
	b.WriteString("    public static int Main(string[] args)\r\n    {\r\n")
	b.WriteString(fmt.Sprintf("        TestRegistry registry = new(\"%s\", new string[]\r\n        {\r\n", escapeCSharp(importPath)))
	for _, fixture := range fixtures {
		b.WriteString(fmt.Sprintf("            \"%s\",\r\n", escapeCSharp(filepath.ToSlash(fixture))))
	}
	// The run-directory list is OMITTED when the package has no subdirectories — which is most of
	// them — so their host stays byte-identical to what a converter without this capability emitted
	// (TestRegistry defaults the parameter to an empty list). An emitted-but-empty array would churn
	// every banked host for a run environment that is unchanged.
	if len(fixtureDirectories) > 0 {
		b.WriteString("        }, new string[]\r\n        {\r\n")
		for _, directory := range fixtureDirectories {
			b.WriteString(fmt.Sprintf("            \"%s\",\r\n", escapeCSharp(filepath.ToSlash(directory))))
		}
	}

	b.WriteString("        });\r\n")

	for _, test := range declarations {
		if test.Kind != "test" || test.Status != "included" {
			continue
		}
		className := test.CSharpClassName
		if className == "" {
			className = getSanitizedImport(test.PackageName + PackageSuffix)
		}
		methodName := getSanitizedFunctionName(test.Name)
		b.WriteString(fmt.Sprintf("        registry.Add(\"%s\", %s.%s, \"%s\", %d);\r\n", escapeCSharp(test.Name), className, methodName, escapeCSharp(test.Source), test.Line))
	}

	if testMain != nil && testMain.Status == "included" {
		className := testMain.CSharpClassName
		if className == "" {
			className = getSanitizedImport(testMain.PackageName + PackageSuffix)
		}
		b.WriteString(fmt.Sprintf("        registry.SetTestMain(%s.%s);\r\n", className, getSanitizedFunctionName(testMain.Name)))
	}

	b.WriteString("        return TestHost.Run(registry, args);\r\n")
	b.WriteString("    }\r\n}\r\n")

	contents := []byte(b.String())
	fileName := filepath.Join(outputPath, testHostFileName)
	if needToWriteFile(fileName, contents) {
		return os.WriteFile(fileName, contents, 0644)
	}
	return nil
}

// writeTestProject emits the test project from the embedded test-csproj-template.xml (following
// the csproj-template.xml precedent). The template carries the static machinery (explicit
// compile items via EnableDefaultCompileItems=false, generated-files exposure, the go2csPath
// fallback chain with the $(HOME) non-Windows fallback, the Go type-alias usings); the markers
// carry the per-project values.
// testProjectFixedReferences are the references EVERY converted test project carries regardless of
// what the package under test imports: the shared runtime, and the hand-owned `testing` package
// that hosts the run. Both are rooted in the one converted-standard-library tree at
// $(go2csPath)core — the same root every resolved dependency reference uses.
var testProjectFixedReferences = []string{
	`$(go2csPath)core/golib/golib.csproj`,
	`$(go2csPath)core/testing/testing.csproj`,
}

func writeTestProject(projectFile, projectName, namespace string, model testProjectModel, productionFiles, testFiles, fixtures, dependencies []string, options Options) error {
	references := HashSet[string]{}

	for _, fixed := range testProjectFixedReferences {
		references.Add(fixed)
	}

	// REFERENCE model: the production package compiles ONLY in its own project; reference it so
	// its assembly stays the single identity for the production types. Colocated-relative — the
	// -tests contract colocates the test project with the production csproj — so the reference
	// is layout-independent (no $(go2csPath) tree mapping involved).
	if model.referencesProduction() {
		references.Add(projectFileBaseName(projectName) + ".csproj")
	}

	for _, dependency := range dependencies {
		for _, info := range getImportPackageInfo([]string{dependency}, options) {
			// A dependency that fails to resolve must fail the conversion NAMING the dependency
			// (F14b) — silently dropping the reference would surface later as an uncaused CS0246.
			if info.Err != nil {
				return fmt.Errorf("resolve test project dependency %q: %w", dependency, info.Err)
			}

			reference := info.ProjectReference
			if reference != "" && !isSelfProjectReference(reference, projectName) {
				references.Add(reference)
			}
		}
	}

	// The template's last-resort go2csPath fallback must be a COMPLETE property value: an
	// $(MSBuildThisFileDirectory)-anchored relative walk-up when one exists, else the absolute
	// path on its own. filepath.Rel fails across Windows drive letters (an H:\ checkout with the
	// default C:\Users\...\go2cs), and concatenating the absolute after the MSBuild prefix
	// produced an unresolvable garbage path — the bare-clone CS0246 golib failure.
	relativeGo2CSPath, relErr := filepath.Rel(filepath.Dir(projectFile), options.go2csPath)
	if relErr == nil {
		relativeGo2CSPath = "$(MSBuildThisFileDirectory)" + strings.TrimRight(filepath.ToSlash(relativeGo2CSPath), "/") + "/"
	} else {
		relativeGo2CSPath = strings.TrimRight(filepath.ToSlash(options.go2csPath), "/") + "/"
	}

	var compileItems strings.Builder
	compileFiles := append([]string{}, testFiles...)

	// The production sources are compile items only under the RECOMPILE model; the reference
	// model binds them through the production project reference above instead.
	if !model.referencesProduction() {
		compileFiles = append(compileFiles, productionFiles...)
	}

	compileFiles = append(compileFiles, testPackageInfoFileName, testHostFileName)
	sort.Strings(compileFiles)
	for _, file := range compileFiles {
		compileItems.WriteString(fmt.Sprintf("\r\n    <Compile Include=\"%s\" />", escapeXMLAttributeValue(filepath.ToSlash(file))))
	}

	var fixtureItems strings.Builder
	for _, fixture := range fixtures {
		slashed := filepath.ToSlash(fixture)

		// A fixture ABOVE the package ("../testdata/e.txt") needs an explicit <Link>: MSBuild's
		// default link for a `..`-relative item is its BARE FILE NAME, which both flattens the two
		// `testdata` trees into one and drops the relative shape the test's own open() needs. Link
		// it into a staging root under the output directory, keyed by how far up it reaches, and
		// TestHost.CopyFixtures maps it back to the true relative path inside the run sandbox
		// (SharedFixtureStagingRoot there — keep the two in sync).
		if up, tail, isShared := sharedFixtureStagingParts(slashed); isShared {
			fixtureItems.WriteString(fmt.Sprintf("\r\n    <None Include=\"%s\" Link=\"%s/up%d/%s\" CopyToOutputDirectory=\"PreserveNewest\" />",
				escapeXMLAttributeValue(slashed), SharedFixtureStagingRoot, up, escapeXMLAttributeValue(tail)))
			continue
		}

		fixtureItems.WriteString(fmt.Sprintf("\r\n    <None Include=\"%s\" CopyToOutputDirectory=\"PreserveNewest\" />", escapeXMLAttributeValue(slashed)))
	}

	var referenceItems strings.Builder
	refs := references.Keys()
	sort.Strings(refs)
	for _, reference := range refs {
		// Forward slashes on every host, matching the production writer (see F5): a resolved
		// dependency arrives already slashed from emittedProjectReference, but an ABSOLUTE
		// reference (a local module) is OS-native.
		referenceItems.WriteString(fmt.Sprintf("\r\n    <ProjectReference Include=\"%s\" />", escapeXMLAttributeValue(filepath.ToSlash(reference))))
	}

	contents := []byte(strings.NewReplacer(
		TestRootNamespaceMarker, namespace,
		TestAssemblyNameMarker, projectName+".tests",
		TestGo2CSRelativePathMarker, escapeXMLAttributeValue(relativeGo2CSPath),
		TestCompileItemsMarker, compileItems.String(),
		TestFixtureItemsMarker, fixtureItems.String(),
		TestProjectReferencesMarker, referenceItems.String(),
	).Replace(string(testCsprojTemplate)))

	if needToWriteFile(projectFile, contents) {
		return os.WriteFile(projectFile, contents, 0644)
	}
	return nil
}

// aliasReferenceImports returns the import paths of converted packages that `using` ALIASES in
// the scanned files target but that the test project does not directly reference (B2c). Both the
// test metadata files AND the converted test sources are scanned: a seeded global alias, or a
// file-local package-qualifier using emitted into a *_test.cs, can target an assembly the package
// reaches only transitively — including one no import list mentions, when a test-only helper
// RETURNS a type from it — and DisableTransitiveProjectReferences (B2b) hides such assemblies
// from the test compile view, so the alias line itself fails (CS0234). Candidates
// come from the module-aware TRANSITIVE import closure captured at load time
// (importPackageDirs), whose namespace tokens are rendered by the same machinery that emitted
// the aliases — including the /vN major-version collapse — so matching is exact. When several
// closure paths render the same token (math/rand beside math/rand/v2), the lexically first is
// taken, deterministically.
// testProjectAliasScanFiles returns the files whose emitted `using` aliases and conversion records
// the B2c project-reference scan must read: EVERY C# source the test project compiles, plus the two
// metadata files.
//
// B2c: a seeded/merged `using` ALIAS in the test metadata — or a package-qualifier `using` emitted
// into a converted SOURCE — can target an assembly the package reaches only TRANSITIVELY (sort's
// `global using reflectliteꓸKind = go.@internal.abi_package.ΔKind;` targets internal/abi via
// sort → reflectlite → abi; math/rand's default_test.cs needs os/exec purely because
// testenv.Command RETURNS *exec.Cmd, so "os/exec" appears in no import list), which
// DisableTransitiveProjectReferences (B2b) hides from the test compile view. The manifest's
// dependency list stays import-derived — alias targets are purely a project-reference concern.
//
// The PRODUCTION sources belong in that scan under the RECOMPILE model for exactly the same reason
// the test sources do, and for no other: there they are compile items of the test assembly (see
// writeTestProject), so an alias one of them emits is a reference the TEST project owns. That
// production sources were omitted was a scan-set gap, not a different rule — and the omission is
// invisible in the ordinary case because a production file's aliases are usually its own package's
// direct imports, which `dependencies` already carries. It bites where the alias names a package
// the production half reaches only transitively: `crypto/x509`'s x509.cs and pem_decrypt.cs emit
// `using hash = hash_package;` (crypto.Hash.New() RETURNS hash.Hash — `hash` is in no import list
// of x509 and in no reference of its own production csproj, which compiles only because it does NOT
// disable transitive references), and the test build failed CS0246 inside the PRODUCTION files.
//
// Under the REFERENCE model the production sources compile in their own project and are bound
// through its assembly reference, so their aliases are that project's concern; scanning them here
// would add references the test project does not need.
func testProjectAliasScanFiles(model testProjectModel, outputPath, testInfoPath string, testFiles, productionFiles []string) []string {
	scanFiles := []string{testInfoPath, filepath.Join(outputPath, externalTestPackageInfoFileName)}

	for _, testFile := range testFiles {
		scanFiles = append(scanFiles, filepath.Join(outputPath, testFile))
	}

	if !model.referencesProduction() {
		for _, productionFile := range productionFiles {
			scanFiles = append(scanFiles, filepath.Join(outputPath, productionFile))
		}
	}

	return scanFiles
}

func aliasReferenceImports(infoFiles []string, productionPkgPath string, directDependencies []string) []string {
	direct := NewHashSet(directDependencies)
	tokens := make(map[string][]string)
	bareTokens := make(map[string][]string)

	for importPath := range importPackageDirs {
		if importPath == productionPkgPath || importPath == "testing" || direct.Contains(importPath) {
			continue
		}

		namespace := convertImportPathToNamespace(importPath, PackageSuffix)

		token := RootNamespace + "." + namespace
		tokens[token] = append(tokens[token], importPath)

		// A SINGLE-SEGMENT package emits its alias UNROOTED — `using hash = hash_package;` inside
		// `namespace go.math.rand`, where C#'s outward lookup finds the class in the enclosing root
		// namespace without a qualifier. The rooted token above never matches such a line, so the
		// reference went missing and DisableTransitiveProjectReferences turned it into CS0246
		// (math/rand/v2's chacha8_test.cs: `sha256.New()` RETURNS hash.Hash, so `hash` appears in no
		// import list and only this alias scan can find it). Multi-segment namespaces always emit
		// with at least their leading package segment, so the rooted token still covers them.
		// Matched on a SEGMENT boundary (see bareTokens below) — a substring test would let
		// `hash_package` match `go.hash.maphash_package` and pull in a package nothing references.
		if !strings.Contains(namespace, ".") {
			bareTokens[namespace] = append(bareTokens[namespace], importPath)
		}
	}

	found := HashSet[string]{}

	for _, infoFile := range infoFiles {
		data, err := os.ReadFile(infoFile)
		if err != nil {
			continue
		}

		for _, line := range strings.Split(string(data), "\n") {
			trimmed := strings.TrimSpace(strings.TrimSuffix(line, "\r"))

			for _, target := range referenceScanTargets(trimmed) {
				for token, paths := range tokens {
					// Three match shapes for a multi-segment package's alias target:
					//   Contains(target, token+".")  — token is a leading/middle namespace segment.
					//   HasSuffix(target, token)      — target ends with the fully-ROOTED token,
					//                                    e.g. `go.os.exec_package` or `global::go.os.exec_package`
					//                                    (math/rand's default_test.cs, emitted from namespace go.math).
					//   HasSuffix(token, "."+target)  — target is the UNROOTED tail of the rooted token,
					//                                    e.g. `os.exec_package` matching token `go.os.exec_package`.
					//                                    A test emitted inside a namespace that SHADOWS the root
					//                                    `go` (go/doc/comment's std_test.cs in namespace go.go.doc,
					//                                    internal/abi's abi_test.cs in go.@internal) emits the alias
					//                                    unrooted and relies on C# outward lookup; the single-segment
					//                                    bareTokens path below never covers a multi-segment tail.
					//                                    Anchored on the leading "." so `os.exec_package` cannot
					//                                    match an unrelated `go.xos.exec_package`.
					if rootedQualifierMatch(target, token) {
						sort.Strings(paths)
						found.Add(paths[0])
					}
				}

				for token, paths := range bareTokens {
					if bareQualifierMatch(target, token) {
						sort.Strings(paths)
						found.Add(paths[0])
					}
				}
			}
		}
	}

	result := found.Keys()
	sort.Strings(result)

	return result
}

// rootedQualifierMatch reports whether a rendered qualifier TARGET names the package class spelled
// by a fully-ROOTED token (`go.os.exec_package`). Three shapes, all of them observed in emitted
// output — see the call site in aliasReferenceImports for which emission produces each.
func rootedQualifierMatch(target, token string) bool {
	return strings.Contains(target, token+".") || strings.HasSuffix(target, token) || strings.HasSuffix(token, "."+target)
}

// bareQualifierMatch is the same test for a SINGLE-SEGMENT package, whose alias is emitted
// UNROOTED (`using hash = hash_package;`) and found by C#'s outward lookup. Matched on a segment
// boundary so `hash_package` cannot match `go.hash.maphash_package`.
func bareQualifierMatch(target, token string) bool {
	return target == token || strings.HasPrefix(target, token+".")
}

// qualifierTargetNamesPackage reports whether a rendered qualifier TARGET names importPath's
// package class, in either spelling. The inverse direction of aliasReferenceImports' index: there
// the target is matched against every known import path, here against ONE already in hand (the
// closure walk knows its candidate interface's package and only asks whether a record named it).
func qualifierTargetNamesPackage(target, importPath string) bool {
	namespace := convertImportPathToNamespace(importPath, PackageSuffix)

	if rootedQualifierMatch(target, RootNamespace+"."+namespace) {
		return true
	}

	return !strings.Contains(namespace, ".") && bareQualifierMatch(target, namespace)
}

// packageImplementBases returns, per declared TYPE, the package-class qualifiers of the interfaces
// that package's VALUE-form `[assembly: GoImplement<T, I>]` records name — the base list go2cs-gen
// realizes on the type inside its own assembly, and therefore what binding a member on it must
// resolve (see declarationClosureImports' implementEdge).
//
// POINTER-form records are excluded by the parser: they generate an adapter CLASS (`FileжWriter`)
// rather than a base on the type, so they place no demand on a member binding — os records `File`
// as io/fs.File and io.Writer that way, and thirteen banked projects bind `os.File` members with
// neither reference.
//
// A record naming an interface the recording package DECLARES ITSELF (`error`, `FileInfo`) carries
// no `<pkg>_package` qualifier, so it contributes nothing and needs nothing: a same-package base
// is in the assembly already being referenced.
func packageImplementBases(packageInfoFile string) map[string][]string {
	lines, err := readPackageInfoLines(packageInfoFile)

	if err != nil {
		return nil
	}

	bases := map[string][]string{}

	for _, pair := range parseExportedValueImplementLines(lines) {
		qualifier := packageQualifierPattern.FindStringSubmatch(pair[1])

		if qualifier == nil {
			continue
		}

		bases[recordTypeGoName(pair[0])] = append(bases[recordTypeGoName(pair[0])], qualifier[1])
	}

	return bases
}

// foreignImplementBasesResolver returns the per-package record lookup declarationClosureImports'
// implementEdge uses for a type declared OUTSIDE the roots: the same packageImplementBases parse,
// pointed at the DECLARING package's own emitted package_info.cs in the runtime root rather than at
// the package under test's. Resolution reuses getImportPackageInfo, so it follows exactly the route
// the emitted `<ImportedTypeAliases>` block already reads a dependency's metadata by — including
// layout L3's per-GOOS placement (platformPackageInfoPath).
//
// Memoized per import path because the walk asks once per NAMED TYPE and a suite mentions the same
// few dependencies repeatedly. A package with no readable package_info.cs — never converted, or a
// runtime root that does not hold it — caches an empty map and simply contributes no edge, which is
// the same silent-nothing the root lookup produces from an unreadable file and is why the resolved
// root gets the loud once-per-run warning documented in CLAUDE.md.
func foreignImplementBasesResolver(options Options) func(string) map[string][]string {
	cache := map[string]map[string][]string{}

	return func(importPath string) map[string][]string {
		if bases, ok := cache[importPath]; ok {
			return bases
		}

		bases := map[string][]string{}

		for _, info := range getImportPackageInfo([]string{importPath}, options) {
			if info.Err != nil || len(info.TargetDir) == 0 {
				continue
			}

			if parsed := packageImplementBases(platformPackageInfoPath(info.TargetDir, goosOfTarget(options.targetPlatform))); parsed != nil {
				bases = parsed
			}
		}

		cache[importPath] = bases

		return bases
	}
}

// recordTypeGoName recovers the Go type name a record's IMPLEMENTATION side was emitted from, by
// undoing the spellings identifierNaming adds: the `@` keyword escape, the `Δ` reserved/collision
// prefix and its `ᴛ` type marker, and a generic instantiation's argument list
// (`nistCurve<Point>` → `nistCurve`). Two Go types in one package cannot normalize to one name —
// the markers exist to separate a type from a METHOD, never from another type — so the recovery is
// unambiguous where it is used, and an unrecovered name can only cost the edge a reference the
// build then demands loudly.
func recordTypeGoName(recorded string) string {
	if open := strings.Index(recorded, "<"); open >= 0 {
		recorded = recorded[:open]
	}

	recorded = removeLeadingSanitizationMarker(recorded)
	recorded = strings.TrimSuffix(strings.TrimPrefix(recorded, ShadowVarMarker), TempVarMarker)

	return recorded
}

// conversionRecordPrefixes are the emitted assembly-attribute line prefixes whose GENERIC ARGUMENT
// LIST names converted types that the test compilation must be able to BIND — go2cs-gen realizes
// each record into a generated adapter/partial/operator, so an unreferenced assembly on either side
// is CS0246 at the attribute itself.
var conversionRecordPrefixes = []string{"[assembly: GoImplement<", "[assembly: GoImplicitConv<"}

// packageQualifierPattern captures the PACKAGE-CLASS qualifier of a rendered type reference —
// everything up to and including the first segment that ends in PackageSuffix (`io_package`,
// `go.io.fs_package`, `go.@internal.abi_package`). Deliberately stops at the package class rather
// than taking the whole type reference, so the captured text has exactly the shape a `using` alias
// TARGET has and the same token matcher decides both.
var packageQualifierPattern = regexp.MustCompile(`(?:global::)?((?:@?[\p{L}_][\p{L}\p{N}_]*\.)*@?[\p{L}_][\p{L}\p{N}_]*` + PackageSuffix + `)`)

// referenceScanTargets returns the reference TARGETS a scanned metadata/source line contributes to
// the B2c project-reference augmentation.
//
// Two line shapes carry a cross-assembly type reference that no import list mentions:
//
//   - a `using` ALIAS (`global using reflectliteꓸKind = go.@internal.abi_package.ΔKind;`) — the
//     alias target itself, handled since B2c.
//
//   - an emitted CONVERSION RECORD (`[assembly: GoImplement<strings_package.Builder,
//     io_package.Writer>(Pointer = true)]`). The converter records an interface pair from a
//     type's USE — os/signal's test reaches `cmd.Stdout = &buf`, whose os/exec field type names
//     io.Writer — so the interface side can belong to a package that appears in NO import list of
//     either the production package or its tests, and DisableTransitiveProjectReferences (B2b)
//     then hides it (CS0246 on package_test_info.cs itself, plus a cascading go2cs-gen
//     CS8785 "second generic type argument must be an interface" once the interface fails to bind).
//     Only the generic argument list is scanned — an attribute's `(Pointer = true)` /
//     `(ValueType = "…")` payload is metadata, not a bindable reference.
func referenceScanTargets(line string) []string {
	if strings.HasPrefix(line, "global using ") || strings.HasPrefix(line, "using ") {
		if strings.HasPrefix(line, "using static ") || !strings.Contains(line, "=") {
			return nil
		}

		_, target, _ := strings.Cut(line, "=")

		return []string{strings.TrimSuffix(strings.TrimSpace(target), ";")}
	}

	isRecord := false

	for _, prefix := range conversionRecordPrefixes {
		if strings.HasPrefix(line, prefix) {
			isRecord = true
			break
		}
	}

	if !isRecord {
		return nil
	}

	// Span the record's generic argument list — first '<' to LAST '>' — so a nested generic
	// (`GoImplicitConv<Δindirect<K, V>, ж<Δindirect<K, V>>>`) is covered whole.
	open := strings.Index(line, "<")
	end := strings.LastIndex(line, ">")

	if open < 0 || end < open {
		return nil
	}

	var targets []string

	for _, match := range packageQualifierPattern.FindAllStringSubmatch(line[open+1:end], -1) {
		targets = append(targets, match[1])
	}

	return targets
}

// declarationClosureImports returns the import paths a test project must reference IN ADDITION to
// its computed direct set so that set is CLOSED under the type-reference edges of the C#
// DECLARATIONS of the types the compilation NAMES — a class of dependency neither the import lists
// nor the B2c alias scan can see.
//
// One rule: binding something in C# requires the assemblies of the types ITS OWN declaration names,
// and those names belong to the DECLARING package's import graph, so they appear in NO test-file
// import and NO alias `using`. The import-derived + alias-scan set (B2c) misses them,
// `DisableTransitiveProjectReferences` (B2b) hides the declaring package's own reference, and the test
// compile fails CS0012. Four edges carry it — two on a TYPE's declaration, two on an ACCESS (the
// second of which reads the declaration of the type the access lands on):
//
//   - INTERFACE BASES. Go interfaces satisfy structurally and compose by embedding; C# interfaces
//     are nominal, so the converter carries both shapes as C# inheritance at the declaration site
//     (getStructuralInterfaceBases): hash's `Hash` embeds io.Writer, io/fs's `File` merely lists
//     Read/Close, and both emit a declaration that NAMES an io base. The failure shows up at the
//     emitted conversion record (`[assembly: GoImplement<Hash, hash_package.Hash64>]` —
//     'io_package.Writer' is defined in an unreferenced assembly: hash/maphash, crypto/hmac, whose
//     closures reach `hash` but never `io`), at the go2cs-gen adapter realizing it, and at every
//     converted source naming the interface in a signature.
//
//   - STRUCT FIELDS AT AN ELEMENT-BEARING COMPOSITE LITERAL. The converter renders such a literal
//     as `new T(Field: …)` — a call to the FIELDWISE CONSTRUCTOR go2cs-gen generates for a
//     `[GoType]` struct, whose parameter list spells out EVERY field's type, supplied or not.
//     Binding that call therefore needs every field type's assembly. testing/quick's `Config` holds
//     a `Rand *rand.Rand`, so image/draw's `quick.CheckEqual(…, &quick.Config{MaxCountScale: 10})`
//     fails `CS0012 … 'rand_package.Rand' … assembly that is not referenced` at the
//     `new quick.Config(MaxCountScale: 10D)` expression, with math/rand in no import list on either
//     side. No interface closure can reach it — `Rand` is a STRUCT. A ZERO-VALUE DECLARATION is the
//     same constructor call by another route: `var l Logger` names no literal at all, yet renders as
//     `heap(new Logger(), out var Ꮡl)`, and log's white-box TestNonNewLogger failed
//     `CS0012 … 'atomic_package.Pointer<>'` resolving against the accessible fieldwise overload.
//
//   - THE RECEIVER OF A MEMBER ACCESS. Resolving `x.M` requires binding x's TYPE, and when x is
//     declared in another package that type is spelled nowhere here. `unique`'s white-box suite calls
//     `cleanupMu.Lock()` on the production package's `var cleanupMu sync.Mutex`; the test project
//     referenced no `sync` and the compile died `CS0012 … 'sync_package.Mutex'` twice, so the package
//     never linked a host and had never been measured. See the seed's own minimality note below.
//
//   - THE INTERFACES A NAMED TYPE'S DECLARATION IMPLEMENTS — what binding a member costs ON TOP
//     of binding the type. A converted CONCRETE type names no interface in its own emitted
//     declaration (`[GoType("[]ж<ΔError>")] partial struct ErrorList;`): its bases arrive as the
//     VALUE-form `[assembly: GoImplement<T, I>]` records its package emits, which go2cs-gen realizes
//     as `partial struct ErrorList : sort_package.Interface` INSIDE THE DECLARING ASSEMBLY. The
//     metadata type therefore declares that base, and binding any member on it resolves the list:
//     go/scanner's `list.Sort()`, `len(list)` and the generated `error` adapter's own
//     `m_value.Equals(…)` all failed `CS0012 … 'sort_package.Interface'`, ×13.
//
//     The records are read PER DECLARING PACKAGE, so the edge reaches a FOREIGN type's base list
//     too: go/types' check_test.go asserts `err.(scanner.ErrorList)` and calls `len(list)` on the
//     result, and `sort` — which go/types' PRODUCTION project references and no test file imports —
//     is surfaced only from go/scanner's own record set. Reading the declaring package's
//     package_info.cs is what makes that one lookup answer both shapes.
//
// Neither scan covers these because the named type itself DOES bind: its package IS referenced.
// What is missing is a package named inside that type's own C# declaration — or, for the third edge,
// the type of a value the compilation only ever reaches THROUGH a declaration in another package.
//
// MINIMALITY — three gates, because over-including is its own defect (every extra reference is
// churn across the banked corpus and a chance at a duplicate-type conflict):
//
//   - Seeds come from the files the test assembly actually COMPILES. A Phase-4D-excluded
//     Example/Benchmark-only file (selectCompileExcludedTestFiles) is analyzed but never emitted, so
//     it names nothing in the compilation: seeding from it handed compress/gzip the context,
//     crypto/tls, mime/multipart, net/http and net/url references reached through
//     `http.Request`'s fields — from an example_test.go that is not compiled at all.
//   - The interface walk starts from the types those files NAME, never from whole packages. C#
//     needs a base interface's assembly only when the derived interface is BOUND, and walking every
//     exported interface of every referenced package would hand io to almost the whole corpus
//     through `fmt.State`'s structural io.Writer base — a reference no project that never names
//     `fmt.State` requires.
//   - The struct-field edge fires where a composite literal constructs the struct, and an EMPTY
//     literal only when the struct is declared in a ROOT package. Measured against the corpus in
//     three steps: eleven banked packages hold `sync.Once`/`sync.Map`/`reflect.Value` VALUES
//     (strconv's package-level `atofOnce`, encoding/binary's `reflect.ValueOf`) and compile clean
//     today with no sync/atomic or internal/abi reference, so mere value use demands nothing;
//     three more (encoding/binary, mime, testing/quick) construct those same FOREIGN types with an
//     EMPTY literal — `once = sync.Once{}`, `return reflect.Value{}, false` — and also compile
//     clean, because the fieldwise constructor is `internal` for any struct with an unexported
//     field and so is not even a resolution candidate outside its assembly and friends; but a ROOT
//     package's struct IS visible that way (recompiled into the test assembly, or reached through
//     the white-box `InternalsVisibleTo` grant), so its empty literal must carry the edge —
//     math/rand/v2's `*p = ChaCha8{}` renders `new ChaCha8(nil)` and still fails
//     `CS0012 … 'chacha8rand_package.State'` while resolving against the internal fieldwise
//     overload. A one-level edge suffices throughout: the fieldwise constructor's parameters are
//     `default` unless supplied, and a NESTED literal is itself a seed.
//
// `testing` is skipped as a walk SOURCE (closureWalkable): it binds to the hand-owned core/testing
// shim per F15b, whose C# declarations are authored by hand and share only NAMES with Go's — Go's
// `testing.T` embeds a `common` holding io.Writer, time.Time, sync.RWMutex and a dozen more, none
// of which the shim's two-field `T` names, so inferring C# edges from the Go declaration there is
// simply invalid. Nothing is lost: the shim's reference is fixed in the project template.
//
// The per-interface match reproduces the CANDIDATE gates the converter runs at each declaration
// site (the same Exported / non-alias / non-generic / method-set / strictly-fewer-methods /
// types.Implements tests as getStructuralInterfaceBases). It is deliberately taken before that
// function's covered-by-embed skip and minimal-covering-set prune, so the result is a superset of
// the emitted base list — the guarantee that matters is that no emitted base's assembly is missing.
// Only the declaring package's own IMPORTS are scanned, so a same-package base contributes nothing
// new and needs no separate visit: an interface implements its base's bases too, so those candidates
// are found directly. Output is a sorted set, so the map-ordered walk stays deterministic.
func declarationClosureImports(roots []*packages.Package, compileExcluded map[string]bool, referenced []string, recordedBases map[string][]string, foreignBases func(importPath string) map[string][]string) []string {
	found := HashSet[string]{}
	seen := NewHashSet(referenced)
	visited := map[*types.Named]bool{}

	var queue []*types.Named

	enqueue := func(named *types.Named) {
		if named == nil || visited[named] || !closureWalkable(named) {
			return
		}

		if _, isInterface := named.Underlying().(*types.Interface); !isInterface {
			return
		}

		visited[named] = true
		queue = append(queue, named)
	}

	// reach records the assembly a named type the compilation must BIND lives in. TYPE seeds never
	// go through it — their packages are already referenced by construction; only a package named by
	// a walked DECLARATION is an addition (plus the member-access edge below, whose RECEIVER types
	// are likewise spelled nowhere in the compilation).
	reach := func(named *types.Named) {
		object := named.Obj()

		if object == nil || object.Pkg() == nil {
			return
		}

		// `testing` is never an ADDITION for the same reason it is never a walk SOURCE
		// (closureWalkable): it binds to the hand-owned core/testing shim, and that reference is
		// fixed in the project template — which is why the caller strips "testing" from the
		// import-derived set rather than passing it through as already-referenced. Every -tests
		// compilation calls a method ON a `*testing.T`, so without this the member-access edge
		// would hand a second, closure-derived `testing` reference to every test project.
		if !closureWalkable(named) {
			return
		}

		path := object.Pkg().Path()

		if seen.Contains(path) {
			return
		}

		seen.Add(path)
		found.Add(path)
	}

	// A ROOT's own types are compiled into the test assembly (or, for the production package under
	// the reference model, bound through the colocated project reference the template already
	// carries), so an edge landing back on one is never a project reference. The EXTERNAL variant
	// makes this load-bearing rather than theoretical: its go/packages PkgPath is the synthetic
	// `<pkg>_test`, which resolves to no importable package at all — a `bytes_test` struct literal
	// whose field type is declared beside it would fail the conversion outright ("package
	// bytes_test is not in std"), by design (F14b: a dependency that cannot resolve is loud).
	rootPaths := HashSet[string]{}

	for _, root := range roots {
		if root != nil {
			seen.Add(root.PkgPath)
			rootPaths.Add(root.PkgPath)
		}
	}

	// The fieldwise-constructor edge. One level, and never recursive on its own account: an
	// interface field still joins the base walk, and a nested literal is its own seed.
	fieldEdge := func(named *types.Named) {
		structType, isStruct := named.Underlying().(*types.Struct)

		if !isStruct || !closureWalkable(named) {
			return
		}

		for i := range structType.NumFields() {
			for _, mentioned := range namedTypesIn(structType.Field(i).Type()) {
				reach(mentioned)
				enqueue(mentioned)
			}
		}
	}

	// The IMPLEMENTED-INTERFACE edge, the CONCRETE counterpart of the interface-base walk. A
	// converted struct or named type carries its interfaces NOT in its own emitted declaration
	// (`[GoType("[]ж<ΔError>")] partial struct ErrorList;` names none) but in the VALUE-form
	// `[assembly: GoImplement<T, I>]` records its package emits, which go2cs-gen realizes as
	// `partial struct ErrorList : global::go.sort_package.Interface` INSIDE THE DECLARING
	// ASSEMBLY. So the type's metadata declares that base, and binding ANY member on it — Go's
	// `len(list)`, `list.Sort()`, and the generated value adapter's own `m_value.Equals(…)` —
	// makes the compiler resolve the base list. go/scanner's white-box suite failed
	// `CS0012 … 'sort_package.Interface'` ×13 that way, `sort` being in no test import and no
	// alias `using`. Interfaces are excluded here because the base walk already covers them.
	//
	// The RECORDS, not go/types satisfaction, are the gate, and that is measured rather than
	// argued: a record exists only where the converter converted a CAST, so Go satisfaction wildly
	// over-approximates the emitted base list. Gating on satisfaction alone drifts 16 of the 96
	// banked projects — `os.File` satisfies `syscall.Conn` and hands syscall to thirteen (os
	// records `File` only as io/fs.File and io.Writer, and both POINTER-form, which generate an
	// adapter CLASS rather than a base); `bytes.Buffer` satisfies most of io and hands io to sort
	// and unicode/utf8 though bytes records nothing at all; `internal/buildcfg`'s Stringer hands
	// it fmt from an empty record set. All 96 compile clean today with none of it. Pointer-form
	// records are therefore excluded here too — only the value form lands a base on the type.
	implementEdge := func(named *types.Named) {
		object := named.Obj()

		if _, isInterface := named.Underlying().(*types.Interface); isInterface || !closureWalkable(named) {
			return
		}

		if object == nil || object.Pkg() == nil {
			return
		}

		// The records are read PER DECLARING PACKAGE. A ROOT type's list is the record set the
		// production half of this same run just emitted (recordedBases); a FOREIGN type's is its own
		// package's, in its own package_info.cs — the widening this lookup was always shaped for
		// ("no measured case has ever demanded one" held until go/types), and it is the same gate
		// pointed at a different file, not a looser one. The per-package keying is what keeps a
		// same-named production type from answering for a foreign one.
		//
		// go/types' check_test.go is the measured case: `if list, _ := err.(scanner.ErrorList);
		// len(list) > 0`. `ErrorList` is declared in go/scanner — REFERENCED, so it binds — but its
		// realized base list comes from go/scanner's own
		// `[assembly: GoImplement<ErrorList, sort_package.Interface>]`, and resolving the `len(list)`
		// overload against it failed `CS0012 … 'sort_package.Interface'`. `sort` is in the PRODUCTION
		// project's references (go/types imports it) and in no test import, so only this edge can
		// surface it.
		targets := recordedBases[object.Name()]

		if !rootPaths.Contains(object.Pkg().Path()) {
			if foreignBases == nil {
				return
			}

			targets = foreignBases(object.Pkg().Path())[object.Name()]
		}

		if len(targets) == 0 {
			return
		}

		for _, candidate := range implementedInterfaceCandidates(named) {
			candidateObject := candidate.Obj()

			if candidateObject == nil || candidateObject.Pkg() == nil {
				continue
			}

			for _, target := range targets {
				if qualifierTargetNamesPackage(target, candidateObject.Pkg().Path()) {
					reach(candidate)
					enqueue(candidate)

					break
				}
			}
		}
	}

	for _, root := range roots {
		seeds := referencedTypeSeeds(root, compileExcluded)

		for _, named := range seeds.named {
			enqueue(named)
		}

		for _, named := range seeds.constructed {
			fieldEdge(named)
		}

		// The MEMBER-ACCESS edge. `cleanupMu` is `var cleanupMu sync.Mutex` in unique's PRODUCTION
		// source; the white-box suite calls `cleanupMu.Lock()`, and binding that member needs sync —
		// a package no test file imports and no alias `using` names, whose reference the reference
		// model deliberately does not inherit from the production assembly (that model adds only what
		// the test files import, precisely so a package's whole import graph is not re-declared).
		// CS0012 ×2, and `unique` never linked a host. TYPE seeds still never go through reach() —
		// the type a test file SPELLS comes from a package it imports — but a RECEIVER's type is
		// spelled nowhere in the compilation, which is exactly the class this function exists for.
		//
		// The receiver is the minimal form of the edge, and BOTH halves of that were measured against
		// the banked roster rather than argued. Widening it to the type of every var/const/func the
		// compilation NAMES is equally true of C#'s binding rules in the abstract and drifts **23 of
		// 73** banked projects (bufio into compress/bzip2, internal/abi + internal/reflectlite into
		// errors, three into hash/crc32 …), all of which compile clean today with none of it: naming
		// a declaration does not force its signature to be materialized, ACCESSING A MEMBER of it
		// forces the receiver's. And the seed is `_test.go`-scoped (referencedTypeSeeds), because
		// under the reference model the production sources are not in this compilation at all;
		// seeding from them too still drifts **13** (`castagnoliOnce.Do` in crc32.go, `cpu.X86` in
		// math's arith, …). Both restrictions together are ZERO-drift across the banked roster:
		// unique's own `sync` reference is the only line that changes.
		for _, named := range seeds.memberBases {
			reach(named)
			enqueue(named)
			implementEdge(named)
		}

		// The implemented-interface edge ALSO fires where a member is bound on a value WITHOUT a
		// selector: `len(list)`, `range list`, `list[i]`. Each lowers to a member on the value's
		// type — golib's generic `len`, the emitted enumeration, the indexer — so resolving it makes
		// the compiler read that type's realized base list exactly as `x.M` does. go/types'
		// check_test.go binds `scanner.ErrorList` only that way (`len(list)` and `range list`; there
		// is no `list.Sort()` anywhere in the suite), and failed `CS0012 … 'sort_package.Interface'`.
		//
		// Deliberately NOT routed through reach()/enqueue() the way memberBases is, and deliberately
		// NOT seeded from every NAMED type. Both restrictions keep the pinned negatives true: a type
		// a test file merely spells or PASSES ALONG (`var r Rows; Order(r)`) binds no member and
		// needs no base assembly — measured, and the boundary this edge is not allowed to cross —
		// while the type's OWN package is referenced by construction, since the value is spelled by
		// a package the suite imports.
		for _, named := range seeds.memberBound {
			implementEdge(named)
		}

		// The EMPTY-literal form of the same edge, scoped to the ROOT packages. `T{}` converts to
		// `new T(nil)` — go2cs-gen's dedicated nil constructor, which names no field — but the
		// FIELDWISE overload remains a resolution candidate whenever it is ACCESSIBLE, and
		// binding a candidate's signature is what demands its parameter assemblies. That
		// constructor is `internal` for any struct with an unexported field, so it is a candidate
		// exactly in the declaring assembly and its FRIENDS: a root package's types either
		// recompile into the test assembly or are reached through the white-box
		// `InternalsVisibleTo` grant, so both make it visible. math/rand/v2's `*p = ChaCha8{}`
		// failed `CS0012 … 'chacha8rand_package.State' … assembly that is not referenced` at the
		// `new ChaCha8(nil)` expression for exactly that reason, with internal/chacha8rand in no
		// import list on either side. A FOREIGN struct's internal constructor is invisible here,
		// which is the measured negative that keeps this edge root-scoped: mime's
		// `once = sync.Once{}` and testing/quick's `return reflect.Value{}, false` compile clean
		// today with no sync/atomic or internal/abi reference, and must stay that way.
		for _, named := range seeds.constructedEmpty {
			object := named.Obj()

			if object == nil || object.Pkg() == nil || !rootPaths.Contains(object.Pkg().Path()) {
				continue
			}

			fieldEdge(named)
		}
	}

	for len(queue) > 0 {
		named := queue[0]
		queue = queue[1:]

		for _, base := range interfaceBaseCandidates(named) {
			reach(base)
			enqueue(base)
		}
	}

	result := found.Keys()
	sort.Strings(result)

	return result
}

// closureWalkable reports whether declarationClosureImports may read named's Go DECLARATION for
// reference edges. `testing` is excluded: it binds to the hand-owned core/testing shim, whose C#
// declarations are authored by hand rather than converted from the Go declaration this walk would
// read (see declarationClosureImports).
func closureWalkable(named *types.Named) bool {
	object := named.Obj()

	return object != nil && object.Pkg() != nil && object.Pkg().Path() != "testing"
}

// namedTypesIn returns every NAMED type a type expression mentions — what the C# rendering of that
// expression spells out, and therefore what must bind. It descends through the composite forms the
// converter renders as generic instantiations or delegates (ж<T>, slice<T>, array<T>, map<K,V>,
// channel<T>, Func/Action<…>) and through a generic type's ARGUMENTS, but deliberately NOT through
// a named type's own underlying: whether that declaration is walked in turn is the caller's
// recursion decision (see declarationClosureImports' minimality note).
func namedTypesIn(typ types.Type) []*types.Named {
	var result []*types.Named

	visited := map[types.Type]bool{}

	var walk func(types.Type)

	walk = func(current types.Type) {
		if current == nil || visited[current] {
			return
		}

		visited[current] = true

		switch typed := types.Unalias(current).(type) {
		case *types.Named:
			result = append(result, typed)

			for i := range typed.TypeArgs().Len() {
				walk(typed.TypeArgs().At(i))
			}
		case *types.Pointer:
			walk(typed.Elem())
		case *types.Slice:
			walk(typed.Elem())
		case *types.Array:
			walk(typed.Elem())
		case *types.Chan:
			walk(typed.Elem())
		case *types.Map:
			walk(typed.Key())
			walk(typed.Elem())
		case *types.Signature:
			for i := range typed.Params().Len() {
				walk(typed.Params().At(i).Type())
			}

			for i := range typed.Results().Len() {
				walk(typed.Results().At(i).Type())
			}
		case *types.Struct: // an ANONYMOUS struct field type renders its own field types inline
			for i := range typed.NumFields() {
				walk(typed.Field(i).Type())
			}
		case *types.Interface: // likewise an anonymous interface: its method signatures
			for i := range typed.NumMethods() {
				walk(typed.Method(i).Type())
			}
		}
	}

	walk(typ)

	return result
}

// typeSeeds carries the seed sets declarationClosureImports takes from one compilation unit:
// every named type its compiled files MENTION (what an interface base edge starts from) and the
// named types a composite literal CONSTRUCTS (what the fieldwise-constructor edge starts from).
// The constructed set is split by whether the literal bears elements, because only the EMPTY form
// depends on the fieldwise constructor's ACCESSIBILITY (see declarationClosureImports).
type typeSeeds struct {
	named            []*types.Named
	constructed      []*types.Named
	constructedEmpty []*types.Named
	memberBases      []*types.Named
	// memberBound carries the types a compiled test source binds a member on through a form that
	// spells no selector — a builtin call, a range, an index/slice. It feeds ONLY the
	// implemented-interface edge (see declarationClosureImports): the demand is on the type's
	// realized base list, never on its own package, which the spelling already references.
	memberBound []*types.Named
}

// referencedTypeSeeds collects those seeds from the files the test assembly actually COMPILES.
// Phase-4D compile-excluded files (selectCompileExcludedTestFiles) are skipped: they are analyzed
// so their declarations still reach the manifest, but no C# is emitted for them, so they name
// nothing the compilation must bind — seeding from one handed compress/gzip five references its
// example_test.go reached through `http.Request` (see declarationClosureImports' minimality note).
// Walking the syntax tree rather than iterating the TypesInfo maps is what makes the file scoping
// possible at all, and it makes the seed ORDER deterministic as a side effect.
func referencedTypeSeeds(pkg *packages.Package, compileExcluded map[string]bool) typeSeeds {
	var seeds typeSeeds

	if pkg == nil || pkg.TypesInfo == nil {
		return seeds
	}

	add := func(typ types.Type) {
		if typ == nil {
			return
		}

		if named, ok := types.Unalias(typ).(*types.Named); ok {
			seeds.named = append(seeds.named, named)
		}
	}

	for i, file := range pkg.Syntax {
		if i < len(pkg.CompiledGoFiles) && compileExcluded[filepath.Clean(pkg.CompiledGoFiles[i])] {
			continue
		}

		// The member-access edge is scoped to `_test.go` sources: under the REFERENCE model the
		// production files are not in this compilation at all (the internal variant loads them
		// alongside its own, so the scoping has to be per-FILE, not per-package), and under the
		// recompile model they are, but that model already references every production import
		// wholesale — so a production receiver can never be an addition either way. See
		// declarationClosureImports.
		isTestFile := i < len(pkg.CompiledGoFiles) && strings.HasSuffix(pkg.CompiledGoFiles[i], "_test.go")

		ast.Inspect(file, func(node ast.Node) bool {
			switch typed := node.(type) {
			case *ast.Ident:
				if object := pkg.TypesInfo.Uses[typed]; object != nil {
					add(object.Type())
				}

				if object := pkg.TypesInfo.Defs[typed]; object != nil {
					add(object.Type())
				}
			case *ast.SelectorExpr:
				// The MEMBER-ACCESS edge. Resolving `x.M` in C# requires BINDING x's type, and when
				// x is declared in another package that type is spelled nowhere in this compilation
				// — not in an import, not in an alias `using`. `unique`'s white-box suite calls
				// `cleanupMu.Lock()` on the production package's `var cleanupMu sync.Mutex`, the
				// test project referenced no `sync`, and the compile died CS0012 ×2 with no host
				// ever linking. A package-QUALIFIED selector (`sync.Mutex`, `lib.F`) is not this
				// shape: its base is a PkgName, which has no type, so it contributes nothing — and
				// the import that spells it already carries the reference.
				if isTestFile {
					seeds.memberBases = append(seeds.memberBases, namedTypesIn(pkg.TypesInfo.Types[typed.X].Type)...)
				}
			case *ast.RangeStmt:
				// `range list` enumerates the value, which binds a member on its type.
				if isTestFile {
					seeds.memberBound = append(seeds.memberBound, namedTypesIn(pkg.TypesInfo.Types[typed.X].Type)...)
				}
			case *ast.IndexExpr:
				// `list[i]` binds the indexer. A generic INSTANTIATION wears the same node shape;
				// its base is a func/type, not a value, so namedTypesIn over the operand's type
				// yields the instantiated named type — harmless, and still record-gated.
				if isTestFile {
					seeds.memberBound = append(seeds.memberBound, namedTypesIn(pkg.TypesInfo.Types[typed.X].Type)...)
				}
			case *ast.SliceExpr:
				// `list[1:2]` binds the slice member for the same reason.
				if isTestFile {
					seeds.memberBound = append(seeds.memberBound, namedTypesIn(pkg.TypesInfo.Types[typed.X].Type)...)
				}
			case *ast.CallExpr:
				// A BUILTIN call (`len`, `cap`, `append`, `copy`, `clear`, `delete`) lowers to a
				// golib member resolved against the ARGUMENT's type — the shape go/types' failing
				// `len(list)` takes. An ordinary call is NOT this: passing a value to a function
				// with an exact parameter type binds nothing on the value's own type, which is the
				// measured negative (`Order(r)`) this must not cross.
				if isTestFile {
					if identifier, isIdent := typed.Fun.(*ast.Ident); isIdent {
						if _, isBuiltin := pkg.TypesInfo.Uses[identifier].(*types.Builtin); isBuiltin {
							for _, argument := range typed.Args {
								seeds.memberBound = append(seeds.memberBound, namedTypesIn(pkg.TypesInfo.Types[argument].Type)...)
							}
						}
					}
				}
			case *ast.CompositeLit:
				// An IMPLICIT element literal ([]T{{…}}) carries no Type expression of its own;
				// go/types still records the composite's type, which is the one being constructed.
				literalType := pkg.TypesInfo.Types[typed].Type

				add(literalType)

				// An ELEMENT-BEARING literal calls the fieldwise constructor outright. The EMPTY
				// literal — Go's zero value — converts to `new Δsync.Once(nil)`, go2cs-gen's
				// dedicated nil constructor, but the fieldwise overload is still a RESOLUTION
				// CANDIDATE wherever it is accessible; that distinction is the caller's
				// (declarationClosureImports' root-scoped empty-literal edge).
				if named, ok := types.Unalias(literalType).(*types.Named); ok {
					if len(typed.Elts) == 0 {
						seeds.constructedEmpty = append(seeds.constructedEmpty, named)
					} else {
						seeds.constructed = append(seeds.constructed, named)
					}
				}
			case *ast.ValueSpec:
				// The ZERO-VALUE DECLARATION form of the same construction. `var l Logger` names no
				// composite literal anywhere, yet the converter renders Go's zero value as a
				// CONSTRUCTOR CALL — `ref var l = ref heap(new Logger(), out var Ꮡl)` for the
				// address-taken shape, `new Logger()` otherwise — so it makes exactly the demand
				// `Logger{}` does and reaches it by a route no CompositeLit walk can see. `log`'s
				// white-box suite declares `var l Logger` in TestNonNewLogger and the compile died
				// `CS0012 … 'atomic_package.Pointer<>'`. Scoped to `_test.go` for the member-access
				// edge's reason: under the reference model the production files are not in this
				// compilation, and under the recompile model that model already references every
				// production import wholesale. The ROOT/accessibility gate is the caller's, shared
				// with the empty-literal form.
				if isTestFile && len(typed.Values) == 0 {
					for _, name := range typed.Names {
						variable, isVar := pkg.TypesInfo.Defs[name].(*types.Var)

						if !isVar {
							continue
						}

						if named, ok := types.Unalias(variable.Type()).(*types.Named); ok {
							seeds.constructedEmpty = append(seeds.constructedEmpty, named)
						}
					}
				}
			}

			if expression, ok := node.(ast.Expr); ok {
				add(pkg.TypesInfo.Types[expression].Type)
			}

			return true
		})
	}

	return seeds
}

// interfaceBaseCandidates returns the exported interfaces from the DECLARING package's imports that
// the converter can name as C# bases of named — the same candidate match getStructuralInterfaceBases
// makes at the declaration site. See interfaceBaseClosureImports.
func interfaceBaseCandidates(named *types.Named) []*types.Named {
	pkg := named.Obj().Pkg()

	if pkg == nil {
		return nil
	}

	iface, ok := named.Underlying().(*types.Interface)

	if !ok || iface.NumMethods() == 0 {
		return nil
	}

	var result []*types.Named

	for _, imported := range pkg.Imports() {
		scope := imported.Scope()

		for _, name := range scope.Names() {
			typeName, ok := scope.Lookup(name).(*types.TypeName)

			if !ok || !typeName.Exported() || typeName.IsAlias() {
				continue
			}

			candidate, ok := typeName.Type().(*types.Named)

			if !ok || candidate.TypeParams().Len() > 0 {
				continue
			}

			candidateInterface, ok := candidate.Underlying().(*types.Interface)

			if !ok || candidateInterface.NumMethods() == 0 || candidateInterface.NumMethods() >= iface.NumMethods() || !candidateInterface.IsMethodSet() {
				continue
			}

			if types.Implements(named, candidateInterface) {
				result = append(result, candidate)
			}
		}
	}

	return result
}

// implementedInterfaceCandidates returns the exported interfaces from the DECLARING package's
// imports that a CONCRETE named type can carry as a C# base — the counterpart of
// interfaceBaseCandidates for a type whose interfaces reach its declaration through the package's
// emitted `[assembly: GoImplement<T, I>]` records rather than through the Go declaration itself.
// Same candidate gates (exported, non-alias, non-generic, real method set), and both receiver
// forms are tested: a record is written for whichever of T or *T satisfies the interface, and a
// pointer-form record realizes a base on the value type all the same.
//
// This supplies only the CANDIDATE UNIVERSE — the interfaces a record for this type could possibly
// name, resolved to go/types objects the walk can carry on with. It is a wild over-approximation of
// the emitted base list on its own (a record exists only where a CAST was converted, so `os.File`
// satisfies syscall.Conn while os records no such base), which is why the caller gates every
// candidate on the declaring package's actual records rather than on satisfaction — see
// declarationClosureImports' implementEdge for that measurement.
func implementedInterfaceCandidates(named *types.Named) []*types.Named {
	pkg := named.Obj().Pkg()

	if pkg == nil {
		return nil
	}

	pointer := types.NewPointer(named)

	var result []*types.Named

	for _, imported := range pkg.Imports() {
		scope := imported.Scope()

		for _, name := range scope.Names() {
			typeName, ok := scope.Lookup(name).(*types.TypeName)

			if !ok || !typeName.Exported() || typeName.IsAlias() {
				continue
			}

			candidate, ok := typeName.Type().(*types.Named)

			if !ok || candidate.TypeParams().Len() > 0 {
				continue
			}

			candidateInterface, ok := candidate.Underlying().(*types.Interface)

			if !ok || candidateInterface.NumMethods() == 0 || !candidateInterface.IsMethodSet() {
				continue
			}

			if types.Implements(named, candidateInterface) || types.Implements(pointer, candidateInterface) {
				result = append(result, candidate)
			}
		}
	}

	return result
}

// The F15 mixed-tree remap that used to live here is GONE (2026-08-01): the converted standard
// library now lives at src/core, which is exactly where every resolver already emits its
// `$(go2csPath)core\<pkg>` reference, so a test project's stdlib dependencies need no mapping at
// all. F15b's "ONE testing package, period" is now enforced structurally instead: `testing` is
// hand-owned like `unsafe` (the converter never queues it — see stdLibConverter.go), so
// core/testing IS the only testing package and there is nothing left to collide with.
//
// isSelfProjectReference reports whether reference points at the package-under-test's own
// production csproj. The comparison must be on the path's BASE NAME: a raw suffix test drops
// any dependency whose project file name merely ENDS with the target's ("runtime.csproj" ends
// with "time.csproj", so converting time silently lost its runtime reference — 5x CS0234).
//
// The reference is normalized first, and with path.Base rather than filepath.Base, so the base name
// is taken the same way on every host: filepath.Base off Windows does not split on a backslash, so a
// `\`-spelled reference (a pre-F5 corpus, a deployed tree, a hand-authored project) came back whole
// and matched nothing.
func isSelfProjectReference(reference, projectName string) bool {
	return strings.EqualFold(path.Base(normalizeEmittedPath(reference)), projectFileBaseName(projectName)+".csproj")
}

// productionCSFiles enumerates the package's converted PRODUCTION sources — the compile items a
// recompile-model test project adds to its own (see writeTestProject), and the files the B2c alias
// scan must read for it (testProjectAliasScanFiles).
//
// Layout L3: a package whose emitted C# varies by GOOS keeps the varying files in per-GOOS
// subfolders and its production csproj compiles exactly one of them via `$(GoTargetOS)/*.cs`
// (docs/phase4/DESIGN-multiplatform-corpus.md). The test project lists its compile items
// EXPLICITLY, so the same selection has to be made here or the recompiled half is simply missing
// those files. `crypto/x509` is the corpus's only L3 package on the recompile model — every other
// L3 suite takes the reference model, where the production ASSEMBLY carries its per-GOOS half — so
// the omission had never been exercised. What it costs is not subtle: x509's whole Windows verifier
// (windows/verify.cs, windows/root_windows.cs) fell out of the test compilation, and with it
// `Verify`, `VerifyOptions`' fields, `loadSystemRoots`, `domainToReverseLabels` and every error
// type's `Error()` method — 187 errors that name the TEST files, not the missing folder.
//
// The per-GOOS `package_init.cs` belongs in the set for the same reason: a `-tests` run rewrites it
// to declare the `initᴛᴛtests()` partial hook the internal variant's package_init_internal_test.cs
// implements, and a declaration in one compilation with its implementation in another is no hook at
// all. Only the TARGET platform's folder is taken; the others are a different build.
func productionCSFiles(outputPath string, goos string) ([]string, error) {
	result, err := productionCSFilesIn(outputPath, "")

	if err != nil {
		return nil, err
	}

	if len(goos) > 0 && isPlatformSourceFolder(outputPath, goos) {
		platformFiles, err := productionCSFilesIn(filepath.Join(outputPath, goos), goos)

		if err != nil {
			return nil, err
		}

		result = append(result, platformFiles...)
	}

	sort.Strings(result)
	return result, nil
}

// productionCSFilesIn returns the converted production sources directly inside one directory, named
// relative to the package root (so a per-GOOS folder's files carry their `<goos>/` prefix, which is
// exactly the compile-item and scan spelling both callers need). Subdirectories are never
// descended: below a per-GOOS folder there is nothing, and below the package root there are only
// NESTED PACKAGES, which are separate assemblies.
func productionCSFilesIn(directory string, relativeTo string) ([]string, error) {
	entries, err := os.ReadDir(directory)
	if err != nil {
		return nil, err
	}
	result := make([]string, 0)
	for _, entry := range entries {
		name := entry.Name()
		lower := strings.ToLower(name)
		if entry.IsDir() || !strings.HasSuffix(lower, ".cs") || strings.HasSuffix(lower, "_test.cs") ||
			lower == strings.ToLower(PackageInfoFileName) || lower == testPackageInfoFileName || lower == testHostFileName || strings.HasSuffix(lower, ".g.cs") {
			continue
		}
		if relativeTo != "" {
			name = relativeTo + "/" + name
		}
		result = append(result, name)
	}
	return result, nil
}

// testFixturePaths enumerates the package's test fixture inputs — every top-level *.go source
// plus the full testdata/ tree — as sorted slash-relative paths. Shared by copyTestFixtures and
// testInputDigest so staleness detection always sees the CURRENT fixture set (a newly added
// testdata file changes the digest; the manifest's recorded list plays no part — F7).
func testFixturePaths(inputPath string) ([]string, error) {
	paths := make([]string, 0)

	goSources, err := filepath.Glob(filepath.Join(inputPath, "*.go"))
	if err != nil {
		return nil, err
	}
	for _, sourceFile := range goSources {
		paths = append(paths, filepath.Base(sourceFile))
	}

	testdata := filepath.Join(inputPath, "testdata")
	if info, err := os.Stat(testdata); err == nil && info.IsDir() {
		err = filepath.WalkDir(testdata, func(path string, entry fs.DirEntry, walkErr error) error {
			if walkErr != nil {
				return walkErr
			}
			if entry.IsDir() {
				return nil
			}
			rel, err := filepath.Rel(inputPath, path)
			if err != nil {
				return err
			}
			paths = append(paths, filepath.ToSlash(rel))
			return nil
		})
		if err != nil {
			return nil, err
		}
	}

	shared, err := parentRelativeFixturePaths(inputPath)
	if err != nil {
		return nil, err
	}
	paths = append(paths, shared...)

	sort.Strings(paths)
	return paths, nil
}

// testFixtureDirectories enumerates the package directory's IMMEDIATE subdirectory names, sorted.
// The host creates each one (empty) in its isolated run directory, so a test that asks what its own
// working directory contains sees the same SHAPE `go test` shows it.
//
// `go test` runs a package in its real source directory, where the sibling packages nested under it
// are present as subdirectories; the isolated run directory holds only the staged *.go sources and
// testdata, so os's TestReadDir found `read_test.go` but not the `exec` SUBDIRECTORY and failed on
// "exec directory not found". That is environment fidelity, not conversion — os.ReadDir itself was
// probed against `go run` over the same 201-entry directory and is byte-identical, IsDir() included.
//
// NAMES ONLY, one level deep, and empty: that is the measured requirement (across the whole
// validated roster only os, io and math/rand have any subdirectory at all beyond the `testdata`
// already staged with its contents), and mirroring a sibling package's files would stage a second
// copy of the tree for no test that reads one. A test that reads INTO a sibling directory would
// still need its content — none does, and such a read would be a fixture reference, which the
// fixture pass already covers.
func testFixtureDirectories(inputPath string) ([]string, error) {
	entries, err := os.ReadDir(inputPath)
	if err != nil {
		return nil, err
	}

	directories := make([]string, 0)

	for _, entry := range entries {
		if entry.IsDir() {
			directories = append(directories, entry.Name())
		}
	}

	sort.Strings(directories)
	return directories, nil
}

// sharedFixtureRef matches a Go double-quoted literal naming a fixture ABOVE the package —
// "../testdata/e.txt", "../../testdata/Isaac.Newton-Opticks.txt". Go's stdlib spells these as
// plain literals everywhere they occur, so a source scan is exact; nothing builds them with
// filepath.Join.
var sharedFixtureRef = regexp.MustCompile(`"((?:\.\./)+[^"]*)"`)

// parentRelativeFixturePaths finds the fixtures a package's tests read from ABOVE their own
// directory and returns them as "../"-prefixed slash paths — the same shape testFixturePaths
// returns for the package's own testdata, so copyTestFixtures stages them and testInputDigest
// covers them for staleness with no further work (filepath.Join cleans the "../" on both the read
// and the write, landing each file at the mirrored ancestor location under the output root, which
// is what makes the test's own relative open() resolve).
//
// Go shares large fixtures between sibling packages rather than duplicating them: compress/flate,
// compress/zlib and compress/lzw all read ../testdata/{e,pi,gettysburg}.txt, and flate also reads
// ../../testdata/Isaac.Newton-Opticks.txt. Staging only the package's OWN testdata/ left those
// opens failing, which is what kept compress/flate at 61 of 64 tests (and gates image/{draw,gif,
// jpeg,png}, index/suffixarray, internal/zstd and net the same way).
//
// Two constraints keep this bounded. The path must have a "testdata" segment — the universal
// convention for every occurrence in the stdlib — and the resolved source must exist. Together
// they stop an unrelated "../" literal (a URL, a comment fragment, a relative import in a string)
// from being treated as a fixture and reaching outside the tree.
func parentRelativeFixturePaths(inputPath string) ([]string, error) {
	testSources, err := filepath.Glob(filepath.Join(inputPath, "*_test.go"))
	if err != nil {
		return nil, err
	}

	seen := HashSet[string]{}
	paths := make([]string, 0)

	for _, testSource := range testSources {
		contents, err := os.ReadFile(testSource)
		if err != nil {
			return nil, err
		}

		for _, match := range sharedFixtureRef.FindAllStringSubmatch(string(contents), -1) {
			reference := filepath.ToSlash(filepath.Clean(match[1]))

			if !hasPathSegment(reference, "testdata") || seen.Contains(reference) {
				continue
			}

			resolved := filepath.Join(inputPath, filepath.FromSlash(reference))
			info, err := os.Stat(resolved)

			if err != nil {
				// A referenced-but-absent fixture is not this pass's business: the test that reads
				// it fails identically under `go test`, so the differential comparison still agrees.
				continue
			}

			seen.Add(reference)

			if !info.IsDir() {
				paths = append(paths, reference)
				continue
			}

			err = filepath.WalkDir(resolved, func(path string, entry fs.DirEntry, walkErr error) error {
				if walkErr != nil {
					return walkErr
				}
				if entry.IsDir() {
					return nil
				}
				rel, err := filepath.Rel(inputPath, path)
				if err != nil {
					return err
				}
				paths = append(paths, filepath.ToSlash(rel))
				return nil
			})

			if err != nil {
				return nil, err
			}
		}
	}

	return paths, nil
}

// SharedFixtureStagingRoot is the output-directory folder that holds fixtures reaching ABOVE the
// package. They cannot keep their `../` shape under the build output, so each is staged at
// "<root>/up<N>/<tail>" and the test host restores the true relative path inside its run sandbox.
// MUST match TestHost.SharedFixtureStagingRoot.
const SharedFixtureStagingRoot = "go2cs_shared_fixtures"

// sharedFixtureStagingParts splits a fixture path that reaches above the package into the number of
// levels it ascends and the remainder, so it can be staged at a flat, collision-free location:
// "../testdata/e.txt" -> (1, "testdata/e.txt"). The level count is part of the key because two
// different ancestors can hold a same-named file ("../testdata/e.txt" vs "../../testdata/e.txt").
// Reports false for a fixture at or below the package, which needs no staging.
func sharedFixtureStagingParts(fixture string) (int, string, bool) {
	up := 0
	tail := fixture

	for strings.HasPrefix(tail, "../") {
		up++
		tail = tail[len("../"):]
	}

	return up, tail, up > 0 && tail != ""
}

// hasPathSegment reports whether a slash path contains the given segment whole — "testdata/e.txt"
// and "../testdata" match "testdata", "mytestdata.txt" does not.
func hasPathSegment(path, segment string) bool {
	for _, part := range strings.Split(path, "/") {
		if part == segment {
			return true
		}
	}

	return false
}

func copyTestFixtures(inputPath, outputPath string) ([]string, error) {
	fixtures, err := testFixturePaths(inputPath)
	if err != nil {
		return nil, err
	}

	if samePath(inputPath, outputPath) {
		return fixtures, nil
	}

	for _, fixture := range fixtures {
		data, err := os.ReadFile(filepath.Join(inputPath, filepath.FromSlash(fixture)))
		if err != nil {
			return nil, err
		}

		target := filepath.Join(outputPath, filepath.FromSlash(fixture))
		if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
			return nil, err
		}
		if needToWriteFile(target, data) {
			if err := os.WriteFile(target, data, 0644); err != nil {
				return nil, err
			}
		}
	}

	return fixtures, nil
}

func classifyTestSources(inputPath string, included HashSet[string], compileExcluded map[string]bool, external *packages.Package) ([]testSource, error) {
	matches, err := filepath.Glob(filepath.Join(inputPath, "*_test.go"))
	if err != nil {
		return nil, err
	}
	result := make([]testSource, 0, len(matches))
	for _, path := range matches {
		kind := "internal-test"
		if external != nil {
			for _, file := range external.CompiledGoFiles {
				if samePath(file, path) {
					kind = "external-test"
					break
				}
			}
		}
		// compile-excluded is checked BEFORE included: a Phase-4D Example/Benchmark-only file was
		// platform-SELECTED (so it is not platform-excluded) yet is deliberately not compiled, and
		// its distinct status keeps the manifest truthful about why.
		status, reason := "included", ""
		switch {
		case compileExcluded[filepath.Clean(path)]:
			status, reason = compileExcludedSourceStatus, compileExcludedSourceReason
		case !included.Contains(filepath.Clean(path)):
			status, reason = "platform-excluded", "not selected by go/packages for the requested GOOS/GOARCH and build constraints"
		}
		result = append(result, testSource{Path: filepath.ToSlash(filepath.Base(path)), Kind: kind, Status: status, Reason: reason})
	}
	sort.Slice(result, func(i, j int) bool { return result[i].Path < result[j].Path })
	return result, nil
}

// conversionOptionsDigest canonicalizes the OUTPUT-AFFECTING conversion options for the input
// digest (F7): any option that changes emitted C# invalidates the manifest. Machine-specific
// paths (goRoot/goPath/go2csPath) are deliberately excluded so digests stay machine-portable.
func conversionOptionsDigest(options Options) string {
	return fmt.Sprintf("uco=%t;var=%t;indent=%d;comments=%t;cgo=%t",
		options.useChannelOperators, options.preferVarDecl, options.indentSpaces,
		options.includeComments, options.parseCgoTargets)
}

// runtimeSourcesDigest hashes the hand-owned runtime the converted tests build against (golib +
// the go.testing shim) as staged under the converter's go2csPath output root (F7): a runtime
// behavior change invalidates prior comparisons. KNOWN ITEM (review #5, accepted): best-effort
// by design — in a dev tree the runtime is resolved by MSBuild from $(SolutionDir), not the
// converter's output root, so the sources may not be present and a runtime edit then does NOT
// invalidate the manifest ("runtime-unavailable" keeps the digest deterministic either way);
// deployed (deploy-core) and -go2cspath-staged layouts get full invalidation.
func runtimeSourcesDigest(options Options) string {
	var files []string

	for _, dir := range []string{
		filepath.Join(options.go2csPath, "core", "golib"),
		filepath.Join(options.go2csPath, "core", "testing"),
	} {
		if matches, err := filepath.Glob(filepath.Join(dir, "*.cs")); err == nil {
			files = append(files, matches...)
		}
	}

	if len(files) == 0 {
		return "runtime-unavailable"
	}

	sort.Strings(files)
	hash := sha256.New()

	for _, fileName := range files {
		data, err := os.ReadFile(fileName)
		if err != nil {
			return "runtime-unavailable"
		}
		fmt.Fprintf(hash, "%s\x00%d\x00", filepath.Base(fileName), len(data))
		hash.Write(data)
	}

	return "runtime-" + hex.EncodeToString(hash.Sum(nil)[:8])
}

// testInputDigest fingerprints everything that determines a test conversion's outputs: the
// package's Go sources and testdata (globbed FRESH — never from a recorded list, F7), hand-owned
// *_impl.cs companions in the output, the output-affecting conversion options, the staged
// runtime sources, the target platform, the Go toolchain, and the converter revision.
func testInputDigest(inputPath, outputPath string, options Options, revision string) (string, error) {
	hash := sha256.New()

	fixtures, err := testFixturePaths(inputPath)
	if err != nil {
		return "", err
	}

	inputs := make([]string, 0, len(fixtures)+8)
	for _, fixture := range fixtures {
		inputs = append(inputs, "source:"+fixture)
	}

	companions, err := filepath.Glob(filepath.Join(outputPath, "*_impl.cs"))
	if err != nil {
		return "", err
	}
	for _, path := range companions {
		inputs = append(inputs, "output:"+filepath.Base(path))
	}

	// TEST-file companions (`*_impl_test.cs`) are conversion inputs exactly as the production
	// `*_impl.cs` companions above are: editing one must invalidate a prior comparison.
	testCompanions, err := filepath.Glob(filepath.Join(outputPath, "*_impl_test.cs"))
	if err != nil {
		return "", err
	}
	for _, path := range testCompanions {
		inputs = append(inputs, "output:"+filepath.Base(path))
	}

	sort.Strings(inputs)
	for _, taggedPath := range inputs {
		tag, rel, _ := strings.Cut(taggedPath, ":")
		root := inputPath
		if tag == "output" {
			root = outputPath
		}
		data, err := os.ReadFile(filepath.Join(root, filepath.FromSlash(rel)))
		if err != nil {
			return "", err
		}
		fmt.Fprintf(hash, "%s\x00%d\x00", taggedPath, len(data))
		hash.Write(data)
	}

	// The package's immediate subdirectory NAMES are part of the run environment the host builds
	// (testFixtureDirectories), so a subdirectory appearing or disappearing invalidates a prior
	// comparison exactly as a new testdata file does. Names only — their contents are never staged.
	directories, err := testFixtureDirectories(inputPath)
	if err != nil {
		return "", err
	}
	for _, directory := range directories {
		fmt.Fprintf(hash, "dir:%s\x00", directory)
	}

	fmt.Fprintf(hash, "\x00%s\x00%s\x00%s\x00%s\x00%s",
		options.targetPlatform, conversionOptionsDigest(options), runtimeSourcesDigest(options),
		runtime.Version(), revision)
	return hex.EncodeToString(hash.Sum(nil)), nil
}

func writeNoTestsManifest(production *packages.Package, inputPath, outputPath string, target []string, options Options) error {
	projectName, _ := getProjectName(inputPath, options)
	manifest := testManifest{
		SchemaVersion: 1, CapabilitiesVersion: 1, PackageImportPath: production.PkgPath,
		ProjectName: projectName, TestProject: projectFileBaseName(projectName) + ".tests.csproj", GoVersion: runtime.Version(),
		TargetGOOS: target[0], TargetGOARCH: target[1], SourceRevision: gitRevision(inputPath),
		ConverterRevision: converterRevision(), ProductionFiles: []string{}, TestSources: []testSource{},
		Fixtures: []string{}, FixtureDirectories: []string{}, Tests: []testDeclaration{}, Dependencies: []string{}, Capabilities: supportedTestCapabilities(),
		RequiredCapabilities: []string{}, UnsupportedCapabilities: []string{},
	}
	digest, err := testInputDigest(inputPath, outputPath, options, manifest.ConverterRevision)
	if err != nil {
		return fmt.Errorf("compute no-tests manifest input digest: %w", err)
	}
	manifest.InputDigest = digest

	return writeJSONFile(filepath.Join(outputPath, testManifestFileName), manifest)
}

func writeJSONFile(fileName string, value any) error {
	data, err := json.MarshalIndent(value, "", "  ")
	if err != nil {
		return err
	}
	data = append(data, '\n')
	if needToWriteFile(fileName, data) {
		return os.WriteFile(fileName, data, 0644)
	}
	return nil
}

// converterRevision identifies the converter BINARY that produced a manifest. The executable
// hash comes first (F7): hashing the on-disk source directory would report a fresh revision for
// a STALE go2cs.exe — precisely the stale-binary false-green failure mode this project has been
// burned by. VCS build info (when unmodified) and the source-directory digest are fallbacks.
func converterRevision() string {
	if executable, err := os.Executable(); err == nil {
		if data, readErr := os.ReadFile(executable); readErr == nil {
			digest := sha256.Sum256(data)
			return "exe-" + hex.EncodeToString(digest[:8])
		}
	}

	revision := "development"
	modified := false
	if info, ok := debug.ReadBuildInfo(); ok {
		for _, setting := range info.Settings {
			switch setting.Key {
			case "vcs.revision":
				if setting.Value != "" {
					revision = setting.Value
				}
			case "vcs.modified":
				modified = setting.Value == "true"
			}
		}
	}
	if !modified && revision != "development" {
		return revision
	}

	if _, sourceFile, _, ok := runtime.Caller(0); ok {
		sourceFiles, globErr := filepath.Glob(filepath.Join(filepath.Dir(sourceFile), "*.go"))
		if globErr == nil && len(sourceFiles) > 0 {
			sort.Strings(sourceFiles)
			hash := sha256.New()
			complete := true
			for _, fileName := range sourceFiles {
				data, readErr := os.ReadFile(fileName)
				if readErr != nil {
					complete = false
					break
				}
				fmt.Fprintf(hash, "%s\x00%d\x00", filepath.Base(fileName), len(data))
				hash.Write(data)
			}
			if complete {
				return "source-" + hex.EncodeToString(hash.Sum(nil)[:8])
			}
		}
	}

	return revision + "+modified"
}

func gitRevision(path string) string {
	cmd := exec.Command("git", "-C", path, "rev-parse", "HEAD")
	output, err := cmd.Output()
	if err != nil {
		return ""
	}
	return strings.TrimSpace(string(output))
}

func executeTestAction(inputPath, outputPath string, options Options) error {
	projectName, _ := getProjectName(inputPath, options)
	testProject := filepath.Join(outputPath, projectFileBaseName(projectName)+".tests.csproj")
	if err := validateTestManifest(inputPath, outputPath, options); err != nil {
		return err
	}
	manifest, err := readTestManifest(outputPath)
	if err != nil {
		return err
	}

	// Package-level infrastructure blocking applies only when NO converted test can run (a
	// capability-blocked TestMain gates everything; or every declared test blocked itself).
	// Individually blocked tests among runnable siblings are excluded-disclosed instead (F4).
	if blocked := manifestCapabilityBlock(manifest); len(blocked) > 0 {
		result := map[string]any{
			"package": filepath.Base(inputPath), "status": "infrastructure-blocked", "matched": false,
			"errors": []string{"unsupported testing capabilities: " + strings.Join(blocked, ", ")},
		}
		if err := writeJSONFile(filepath.Join(outputPath, "go2cs_test_comparison.json"), result); err != nil {
			return err
		}
		return fmt.Errorf("converted tests are infrastructure-blocked: %s", strings.Join(blocked, ", "))
	}

	if !manifestHasEligibleTests(manifest) {
		if options.testAction == "all" || options.testAction == "compare" {
			result := map[string]any{"package": filepath.Base(inputPath), "status": "not-applicable", "matched": true, "errors": []string{}}
			if err := writeJSONFile(filepath.Join(outputPath, "go2cs_test_comparison.json"), result); err != nil {
				return err
			}
		}
		fmt.Println("No eligible Go tests for the requested target.")
		return nil
	}

	if _, err := os.Stat(testProject); err != nil {
		return fmt.Errorf("test project is missing or stale; run -tests -test-action convert first: %w", err)
	}

	switch options.testAction {
	case "build":
		_, err := runCommandWithTimeout(options.testTimeout, outputPath, options, "dotnet", "build", testProject)
		return err
	case "run":
		output, err := runCommandWithTimeout(testChildTimeout(options), outputPath, options, "dotnet", "run", "--project", testProject, "--",
			"--json", "-timeout", options.testTimeout.String())
		fmt.Print(output)
		return err
	case "compare", "all":
		return compareGoAndConvertedTests(inputPath, outputPath, testProject, options)
	default:
		return nil
	}
}

func readTestManifest(outputPath string) (testManifest, error) {
	var manifest testManifest
	data, err := os.ReadFile(filepath.Join(outputPath, testManifestFileName))
	if err != nil {
		return manifest, fmt.Errorf("test manifest is missing: %w", err)
	}
	if err := json.Unmarshal(data, &manifest); err != nil {
		return manifest, fmt.Errorf("test manifest is invalid: %w", err)
	}
	return manifest, nil
}

func manifestHasEligibleTests(manifest testManifest) bool {
	for _, test := range manifest.Tests {
		if test.Kind == "test" && test.Status == "included" {
			return true
		}
	}
	return false
}

// manifestCapabilityBlock returns the capability names that leave the package with NO runnable
// converted tests: a capability-blocked TestMain gates every test (Go routes all tests through
// it), and a package whose every declared test blocked itself has nothing to run. A blocked test
// among runnable siblings does NOT block the package (F4) — it is excluded-disclosed.
func manifestCapabilityBlock(manifest testManifest) []string {
	capabilityReason := func(declaration testDeclaration) []string {
		if declaration.Status != "unsupported" || !strings.HasPrefix(declaration.Reason, unsupportedCapabilityReasonPrefix) {
			return nil
		}
		return strings.Split(strings.TrimPrefix(declaration.Reason, unsupportedCapabilityReasonPrefix), ", ")
	}

	if manifest.TestMain != nil {
		if blocked := capabilityReason(*manifest.TestMain); len(blocked) > 0 {
			return blocked
		}
	}

	blockedCapabilities := HashSet[string]{}
	hasIncludedTest := false
	hasBlockedTest := false

	for _, test := range manifest.Tests {
		if test.Kind != "test" {
			continue
		}
		if test.Status == "included" {
			hasIncludedTest = true
			continue
		}
		if blocked := capabilityReason(test); len(blocked) > 0 {
			hasBlockedTest = true
			blockedCapabilities.UnionWith(blocked)
		}
	}

	if hasIncludedTest || !hasBlockedTest {
		return nil
	}

	blocked := blockedCapabilities.Keys()
	sort.Strings(blocked)
	return blocked
}

func validateTestManifest(inputPath, outputPath string, options Options) error {
	manifest, err := readTestManifest(outputPath)
	if err != nil {
		return err
	}
	target := strings.Split(options.targetPlatform, "/")
	if len(target) != 2 || manifest.TargetGOOS != target[0] || manifest.TargetGOARCH != target[1] {
		return fmt.Errorf("test manifest is stale: target is %s/%s, requested %s", manifest.TargetGOOS, manifest.TargetGOARCH, options.targetPlatform)
	}
	// The digest recomputes over the CURRENT inputs (fresh fixture glob — a newly added testdata
	// file is a staleness signal the manifest's recorded list could never carry, F7).
	digest, err := testInputDigest(inputPath, outputPath, options, converterRevision())
	if err != nil {
		return fmt.Errorf("validate test manifest inputs: %w", err)
	}
	if digest != manifest.InputDigest {
		return fmt.Errorf("test manifest is stale: input digest changed (run -tests -test-action convert)")
	}
	return nil
}

type normalizedTestEvent struct {
	Test    string  `json:"test"`
	Action  string  `json:"action"`
	Output  string  `json:"output,omitempty"`
	Elapsed float64 `json:"elapsed,omitempty"`
}

type testComparison struct {
	Package   string            `json:"package"`
	Status    string            `json:"status"`
	Go        map[string]string `json:"go"`
	CSharp    map[string]string `json:"csharp"`
	Matched   bool              `json:"matched"`
	Skipped   []string          `json:"skipped"`
	Disclosed []string          `json:"disclosed"`
	Excluded  []string          `json:"excluded"`
	Errors    []string          `json:"errors"`

	// Gated is the row-level detail behind the capability-gated members of Excluded, and it exists
	// because a DECLARATION-keyed gate is not a declaration-sized omission: eligibleTerminalTestResults
	// cuts a verdict row at its first "/", so gating one table-driven test can withdraw dozens of rows
	// from both sides at once. A matched count that silently absorbed that would be the quiet kind of
	// dishonest, so the rows are enumerated from the UNFILTERED `go test` results and published on the
	// proof page. Empty for every package with no gated declaration, which is nearly all of them.
	Gated []capabilityGatedDeclaration `json:"gated,omitempty"`

	// Withdrawn is the same honesty for the DOWNWARD disclosure dual (matchTerminalStatuses'
	// withdrawal rule): the Go-side verdict rows underneath a signature-matched disclosed root,
	// which the converted host never reached because the disclosed failure precedes its case
	// fan-out. Removed from the Go map so every count self-corrects, published here and on the
	// proof page so the omission is stated rather than absorbed. Empty for every package whose
	// disclosed tests have no subtests, which is all of them before crypto/tls's TestBogoSuite.
	Withdrawn []string `json:"withdrawn,omitempty"`
}

// capabilityGatedDeclaration records one test declaration the converted host provably cannot run,
// the capability it would need, and every verdict row `go test` reports underneath it.
type capabilityGatedDeclaration struct {
	Name         string   `json:"name"`
	Capabilities string   `json:"capabilities"`
	Rows         []string `json:"rows"`
}

// testDisclosure pins one test-level disclosed divergence — extending the declaration-level
// "disclosed-unsupported" vocabulary (req §2.7) to individual test outcomes. A hand-owned,
// repo-committed manifest beside the converted package lists tests whose Go=pass/C#=fail
// divergence is provably unsatisfiable in the managed runtime (e.g. the AllocsPerRun
// allocation-count/-profile classes: the CLR allocates where Go's compiler stack-allocates, so
// a malloc-counting shim would fail the same asserts). The signature pin is the integrity
// guard: the oracle reclassifies ONLY a failure whose captured C# output contains the pinned
// substring — a disclosed test failing any OTHER way (a regression beyond the documented
// divergence) is still a mismatch, and a package without a manifest compares strictly.
type testDisclosure struct {
	Name      string `json:"name"`
	Class     string `json:"class"`
	Signature string `json:"signature"`
	Reason    string `json:"reason"`
}

type testDisclosureManifest struct {
	SchemaVersion int              `json:"schemaVersion"`
	Disclosures   []testDisclosure `json:"disclosures"`

	// Notes are optional package-level caveats the generated proof page must carry — facts about
	// the comparison's MEANING that no verdict row can express, rendered verbatim above the
	// verdicts. First consumer: crypto/tls's expired-fixture ceiling, where four tests fail
	// AGREEING on both runtimes because the suite's test certificates expired 2025-01-01 — the
	// agreement is honest, but a reader must know the ceiling moves with the calendar. Hand-owned
	// here rather than hand-edited into the page, because the page is regenerated on every
	// re-validation and a hand edit would not survive one.
	Notes []string `json:"notes,omitempty"`
}

// loadTestDisclosures reads the package's hand-owned disclosure manifest. A missing file is the
// normal case (no disclosures — strict comparison); a malformed or incomplete manifest is an
// error, never a silent no-op, because a broken disclosure must not widen the oracle. Every
// field is required: an empty signature would substring-match ANY failure, defeating the pin.
func loadTestDisclosures(outputPath string) (map[string]testDisclosure, []string, error) {
	data, err := os.ReadFile(filepath.Join(outputPath, testDisclosureFileName))
	if os.IsNotExist(err) {
		return nil, nil, nil
	}
	if err != nil {
		return nil, nil, err
	}

	var manifest testDisclosureManifest
	if err := json.Unmarshal(data, &manifest); err != nil {
		return nil, nil, err
	}

	disclosures := make(map[string]testDisclosure, len(manifest.Disclosures))
	for _, disclosure := range manifest.Disclosures {
		if disclosure.Name == "" || disclosure.Class == "" || disclosure.Signature == "" || disclosure.Reason == "" {
			return nil, nil, fmt.Errorf("disclosure entries require name, class, signature, and reason: %+v", disclosure)
		}
		if _, exists := disclosures[disclosure.Name]; exists {
			return nil, nil, fmt.Errorf("duplicate disclosure for %s", disclosure.Name)
		}
		disclosures[disclosure.Name] = disclosure
	}

	return disclosures, manifest.Notes, nil
}

// matchTerminalStatuses compares the two sides' terminal statuses per test. A test matches when
// both sides report the SAME terminal status (F1) — skip==skip is agreement, disclosed via the
// returned skipped list rather than flagged as failure (real stdlib suites skip routinely). A
// Go=pass/C#=fail divergence pinned by the package's hand-owned disclosure manifest — exact
// test name AND the pinned signature present in the captured C# failure output — is returned
// as disclosed-divergent instead of a mismatch; any other failure shape of a disclosed test
// (different signature, different status pair, a subtest) remains a strict mismatch.
// addressTokenPattern matches a 0x-hex token in a subtest name — a pointer ADDRESS embedded via
// %v/%p, run-varying on BOTH sides by construction (Go's own reruns disagree with themselves).
var addressTokenPattern = regexp.MustCompile(`0x[0-9a-fA-F]+`)

// pairAddressVariantNames re-keys the ONE-SIDED rows of the two result maps whose names differ
// only by embedded 0x-hex address tokens onto a shared normalized key, so the status match
// compares them as one row (errors' TestAsValidation/*string(0xc…) names). This is the SECOND
// phase of matching — exact names already paired stay untouched, so a deterministic hex literal
// used as a subtest name is never collapsed. Only UNAMBIGUOUS 1:1 pairs are re-keyed: a
// normalized key claimed by multiple names on either side, or colliding with an existing exact
// name, keeps all originals — the rows stay one-sided and the comparison fails loud, never
// masking. csOutputs follows the C# rename so disclosure-signature matching keeps its text.
func pairAddressVariantNames(goResults, csResults, csOutputs map[string]string) {
	goOnly := make(map[string][]string)
	csOnly := make(map[string][]string)

	for name := range goResults {
		if _, matched := csResults[name]; !matched {
			if key := addressTokenPattern.ReplaceAllString(name, "0x?"); key != name {
				goOnly[key] = append(goOnly[key], name)
			}
		}
	}

	for name := range csResults {
		if _, matched := goResults[name]; !matched {
			if key := addressTokenPattern.ReplaceAllString(name, "0x?"); key != name {
				csOnly[key] = append(csOnly[key], name)
			}
		}
	}

	for key, goNames := range goOnly {
		csNames := csOnly[key]

		if len(goNames) != 1 || len(csNames) != 1 {
			continue
		}

		if _, exists := goResults[key]; exists {
			continue
		}

		if _, exists := csResults[key]; exists {
			continue
		}

		goResults[key] = goResults[goNames[0]]
		delete(goResults, goNames[0])
		csResults[key] = csResults[csNames[0]]
		delete(csResults, csNames[0])

		if output, ok := csOutputs[csNames[0]]; ok {
			csOutputs[key] = output
			delete(csOutputs, csNames[0])
		}
	}
}

func matchTerminalStatuses(names []string, goResults, csResults map[string]string, disclosures map[string]testDisclosure, csOutputs map[string]string) (mismatches, skipped, disclosed, withdrawn []string) {
	// Deepest names classify FIRST: a subtest failure rolls up to its ancestors in BOTH
	// runtimes, so an ancestor whose Go=pass/C#=fail divergence is PURELY the aggregation of
	// disclosed descendants — no failure output of its own, no own disclosure entry, at least
	// one disclosed descendant, and NO mismatched descendant — is itself disclosed-divergent
	// (encoding/binary's TestSizeAllocs: every failing child is a pinned alloc-profile
	// disclosure; the t.Run parent carries no text). Any other ancestor failure stays a strict
	// mismatch — the aggregation rule can never mask an undisclosed child.
	ordered := make([]string, len(names))
	copy(ordered, names)
	sort.SliceStable(ordered, func(i, j int) bool {
		return strings.Count(ordered[i], "/") > strings.Count(ordered[j], "/")
	})

	// The DOWNWARD dual of that ancestor aggregation, resolvable UP FRONT because it depends
	// only on the manifest and the two top-level rows: a disclosure ROOT is a pinned test whose
	// own Go=pass/C#=fail divergence matches its signature, and a Go-side row UNDERNEATH one —
	// a subtest `go test` ran that the converted host never reached, because the disclosed test
	// failed at its root before its case fan-out — is the disclosed failure's mechanical
	// consequence, not an independent divergence. Those rows are WITHDRAWN: returned by name so
	// the caller reports and subtracts them, never silently dropped (req §2.7), and never
	// widened — a C#-side row that EXISTS under a disclosed root still compares strictly, and a
	// Go-only row under anything that is not a signature-matched disclosure root stays a
	// mismatch. First consumer: crypto/tls's TestBogoSuite, whose Go run fans out 3,242 BoGo
	// case rows the disclosed (host-limit) root failure precedes.
	disclosureRoots := HashSet[string]{}
	for name, disclosure := range disclosures {
		if goResults[name] == "pass" && csResults[name] == "fail" && strings.Contains(csOutputs[name], disclosure.Signature) {
			disclosureRoots.Add(name)
		}
	}

	mismatchNames := HashSet[string]{}
	disclosedNames := HashSet[string]{}

	for _, name := range ordered {
		goStatus, goOK := goResults[name]
		csStatus, csOK := csResults[name]

		if !goOK || !csOK || goStatus != csStatus {
			if goOK && !csOK && underDisclosureRoot(name, disclosureRoots) {
				withdrawn = append(withdrawn, name)
				continue
			}

			if disclosure, ok := disclosures[name]; ok && goStatus == "pass" && csStatus == "fail" {
				if strings.Contains(csOutputs[name], disclosure.Signature) {
					disclosed = append(disclosed, name)
					disclosedNames.Add(name)
					continue
				}
				mismatches = append(mismatches, fmt.Sprintf("%s: Go=%q C#=%q (failure does not match the disclosed %s signature %q)",
					name, goStatus, csStatus, disclosure.Class, disclosure.Signature))
				mismatchNames.Add(name)
				continue
			}

			if goStatus == "pass" && csStatus == "fail" && strings.TrimSpace(csOutputs[name]) == "" {
				prefix := name + "/"
				hasDisclosedDescendant := false
				hasMismatchedDescendant := false

				for descendant := range disclosedNames {
					if strings.HasPrefix(descendant, prefix) {
						hasDisclosedDescendant = true
						break
					}
				}

				for descendant := range mismatchNames {
					if strings.HasPrefix(descendant, prefix) {
						hasMismatchedDescendant = true
						break
					}
				}

				if hasDisclosedDescendant && !hasMismatchedDescendant {
					disclosed = append(disclosed, name)
					disclosedNames.Add(name)
					continue
				}
			}

			mismatches = append(mismatches, fmt.Sprintf("%s: Go=%q C#=%q", name, goStatus, csStatus))
			mismatchNames.Add(name)
			continue
		}

		if goStatus == "skip" {
			skipped = append(skipped, name)
		}
	}

	sort.Strings(withdrawn)

	return mismatches, skipped, disclosed, withdrawn
}

// underDisclosureRoot reports whether name is a strict descendant of a signature-matched
// disclosure root (see the withdrawal rule in matchTerminalStatuses).
func underDisclosureRoot(name string, roots HashSet[string]) bool {
	for {
		idx := strings.LastIndex(name, "/")

		if idx < 0 {
			return false
		}

		name = name[:idx]

		if roots.Contains(name) {
			return true
		}
	}
}

// manifestCensusGaps returns the top-level test names present in the RAW `go test -json` results
// that the manifest cannot account for (F6 census gate). Discovery and comparison otherwise share
// a single point of failure: eligibleTerminalTestResults filters BOTH sides by the manifest, so a
// discovery bug self-censors — a test the converter never discovered is silently removed from the
// comparison and the package can be declared "validated" without it. The census runs against the
// UNFILTERED Go results: every name go test actually ran must be declared in the manifest under
// SOME status (included, capability-blocked, or disclosed-unsupported — examples and fuzz
// seed-corpus runs land here too); subtest names roll up to their top-level parent. Any gap fails
// the comparison — a package cannot validate past a test the manifest never accounted for.
func manifestCensusGaps(goResults map[string]string, manifest testManifest) []string {
	declared := HashSet[string]{}
	for _, test := range manifest.Tests {
		declared.Add(test.Name)
	}
	if manifest.TestMain != nil {
		declared.Add(manifest.TestMain.Name)
	}

	gaps := HashSet[string]{}
	for name := range goResults {
		topLevelName, _, _ := strings.Cut(name, "/")
		if !declared.Contains(topLevelName) {
			gaps.Add(topLevelName)
		}
	}

	result := gaps.Keys()
	sort.Strings(result)
	return result
}

// excludedDeclarations lists every disclosed-unsupported declaration the comparison excludes
// (F2/F3): benchmarks, fuzz targets, Examples, and capability-blocked tests are filtered from
// BOTH sides of the oracle, so the comparison record must say what was excluded and why —
// silent filtering is the exact silent-pass channel req §2.7 forbids.
func excludedDeclarations(manifest testManifest) []string {
	excluded := make([]string, 0)

	for _, test := range manifest.Tests {
		if test.Status != "included" {
			excluded = append(excluded, fmt.Sprintf("%s (%s): %s", test.Name, test.Kind, test.Reason))
		}
	}

	if manifest.TestMain != nil && manifest.TestMain.Status != "included" {
		excluded = append(excluded, fmt.Sprintf("%s (%s): %s", manifest.TestMain.Name, manifest.TestMain.Kind, manifest.TestMain.Reason))
	}

	return excluded
}

// testChildTimeoutGrace is how much longer a test child PROCESS is allowed to live than the package
// deadline it was given. The deadline is enforced IN-process by `go test` and by the converted host,
// both of which write their results on expiry; the outer kill is only a safety net for a child that
// ignores it, so it must fire strictly later or it destroys the very results the deadline produced.
const testChildTimeoutGrace = time.Minute

// testChildTimeout is the outer kill for a test child process — the package deadline plus the grace
// margin above.
func testChildTimeout(options Options) time.Duration {
	return options.testTimeout + testChildTimeoutGrace
}

func compareGoAndConvertedTests(inputPath, outputPath, testProject string, options Options) error {
	// -test-timeout is the PACKAGE deadline, handed to BOTH sides so they agree: `go test -timeout`
	// and the converted host's own `--timeout`. Without it each side silently used its OWN 10-minute
	// default — `go test`'s and TestHost's — so no value of the flag could let a slow suite finish:
	// hash/maphash's C# run self-terminated at exactly 600 s under `-test-timeout 40m`, reporting its
	// still-running TestSmhasherAvalanche as an empty verdict that reads exactly like a real failure
	// (the C# suite needs ~15 min where Go's needs 7.6 s — a performance gap, not a correctness one).
	goOutput, goErr := runCommandWithTimeout(testChildTimeout(options), inputPath, options, "go", "test", "-json", "-count=1",
		"-timeout", options.testTimeout.String(), ".")
	csOutput, csErr := runCommandWithTimeout(testChildTimeout(options), outputPath, options, "dotnet", "run", "--project", testProject, "--", "--json",
		"-timeout", options.testTimeout.String(),
		"--result", filepath.Join(outputPath, "go2cs_test_results.json"), "--junit", filepath.Join(outputPath, "go2cs_test_results.xml"))

	goResults := terminalTestResults(goOutput)
	csResults := terminalTestResults(csOutput)
	csOutputs := terminalTestOutputs(csOutput)
	disclosures, disclosureNotes, disclosureErr := loadTestDisclosures(outputPath)
	var manifest testManifest
	var censusGaps []string
	var gated []capabilityGatedDeclaration
	manifestData, manifestErr := os.ReadFile(filepath.Join(outputPath, testManifestFileName))
	if manifestErr == nil {
		manifestErr = json.Unmarshal(manifestData, &manifest)
		if manifestErr == nil {
			// F6 census gate: computed over the RAW Go results BEFORE the manifest-driven
			// filtering below — the filter shares the manifest with discovery, so only the
			// unfiltered stream can expose a declaration discovery missed.
			censusGaps = manifestCensusGaps(goResults, manifest)
			// Same window, same reason: the rows a capability gate withdraws exist only in the
			// unfiltered stream, and the proof page publishes them so the matched count below
			// never absorbs a subtest silently.
			gated = capabilityGatedDeclarations(goResults, manifest)
			goResults = eligibleTerminalTestResults(goResults, manifest)
			csResults = eligibleTerminalTestResults(csResults, manifest)
		}
	}
	pairAddressVariantNames(goResults, csResults, csOutputs)

	names := make([]string, 0, len(goResults)+len(csResults))
	seen := HashSet[string]{}
	for name := range goResults {
		if seen.Add(name) {
			names = append(names, name)
		}
	}
	for name := range csResults {
		if seen.Add(name) {
			names = append(names, name)
		}
	}
	sort.Strings(names)

	status := "validated"
	if !manifestHasEligibleTests(manifest) {
		status = "not-applicable"
	}
	result := testComparison{
		Package: filepath.Base(inputPath), Status: status, Go: goResults, CSharp: csResults,
		Matched: true, Skipped: []string{}, Disclosed: []string{}, Excluded: excludedDeclarations(manifest), Errors: []string{},
		Gated: gated, Withdrawn: []string{},
	}
	if disclosureErr != nil {
		result.Matched = false
		result.Errors = append(result.Errors, "test disclosures: "+disclosureErr.Error())
		disclosures = nil
	}
	if manifestErr != nil {
		result.Matched = false
		result.Status = "conversion-blocked"
		result.Errors = append(result.Errors, "test manifest: "+manifestErr.Error())
	} else if blocked := manifestCapabilityBlock(manifest); len(blocked) > 0 {
		result.Matched = false
		result.Status = "infrastructure-blocked"
		result.Errors = append(result.Errors, "unsupported testing capabilities: "+strings.Join(blocked, ", "))
	}

	if len(censusGaps) > 0 {
		// F6: go test ran declarations the manifest never accounted for — a DISCOVERY defect,
		// not a test failure. The package must not validate past it.
		result.Matched = false
		result.Errors = append(result.Errors, "census: go test reported tests the manifest does not declare: "+strings.Join(censusGaps, ", "))
	}

	mismatches, skipped, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)
	if len(mismatches) > 0 {
		result.Matched = false
		result.Errors = append(result.Errors, mismatches...)
	}
	result.Skipped = append(result.Skipped, skipped...)
	for _, name := range disclosed {
		disclosure := disclosures[name]
		result.Disclosed = append(result.Disclosed, fmt.Sprintf("%s (%s): %s", name, disclosure.Class, disclosure.Reason))
	}

	// Withdrawn rows leave the comparison record the way capability-gated rows do: removed from
	// the Go map (they are Go-only by construction, so nothing exists on the C# side) and
	// PUBLISHED in their own field, so the matched count below and every proof-page derivation
	// self-correct while the omission stays visible — silent absorption is the channel §2.7
	// forbids.
	for _, name := range withdrawn {
		delete(goResults, name)
	}
	result.Withdrawn = append(result.Withdrawn, withdrawn...)

	// Whether at least one failure is AGREED — both runtimes reporting "fail" for the same row.
	// An agreed failure is a matched verdict, and it is the one legitimate reason a side's exit
	// code goes nonzero without any divergence existing.
	agreedFailure := false
	for name, goStatus := range goResults {
		if goStatus == "fail" && csResults[name] == "fail" {
			agreedFailure = true
			break
		}
	}

	if goErr != nil && csErr != nil && agreedFailure && len(mismatches) == 0 && len(goResults) > 0 && len(csResults) > 0 {
		// The MIRROR of the C# forgiveness below: go test's nonzero exit is the agreed outcome
		// of failures BOTH runtimes report identically, so the exit codes carry no information
		// the per-test rows have not already matched. First consumer: crypto/tls, whose test
		// fixtures expired 2025-01-01 and fail four resumption/verification tests with the same
		// `x509: certificate has expired` text on either runtime — the most that suite can
		// score in either language, worsening with the calendar. Narrow on the same terms as
		// the arm below: zero mismatches (a truncated or divergent run stays fatal), both runs
		// produced results, at least one agree-fail row exists to attribute the exits to, and
		// BOTH sides exited nonzero — a red Go baseline beside a green converted run is a
		// divergence, never a forgiveness.
		goErr = nil
	}

	if csErr != nil && goErr == nil && (len(disclosed) > 0 || agreedFailure) && len(mismatches) == 0 && len(csResults) > 0 {
		// The converted host exits nonzero BECAUSE the disclosed-divergent tests fail — that
		// exit code is part of the disclosed outcome, not an additional failure signal.
		// Forgiveness is deliberately narrow: go test itself was clean (or its own exit was
		// just forgiven on agreed failures, which carry a C# exit exactly as disclosed rows
		// do), the host produced results, and every divergence matched its pinned signature
		// (zero mismatches — a truncated run surfaces as one-sided rows, which are mismatches,
		// and stays fatal).
		csErr = nil
	}

	if goErr != nil {
		result.Matched = false
		result.Status = "failing"
		result.Errors = append(result.Errors, "go test: "+goErr.Error())
	}
	if csErr != nil {
		result.Matched = false
		if result.Status != "infrastructure-blocked" {
			// Parsed events prove the converted host RAN: a nonzero exit with results is a
			// genuine test failure (`failing`). `conversion-blocked` is reserved for actual
			// conversion/build/run infrastructure causes — the host produced no events at all.
			if len(csResults) > 0 {
				result.Status = "failing"
			} else {
				result.Status = "conversion-blocked"
			}
		}
		result.Errors = append(result.Errors, "converted tests: "+csErr.Error())
	}
	if !result.Matched && result.Status == "validated" {
		result.Status = "failing"
	}
	if err := writeJSONFile(filepath.Join(outputPath, "go2cs_test_comparison.json"), result); err != nil {
		return err
	}
	if !result.Matched {
		return fmt.Errorf("Go/C# test comparison failed: %s", strings.Join(result.Errors, "; "))
	}
	// The differential that just proved the package is the proof: publish it as a committed page
	// under docs/validation (no-op outside a repository checkout). See validationProofPages.go.
	if result.Status == "validated" {
		if err := emitValidationProofPage(outputPath, result, manifest, disclosures, disclosureNotes, options); err != nil {
			return fmt.Errorf("write validation proof page: %w", err)
		}
	}
	if len(disclosed) > 0 {
		classes := HashSet[string]{}
		for _, name := range disclosed {
			classes.Add(disclosures[name].Class)
		}
		classList := classes.Keys()
		sort.Strings(classList)
		fmt.Printf("Validated %d tests against go test (%d skipped identically on both sides, %d disclosed-divergent (%s), %d disclosed-unsupported declarations excluded).\n",
			len(goResults)-len(disclosed), len(result.Skipped), len(disclosed), strings.Join(classList, ", "), len(result.Excluded))
	} else {
		fmt.Printf("Validated %d tests against go test (%d skipped identically on both sides, %d disclosed-unsupported declarations excluded).\n",
			len(goResults), len(result.Skipped), len(result.Excluded))
	}
	return nil
}

func terminalTestResults(output string) map[string]string {
	result := make(map[string]string)
	for _, line := range strings.Split(output, "\n") {
		var event normalizedTestEvent
		if json.Unmarshal([]byte(line), &event) != nil || event.Test == "" {
			continue
		}
		switch event.Action {
		case "pass", "fail", "skip", "timeout", "infrastructure-error":
			result[event.Test] = event.Action
		}
	}
	return result
}

// terminalTestOutputs captures each test's accumulated log output from its terminal event —
// the converted host attaches the joined t.Log/t.Error text to the terminal record — keyed by
// test name, for disclosure signature matching against the C# side's failure messages.
func terminalTestOutputs(output string) map[string]string {
	result := make(map[string]string)
	for _, line := range strings.Split(output, "\n") {
		var event normalizedTestEvent
		if json.Unmarshal([]byte(line), &event) != nil || event.Test == "" {
			continue
		}
		switch event.Action {
		case "pass", "fail", "skip", "timeout", "infrastructure-error":
			result[event.Test] = event.Output
		}
	}
	return result
}

// capabilityGatedDeclarations enumerates, per capability-gated declaration, the verdict rows the
// UNFILTERED `go test` run produced for it — the declaration itself plus every subtest underneath.
// It must be called before eligibleTerminalTestResults, which is precisely what removes those rows;
// after the filter the information no longer exists anywhere.
//
// Ordering is fully determined here — declarations and rows are both sorted — so the proof page this
// feeds is byte-stable for a given row SET. The set itself comes from a live `go test`, which is the
// one thing this cannot pin: a gated test whose subtests vary run to run would churn its page on
// every sweep, where a non-gated one would fail the verdict count instead. No such test is gated
// today (both os/exec candidates are fixed tables), and the first that is should be checked for it.
func capabilityGatedDeclarations(goResults map[string]string, manifest testManifest) []capabilityGatedDeclaration {
	gated := make(map[string]string)

	for _, test := range manifest.Tests {
		if test.Status == "unsupported" && strings.HasPrefix(test.Reason, unsupportedCapabilityReasonPrefix) {
			gated[test.Name] = strings.TrimPrefix(test.Reason, unsupportedCapabilityReasonPrefix)
		}
	}

	if len(gated) == 0 {
		return nil
	}

	rows := make(map[string][]string)

	for name := range goResults {
		topLevelName, _, _ := strings.Cut(name, "/")

		if _, blocked := gated[topLevelName]; blocked {
			rows[topLevelName] = append(rows[topLevelName], name)
		}
	}

	names := make([]string, 0, len(gated))

	for name := range gated {
		names = append(names, name)
	}

	sort.Strings(names)

	declarations := make([]capabilityGatedDeclaration, 0, len(names))

	for _, name := range names {
		declarationRows := rows[name]
		sort.Strings(declarationRows)
		declarations = append(declarations, capabilityGatedDeclaration{
			Name: name, Capabilities: gated[name], Rows: declarationRows,
		})
	}

	return declarations
}

func eligibleTerminalTestResults(results map[string]string, manifest testManifest) map[string]string {
	eligible := HashSet[string]{}
	for _, test := range manifest.Tests {
		if test.Kind == "test" && test.Status == "included" {
			eligible.Add(test.Name)
		}
	}

	filtered := make(map[string]string)
	for name, status := range results {
		topLevelName, _, _ := strings.Cut(name, "/")
		if eligible.Contains(topLevelName) {
			filtered[name] = status
		}
	}
	return filtered
}

func runCommandWithTimeout(timeout time.Duration, workingDir string, options Options, name string, args ...string) (string, error) {
	ctx, cancel := context.WithTimeout(context.Background(), timeout)
	defer cancel()
	cmd := exec.CommandContext(ctx, name, args...)
	cmd.Dir = workingDir
	target := strings.Split(options.targetPlatform, "/")
	cmd.Env = append(os.Environ(), "go2csPath="+ensureTrailingSeparator(options.go2csPath))
	if len(target) == 2 {
		cmd.Env = append(cmd.Env, "GOOS="+target[0], "GOARCH="+target[1])
	}
	if len(options.goRoot) > 0 {
		// Hand both sides the same GOROOT explicitly. `go test` resolves it on its own, but the
		// converted C# host has no linker-baked defaultGOROOT to fall back on — runtime.GOROOT()
		// reads the environment — so testenv.GOROOT consumers agree with Go only when the pipeline
		// exports the root it converted from. Duplicate keys are fine: os/exec takes the last value.
		cmd.Env = append(cmd.Env, "GOROOT="+options.goRoot)

		// `go test` PREPENDS $GOROOT/bin to the test binary's PATH, so a test that shells out to
		// `go` gets the toolchain matching the GOROOT it was built against. Measured against Go
		// 1.23.1: inside a test, PATH[0] is $GOROOT/bin and exec.LookPath("go") resolves there.
		// Without the same treatment the converted host resolves `go` from the ambient PATH, which
		// on a machine with more than one installation is a DIFFERENT go of the same version —
		// internal/testenv's TestGoToolLocation compares ../../../bin/go against
		// exec.LookPath("go") with os.SameFile and fails on exactly that difference, and
		// internal/godebugs shells out to `go list std cmd`. Reproducing go test's environment is
		// the harness's job, and PATH is part of that environment just as GOROOT and the working
		// directory are.
		cmd.Env = append(cmd.Env, "PATH="+filepath.Join(options.goRoot, "bin")+string(os.PathListSeparator)+os.Getenv("PATH"))
	}
	output, err := cmd.CombinedOutput()
	if ctx.Err() == context.DeadlineExceeded {
		return string(output), fmt.Errorf("%s timed out after %s", name, timeout)
	}
	if err != nil {
		return string(output), fmt.Errorf("%s %s failed: %w\n%s", name, strings.Join(args, " "), err, strings.TrimSpace(string(output)))
	}
	return string(output), nil
}

func ensureTrailingSeparator(path string) string {
	return strings.TrimRight(path, `/\`) + string(filepath.Separator)
}

func removeString(values []string, target string) []string {
	result := values[:0]
	for _, value := range values {
		if value != target {
			result = append(result, value)
		}
	}
	return result
}

func samePath(left, right string) bool {
	leftAbs, _ := filepath.Abs(left)
	rightAbs, _ := filepath.Abs(right)
	return strings.EqualFold(filepath.Clean(leftAbs), filepath.Clean(rightAbs))
}

// escapeCSharp escapes a value for a C# regular (non-verbatim) string literal, so a Windows path
// emitted into generated test-host source survives as written rather than turning its separators
// into escape sequences.
//
// XML attribute values use escapeXMLAttributeValue (solutionGenerator.go) instead.
func escapeCSharp(value string) string {
	return strings.ReplaceAll(strings.ReplaceAll(value, `\`, `\\`), `"`, `\"`)
}
