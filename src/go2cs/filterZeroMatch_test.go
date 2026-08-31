// filterZeroMatch_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"strings"
	"testing"
)

// The ZERO-MATCH GUARD. A `-test-filter` run that matches nothing compares zero verdicts, so both
// sides exit 0, no mismatch exists, and the run reads exactly like a clean validation — a vacuous
// proof that is indistinguishable from a real one. filterMatchedNothing is what makes that case
// loud.
//
// This test is a POSITIVE CONTROL first and a specification second: the fire case is asserted
// before the quiet cases, because a guard that never fires would pass every "does not fire" test
// trivially. Neuter the condition in filterMatchedNothing and TestFilterZeroMatchGuardFires goes
// red immediately.
func TestFilterZeroMatchGuardFires(t *testing.T) {
	message, fired := filterMatchedNothing("^TestNoSuchName$", "validated", true, 0, 0, 0)

	if !fired {
		t.Fatal("guard must fire: a filtered run that compared ZERO verdicts measured nothing and must not read as a pass")
	}

	// The message has to name the filter — an operator reading a failed census needs to see the
	// regex that matched nothing without going back to the command line.
	if !strings.Contains(message, "^TestNoSuchName$") {
		t.Errorf("message must quote the filter that matched nothing, got: %s", message)
	}

	// ...and must say which case this is, per the excluded/silent distinction the guard exists to
	// disambiguate rather than to exempt.
	if !strings.Contains(message, "excludes no declarations") {
		t.Errorf("with nothing excluded the message must say the regex itself matched nothing, got: %s", message)
	}
}

// The excluded case still FIRES — it is not an exemption. A filter naming a deliberately excluded
// declaration measured just as little as a typo'd one; the only difference the guard owes is
// telling the operator which situation they are in.
func TestFilterZeroMatchGuardReportsExcludedDeclarations(t *testing.T) {
	message, fired := filterMatchedNothing("^TestGatedThing$", "validated", true, 0, 0, 3)

	if !fired {
		t.Fatal("guard must still fire when the package has excluded declarations: an excluded target measured nothing either")
	}

	if !strings.Contains(message, "excludes 3 declaration(s)") {
		t.Errorf("message must report the excluded count so the operator can tell the two cases apart, got: %s", message)
	}
}

// Everything the guard must NOT claim. Each row is a case that already has a correct outcome
// elsewhere, and a guard that stole it would turn a precise diagnosis into a misleading one.
func TestFilterZeroMatchGuardStaysQuiet(t *testing.T) {
	cases := []struct {
		name          string
		filter        string
		status        string
		matched       bool
		goCount       int
		csCount       int
		excludedCount int
		why           string
	}{
		{
			name: "no filter", filter: "", status: "validated", matched: true,
			why: "an UNFILTERED package with no verdicts is a different situation; this guard is about the filter",
		},
		{
			name: "filter matched something", filter: "^TestReal$", status: "validated", matched: true,
			goCount: 4, csCount: 4,
			why: "verdicts were compared, so the census measured what it was asked to",
		},
		{
			name: "not-applicable package", filter: "^TestAnything$", status: "not-applicable", matched: true,
			why: "zero verdicts are a property of a package with no eligible tests, not of the filter",
		},
		{
			name: "already failing", filter: "^TestAnything$", status: "failing", matched: false,
			why: "a real failure already owns this run; the guard must not overwrite its diagnosis",
		},
		{
			name: "one-sided match, go only", filter: "^TestOneSided$", status: "validated", matched: true,
			goCount: 2, csCount: 0,
			why: "one-sided rows are mismatches and stay fatal through the existing path",
		},
		{
			name: "one-sided match, c# only", filter: "^TestOneSided$", status: "validated", matched: true,
			goCount: 0, csCount: 2,
			why: "same as above, mirrored — the guard must not claim either direction",
		},
		{
			name: "infrastructure-blocked", filter: "^TestAnything$", status: "infrastructure-blocked", matched: false,
			why: "a capability block is a more specific and more useful diagnosis than 'matched nothing'",
		},
	}

	for _, test := range cases {
		t.Run(test.name, func(t *testing.T) {
			if message, fired := filterMatchedNothing(
				test.filter, test.status, test.matched,
				test.goCount, test.csCount, test.excludedCount); fired {
				t.Errorf("guard must not fire (%s), got: %s", test.why, message)
			}
		})
	}
}
