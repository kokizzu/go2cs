// compilerIntrinsic.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/ast"
	"go/token"
	"go/types"
	"strconv"
	"strings"
)

// THE COMPILER INTRINSIC — a Go function the COMPILER replaces, whose written body exists only to
// be unreachable.
//
// `hash/maphash`'s `func escapeForHash[T comparable](v T) { panic("intrinsic") }` is the shape, and
// `panic("intrinsic")` is Go's own convention for it: the gc compiler rewrites every call, so the
// body never runs and the panic is the author saying "reaching this is a compiler bug". Converted
// faithfully, that body becomes `throw panic("intrinsic")` — and C# has no rewrite, so the call
// panics for real. MEASURED at the row: hash/maphash reached BUILD-clean and then failed 37 of 59
// tests, every one through `escapeForHash` (mailbox: the seat's row reading), including
// `testComparableNoEqual`, which reaches it through production `Comparable`.
//
// ⚠ THE RULE IS THE SHAPE, NOT THE NAME. A one-off hand-own of `escapeForHash` would fix the row and
// nothing else; the next intrinsic Go adds would arrive the same way and cost the same discovery.
// The body shape is a convention across the toolchain, so the converter recognises the convention.
//
// WHAT IS EMITTED: nothing for a function with no results, the zero value for one with results, plus
// a one-line comment naming it. That is the FAITHFUL emulation rather than an approximation:
// `escapeForHash`'s whole meaning is "force v to escape to the heap when it holds a pointer", which
// is a statement about Go's escape analysis and has no CLR counterpart — the runtime's GC does not
// need it — so a no-op IS the behaviour, not a stub standing in for one.
//
// ⚠ NARROWED THREE WAYS, and each narrowing is a control in compilerIntrinsic_test.go:
//   - the body holds EXACTLY ONE statement (a second statement means the author meant it to run);
//   - that statement is a CALL to the UNSHADOWED universe `panic` (resolved through the object, so a
//     package declaring its own `panic` cannot trip it — the name-only form at visitSwitchStmt.go is
//     the shape this deliberately does not copy);
//   - its sole argument is the STRING LITERAL "intrinsic" and nothing else (`panic("other")` is an
//     ordinary panic and stays one).
const compilerIntrinsicMarker = "intrinsic"

// intrinsicResultExpression renders the zero value an intrinsic's no-op body returns — one value, or
// the C# tuple a multi-result Go signature emits as. Each element goes through zeroValueInitializer,
// the one ladder every other zero-value site in the converter shares, so a directional channel or an
// array result answers here exactly as it answers in a `var x T` declaration.
func (v *Visitor) intrinsicResultExpression(results *types.Tuple) string {
	if results.Len() == 1 {
		return v.zeroValueInitializer(results.At(0).Type())
	}

	parts := make([]string, results.Len())

	for i := range results.Len() {
		parts[i] = v.zeroValueInitializer(results.At(i).Type())
	}

	return "(" + strings.Join(parts, ", ") + ")"
}

// isCompilerIntrinsicBody reports whether body is exactly `{ panic("intrinsic") }`.
func (v *Visitor) isCompilerIntrinsicBody(body *ast.BlockStmt) bool {
	if body == nil || len(body.List) != 1 {
		return false
	}

	exprStmt, isExpr := body.List[0].(*ast.ExprStmt)

	if !isExpr {
		return false
	}

	callExpr, isCall := exprStmt.X.(*ast.CallExpr)

	if !isCall || len(callExpr.Args) != 1 || callExpr.Ellipsis != token.NoPos {
		return false
	}

	ident, isIdent := callExpr.Fun.(*ast.Ident)

	if !isIdent || ident.Name != "panic" || !v.identIsUniverseBuiltin(ident) {
		return false
	}

	lit, isLit := callExpr.Args[0].(*ast.BasicLit)

	if !isLit || lit.Kind != token.STRING {
		return false
	}

	text, err := strconv.Unquote(lit.Value)

	return err == nil && text == compilerIntrinsicMarker
}
