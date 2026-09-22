// perUnitAliasRenames_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"go/types"
	"testing"
)

// The import-alias rename set describes ONE COMPILATION UNIT. A package's production `.cs`
// compiles into the production assembly, whose reference closure is the package's own; the
// converted `_test.go` variants compile into the test assembly, whose closure is the UNION with
// the test half. Applying the union to the production files Δ-renames a production alias against a
// collision only the test assembly has.
//
// regexp is the measured instance (2026-09-22, tree c6fdbe73c3): its production closure holds NO
// `io/*` path, its test closure reaches `os` and so `io/fs`, `regexp.csproj` does not reference
// `core/io/fs` while `regexp.tests.csproj` does — and a `-tests` run rewrote the tracked
// production `regexp.cs` from `using io = io_package;` to `using Δio = io_package;` plus five use
// sites. It compiles either way, so nothing failed; the tracked corpus simply changed under
// whichever driver ran last, and `Δio.RuneReader` stops reading like Go's `io.RuneReader`.
//
// The guard is red-first: with the fold ungated (the pre-fix behaviour) the production arm below
// renames and the test fails naming `io`.
func TestProductionUnitExcludesSiblingClosure(t *testing.T) {
	withCleanAliasRenameState(t)

	// regexp's shape: it imports io directly, and nothing in its own closure makes go.io a
	// namespace. The sibling (_test.go) half reaches io/fs, which does.
	io := types.NewPackage("io", "io")
	production := types.NewPackage("regexp", "regexp")
	production.SetImports([]*types.Package{io})

	const packageNS = RootNamespace

	// PRODUCTION unit: the sibling closure is present in the run's state but must NOT be folded.
	setShadowState(t, packageNS, nil)
	siblingClosureImportPaths = []string{"io/fs"}
	computeImportAliasRenames(nil, production, packageNS, "", "", false)

	if got, renamed := packageImportAliasRenames["io"]; renamed {
		t.Fatalf("the production unit renamed io to %q: io/fs is in the TEST assembly's closure, not the production assembly's, "+
			"so production must keep `using io = io_package;` (measured on regexp at c6fdbe73c3)", got)
	}

	if got := importQualifier("io"); got != "io" {
		t.Fatalf("every render site reads importQualifier: want %q, got %q", "io", got)
	}

	// TEST unit, same state: io/fs IS in this assembly's closure, so the rename is REQUIRED — a
	// bare `using io` inside namespace go collides with the go.io child namespace (CS0576).
	withCleanAliasRenameState(t)
	setShadowState(t, packageNS, nil)
	siblingClosureImportPaths = []string{"io/fs"}
	computeImportAliasRenames(nil, production, packageNS, "", "", true)

	if got, renamed := packageImportAliasRenames["io"]; !renamed || got != ShadowVarMarker+"io" {
		t.Fatalf("the test unit must keep the collision rename: want %q, got %q (renamed=%v)",
			ShadowVarMarker+"io", got, renamed)
	}
}

// The gate is on the FOLD, not on the package's own closure: a collision the production closure
// itself contributes is renamed in both units. Without this, "production passes false" could be
// mistaken for "production never renames".
func TestProductionUnitStillRenamesItsOwnCollisions(t *testing.T) {
	withCleanAliasRenameState(t)

	fs := types.NewPackage("io/fs", "fs")
	io := types.NewPackage("io", "io")
	production := types.NewPackage("regexp", "regexp")

	// The package's OWN closure reaches io/fs, so go.io is a namespace for the production assembly.
	production.SetImports([]*types.Package{io, fs})

	const packageNS = RootNamespace

	setShadowState(t, packageNS, nil)
	siblingClosureImportPaths = nil
	computeImportAliasRenames(nil, production, packageNS, "", "", false)

	if got, renamed := packageImportAliasRenames["io"]; !renamed || got != ShadowVarMarker+"io" {
		t.Fatalf("io/fs is in the PRODUCTION closure here, so the production unit must rename io: want %q, got %q (renamed=%v)",
			ShadowVarMarker+"io", got, renamed)
	}
}
