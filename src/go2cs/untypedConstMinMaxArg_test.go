// untypedConstMinMaxArg_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/ast"
	"go/importer"
	"go/parser"
	"go/token"
	"go/types"
	"testing"
)

// RED 6 (COORD 2c9ecadad1): a constant EXPRESSION argument to builtin min/max beside a typed operand
// was emitted UNCAST while the constant IDENTIFIER form beside it was cast, so the expression's C#
// stayed UntypedInt, T could not be inferred from nint and UntypedInt, resolution fell to the
// params-span overload and failed — CS1503 at os/user/user_windows_test.cs(50,49), the whole
// measured population of the class in std at 1.24.13 (census: 1 site with tests on windows; 0 in
// production on any target, 0 at 1.23.12 on any target, controls populated on every arm).
//
// The rule these arms guard: an argument whose EMISSION is an UntypedInt static takes the call's
// result type, whether it arrives as an identifier or as a fold over identifiers — one predicate,
// used by both the arm's trigger and its cast application.
func TestMinMaxUntypedConstArgPredicate(t *testing.T) {
	const src = `package p

const maxNameLen, suffixLen = 20, 4
const typedConst int = 7

func hitExpr(s string) int      { return min(len(s), maxNameLen-suffixLen) }
func hitParen(s string) int     { return min(len(s), (maxNameLen - suffixLen)) }
func hitUnary(n int) int        { return max(n, -suffixLen) }
func hitIdent(s string) int     { return min(len(s), suffixLen) }
func negLit(s string) int       { return min(len(s), 8) }
func negLitExpr(s string) int   { return min(len(s), 20-4) }
func negTypedConst(n int) int   { return min(n, typedConst) }
func negTyped(a, b int) int     { return min(a, b) }
func negNoTypedSide() int       { return min(maxNameLen-suffixLen, suffixLen) }
`

	// argument index -> expected predicate result, per function
	want := map[string][]bool{
		"hitExpr":        {false, true},
		"hitParen":       {false, true},
		"hitUnary":       {false, true},
		"hitIdent":       {false, true},
		"negLit":         {false, false}, // a bare literal is not matched; the arm casts it only once triggered
		"negLitExpr":     {false, false}, // a pure-literal fold emits as C# int arithmetic, like the literal beside it
		"negTypedConst":  {false, false}, // a TYPED constant emits typed and needs no cast
		"negTyped":       {false, false},
		"negNoTypedSide": {true, true}, // both are untyped constants; Go types them int and the arm casts both
	}

	fset := token.NewFileSet()
	file, err := parser.ParseFile(fset, "p.go", src, 0)

	if err != nil {
		t.Fatalf("parse: %v", err)
	}

	info := &types.Info{
		Uses:  map[*ast.Ident]types.Object{},
		Defs:  map[*ast.Ident]types.Object{},
		Types: map[ast.Expr]types.TypeAndValue{},
	}

	if _, err := (&types.Config{Importer: importer.Default()}).Check("p", fset, []*ast.File{file}, info); err != nil {
		t.Fatalf("type-check: %v", err)
	}

	seen := map[string]bool{}

	for _, decl := range file.Decls {
		fn, ok := decl.(*ast.FuncDecl)

		if !ok {
			continue
		}

		expected, tracked := want[fn.Name.Name]

		if !tracked {
			continue
		}

		ast.Inspect(fn, func(node ast.Node) bool {
			call, ok := node.(*ast.CallExpr)

			if !ok {
				return true
			}

			ident, ok := call.Fun.(*ast.Ident)

			if !ok || (ident.Name != "min" && ident.Name != "max") {
				return true
			}

			if _, isBuiltin := info.ObjectOf(ident).(*types.Builtin); !isBuiltin {
				return true
			}

			seen[fn.Name.Name] = true

			if len(call.Args) != len(expected) {
				t.Fatalf("%s: %d arguments, fixture expects %d", fn.Name.Name, len(call.Args), len(expected))
			}

			for i, arg := range call.Args {
				if got := argRendersAsUntypedConst(info, arg); got != expected[i] {
					t.Errorf("%s argument %d: argRendersAsUntypedConst = %v, want %v", fn.Name.Name, i, got, expected[i])
				}
			}

			return true
		})
	}

	// ⚠ ANTI-VACUITY: every fixture function must have been reached. A fixture that stops parsing as
	// a builtin call — a rename, a shadow, a signature change — would otherwise leave this test
	// asserting nothing and passing, which is the shape this repo has paid for repeatedly.
	if len(seen) != len(want) {
		t.Fatalf("VACUOUS: %d of %d fixture functions reached a builtin min/max call", len(seen), len(want))
	}
}
