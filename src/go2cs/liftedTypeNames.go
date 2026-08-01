// liftedTypeNames.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the naming of LIFTED types — the generated C# names given to Go types that have
// no name of their own (anonymous structs and interfaces) or whose own name cannot be used as-is.
//
// The whole problem is uniqueness under concurrency. A package's files are visited in parallel,
// each may need to lift a type, and two files must never claim the same name for different types
// nor different names for the same one. So a name is CLAIMED (claimLiftedTypeName) under the
// package lock rather than merely generated, and the taken-name check spans the whole package
// rather than the current file.
//
// Deciding a name is separate from RESOLVING a reference to one another file claimed — that is
// dynamicTypeOperations.go's deferred-marker pass.

package main

import (
	"fmt"
	"go/ast"
	"go/types"
	"strings"
)

func (v *Visitor) typeExists(name string) bool {
	// Look in the package scope
	obj := v.pkg.Scope().Lookup(name)

	if obj != nil && (obj.Type() != nil || obj.Type().Underlying() != nil) {
		return true
	}

	// Or search through all definitions
	for _, obj := range v.info.Defs {
		if obj != nil && obj.Name() == name && (obj.Type() != nil || obj.Type().Underlying() != nil) {
			return true
		}
	}

	return false
}

func getGlobalTempVarName(varPrefix string) string {
	if globalTempVarCount == nil {
		globalTempVarCount = make(map[string]int)
	}

	count := globalTempVarCount[varPrefix]
	count++
	globalTempVarCount[varPrefix] = count

	return fmt.Sprintf("%s%s%d", varPrefix, TempVarMarker, count)
}

func (v *Visitor) getUniqueLiftedTypeName(typeName string) string {
	// Recover the original Go name by stripping BOTH sanitization markers ('@' and the Δ collision
	// rename) so the typeExists check below hits the real package scope (which holds the unsanitized
	// name). The lift is often called with the already-sanitized name (e.g. `Δtrace`).
	originalName := strings.TrimPrefix(removeLeadingSanitizationMarker(typeName), ShadowVarMarker)
	typeName = getSanitizedIdentifier(originalName)
	uniqueTypeName := typeName
	count := 0

	// typeExists looks names up in the Go package scope, which holds UNSANITIZED names. A lifted type
	// named after a global var that is Δ-renamed (e.g. a `var trace struct{…}` whose anon type lifts
	// to `trace`→`Δtrace`) would check the sanitized `Δtrace`, miss the `trace` var, and collide with
	// it (a nested type + a property both named `Δtrace`, CS0102). Also test the original name so the
	// first iteration forces a `ᴛ1` suffix in that case.
	for v.liftedTypeNameTaken(uniqueTypeName) || v.typeExists(uniqueTypeName) || (count == 0 && v.typeExists(originalName)) {
		count++
		uniqueTypeName = fmt.Sprintf("%s%s%d", typeName, TempVarMarker, count)
	}

	v.claimLiftedTypeName(uniqueTypeName)

	return uniqueTypeName
}

// liftedTypeNameTaken reports whether a lifted type name is already claimed. The scope is the whole
// PACKAGE — every lifted type nests in the one `<pkg>_package` class (see packageLiftedTypeNames) —
// plus, on the `-tests` internal variant, the production names that class already declares on disk.
// A hand-owned file's visitor sees only its own per-file claims: its emission lands in a
// non-compiled `.cs.auto` sibling, so it neither collides with nor constrains the real declarations.
func (v *Visitor) liftedTypeNameTaken(name string) bool {
	if v.liftedTypeNames.Contains(name) {
		return true
	}

	if v.manualConversion {
		return false
	}

	packageLock.Lock()
	defer packageLock.Unlock()

	return packageLiftedTypeNames.Contains(name) || productionLiftedTypeNames.Contains(name)
}

// claimLiftedTypeName records a lifted type name against this file and — unless the file is
// hand-owned — against the package (see liftedTypeNameTaken).
func (v *Visitor) claimLiftedTypeName(name string) {
	v.liftedTypeNames.Add(name)

	if v.manualConversion {
		return
	}

	packageLock.Lock()

	if packageLiftedTypeNames == nil {
		packageLiftedTypeNames = HashSet[string]{}
	}

	packageLiftedTypeNames.Add(name)
	packageLock.Unlock()
}

func (v *Visitor) liftedTypeExists(expr ast.Expr) bool {
	if expr == nil {
		return false
	}

	exprType := v.getType(expr, false)

	if exprType == nil {
		return false
	}

	if _, ok := v.liftedTypeMap[exprType]; ok {
		return true
	}

	if named, ok := exprType.(*types.Named); ok {
		if _, ok := v.liftedTypeMap[named.Underlying()]; ok {
			return true
		}
	}

	return false
}
