// testAliasShadowOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/build"
	"go/parser"
	"go/token"
	"go/types"
	"os"
	"path/filepath"
	"sort"
	"strings"
)

// collectSiblingTestFuncMethodNames returns the function/method names declared by build-selected
// in-package `_test.go` files. A direct directory scan keeps ordinary single-package retranspiles
// fast: no test dependency graph or second go/packages type-check is needed. External-package tests
// are deliberately excluded because their declarations emit into a different C# package class.
func collectSiblingTestFuncMethodNames(packageDir, packageName string, options Options) []string {
	if packageDir == "" {
		return nil
	}

	targetParts := strings.Split(options.targetPlatform, "/")
	if len(targetParts) != 2 {
		return nil
	}

	buildContext := build.Default
	buildContext.GOOS = targetParts[0]
	buildContext.GOARCH = targetParts[1]
	buildContext.BuildTags = append([]string(nil), options.buildTags...)

	entries, err := os.ReadDir(packageDir)
	if err != nil {
		return nil
	}

	names := HashSet[string]{}

	for _, entry := range entries {
		if entry.IsDir() || !strings.HasSuffix(strings.ToLower(entry.Name()), "_test.go") {
			continue
		}

		selected, constraintErr := buildContext.MatchFile(packageDir, entry.Name())
		if constraintErr != nil || !selected {
			continue
		}

		path := filepath.Join(packageDir, entry.Name())
		file, parseErr := parser.ParseFile(token.NewFileSet(), path, nil, parser.SkipObjectResolution)
		if parseErr != nil || file.Name == nil || file.Name.Name != packageName {
			continue
		}

		for _, decl := range file.Decls {
			if funcDecl, ok := decl.(*ast.FuncDecl); ok {
				names.Add(funcDecl.Name.Name)
			}
		}
	}

	result := names.Keys()
	sort.Strings(result)
	return result
}

// testAliasShadowName reports the first imported-package alias in this statement whose
// qualification is required only by a same-package test declaration. Nested statements are
// excluded: each is visited independently and receives its own adjacent comment.
func (v *Visitor) testAliasShadowName(stmt ast.Stmt) string {
	if len(packageTestAliasShadows) == 0 {
		return ""
	}

	names := HashSet[string]{}

	ast.Inspect(stmt, func(node ast.Node) bool {
		if node == nil {
			return false
		}

		if node != stmt {
			if _, nestedStatement := node.(ast.Stmt); nestedStatement {
				return false
			}
		}

		ident, ok := node.(*ast.Ident)
		if !ok || !packageTestAliasShadows[ident.Name] {
			return true
		}

		if _, isPackageName := v.info.ObjectOf(ident).(*types.PkgName); isPackageName {
			names.Add(ident.Name)
		}

		return true
	})

	if len(names) == 0 {
		return ""
	}

	ordered := names.Keys()
	sort.Strings(ordered)
	return ordered[0]
}

// writeTestAliasShadowComment explains a qualification that otherwise looks unnecessary when the
// generated production source is reviewed without its same-package tests in view. Inline
// for-init/post clauses have no standalone comment slot; their enclosing for statement carries the
// relevant header expression and receives the comment instead.
func (v *Visitor) writeTestAliasShadowComment(stmt ast.Stmt, contexts []StmtContext) {
	for _, context := range contexts {
		if format, ok := context.(FormattingContext); ok && !format.useNewLine {
			return
		}
	}

	alias := v.testAliasShadowName(stmt)
	if alias == "" {
		return
	}

	v.targetFile.WriteString(v.newline)
	v.targetFile.WriteString(v.indent(v.indentLevel))
	v.targetFile.WriteString(fmt.Sprintf(
		"// Fully qualified to avoid alias shadowing by the same-package test declaration %q.",
		alias))
}
