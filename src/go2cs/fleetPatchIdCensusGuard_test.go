// fleetPatchIdCensusGuard_test.go - Gbtc
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

// THE GUARD FOR src/fleet-patchid-census.sh.
//
// That script answers "what does the WHOLE REMOTE carry twice", where its sibling
// src/seat-duplication-census.sh answers "does THIS TRAIN carry anything twice". Both exist because
// ANCESTRY CANNOT SEE A CHERRY-PICK: the same diff under a new SHA leaves `merge-base --is-ancestor`
// reading clean on both branches while the content sits on both (coordinator ruling, mailbox
// c53db4e3a, 2026-09-13).
//
// ⚠ THE ARM THIS FILE EXISTS FOR IS ARM 3, the classification. Measured the same day over 120
// branches: 17 patch-ids appeared on more than one branch, and FOUR of them were a deliberate
// three-lane docs stack that the fleet's coordinator had ruled into place hours earlier. A census
// that calls a stack a duplicate refuses the shape the fleet builds on purpose, and one that calls a
// duplicate a stack misses the class it was written for -- so STACK (one SHA on several refs) and
// DUPLICATE (one diff under two or more SHAs) are separate findings, and arm 3 asserts BOTH
// directions: the stack is reported as a STACK, and it is NOT reported as a duplicate.
//
// A script nobody runs fails open, which is false-green route #6, so it lands in src/ WITH A GUARD,
// and the guard lives here for the same reason projitemsIntegrity_test.go, safePushGuard_test.go and
// internal/repoguard/fleetIdentifierCensus_test.go do: the converter's own `go test ./...` is the one
// gate every lane already pays for.
//
// This one is CHEAP, unlike its safe-push sibling: the self-test builds a hermetic repository in a
// temp dir and touches no network, no clone and nothing outside it, so there is no nested `go test`
// and no push. Roughly a second.
func TestFleetPatchIdCensusSelfTest(t *testing.T) {
	// A POSIX spelling, not filepath.Join: the argument is read by bash, not by Windows.
	script := "../fleet-patchid-census.sh"

	// safePushBash (safePushGuard_test.go) resolves the bash that can actually drive a fleet script
	// on this host -- on Windows from `git --exec-path` rather than PATH order, because PATH's bash
	// there may be WSL, which cannot open a Windows worktree's gitdir. Reused rather than
	// re-derived: there is one right answer to "which bash" per host and it belongs in one place.
	bash, unmeasured := safePushBash()
	if bash == "" {
		t.Skip(unmeasured)
	}
	t.Logf("driving src/fleet-patchid-census.sh through %s", bash)

	out, err := exec.Command(bash, script, "--self-test").CombinedOutput()
	text := string(out)
	if err != nil {
		t.Fatalf("src/fleet-patchid-census.sh --self-test failed: %v\n%s", err, text)
	}

	if !strings.Contains(text, "SELF-TEST CLEAN") {
		t.Fatalf("src/fleet-patchid-census.sh --self-test did not report a clean run:\n%s", text)
	}

	// The ARM COUNT, because an exit code cannot distinguish a suite that ran six arms from one that
	// silently lost five.
	const wantArms = 6
	if got := strings.Count(text, "\n  ok   "); got != wantArms {
		t.Fatalf("expected %d passing arms from src/fleet-patchid-census.sh --self-test, counted %d -- an arm that quietly stops running is exactly what this count exists to catch:\n%s",
			wantArms, got, text)
	}

	// Each arm's REASON, not merely its count: an arm that keeps its name and loses its meaning is
	// the failure this file's older sibling was written about. Two of these are load-bearing in a way
	// the others are not -- "disjoint refs read CLEAN" is what keeps the two red arms from being a
	// census that reports everything, and the STACK line is the classification the fleet's own
	// declared stacks depend on.
	for _, reason := range []string{
		"cherry-picked duplicate FOUND and named",
		"ancestry BLIND to that same duplicate",
		"classified as STACK",
		"disjoint refs read CLEAN",
		"a single-ref census REFUSES",
		"merge commits EXCLUDED and COUNTED",
	} {
		if !strings.Contains(text, reason) {
			t.Errorf("the self-test no longer asserts the reason %q -- an arm asserts the REASON it failed, or it is not a control:\n%s", reason, text)
		}
	}
}
