// h5RederivePatchGuard_test.go - Gbtc
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

// THE GUARD FOR src/apply-h5-c1-1-rederives.sh.
//
// That script carries the two C1-1 hand-own re-derives that GATE H4a: R's fifth rehearsal (mailbox
// 4b4134242) measured that the landing tree plus a seeded 1.24.13 reconvert plus H5c does not build
// `runtime` -- 120/120/120 unique sites, every root one of eight in two frozen hand-owns. The fix
// cannot be a commit, because both defects are HOP-CONDITIONAL: at a02ac3df3 runtime/internal/sys is
// PRESENT and note_other.cs is ABSENT, so a landed fix breaks the corpus that is green today. It is
// therefore a patch the H5 scratch applies, and a patch nobody runs until H5 is a patch nobody has
// tested -- which is what this guard is for. See docs/phase4/PATCH-h5-c1-1-runtime-rederives.md.
//
// The script's own --self-test is hermetic (no clone, no network, a temp tree) and red-first. This
// asserts its verdict, its ARM COUNT and each arm's REASON, for the reasons safePushGuard_test.go
// gives: an exit code cannot tell a suite that ran every arm from one that silently lost three, and
// an arm that keeps its name while losing its meaning is the failure that file was written about.
//
// ⚠ TWO OF THESE ARMS EXIST BECAUSE THE CHECKER WAS WRONG FIRST, and both faults are this session's
// recurring class rather than anything exotic:
//
//   - the first checker used `grep -x` on CRLF files, matched nothing, and reported a failure on a
//     CORRECTLY patched tree. It also made the unpatched-tree arm pass for the wrong reason, since
//     that arm wanted only a non-zero exit and a checker broken on every input supplies one. The
//     unpatched arm now asserts WHICH defects it saw.
//   - the goǃ(runfinq) check was file-wide, and mfinal.cs's own header comment NAMES goǃ(runfinq)
//     while describing the body it replaced -- so the correctly-carried file failed on its own
//     documentation. Arm 8 exists so that can never silently return.
func TestH5RederivePatchSelfTest(t *testing.T) {
	// A POSIX spelling, not filepath.Join: the argument is read by bash, not by Windows.
	script := "../apply-h5-c1-1-rederives.sh"

	bash, unmeasured := safePushBash()
	if bash == "" {
		t.Skip(unmeasured)
	}
	t.Logf("driving src/apply-h5-c1-1-rederives.sh through %s", bash)

	out, err := exec.Command(bash, script, "--self-test").CombinedOutput()
	text := string(out)

	if err != nil {
		t.Fatalf("src/apply-h5-c1-1-rederives.sh --self-test failed: %v\n%s", err, text)
	}

	if !strings.Contains(text, "SELF-TEST CLEAN") {
		t.Fatalf("src/apply-h5-c1-1-rederives.sh --self-test did not report a clean run:\n%s", text)
	}

	const wantArms = 16
	if got := strings.Count(text, "\n  ok   "); got != wantArms {
		t.Fatalf("expected %d passing arms from src/apply-h5-c1-1-rederives.sh --self-test, counted %d -- an arm that quietly stops running is exactly what this count exists to catch:\n%s",
			wantArms, got, text)
	}

	for _, reason := range []string{
		// The load-bearing refusal: applied to a pre-H5c tree these edits break a tree that BUILDS,
		// which is the whole reason the fix is a patch and not a commit.
		"a PRE-H5c tree is REFUSED",
		"an UNPATCHED tree FAILS --verify",
		"apply then verify is GREEN",
		// The duplicate-using trap: both files already carry `using @internal.runtime;`, so the old
		// namespace line is DELETED rather than re-pointed. A naive re-point emits CS0105.
		"no DUPLICATE using directive",
		"CRLF preserved byte for byte",
		"re-apply is IDEMPOTENT",
		// The carry hazard, made decidable: a re-derive that re-applies the PRE-mcleanup hand-own
		// returns runtime.AddCleanup to a silent no-op, and the merge that does it is CLEAN.
		"a LOST mcleanup hand-own FAILS",
		"a COMMENT naming the old body OK",
		// R scored the applier on the REAL post-H5c root (mailbox 6f6528938 §6) and found the
		// precondition keyed on a shape H5c does not produce: H5c removes FILES, not directories, so
		// runtime/internal/sys survives with its csproj, README, icons and tests. The first cut
		// therefore refused rc=2 on the one tree it exists for. Arm 9 is that tree; arm 10 keeps arm 9
		// from having simply deleted the check. The fixture could not contain this shape -- only real
		// data did, which is the argument for scoring on a real root.
		"H5c RESIDUE is accepted",
		"one PRODUCTION .cs still REFUSES",
		// i9 scored the applier on their lane (mailbox a50d4f8c1) and found two defects, NEITHER in
		// the edit logic -- which they scored sound 10 of 10 -- but in REACHING it and in the run's
		// ability to say when it had not. The script called python3, that lane has `python` and no
		// `python3`, and apply() never read an exit status: the run printed APPLIED having edited
		// nothing. It failed safe only because verify() is pure shell; on a partly-patched tree the
		// post-condition would have passed over an apply that never ran.
		"a DEAD interpreter REFUSES",
		"a FAILING edit step REFUSES",
		// And arm 5 was STRUCTURALLY DEAD on a native-Windows python: both reads threw, both captures
		// were empty, and a STRING compare called that equal -- while ARM 4 read the same path
		// successfully one line earlier because tr is an MSYS tool. An arm that cannot fail proves
		// nothing, so arm 13 is arm 5's own negative control.
		"the CRLF arm CAN go red",
		// C2 scored the re-cut on the complement platform (mailbox a2b892aef) and found the GATE's
		// own hole: it probed with an exit STATUS, so any program ignoring its arguments and exiting
		// 0 became the interpreter -- /bin/true passes, and so does /bin/echo, which is exactly the
		// Windows Store-alias shape this repo's own apply.py header warns about. Worse, the loop
		// takes the first passer, so an alias SHADOWED the real python one candidate later: i9's
		// defect through a door that exits 0 instead of 1, which the status check cannot see. The
		// probe now asserts an ANSWER (print(6*7) == 42), which refuses the no-op and lets the loop
		// fall through. C2's sentence for it: a tool that exits 0 has not told you it did the work.
		"a probe-passing NO-OP REFUSED",
		"a Store-ALIAS is skipped",
		// i9 ran the applier on the real train-47 union (mailbox 0687402db) and the carry post-condition
		// fired -- correctly, but its message asserted "the hand-own was NOT carried into this
		// re-derive" when in fact the mcleanup seat is train 48 and had simply not landed. The
		// predicate cannot tell those apart from mfinal.cs alone, so it now reports the DISCRIMINATOR
		// (is runtime/mcleanup.cs present as a marked hand-own?) rather than asserting a cause. Arm 7
		// was itself mis-fixtured: named for the LOST case, it carried no mcleanup.cs and so exercised
		// the not-landed state under the lost label. Arm 7 gained the marked fixture; arm 16 is its twin.
		"NOT-LANDED reads as not landed",
	} {
		if !strings.Contains(text, reason) {
			t.Fatalf("src/apply-h5-c1-1-rederives.sh --self-test did not report the arm %q -- the arm names may survive a rewrite that loses what they assert:\n%s", reason, text)
		}
	}
}
