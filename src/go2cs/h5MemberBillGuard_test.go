// h5MemberBillGuard_test.go - Gbtc
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

// THE GUARD FOR src/apply-h5-c1-2-member-bill.sh.
//
// That script carries the C1-2 member bill for the frozen runtime2.cs hand-own at go1.24.13: two g
// fields, fourteen waitReason constants RENUMBERED, six ADDED, six waitReasonStrings entries, the
// isIdleInSynctest accessor and table, and two recorded omissions. Like C1-1 it is a PATCH and not a
// commit, and for the same kind of reason: g.syncGroup is ж<synctestGroup>, and synctestGroup arrives
// with the 1.24 emission, so landing the edit breaks a corpus that is green today. A patch nobody
// runs until H5 is a patch nobody has tested. See docs/phase4/PATCH-h5-c1-2-runtime2-member-bill.md.
//
// ⚠ WHY THIS BILL IS GUARDED HARDER THAN ITS SIZE SUGGESTS. Fourteen of the twenty items are a
// RENUMBER, and a renumber is invisible to every instrument the fleet had pointed at this hop:
//
//   - the corpus spells each constant as an explicit `= N` literal, so a WRONG N COMPILES;
//   - waitReasonStrings is keyed SYMBOLICALLY, so it follows the constants wherever they go -- the
//     table stays self-consistent while every reason from index 24 up names its NEIGHBOUR;
//   - i9's rebuild falsifier ("a build naming a waitReason constant means incomplete") fires on the
//     six ADDITIONS and cannot fire on the fourteen shifts at all.
//
// So the applier derives NOTHING from a typed table: names, values and strings all come out of Go's
// own runtime2.go at the resolved GOROOT, and the post-condition re-extracts and joins BY NAME.
//
// ⚠ AND THE EXTRACTOR REFUSES ON AN EMPTY READ, which is the other half. C1's first sizing of this
// bill said "six constants, appended" and was WRONG -- an awk range that never opened returned zero
// constants for both releases, and the prefix check then compared TWO EMPTY FILES and printed
// "IDENTICAL: no renumbering". A comparison of two empty sets reports agreement, and "identical" is
// the most dangerous word an empty reading can produce, because unlike a zero count it does not look
// like nothing. Arm 13 is that near-miss turned into an arm.
func TestH5MemberBillSelfTest(t *testing.T) {
	// A POSIX spelling, not filepath.Join: the argument is read by bash, not by Windows.
	script := "../apply-h5-c1-2-member-bill.sh"

	bash, unmeasured := safePushBash()
	if bash == "" {
		t.Skip(unmeasured)
	}
	t.Logf("driving src/apply-h5-c1-2-member-bill.sh through %s", bash)

	out, err := exec.Command(bash, script, "--self-test").CombinedOutput()
	text := string(out)

	if err != nil {
		t.Fatalf("src/apply-h5-c1-2-member-bill.sh --self-test failed: %v\n%s", err, text)
	}

	if !strings.Contains(text, "SELF-TEST CLEAN") {
		t.Fatalf("src/apply-h5-c1-2-member-bill.sh --self-test did not report a clean run:\n%s", text)
	}

	const wantArms = 15
	if got := strings.Count(text, "\n  ok   "); got != wantArms {
		t.Fatalf("expected %d passing arms from src/apply-h5-c1-2-member-bill.sh --self-test, counted %d -- an arm that quietly stops running is exactly what this count exists to catch:\n%s",
			wantArms, got, text)
	}

	// ⚠ A SKIP IS NOT A PASS. The self-test's fixture is the clone's OWN runtime2.cs -- real data, not
	// a synthetic const block written to satisfy the parser -- so it reports NOT RUN rather than
	// substituting one. That line must never appear in a run this guard calls clean.
	if strings.Contains(text, "NOT RUN") {
		t.Fatalf("the self-test declined to run an arm; a skipped arm is not a passing arm:\n%s", text)
	}

	for _, reason := range []string{
		// The load-bearing refusal, and the reason the bill is a patch: g.syncGroup names a type that
		// arrives with the 1.24 emission, so on a pre-hop tree this edit does not compile.
		"a PRE-HOP tree is REFUSED",
		"the UNPATCHED file FAILS --verify",
		"apply then verify is GREEN",
		// Counted off the DIFF rather than asserted by the code that wrote it: 14 lines rewritten,
		// 6 constants added, 6 strings rows added, 2 g fields.
		"the BILL is 14 + 6 + 6 + 2",
		"the boundary values are right",
		"CRLF preserved byte for byte",
		// waitReasonStrings is rebuilt WHOLE from Go's table, which is the shape most likely to
		// duplicate rows on a second pass, so idempotence asserts the counts and not the exit code.
		"re-apply is IDEMPOTENT",
		// Floor item 13, and the arm the whole HOLD was about: ONE constant left at its 1.23 value must
		// go red NAMING it. That state compiles, links and passes a green suite.
		"ONE stale constant goes RED",
		// C2's hole (mailbox 2a6938f4b): a constant without its waitReasonStrings row stringifies as
		// "unknown wait reason" and the sparse table stops materialising dense. Neither is a build error.
		"a MISSING strings row goes RED",
		// SparseArray sizes itself at max key + 1, so the idle table reads 44 slots only because its
		// highest key IS the last constant. That is load-bearing rather than incidental, so it is pinned.
		"a SHORT idle table goes RED",
		"a MISSING g field goes RED",
		// The two omissions (m.mWaitList, m's size-class padding) are RECORDED at the site. An
		// undocumented gap is indistinguishable from an oversight to the next reader holding a diff.
		"a STRIPPED omission note goes RED",
		// C1's own near-miss, as an arm: an empty extraction must ABORT rather than compare. This arm
		// also caught the dispatch turning a REFUSAL into a verdict -- it printed "POST-CONDITION
		// FAILED" over a run that never read its inputs, which sends a reader to look at the corpus.
		"an EMPTY extraction REFUSES",
		// The version discriminator is DERIVED (isIdleInSynctest does not exist before 1.24) rather
		// than a typed list of the six new names, which would be a second copy of the thing checked.
		"a PRE-1.24 GOROOT is REFUSED",
		// Carried from C1-1, where C2 measured the gate probing an exit STATUS: /bin/echo passed it.
		// A tool that exits 0 has not told you it did the work.
		"a probe-passing NO-OP is REFUSED",
	} {
		if !strings.Contains(text, reason) {
			t.Fatalf("src/apply-h5-c1-2-member-bill.sh --self-test did not report the arm %q -- the arm names may survive a rewrite that loses what they assert:\n%s", reason, text)
		}
	}
}
