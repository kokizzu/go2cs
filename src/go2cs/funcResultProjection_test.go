// funcResultProjection_test.go - Gbtc
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
	"testing"
)

// The defect this locks in (H7 red 3, COORD 17e1ba0d2): crypto/internal/fips140/hmac's cast.go, new at
// 1.24, calls `New(sha256.New, input)` against `New[H fips140.Hash](h func() H, key []byte)`. The method-group
// argument forces explicit type arguments, which rendered the box — `New<ж<sha256.Digest>>` — and a box can
// never NOMINALLY satisfy `where H : fips140.Hash`: its generated pointer adapter implements the interface,
// the box does not (CS0311). The fix renders H as the constraint and widens the delegate through the adapter,
// the func-result twin of the slice-element projection go/ast's walkList already takes.
//
// RED 4 (COORD 50c02fe0e) is the same rule one type-argument kind over: crypto/hkdf passes a `func() hash.Hash`
// into `[H fips140.Hash]`, and `hash.Hash` is a SIBLING of the constraint (identical method set, no embedding
// edge), so C# sees no nominal relation and `Extract<hash.Hash>` is CS0311 too. A sibling interface argument
// projects through the generated INTERFACE adapter; an interface that embeds the constraint, or IS it, already
// satisfies it nominally in the emitted C# and declines.
//
// These tests pin the predicate's scope against the converter's own helpers: the func-result reach projects,
// a sibling-parameterized constraint is instantiated over the call's arguments, and every other reach — a
// value argument, a self-referential constraint (the proxy's), a bare parameter, a result naming the type
// parameter, a two-result factory, an interface already derived from the constraint — declines.
const funcResultFixture = `package funcresult

type named interface{ label() string }

// labeler is a SIBLING of named: the same method set, no embedding edge (hash.Hash vs fips140.Hash).
type labeler interface{ label() string }

// labelerPlus EMBEDS named, so the emitted C# interface derives from it nominally.
type labelerPlus interface {
	named
	extra()
}

// keyed is parameterized by a SIBLING type parameter, not by the one it constrains.
type keyed[E any] interface{ encap() E }

// point is self-referential: the constraint proxy's case, never this rule's.
type point[T any] interface{ combine(T) T }

type digest struct{ v int }

func (d *digest) label() string            { return "digest" }
func (d *digest) encap() int               { return d.v }
func (d *digest) combine(o *digest) *digest { return o }
func (d *digest) extra()                    {}

type value struct{}

func (value) label() string { return "value" }

func newDigest() *digest                   { return &digest{} }
func newValue() value                      { return value{} }
func newLabeler() labeler                  { return &digest{} }
func newPlus() labelerPlus                 { return &digest{} }
func newNamed() named                      { return &digest{} }
func newAnon() interface{ label() string } { return &digest{} }

func factory[H named](h func() H, key []byte) int        { return len(key) }
func sibling[E any, D keyed[E]](newD func() D, e E) int  { return 0 }
func proxied[P point[P]](newP func() P) int              { return 0 }
func bareToo[H named](h func() H, other H) int           { return 0 }
func returns[H named](h func() H) H                      { return h() }
func twoResults[H named](h func() (H, error)) int        { return 0 }
func variadic[H named](hs ...func() H) int               { return len(hs) }

func pointerCall() int   { return factory(newDigest, nil) }
func valueCall() int     { return factory(newValue, nil) }
func siblingCall() int   { return sibling(newDigest, 1) }
func proxiedCall() int   { return proxied(newDigest) }
func bareCall() int      { return bareToo(newDigest, newDigest()) }
func returnsCall() named { return returns(newDigest) }
func twoCall() int       { return twoResults(func() (*digest, error) { return newDigest(), nil }) }
func variadicCall() int  { return variadic(newDigest) }
func ifaceCall() int     { return factory(newLabeler, nil) }
func embedCall() int     { return factory(newPlus, nil) }
func sameCall() int      { return factory(newNamed, nil) }
func anonCall() int      { return factory(newAnon, nil) }
`

func loadFuncResultFixture(t *testing.T) (*Visitor, map[string]*ast.CallExpr) {
	t.Helper()

	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod":        "module example/funcresult\n\ngo 1.23\n",
		"funcresult.go": funcResultFixture,
	})

	// renderedTypeArgs consults the constraint proxy first, which registers what it resolves; stand the
	// map up and restore it so these tests neither panic on a nil map nor leak into a sibling test.
	previousProxies := constraintProxies
	t.Cleanup(func() { constraintProxies = previousProxies })
	constraintProxies = make(map[string][2]string)

	production := loadProductionForDir(t, dir)
	visitor := &Visitor{info: production.TypesInfo, pkg: production.Types}
	calls := map[string]*ast.CallExpr{}

	for _, file := range production.Syntax {
		for _, decl := range file.Decls {
			funcDecl, ok := decl.(*ast.FuncDecl)

			if !ok || funcDecl.Body == nil {
				continue
			}

			ast.Inspect(funcDecl.Body, func(node ast.Node) bool {
				if callExpr, ok := node.(*ast.CallExpr); ok {
					if _, exists := calls[funcDecl.Name.Name]; !exists {
						calls[funcDecl.Name.Name] = callExpr
					}
				}

				return true
			})
		}
	}

	return visitor, calls
}

func funcResultInstance(t *testing.T, visitor *Visitor, calls map[string]*ast.CallExpr, wrapper string) (*ast.Ident, *types.TypeList) {
	t.Helper()

	callExpr, ok := calls[wrapper]

	if !ok {
		t.Fatalf("fixture call inside %s was not found", wrapper)
	}

	funIdent := getCallFunIdent(callExpr.Fun)

	if funIdent == nil {
		t.Fatalf("call inside %s has no resolvable callee ident", wrapper)
	}

	instance, ok := visitor.info.Instances[funIdent]

	if !ok || instance.TypeArgs == nil {
		t.Fatalf("go/types recorded no instantiation for the call inside %s", wrapper)
	}

	return funIdent, instance.TypeArgs
}

// TestFuncResultProjectionPositive is the direct regression: the hmac shape projects, renders its type argument
// as the constraint, and maps only the `func() H` argument onto the projection.
func TestFuncResultProjectionPositive(t *testing.T) {
	visitor, calls := loadFuncResultFixture(t)
	funIdent, typeArgs := funcResultInstance(t, visitor, calls, "pointerCall")

	arg, constraint, ok := visitor.funcResultProjection(funIdent, typeArgs, 0)

	if !ok {
		t.Fatal("a pointer reached as a func RESULT against a plain method-set constraint did not project — this is the CS0311 defect")
	}

	if got := arg.String(); got != "*example/funcresult.digest" {
		t.Fatalf("projected pointer = %s, want *example/funcresult.digest", got)
	}

	if got := constraint.String(); got != "example/funcresult.named" {
		t.Fatalf("projected constraint = %s, want example/funcresult.named", got)
	}

	if rendered := visitor.renderedTypeArgs(funIdent, typeArgs); len(rendered) != 1 || rendered[0] != "named" {
		t.Fatalf("renderedTypeArgs = %v, want [named] — the constraint, not the box", rendered)
	}

	if _, _, ok := visitor.funcResultProjectionArg(funIdent, typeArgs, 0); !ok {
		t.Fatal("argument 0 (`h func() H`) did not map onto the projection")
	}

	if _, _, ok := visitor.funcResultProjectionArg(funIdent, typeArgs, 1); ok {
		t.Fatal("argument 1 (`key []byte`) mapped onto the projection")
	}

	// A constraint parameterized by a SIBLING type parameter is not self-referential: it is instantiated
	// over the call's own type arguments (keyed[int]) and projects.
	siblingIdent, siblingArgs := funcResultInstance(t, visitor, calls, "siblingCall")

	if _, constraint, ok := visitor.funcResultProjection(siblingIdent, siblingArgs, 1); !ok {
		t.Fatal("a sibling-parameterized constraint did not project")
	} else if got := constraint.String(); got != "example/funcresult.keyed[int]" {
		t.Fatalf("sibling constraint = %s, want example/funcresult.keyed[int]", got)
	}

	if _, _, ok := visitor.funcResultProjection(siblingIdent, siblingArgs, 0); ok {
		t.Fatal("the sibling's own `E any` position (an int, no method set) projected")
	}
}

// TestFuncResultProjectionSiblingInterface is RED 4's regression: a declared interface that satisfies the
// constraint by method set but does not derive from it projects exactly as the pointer does, renders the
// constraint, and hands back the interface itself as the argument to wrap.
func TestFuncResultProjectionSiblingInterface(t *testing.T) {
	visitor, calls := loadFuncResultFixture(t)
	funIdent, typeArgs := funcResultInstance(t, visitor, calls, "ifaceCall")

	arg, constraint, ok := visitor.funcResultProjection(funIdent, typeArgs, 0)

	if !ok {
		t.Fatal("a SIBLING interface reached as a func RESULT did not project — this is RED 4's CS0311 (hash.Hash vs fips140.Hash)")
	}

	if got := arg.String(); got != "example/funcresult.labeler" {
		t.Fatalf("projected argument = %s, want example/funcresult.labeler", got)
	}

	if got := constraint.String(); got != "example/funcresult.named" {
		t.Fatalf("projected constraint = %s, want example/funcresult.named", got)
	}

	if rendered := visitor.renderedTypeArgs(funIdent, typeArgs); len(rendered) != 1 || rendered[0] != "named" {
		t.Fatalf("renderedTypeArgs = %v, want [named] — the constraint, not the sibling interface", rendered)
	}

	if _, _, ok := visitor.funcResultProjectionArg(funIdent, typeArgs, 0); !ok {
		t.Fatal("argument 0 of the sibling-interface call did not map onto the projection")
	}
}

// TestFuncResultProjectionNegativeControls keeps the rule to the reach it can carry. Each call below
// instantiates the same kind of callee; none may project, or unrelated generics churn.
func TestFuncResultProjectionNegativeControls(t *testing.T) {
	visitor, calls := loadFuncResultFixture(t)

	for wrapper, reason := range map[string]string{
		"valueCall":    "a VALUE type argument satisfies its constraint nominally",
		"proxiedCall":  "a SELF-REFERENTIAL constraint is the constraint proxy's",
		"bareCall":     "a type parameter also reached as a BARE parameter would receive the pointer where C# wants the interface",
		"returnsCall":  "a RESULT naming the type parameter would hand the caller the interface where Go has the pointer",
		"twoCall":      "a two-result factory is not `func() H`",
		"variadicCall": "a variadic `...func() H` slot is a slice of delegates, not a delegate",
		"embedCall":    "an interface that EMBEDS the constraint derives from it nominally in the emitted C#",
		"sameCall":     "the constraint ITSELF as the type argument needs no projection",
		"anonCall":     "an ANONYMOUS interface has no generated adapter class to wrap with",
	} {
		funIdent, typeArgs := funcResultInstance(t, visitor, calls, wrapper)

		if _, _, ok := visitor.funcResultProjection(funIdent, typeArgs, 0); ok {
			t.Errorf("%s projected: %s", wrapper, reason)
		}

		if _, _, ok := visitor.funcResultProjectionArg(funIdent, typeArgs, 0); ok {
			t.Errorf("%s mapped argument 0 onto the projection: %s", wrapper, reason)
		}
	}
}
