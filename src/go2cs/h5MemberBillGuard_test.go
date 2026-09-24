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
//
// ⚠ AMENDED 2026-09-16 (q99): TWO SYMPTOMS, ONE GUARD, TWO CAUSES, TWO BOXES — and they were nearly
// read as one fault.
//
//   - STALE BY COMPLETION. The self-test's fixture was the LIVE src/core/runtime/runtime2.cs. The
//     bill is APPLIED on the version branch, so the "unpatched" tree the suite builds was already
//     patched and ARM 2 — the RED control, "the UNPATCHED file FAILS --verify" — could not be red.
//     The failure read as a broken checker; it was a fixture that had finished being a fixture. The
//     cure is a COMMITTED PRE-BILL COPY beside the script (src/h5-c1-2-fixture/runtime2.pre-bill.cs,
//     the file at dc78fb0df^), plus ARM 0, which refuses a fixture that is not pre-bill — because
//     freezing solves this ONCE and a later re-freeze from a patched corpus would put the defect back
//     silently.
//   - THE `py` DETECTION, and it is a SEPARATE cause on a SEPARATE box. resolve_python compared the
//     probe's answer to "42" exactly. Windows' Python launcher is py.exe, a native Windows program:
//     it answers "42\r\n" through a Git-Bash pipe, $(...) strips only the trailing newline, and the
//     gate refused a working interpreter — on a box where python3 and python are Store redirectors,
//     so `py` was the only real candidate and the run died saying none was found. The answer is
//     compared CR-stripped now, and ARM 16 proves the tolerance did not widen: 43\r\n is still
//     refused and /bin/echo is still refused.
//   - ⚠ AND THE DETECTION WAS STILL RED ON THAT BOX AFTER THE CR FIX, for a SECOND cause the CR fix
//     could not touch and neither box could see alone (G, 2026-09-16). resolve_python's loop used
//     `command -v`, which resolves shell FUNCTIONS before it searches PATH — and this script BOUND
//     the name `py` to a helper of its own, which dispatches through the very $PYBIN the resolver
//     was mid-computation of. Circular: the resolver consulted a name the script had bound to a
//     helper that needs the resolver's answer. Invisible with H5_PYTHON set (that branch returns
//     above the loop) and invisible wherever python3 or python answers (the loop returns at
//     candidate one or two), so ONLY a box whose first two candidates are non-answering stubs ever
//     reached it. The loop now resolves each candidate with `type -P`, a PATH search that ignores
//     functions, aliases and builtins, and the helper is renamed run_py. ARM 17 BUILDS that
//     intersection — two non-answering stubs and a working `py` on a prepended PATH, plus a
//     shadowing shell function — so the class is reproducible on every box rather than on one.
//
// Two symptoms in one red guard is exactly the shape that gets half-diagnosed. Both are named here
// so the next reader does not fix one and call the guard cured.
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

	// 19 at q99: ARM 0 (the fixture is PRE-BILL), ARM 16 (a CR-carrying answer is ACCEPTED) and
	// ARM 17 (a shadowing shell function LOSES) were added with the three fixes. The count is
	// asserted rather than bounded because an arm that quietly stops running is precisely what it
	// exists to catch, and a >= would admit exactly that.
	const wantArms = 19
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
		// q99: the fixture is a COMMITTED PRE-BILL copy, and this arm is what stops a later re-freeze
		// from an already-patched corpus quietly disabling the red control below it.
		"the FIXTURE is PRE-BILL",
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
		// ⚠ The DECLARED LENGTH, one table per arm. Go writes `[len(waitReasonStrings)]bool` for both;
		// the converter cannot fold a non-literal length and emits a bare `.array()`, which SparseArray
		// sizes at max key + 1 — so isWaitingForSuspendG materialises 36 against Go's 38 today (37
		// against 44 after the renumber) and THROWS where Go returns false, confirmed on a built tree by
		// i9 at f73b56b18. isIdleInSynctest read 44 only because Go's twelfth key happens to be the last
		// constant: correct by coincidence of a top key. COORD ruled both take the length explicitly
		// (486a3926a). Run per table with a restore between, because regressing both at once would prove
		// only that the first check is reached.
		"a BARE ΔisIdleInSynctest goes RED",
		"a BARE ΔisWaitingForSuspendG goes RED",
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
		// q99, and the OTHER half of the same red guard: Windows' py.exe answers 42 with a trailing
		// CR through a Git-Bash pipe, which the exact-match probe refused. Both directions are
		// asserted in the arm itself — a wrong answer and a no-op are still refused.
		"a CR-carrying ANSWER is ACCEPTED",
		// q99 re-cut, and the SECOND cause behind the same red: the resolver must search PATH rather
		// than consult shell names, or the script's own helper shadows the interpreter it is looking
		// for. The arm builds the two-stub intersection itself, so it no longer needs the one box
		// that happened to have it.
		"a SHADOWING function LOSES",
	} {
		if !strings.Contains(text, reason) {
			t.Fatalf("src/apply-h5-c1-2-member-bill.sh --self-test did not report the arm %q -- the arm names may survive a rewrite that loses what they assert:\n%s", reason, text)
		}
	}
}
