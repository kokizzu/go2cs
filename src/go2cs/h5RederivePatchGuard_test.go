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

	const wantArms = 8
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
	} {
		if !strings.Contains(text, reason) {
			t.Fatalf("src/apply-h5-c1-1-rederives.sh --self-test did not report the arm %q -- the arm names may survive a rewrite that loses what they assert:\n%s", reason, text)
		}
	}
}
