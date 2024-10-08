package main

import (
	"fmt"
	"go/ast"
)

func (v *Visitor) convKeyValueExpr(keyValueExpr *ast.KeyValueExpr) string {
	return fmt.Sprintf("%s: %s", v.convExpr(keyValueExpr.Key, nil), v.convExpr(keyValueExpr.Value, nil))
}
