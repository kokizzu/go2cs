// refLoweringAnalysisOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the ж-box reduction arc's REF-LOWERING CLASSIFICATION pass — stage A1 of
// docs/phase4/DESIGN-zh-box-reduction.md (design SIGNED OFF 2026-08-10; §10's six rulings are
// ratified and binding).
//
// What it decides. A pointer parameter `p *T` of a package-level function is REF-LOWERABLE when
// every use of `p` in the body is a dereference (D1), a derived field/element address that itself
// feeds a lowered position (D1′), or a forward of `p` into another lowered position (D2) — and no
// use is pointer identity (X1), an escape (X2), a representation observation (X3), a re-point
// (X4), or a function-identity escape (X5: exported [Phase A], func-value use, linkname registry
// membership, named-pointer-type parameter, absent body). Soundness is WHITELIST-shaped: any use
// the classifier does not positively recognize disqualifies (§3.2) — that is the load-bearing
// statement, so the default arm of every switch below vetoes.
//
// The fixed point is TWO-SIDED (§3.2): the D2/D1′ iteration starts from "all candidates
// lowerable" and strips until stable, and it also strips on caller-side emittability — a call
// site whose argument shape has no `ref` emission row in §3.3 un-lowers that parameter position
// rather than dead-ending emission later. `defer`/`go` call sites are BOXED sites, categorically
// (§3.3): they do not un-lower the callee, but an address-carrying use of a candidate's OWN
// parameter under a defer/go argument does veto that parameter (the thunk frame stores a box the
// lowered parameter no longer has — the parameter-side mirror of §3.3's locals carve-out (a)).
//
// What stage A1 does with the verdicts: NOTHING, emission-wise. The pass runs in all three
// conversion drivers (normal, -tests, hand-owned-sibling — the charter's three-driver rule) and
// records its result on the package context (packageRefLoweringResult); no emission visitor reads
// it yet. The consumers today are the `-debug` per-package census block and the corpus-wide
// `-ref-census` instrument (refLoweringCensus.go), whose report is the A1 bank.
//
// Determinism across emissions (§3.5, the `-tests`-closure invariant): classification reads ONLY
// the production package's own files — never `_test.go`. The filter is applied HERE, structurally,
// not left to the drivers, so the merged white-box package a `-tests` conversion type-checks (whose
// export_test.go may hold `var X = unexportedFn` — a func-value alias that would X5 what -stdlib
// lowered) can never desynchronize the two emissions' classifications.

package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"path/filepath"
	"sort"
	"strings"
)

// Veto reasons — callee-side (§3.2). Every veto carries one of these tags so the census can
// aggregate honestly; the whitelist default arm uses refVetoOtherUse.
const (
	refVetoX1Identity    = "X1-identity"        // p == nil / p == q / switch p / case p / map key
	refVetoX2Escape      = "X2-escape"          // stored, composite lit, channel send
	refVetoX2Return      = "X2-return"          // p returned — the constructor/self-return shape (§9 item f's bucket)
	refVetoX2Capture     = "X2-capture"         // referenced inside a nested closure body
	refVetoX2DeferArg    = "X2-defer-arg"       // address-carrying use in a defer/go argument frame
	refVetoX3Repr        = "X3-representation"  // unsafe.Pointer/uintptr conversion, interface arg, method on p
	refVetoX4Repoint     = "X4-repoint"         // p = q assignment to the parameter itself
	refVetoX5NamedPtr    = "X5-named-pointer"   // parameter type is a named pointer type (type P *T)
	refVetoOtherUse      = "other-use"          // whitelist default: unrecognized use shape
	refVetoPtrSlice      = "other-use-ptr-slice" // p[:] over a pointer-to-array parameter — not a §3.2 D row; distinct tag so the class is measurable
	refVetoForward       = "forward-unlowered"  // D2/D1' target position is not (or no longer) lowered
	refVetoCallerShape   = "caller-shape"       // stripped by an other-veto argument shape at a call site
)

// A variadic `...*T` parameter needs no veto arm: its type is a SLICE of pointers, so the
// pointer-param candidate filter never admits it. What variadic signatures do need is the
// call-site position guard — an argument AT the variadic position lands in the arg slice
// (an X2 store for the forwarded pointer), handled where positions are mapped.

// Function-level exclusions (X5 — the function's identity escapes). Exported-ness is deliberately
// NOT in this list: Phase A's scope ruling (§10.1) keeps exported functions un-lowered, but the A1
// census classifies them anyway under the exported-candidate flag (§9 item f), so exported-ness is
// a scope predicate applied at fixed-point time, never an eligibility veto.
const (
	refFuncVetoBodiless  = "X5-bodiless"   // Body == nil: the assembly/cgo partial-stub shape
	refFuncVetoLinkname  = "X5-linkname"   // appears in a //go:linkname registry (handle/forward/push)
	refFuncVetoFuncValue = "X5-func-value" // used as a func value somewhere in the package
)

// Call-site argument shapes — the §3.3 emission rows plus the defer/go carve-out and the veto
// bucket. Shape names are stable strings because they land in the census JSON.
const (
	refShapeFieldAddr  = "row1-field-addr"  // &e.x — field of a deref'd base
	refShapeLocalAddr  = "row2-local-addr"  // &x — address-taken local/param/result (globals sub-tagged)
	refShapePointerVar = "row3-pointer-var" // a pointer-valued variable read (extended carriers sub-tagged)
	refShapeElemAddr   = "row4-elem-addr"   // &s[i]
	refShapePtrConv    = "row5-pointer-conv" // (*T2)(&v.x) — the conversion shape (§10.3: hoisted-temp rule)
	refShapeTempNeeded = "row6-temp"        // &T{…}, new(T), call results — hoisted temp
	refShapeNilLit     = "row7-nil"         // the literal nil
	refShapeDeferGo    = "defer-go"         // boxed carve-out: the site keeps today's boxed emission
	refShapeOtherVeto  = "other-veto"       // no emission row — strips the position (two-sided fixed point)
)

// refPosKey names one pointer-parameter position of one package-level function — the unit the
// fixed point resolves and the census aggregates over.
type refPosKey struct {
	PkgPath string `json:"pkg"`
	Func    string `json:"func"`
	Index   int    `json:"index"` // position among ALL parameters (signature order)
}

func (k refPosKey) String() string {
	return fmt.Sprintf("%s.%s#%d", k.PkgPath, k.Func, k.Index)
}

// refForward records a D2/D1′ use of a candidate parameter whose legality depends on the TARGET
// position still being lowered when the fixed point stabilizes.
type refForward struct {
	Target refPosKey `json:"target"`
	Kind   string    `json:"kind"` // "D2" or "D1'"
}

// refParamVerdict is the per-parameter classification record.
type refParamVerdict struct {
	Index    int      `json:"index"`
	Name     string   `json:"name"`
	Vetoes   []string `json:"vetoes,omitempty"`   // immediate callee-side vetoes (empty = candidate)
	Forwards []refForward `json:"forwards,omitempty"` // D2/D1' dependencies resolved by the fixed point
	// Final verdicts, filled from the fixed-point sets: LoweredA is the Phase-A (unexported-only)
	// verdict — the one A2's emission will read; LoweredAPrime is the A′-world verdict the census
	// computes globally (exported candidates included). StrippedBy names the fixed-point strip
	// reason when a parameter with no immediate veto still fails.
	LoweredA      bool   `json:"loweredA"`
	LoweredAPrime bool   `json:"loweredAPrime,omitempty"`
	StrippedBy    string `json:"strippedBy,omitempty"`
}

// refFuncVerdict is the per-function classification record for a package-level function.
type refFuncVerdict struct {
	PkgPath    string             `json:"pkg"`
	Name       string             `json:"name"`
	Exported   bool               `json:"exported"`
	FuncVetoes []string           `json:"funcVetoes,omitempty"` // X5 function-level exclusions
	Params     []*refParamVerdict `json:"params,omitempty"`     // pointer parameters only
	// ExportedCandidate marks an exported function with ≥1 parameter lowered in the A′-world fixed
	// point — the §10.1 decision input for the A′ checkpoint (§9 item f).
	ExportedCandidate bool `json:"exportedCandidate,omitempty"`
}

// refCallArg records one argument at a candidate position — the §3.3 shape census unit.
type refCallArg struct {
	Target refPosKey `json:"target"`
	Shape  string    `json:"shape"`
	Detail string    `json:"detail,omitempty"` // sub-shape: row2-global, row3-selector, row6-composite, …
	File   string    `json:"file,omitempty"`
	Line   int       `json:"line,omitempty"`
}

// refLocalVerdict records one address-taken local (or value parameter / named result — anything
// that takes an entry heap box today) and whether it reverts to a plain local under §3.3's
// reversion predicate: every address-connected use feeds a Phase-A-lowered position outside
// defer/go, with no other box-forcing use surviving.
type refLocalVerdict struct {
	Func        string   `json:"func"`
	Name        string   `json:"name"`
	Origin      string   `json:"origin"` // local | param | result
	Lowers      bool     `json:"lowers"`
	KeptReasons []string `json:"keptReasons,omitempty"`
}

// refLoweringPackageResult is the pass's package-scope product — the "package context the emission
// visitors read" (§3.5). At A1 nothing emission-side reads it; the census does.
type refLoweringPackageResult struct {
	PkgPath string `json:"pkg"`
	// Funcs is keyed by function name (package-level functions are unique per package).
	Funcs map[string]*refFuncVerdict `json:"funcs"`
	// CallArgs holds every classified argument at a candidate position, including cross-package
	// targets (calls into an imported package's exported functions — the A′ traffic census).
	CallArgs []refCallArg `json:"callArgs,omitempty"`
	// Locals is the address-taken-local reversion census (Phase-A scope).
	Locals []refLocalVerdict `json:"locals,omitempty"`
	// MethodPtrParams counts pointer-parameter positions on METHODS (receivers stay ж in Phase A;
	// the count prices B′'s constituency, nothing more).
	MethodPtrParams int `json:"methodPtrParams,omitempty"`
	// TypeParamPointerCoreParams counts parameters typed by a type parameter whose core type is a
	// pointer (`p P` under `[P *T]`) — not classified in Phase A, counted for honesty.
	TypeParamPointerCoreParams int `json:"typeParamPointerCoreParams,omitempty"`
}

// packageRefLoweringResult is the package-scope registry the conversion drivers record into —
// reset per package by resetPackageState alongside every other package-scoped global. Stage A1:
// analysis-only; no emission visitor reads it.
var packageRefLoweringResult *refLoweringPackageResult

// performRefLoweringAnalysis is the driver-facing entry: classify the package's production files
// and record the result on the package context. Wired into all three conversion drivers (normal,
// -tests, hand-owned-sibling). The `_test.go` filter is applied here, structurally — see the file
// header's determinism note. Linkname handles are scanned purely from the same production files
// (censusLinknameHandles) rather than read from the collectLinknameHandles global, so the pass is
// ordering-independent across the drivers (the hand-owned-sibling branch never populates the
// global) and can never perturb conversion state of its own.
func performRefLoweringAnalysis(files []FileEntry, pkg *types.Package, info *types.Info, options Options) {
	syntax := make([]*ast.File, 0, len(files))

	for _, entry := range files {
		if strings.HasSuffix(strings.ToLower(entry.filePath), "_test.go") {
			continue
		}

		syntax = append(syntax, entry.file)
	}

	packageRefLoweringResult = analyzeRefLowering(nil, syntax, pkg, info, censusLinknameHandles(syntax))

	if options.debugMode {
		printRefLoweringDebugCensus(packageRefLoweringResult)
	}
}

// analyzeRefLowering runs the whole classification over one package's production syntax: collect
// candidates, classify every parameter use (D/X), classify every call-site argument shape at a
// candidate position, resolve the Phase-A fixed point, and run the address-taken-local census
// against the resolved lowered set. handles is the package's one-arg //go:linkname handle set
// (linknameHandles in the drivers; a local scan in the census driver). fset is optional — with
// one, call-arg records carry file:line detail (the census driver passes it).
func analyzeRefLowering(fset *token.FileSet, syntax []*ast.File, pkg *types.Package, info *types.Info, handles HashSet[string]) *refLoweringPackageResult {
	analysis := &refLoweringAnalysis{
		pkg:     pkg,
		info:    info,
		handles: handles,
		fset:    fset,
		result: &refLoweringPackageResult{
			PkgPath: pkg.Path(),
			Funcs:   map[string]*refFuncVerdict{},
		},
		paramOwner: map[*types.Var]refPosKey{},
		funcDecls:  map[*types.Func]*ast.FuncDecl{},
	}

	// Pass 1: collect package-level function candidates (and the X5 function-level exclusions that
	// are visible from the declaration alone).
	for _, file := range syntax {
		for _, decl := range file.Decls {
			funcDecl, ok := decl.(*ast.FuncDecl)

			if !ok {
				continue
			}

			analysis.collectFunc(funcDecl)
		}
	}

	// Pass 2: the func-value scan (X5) — any reference to a candidate outside call position
	// disqualifies the whole function (its boxed signature must survive for the func value).
	for _, file := range syntax {
		analysis.scanFuncValues(file)
	}

	// Pass 3: per-use classification of candidate parameters + call-site argument shapes. Both
	// need parent chains, so each file builds one parent map and shares it.
	for _, file := range syntax {
		parents := buildParentMap(file)
		analysis.classifyFile(file, parents)
	}

	// Phase-A fixed point: unexported candidates only (§10.1). The A′-world resolution is run by
	// the corpus census over the union of packages; per-package the A verdict is definitive
	// because an unexported function's call sites are all within its package.
	lowered, stripped := resolveRefLoweringFixedPoint(analysis.result.Funcs, analysis.result.CallArgs, false)
	applyRefLoweringVerdicts(analysis.result.Funcs, lowered, stripped, false)

	// Pass 4: the address-taken-local reversion census, against the resolved A-scope lowered set.
	for _, file := range syntax {
		parents := buildParentMap(file)
		analysis.censusLocals(file, parents, lowered)
	}

	return analysis.result
}

// refLoweringAnalysis is the pass's working state for one package.
type refLoweringAnalysis struct {
	pkg     *types.Package
	info    *types.Info
	handles HashSet[string]
	result  *refLoweringPackageResult

	// fset is optional — the census driver provides it for line-accurate shape records; the
	// wired-in driver pass leaves it nil (the -debug census aggregates by shape, not by line).
	fset *token.FileSet

	// paramOwner maps a candidate parameter's types.Var to its position key, so a body walk can
	// recognize "this ident is candidate parameter p of function f" in one lookup.
	paramOwner map[*types.Var]refPosKey

	// funcDecls maps a package-level function object to its declaration (bodies walked in pass 3).
	funcDecls map[*types.Func]*ast.FuncDecl
}

// collectFunc records one FuncDecl: package-level functions become candidate records (with X5
// function-level vetoes applied); methods contribute only the B′-context pointer-param count.
func (a *refLoweringAnalysis) collectFunc(funcDecl *ast.FuncDecl) {
	obj, ok := a.info.Defs[funcDecl.Name].(*types.Func)

	if !ok {
		return
	}

	signature, ok := obj.Type().(*types.Signature)

	if !ok {
		return
	}

	if signature.Recv() != nil {
		// Methods do not lower in Phase A (receivers stay ж — §10.1); count their pointer-param
		// positions for the B′ pricing context only.
		params := signature.Params()

		for i := 0; i < params.Len(); i++ {
			if _, isPtr := types.Unalias(params.At(i).Type()).(*types.Pointer); isPtr {
				a.result.MethodPtrParams++
			}
		}

		return
	}

	// init functions never lower (they are emitted as registered init hooks, not callable
	// signatures) — and they have no callers, so candidacy would be vacuous anyway.
	if funcDecl.Name.Name == "init" {
		return
	}

	verdict := &refFuncVerdict{
		PkgPath:  a.pkg.Path(),
		Name:     obj.Name(),
		Exported: obj.Exported(),
	}

	if funcDecl.Body == nil {
		// The assembly/cgo partial-stub shape: an empty use set satisfies D vacuously, and
		// lowering the emitted partial declaration would orphan its hand-written *_impl.cs
		// companion (§3.2 X5, panel C-F5).
		verdict.FuncVetoes = append(verdict.FuncVetoes, refFuncVetoBodiless)
	}

	if a.isLinknameExposed(obj.Name()) {
		// A linkname pull publicizes an unexported function across the assembly boundary in the
		// boxed convention, invisibly to a per-package scan (§3.2 X5, panel C-F4).
		verdict.FuncVetoes = append(verdict.FuncVetoes, refFuncVetoLinkname)
	}

	params := signature.Params()

	for i := 0; i < params.Len(); i++ {
		param := params.At(i)
		paramType := types.Unalias(param.Type())

		if _, isTypeParam := paramType.(*types.TypeParam); isTypeParam {
			// A `p P` under `[P *T]` constraint renders as ж<T> (pointerCoreConstraint) but is not
			// a *types.Pointer; Phase A does not classify it. Counted for honesty.
			if _, isCorePtr := paramType.Underlying().(*types.Pointer); isCorePtr {
				a.result.TypeParamPointerCoreParams++
			}

			continue
		}

		// Admit plain pointers as candidates — and NAMED pointer types (`type P *T`) as recorded
		// X5 vetoes: their generated wrapper's operator surface must survive (§3.2 X5), but the
		// census still counts the position. types.Alias was stripped above, so an alias to *T
		// classifies as the plain pointer it transparently is.
		namedPointer := false

		switch pt := paramType.(type) {
		case *types.Pointer:
		case *types.Named:
			if _, isPtr := pt.Underlying().(*types.Pointer); !isPtr {
				continue
			}

			namedPointer = true
		default:
			continue
		}

		paramVerdict := &refParamVerdict{Index: i, Name: param.Name()}

		if namedPointer {
			paramVerdict.Vetoes = append(paramVerdict.Vetoes, refVetoX5NamedPtr)
		}

		verdict.Params = append(verdict.Params, paramVerdict)
		a.paramOwner[param] = refPosKey{PkgPath: a.pkg.Path(), Func: obj.Name(), Index: i}
	}

	a.result.Funcs[obj.Name()] = verdict
	a.funcDecls[obj] = funcDecl
}

// isLinknameExposed reports whether name participates in any //go:linkname registry: the package's
// own one-arg handles, the curated forward-target list, or either side of the curated push
// registry (§3.2 X5's linkname strip rule).
func (a *refLoweringAnalysis) isLinknameExposed(name string) bool {
	if a.handles.Contains(name) {
		return true
	}

	qualified := a.pkg.Path() + "." + name

	if linknameForwardTargets[qualified] || linknamePushSources[qualified] {
		return true
	}

	if _, isPushTarget := linknamePushTargets[qualified]; isPushTarget {
		return true
	}

	return false
}

// scanFuncValues walks one file for references to candidate functions OUTSIDE call position — the
// X5 func-value use that keeps the boxed signature alive. A reference is a call use only when the
// ident is the call's Fun (through parens and a generic instantiation index).
func (a *refLoweringAnalysis) scanFuncValues(file *ast.File) {
	parents := buildParentMap(file)

	ast.Inspect(file, func(n ast.Node) bool {
		ident, ok := n.(*ast.Ident)

		if !ok {
			return true
		}

		obj, ok := a.info.Uses[ident].(*types.Func)

		if !ok {
			return true
		}

		// Identity through funcDecls, not by name: a METHOD sharing a candidate's name resolves to
		// a different *types.Func and must not veto the package-level function.
		if _, tracked := a.funcDecls[obj]; !tracked {
			return true
		}

		verdict, recorded := a.result.Funcs[obj.Name()]

		if !recorded {
			return true
		}

		if !identIsCallFun(ident, parents) {
			verdict.FuncVetoes = appendUnique(verdict.FuncVetoes, refFuncVetoFuncValue)
		}

		return true
	})
}

// identIsCallFun reports whether ident is the callee expression of a CallExpr — directly, through
// parens, or through a generic instantiation (`f[T](…)`).
func identIsCallFun(ident *ast.Ident, parents map[ast.Node]ast.Node) bool {
	var node ast.Node = ident

	for {
		parent := parents[node]

		switch p := parent.(type) {
		case *ast.ParenExpr:
			node = p
		case *ast.IndexExpr:
			// Generic instantiation in Fun position only (f[T](…)); an ident under Index is a use.
			if p.X != node {
				return false
			}

			node = p
		case *ast.IndexListExpr:
			if p.X != node {
				return false
			}

			node = p
		case *ast.CallExpr:
			return p.Fun == node
		default:
			return false
		}
	}
}

// classifyFile runs pass 3 over one file: candidate-parameter use classification (for candidates
// declared in this file) and call-site argument shapes (for calls appearing in this file into ANY
// candidate position, own-package or imported).
func (a *refLoweringAnalysis) classifyFile(file *ast.File, parents map[ast.Node]ast.Node) {
	// Parameter-use classification: walk each candidate function's body once.
	for _, decl := range file.Decls {
		funcDecl, ok := decl.(*ast.FuncDecl)

		if !ok || funcDecl.Body == nil {
			continue
		}

		obj, ok := a.info.Defs[funcDecl.Name].(*types.Func)

		if !ok {
			continue
		}

		if _, tracked := a.funcDecls[obj]; !tracked {
			continue
		}

		a.classifyParamUses(funcDecl, parents)
	}

	// Call-site argument shapes: every call in the file (any body, methods and literals included)
	// whose callee is a candidate position — own-package by ident, imported by package selector.
	ast.Inspect(file, func(n ast.Node) bool {
		call, ok := n.(*ast.CallExpr)

		if !ok {
			return true
		}

		callee := a.resolveCandidateCallee(call)

		if callee == nil {
			return true
		}

		signature, ok := callee.Type().(*types.Signature)

		if !ok {
			return true
		}

		deferGo := callIsDeferOrGo(call, parents)
		params := signature.Params()

		for argIndex, arg := range call.Args {
			paramIndex := argIndex

			if signature.Variadic() && paramIndex >= params.Len()-1 {
				// Variadic positions are refVetoVariadic candidates already; skip the shape rows.
				continue
			}

			if paramIndex >= params.Len() {
				continue
			}

			if _, isPtr := types.Unalias(params.At(paramIndex).Type()).(*types.Pointer); !isPtr {
				continue
			}

			target := refPosKey{PkgPath: callee.Pkg().Path(), Func: callee.Name(), Index: paramIndex}
			shape, detail := a.classifyArgShape(arg, deferGo)
			position := a.positionOf(arg)

			a.result.CallArgs = append(a.result.CallArgs, refCallArg{
				Target: target,
				Shape:  shape,
				Detail: detail,
				File:   position.Filename,
				Line:   position.Line,
			})
		}

		return true
	})
}

// resolveCandidateCallee resolves a call's callee to a PACKAGE-LEVEL function object — own-package
// (plain ident, possibly generic-instantiated) or imported (package-qualified selector). Methods,
// builtins, conversions, and func-typed values all return nil: none of their positions can lower.
func (a *refLoweringAnalysis) resolveCandidateCallee(call *ast.CallExpr) *types.Func {
	fun := ast.Unparen(call.Fun)

	// Strip a generic instantiation: f[T](…) / f[T1, T2](…).
	switch indexed := fun.(type) {
	case *ast.IndexExpr:
		fun = ast.Unparen(indexed.X)
	case *ast.IndexListExpr:
		fun = ast.Unparen(indexed.X)
	}

	var obj types.Object

	switch fn := fun.(type) {
	case *ast.Ident:
		obj = a.info.Uses[fn]
	case *ast.SelectorExpr:
		// Package-qualified only: the X must name a package, not a value (a value selector is a
		// method call — receivers stay ж in Phase A).
		base, ok := ast.Unparen(fn.X).(*ast.Ident)

		if !ok {
			return nil
		}

		if _, isPkg := a.info.Uses[base].(*types.PkgName); !isPkg {
			return nil
		}

		obj = a.info.Uses[fn.Sel]
	default:
		return nil
	}

	funcObj, ok := obj.(*types.Func)

	if !ok || funcObj.Pkg() == nil {
		return nil
	}

	signature, ok := funcObj.Type().(*types.Signature)

	if !ok || signature.Recv() != nil {
		return nil
	}

	return funcObj
}

// callIsDeferOrGo reports whether call is the immediate call of a DeferStmt or GoStmt — the boxed
// carve-out site class (§3.3). A call nested INSIDE a deferred call's argument list evaluates
// eagerly at defer time and is an ordinary site.
func callIsDeferOrGo(call *ast.CallExpr, parents map[ast.Node]ast.Node) bool {
	switch parent := parents[call].(type) {
	case *ast.DeferStmt:
		return parent.Call == call
	case *ast.GoStmt:
		return parent.Call == call
	default:
		return false
	}
}

// classifyArgShape classifies one argument expression at a lowered-candidate position into the
// §3.3 emission rows. deferGo wins over every shape row: the site keeps today's boxed emission.
func (a *refLoweringAnalysis) classifyArgShape(arg ast.Expr, deferGo bool) (shape, detail string) {
	if deferGo {
		return refShapeDeferGo, ""
	}

	expr := ast.Unparen(arg)

	switch e := expr.(type) {
	case *ast.Ident:
		if _, isNil := a.info.Uses[e].(*types.Nil); isNil || e.Name == "nil" {
			return refShapeNilLit, ""
		}

		if obj, ok := a.info.Uses[e].(*types.Var); ok {
			if obj.Parent() != nil && obj.Parent().Parent() == types.Universe {
				// Package-scope pointer var: carries its box exactly like a local pointer var.
				return refShapePointerVar, "global"
			}
		}

		return refShapePointerVar, ""

	case *ast.UnaryExpr:
		if e.Op != token.AND {
			if e.Op == token.ARROW {
				// A pointer received from a channel carries a box and is evaluated once — the
				// hoisted-temp mechanism covers it (extended carrier, priced in detail).
				return refShapeTempNeeded, "chan-recv"
			}

			return refShapeOtherVeto, fmt.Sprintf("unary-%s", e.Op)
		}

		operand := ast.Unparen(e.X)

		switch inner := operand.(type) {
		case *ast.SelectorExpr:
			return refShapeFieldAddr, ""
		case *ast.IndexExpr:
			return refShapeElemAddr, ""
		case *ast.CompositeLit:
			return refShapeTempNeeded, "composite-lit"
		case *ast.Ident:
			if obj, ok := a.info.Uses[inner].(*types.Var); ok {
				if obj.Parent() != nil && obj.Parent().Parent() == types.Universe {
					// &global — the addressed-global box exposes a ref property (§11 lens 3,
					// verified sound); folded under row 2 with the split visible in detail.
					return refShapeLocalAddr, "global"
				}
			}

			return refShapeLocalAddr, ""
		case *ast.StarExpr:
			// &*p is p — a pointer carrier.
			return refShapePointerVar, "re-addressed-deref"
		default:
			return refShapeOtherVeto, fmt.Sprintf("addr-of-%T", operand)
		}

	case *ast.CallExpr:
		if a.exprIsTypeConversion(e) {
			if _, isPtr := types.Unalias(a.info.TypeOf(e)).(*types.Pointer); isPtr {
				return refShapePtrConv, a.ptrConvDetail(e)
			}

			return refShapeOtherVeto, "non-pointer-conversion"
		}

		if fn, ok := ast.Unparen(e.Fun).(*ast.Ident); ok {
			if _, isBuiltin := a.info.Uses[fn].(*types.Builtin); isBuiltin {
				if fn.Name == "new" {
					return refShapeTempNeeded, "new"
				}

				return refShapeTempNeeded, "builtin-" + fn.Name
			}
		}

		return refShapeTempNeeded, "call-result"

	case *ast.SelectorExpr:
		// A pointer-valued field read carries the box the field holds.
		return refShapePointerVar, "selector"

	case *ast.IndexExpr:
		// A pointer-valued element read (slice/array/map element) carries a box.
		return refShapePointerVar, "index"

	case *ast.StarExpr:
		// *pp of a **T — a deref READ producing a pointer value (carries the inner box).
		return refShapePointerVar, "deref"

	case *ast.TypeAssertExpr:
		// x.(*T) evaluates once to the asserted box.
		return refShapePointerVar, "type-assert"

	case *ast.FuncLit:
		return refShapeOtherVeto, "func-lit"

	default:
		return refShapeOtherVeto, fmt.Sprintf("%T", expr)
	}
}

// exprIsTypeConversion reports whether call is a type conversion rather than a function call.
func (a *refLoweringAnalysis) exprIsTypeConversion(call *ast.CallExpr) bool {
	if tv, ok := a.info.Types[call.Fun]; ok {
		return tv.IsType()
	}

	return false
}

// ptrConvDetail sub-classifies a row-5 pointer conversion: the ruled hoisted-temp mechanism was
// priced against `(*T2)(&v.x)`-shaped operands (an address expression); a pointer-VALUE operand is
// mechanically different and is surfaced separately so A2 prices it, not discovers it.
func (a *refLoweringAnalysis) ptrConvDetail(call *ast.CallExpr) string {
	if len(call.Args) != 1 {
		return "conv-malformed"
	}

	operand := ast.Unparen(call.Args[0])

	if unary, ok := operand.(*ast.UnaryExpr); ok && unary.Op == token.AND {
		return "conv-of-address"
	}

	if inner, ok := operand.(*ast.CallExpr); ok && a.exprIsTypeConversion(inner) {
		// (*T)(unsafe.Pointer(p)) and friends — a conversion chain.
		return "conv-chain"
	}

	return "conv-of-value"
}

// classifyParamUses walks one candidate function's body and classifies every use of its candidate
// pointer parameters per the D/X whitelist.
func (a *refLoweringAnalysis) classifyParamUses(funcDecl *ast.FuncDecl, parents map[ast.Node]ast.Node) {
	ast.Inspect(funcDecl.Body, func(n ast.Node) bool {
		ident, ok := n.(*ast.Ident)

		if !ok {
			return true
		}

		obj, ok := a.info.Uses[ident].(*types.Var)

		if !ok {
			return true
		}

		key, isCandidate := a.paramOwner[obj]

		if !isCandidate || key.Func != funcDecl.Name.Name {
			// A use of ANOTHER function's parameter cannot occur (Go scoping); the func-name check
			// is defensive symmetry, not a reachable branch.
			return true
		}

		verdict := a.result.Funcs[key.Func]

		if verdict == nil {
			return true
		}

		var param *refParamVerdict

		for _, p := range verdict.Params {
			if p.Index == key.Index {
				param = p
				break
			}
		}

		if param == nil {
			return true
		}

		// X2-capture first: any reference from inside a nested closure body disqualifies —
		// a managed ref cannot be captured (§3.2 X2), whatever the use inside the closure is.
		if crossesFuncLit(ident, funcDecl.Body, parents) {
			param.Vetoes = appendUnique(param.Vetoes, refVetoX2Capture)
			return true
		}

		veto, forward := a.classifyParamUse(ident, parents)

		if veto != "" {
			param.Vetoes = appendUnique(param.Vetoes, veto)
		}

		if forward != nil {
			param.Forwards = append(param.Forwards, *forward)
		}

		return true
	})
}

// crossesFuncLit reports whether a FuncLit sits between node and root on the parent chain.
func crossesFuncLit(node ast.Node, root ast.Node, parents map[ast.Node]ast.Node) bool {
	for current := parents[node]; current != nil && current != root; current = parents[current] {
		if _, isLit := current.(*ast.FuncLit); isLit {
			return true
		}
	}

	return false
}

// classifyParamUse classifies ONE use of a candidate pointer parameter. It returns a veto tag
// (empty when the use is whitelisted) and, for D2/D1′ uses, the forward dependency the fixed
// point must resolve. The structure mirrors §3.2: a two-state climb — state P (the node denotes
// the pointer itself) and state V (a deref has happened; the node denotes pointee storage).
func (a *refLoweringAnalysis) classifyParamUse(ident *ast.Ident, parents map[ast.Node]ast.Node) (string, *refForward) {
	var node ast.Node = ident

	// ---- State P: the node evaluates to the pointer p itself. ----
	for {
		parent := parents[node]

		switch p := parent.(type) {
		case *ast.ParenExpr:
			node = p
			continue

		case *ast.StarExpr:
			// *p — the deref. Whatever happens to the VALUE is value-level (D1) — except a
			// selector/index continuing the chain, which stays in state V below.
			return a.classifyPointeeUse(p, parents)

		case *ast.SelectorExpr:
			if p.X != node {
				return refVetoOtherUse, nil
			}

			// p.f — auto-deref field access, or a method on p.
			if sel, ok := a.info.Uses[p.Sel].(*types.Func); ok {
				_ = sel
				// A method call (or method value) ON p — receivers stay ж in Phase A (§3.2 X3).
				return refVetoX3Repr, nil
			}

			return a.classifyPointeeUse(p, parents)

		case *ast.IndexExpr:
			if p.X == node {
				// p[i] — pointer-to-array indexing (auto-deref).
				return a.classifyPointeeUse(p, parents)
			}

			// p used as an index/key: a map key is pointer identity (§3.2 X1).
			baseType := a.info.TypeOf(p.X)

			if baseType != nil {
				if _, isMap := baseType.Underlying().(*types.Map); isMap {
					return refVetoX1Identity, nil
				}
			}

			return refVetoOtherUse, nil

		case *ast.UnaryExpr:
			if p.Op == token.AND {
				// &p — address of the parameter variable itself (a **T source): beyond the
				// whitelist.
				return refVetoOtherUse, nil
			}

			return refVetoOtherUse, nil

		case *ast.CallExpr:
			if p.Fun == node {
				// p invoked — a pointer is not callable; unreachable, veto defensively.
				return refVetoOtherUse, nil
			}

			return a.classifyPointerArgUse(node, p, parents)

		case *ast.BinaryExpr:
			// Any binary use of a pointer is a comparison (Go defines no other operator).
			return refVetoX1Identity, nil

		case *ast.AssignStmt:
			for _, lhs := range p.Lhs {
				if ast.Unparen(lhs) == node {
					return refVetoX4Repoint, nil
				}
			}

			return refVetoX2Escape, nil

		case *ast.ValueSpec:
			// var q = p — stored.
			return refVetoX2Escape, nil

		case *ast.ReturnStmt:
			return refVetoX2Return, nil

		case *ast.CompositeLit:
			return refVetoX2Escape, nil

		case *ast.KeyValueExpr:
			return refVetoX2Escape, nil

		case *ast.SendStmt:
			return refVetoX2Escape, nil

		case *ast.SwitchStmt:
			if p.Tag == node {
				return refVetoX1Identity, nil
			}

			return refVetoOtherUse, nil

		case *ast.CaseClause:
			return refVetoX1Identity, nil

		case *ast.TypeAssertExpr, *ast.TypeSwitchStmt:
			return refVetoX3Repr, nil

		case *ast.SliceExpr:
			// p[:] over a pointer-to-array — Go auto-derefs and the slice aliases the pointee's
			// storage. NOT a §3.2 D row (the whitelist has no slicing entry), so it vetoes — under
			// its own tag, because the shape is real in the corpus (edwards25519's
			// copyFieldElement) and A2 may want to price a D-row for it.
			return refVetoPtrSlice, nil

		case *ast.RangeStmt:
			if p.X == node {
				// range p over a pointer-to-array — an auto-deref read (D1).
				return "", nil
			}

			// `for _, p = range …` re-points the parameter (X4).
			if p.Key == node || p.Value == node {
				return refVetoX4Repoint, nil
			}

			return refVetoOtherUse, nil

		default:
			return refVetoOtherUse, nil
		}
	}
}

// classifyPointerArgUse handles state P's call-argument case: p forwarded as an argument. D2 when
// the target is a candidate position outside defer/go; X3 when the parameter lands in an
// interface/unsafe/uintptr position; forward-unlowered veto when the callee can never lower.
func (a *refLoweringAnalysis) classifyPointerArgUse(node ast.Node, call *ast.CallExpr, parents map[ast.Node]ast.Node) (string, *refForward) {
	// A type conversion over p: unsafe.Pointer(p), uintptr(p), (*U)(p), Named(p) — representation
	// or a re-typed pointer; either way the box convention must survive (§3.2 X3).
	if a.exprIsTypeConversion(call) {
		return refVetoX3Repr, nil
	}

	argIndex := -1

	for i, arg := range call.Args {
		if ast.Unparen(arg) == node {
			argIndex = i
			break
		}
	}

	if argIndex < 0 {
		return refVetoOtherUse, nil
	}

	callee := a.resolveCandidateCallee(call)

	if callee == nil {
		// Builtins, methods, func values, interface dispatch: no lowerable position there. The
		// specific historical tags (append→X2, println→X3) all resolve to the same verdict; the
		// census carries them under one honest name.
		return refVetoForward, nil
	}

	signature := callee.Type().(*types.Signature)
	params := signature.Params()

	if signature.Variadic() && argIndex >= params.Len()-1 {
		// p lands in the variadic arg slice — a store (§3.2 X2).
		return refVetoX2Escape, nil
	}

	if argIndex >= params.Len() {
		return refVetoForward, nil
	}

	paramType := types.Unalias(params.At(argIndex).Type())

	if _, isPtr := paramType.(*types.Pointer); !isPtr {
		// p handed to an interface{}/any/named-interface parameter — an interface conversion
		// observes the box (§3.2 X3).
		if _, isIface := paramType.Underlying().(*types.Interface); isIface {
			return refVetoX3Repr, nil
		}

		return refVetoOtherUse, nil
	}

	if callIsDeferOrGo(call, parents) {
		// The defer/go frame stores boxes; a lowered parameter has none to store (§3.3's locals
		// carve-out (a), parameter-side mirror).
		return refVetoX2DeferArg, nil
	}

	return "", &refForward{
		Target: refPosKey{PkgPath: callee.Pkg().Path(), Func: callee.Name(), Index: argIndex},
		Kind:   "D2",
	}
}

// classifyPointeeUse is state V: chainRoot denotes pointee storage reached by deref (auto or
// explicit). Climb selectors/indexes; a re-derived address (&p.f / &p[i]) is D1′ — legal only fed
// directly (through parens and at most one pointer conversion) to a candidate position outside
// defer/go. Everything value-level is D1.
func (a *refLoweringAnalysis) classifyPointeeUse(chainRoot ast.Node, parents map[ast.Node]ast.Node) (string, *refForward) {
	node := chainRoot

	for {
		parent := parents[node]

		switch p := parent.(type) {
		case *ast.ParenExpr:
			node = p
			continue

		case *ast.SelectorExpr:
			if p.X != node {
				return refVetoOtherUse, nil
			}

			// Deeper field — or a method on the pointee chain.
			if method, ok := a.info.Uses[p.Sel].(*types.Func); ok {
				if methodHasPointerReceiver(method) && !exprTypeIsPointer(a.info, node) {
					// (&p.f).M() — an implicit re-address of a VALUE field into a ж receiver;
					// the box convention must survive (§3.2 X3's receiver arm). When the chain
					// value is ALREADY a pointer (p.f of type *U), the call carries the field's
					// own box and takes no new address — value-level D1 for p.
					return refVetoX3Repr, nil
				}

				// A value-receiver method call copies the pointee — value-level, D1.
				return "", nil
			}

			node = p
			continue

		case *ast.IndexExpr:
			if p.X == node {
				node = p
				continue
			}

			// The pointee value used as an index — a plain value read (D1).
			return "", nil

		case *ast.UnaryExpr:
			if p.Op != token.AND {
				// A value-level unary (-, ^, !) over the pointee — D1.
				return "", nil
			}

			// &p.f / &p[i] — the derived address (D1′).
			return a.classifyDerivedAddress(p, parents)

		case *ast.SliceExpr:
			// p.f[:] — slices the pointee's array backing; the resulting header aliases heap
			// backing in both emissions (array<T> keeps a shared T[]), value-level for the
			// parameter's own classification.
			return "", nil

		case *ast.StarExpr:
			// *(p.f) — deref of a pointer-typed FIELD: the field's value (a box) is read (D1) and
			// the deref happens on the loaded value, not on p.
			return "", nil

		default:
			// Reads, writes, argument copies, returns of the VALUE, comparisons of the value,
			// range over the value, conversions of the value — all value-level D1: the pointee is
			// observed, never p's identity.
			return "", nil
		}
	}
}

// exprTypeIsPointer reports whether node's static type is (or underlies to) a pointer — the
// operand test that separates an implicit auto-& receiver call from a call through a pointer the
// expression already carries.
func exprTypeIsPointer(info *types.Info, node ast.Node) bool {
	expr, ok := node.(ast.Expr)

	if !ok {
		return false
	}

	exprType := info.TypeOf(expr)

	if exprType == nil {
		return false
	}

	_, isPtr := exprType.Underlying().(*types.Pointer)

	return isPtr
}

// methodHasPointerReceiver reports whether method's receiver is a pointer type.
func methodHasPointerReceiver(method *types.Func) bool {
	signature, ok := method.Type().(*types.Signature)

	if !ok || signature.Recv() == nil {
		return false
	}

	_, isPtr := types.Unalias(signature.Recv().Type()).(*types.Pointer)

	return isPtr
}

// classifyDerivedAddress resolves a D1′ derived address (&p.f, &p[i]): legal only when fed —
// through parens and at most one pointer conversion — directly as an argument at a candidate
// position outside defer/go. Any other disposition (stored, returned, compared) re-creates the
// field-ref box requirement and vetoes.
func (a *refLoweringAnalysis) classifyDerivedAddress(addrExpr ast.Expr, parents map[ast.Node]ast.Node) (string, *refForward) {
	node := ast.Node(addrExpr)
	conversionSeen := false

	for {
		parent := parents[node]

		switch p := parent.(type) {
		case *ast.ParenExpr:
			node = p
			continue

		case *ast.CallExpr:
			if a.exprIsTypeConversion(p) {
				if conversionSeen {
					return refVetoOtherUse, nil
				}

				if _, isPtr := types.Unalias(a.info.TypeOf(p)).(*types.Pointer); !isPtr {
					// unsafe.Pointer(&p.f) / uintptr(&p.f) — representation (§3.2 X3).
					return refVetoX3Repr, nil
				}

				conversionSeen = true
				node = p
				continue
			}

			// An argument of a real call: resolve the position exactly as D2 does.
			argIndex := -1

			for i, arg := range p.Args {
				if ast.Unparen(arg) == node {
					argIndex = i
					break
				}
			}

			if argIndex < 0 {
				// The address participates in the call some other way (callee expression …).
				return refVetoOtherUse, nil
			}

			callee := a.resolveCandidateCallee(p)

			if callee == nil {
				return refVetoForward, nil
			}

			signature := callee.Type().(*types.Signature)
			params := signature.Params()

			if signature.Variadic() && argIndex >= params.Len()-1 {
				// The derived address lands in the variadic arg slice — a store (§3.2 X2).
				return refVetoX2Escape, nil
			}

			if argIndex >= params.Len() {
				return refVetoForward, nil
			}

			if _, isPtr := types.Unalias(params.At(argIndex).Type()).(*types.Pointer); !isPtr {
				return refVetoX3Repr, nil
			}

			if callIsDeferOrGo(p, parents) {
				return refVetoX2DeferArg, nil
			}

			return "", &refForward{
				Target: refPosKey{PkgPath: callee.Pkg().Path(), Func: callee.Name(), Index: argIndex},
				Kind:   "D1'",
			}

		default:
			// Stored, returned, compared, composite-lit'd — the derived address escapes the
			// argument position; the field-ref box must exist, so p keeps its box.
			return refVetoX2Escape, nil
		}
	}
}

// resolveRefLoweringFixedPoint runs the two-sided fixed point (§3.2) over a function universe:
// start from "every immediate-veto-free candidate parameter is lowered", strip positions whose
// call sites carry an other-veto argument shape (caller-side emittability), then iterate the
// D2/D1′ forward dependencies until stable. includeExported widens the candidate universe to the
// A′ world (§9 item f); Phase A's scope is unexported only (§10.1).
//
// Returns the lowered position set and, for candidates that fell, the strip reason.
func resolveRefLoweringFixedPoint(funcs map[string]*refFuncVerdict, callArgs []refCallArg, includeExported bool) (map[refPosKey]bool, map[refPosKey]string) {
	lowered := map[refPosKey]bool{}
	stripped := map[refPosKey]string{}
	forwardsByPos := map[refPosKey][]refForward{}

	for _, verdict := range funcs {
		if len(verdict.FuncVetoes) > 0 {
			continue
		}

		if verdict.Exported && !includeExported {
			continue
		}

		for _, param := range verdict.Params {
			if len(param.Vetoes) > 0 {
				continue
			}

			key := refPosKey{PkgPath: verdict.PkgPath, Func: verdict.Name, Index: param.Index}
			lowered[key] = true
			forwardsByPos[key] = param.Forwards
		}
	}

	// Caller-side emittability: an other-veto shape at any call site strips the position outright
	// (defer/go sites are BOXED sites, not vetoes — §3.3).
	for _, callArg := range callArgs {
		if callArg.Shape != refShapeOtherVeto {
			continue
		}

		if lowered[callArg.Target] {
			delete(lowered, callArg.Target)
			stripped[callArg.Target] = refVetoCallerShape + ":" + callArg.Detail
		}
	}

	// D2/D1′ iteration: strip any candidate one of whose forwards targets a non-lowered position.
	for changed := true; changed; {
		changed = false

		for key := range lowered {
			for _, forward := range forwardsByPos[key] {
				if !lowered[forward.Target] {
					delete(lowered, key)
					stripped[key] = refVetoForward + ":" + forward.Target.String()
					changed = true
					break
				}
			}
		}
	}

	return lowered, stripped
}

// applyRefLoweringVerdicts writes a fixed-point result back onto the per-parameter records —
// LoweredA for the Phase-A scope, LoweredAPrime (plus the ExportedCandidate flag) for the A′ run.
func applyRefLoweringVerdicts(funcs map[string]*refFuncVerdict, lowered map[refPosKey]bool, stripped map[refPosKey]string, aPrime bool) {
	for _, verdict := range funcs {
		exportedCandidate := false

		for _, param := range verdict.Params {
			key := refPosKey{PkgPath: verdict.PkgPath, Func: verdict.Name, Index: param.Index}

			if aPrime {
				param.LoweredAPrime = lowered[key]

				if verdict.Exported && lowered[key] {
					exportedCandidate = true
				}
			} else {
				param.LoweredA = lowered[key]
			}

			if reason, wasStripped := stripped[key]; wasStripped && param.StrippedBy == "" {
				param.StrippedBy = reason
			}
		}

		if aPrime && exportedCandidate {
			verdict.ExportedCandidate = true
		}
	}
}

// censusLocals runs the address-taken-local reversion census (§3.3) over one file against the
// resolved Phase-A lowered set: a local (or value parameter, or named result) whose EVERY
// address-connected use feeds a lowered position — directly, outside defer/go, outside any nested
// closure — reverts to a plain local (box + eager slot both disappear); any other address use
// keeps the box, with the reason recorded.
func (a *refLoweringAnalysis) censusLocals(file *ast.File, parents map[ast.Node]ast.Node, lowered map[refPosKey]bool) {
	for _, decl := range file.Decls {
		funcDecl, ok := decl.(*ast.FuncDecl)

		if !ok || funcDecl.Body == nil {
			continue
		}

		funcName := funcDecl.Name.Name

		if funcDecl.Recv != nil {
			funcName = "(method)." + funcName
		}

		a.censusFuncLocals(funcDecl, funcName, parents, lowered)
	}
}

// censusFuncLocals collects the function's box-candidate variables (body locals, value parameters,
// named results — every category that takes an entry heap box today) and classifies each one's
// address-connected uses.
func (a *refLoweringAnalysis) censusFuncLocals(funcDecl *ast.FuncDecl, funcName string, parents map[ast.Node]ast.Node, lowered map[refPosKey]bool) {
	origins := map[*types.Var]string{}

	// Value parameters (a pointer parameter is not a box candidate — it IS a box already).
	if funcDecl.Type.Params != nil {
		for _, field := range funcDecl.Type.Params.List {
			for _, name := range field.Names {
				if obj, ok := a.info.Defs[name].(*types.Var); ok {
					if _, isPtr := types.Unalias(obj.Type()).(*types.Pointer); !isPtr {
						origins[obj] = "param"
					}
				}
			}
		}
	}

	// Named results.
	if funcDecl.Type.Results != nil {
		for _, field := range funcDecl.Type.Results.List {
			for _, name := range field.Names {
				if obj, ok := a.info.Defs[name].(*types.Var); ok {
					origins[obj] = "result"
				}
			}
		}
	}

	// Body-declared locals — := defines, var specs, range/if/switch defines.
	ast.Inspect(funcDecl.Body, func(n ast.Node) bool {
		ident, ok := n.(*ast.Ident)

		if !ok {
			return true
		}

		if obj, ok := a.info.Defs[ident].(*types.Var); ok && !obj.IsField() {
			if _, exists := origins[obj]; !exists {
				origins[obj] = "local"
			}
		}

		return true
	})

	type localState struct {
		addressTaken bool
		keptReasons  []string
	}

	states := map[*types.Var]*localState{}

	stateOf := func(obj *types.Var) *localState {
		state, exists := states[obj]

		if !exists {
			state = &localState{}
			states[obj] = state
		}

		return state
	}

	ast.Inspect(funcDecl.Body, func(n ast.Node) bool {
		ident, ok := n.(*ast.Ident)

		if !ok {
			return true
		}

		obj, ok := a.info.Uses[ident].(*types.Var)

		if !ok {
			if defObj, isDef := a.info.Defs[ident].(*types.Var); isDef {
				obj = defObj
			} else {
				return true
			}
		}

		origin, tracked := origins[obj]

		if !tracked {
			return true
		}

		_ = origin
		state := stateOf(obj)

		// Address-connected use classification.
		reason, addressUse := a.classifyLocalUse(ident, funcDecl.Body, parents, lowered)

		if addressUse {
			state.addressTaken = true
		}

		if reason != "" {
			state.keptReasons = appendUnique(state.keptReasons, reason)
		}

		return true
	})

	// Emit verdicts for the address-taken set only (a never-addressed local was never a box).
	names := make([]*types.Var, 0, len(states))

	for obj, state := range states {
		if state.addressTaken {
			names = append(names, obj)
		}
	}

	sort.Slice(names, func(i, j int) bool {
		if names[i].Pos() != names[j].Pos() {
			return names[i].Pos() < names[j].Pos()
		}

		return names[i].Name() < names[j].Name()
	})

	for _, obj := range names {
		state := states[obj]

		a.result.Locals = append(a.result.Locals, refLocalVerdict{
			Func:        funcName,
			Name:        obj.Name(),
			Origin:      origins[obj],
			Lowers:      len(state.keptReasons) == 0,
			KeptReasons: state.keptReasons,
		})
	}
}

// classifyLocalUse classifies one use of a box-candidate variable for the locals census. Returns
// a kept-reason (empty when the use is compatible with reversion) and whether the use is
// address-connected (an explicit & or an implicit address-take).
func (a *refLoweringAnalysis) classifyLocalUse(ident *ast.Ident, bodyRoot ast.Node, parents map[ast.Node]ast.Node, lowered map[refPosKey]bool) (string, bool) {
	inClosure := crossesFuncLit(ident, bodyRoot, parents)

	// Climb the value chain from the ident: selectors/indexes stay within the variable's own
	// storage; the interesting dispositions are & (explicit address), slicing an array (implicit),
	// and a pointer-receiver method call (implicit).
	var node ast.Node = ident

	for {
		parent := parents[node]

		switch p := parent.(type) {
		case *ast.ParenExpr:
			node = p
			continue

		case *ast.SelectorExpr:
			if p.X != node {
				return "", false
			}

			if method, ok := a.info.Uses[p.Sel].(*types.Func); ok {
				if methodHasPointerReceiver(method) && !exprTypeIsPointer(a.info, node) {
					// x.M() / x.f.M() on a VALUE chain with a ж receiver — implicit &x keeps the
					// box. An already-pointer operand (t0 := new(T); t0.M()) carries its own box
					// and takes no address of the local.
					return "ptr-receiver", true
				}

				return "", false
			}

			node = p
			continue

		case *ast.IndexExpr:
			if p.X == node {
				node = p
				continue
			}

			return "", false

		case *ast.SliceExpr:
			if p.X != node {
				return "", false
			}

			// Slicing an ARRAY-typed chain aliases the variable's storage (implicit address).
			chainType := a.info.TypeOf(node.(ast.Expr))

			if chainType != nil {
				if _, isArray := chainType.Underlying().(*types.Array); isArray {
					return "array-slice", true
				}
			}

			return "", false

		case *ast.UnaryExpr:
			if p.Op != token.AND {
				return "", false
			}

			// Explicit address of the variable (or of a field/element of it). Disposition decides.
			if inClosure {
				return "closure-capture", true
			}

			return a.classifyLocalAddressDisposition(p, parents, lowered), true

		default:
			return "", false
		}
	}
}

// classifyLocalAddressDisposition resolves where an explicit &x / &x.f / &x[i] address goes:
// directly (through parens, at most one pointer conversion) into a Phase-A-lowered position
// outside defer/go is the reverting feed; anything else keeps the box.
func (a *refLoweringAnalysis) classifyLocalAddressDisposition(addrExpr ast.Expr, parents map[ast.Node]ast.Node, lowered map[refPosKey]bool) string {
	node := ast.Node(addrExpr)
	conversionSeen := false

	for {
		parent := parents[node]

		switch p := parent.(type) {
		case *ast.ParenExpr:
			node = p
			continue

		case *ast.CallExpr:
			if a.exprIsTypeConversion(p) {
				if conversionSeen {
					return "addr-conversion-chain"
				}

				if _, isPtr := types.Unalias(a.info.TypeOf(p)).(*types.Pointer); !isPtr {
					return "addr-unsafe-conversion"
				}

				conversionSeen = true
				node = p
				continue
			}

			argIndex := -1

			for i, arg := range p.Args {
				if ast.Unparen(arg) == node {
					argIndex = i
					break
				}
			}

			if argIndex < 0 {
				return "addr-in-callee-expr"
			}

			if callIsDeferOrGo(p, parents) {
				// §3.3 carve-out (a): an address flowing to a lowered position under defer/go
				// keeps the box.
				return "defer-go-arg"
			}

			callee := a.resolveCandidateCallee(p)

			if callee == nil {
				return "non-candidate-callee"
			}

			signature := callee.Type().(*types.Signature)
			params := signature.Params()

			if argIndex >= params.Len() || (signature.Variadic() && argIndex >= params.Len()-1) {
				return "variadic-position"
			}

			target := refPosKey{PkgPath: callee.Pkg().Path(), Func: callee.Name(), Index: argIndex}

			if !lowered[target] {
				return "unlowered-position"
			}

			// The reverting feed: &x directly at a lowered position.
			return ""

		default:
			return "addr-escapes"
		}
	}
}

// buildParentMap builds the child→parent index one classification walk climbs. Built per file,
// per pass use; the map is the price of climbing from an ident without a quadratic path search.
func buildParentMap(file *ast.File) map[ast.Node]ast.Node {
	parents := map[ast.Node]ast.Node{}
	var stack []ast.Node

	ast.Inspect(file, func(n ast.Node) bool {
		if n == nil {
			stack = stack[:len(stack)-1]
			return true
		}

		if len(stack) > 0 {
			parents[n] = stack[len(stack)-1]
		}

		stack = append(stack, n)

		return true
	})

	return parents
}

// appendUnique appends value to list unless already present (veto lists stay small; linear is fine).
func appendUnique(list []string, value string) []string {
	for _, existing := range list {
		if existing == value {
			return list
		}
	}

	return append(list, value)
}

// positionOf resolves a node's file position when a FileSet was attached (census driver); the
// wired-in driver pass records shapes without line detail.
func (a *refLoweringAnalysis) positionOf(node ast.Node) token.Position {
	if a.fset != nil {
		return a.fset.Position(node.Pos())
	}

	return token.Position{}
}

// printRefLoweringDebugCensus prints the per-package census block the -debug flag surfaces during
// any conversion — the design's §9 A1 "-debug census" shape for a single package.
func printRefLoweringDebugCensus(result *refLoweringPackageResult) {
	if result == nil {
		return
	}

	summary := summarizeRefLowering(result)

	showMessage("[ref-lowering census] %s: funcs %d (candidates %d), ptr-params %d, lowered(A) %d; locals address-taken %d, revert %d",
		result.PkgPath, summary.Funcs, summary.CandidateFuncs, summary.PtrParams, summary.LoweredParamsA,
		summary.LocalsAddressTaken, summary.LocalsRevert)

	if len(summary.ShapeCounts) > 0 {
		shapes := make([]string, 0, len(summary.ShapeCounts))

		for shape, count := range summary.ShapeCounts {
			shapes = append(shapes, fmt.Sprintf("%s=%d", shape, count))
		}

		sort.Strings(shapes)
		showMessage("[ref-lowering census]   call-arg shapes at lowered positions: %s", strings.Join(shapes, " "))
	}
}

// refLoweringSummary aggregates one package result for reporting.
type refLoweringSummary struct {
	Funcs              int            `json:"funcs"`
	CandidateFuncs     int            `json:"candidateFuncs"`
	PtrParams          int            `json:"ptrParams"`
	LoweredParamsA     int            `json:"loweredParamsA"`
	LocalsAddressTaken int            `json:"localsAddressTaken"`
	LocalsRevert       int            `json:"localsRevert"`
	ShapeCounts        map[string]int `json:"shapeCounts,omitempty"`
	VetoCounts         map[string]int `json:"vetoCounts,omitempty"`
	KeptLocalReasons   map[string]int `json:"keptLocalReasons,omitempty"`
}

// summarizeRefLowering rolls one package result up. Shape counts include only arguments whose
// target position LOWERED under Phase A — the §9(b) distribution is about sites the emission will
// actually rewrite (the strip events are visible through VetoCounts's caller-shape entries).
func summarizeRefLowering(result *refLoweringPackageResult) refLoweringSummary {
	summary := refLoweringSummary{
		ShapeCounts:      map[string]int{},
		VetoCounts:       map[string]int{},
		KeptLocalReasons: map[string]int{},
	}

	loweredPositions := map[refPosKey]bool{}

	for _, verdict := range result.Funcs {
		summary.Funcs++

		if len(verdict.FuncVetoes) == 0 {
			summary.CandidateFuncs++
		} else {
			for _, veto := range verdict.FuncVetoes {
				summary.VetoCounts[veto]++
			}
		}

		for _, param := range verdict.Params {
			summary.PtrParams++

			if param.LoweredA {
				summary.LoweredParamsA++
				loweredPositions[refPosKey{PkgPath: verdict.PkgPath, Func: verdict.Name, Index: param.Index}] = true
			}

			for _, veto := range param.Vetoes {
				summary.VetoCounts[veto]++
			}

			if param.StrippedBy != "" {
				tag := param.StrippedBy

				if colon := strings.Index(tag, ":"); colon > 0 {
					tag = tag[:colon]
				}

				summary.VetoCounts[tag]++
			}
		}
	}

	for _, callArg := range result.CallArgs {
		if loweredPositions[callArg.Target] {
			summary.ShapeCounts[callArg.Shape]++
		}
	}

	for _, local := range result.Locals {
		summary.LocalsAddressTaken++

		if local.Lowers {
			summary.LocalsRevert++
		} else {
			for _, reason := range local.KeptReasons {
				summary.KeptLocalReasons[reason]++
			}
		}
	}

	return summary
}

// relPathForCensus renders a census file path relative to root when possible (report readability).
func relPathForCensus(root, path string) string {
	if root == "" {
		return path
	}

	if rel, err := filepath.Rel(root, path); err == nil && !strings.HasPrefix(rel, "..") {
		return rel
	}

	return path
}
