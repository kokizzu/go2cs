// compilerIntrinsic_test.go - Gbtc
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

// The defect these arms lock in: `hash/maphash`'s
// `func escapeForHash[T comparable](v T) { panic("intrinsic") }` is a Go COMPILER INTRINSIC — the gc
// compiler replaces every call, so the written body is unreachable by construction. Converted
// faithfully it becomes `throw panic("intrinsic")`, and C# has no rewrite: the row reached
// BUILD-clean and then failed 37 of 59 tests, every one through that call, including
// `testComparableNoEqual`, which reaches it through production `Comparable`.
//
// ⚠ THE PREDICATE IS A CLAIM ABOUT A SHAPE, SO EVERY NARROWING IS A CONTROL. A rule this broad
// ("a body that panics becomes a no-op") would silently delete real panics, which is the worst
// possible failure for a converter: the emission compiles, runs, and is wrong. Each arm below is one
// axis away from the positive.
const compilerIntrinsicFixture = `package intrinsic

// escaper is the corpus shape, generic and constrained exactly as maphash's is.
func escaper[T comparable](v T) { panic("intrinsic") }

// resulted is the same shape WITH RESULTS, so the zero-value path is exercised rather than assumed.
func resulted[T any](v T) (int, string) { panic("intrinsic") }

// other panics for a real reason and must stay a panic — one axis: the marker text.
func other[T comparable](v T) { panic("other") }

// second means it: the marker is there, but the body holds ANOTHER statement, so the author wrote a
// body that runs — one axis: the statement count.
//
// ⚠ THIS CONTROL WAS WRONG TWICE, AND THE SECOND WAY IS THE INSTRUCTIVE ONE. First it was the panic
// alone on its own line inside braces — still EXACTLY ONE statement, a layout difference dressed as
// a structural one, so it reported the predicate broken when the fixture could not reach the
// property it was named for. Then the extra statement was put BEFORE the panic, which passes for
// the WRONG REASON: the first statement is then an AssignStmt, so the ExprStmt check rejects it and
// the statement-COUNT narrowing is never reached. Relaxing that count left the control green
// — a guard refusing by a different clause than the one under test. The panic goes FIRST so the
// count is the only thing that can reject this body.
func second[T comparable](v T) {
	panic("intrinsic")
	_ = v
}

// notAPanic is the marker text handed to something that is not panic at all.
func notAPanic[T comparable](v T) { println("intrinsic") }

// empty has no statements; the predicate must not read that as the shape.
func empty[T comparable](v T) {}
`

// shadowedPanicFixture declares its OWN panic, which is legal Go. The predicate resolves the callee
// through the object, so this body is an ordinary call to a package function and must not be
// recognised — the narrowing that the name-only form at visitSwitchStmt.go does not have.
const shadowedPanicFixture = `package shadowed

func panic(s string) {}

func looksLikeOne[T comparable](v T) { panic("intrinsic") }
`

func loadIntrinsicBodies(t *testing.T, source, module string) (*Visitor, map[string]*ast.BlockStmt) {
	t.Helper()

	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod":       "module example/" + module + "\n\ngo 1.23\n",
		module + ".go": source,
	})

	production := loadProductionForDir(t, dir)
	visitor := &Visitor{info: production.TypesInfo, pkg: production.Types}
	bodies := map[string]*ast.BlockStmt{}

	for _, file := range production.Syntax {
		for _, decl := range file.Decls {
			if funcDecl, ok := decl.(*ast.FuncDecl); ok {
				bodies[funcDecl.Name.Name] = funcDecl.Body
			}
		}
	}

	return visitor, bodies
}

func TestCompilerIntrinsicBodyIsRecognised(t *testing.T) {
	visitor, bodies := loadIntrinsicBodies(t, compilerIntrinsicFixture, "intrinsic")

	for _, name := range []string{"escaper", "resulted"} {
		if !visitor.isCompilerIntrinsicBody(bodies[name]) {
			t.Errorf("%s is the compiler-intrinsic shape and was not recognised — its call panics for real in C#, which is hash/maphash's 37 failures", name)
		}
	}
}

// ⚠ These carry more weight than the positive. A false POSITIVE deletes a panic the author meant,
// and the emission still compiles and runs — so the defect ships silently.
func TestCompilerIntrinsicNegativeControls(t *testing.T) {
	visitor, bodies := loadIntrinsicBodies(t, compilerIntrinsicFixture, "intrinsic")

	for name, why := range map[string]string{
		"other":     `panic("other") is an ordinary panic; only the "intrinsic" marker is the convention`,
		"second":    `a statement FOLLOWS the panic, so the body was written to run`,
		"notAPanic": `the marker text was handed to println, not to panic`,
		"empty":     `an empty body is not the shape and must not be read as one`,
	} {
		if visitor.isCompilerIntrinsicBody(bodies[name]) {
			t.Errorf("%s was recognised as a compiler intrinsic — %s; a false positive DELETES a real panic and still compiles", name, why)
		}
	}
}

// TestShadowedPanicIsNotAnIntrinsic is the arm for resolving the callee through the OBJECT rather
// than by name. A package may declare its own `panic`, and the converter has a name-only recognition
// elsewhere (visitSwitchStmt.go) that this rule deliberately does not copy.
func TestShadowedPanicIsNotAnIntrinsic(t *testing.T) {
	visitor, bodies := loadIntrinsicBodies(t, shadowedPanicFixture, "shadowed")

	if visitor.isCompilerIntrinsicBody(bodies["looksLikeOne"]) {
		t.Fatal("a package-local func named panic was read as the universe builtin — the body is an ordinary call and its emission must keep it")
	}
}
