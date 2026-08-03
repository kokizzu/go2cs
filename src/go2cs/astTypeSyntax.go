// astTypeSyntax.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the small AST/type accessors the rest of the converter leans on constantly:
// reaching the type SYNTAX inside an expression, and reaching the resolved go/types type behind
// an expression.
//
// The distinction matters. typeSyntaxOf and friends stay in the syntax world — useful when what
// matters is how the source SPELLED a type (an anonymous literal, a composed constraint). getType
// and friends cross into go/types, where the answer is what the type CHECKER concluded, which may
// have no syntax anywhere (an inferred type argument).
//
// Everything here is a lookup, not a decision. Anything that renders a NAME belongs in
// typeNameResolution.go instead.

package main

import (
	"go/ast"
	"go/types"
)

// typeSyntaxOf narrows an expression to the sub-node that carries its written TYPE syntax, for the
// call sites that hand an arbitrary expression to the extractors (a call argument, most of all).
// Every other form is already type syntax and passes through.
func typeSyntaxOf(expr ast.Expr) ast.Expr {
	switch typed := expr.(type) {
	case *ast.CompositeLit:
		return typed.Type
	case *ast.IndexExpr:
		return typed.X
	case *ast.SliceExpr:
		return typed.X
	case *ast.CallExpr:
		return typed.Fun
	case *ast.TypeAssertExpr:
		return typed.Type
	case *ast.SelectorExpr:
		return typed.X
	}

	return expr
}

// composedTypeOperands returns the sub-expressions a TYPE-COMPOSING expression is built from: the
// pointee of `*T`, the element of `[]T`/`[N]T`/`...T`, a map's value then key, a channel's element,
// and the inside of a parenthesization. Composition is what puts an anonymous struct at a depth a
// one-level probe cannot see — net's `var ipStringTests = []*struct{…}{…}` is a pointer inside a
// slice, and before this the declaration emitted the raw Go `struct{…}` text into the C# type
// (CS1031 and a whole-file syntax cascade).
func composedTypeOperands(expr ast.Expr) []ast.Expr {
	switch typed := expr.(type) {
	case *ast.StarExpr:
		return []ast.Expr{typed.X}
	case *ast.ArrayType:
		return []ast.Expr{typed.Elt}
	case *ast.Ellipsis:
		return []ast.Expr{typed.Elt}
	case *ast.ParenExpr:
		return []ast.Expr{typed.X}
	case *ast.MapType:
		return []ast.Expr{typed.Value, typed.Key}
	case *ast.ChanType:
		return []ast.Expr{typed.Value}
	}

	return nil
}

// firstAnonymousTypeLiteral walks a type expression's composition operands depth-first and returns
// the first node the predicate accepts, or nil. Deliberately FIRST-match: the lifting callers can
// name one anonymous type per declaration, so a type expression carrying two distinct anonymous
// literals (`map[struct{…}]struct{…}`) lifts the value's and leaves the key's — the residual the
// one-level probe had for every composed shape, now narrowed to that one.
func firstAnonymousTypeLiteral(expr ast.Expr, match func(ast.Expr) bool) ast.Expr {
	if expr == nil {
		return nil
	}

	if match(expr) {
		return expr
	}

	for _, operand := range composedTypeOperands(expr) {
		if found := firstAnonymousTypeLiteral(operand, match); found != nil {
			return found
		}
	}

	return nil
}

func (v *Visitor) getUnderlyingType(expr ast.Expr) types.Type {
	typ := v.info.TypeOf(expr)
	if typ == nil {
		return nil
	}

	// If it's already a concrete type, return it
	if _, isInterface := typ.Underlying().(*types.Interface); !isInterface {
		return typ
	}

	// Get the type and value information
	tv, ok := v.info.Types[expr]
	if !ok {
		return nil
	}

	// The concrete type is available in the type checker's type-and-value info
	if tv.IsValue() {
		return tv.Type
	}

	return nil
}

func getIdentifier(node ast.Node) *ast.Ident {
	var ident *ast.Ident

	if identExpr, ok := node.(*ast.Ident); ok {
		ident = identExpr
	} else if indexExpr, ok := node.(*ast.IndexExpr); ok {
		return getIdentifier(indexExpr.X)
	} else if starExpr, ok := node.(*ast.StarExpr); ok {
		ident = getIdentifier(starExpr.X)
	} else if chanExpr, ok := node.(*ast.ChanType); ok {
		ident = getIdentifier(chanExpr.Value)
	} else if arrayExpr, ok := node.(*ast.ArrayType); ok {
		ident = getIdentifier(arrayExpr.Elt)
	} else if mapExpr, ok := node.(*ast.MapType); ok {
		ident = getIdentifier(mapExpr.Key)
	} else if selExpr, ok := node.(*ast.SelectorExpr); ok {
		ident = getIdentifier(selExpr.X)
	}

	// TODO: Other types expected to have an identifier
	/*
		} else if funcExpr, ok := node.(*ast.FuncType); ok {
			ident = getIdentifier(funcExpr.Results)
		}
	*/

	return ident
}

func (v *Visitor) getIdentType(ident *ast.Ident) types.Type {
	// First check the Types map (for expressions)
	if tv, ok := v.info.Types[ident]; ok {
		return tv.Type
	}

	// Then check the Defs map (for declarations)
	if obj := v.info.Defs[ident]; obj != nil {
		return obj.Type()
	}

	// Finally, check the Uses map (for identifier usages)
	if obj := v.info.Uses[ident]; obj != nil {
		return obj.Type()
	}

	return nil
}

// isUnsignedType reports whether the expression's contextual type is an unsigned
// integer. An untyped constant adopts its target type (e.g. a uint32 argument), so
// this drives correct C# literal suffixing for values outside the int32 range.
func (v *Visitor) isUnsignedType(expr ast.Expr) bool {
	if tv, ok := v.info.Types[expr]; ok && tv.Type != nil {
		if basic, ok := tv.Type.Underlying().(*types.Basic); ok {
			return basic.Info()&types.IsUnsigned != 0
		}
	}

	return false
}

func (v *Visitor) getType(expr ast.Expr, underlying bool) types.Type {
	if expr == nil {
		return nil
	}

	exprType := v.info.TypeOf(expr)

	if exprType == nil {
		return nil
	}

	if underlying {
		return exprType.Underlying()
	}

	return exprType
}

func getParameterType(sig *types.Signature, i int) (types.Type, bool) {
	var paramType types.Type
	params := sig.Params()

	// Check variadic parameter type
	if sig.Variadic() && i >= params.Len()-1 {
		paramType = params.At(params.Len() - 1).Type()

		if sliceType, ok := paramType.(*types.Slice); ok {
			paramType = sliceType.Elem()
		}
	} else if i < params.Len() {
		paramType = params.At(i).Type()
	} else {
		return nil, false
	}

	return paramType, true
}

func (v *Visitor) getVarIdent(varType *types.Var) *ast.Ident {
	for ident, obj := range v.info.Defs {
		if obj == varType {
			return ident
		}
	}

	return nil
}

func (v *Visitor) getExprType(expr ast.Expr) types.Type {
	return v.info.TypeOf(expr)
}

// argIsUntypedNil reports whether expr is the predeclared `nil` identifier (not a shadowing local,
// and not an `&x` / typed-nil expression).
func argIsUntypedNil(expr ast.Expr, info *types.Info) bool {
	ident, ok := expr.(*ast.Ident)

	if !ok || ident.Name != "nil" {
		return false
	}

	if obj := info.Uses[ident]; obj != nil {
		return obj == types.Universe.Lookup("nil")
	}

	if tv, ok := info.Types[expr]; ok {
		return tv.Type == types.Typ[types.UntypedNil]
	}

	return false
}
