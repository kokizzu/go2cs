// adapterStructLocality_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

// The STRUCT-side locality rule of the collision key, and its SCOPE. The generator decides bare
// versus `<pkg>_<Simple>` by resolving the SYMBOL (AdapterStructKey's `container ==
// packageClassName`) and its own doc states the contract: "the two must agree or the collision
// groups diverge". The converter decides it from a SPELLING, and one spelling used to escape it.
//
// ⚠ THE SCOPE ARMS CARRY AS MUCH WEIGHT AS THE POSITIVE. A rule that called every dotted
// qualifier local would key compress/flate's foreign `flate_package.Writer` bare and rename a
// validated package's adapters for nothing. testLocalTypePrefixes is what bounds it, and it is
// populated ONLY under the recompile model -- both reference models clear testPackageName so the
// package under test binds as an ordinary import -- so arms 3 and 4 below are the reference
// models' emission standing still.

package main

import "testing"

const (
	localityLocalPrefix = "go.crypto.sha3_package"
	localityAliasRef    = "sha3.SHA3"
	localityClassRef    = "sha3_package.SHA3"
	localityForeignRef  = "hash_package.Hash"
)

// withLocalPrefixes runs fn with testLocalTypePrefixes set, restoring it after -- the variable is
// package-scoped state the real conversion sets per variant.
func withLocalPrefixes(t *testing.T, prefixes []string, fn func()) {
	t.Helper()

	saved := testLocalTypePrefixes
	testLocalTypePrefixes = prefixes

	defer func() { testLocalTypePrefixes = saved }()

	fn()
}

// TestAdapterStructKeyTreatsThePackageUnderTestAliasAsLocal is the defect. An external `<pkg>_test`
// variant reaches the package under test through the using alias the converter minted for it
// (`using sha3 = go.crypto.sha3_package;`), so a cast site spells the struct `sha3.SHA3`. The
// qualifier carries no PackageSuffix, so the key composed the FOREIGN `sha3_SHA3` while the
// generator composed bare `SHA3` -- the group split and the cast took an unprefixed name for a
// class that is never emitted (crypto/sha3: CS0246/CS0426).
func TestAdapterStructKeyTreatsThePackageUnderTestAliasAsLocal(t *testing.T) {
	withLocalPrefixes(t, []string{localityLocalPrefix}, func() {
		if got := adapterStructKey(localityAliasRef); got != "SHA3" {
			t.Errorf("alias-qualified local struct keyed %q, want the bare local name %q", got, "SHA3")
		}

		// The RECORD's own spelling names the class whole and must reach the same key, or the two
		// halves of one pair group apart for a different reason than the one just fixed.
		if got := adapterStructKey(localityClassRef); got != "SHA3" {
			t.Errorf("class-qualified local struct keyed %q, want the bare local name %q", got, "SHA3")
		}

		// And a BARE record spelling -- what stripLocalTypeQualifier actually writes -- is already
		// the key; it is the value the other two must agree with.
		if got := adapterStructKey("SHA3"); got != "SHA3" {
			t.Errorf("bare local struct keyed %q, want %q", got, "SHA3")
		}
	})
}

// TestAdapterStructKeyKeepsForeignQualifiersForeign is the SCOPE arm inside the recompile model: a
// qualifier that is not the local package class still composes `<pkg>_<Simple>`.
func TestAdapterStructKeyKeepsForeignQualifiersForeign(t *testing.T) {
	withLocalPrefixes(t, []string{localityLocalPrefix}, func() {
		for ref, want := range map[string]string{
			localityForeignRef:     "hash_Hash",
			"bytes_package.Buffer": "bytes_Buffer",
			"go.os_package.File":   "os_File",
			"bufio_package.Reader": "bufio_Reader",
		} {
			if got := adapterStructKey(ref); got != want {
				t.Errorf("foreign struct %q keyed %q, want %q", ref, got, want)
			}
		}
	})
}

// TestAdapterStructKeyIsForeignUnderTheReferenceModels is the SCOPE arm that keeps compress/flate
// still. Both reference models clear testPackageName, so testLocalTypePrefixes is empty, the
// package under test IS a separate assembly, and even the alias spelling must stay foreign.
func TestAdapterStructKeyIsForeignUnderTheReferenceModels(t *testing.T) {
	withLocalPrefixes(t, nil, func() {
		if got := adapterStructKey(localityAliasRef); got != "sha3_SHA3" {
			t.Errorf("with no local prefixes the alias spelling keyed %q, want the foreign %q", got, "sha3_SHA3")
		}

		// flate's own Writer, recorded fully qualified, is the corpus instance of this arm.
		if got := adapterStructKey("global::go.compress.flate_package.Writer"); got != "flate_Writer" {
			t.Errorf("flate's own Writer keyed %q under a reference model, want %q", got, "flate_Writer")
		}
	})
}

// TestAdapterGroupKeyCollidesAcrossTheTwoSpellings is the END of the defect rather than its
// mechanism: the cast site's key and the record's key must land in ONE group, so the collision set
// the records build is the set the cast site looks itself up in. crypto/sha3's SHA3 reaches
// hash.Hash and fips140.Hash; both compose `SHA3жHash`, so the group collides and both members
// take their interface prefix.
func TestAdapterGroupKeyCollidesAcrossTheTwoSpellings(t *testing.T) {
	withLocalPrefixes(t, []string{localityLocalPrefix}, func() {
		records := [][2]string{
			{"SHA3", "hash_package.Hash"},
			{"SHA3", "go.crypto.internal.fips140_package.Hash"},
		}

		colliding := adapterNameCollisionSet(records)
		recordKey := adapterGroupKey(records[0][0], records[0][1])

		if !colliding[recordKey] {
			t.Fatalf("the two Hash records did not collide on %q", recordKey)
		}

		// THE POINT: the cast site's own spelling must key into that same group.
		castKey := adapterGroupKey("sha3.SHA3", "go.crypto.internal.fips140_package.Hash")

		if castKey != recordKey {
			t.Fatalf("cast site keyed %q, records keyed %q -- the group is split", castKey, recordKey)
		}

		if !colliding[castKey] {
			t.Errorf("cast site key %q missed the collision set built from the records", castKey)
		}

		// And the resolved name therefore carries the interface prefix the generator emits.
		if got := adapterResolvedName("sha3.SHA3", "go.crypto.internal.fips140_package.Hash", colliding); got != "sha3.SHA3"+PointerPrefix+"fips140_Hash" {
			t.Errorf("resolved %q, want the prefixed %q", got, "sha3.SHA3"+PointerPrefix+"fips140_Hash")
		}
	})
}
