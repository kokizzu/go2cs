package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"strings"
)

func (v *Visitor) convUnaryExpr(unaryExpr *ast.UnaryExpr, context UnaryExprContext) string {
	// Check if the unary expression is a pointer dereference
	if unaryExpr.Op == token.AND {
		// Check if the unary expression is an address of a structure field
		if selectorExpr, ok := unaryExpr.X.(*ast.SelectorExpr); ok {
			if _, ok := v.getType(selectorExpr.X, true).(*types.Struct); ok {
				// For a structure field, we use the "ж.of(StructType.ᏑField)" syntax
				return fmt.Sprintf("%s%s.of(%s.%s%s)", AddressPrefix, v.convExpr(selectorExpr.X, nil), v.getExprTypeName(selectorExpr.X, false), AddressPrefix, getUnsanitizedIdentifier(v.convExpr(selectorExpr.Sel, nil)))
			}
		}

		// Check if the unary expression is an address of an indexed array or slice
		if indexExpr, ok := unaryExpr.X.(*ast.IndexExpr); ok {
			exprType := v.getType(indexExpr.X, true)

			// For address of an indexed reference into slice we use the "Ꮡ(x, index)" syntax
			if getIdentifier(indexExpr.X) != nil {
				if _, ok := exprType.Underlying().(*types.Slice); ok {
					return fmt.Sprintf("%s(%s, %s)", AddressPrefix, v.convExpr(indexExpr.X, nil), v.convExpr(indexExpr.Index, nil))
				}
			}

			typeName := v.getTypeName(exprType, false)

			if strings.HasPrefix(typeName, "[") {
				// For an indexed reference into an array or slice, we use the "ж.at<T>(index)" syntax
				csTypeName := convertToCSTypeName(typeName[strings.Index(typeName, "]")+1:])
				return fmt.Sprintf("%s%s.at<%s>(%s)", AddressPrefix, v.convExpr(indexExpr.X, nil), csTypeName, v.convExpr(indexExpr.Index, nil))
			}
		}

		// Check if unary target is a pointer to a pointer
		if _, ok := v.getType(unaryExpr.X, true).(*types.Pointer); ok {
			return fmt.Sprintf("%s(%s)", AddressPrefix, v.convExpr(unaryExpr.X, nil))
		}

		// Check if unary target is not a variable or a field
		if _, ok := unaryExpr.X.(*ast.Ident); !ok {
			return fmt.Sprintf("%s(%s)", AddressPrefix, v.convExpr(unaryExpr.X, nil))
		}

		var escapesHeap bool
		ident := getIdentifier(unaryExpr.X)

		obj := v.info.Defs[ident]

		if obj == nil {
			obj = v.info.Uses[ident]
		}

		if obj != nil {
			escapesHeap = v.identEscapesHeap[obj]
		}

		if escapesHeap {
			// If the variables escapes to heap, existing pointer reference should exist
			return AddressPrefix + v.convExpr(unaryExpr.X, nil)
		}

		// Otherwise, call the address of function to get a pointer reference
		return fmt.Sprintf("%s(%s)", AddressPrefix, v.convExpr(unaryExpr.X, nil))
	}

	if unaryExpr.Op == token.ARROW {
		// Check if the unary expression is channel receive operation
		if _, ok := v.getType(unaryExpr.X, true).(*types.Chan); ok {
			var tupleResult string

			if v.options.useChannelOperators {
				if context.isTupleResult {
					tupleResult = ", " + OverloadDiscriminator
				}

				return fmt.Sprintf("%s(%s%s)", ChannelLeftOp, v.convExpr(unaryExpr.X, nil), tupleResult)
			}

			if context.isTupleResult {
				tupleResult = OverloadDiscriminator
			}

			return fmt.Sprintf("%s.Receive(%s)", v.convExpr(unaryExpr.X, nil), tupleResult)
		}
	}

	return unaryExpr.Op.String() + v.convExpr(unaryExpr.X, nil)
}
