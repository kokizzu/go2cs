// nestedMapPointerValue_test.go - Gbtc
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
	"strings"
	"testing"
)

// RED 10. A NESTED map assignment `m[k1][k2] = v` emits `m[k1].Set(k2, v)` (a C# indexer setter on the
// outer index's rvalue is CS1612), and that branch rendered its VALUE context-free while every other
// assignment RHS in visitAssignStmt takes appendRhsPtrContext. A deref-aliased pointer PARAMETER therefore
// arrived as its value alias where the map holds `ж<T>`: crypto/x509's
// `pg.strata[pg.depth][string(n.validPolicy.der)] = n` emitted `.Set(…, n)` against
// `map<@string, ж<policyGraphNode>>` — CS1503, in all three L3 flavours. The SINGLE-index form `m[k] = n`
// was always right, because it passes that context; this is the same rule reaching its second caller.
const nestedMapPointerFixture = `package nestedmap

type node struct{ v int }

type graph struct {
	strata []map[string]*node
	plain  []map[string]int
	depth  int
}

// insert is the shape: a nested map whose VALUE type is a pointer, written from a pointer PARAMETER.
func (g *graph) insert(n *node) {
	g.strata[g.depth][n.name()] = n
}

// insertValue is the NEGATIVE: the same alias, a VALUE-typed map. Its RHS is not a pointer identifier,
// so no pointer context is appended and the bare rendering stays.
func (g *graph) insertValue(n *node) {
	g.plain[g.depth][n.name()] = n.v
}

func (n *node) name() string { return "" }
`

// loadNestedMapFixture returns a Visitor over the fixture plus the two methods' nested-map assignments,
// with the parameter state visitFuncDecl would have established (paramNames / paramObjects), because the
// rendering under test is gated on identIsParameter.
func loadNestedMapFixture(t *testing.T) (*Visitor, map[string]*ast.AssignStmt, map[string]*ast.FuncDecl) {
	t.Helper()

	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod":       "module example/nestedmap\n\ngo 1.24\n",
		"nestedmap.go": nestedMapPointerFixture,
	})

	production := loadProductionForDir(t, dir)
	visitor := &Visitor{info: production.TypesInfo, pkg: production.Types, newline: "\n"}
	assigns := map[string]*ast.AssignStmt{}
	decls := map[string]*ast.FuncDecl{}

	for _, file := range production.Syntax {
		for _, decl := range file.Decls {
			funcDecl, ok := decl.(*ast.FuncDecl)

			if !ok || funcDecl.Body == nil {
				continue
			}

			decls[funcDecl.Name.Name] = funcDecl

			ast.Inspect(funcDecl.Body, func(node ast.Node) bool {
				if assign, ok := node.(*ast.AssignStmt); ok {
					if _, exists := assigns[funcDecl.Name.Name]; !exists {
						assigns[funcDecl.Name.Name] = assign
					}
				}

				return true
			})
		}
	}

	return visitor, assigns, decls
}

// withFixtureParams installs the parameter state for one fixture method, as visitFuncDecl does for a real
// conversion: the rendering under test reaches its address arm only for a genuine PARAMETER object.
func withFixtureParams(t *testing.T, visitor *Visitor, production *ast.FuncDecl) {
	t.Helper()

	visitor.paramNames = HashSet[string]{}
	visitor.paramObjects = map[types.Object]bool{}

	for _, field := range production.Type.Params.List {
		for _, name := range field.Names {
			visitor.paramNames.Add(name.Name)

			if obj := visitor.info.ObjectOf(name); obj != nil {
				visitor.paramObjects[obj] = true
			}
		}
	}
}

// emitNestedMapAssign drives the BRANCH under test — visitAssignStmt's nested-map arm — and returns the
// statement it emits. Asserting on convExpr/appendRhsPtrContext directly would pass against the UNFIXED
// converter (measured: both arms stayed green with the change reverted), because those helpers were never
// the defect; the defect was this branch not calling them. The emission is the only reading that can fail.
func emitNestedMapAssign(t *testing.T, visitor *Visitor, assign *ast.AssignStmt, funcDecl *ast.FuncDecl) string {
	t.Helper()

	withFixtureParams(t, visitor, funcDecl)

	visitor.outputBuilder = &strings.Builder{}
	visitor.indentLevel = 1
	visitor.options = Options{indentSpaces: 4}

	// No newline and no indent: the statement's own text is what is under test, not its placement.
	visitor.visitAssignStmt(assign, FormattingContext{})

	return visitor.outputBuilder.String()
}

// The POSITIVE: the map's value type is a pointer and the RHS is the pointer parameter, so the emitted
// value is the BOX (`Ꮡn`) — the spelling the map's `ж<T>` slot requires. Reverting the one-line change
// puts a bare `n` here, which is crypto/x509's CS1503.
func TestNestedMapPointerValueRendersTheBox(t *testing.T) {
	visitor, assigns, decls := loadNestedMapFixture(t)

	assign, ok := assigns["insert"]

	if !ok {
		t.Fatal("fixture assignment inside insert was not found")
	}

	rhsIdent, isIdent := assign.Rhs[0].(*ast.Ident)

	if !isIdent {
		t.Fatalf("fixture RHS is %T, want a bare identifier", assign.Rhs[0])
	}

	if _, isPtr := visitor.getIdentType(rhsIdent).(*types.Pointer); !isPtr {
		t.Fatalf("fixture premise broken: the RHS %q is not a pointer", rhsIdent.Name)
	}

	got := emitNestedMapAssign(t, visitor, assign, decls["insert"])
	want := "(~g).strata[(~g).depth].Set(n.name(), " + AddressPrefix + "n);"

	if got != want {
		t.Fatalf("the nested map's pointer VALUE slot must take the box:\n got %q\nwant %q\n(a bare %q is CS1503 against map<K, ж<T>>)",
			got, want, rhsIdent.Name)
	}
}

// The NEGATIVE: the same alias and the same branch, over a VALUE-typed map. Nothing is addressed — so the
// rule is keyed on the map's value type being a pointer, not on "this is a nested map assignment". This arm
// must stay GREEN when the change is reverted; it is what bounds the cut.
func TestNestedMapValueSlotStaysBare(t *testing.T) {
	visitor, assigns, decls := loadNestedMapFixture(t)

	assign, ok := assigns["insertValue"]

	if !ok {
		t.Fatal("fixture assignment inside insertValue was not found")
	}

	got := emitNestedMapAssign(t, visitor, assign, decls["insertValue"])
	want := "(~g).plain[(~g).depth].Set(n.name(), (~n).v);"

	if got != want {
		t.Fatalf("a VALUE-typed nested map must emit its value bare:\n got %q\nwant %q", got, want)
	}

	if strings.Contains(got, AddressPrefix) {
		t.Fatalf("a non-pointer value slot took an address prefix: %q", got)
	}
}
