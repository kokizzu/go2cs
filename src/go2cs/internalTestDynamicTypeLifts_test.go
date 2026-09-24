// internalTestDynamicTypeLifts_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

// Guards the INTERNAL-to-EXTERNAL test-variant carry for purely-anonymous lifts.
//
// The measured row is Go 1.24's `time`, one of exactly two CONVERT failures across the 228-row H10
// population (C2's roster-wide pre-flight, 2026-09-20). `abs_test.go` — an INTERNAL test file,
// `package time` — declares at package level:
//
//	var InternalTests = []struct{ Name string; Test func(testingT) }{ … }
//
// and `time_test.go` — the EXTERNAL suite, `package time_test` — ranges over it. The internal
// variant lifts the element type to `InternalTestsᴛ1`, declares it in the bridge class and records
// it in package_info_internal_test.cs. The external variant then starts with a reset package
// registry and a production seed that CANNOT carry it (measured: `time`'s six per-GOOS
// package_info.cs files carry ZERO GoDynamicTypeLift records), so the reference fell to a deferred
// marker, the marker resolved to the raw Go signature at time_test.cs(33), and the W2b gate failed
// the whole row.
//
// ⚠ These tests call the CONVERTER'S OWN functions — captureInternalTestDynamicTypeLifts and
// seedInternalTestDynamicTypeLifts — and not a re-implementation of what they do. That is the whole
// reason the two halves are named functions: a replica of a registry rule goes green while the rule
// it copies has drifted, and this rule is two cooperating halves across a state reset, which is
// precisely the shape a replica gets wrong.

package main

import "testing"

// timeInternalTestsSignature is the element type of `time`'s InternalTests, verbatim as
// go/types renders it and as the failing emission reported it.
const timeInternalTestsSignature = "struct{Name string; Test func(time.testingT)}"

// withInternalTestLiftCarry isolates BOTH registries this carry spans — the per-variant package
// registry and the cross-variant snapshot — and restores them, so neighbouring tests in this
// package are unaffected by either.
func withInternalTestLiftCarry(t *testing.T, body func()) {
	t.Helper()

	savedPackage := packageDynamicTypeNames
	savedProduction := productionDynamicTypeNames
	savedInternal := internalTestDynamicTypeNames

	packageDynamicTypeNames = map[string]string{}
	productionDynamicTypeNames = nil
	internalTestDynamicTypeNames = map[string]string{}

	defer func() {
		packageDynamicTypeNames = savedPackage
		productionDynamicTypeNames = savedProduction
		internalTestDynamicTypeNames = savedInternal
	}()

	body()
}

// TestInternalTestLiftSurvivesTheVariantReset is the guard. The reset between the two calls is the
// defect's whole mechanism, so it is reproduced literally: resetPackageState replaces
// packageDynamicTypeNames with a fresh map, and everything the internal variant published is gone.
func TestInternalTestLiftSurvivesTheVariantReset(t *testing.T) {
	withInternalTestLiftCarry(t, func() {
		// The INTERNAL variant lifts and publishes the element type.
		packageDynamicTypeNames[timeInternalTestsSignature] = "InternalTestsᴛ1"
		captureInternalTestDynamicTypeLifts()

		// resetPackageState, at the external variant's start.
		packageDynamicTypeNames = map[string]string{}
		productionDynamicTypeNames = nil

		// ⚠ The state the defect leaves: both lookups the resolver uses come back empty, which is
		// what sends the reference to a deferred marker. Asserted so the test proves the reset is
		// doing what the real one does — without it the rest of this test could pass vacuously.
		if name := lookupDynamicTypeName(timeInternalTestsSignature); name != "" {
			t.Fatalf("the package registry survived the reset (%q); this test is not reproducing the defect", name)
		}

		if name := lookupProductionDynamicTypeName(timeInternalTestsSignature); name != "" {
			t.Fatalf("production already publishes %q; the carry cannot be what makes this resolve", name)
		}

		// seedProductionDynamicTypeLifts would run here and find nothing: production's metadata
		// carries no record for a type an internal `_test.go` declared.
		seedInternalTestDynamicTypeLifts()

		if name := lookupProductionDynamicTypeName(timeInternalTestsSignature); name != "InternalTestsᴛ1" {
			t.Errorf("after the carry the external variant resolves %q, want %q -- the reference "+
				"falls to a deferred marker and the W2b gate fails the row",
				name, "InternalTestsᴛ1")
		}
	})
}

// TestProductionLiftWinsOverTheInternalTestLift pins the DIRECTION. Production's class is reachable
// from both variants; the internal bridge's is the narrower scope, so a signature production
// already publishes must keep production's name. Left to call order this would be a silent
// behaviour change on every row that has both — which is most of them.
func TestProductionLiftWinsOverTheInternalTestLift(t *testing.T) {
	withInternalTestLiftCarry(t, func() {
		packageDynamicTypeNames[timeInternalTestsSignature] = "bridgeLiftᴛ1"
		captureInternalTestDynamicTypeLifts()

		packageDynamicTypeNames = map[string]string{}
		productionDynamicTypeNames = map[string]string{timeInternalTestsSignature: "productionLiftᴛ1"}

		seedInternalTestDynamicTypeLifts()

		if name := lookupProductionDynamicTypeName(timeInternalTestsSignature); name != "productionLiftᴛ1" {
			t.Errorf("the carry overwrote production's own lift with %q; production must win", name)
		}
	})
}

// TestInternalTestLiftCarryIsInertWithoutAnInternalVariant is the negative control: a package with
// no internal `_test.go` publishes nothing here, so the seed must leave the registry exactly as
// seedProductionDynamicTypeLifts left it — including leaving it NIL, which is the state
// productionDynamicTypeNames documents for a variant with nothing to inherit.
func TestInternalTestLiftCarryIsInertWithoutAnInternalVariant(t *testing.T) {
	withInternalTestLiftCarry(t, func() {
		seedInternalTestDynamicTypeLifts()

		if productionDynamicTypeNames != nil {
			t.Errorf("the carry allocated a registry for a package with no internal test variant: %v",
				productionDynamicTypeNames)
		}
	})
}

// TestInternalTestLiftCaptureIsAdditive pins that a second internal pass cannot drop an earlier
// claim — the capture merges rather than replaces, and a model that visits the bridge twice must
// not lose the first visit's lifts.
func TestInternalTestLiftCaptureIsAdditive(t *testing.T) {
	withInternalTestLiftCarry(t, func() {
		packageDynamicTypeNames = map[string]string{"struct{a int}": "firstᴛ1"}
		captureInternalTestDynamicTypeLifts()

		packageDynamicTypeNames = map[string]string{"struct{b int}": "secondᴛ1"}
		captureInternalTestDynamicTypeLifts()

		packageDynamicTypeNames = map[string]string{}
		productionDynamicTypeNames = nil
		seedInternalTestDynamicTypeLifts()

		for signature, want := range map[string]string{"struct{a int}": "firstᴛ1", "struct{b int}": "secondᴛ1"} {
			if name := lookupProductionDynamicTypeName(signature); name != want {
				t.Errorf("signature %q resolved %q, want %q -- the capture replaced rather than merged",
					signature, name, want)
			}
		}
	})
}
