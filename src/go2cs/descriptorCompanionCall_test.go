// descriptorCompanionCall_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/ast"
	"go/types"
	"os"
	"strings"
	"testing"
)

// The defect these arms lock in: a generic function whose body reads a type parameter's Go NAME
// through `reflect.TypeFor` gains a COMPANION C# type parameter (`Tᴺ`, descriptorCompanion.go), and
// the companion sits in NO parameter position by construction — so C# can never infer it. A Go call
// site that relies on INFERENCE therefore emits `testComparable(Ꮡt, (int64)2)` against
// `testComparable<T, Tᴺ>`: CS0411 ×42 in hash/maphash, ×10 in unique on the 1.24.13 corpus.
//
// ⚠ EVERY OTHER PREDICATE ON convCallExpr's EXPLICIT-TYPE-ARGS CHAIN IS BLIND TO IT, which is why
// this one had to exist rather than widening one of them: they all reason over the GO signature —
// where a type parameter appears in the parameter types, which positions the call supplied — and the
// companion appears in no Go signature at all.
//
// ⚠⚠ THESE ARMS EXIST BECAUSE THE PREDICATE CAN BE DROPPED SILENTLY. It is one line in a six-term
// `||` chain that four lanes' seats now edit, and the merge that put G's
// `calleeTypeParamMixesUntypedAndTypedArgs` beside it CONFLICTED on exactly those lines. A
// resolution that keeps five of six still compiles, still passes every other arm in the suite, and
// returns CS0411 to a row nobody is re-running. The seat that found it shipped without an arm here;
// this file is that omission closed.
const descriptorCompanionCallFixture = `package companion

import "reflect"

// reads names its own type parameter to reflect.TypeFor, so it gains a companion.
func reads[T comparable](v T) string { return reflect.TypeFor[T]().String() }

// plain is the CONTROL, one axis: the same shape with no TypeFor at all, so no companion and no
// reason to force a list. maphash's testComparableNoEqual is this function in the corpus.
func plain[T comparable](v T) bool { var zero T; return v == zero }

// concrete reads a name, but of a CONCRETE type rather than of its own parameter — encoding/json's
// decode_test shape, the false positive that cost G a sizing (C1's correction d6a1fdde). No
// companion, so no forced list.
type ss string

func concrete[T comparable](v T) string { return reflect.TypeFor[ss]().String() }

func readsCall() string { return reads(int64(2)) }

func plainCall() bool { return plain(int64(2)) }

func concreteCall() string { return concrete(int64(2)) }
`

type companionCallFixture struct {
	visitor *Visitor
	calls   map[string]*ast.CallExpr
}

func loadCompanionCallFixture(t *testing.T) companionCallFixture {
	t.Helper()

	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod":       "module example/companion\n\ngo 1.23\n",
		"companion.go": descriptorCompanionCallFixture,
	})

	production := loadProductionForDir(t, dir)

	// ⚠ THE PREDICATE READS PROCESS-GLOBAL PACKAGE STATE, which a fixture has to install or it
	// answers FALSE for every callee. descriptorCompanionParams resolves the declaring package's
	// SYNTAX through descriptorCarrierPackage (visitTypeSpec.go), which looks in
	// `currentPackageSource` and then in `importedPackages` — a conversion run fills those per
	// package. Without them the body cannot be walked, no TypeFor site is found, and the answer is
	// indistinguishable from "this callee reads no name". Found by this arm failing on its first
	// run, which is the reason it is worth having: the same nil-handle path would make the
	// predicate silently inert in any caller that reaches it before the package is registered.
	// The cache is identity-keyed on *types.Func, so it is reset alongside to keep a sibling test's
	// entries from answering here.
	previousSource := currentPackageSource
	previousCache := descriptorCompanionCache

	t.Cleanup(func() {
		currentPackageSource = previousSource
		descriptorCompanionCacheLock.Lock()
		descriptorCompanionCache = previousCache
		descriptorCompanionCacheLock.Unlock()
	})

	currentPackageSource = production

	descriptorCompanionCacheLock.Lock()
	descriptorCompanionCache = map[*types.Func][]int{}
	descriptorCompanionCacheLock.Unlock()

	fixture := companionCallFixture{
		visitor: &Visitor{info: production.TypesInfo, pkg: production.Types},
		calls:   map[string]*ast.CallExpr{},
	}

	for _, file := range production.Syntax {
		for _, decl := range file.Decls {
			funcDecl, ok := decl.(*ast.FuncDecl)

			if !ok || funcDecl.Body == nil {
				continue
			}

			ast.Inspect(funcDecl.Body, func(node ast.Node) bool {
				callExpr, ok := node.(*ast.CallExpr)

				if !ok {
					return true
				}

				if _, exists := fixture.calls[funcDecl.Name.Name]; !exists {
					fixture.calls[funcDecl.Name.Name] = callExpr
				}

				return true
			})
		}
	}

	return fixture
}

// funIdent resolves the callee ident of the call inside the named wrapper.
func (f companionCallFixture) funIdent(t *testing.T, wrapper string) *ast.Ident {
	t.Helper()

	callExpr, ok := f.calls[wrapper]

	if !ok {
		t.Fatalf("fixture call inside %s was not found", wrapper)
	}

	funIdent := getCallFunIdent(callExpr.Fun)

	if funIdent == nil {
		t.Fatalf("call inside %s has no resolvable callee ident", wrapper)
	}

	return funIdent
}

// TestCompanionCalleeForcesExplicitTypeArgs is the direct regression: dropping
// calleeReadsDescriptorName from convCallExpr's chain reds here, and CS0411 returns to hash/maphash
// and unique.
func TestCompanionCalleeForcesExplicitTypeArgs(t *testing.T) {
	fixture := loadCompanionCallFixture(t)

	if !fixture.visitor.calleeReadsDescriptorName(fixture.funIdent(t, "readsCall")) {
		t.Fatal("a callee that reads its OWN type parameter's name was not flagged — C# cannot infer the companion, so this is CS0411 at every inferred call site")
	}
}

// TestExplicitTypeArgsChainCarriesEveryPredicate is the arm the MERGE needs, and neither this
// file's other arms nor G's can stand in for it.
//
// ⚠ A PREDICATE TEST SURVIVES A CHAIN EDIT. TestCompanionCalleeForcesExplicitTypeArgs asks
// `calleeReadsDescriptorName` the question directly, so deleting its term from convCallExpr's `||`
// chain leaves that arm GREEN while every inferred call site silently loses its type-argument list.
// The same is true of G's arms for `calleeTypeParamMixesUntypedAndTypedArgs`. The resolution of a
// conflict IN that chain therefore has no in-suite guard at all unless one reads the chain itself.
//
// Four lanes' seats now edit these lines and the G/R merge conflicted on exactly them; a resolution
// that keeps five of six compiles clean and passes the whole suite. So the guard is a SOURCE read:
// every predicate the chain is supposed to carry must appear between the `len(typeParamExpr) == 0`
// gate and its closing brace. Adding a seventh predicate means adding its name here in the same
// commit — which is the point, not an inconvenience.
func TestExplicitTypeArgsChainCarriesEveryPredicate(t *testing.T) {
	source, err := os.ReadFile("convCallExpr.go")

	if err != nil {
		t.Fatalf("reading convCallExpr.go: %v", err)
	}

	const gate = "if len(typeParamExpr) == 0 {"
	start := strings.Index(string(source), gate)

	if start < 0 {
		t.Fatalf("the explicit-type-args gate %q was not found — this arm is measuring nothing", gate)
	}

	chain := string(source)[start:]

	if end := strings.Index(chain, "renderedTypeArgs"); end > 0 {
		chain = chain[:end]
	} else {
		t.Fatal("the chain's renderedTypeArgs call was not found after the gate — the window is wrong, not the chain")
	}

	for _, predicate := range []string{
		"calleeHasConstraintOnlyTypeParam",
		"callHasMethodGroupArg",
		"calleeTypeParamUnsuppliedByCall",
		"calleeReadsDescriptorName",
		"callNeedsConstraintProxy",
		"calleeTypeParamMixesUntypedAndTypedArgs",
	} {
		if !strings.Contains(chain, predicate) {
			t.Errorf("convCallExpr's explicit-type-args chain no longer carries %s — a merge resolution dropped a term, and the CS-error class it was cut for returns silently", predicate)
		}
	}
}

// The negative controls carry the same weight: the predicate forces an explicit list, which is a
// real emission change, so each one is a guard against churning generics that do not need it.
func TestCompanionNegativeControls(t *testing.T) {
	fixture := loadCompanionCallFixture(t)

	if fixture.visitor.calleeReadsDescriptorName(fixture.funIdent(t, "plainCall")) {
		t.Error("a callee with NO reflect.TypeFor gained a forced type-argument list; maphash's testComparableNoEqual is this shape and must stay bare")
	}

	if fixture.visitor.calleeReadsDescriptorName(fixture.funIdent(t, "concreteCall")) {
		t.Error("a callee reading a CONCRETE type's name was flagged; TypeFor on a non-type-parameter mints no companion (C1's encoding/json correction)")
	}
}
