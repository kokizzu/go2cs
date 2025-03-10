package main

import (
	"fmt"
	"go/ast"
)

func (v *Visitor) convBinaryExpr(binaryExpr *ast.BinaryExpr, context PatternMatchExprContext) string {
	leftOperand := v.convExpr(binaryExpr.X, nil)
	binaryOp := binaryExpr.Op.String()
	rightOperand := v.convExpr(binaryExpr.Y, nil)

	if !context.usePattenMatch {
		// Check for comparisons between interface and pointer types,
		// dereferencing pointer type for the comparison if necessary
		if binaryOp == "==" || binaryOp == "!=" {
			lhsType := v.getExprType(binaryExpr.X)
			rhsType := v.getExprType(binaryExpr.Y)

			lhsIsInterface, isEmpty := isInterface(lhsType)

			if lhsIsInterface && !isEmpty && isPointer(rhsType) {
				rightOperand = fmt.Sprintf("%s%s", PointerDerefOp, rightOperand)
			} else {
				rhsIsInterface, isEmpty := isInterface(rhsType)

				if rhsIsInterface && !isEmpty && isPointer(lhsType) {
					leftOperand = fmt.Sprintf("%s%s", PointerDerefOp, leftOperand)
				}

				if lhsIsInterface && rhsIsInterface {
					// Handle interface comparison with special runtime function
					if binaryOp == "==" {
						return fmt.Sprintf("AreEqual(%s, %s)", leftOperand, rightOperand)
					} else {
						return fmt.Sprintf("!AreEqual(%s, %s)", leftOperand, rightOperand)
					}
				}
			}
		} else if binaryOp == "<<" || binaryOp == ">>" {
			rightOperand = fmt.Sprintf("(int)(%s)", rightOperand)
		}

		if binaryOp == "&^" {
			binaryOp = " & ~"
		} else {
			binaryOp = " " + binaryOp + " "
		}

		return fmt.Sprintf("%s%s%s", leftOperand, binaryOp, rightOperand)
	}

	// Handle start of pattern match expression with "is" keyword:
	if context.declareIsExpr {
		if binaryOp == "==" {
			binaryOp = ""
		} else {
			binaryOp = " " + binaryOp
		}

		return fmt.Sprintf("%s is%s %s", leftOperand, binaryOp, rightOperand)
	}

	// Handle remaining pattern match expressions:
	if binaryOp == "==" {
		binaryOp = ""
	} else {
		binaryOp = binaryOp + " "
	}

	return fmt.Sprintf("%s%s", binaryOp, rightOperand)
}
