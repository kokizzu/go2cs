// variadicPackView_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/ast"
	"go/parser"
	"go/token"
	"go/types"
	"testing"
)

// TestVariadicPackViewAdmitsOnlyNonRetainingUses controls ssliceUsesAreSafe BOTH ways (REC-C,
// docs/phase4/DESIGN-slice-idiom-allocations.md §A): the pack keeps its heap copy for every use
// that can retain, grow, write through or outlive it, and takes the stack view for every use that
// only reads it within the frame -- including the two uses §A adds, copy SOURCE and PASS-THROUGH.
//
// RED at fa18863b94 on the four "admitted (§A)" cases: the predicate did not know them.
func TestVariadicPackViewAdmitsOnlyNonRetainingUses(t *testing.T) {
	const source = `package pack

type holder struct{ xs []int }

var global []int

func sink(xs ...int) {}

func takesSlice(xs []int) {}

// admitted before §A
func lenOnly(xs ...int) int  { return len(xs) }
func indexed(xs ...int) int  { return xs[0] }
func ranged(xs ...int) (t int) {
	for _, x := range xs {
		t += x
	}
	return
}

// admitted (§A)
func forwarded(xs ...int)             { sink(xs...) }
func appendedFrom(d []int, xs ...int) []int { return append(d, xs...) }
func copySource(d []int, xs ...int) int     { return copy(d, xs) }
func forwardedTwice(xs ...int)        { sink(xs...); sink(1, 2) ; sink(xs...) }

// refused
func appendedInto(xs ...int) []int   { return append(xs, 1) }
func copyDestination(xs ...int) int  { return copy(xs, []int{1}) }
func deferredForward(xs ...int)      { defer sink(xs...) }
func goForward(xs ...int)            { go sink(xs...) }
func captured(xs ...int) func()      { return func() { sink(xs...) } }
func returned(xs ...int) []int       { return xs }
func stored(h *holder, xs ...int)    { h.xs = xs }
func assignedGlobal(xs ...int)       { global = xs }
func passedAsSlice(xs ...int)        { takesSlice(xs) }
func resliced(xs ...int)             { sink(xs[1:]...) }
func addressTaken(xs ...int) *int    { return &xs[0] }
`

	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, "pack.go", source, parser.ParseComments)

	if err != nil {
		t.Fatalf("parse: %v", err)
	}

	info := &types.Info{
		Types:      map[ast.Expr]types.TypeAndValue{},
		Defs:       map[*ast.Ident]types.Object{},
		Uses:       map[*ast.Ident]types.Object{},
		Implicits:  map[ast.Node]types.Object{},
		Selections: map[*ast.SelectorExpr]*types.Selection{},
		Scopes:     map[ast.Node]*types.Scope{},
	}

	pkg, err := (&types.Config{}).Check("example.com/pack", fset, []*ast.File{file}, info)

	if err != nil {
		t.Fatalf("type-check: %v", err)
	}

	files := []FileEntry{{
		file:             file,
		filePath:         "pack.go",
		identEscapesHeap: map[types.Object]bool{},
		sstringEligible:  map[types.Object]bool{},
		ssliceEligible:   map[types.Object]bool{},
		sstringConvExprs: map[*ast.CallExpr]bool{},
	}}

	performEscapeAnalysis(files, fset, pkg, info)

	eligible := map[string]bool{}

	for _, decl := range file.Decls {
		funcDecl, ok := decl.(*ast.FuncDecl)

		if !ok || funcDecl.Type.Params == nil || len(funcDecl.Type.Params.List) == 0 {
			continue
		}

		last := funcDecl.Type.Params.List[len(funcDecl.Type.Params.List)-1]

		if _, variadic := last.Type.(*ast.Ellipsis); !variadic {
			continue
		}

		eligible[funcDecl.Name.Name] = files[0].ssliceEligible[info.Defs[last.Names[0]]]
	}

	want := map[string]bool{
		"sink":    true,
		"lenOnly": true, "indexed": true, "ranged": true,
		"forwarded": true, "appendedFrom": true, "copySource": true, "forwardedTwice": true,
		"appendedInto": false, "copyDestination": false, "deferredForward": false, "goForward": false,
		"captured": false, "returned": false, "stored": false, "assignedGlobal": false,
		"passedAsSlice": false, "resliced": false, "addressTaken": false,
	}

	for name, expected := range want {
		got, found := eligible[name]

		if !found {
			t.Errorf("%s: no variadic parameter was read -- the fixture no longer says what the test claims", name)
			continue
		}

		if got != expected {
			t.Errorf("%s: stack view = %v, want %v", name, got, expected)
		}
	}

	if len(eligible) != len(want) {
		t.Errorf("read %d variadic functions, expected %d", len(eligible), len(want))
	}
}
