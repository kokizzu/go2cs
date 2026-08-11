// refLoweringAnalysis_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Guards the ж-box arc's A1 classification pass (docs/phase4/DESIGN-zh-box-reduction.md §3.2/§3.3,
// refLoweringAnalysisOperations.go): the D1/D1′/D2 whitelist, every X veto family, the two-sided
// fixed point (forward strips and caller-shape strips), the §3.3 argument-shape census rows, the
// address-taken-local reversion census, the exported-candidate (A′) flag, and the §3.5
// production-files-only determinism invariant (an export_test.go func-value alias must not
// desynchronize the -tests classification from the -stdlib one).

package main

import (
	"go/ast"
	"strings"
	"testing"

	"golang.org/x/tools/go/packages"
)

// loadRefLoweringFixture writes a one-package fixture module and loads it with full syntax.
func loadRefLoweringFixture(t *testing.T, files map[string]string, withTests bool) *packages.Package {
	t.Helper()

	dir := t.TempDir()
	writeModuleFiles(t, dir, files)

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: dir, Tests: withTests}, ".")

	if err != nil {
		t.Fatalf("fixture load: %v", err)
	}

	if !withTests {
		if len(loaded) != 1 {
			t.Fatalf("fixture load: expected 1 package, got %d", len(loaded))
		}

		return loaded[0]
	}

	// With Tests: pick the test-augmented variant (its Syntax includes the _test.go files) —
	// the same merged white-box package the -tests driver type-checks.
	for _, pkg := range loaded {
		for _, file := range pkg.Syntax {
			filename := pkg.Fset.Position(file.Pos()).Filename

			if strings.HasSuffix(filename, "_test.go") {
				return pkg
			}
		}
	}

	t.Fatal("fixture load: no test-augmented package variant found")

	return nil
}

// analyzeFixture runs the classification pass exactly as the census driver does.
func analyzeFixture(t *testing.T, pkg *packages.Package) *refLoweringPackageResult {
	t.Helper()

	if len(pkg.Errors) > 0 {
		t.Fatalf("fixture did not type-check cleanly: %v", pkg.Errors)
	}

	return analyzeRefLowering(pkg.Fset, pkg.Syntax, pkg.Types, pkg.TypesInfo, censusLinknameHandles(pkg.Syntax))
}

// paramOf fetches one parameter verdict, failing loudly when the shape moved.
func paramOf(t *testing.T, result *refLoweringPackageResult, funcName string, index int) *refParamVerdict {
	t.Helper()

	verdict, ok := result.Funcs[funcName]

	if !ok {
		t.Fatalf("function %q not classified", funcName)
	}

	for _, param := range verdict.Params {
		if param.Index == index {
			return param
		}
	}

	t.Fatalf("function %q has no pointer parameter at index %d", funcName, index)

	return nil
}

// hasVeto reports whether a veto tag (exact) was recorded.
func hasVeto(param *refParamVerdict, tag string) bool {
	for _, veto := range param.Vetoes {
		if veto == tag {
			return true
		}
	}

	return false
}

const refLoweringCoreFixture = `package fixture

import "unsafe"

type pair struct {
	x uint64
	y uint64
}

func (p *pair) bump() { p.x++ }

var sink *int
var sunkAddr *uint64

// D1: pure deref traffic — both parameters lower.
func sub(out, a *uint64) { *out = *a + 1 }

// D1': derived field addresses fed to lowered positions — lowers.
func fieldFwd(p *pair) { sub(&p.x, &p.y) }

// D2: forwarding into lowered positions — lowers.
func fwd(p *uint64) { sub(p, p) }

// D2 chain: lowers only because fwd lowers (fixed-point transitivity).
func chainFwd(p *uint64) { fwd(p) }

// X1: nil comparison.
func nilCheck(p *int) bool { return p == nil }

// X2-return: the constructor/self-return shape.
func constructor(p *int) *int { return p }

// X2-escape: stored into a global.
func store(p *int) { sink = p }

// Forward into a vetoed callee: strips at the fixed point (forward-unlowered).
func chainBroken(p *int) { store(p) }

// X2-capture: referenced inside a closure body.
func capture(p *int) func() int { return func() int { return *p } }

// X2-defer-arg: a derived address in a defer frame (the boxed thunk needs a box the lowered
// parameter would no longer have).
func deferArg(p *pair) {
	defer sub(&p.x, &p.x)
	p.y = 1
}

// X3: representation.
func repr(p *int) uintptr { return uintptr(unsafe.Pointer(p)) }

// X3: a method call on p (receivers stay ж in Phase A).
func methodOn(p *pair) { p.bump() }

// X4: re-pointing the parameter.
func repoint(p *int, q *int) { p = q; _ = p }

// Variadic pointer parameter: a SLICE of pointers, never a candidate.
func variadicPtr(ps ...*int) {
	for range ps {
	}
}

// Forward into a variadic position: the pointer lands in the arg slice (X2).
func variadicFwd(p *int) { variadicPtr(p) }

// X5-named-pointer: the wrapper's operator surface must survive.
type ptrT *int

func namedPtr(p ptrT) { _ = p }

// X5-func-value: otherwise clean, but taken as a value below.
func fwdValue(p *uint64) { sub(p, p) }

var handler = fwdValue

// Exported and clean: not lowered in Phase A, exported-candidate under A'.
func Clean(out, a *uint64) { sub(out, a) }
`

func TestRefLoweringClassificationCore(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	pkg := loadRefLoweringFixture(t, map[string]string{
		"go.mod":     "module example.com/fixture\n\ngo 1.23\n",
		"fixture.go": refLoweringCoreFixture,
	}, false)

	result := analyzeFixture(t, pkg)

	// The D family lowers.
	for _, tc := range []struct {
		fn    string
		index int
	}{
		{"sub", 0}, {"sub", 1}, {"fieldFwd", 0}, {"fwd", 0}, {"chainFwd", 0},
	} {
		param := paramOf(t, result, tc.fn, tc.index)

		if !param.LoweredA {
			t.Errorf("%s#%d must lower (vetoes %v, strippedBy %q)", tc.fn, tc.index, param.Vetoes, param.StrippedBy)
		}
	}

	// Each X family vetoes with its own tag.
	for _, tc := range []struct {
		fn    string
		index int
		tag   string
	}{
		{"nilCheck", 0, refVetoX1Identity},
		{"constructor", 0, refVetoX2Return},
		{"store", 0, refVetoX2Escape},
		{"capture", 0, refVetoX2Capture},
		{"deferArg", 0, refVetoX2DeferArg},
		{"repr", 0, refVetoX3Repr},
		{"methodOn", 0, refVetoX3Repr},
		{"repoint", 0, refVetoX4Repoint},
		{"variadicFwd", 0, refVetoX2Escape},
		{"namedPtr", 0, refVetoX5NamedPtr},
	} {
		param := paramOf(t, result, tc.fn, tc.index)

		if param.LoweredA {
			t.Errorf("%s#%d must NOT lower", tc.fn, tc.index)
		}

		if !hasVeto(param, tc.tag) {
			t.Errorf("%s#%d missing veto %s (recorded %v)", tc.fn, tc.index, tc.tag, param.Vetoes)
		}
	}

	// A variadic ...*T parameter is a slice, never a candidate.
	if verdict := result.Funcs["variadicPtr"]; verdict == nil {
		t.Error("variadicPtr not classified")
	} else if len(verdict.Params) != 0 {
		t.Errorf("variadicPtr must have no candidate pointer params, got %d", len(verdict.Params))
	}

	// The fixed point strips a forward into a vetoed callee.
	broken := paramOf(t, result, "chainBroken", 0)

	if broken.LoweredA {
		t.Error("chainBroken#0 must strip: it forwards into store, which is X2-vetoed")
	}

	if !strings.HasPrefix(broken.StrippedBy, refVetoForward) {
		t.Errorf("chainBroken#0 strippedBy = %q, want %s:*", broken.StrippedBy, refVetoForward)
	}

	// X5-func-value is function-level.
	if verdict := result.Funcs["fwdValue"]; verdict == nil {
		t.Error("fwdValue not classified")
	} else {
		found := false

		for _, veto := range verdict.FuncVetoes {
			if veto == refFuncVetoFuncValue {
				found = true
			}
		}

		if !found {
			t.Errorf("fwdValue must carry %s (recorded %v)", refFuncVetoFuncValue, verdict.FuncVetoes)
		}
	}

	// Exported functions stay un-lowered in Phase A and flag as A' candidates.
	clean := paramOf(t, result, "Clean", 0)

	if clean.LoweredA {
		t.Error("Clean#0 must not lower in Phase A (exported)")
	}

	unionFuncs := map[string]*refFuncVerdict{}

	for name, verdict := range result.Funcs {
		unionFuncs[result.PkgPath+"|"+name] = verdict
	}

	lowered, stripped := resolveRefLoweringFixedPoint(unionFuncs, result.CallArgs, true)
	applyRefLoweringVerdicts(unionFuncs, lowered, stripped, true)

	if !result.Funcs["Clean"].ExportedCandidate {
		t.Error("Clean must flag as an exported candidate under the A' fixed point")
	}

	if !paramOf(t, result, "Clean", 0).LoweredAPrime {
		t.Error("Clean#0 must lower under the A' fixed point")
	}
}

func TestRefLoweringSyntheticFixedPointStrips(t *testing.T) {
	// The resolver in isolation: a caller-shape strip (an other-veto argument shape) and the D2
	// cascade it triggers — the two-sided fixed point of §3.2.
	pos := func(name string) refPosKey { return refPosKey{PkgPath: "p", Func: name, Index: 0} }

	funcs := map[string]*refFuncVerdict{
		"leaf": {PkgPath: "p", Name: "leaf", Params: []*refParamVerdict{{Index: 0, Name: "a"}}},
		"mid": {PkgPath: "p", Name: "mid", Params: []*refParamVerdict{{
			Index: 0, Name: "b", Forwards: []refForward{{Target: pos("leaf"), Kind: "D2"}},
		}}},
		"top": {PkgPath: "p", Name: "top", Params: []*refParamVerdict{{
			Index: 0, Name: "c", Forwards: []refForward{{Target: pos("mid"), Kind: "D2"}},
		}}},
	}

	callArgs := []refCallArg{{Target: pos("leaf"), Shape: refShapeOtherVeto, Detail: "synthetic"}}

	lowered, stripped := resolveRefLoweringFixedPoint(funcs, callArgs, false)

	if lowered[pos("leaf")] || lowered[pos("mid")] || lowered[pos("top")] {
		t.Errorf("caller-shape strip must cascade through the D2 chain: lowered=%v", lowered)
	}

	if !strings.HasPrefix(stripped[pos("leaf")], refVetoCallerShape) {
		t.Errorf("leaf strip reason = %q, want %s:*", stripped[pos("leaf")], refVetoCallerShape)
	}

	if !strings.HasPrefix(stripped[pos("mid")], refVetoForward) || !strings.HasPrefix(stripped[pos("top")], refVetoForward) {
		t.Errorf("chain strips must be forward-tagged: mid=%q top=%q", stripped[pos("mid")], stripped[pos("top")])
	}

	// Control: without the shape veto the whole chain lowers.
	lowered, _ = resolveRefLoweringFixedPoint(funcs, nil, false)

	if !lowered[pos("leaf")] || !lowered[pos("mid")] || !lowered[pos("top")] {
		t.Errorf("control chain must lower end to end: lowered=%v", lowered)
	}
}

const refLoweringShapeFixture = `package shapes

type mont [4]uint64

type elem struct{ x mont }

type big struct {
	a uint64
	b uint64
}

var globalBig big

// The lowered sinks.
func sink(p *big)          { p.a++ }
func sinkArr(p *[4]uint64) { p[0]++ }
func sub2(out *uint64)     { *out = 7 }

func mk() *big { return &big{} }

type holder struct{ f big }

// row 1: field address of a deref'd receiver base.
func (h *holder) row1() { sink(&h.f) }

// rows 2/6/7 and defer-go.
func rows() {
	var x big
	sink(&x)          // row 2 (local)
	sink(&globalBig)  // row 2 (global detail)
	sink(&big{})      // row 6 composite
	sink(new(big))    // row 6 new
	sink(mk())        // row 6 call result
	sink(nil)         // row 7
	var d big
	defer sink(&d) // defer-go bucket; d keeps its box
	_ = d
	_ = x
}

// row 3: a pointer variable forwarded (also a D2 use of q).
func relay(q *big) { sink(q) }

// row 4: element address.
func row4() {
	var arr [3]uint64
	sub2(&arr[0])
}

// row 5: the fiat conversion shape.
func conv(e *elem) { sinkArr((*[4]uint64)(&e.x)) }
`

func TestRefLoweringCallSiteShapes(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	pkg := loadRefLoweringFixture(t, map[string]string{
		"go.mod":    "module example.com/shapes\n\ngo 1.23\n",
		"shapes.go": refLoweringShapeFixture,
	}, false)

	result := analyzeFixture(t, pkg)
	summary := summarizeRefLowering(result)

	expected := map[string]int{
		refShapeFieldAddr:  2, // (&h.f) + conv's (&e.x) inside the row-5 conversion? No: conv classifies row5 at the arg; &h.f and... see below
		refShapeLocalAddr:  3, // &x, &globalBig(global), &arr[0]? No — &arr[0] is row 4. Recounted below.
		refShapePointerVar: 1, // relay's q
		refShapeElemAddr:   1, // &arr[0]
		refShapePtrConv:    1, // (*[4]uint64)(&e.x)
		refShapeTempNeeded: 3, // &big{}, new(big), mk()
		refShapeNilLit:     1, // nil
		refShapeDeferGo:    1, // defer sink(&d)
	}

	// Correct the two mis-annotated rows above (kept as comments for the arithmetic): the shape
	// walk classifies &h.f as row 1 (selector operand) and &e.x reaches the census only inside
	// the row-5 conversion, so field-addr counts exactly 1; &x and &globalBig are the two row-2
	// entries.
	expected[refShapeFieldAddr] = 1
	expected[refShapeLocalAddr] = 2

	for shape, want := range expected {
		if got := summary.ShapeCounts[shape]; got != want {
			t.Errorf("shape %s: got %d, want %d (full: %v)", shape, got, want, summary.ShapeCounts)
		}
	}

	if got := summary.ShapeCounts[refShapeOtherVeto]; got != 0 {
		t.Errorf("no other-veto shapes expected in the fixture, got %d", got)
	}

	// The row-5 site is recorded with conversion-of-address detail (the §10.3 temp rule's priced
	// constituency).
	found := false

	for _, callArg := range result.CallArgs {
		if callArg.Shape == refShapePtrConv {
			found = true

			if callArg.Detail != "conv-of-address" {
				t.Errorf("row-5 detail = %q, want conv-of-address", callArg.Detail)
			}
		}
	}

	if !found {
		t.Error("row-5 conversion site not recorded")
	}
}

const refLoweringLocalsFixture = `package locals

type mutexish struct{ held bool }

func (m *mutexish) lock() { m.held = true }

var escape *uint64

func sub3(out *uint64) { *out = 3 }

func use(v uint64) uint64   { return v }
func useSlice(b []byte) int { return len(b) }

// Reverts: every address use feeds a lowered position.
func lowersLocal() uint64 {
	var x uint64
	sub3(&x)
	return use(x)
}

// Kept: the address also escapes to a global.
func keptStore() {
	var y uint64
	sub3(&y)
	escape = &y
}

// Kept: the address flows to a lowered position under defer (carve-out (a)).
func keptDefer() {
	var z uint64
	defer sub3(&z)
	_ = z
}

// Kept: an implicit address-take by a pointer-receiver method.
func keptMethod() {
	var m mutexish
	m.lock()
	_ = m.held
}

// Kept: slicing an array local aliases its storage.
func keptSlice() int {
	var a [4]byte
	return useSlice(a[:])
}
`

func TestRefLoweringLocalsCensus(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	pkg := loadRefLoweringFixture(t, map[string]string{
		"go.mod":    "module example.com/locals\n\ngo 1.23\n",
		"locals.go": refLoweringLocalsFixture,
	}, false)

	result := analyzeFixture(t, pkg)

	verdictFor := func(funcName, varName string) *refLocalVerdict {
		t.Helper()

		for i := range result.Locals {
			local := &result.Locals[i]

			if local.Func == funcName && local.Name == varName {
				return local
			}
		}

		t.Fatalf("local %s.%s not in the census (have %+v)", funcName, varName, result.Locals)

		return nil
	}

	if v := verdictFor("lowersLocal", "x"); !v.Lowers {
		t.Errorf("lowersLocal.x must revert (kept: %v)", v.KeptReasons)
	}

	for _, tc := range []struct {
		fn, name, reason string
	}{
		{"keptStore", "y", "addr-escapes"},
		{"keptDefer", "z", "defer-go-arg"},
		{"keptMethod", "m", "ptr-receiver"},
		{"keptSlice", "a", "array-slice"},
	} {
		v := verdictFor(tc.fn, tc.name)

		if v.Lowers {
			t.Errorf("%s.%s must keep its box", tc.fn, tc.name)
			continue
		}

		found := false

		for _, reason := range v.KeptReasons {
			if reason == tc.reason {
				found = true
			}
		}

		if !found {
			t.Errorf("%s.%s kept reasons %v, want %s", tc.fn, tc.name, v.KeptReasons, tc.reason)
		}
	}
}

func TestRefLoweringProductionOnlyDeterminism(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	// The §3.5 invariant, at the analysis level: an export_test.go func-value alias must not
	// change the production classification, because the pass reads production files only. The
	// -tests driver hands the pass production entries AND the entry point filters _test.go
	// structurally; this test proves the underlying rule both ways.
	pkg := loadRefLoweringFixture(t, map[string]string{
		"go.mod": "module example.com/detrm\n\ngo 1.23\n",
		"detrm.go": `package detrm

func lowerme(out *uint64) { *out = 1 }

func caller() {
	var x uint64
	lowerme(&x)
	_ = x
}
`,
		"export_test.go": `package detrm

// The white-box alias shape: a func value over the unexported function.
var Hook = lowerme
`,
	}, true)

	var production, all []*ast.File

	for _, file := range pkg.Syntax {
		filename := pkg.Fset.Position(file.Pos()).Filename
		all = append(all, file)

		if !strings.HasSuffix(filename, "_test.go") {
			production = append(production, file)
		}
	}

	if len(production) == len(all) {
		t.Fatal("fixture must include a _test.go file in the merged package")
	}

	prodResult := analyzeRefLowering(pkg.Fset, production, pkg.Types, pkg.TypesInfo, censusLinknameHandles(production))

	if param := paramOf(t, prodResult, "lowerme", 0); !param.LoweredA {
		t.Errorf("production-only classification must lower lowerme#0 (vetoes %v)", param.Vetoes)
	}

	allResult := analyzeRefLowering(pkg.Fset, all, pkg.Types, pkg.TypesInfo, censusLinknameHandles(all))
	verdict := allResult.Funcs["lowerme"]

	if verdict == nil {
		t.Fatal("lowerme missing from the all-files control")
	}

	// The control proves the test file WOULD veto — which is exactly why the pass must not see it.
	found := false

	for _, veto := range verdict.FuncVetoes {
		if veto == refFuncVetoFuncValue {
			found = true
		}
	}

	if !found {
		t.Error("control: the export_test.go alias must X5 the function when test files leak into the walk")
	}

	// And the driver-facing entry filters by filename even when handed the merged set.
	entries := make([]FileEntry, 0, len(all))

	for _, file := range all {
		entries = append(entries, FileEntry{file: file, filePath: pkg.Fset.Position(file.Pos()).Filename})
	}

	savedResult := packageRefLoweringResult
	defer func() { packageRefLoweringResult = savedResult }()

	performRefLoweringAnalysis(entries, pkg.Types, pkg.TypesInfo, Options{})

	if packageRefLoweringResult == nil {
		t.Fatal("performRefLoweringAnalysis recorded no result")
	}

	if param := paramOf(t, packageRefLoweringResult, "lowerme", 0); !param.LoweredA {
		t.Error("performRefLoweringAnalysis must classify from production files only (structural _test.go filter)")
	}
}

func TestRefLoweringBodilessAndLinknameVetoes(t *testing.T) {
	if testing.Short() {
		t.Skip("integration test: loads the fixture package via go/packages")
	}

	// Bodiless declarations type-check with an error ("missing function body"), which the census
	// driver skips packages over — but the classifier itself must still veto them (the -tests and
	// hand-owned drivers can meet them through assembly stubs under purego-less tag sets). Load
	// tolerating the error.
	dir := t.TempDir()
	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example.com/stubs\n\ngo 1.23\n",
		"stubs.go": `package stubs

import _ "unsafe"

// The assembly-stub shape: vacuously D-clean, must veto X5-bodiless.
func asmStub(p *int)

//go:linkname handled
var handled int

// A one-arg linkname HANDLE on a function: the registry exposure (X5-linkname).
//go:linkname exposed
func exposed(p *uint64) { *p = 1 }
`,
	})

	loaded, err := packages.Load(&packages.Config{Mode: packages.LoadAllSyntax, Dir: dir}, ".")

	if err != nil || len(loaded) != 1 {
		t.Fatalf("fixture load: %v (%d packages)", err, len(loaded))
	}

	pkg := loaded[0]
	result := analyzeRefLowering(pkg.Fset, pkg.Syntax, pkg.Types, pkg.TypesInfo, censusLinknameHandles(pkg.Syntax))

	assertFuncVeto := func(name, tag string) {
		t.Helper()

		verdict := result.Funcs[name]

		if verdict == nil {
			t.Fatalf("%s not classified", name)
		}

		for _, veto := range verdict.FuncVetoes {
			if veto == tag {
				return
			}
		}

		t.Errorf("%s must carry %s (recorded %v)", name, tag, verdict.FuncVetoes)
	}

	assertFuncVeto("asmStub", refFuncVetoBodiless)
	assertFuncVeto("exposed", refFuncVetoLinkname)
}
