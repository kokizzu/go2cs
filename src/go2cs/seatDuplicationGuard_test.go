// seatDuplicationGuard_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"os/exec"
	"strings"
	"testing"
)

// THE GUARD FOR src/seat-duplication-census.sh.
//
// The instrument it drives exists because ANCESTRY CANNOT SEE A CHERRY-PICK: a lane that develops an
// item on its own branch and cherry-picks it onto a seat gives the same content a new SHA, so
// `merge-base --is-ancestor` reads clean on both seats while the diff is present on both. Measured
// 2026-09-13 (coordinator ruling, mailbox bcada15ae): two of C1's seat branches each carried three
// items already boarding elsewhere, and the contamination reached an ACCEPTANCE RUN -- a tree carrying
// an unrelated seat was read as evidence about the seat under test, and a race was inferred from it.
//
// This guard drives the script's own --self-test, which builds a hermetic repo (no network, no clone,
// nothing outside a temp dir) and runs its arms RED-FIRST. It asserts the verdict line, the ARM COUNT
// and each arm's REASON -- the count because an exit code cannot tell a suite that ran every arm from
// one that silently lost three, and the reasons because an arm that keeps its name and loses its
// meaning is the failure this file's older sibling (safePushGuard_test.go) was written about. That is
// not hypothetical here: arm 6 asserted only its exit code until a negative control on arm 7 made it
// misreport a stack as a cherry-pick while still exiting 1, which it passed.
//
// It reuses safePushBash() rather than re-deriving the interpreter: one definition of "which bash can
// drive a fleet script here", so the two guards cannot disagree about a host.
func TestSeatDuplicationCensusSelfTest(t *testing.T) {
	// A POSIX spelling, not filepath.Join: the argument is read by bash, not by Windows.
	script := "../seat-duplication-census.sh"

	bash, unmeasured := safePushBash()
	if bash == "" {
		t.Skip(unmeasured)
	}
	t.Logf("driving src/seat-duplication-census.sh through %s", bash)

	out, err := exec.Command(bash, script, "--self-test").CombinedOutput()
	text := string(out)

	if err != nil {
		t.Fatalf("src/seat-duplication-census.sh --self-test failed: %v\n%s", err, text)
	}

	if !strings.Contains(text, "SELF-TEST CLEAN") {
		t.Fatalf("src/seat-duplication-census.sh --self-test did not report a clean run:\n%s", text)
	}

	const wantArms = 7
	if got := strings.Count(text, "\n  ok   "); got != wantArms {
		t.Fatalf("expected %d passing arms from src/seat-duplication-census.sh --self-test, counted %d -- an arm that quietly stops running is exactly what this count exists to catch:\n%s",
			wantArms, got, text)
	}

	// Each arm's REASON. The second one is the load-bearing one and is worth naming here rather than
	// only in the script: it asserts that ancestry is BLIND to the duplicate the first arm just found.
	// Without it the suite could pass while the whole instrument was redundant, and a redundant
	// instrument in a train gate is a cost with no reading.
	for _, reason := range []string{
		"cherry-picked duplicate FOUND",
		"ancestry BLIND to the same duplicate",
		"disjoint seats read CLEAN",
		"single-seat census REFUSES",
		// Added the day the tool shipped, on G's fleet-wide run (mailbox db6ab3484): a DECLARED
		// stack has the same SHAPE as a contamination, and the arm as first written would have
		// refused an accepted seat. Ancestry cannot be the exemption -- C1's own contamination was
		// ancestor-related while a cherry-pick is not -- so the discriminator is the DECLARATION.
		// The second of these two keeps the first honest: without it, --stack could be weakening
		// the tool rather than narrowing it and no arm would say so.
		"DECLARED stack reads CLEAN",
		"the same pair UNDECLARED stays RED",
		// COORD c53db4e3a ruled the SHA-first split: the same COMMIT on two seats is a STACK (git
		// merges it once, so the union carries it once) and may be declared; two DIFFERENT SHAs
		// sharing a patch-id is a cherry-pick and REFUSES ALWAYS. This last arm is the BOUND on
		// --stack, and it is named here because arms 5 and 7 are two halves of one claim: the
		// declaration exempts the shape git COLLAPSES and nothing else. If arm 7 ever goes green,
		// --stack has become a way to wave content aboard twice -- the whole failure the instrument
		// was built for, re-admitted through its own exemption.
		//
		// Made to fail deliberately before it was believed (floor 13), and the FIRST attempt at that
		// control is the reason arm 6 now asserts a reason too: hoisting the declaration check above
		// the split was caught by ARM 6, not arm 7, because arm 6's pair then misreported as a
		// cherry-pick while still exiting 1 -- which the old rc-only arm 6 would have passed. The
		// isolated control (a declaration escape added to the cherry-pick branch alone) lands on
		// arm 7 and leaves arms 1-6 green.
		"DECLARED cherry-pick STILL REFUSES",
	} {
		if !strings.Contains(text, reason) {
			t.Fatalf("src/seat-duplication-census.sh --self-test did not report the arm %q -- the arm names may survive a rewrite that loses what they assert:\n%s", reason, text)
		}
	}
}
