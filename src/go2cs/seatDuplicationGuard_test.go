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
// nothing outside a temp dir) and runs four arms RED-FIRST. It asserts the verdict line, the ARM COUNT
// and each arm's REASON -- the count because an exit code cannot tell a suite that ran four arms from
// one that silently lost three, and the reasons because an arm that keeps its name and loses its
// meaning is the failure this file's older sibling (safePushGuard_test.go) was written about.
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

	const wantArms = 4
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
	} {
		if !strings.Contains(text, reason) {
			t.Fatalf("src/seat-duplication-census.sh --self-test did not report the arm %q -- the arm names may survive a rewrite that loses what they assert:\n%s", reason, text)
		}
	}
}
