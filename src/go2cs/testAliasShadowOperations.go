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
	"golang.org/x/tools/go/packages"
	"os"
	"path/filepath"
	"sort"
	"strings"
)

// collectSiblingTestFuncMethodNames returns the function/method names declared by build-selected
// in-package `_test.go` files. A direct directory scan keeps ordinary single-package retranspiles
// fast: no test dependency graph or second go/packages type-check is needed. External-package tests
// are deliberately excluded because their declarations emit into a different C# package class.
func collectSiblingTestFuncMethodNames(packageDir, packageName string, options Options) ([]string, bool) {
	if packageDir == "" {
		return nil, false
	}

	targetParts := strings.Split(options.targetPlatform, "/")
	if len(targetParts) != 2 {
		return nil, false
	}

	buildContext := build.Default
	buildContext.GOOS = targetParts[0]
	buildContext.GOARCH = targetParts[1]
	buildContext.BuildTags = append([]string(nil), options.buildTags...)

	entries, err := os.ReadDir(packageDir)
	if err != nil {
		return nil, false
	}

	names := HashSet[string]{}
	hasInternal := false

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

		hasInternal = true

		for _, decl := range file.Decls {
			if funcDecl, ok := decl.(*ast.FuncDecl); ok {
				names.Add(funcDecl.Name.Name)
			}
		}
	}

	result := names.Keys()
	sort.Strings(result)
	return result, hasInternal
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

// collectWhiteboxInternalTestObjects captures every go/types object declared by an internal test
// file. The internal and external variants share one go/packages load, so the external half can
// recognize an export-test declaration by object identity even when imported export data has no Pos.
func collectWhiteboxInternalTestObjects(pkg *packages.Package) map[types.Object]bool {
	objects := map[types.Object]bool{}
	if pkg == nil || pkg.TypesInfo == nil || pkg.Fset == nil {
		return objects
	}
	for _, obj := range pkg.TypesInfo.Defs {
		if obj == nil {
			continue
		}
		fileName := pkg.Fset.Position(obj.Pos()).Filename
		if strings.HasSuffix(strings.ToLower(fileName), "_test.go") {
			objects[obj] = true
		}
	}
	return objects
}

// collectWhiteboxBridgeDeclaredNames captures the GO names the bridge class declares: every
// package-level object an internal `_test.go` contributes, plus its METHODS — those emit as static
// extension members of the same class and hide a same-named production member just as a function
// does. See whiteboxBridgeDeclaredNames.
func collectWhiteboxBridgeDeclaredNames(pkg *packages.Package) HashSet[string] {
	names := HashSet[string]{}

	if pkg == nil || pkg.TypesInfo == nil || pkg.Fset == nil {
		return names
	}

	for ident, obj := range pkg.TypesInfo.Defs {
		if obj == nil || ident == nil || obj.Name() == "" || obj.Name() == "_" {
			continue
		}

		fileName := pkg.Fset.Position(obj.Pos()).Filename

		if !strings.HasSuffix(strings.ToLower(fileName), "_test.go") {
			continue
		}

		switch typed := obj.(type) {
		case *types.Func:
			// A package-level func, or a method (whose Parent is nil) — both land in the class.
			if signature, ok := typed.Type().(*types.Signature); ok && signature.Recv() != nil {
				names.Add(obj.Name())
				continue
			}

			if obj.Parent() == pkg.Types.Scope() {
				names.Add(obj.Name())
			}
		case *types.Var, *types.Const, *types.TypeName:
			if obj.Parent() == pkg.Types.Scope() {
				names.Add(obj.Name())
			}
		}
	}

	return names
}

// whiteboxProductionNameShadowed reports a BARE reference, from inside the bridge, to a
// package-level production member the bridge itself declares a same-named member for.
func (v *Visitor) whiteboxProductionNameShadowed(obj types.Object) bool {
	if !v.whiteboxProductionObject(obj) || !whiteboxBridgeDeclaredNames.Contains(obj.Name()) {
		return false
	}

	if v.pkg == nil {
		return false
	}

	switch obj.(type) {
	case *types.Func, *types.Var, *types.Const:
		return obj.Parent() == v.pkg.Scope()
	}

	return false
}

// packageScopeClassName names the static class that DECLARES a package-level object of the package
// currently being emitted — the qualifier to use when a bare reference would bind to something
// else (a same-named C# local, which is function-scoped).
//
// Normally that is `<pkg>_package`. Under the white-box test model the emission unit is the bridge
// class instead, and a declaration contributed by an internal `_test.go` lives THERE, not in the
// production class: crypto/md5's `buf := buf` reads a package-level `buf` declared in md5_test.go,
// and qualifying it as `md5_package.buf` names nothing (CS0117). Production members referenced
// from the same bridge keep the production class, so both halves stay addressable.
func (v *Visitor) packageScopeClassName(obj types.Object) string {
	if v.options.testClassNameOverride != "" && v.declaredInTestFile(obj) {
		return v.options.testClassNameOverride
	}

	return getSanitizedImport(packageName + PackageSuffix)
}

// declaredInTestFile reports whether an object's declaration site is a `_test.go` file — the
// signal that it emits into the test variant's class rather than the production one.
func (v *Visitor) declaredInTestFile(obj types.Object) bool {
	if obj == nil {
		return false
	}

	if whiteboxInternalTestObjects[obj] {
		return true
	}

	if v.fset == nil {
		return false
	}

	return strings.HasSuffix(strings.ToLower(v.fset.Position(obj.Pos()).Filename), "_test.go")
}

// whiteboxBridgeObject reports an internal-test object referenced from the external variant.
func (v *Visitor) whiteboxBridgeObject(obj types.Object) bool {
	if !v.options.testWhiteboxReference || !v.options.testExternalVariant || obj == nil ||
		obj.Pkg() == nil || obj.Pkg().Path() != v.options.testProductionPath {
		return false
	}
	if whiteboxInternalTestObjects[obj] {
		return true
	}
	fileName := v.fset.Position(obj.Pos()).Filename
	return strings.HasSuffix(strings.ToLower(fileName), "_test.go")
}

// whiteboxBridgeNamedType qualifies an internal-test type referenced by the external variant.
// Type arguments render through the same constraint-proxy substitution the production twin
// applies, so a generic bridge type instantiated over a proxied constraint spells both halves
// consistently.
func (v *Visitor) whiteboxBridgeNamedType(named *types.Named) (string, bool) {
	if named == nil || !v.whiteboxBridgeObject(named.Obj()) {
		return "", false
	}
	name := getSanitizedIdentifier(named.Obj().Name())
	if typeArgs := named.TypeArgs(); typeArgs != nil && typeArgs.Len() > 0 {
		args := make([]string, typeArgs.Len())
		for i := 0; i < typeArgs.Len(); i++ {
			if proxyName, ok := v.constraintProxyArg(named, i); ok {
				args[i] = proxyName
			} else {
				args[i] = v.getTypeName(typeArgs.At(i), false)
			}
		}
		name += "[" + strings.Join(args, ", ") + "]"
	}
	return "global::" + packageNamespace + "." + v.options.testInternalBridgeName + "." + name, true
}

// whiteboxProductionObject reports a production declaration while converting the internal
// white-box bridge. go/packages presents production and internal-test declarations as one Go
// package, but declaration position restores their different C# owners.
func (v *Visitor) whiteboxProductionObject(obj types.Object) bool {
	if !v.options.testWhiteboxReference || !v.options.testInlineTypeAccess || obj == nil ||
		obj.Pkg() == nil || obj.Pkg().Path() != v.options.testProductionPath {
		return false
	}

	fileName := v.fset.Position(obj.Pos()).Filename
	return fileName != "" && !strings.HasSuffix(strings.ToLower(fileName), "_test.go")
}

// whiteboxProductionNamedType qualifies a production-declared type through its referenced class
// before ordinary same-Go-package rendering can erase that owner.
func (v *Visitor) whiteboxProductionNamedType(named *types.Named) (string, bool) {
	if named == nil || !v.whiteboxProductionObject(named.Obj()) {
		return "", false
	}

	obj := named.Obj()

	name := getSanitizedIdentifier(obj.Name())
	if typeArgs := named.TypeArgs(); typeArgs != nil && typeArgs.Len() > 0 {
		args := make([]string, typeArgs.Len())
		for i := 0; i < typeArgs.Len(); i++ {
			if proxyName, ok := v.constraintProxyArg(named, i); ok {
				args[i] = proxyName
			} else {
				args[i] = v.getTypeName(typeArgs.At(i), false)
			}
		}
		name += "[" + strings.Join(args, ", ") + "]"
	}

	return "global::" + packageNamespace + "." + getSanitizedImport(v.options.testProductionName+PackageSuffix) + "." + name, true
}

// whiteboxBridgeUse reports an external test reference to an object contributed by the
// package-under-test's internal _test.go files. Object identity and declaration position keep
// production members and same-spelled unrelated declarations on their existing paths. A struct
// FIELD is never bridge-qualified: its ident renders inside member-access and named-argument
// positions (`new T(Field: v)`), where a class-qualified spelling is not legal C#.
func (v *Visitor) whiteboxBridgeUse(ident *ast.Ident) bool {
	if !v.options.testWhiteboxReference || !v.options.testExternalVariant || v.options.testInternalBridgeName == "" || v.info.Defs[ident] != nil {
		return false
	}

	obj := v.info.ObjectOf(ident)

	if varObj, ok := obj.(*types.Var); ok && varObj.IsField() {
		return false
	}

	return v.whiteboxBridgeObject(obj)
}

func (v *Visitor) whiteboxBridgeMember(ident *ast.Ident) string {
	name := getSanitizedIdentifier(v.getIdentName(ident))
	switch v.info.ObjectOf(ident).(type) {
	case *types.Func:
		name = getSanitizedFunctionName(v.getIdentName(ident))
		if testMethodRenames[v.info.ObjectOf(ident)] {
			name = ShadowVarMarker + name
		}
	case *types.TypeName:
		name = convertToCSTypeName(v.getIdentName(ident))
	}
	return v.options.testInternalBridgeName + "." + name
}
