// untypedTypeParamArgs_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/ast"
	"testing"
)

// The defect these tests lock in: a call that hands ONE type parameter both an argument emitted as a
// golib `Untyped*` wrapper and an argument emitted at a Go type leaves C# with two irreconcilable
// candidates, so it infers nothing — CS0411. Go has no such problem; an untyped constant simply
// adopts the inferred type.
//
// internal/sync is the corpus's instance and carries its own control on ADJACENT lines of one loop:
//
//	expectNotSwapped(t, s, math.MaxInt, i+j+1)  // UntypedInt meets int -> CS0411
//	expectNotSwapped(t, s, i+j,        i+j+1)  // both typed        -> infers, compiles
//
// 4 of 8 emitted call sites carried math.MaxInt and exactly those 4 were the 4 CS0411.
//
// The remedy is convCallExpr's EXISTING one — join the explicit-type-args chain — which changes no
// argument text and therefore cannot widen UntypedInt's implicit conversions. That the remedy works
// was measured BEFORE the predicate was written: hand-adding `<@string, nint>` at the four sites
// builds the row's test project at rc 0 with zero error classes.
//
// ⚠ NEGATIVE CONTROLS CARRY THE SAME WEIGHT AS THE POSITIVE. Forcing explicit type arguments is a
// real emission change, so each call below that must NOT fire is a guard against churning generics
// that infer perfectly well today. `singleSlot` is the corpus's own such case — `expectNotDeleted(t,
// key, math.MaxInt)` compiles in the very same file and must keep its bare form.
//
// Emission-level proof lives in the row itself (internal/sync -test-action all past BUILD, 4 retired
// and the 4 controls unmoved); these tests pin the PREDICATE that decides it.
const untypedTypeParamFixture = `package untyped

// big is an untyped named constant — the math.MaxInt analogue. It is what emits as an UntypedInt
// wrapper, which is why the predicate keys on a named const reference and not on go/types' TypeOf:
// go/types reports an argument at its INFERRED type, so untypedness is invisible there.
const big = 1 << 40

// pair puts V in TWO argument positions — the shape that can conflict.
func pair[K comparable, V comparable](k K, old, new V) string { return "" }

// single puts V in ONE argument position, so whatever that position gives IS the inference.
func single[K comparable, V comparable](k K, v V) string { return "" }

// variadic puts V in a variadic tail, to prove the parameter walk maps a tail argument to the
// element type rather than running off the end of the parameter list.
func variadic[V comparable](first V, rest ...V) string { return "" }

// plain is not generic at all.
func plain(a, b int) string { return "" }

func mixed(i int) string      { return pair("k", big, i) }
func bothTyped(i int) string  { return pair("k", i, i+1) }
func singleSlot() string      { return single("k", big) }
func literalsOnly() string    { return pair("k", 6, 7) }
func variadicMixed(i int) string { return variadic(big, i) }
func notGeneric(i int) string { return plain(i, i+1) }
`

type untypedArgsFixture struct {
	visitor *Visitor
	calls   map[string]*ast.CallExpr
}

func loadUntypedArgsFixture(t *testing.T) untypedArgsFixture {
	t.Helper()

	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod":     "module example/untyped\n\ngo 1.23\n",
		"untyped.go": untypedTypeParamFixture,
	})

	production := loadProductionForDir(t, dir)
	fixture := untypedArgsFixture{
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

				// The call under test is the OUTERMOST one in each wrapper.
				if _, exists := fixture.calls[funcDecl.Name.Name]; !exists {
					fixture.calls[funcDecl.Name.Name] = callExpr
				}

				return true
			})
		}
	}

	return fixture
}

func (f untypedArgsFixture) fires(t *testing.T, wrapper string) bool {
	t.Helper()

	callExpr, ok := f.calls[wrapper]

	if !ok {
		t.Fatalf("fixture call inside %s was not found", wrapper)
	}

	funIdent := getCallFunIdent(callExpr.Fun)

	if funIdent == nil {
		t.Fatalf("call inside %s has no resolvable callee ident", wrapper)
	}

	return f.visitor.calleeTypeParamMixesUntypedAndTypedArgs(callExpr, funIdent)
}

// TestUntypedAndTypedArgsForOneTypeParamForceExplicitTypeArgs is the direct regression: internal/sync's
// shape must be recognised.
func TestUntypedAndTypedArgsForOneTypeParamForceExplicitTypeArgs(t *testing.T) {
	fixture := loadUntypedArgsFixture(t)

	if !fixture.fires(t, "mixed") {
		t.Fatal("a call giving one type parameter an untyped named constant AND a typed argument was not recognised; C# cannot infer through that pair (CS0411)")
	}
}

// TestVariadicTailIsWalkedAtTheElementType proves the parameter walk maps an argument past the last
// declared parameter onto that parameter's ELEMENT type, rather than indexing off the end.
func TestVariadicTailIsWalkedAtTheElementType(t *testing.T) {
	fixture := loadUntypedArgsFixture(t)

	if !fixture.fires(t, "variadicMixed") {
		t.Fatal("a variadic call mixing an untyped named constant with a typed argument was not recognised; the tail binds the element type and can conflict exactly as a fixed position does")
	}
}

// TestUntypedArgsNegativeControls are the calls that must keep their bare form. Each one is a
// distinct reason the predicate must NOT fire, and together they are what holds the emission
// footprint to the defect.
func TestUntypedArgsNegativeControls(t *testing.T) {
	fixture := loadUntypedArgsFixture(t)

	for _, control := range []struct {
		wrapper string
		why     string
	}{
		{"bothTyped", "both arguments are typed, so inference has one candidate — this is internal/sync's own adjacent-line control"},
		{"singleSlot", "the type parameter is supplied from ONE position, so whatever it gives IS the inference — expectNotDeleted's shape, which compiles today"},
		{"literalsOnly", "plain literals carry no Untyped* wrapper; C#'s own constant conversion handles them"},
		{"notGeneric", "the callee declares no type parameters at all"},
	} {
		if fixture.fires(t, control.wrapper) {
			t.Errorf("%s: predicate fired, but %s", control.wrapper, control.why)
		}
	}
}
