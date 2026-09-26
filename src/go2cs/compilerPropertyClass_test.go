// compilerPropertyClass_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"strings"
	"testing"
)

// The compiler-property class (OWNER RULING 2026-09-26, ledger 4a122cd994, class I of
// docs/phase4/DESIGN-managed-profiling.md). runtime/pprof's TestTryAdd and TestCPUProfileInlining
// open with Go's own self-check for whether the Go COMPILER inlined a function; the converted
// program carries no inlining decisions, so the check fails and the test SKIPS where Go passes.
// The owner ruled the family STRUCTURAL. TestTryAdd has ten subtests Go runs and the converted side
// never reaches, so an entry on the parent alone would leave ten Go-only rows no entry can absorb.
// These guards hold the admission to exactly that: the pass/skip shape on a signature-matched
// entry of THIS class, plus the Go-only rows underneath it, and nothing else.
//
// The class string is spelled literally here rather than through the constant, so a rename of the
// constant cannot silently carry the guards along with it.

const compilerPropertyLiteral = "compiler-property"

func tryAddFixture() (names []string, goResults, csResults, csOutputs map[string]string) {
	names = []string{"TestTryAdd", "TestTryAdd/bug35538", "TestTryAdd/full_stack_trace", "TestTryAdd/recursion_chain_inline", "TestOther"}
	goResults = map[string]string{
		"TestTryAdd": "pass", "TestTryAdd/bug35538": "pass", "TestTryAdd/full_stack_trace": "pass",
		"TestTryAdd/recursion_chain_inline": "pass", "TestOther": "pass",
	}
	csResults = map[string]string{"TestTryAdd": "skip", "TestOther": "pass"}
	csOutputs = map[string]string{"TestTryAdd": "Can't determine whether anything was inlined into inlinedCallerDump."}

	return names, goResults, csResults, csOutputs
}

func tryAddDisclosure(class, signature string) map[string]testDisclosure {
	return map[string]testDisclosure{
		"TestTryAdd": {Name: "TestTryAdd", Class: class, Signature: signature, Reason: "the inlining probe"},
	}
}

const tryAddSignature = "Can't determine whether anything was inlined"

// POSITIVE: the parent's pass/skip pair is disclosed and its Go-only subtests are withdrawn, by name.
func TestCompilerPropertyDisclosesTheSkipAndWithdrawsGoOnlySubtests(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()

	mismatches, _, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure(compilerPropertyLiteral, tryAddSignature), csOutputs)

	if len(mismatches) != 0 {
		t.Fatalf("a signature-matched compiler-property root must leave no mismatch; got %v", mismatches)
	}
	if len(disclosed) != 1 || disclosed[0] != "TestTryAdd" {
		t.Fatalf("exactly the parent must be disclosed; got %v", disclosed)
	}
	if len(withdrawn) != 3 {
		t.Fatalf("all three Go-only subtests must be withdrawn by name; got %v", withdrawn)
	}
}

// NEGATIVE 1: a misspelled class absorbs nothing and withdraws nothing.
func TestMisspelledCompilerPropertyAbsorbsNothing(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()

	mismatches, _, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure("compiler-proprety", tryAddSignature), csOutputs)

	if len(disclosed) != 0 || len(withdrawn) != 0 {
		t.Fatalf("an unknown class must absorb nothing; disclosed=%v withdrawn=%v", disclosed, withdrawn)
	}
	if len(mismatches) != 4 {
		t.Fatalf("the parent and its three Go-only subtests must all read as mismatches; got %v", mismatches)
	}
}

// NEGATIVE 2: the right class with a skip text that does not carry the signature. The row MOVED:
// the parent is a mismatch naming the class and its subtree withdraws nothing.
func TestCompilerPropertyUnmatchedSignatureWithdrawsNothing(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()
	csOutputs["TestTryAdd"] = "skipping: some other reason"

	mismatches, _, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure(compilerPropertyLiteral, tryAddSignature), csOutputs)

	if len(disclosed) != 0 || len(withdrawn) != 0 {
		t.Fatalf("an unmatched skip must absorb and withdraw nothing; disclosed=%v withdrawn=%v", disclosed, withdrawn)
	}
	if len(mismatches) != 4 || !strings.Contains(strings.Join(mismatches, "\n"), compilerPropertyLiteral) {
		t.Fatalf("expected four mismatches, the parent's naming the class; got %v", mismatches)
	}
}

// NEGATIVE 3: ONE shape. A compiler-property row whose C# side FAILS is not absorbed, even when the
// failure text carries the signature, and it roots no withdrawal.
func TestCompilerPropertyAdmitsExactlyTheSkipShape(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()
	csResults["TestTryAdd"] = "fail"

	mismatches, _, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure(compilerPropertyLiteral, tryAddSignature), csOutputs)

	if len(disclosed) != 0 || len(withdrawn) != 0 {
		t.Fatalf("a failure must never be laundered through the class; disclosed=%v withdrawn=%v", disclosed, withdrawn)
	}
	if len(mismatches) != 4 {
		t.Fatalf("the failing parent and its Go-only subtests must be mismatches; got %v", mismatches)
	}
}

// NEGATIVE 4: a subtest that EXISTS on the C# side compares strictly under a disclosed root.
func TestCompilerPropertyTwoSidedChildStaysStrict(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()
	csResults["TestTryAdd/bug35538"] = "fail"

	mismatches, _, _, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure(compilerPropertyLiteral, tryAddSignature), csOutputs)

	if len(mismatches) != 1 || !strings.HasPrefix(mismatches[0], "TestTryAdd/bug35538:") {
		t.Fatalf("a two-sided divergent child must stay a strict mismatch; got %v", mismatches)
	}
	if len(withdrawn) != 2 {
		t.Fatalf("only the still-one-sided subtests withdraw; got %v", withdrawn)
	}
}

// NEGATIVE 5: the widening belongs to THIS class alone. A platform-skip root with the same shape is
// still absorbed on its own row, but its Go-only descendants stay mismatches, exactly as before.
func TestPlatformSkipRootStillWithdrawsNothing(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()

	mismatches, _, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure(platformSkipClass, tryAddSignature), csOutputs)

	if len(disclosed) != 1 || disclosed[0] != "TestTryAdd" {
		t.Fatalf("platform-skip's own absorption must be untouched; got %v", disclosed)
	}
	if len(withdrawn) != 0 || len(mismatches) != 3 {
		t.Fatalf("a platform-skip root must withdraw nothing; withdrawn=%v mismatches=%v", withdrawn, mismatches)
	}
}

// NEGATIVE 6: an absent row under NO disclosed root stays a mismatch.
func TestCompilerPropertyNeverAbsorbsAnArbitraryAbsentRow(t *testing.T) {
	names, goResults, csResults, csOutputs := tryAddFixture()
	names = append(names, "TestUnrelated/child")
	goResults["TestUnrelated/child"] = "pass"

	mismatches, _, _, withdrawn := matchTerminalStatuses(names, goResults, csResults, tryAddDisclosure(compilerPropertyLiteral, tryAddSignature), csOutputs)

	if len(withdrawn) != 3 {
		t.Fatalf("only TestTryAdd's own subtests withdraw; got %v", withdrawn)
	}
	if len(mismatches) != 1 || !strings.HasPrefix(mismatches[0], "TestUnrelated/child:") {
		t.Fatalf("an absent row outside the disclosed root must stay a mismatch; got %v", mismatches)
	}
}

// The proof page must not say "not a skipped test" about a page that discloses a pass/skip row of this
// class, and it names the row as a compiler-property skip in words.
func TestCompilerPropertyProofPageStatesTheSkip(t *testing.T) {
	comparison := testComparison{
		Package: "runtime/pprof", Status: "validated",
		Go:        map[string]string{"TestTryAdd": "pass", "TestOther": "pass"},
		CSharp:    map[string]string{"TestTryAdd": "skip", "TestOther": "pass"},
		Withdrawn: []string{"TestTryAdd/bug35538"},
	}

	page := renderValidationProofPage(proofPageProvenance{importPath: "runtime/pprof", goVersion: "1.24.13", platform: "linux/amd64", date: "2026-09-26"},
		comparison, tryAddDisclosure(compilerPropertyLiteral, tryAddSignature), nil)

	if strings.Contains(page, "not\na skipped test") {
		t.Fatalf("a page disclosing a compiler-property skip must not claim no test was skipped:\n%s", page)
	}
	if !strings.Contains(page, "`TestTryAdd` is a **compiler-property skip**") {
		t.Fatalf("the row must be named as a compiler-property skip in words:\n%s", page)
	}
}
