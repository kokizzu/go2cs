// internalTestDynamicTypeLifts.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

// The two halves of the INTERNAL-to-EXTERNAL test-variant carry for purely-anonymous lifts, each a
// named function rather than a block inside convertTestVariants/convertTestVariant, so the guard
// exercises THE CODE THE CONVERTER RUNS instead of a copy of it. A replica of a registry rule fails
// silently and in the flattering direction (C1 `a7c20e7cb`), and this rule is two cooperating
// halves across a state reset, which is exactly the shape a replica gets subtly wrong.
//
// See internalTestDynamicTypeNames (packageGlobalState.go) for the measured row and the full
// mechanism: Go 1.24 `time` declares `InternalTests` — a package-level slice of an anonymous struct
// — in the INTERNAL test file `abs_test.go`, and the EXTERNAL suite ranges over it. The internal
// variant lifts and publishes the element type; the external variant's resetPackageState clears the
// package registry, and its only seed is production's metadata, which cannot carry a type an
// internal `_test.go` declared. Without the carry the reference falls to a deferred marker, the
// marker resolves to raw Go, and the W2b gate fails the row.

// captureInternalTestDynamicTypeLifts snapshots the INTERNAL variant's package-level dynamic-type
// registry while its claims are still standing — the next variant's resetPackageState is what
// clears them, which is the same window convertTestVariants' whiteboxBridgeTypeNames union uses.
//
// Additive across calls by design: merging rather than replacing means a model that visits the
// bridge more than once cannot drop an earlier pass's claims.
func captureInternalTestDynamicTypeLifts() {
	packageLock.Lock()
	defer packageLock.Unlock()

	if internalTestDynamicTypeNames == nil {
		internalTestDynamicTypeNames = map[string]string{}
	}

	for signature, name := range packageDynamicTypeNames {
		internalTestDynamicTypeNames[signature] = name
	}
}

// seedInternalTestDynamicTypeLifts installs that snapshot into the EXTERNAL variant's
// production-lift registry, which is the map deferredDynamicTypeName consults after this visitor's
// own liftedTypeMap and the (freshly reset) package registry both miss.
//
// ⚠ PRODUCTION WINS. A signature production also publishes keeps PRODUCTION's name, so this can only
// ADD resolutions and never redirect one. Production's class is reachable from both variants; the
// internal bridge's is the narrower scope, and preferring the narrower one for a type production
// already named would be a silent behaviour change on every row that has both — which is most of
// them. The guard pins this direction rather than leaving it to call order.
//
// Called only for the external variant, and only after seedProductionDynamicTypeLifts has run.
func seedInternalTestDynamicTypeLifts() {
	packageLock.Lock()
	defer packageLock.Unlock()

	if len(internalTestDynamicTypeNames) == 0 {
		return
	}

	if productionDynamicTypeNames == nil {
		productionDynamicTypeNames = map[string]string{}
	}

	for signature, name := range internalTestDynamicTypeNames {
		if _, published := productionDynamicTypeNames[signature]; !published {
			productionDynamicTypeNames[signature] = name
		}
	}
}
