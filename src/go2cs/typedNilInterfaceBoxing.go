// typedNilInterfaceBoxing.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns ONE property, applied at ONE kind of place:
//
//	A Go POINTER entering INTERFACE space is represented by its pointer BOX, carrying its static
//	pointee type — however the pointer was produced.
//
// Go's interface value is a (dynamic type, value) pair, so a pointer in it is never merely an
// address: `any((*T)(nil))` is a NON-nil interface whose %T prints `*T` and whose `x.(*T)` assert
// succeeds with a nil result. Two managed renderings break that, and both are silent:
//
//   - a bare `null` reference boxes as nothing at all — the type is gone, `x == nil` reports Go's
//     false as true, the assert fails, and the reflection bridge sees no descriptor; and
//   - a DEREF-aliased pointer (`ref var p = ref Ꮡp.DerefOrNull()`) rendered by its value alias
//     boxes a COPY of the POINTEE — dynamic type `T` where Go says `*T`, pointer identity lost,
//     and a nil pointer panics at the box rather than crossing intact.
//
// A NON-empty interface target is already correct and takes nothing from here: its conversion goes
// through a generated adapter which holds the box and null-coalesces it to the canonical typed nil
// instance (go2cs-gen's AdapterImplTemplate). The EMPTY interface (`any`) has no adapter — C# boxes
// the value directly — so the treatment has to be emitted, and this is where.
//
// It is applied at the BOUNDARY rather than at the sites that mint nil, because those sites are
// unbounded and mostly not emissions at all: `var p *T` is one, but so is a struct's zero value, a
// `make([]*T, n)` element and a map miss, none of which the converter writes. The boundary is
// finite — the same set of slots boxUntypedConstAsDefaultType already serves, for the same reason
// (a value's Go dynamic type must survive being boxed).
//
// The scope is a genuine `*T` (a `types.Pointer`). `unsafe.Pointer` is a Basic and renders as a
// struct, and a NAMED pointer type renders as its generated wrapper struct — neither can be a null
// reference, so neither has anything to carry.

package main

import (
	"go/ast"
	"go/token"
	"go/types"
)

// pointerBoxesIntoEmptyInterface reports whether `value` is a Go pointer being emitted into an
// EMPTY-interface slot — the condition for BOTH halves of the boundary treatment (rendering the
// pointer as its box, and carrying the type of a nil one).
func (v *Visitor) pointerBoxesIntoEmptyInterface(target types.Type, value ast.Expr) bool {
	if value == nil || !isEmptyInterfaceTarget(target) {
		return false
	}

	_, isPointer := v.getType(value, false).(*types.Pointer)

	return isPointer
}

// boxPointerIntoEmptyInterface wraps an already-rendered POINTER expression in the typed-nil
// accessor when it is being boxed into an empty interface, so a null reference crosses as the
// canonical typed nil instance instead of as an untyped C# null. A no-op for every other slot, for
// a non-pointer value, and for a pointer whose rendering provably cannot be null.
func (v *Visitor) boxPointerIntoEmptyInterface(target types.Type, value ast.Expr, rendered string) string {
	if !v.pointerBoxesIntoEmptyInterface(target, value) {
		return rendered
	}

	return v.applyTypedNilPointerBox(value, rendered)
}

// applyTypedNilPointerBox is boxPointerIntoEmptyInterface for a caller that has already established
// its slot is an empty interface (the twin of applyUntypedConstBoxCast / boxUntypedConstAsDefaultType).
// A non-pointer value, and a pointer whose rendering provably cannot be null, pass through unchanged.
func (v *Visitor) applyTypedNilPointerBox(value ast.Expr, rendered string) string {
	if rendered == "" || value == nil || v.pointerExprNeverRendersNull(value) {
		return rendered
	}

	if _, isPointer := v.getType(value, false).(*types.Pointer); !isPointer {
		return rendered
	}

	return rendered + "." + TypedNilBoxAccessor
}

// emptyInterfacePointerContexts returns `contexts` with the ident context's isPointer set when
// `value` is a pointer bound for an empty-interface slot, so a deref-aliased pointer renders as its
// BOX (`Ꮡp`) rather than as the value alias whose boxing would capture a copy of the pointee. The
// flag is inert for a pointer that already holds its own box (a plain local), so this only ever
// moves the aliased shapes.
func (v *Visitor) emptyInterfacePointerContexts(target types.Type, value ast.Expr, contexts []ExprContext) []ExprContext {
	if !v.pointerBoxesIntoEmptyInterface(target, value) {
		return contexts
	}

	result := make([]ExprContext, 0, len(contexts)+1)
	found := false

	for _, context := range contexts {
		if identContext, ok := context.(IdentContext); ok {
			identContext.isPointer = true
			result = append(result, identContext)
			found = true
			continue
		}

		result = append(result, context)
	}

	if !found {
		identContext := DefaultIdentContext()
		identContext.isPointer = true
		result = append(result, identContext)
	}

	return result
}

// pointerExprNeverRendersNull reports whether a pointer expression's RENDERING can never be a bare
// null reference, so the typed-nil accessor would be dead weight. Decided from the AST, not from
// the rendered text: an address-of (`&x`, including of a composite literal), a `new(T)`, and a
// nil→pointer CONVERSION (`(*T)(nil)`, which already renders the canonical instance) are the shapes
// that qualify. Everything else — an identifier, a field, an element, a call result — may be nil and
// is wrapped.
func (v *Visitor) pointerExprNeverRendersNull(expr ast.Expr) bool {
	switch expr := expr.(type) {
	case *ast.ParenExpr:
		return v.pointerExprNeverRendersNull(expr.X)
	case *ast.UnaryExpr:
		return expr.Op == token.AND
	case *ast.CallExpr:
		if ident, ok := expr.Fun.(*ast.Ident); ok && ident.Name == "new" && v.identIsUniverseBuiltin(ident) {
			return true
		}

		// A pointer CONVERSION is exactly as null-able as its operand: `(*T)(nil)` already renders
		// the canonical instance, and `(*Buffer)(&b)` (log/slog's buffer pool) can no more be null
		// than the address it converts. Anything else — a conversion of an identifier, of an
		// unsafe.Pointer, of a call result — stays conservative and takes the accessor.
		if len(expr.Args) == 1 {
			if isConversion, _ := v.isTypeConversion(expr); isConversion {
				if tv, ok := v.info.Types[expr.Args[0]]; ok && tv.IsNil() {
					return true
				}

				return v.pointerExprNeverRendersNull(expr.Args[0])
			}
		}
	}

	return false
}
