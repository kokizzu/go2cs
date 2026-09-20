// anonStructTypeArgLift.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import "go/ast"

// explicitCallTypeArgs returns the type-argument expressions a call WRITES at its Fun, or nil when
// the call has none. `F[A](x)` parses as *ast.IndexExpr and `F[A, B](x)` as *ast.IndexListExpr, so
// both spellings are peeled; parentheses around the callee are peeled with them.
//
// An ordinary index is the same syntax — `funcs[i](x)` is also an *ast.IndexExpr — so this cannot
// be used to decide that a call IS an instantiation. It is safe for the one job it has because the
// caller keeps only indices that are an anonymous struct LITERAL, and a `struct{…}` syntax node can
// never be a value in an index position.
func explicitCallTypeArgs(fun ast.Expr) []ast.Expr {
	for {
		paren, isParen := fun.(*ast.ParenExpr)

		if !isParen {
			break
		}

		fun = paren.X
	}

	switch indexed := fun.(type) {
	case *ast.IndexExpr:
		return []ast.Expr{indexed.Index}
	case *ast.IndexListExpr:
		return indexed.Indices
	}

	return nil
}

// liftExplicitAnonStructTypeArgs lifts — and PUBLISHES into the package-wide dedup registry — every
// non-empty anonymous struct written as an explicit type argument of this call.
//
// ⚠ WHY THIS EXISTS AT ALL, because the emission looks like the lift is missing and it is not.
// A type-argument position renders through getAliasQualifiedTypeName, whose anonymous-struct arm
// (deferredDynamicTypeName) resolves in three steps: this visitor's liftedTypeMap, the shared
// package registry, then a deferred marker. The first step is TYPE-IDENTITY keyed and the type
// reaching it comes from the resolved instance (info.Instances), which is not the same *types.Struct
// the lift recorded for the written syntax node — so it misses even for a lift minted moments
// earlier in the same function. That leaves the SIGNATURE-keyed registry as the only route, and
// visitStructType publishes into it only for a package-level lift or one at a call boundary
// (`!v.inFunction || v.liftAtCallBoundary`). A type argument is a position that gate never
// enumerated, so a function-scoped one minted its lift, declared it, and published nothing; the
// marker then resolved to the raw Go signature, which is never valid C#.
//
// MEASURED SHAPE (2026-09-20, the H10 pre-staging read of the reflect row at the 1.24.13 version
// tip): `reflect.TypeFor[struct{ f int }]()` at all_test.go:3547 — a NEW assertion inside the
// pre-existing TestAllocations — and at :6921 inside TestTypeFieldReadOnly, one of the release's
// six new reflect tests. The construct appears in no earlier release. It made the whole `reflect`
// row unreadable: `go2cs -tests -test-action all` exits 1 at CONVERT in BOTH build configurations,
// so the row produced zero verdicts rather than a divergence anyone could read.
//
// ⚠ THE TWO SITES SHARE ONE SIGNATURE AND THE RESOLVER WARNS ONCE PER SIGNATURE, so the failure
// named only the first — `1 unresolved dynamic type(s) … all_test.cs(4201)` while all_test.cs(8451)
// carried the same raw text unmentioned. Anything validating this against the reported line alone
// would read green with half the defect standing; the guard's fixture carries two sites for exactly
// that reason.
//
// The lift is named "type", which is the fallback convStructType already passes from this very
// position — so the C# name this publishes is the name the converter was ALREADY minting and
// declaring, and nothing about the emitted declaration moves. Only its publication is new.
//
// Bounds, stated rather than quietly assumed:
//
//   - The EMPTY anonymous struct is excluded by extractStructType and needs no lift: it maps to
//     golib's shared EmptyStruct, which is why `TypeFor[struct{}]()` resolved on the broken
//     converter and is the control that makes this one axis rather than three.
//   - A bare generic INSTANTIATION that is not called (`var f = typeFor[struct{ f int }]`) does not
//     reach here. No corpus source carries that spelling — the stdlib census at 1.24.13 found the
//     shape only in call position — so it is left unhandled rather than covered speculatively.
//   - The anonymous-INTERFACE twin is the same gate and the same three-step resolution, and it is
//     NOT addressed here because no measurement has produced it. Named so the next reader knows it
//     was considered and excluded, not overlooked.
func (v *Visitor) liftExplicitAnonStructTypeArgs(callExpr *ast.CallExpr) {
	if callExpr == nil || callExpr.Fun == nil {
		return
	}

	for _, typeArg := range explicitCallTypeArgs(callExpr.Fun) {
		structType, exprType := v.extractStructType(typeArg)

		if structType == nil || v.liftedTypeExists(structType) {
			continue
		}

		// The same toggle the call-ARGUMENT pre-visit uses (convCallExpr's argument classification):
		// a written type argument is externally significant across function scopes in exactly the
		// sense liftAtCallBoundary's doc comment describes, so the lift publishes and two functions
		// writing the identical shape unify on one C# type instead of minting one each.
		v.liftAtCallBoundary = true
		v.indentLevel++
		v.visitStructType(structType, exprType, "type", nil, true, nil)
		v.indentLevel--
		v.liftAtCallBoundary = false
	}
}
