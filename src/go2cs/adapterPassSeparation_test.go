// adapterPassSeparation_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

// THE PASS SEPARATION. Under the recompile model the production `.cs` files are COMPILE ITEMS of
// the test assembly, so go2cs-gen reads the union of both halves' GoImplement records -- but the
// production text was rendered in the production pass against the production set alone and cannot
// be re-rendered. A test-half record joining a production record's collision group therefore must
// not rename the production member. crypto/sha3 is the corpus instance: production records
// <SHA3, hash.Hash>, the external test half adds <SHA3, fips140.Hash>, both compose `SHA3жHash`,
// and the ordinary rule prefixes BOTH -- leaving sha3.cs's four already-written `SHA3жHash` sites
// naming a class the generator no longer emits.
//
// The halves are told apart by the `Production = true` facet the recompile SEED stamps on, and the
// four rules of adapterNameCollisions.go are armed one per test below. Arms 1-3 are the rule; arms
// 4-5 are its SCOPE, which carries as much weight: rule 4 (no facet anywhere) is every production
// conversion and both reference models standing still, and it is what makes this byte-neutral for
// 337 of the corpus's 342 .csproj-carrying packages.
//
// ⚠ Every arm here is a converter-side READ. The generator half of the pair lives in
// src/gen/go2cs-gen and is gated on a hardware lane; nothing in this file compiles C#.

package main

import (
	"strings"
	"testing"
)

const (
	// crypto/sha3's real shapes, as emitted: the production record (seeded, then faceted) and the
	// external test half's addition. Both interfaces are FOREIGN, which is what makes the ordinary
	// rule prefix both and lose the production name.
	passSepProductionRecord = "[assembly: GoImplement<SHA3, hash_package.Hash>(Pointer = true, Production = true)]"
	passSepTestRecord       = "[assembly: GoImplement<SHA3, go.crypto.internal.fips140_package.Hash>(Pointer = true)]"

	passSepStructRef       = "SHA3"
	passSepProductionIface = "hash_package.Hash"
	passSepTestIface       = "go.crypto.internal.fips140_package.Hash"
)

// recordPassSeparationPairs parses the given record lines into the package-scope capture and
// restores the previous capture afterwards -- both halves of that state are what the naming sites
// read, and a test that left either behind would change the next test's answer.
func recordPassSeparationPairs(t *testing.T, lines ...string) [][2]string {
	t.Helper()

	packageLock.Lock()
	savedPairs, savedFacets := emittedPointerAdapterPairs, productionFacetedAdapterPairs
	packageLock.Unlock()

	t.Cleanup(func() {
		packageLock.Lock()
		emittedPointerAdapterPairs, productionFacetedAdapterPairs = savedPairs, savedFacets
		packageLock.Unlock()
	})

	recordEmittedPointerAdapterPairs(lines)

	packageLock.Lock()
	defer packageLock.Unlock()

	return emittedPointerAdapterPairs
}

// ARM 1. The faceted record must SURVIVE THE PARSE and be marked. This is the arm that fails most
// quietly without the fix: recordEmittedPointerAdapterPairs tested one exact suffix, so a faceted
// record was not a pointer record at all -- it fell out of the pair set, no group collided, and
// every test-half cast took the bare name the production half owns. A missing record reads as "no
// collision", which is indistinguishable from correct until the C# compiler is reached.
func TestAFacetedRecordIsParsedAndMarkedProduction(t *testing.T) {
	pairs := recordPassSeparationPairs(t, passSepProductionRecord, passSepTestRecord)

	if len(pairs) != 2 {
		t.Fatalf("expected BOTH records parsed as pointer pairs, got %d: %v", len(pairs), pairs)
	}

	production, test := pairs[0], pairs[1]

	if production[0] != passSepStructRef || production[1] != passSepProductionIface {
		t.Fatalf("production pair parsed as %v, want [%s %s]", production, passSepStructRef, passSepProductionIface)
	}

	if test[0] != passSepStructRef || test[1] != passSepTestIface {
		t.Fatalf("test pair parsed as %v, want [%s %s]", test, passSepStructRef, passSepTestIface)
	}

	if !adapterPairIsProductionFaceted(production) {
		t.Errorf("the seeded production record is not marked faceted")
	}

	if adapterPairIsProductionFaceted(test) {
		t.Errorf("the test-half record is marked faceted -- the two halves are no longer separable")
	}
}

// ARM 2, THE RULE ITSELF (rule 2), at crypto/sha3's exact shape. The group collides over the union
// and has exactly one faceted member: that member keeps `SHA3жHash` -- what sha3.cs already spells
// -- and only the unfaceted member takes the prefix.
func TestTheSoleFacetedMemberKeepsTheProductionName(t *testing.T) {
	pairs := recordPassSeparationPairs(t, passSepProductionRecord, passSepTestRecord)
	colliding := adapterNameCollisionSet(pairs)
	keepers := adapterProductionNameKeepers(pairs, colliding)

	groupKey := adapterGroupKey(passSepStructRef, passSepProductionIface)

	if !colliding[groupKey] {
		t.Fatalf("the union must still SEE the collision -- the facet decides naming, never grouping (key %q)", groupKey)
	}

	if !keepers[groupKey] {
		t.Fatalf("group %q has one faceted member and is not a keeper", groupKey)
	}

	// The production half, as a test-half cast onto that same pair would resolve it.
	if got := adapterResolvedName(passSepStructRef, passSepProductionIface, colliding, true); got != "SHA3"+PointerPrefix+"Hash" {
		t.Errorf("production pair resolved %q, want %q -- sha3.cs's four sites already spell the latter", got, "SHA3"+PointerPrefix+"Hash")
	}

	// The test half takes the prefix, which is the name the (A) seat's emission read predicted.
	if got := adapterResolvedName(passSepStructRef, passSepTestIface, colliding, false); got != "SHA3"+PointerPrefix+"fips140_Hash" {
		t.Errorf("test pair resolved %q, want %q", got, "SHA3"+PointerPrefix+"fips140_Hash")
	}

	// And the anchored sibling answers identically, so the white-box model cannot drift from the
	// recompile model on the one rule both would apply.
	if got := anchoredAdapterMemberName([2]string{passSepStructRef, passSepProductionIface}, colliding, true); got != "SHA3"+PointerPrefix+"Hash" {
		t.Errorf("anchored production member composed %q, want %q", got, "SHA3"+PointerPrefix+"Hash")
	}
}

// ARM 3, RULE 3 -- the negative that keeps the rule from over-firing. TWO faceted members is a
// collision the PRODUCTION pass already saw and already resolved, so the production text spells the
// PREFIXED names and the ordinary rule is what reproduces them. Exempting one would rename a
// production site in the opposite direction: the same defect, mirrored.
func TestTwoFacetedMembersAreNotSeparated(t *testing.T) {
	pairs := recordPassSeparationPairs(t,
		passSepProductionRecord,
		"[assembly: GoImplement<SHA3, go.crypto.internal.fips140_package.Hash>(Pointer = true, Production = true)]")

	colliding := adapterNameCollisionSet(pairs)
	keepers := adapterProductionNameKeepers(pairs, colliding)
	groupKey := adapterGroupKey(passSepStructRef, passSepProductionIface)

	if !colliding[groupKey] {
		t.Fatalf("two records over one name is still a collision (key %q)", groupKey)
	}

	if keepers[groupKey] {
		t.Fatalf("group %q has TWO faceted members and must NOT be a keeper -- rule 3", groupKey)
	}
}

// ARM 4, RULE 4 AND THE SCOPE. With NO faceted record the answers must be exactly what they were
// before this seat existed -- that is every production conversion and both reference models, and it
// is why the corpus cannot move. compress/flate's shape is the control the (A) seat also used.
func TestAnUnfacetedGroupIsTheOrdinaryRule(t *testing.T) {
	pairs := recordPassSeparationPairs(t,
		"[assembly: GoImplement<bufio_package.Reader, Reader>(Pointer = true)]",
		"[assembly: GoImplement<bufio_package.Reader, io_package.Reader>(Pointer = true)]")

	colliding := adapterNameCollisionSet(pairs)
	keepers := adapterProductionNameKeepers(pairs, colliding)

	if len(keepers) != 0 {
		t.Fatalf("no record carries the facet, so no group can be a keeper: %v", keepers)
	}

	// The LOCAL interface stays bare and the FOREIGN one takes its prefix -- the pre-seat rule.
	if got := adapterResolvedName("bufio_Reader", "Reader", colliding, false); got != "bufio_Reader"+PointerPrefix+"Reader" {
		t.Errorf("local member resolved %q, want %q", got, "bufio_Reader"+PointerPrefix+"Reader")
	}

	if got := adapterResolvedName("bufio_Reader", "io_package.Reader", colliding, false); got != "bufio_Reader"+PointerPrefix+"io_Reader" {
		t.Errorf("foreign member resolved %q, want %q", got, "bufio_Reader"+PointerPrefix+"io_Reader")
	}
}

// ARM 5, THE SEED STAMP. The seed is the one file in the `-tests` flow copied byte-for-byte, so the
// stamp must carry every line ending through untouched, must be idempotent, and must not touch a
// value-form, Promoted or ConstraintProxy record -- none of which the generator's pointer path even
// reads.
func TestTheSeedStampIsExactIdempotentAndEndingPreserving(t *testing.T) {
	seed := "// a comment\r\n" +
		"[assembly: GoImplement<SHA3, hash_package.Hash>(Pointer = true)]\r\n" +
		"[assembly: GoImplement<LocalKey, fmt_package.Stringer>]\r\n" +
		"[assembly: GoImplement<IntHeap, Interface>(Promoted = true)]\r\n" +
		"[assembly: GoImplement<P224Point, nistPoint<P224Point>>(ConstraintProxy = true)]\r\n"

	stamped := string(facetProductionPointerRecords([]byte(seed)))

	if want := strings.Count(seed, "\r\n"); strings.Count(stamped, "\r\n") != want || strings.Count(stamped, "\n") != want {
		t.Fatalf("line endings moved: %d CRLF / %d LF, want %d / %d", strings.Count(stamped, "\r\n"), strings.Count(stamped, "\n"), want, want)
	}

	if !strings.Contains(stamped, passSepProductionRecord) {
		t.Errorf("the pointer record was not faceted:\n%s", stamped)
	}

	for _, untouched := range []string{
		"[assembly: GoImplement<LocalKey, fmt_package.Stringer>]",
		"[assembly: GoImplement<IntHeap, Interface>(Promoted = true)]",
		"[assembly: GoImplement<P224Point, nistPoint<P224Point>>(ConstraintProxy = true)]",
	} {
		if !strings.Contains(stamped, untouched) {
			t.Errorf("a non-pointer record was rewritten; %q is gone:\n%s", untouched, stamped)
		}
	}

	if again := string(facetProductionPointerRecords([]byte(stamped))); again != stamped {
		t.Errorf("the stamp is not idempotent:\n%s", again)
	}
}

// ARM 6, THE MERGE DEDUPE. A test variant re-derives a pair the production half also recorded
// whenever both halves cast the same struct to the same interface. The writer's HashSet dedupes
// identical TEXT and these two differ by the facet alone, so without the skip the file carries TWO
// records for ONE pair -- which makes go2cs-gen compose the adapter twice AND, measured here,
// makes the keeper count reach two and fall to rule 3, un-separating the very pass the facet
// separates. Both halves of that are armed: the relation the writer tests, and the consequence.
func TestADuplicateFacetedPairUnseparatesThePass(t *testing.T) {
	fresh := "[assembly: GoImplement<SHA3, hash_package.Hash>(Pointer = true)]"

	if got := pointerRecordFacetedForm(fresh); got != passSepProductionRecord {
		t.Fatalf("the faceted twin of the fresh record composed %q, want %q", got, passSepProductionRecord)
	}

	if got := pointerRecordFacetedForm(passSepProductionRecord); got != passSepProductionRecord {
		t.Errorf("an already-faceted record must come back unchanged, got %q", got)
	}

	if got := pointerRecordFacetedForm("[assembly: GoImplement<LocalKey, fmt_package.Stringer>]"); got != "[assembly: GoImplement<LocalKey, fmt_package.Stringer>]" {
		t.Errorf("a value-form record must come back unchanged, got %q", got)
	}

	// The consequence, if the writer ever stopped skipping: one pair, two spellings.
	pairs := recordPassSeparationPairs(t, passSepProductionRecord, fresh, passSepTestRecord)
	colliding := adapterNameCollisionSet(pairs)
	keepers := adapterProductionNameKeepers(pairs, colliding)

	if keepers[adapterGroupKey(passSepStructRef, passSepProductionIface)] {
		t.Errorf("a duplicated pair left the group separable -- then this arm no longer measures why the writer skips")
	}
}

// ARM 7, THE DEGENERATE CASE IS LOUD. Rule 2 tells the unfaceted members to take the interface
// prefix, and a LOCAL interface's prefix is the empty string -- so a group whose faceted member
// keeps the bare name and whose unfaceted member is local composes ONE name twice. No corpus
// package reaches it (every member of every recompile-model collision group today has a foreign
// interface), so the shape is NAMED rather than machinery built for it: the package that reaches it
// first must arrive with its own name in a warning, not as an unexplained duplicate class.
func TestALocalUnfacetedMemberIsWarnedNotSilentlyDuplicated(t *testing.T) {
	pairs := recordPassSeparationPairs(t,
		passSepProductionRecord,
		"[assembly: GoImplement<SHA3, Hash>(Pointer = true)]")

	colliding := adapterNameCollisionSet(pairs)

	var keepers map[string]bool
	stderr := captureStderr(t, func() { keepers = adapterProductionNameKeepers(pairs, colliding) })

	if !keepers[adapterGroupKey(passSepStructRef, passSepProductionIface)] {
		t.Fatalf("the rule still applies -- the group has one faceted member")
	}

	if !strings.Contains(stderr, "cannot be separated by pass") {
		t.Errorf("no warning named the unseparable group; stderr was:\n%s", stderr)
	}

	if !strings.Contains(stderr, "SHA3"+PointerPrefix+"Hash") {
		t.Errorf("the warning must name the adapter NAME that would be composed twice; stderr was:\n%s", stderr)
	}
}
