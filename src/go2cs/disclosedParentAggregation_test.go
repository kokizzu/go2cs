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

	mismatches, _, disclosed := matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

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

	mismatches, _, disclosed = matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

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

	mismatches, _, _ = matchTerminalStatuses(names, goResults, csResults, disclosures, csOutputs)

	if len(mismatches) == 0 {
		t.Fatal("parent with its own failure output must stay a strict mismatch")
	}
}
