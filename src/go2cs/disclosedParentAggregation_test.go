// disclosedParentAggregation_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"strings"
	"testing"
)

// The compare oracle's parent-aggregation rule: a t.Run parent whose Go=pass/C#=fail divergence
// is PURELY the roll-up of disclosed-divergent children — no failure output of its own, no own
// disclosure entry, at least one disclosed descendant and NO mismatched descendant —
// reclassifies as disclosed itself (encoding/binary's TestSizeAllocs over its pinned
// alloc-profile children). Any undisclosed failing child keeps the parent a strict mismatch —
// the rule can never mask.
func TestDisclosedParentAggregation(t *testing.T) {
	disclosures := map[string]testDisclosure{
		"TestAllocs/a": {Name: "TestAllocs/a", Class: "alloc-profile", Signature: "Expected no allocations", Reason: "r"},
		"TestAllocs/b": {Name: "TestAllocs/b", Class: "alloc-profile", Signature: "Expected no allocations", Reason: "r"},
	}

	names := []string{"TestAllocs", "TestAllocs/a", "TestAllocs/b"}
	goResults := map[string]string{"TestAllocs": "pass", "TestAllocs/a": "pass", "TestAllocs/b": "pass"}
	csResults := map[string]string{"TestAllocs": "fail", "TestAllocs/a": "fail", "TestAllocs/b": "fail"}
	csOutputs := map[string]string{"TestAllocs": "", "TestAllocs/a": "Expected no allocations, got 5", "TestAllocs/b": "Expected no allocations, got 7"}

	mismatches, _, disclosed, _ := matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	if len(mismatches) != 0 {
		t.Fatalf("fully-disclosed parent must aggregate as disclosed, got mismatches: %v", mismatches)
	}

	if len(disclosed) != 3 {
		t.Fatalf("expected parent + both children disclosed (3), got %v", disclosed)
	}

	// An UNDISCLOSED failing child keeps the parent a strict mismatch.
	names = append(names, "TestAllocs/c")
	goResults["TestAllocs/c"] = "pass"
	csResults["TestAllocs/c"] = "fail"
	csOutputs["TestAllocs/c"] = "some real regression"

	mismatches, _, disclosed, _ = matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	foundParent := false

	for _, m := range mismatches {
		if strings.HasPrefix(m, "TestAllocs:") {
			foundParent = true
		}
	}

	if !foundParent {
		t.Fatalf("parent with an undisclosed failing child must stay a strict mismatch, got mismatches: %v disclosed: %v", mismatches, disclosed)
	}

	// A parent with its OWN failure text never aggregates, even over disclosed children.
	csOutputs["TestAllocs"] = "parent-level assertion failed"
	delete(goResults, "TestAllocs/c")
	delete(csResults, "TestAllocs/c")
	names = names[:3]

	mismatches, _, _, _ = matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	if len(mismatches) == 0 {
		t.Fatal("parent with its own failure output must stay a strict mismatch")
	}
}

// The DOWNWARD dual: Go-side rows underneath a signature-matched disclosed root — subtests
// `go test` ran that the converted host never reached, because the disclosed test failed at its
// root before its case fan-out — are withdrawn rather than mismatched (crypto/tls's
// TestBogoSuite over its 3,242 BoGo case rows). The rule never widens: a root failing with the
// WRONG signature withdraws nothing, and a child that EXISTS on the C# side still compares
// strictly.
func TestDisclosedRootWithdrawsGoOnlyDescendants(t *testing.T) {
	disclosures := map[string]testDisclosure{
		"TestBogo": {Name: "TestBogo", Class: "host-limit", Signature: "bogo failed", Reason: "r"},
	}

	names := []string{"TestBogo", "TestBogo/CaseA", "TestBogo/CaseB", "TestBogo/Sub/CaseC", "TestOther"}
	goResults := map[string]string{"TestBogo": "pass", "TestBogo/CaseA": "pass", "TestBogo/CaseB": "skip", "TestBogo/Sub/CaseC": "skip", "TestOther": "pass"}
	csResults := map[string]string{"TestBogo": "fail", "TestOther": "pass"}
	csOutputs := map[string]string{"TestBogo": "bogo failed: exit status 1"}

	mismatches, _, disclosed, withdrawn := matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	if len(mismatches) != 0 {
		t.Fatalf("Go-only rows under a signature-matched disclosed root must withdraw, got mismatches: %v", mismatches)
	}

	if len(disclosed) != 1 || disclosed[0] != "TestBogo" {
		t.Fatalf("expected only the root disclosed, got %v", disclosed)
	}

	if len(withdrawn) != 3 {
		t.Fatalf("expected all three Go-only descendants withdrawn, got %v", withdrawn)
	}

	// A root failing with the WRONG signature is a mismatch and withdraws NOTHING — its
	// children stay one-sided mismatches too, so a regressed root can never quietly absorb its
	// subtree.
	csOutputs["TestBogo"] = "some other failure entirely"

	mismatches, _, disclosed, withdrawn = matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	if len(withdrawn) != 0 {
		t.Fatalf("a non-matching root signature must withdraw nothing, got %v", withdrawn)
	}

	if len(mismatches) != 4 || len(disclosed) != 0 {
		t.Fatalf("expected the root and all three children as mismatches, got mismatches: %v disclosed: %v", mismatches, disclosed)
	}

	// A child that EXISTS on the C# side never rides the withdrawal — a divergent two-sided row
	// under a disclosed root is still a strict mismatch.
	csOutputs["TestBogo"] = "bogo failed: exit status 1"
	csResults["TestBogo/CaseA"] = "fail"

	mismatches, _, _, withdrawn = matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	if len(mismatches) != 1 || !strings.HasPrefix(mismatches[0], "TestBogo/CaseA:") {
		t.Fatalf("a two-sided divergent child under a disclosed root must stay a strict mismatch, got %v", mismatches)
	}

	if len(withdrawn) != 2 {
		t.Fatalf("only the still-one-sided descendants withdraw, got %v", withdrawn)
	}
}

// Package-level manifest notes reach the rendered proof page verbatim, above the verdicts —
// the mechanism that lets a caveat about the comparison's MEANING (crypto/tls's expired-fixture
// ceiling) survive every regeneration, where a hand edit to the generated page would not.
func TestManifestNotesRenderOnTheProofPage(t *testing.T) {
	comparison := testComparison{
		Package: "pkg", Status: "validated",
		Go:     map[string]string{"TestA": "pass", "TestB": "fail"},
		CSharp: map[string]string{"TestA": "pass", "TestB": "fail"},
	}

	note := "the expired-fixture ceiling moves with the calendar"
	page := renderValidationProofPage(proofPageProvenance{importPath: "pkg", goVersion: "1.23.1", platform: "windows/amd64", date: "2026-08-18"},
		comparison, nil, []string{note})

	if !strings.Contains(page, "> "+note) {
		t.Fatalf("manifest note must render as a blockquote above the verdicts, got page:\n%s", page)
	}

	if !strings.Contains(page, "| `TestB` | fail | fail |") {
		t.Fatalf("agreed failures must render as matched fail/fail rows, got page:\n%s", page)
	}
}
