// adapterInterfaceStrip_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import "testing"

// The interface-side twin of the struct-side collision-key rule: a CLOSED generic argument list is
// dropped BEFORE any last-dot scan, at the sites that compose a NAME, and NOWHERE else.
//
// Four of these arms are the stopped i7 sub-agent's, adopted rather than re-derived (COORD
// 938bb886f relays the shapes; its file was deleted). The fifth is this lane's own: the PAIRING
// lookup, which is what actually gates the anchored branch and which no arm covered.
//
// ⚠ THE PARITY ARM IS THE ONE THAT MATTERS MOST TO A LATER READER: the collision KEYS garble a
// generic interface reference on purpose, because the generator's keys garble identically. An arm
// that asserts the garbled value is what stops the asymmetry being tidied away by someone who reads
// the strip above it and assumes it was simply missed here.

const (
	stripStructRef   = "probe_package.digest"
	stripIfaceLocal  = "keyedLike<named>"
	stripIfaceQual   = "keyedLike<other_package.named>"
	stripIfaceNested = "outer<inner<x>>"
)

// TestAdapterInterfaceStripAtTheResolvers is the sub-agent's arm (1): both name-composing resolvers
// render the interface's BARE name for a record naming a closed generic instantiation.
//
// adapterResolvedName reaches it through the deferred marker, which adapterTypeRef already strips;
// anchoredAdapterMemberName does NOT, because its name comes from the emitted GoImplement record —
// which is the one site the marker's strip cannot reach and the reason this file exists.
func TestAdapterInterfaceStripAtTheResolvers(t *testing.T) {
	colliding := map[string]bool{}

	if got := adapterResolvedName("digest", stripIfaceLocal, colliding); got != "digest"+PointerPrefix+"keyedLike" {
		t.Errorf("adapterResolvedName = %s, want digest%skeyedLike — the argument list must not reach the identifier", got, PointerPrefix)
	}

	pair := [2]string{"probe_package.digest", stripIfaceLocal}

	if got := anchoredAdapterMemberName(pair, colliding); got != "probe_digest"+PointerPrefix+"keyedLike" {
		t.Errorf("anchoredAdapterMemberName = %s, want probe_digest%skeyedLike — unstripped it names the ARGUMENT", got, PointerPrefix)
	}
}

// TestAdapterInterfaceStripNested is the sub-agent's arm (2): a NESTED generic reduces to the outer
// name, not to the innermost argument's tail. Unstripped, the last-dot scan leaves `x>>`.
func TestAdapterInterfaceStripNested(t *testing.T) {
	if got := stripAdapterInterfaceTypeArgs(stripIfaceNested); got != "outer" {
		t.Errorf("stripAdapterInterfaceTypeArgs(%s) = %s, want outer", stripIfaceNested, got)
	}

	if got := adapterResolvedName("digest", stripIfaceNested, map[string]bool{}); got != "digest"+PointerPrefix+"outer" {
		t.Errorf("adapterResolvedName over a nested generic = %s, want digest%souter", got, PointerPrefix)
	}
}

// TestAdapterInterfaceStripQualifierComesFromTheInterface is the sub-agent's arm (3), and it is a
// SECOND defect rather than a tidiness: the collision qualifier must be derived from the INTERFACE,
// which is what the generator does. Unstripped, adapterInterfacePackagePrefix reads its prefix out
// of the ARGUMENT's package — `keyedLike<other_package.named>` yielding `other_`.
func TestAdapterInterfaceStripQualifierComesFromTheInterface(t *testing.T) {
	pair := [2]string{stripStructRef, stripIfaceQual}
	colliding := map[string]bool{adapterGroupKey(pair[0], pair[1]): true}

	got := anchoredAdapterMemberName(pair, colliding)

	if got != "probe_digest"+PointerPrefix+"keyedLike" {
		t.Errorf("anchoredAdapterMemberName under collision = %s, want probe_digest%skeyedLike — the qualifier may not come from the ARGUMENT's package", got, PointerPrefix)
	}
}

// TestAdapterInterfaceKeysStillGarbleOnPurpose is the sub-agent's arm (4), the PARITY CONTROL.
//
// ⚠ IT ASSERTS THE WRONG-LOOKING VALUE DELIBERATELY. The collision KEYS reduce a generic interface
// reference by the last dot alone, which for a QUALIFIED argument leaves the argument's tail — and
// the generator's keys do exactly the same. The two halves must garble ALIKE: stripping here and not
// there would manufacture the divergence the struct-side rule removes. If this arm ever fails
// because someone "fixed" the helper, the generator's matching key has to move in the same commit.
func TestAdapterInterfaceKeysStillGarbleOnPurpose(t *testing.T) {
	if got := adapterInterfaceSimpleName(stripIfaceQual); got != "named>" {
		t.Errorf("adapterInterfaceSimpleName(%s) = %s, want named> — the KEYS garble on purpose, in step with the generator", stripIfaceQual, got)
	}

	if got := adapterInterfaceSimpleName(stripIfaceLocal); got != stripIfaceLocal {
		t.Errorf("adapterInterfaceSimpleName(%s) = %s, want it unchanged — a DOTLESS argument leaves the last-dot scan a no-op", stripIfaceLocal, got)
	}
}

// TestEmittedAdapterPairMatchesAcrossTheTwoSpellings is THIS LANE's arm, and it is the one that
// gates everything above it: the anchored branch runs only when emittedAdapterPair finds the record.
//
// ⚠ The two sides arrive in DIFFERENT spellings — the marker's name is already stripped by
// adapterTypeRef, the record's is the closed instantiation — so a strip on one side alone makes the
// lookup MISS. The branch is then skipped silently, the cast site takes the bare resolved name, and
// it loses the anchor class it needed: the right identifier, unqualified, CS0246 under the white-box
// model. That is a failure no NAME arm above can see.
func TestEmittedAdapterPairMatchesAcrossTheTwoSpellings(t *testing.T) {
	pairs := [][2]string{{stripStructRef, stripIfaceLocal}}

	// The marker's side: what adapterTypeRef wrote, already stripped.
	if _, ok := emittedAdapterPair(pairs, stripStructRef, "keyedLike"); !ok {
		t.Fatal("a STRIPPED marker name did not match the record's CLOSED one — the anchored branch is skipped and the cast site loses its anchor class")
	}

	// And the unstripped form still matches, so nothing that already worked stops working.
	if _, ok := emittedAdapterPair(pairs, stripStructRef, stripIfaceLocal); !ok {
		t.Error("the closed form no longer matches itself")
	}

	// A DIFFERENT interface must still miss — otherwise the arm above passes for the wrong reason.
	if _, ok := emittedAdapterPair(pairs, stripStructRef, "somethingElse"); ok {
		t.Error("an unrelated interface matched; the comparison has stopped discriminating")
	}
}
