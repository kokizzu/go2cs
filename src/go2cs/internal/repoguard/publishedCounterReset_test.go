// publishedCounterReset_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package repoguard

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"go2cs/internal/releasestamp"
)

// THE H2 COUNTER GUARD.
//
// H2 of docs/GoCorpusMigration.md bumps <GoStdLibVersion> to the incoming Go release. The build
// counter beside it is the LAST-PUBLISHED build number on THAT base, so a bump makes it stale by
// construction: nothing has been published on the new base at the moment the base changes.
//
// ⚠ WHAT IT COST WHEN IT WAS MISSED, measured at claude/version-go1.24.13 0f97dcc8db on 2026-09-20:
// the base went 1.23.12 -> 1.24.13 and the counter CARRIED at 3. Every converted package README then
// composed `1.24.13.3` and linked it -- 335 READMEs to a `nuget-1.24.13.3` tag with no object behind
// it and 191 to a `docs/validation/1.24.13.3/` directory that was never written. Nothing went red:
// version.props parses, the converter emits, the corpus builds. The only symptom is a reader
// clicking a badge and landing on a 404, which no gate in this repository clicks.
//
// H11.2 used to state this as "scripted monotonicity" -- that the counter only ever increases. That
// is TRUE AND INSUFFICIENT, and 0f97dcc8db is the proof: a carried counter is perfectly monotonic.
// The rule this guard holds is EXISTENCE-PLUS-MONOTONICITY, and existence is the half that catches
// the hop:
//
//	no release recorded on the current base -> the counter reads 0
//	releases recorded on the current base   -> the counter names the LATEST of them, exactly
//
// THE RECORD IS A TREE FACT (H11.5): `docs/validation/<stamp>/` is the write-once snapshot the
// publish ritual leaves behind, so this guard needs no git, no tag list and no feed query -- it reads
// the checkout it is running in. It therefore holds on any clone, including the shallow ones the
// lanes work from.
//
// ⚠ ONE DEFINITION, NOT A COPY. The badge emitter in package main resolves the SAME question with
// releasestamp.PublishedStamp; this guard and that emitter share internal/releasestamp rather than
// each carrying a directory scan. A guard whose notion of "published" had drifted from the emitter's
// would go green while the badges pointed at a release that never shipped -- it would fail in the
// flattering direction, silently, which is this lane's recorded failure mode for replicas
// (2026-09-19: a hand-rolled blanker lost 139 of 8230 index pairs and printed a plausible number).
//
// THE PUBLISH WINDOW, since a second reader asks it first: does this go red between the counter
// bump and the snapshot freeze? No -- push-nuget.ps1 states its own order at line 134, "bump
// version.props -> mint the SIGNED tag -> freeze docs\validation\<version>\ -> VERIFY", all inside
// ONE run, and version.props is committed afterwards to record the release. Both halves are in the
// working tree before anything is committed, so the guard only ever reads them together. A lane that
// committed the counter alone WOULD go red here -- correctly: that is a release whose proof snapshot
// does not exist yet, which is the same shape as the defect below.
//
// POSITIVE CONTROL: restoring <GoBuildNumber> to 3 at this tip must make
// TestPublishedCounterMatchesTheRecordedReleases name version.props, the base, the counter and the
// absent snapshot. Verified at the commit that landed this guard, then restored byte-identical.
// The unit arms below control BOTH directions of the rule without touching the tree.

// counterViolations is the RULE, isolated from the tree so both of its directions can be controlled.
// It returns one line per violation, empty when the counter and the record agree.
//
// base and build are version.props as read; onBase is every recorded release stamp carrying that
// base, oldest first (releasestamp.OnBase's contract).
func counterViolations(base string, build string, onBase []string) []string {
	var violations []string

	if base == "" || build == "" {
		return []string{fmt.Sprintf("version.props does not declare both elements: base %q, counter %q", base, build)}
	}

	if len(onBase) == 0 {
		if build != "0" {
			violations = append(violations, fmt.Sprintf(
				"the counter reads %s but NO release is recorded on base %s: the badge and the publish "+
					"ritual would both name %s.%s, a tag and a proof page that do not exist. H2 resets "+
					"<GoBuildNumber> to 0 when it bumps <GoStdLibVersion>", build, base, base, build))
		}

		return violations
	}

	// ⚠ The LATEST is computed with releasestamp.Compare rather than taken from the end of the
	// slice. OnBase does return oldest-first, so indexing would be correct today — and that is the
	// problem: the guard would then rest on an ordering CONTRACT while appearing to check an order,
	// and a comparator regression would leave this arm green. Recomputing costs nothing and makes
	// the two-digit case below a real exercise of the comparator.
	latest := onBase[0]

	for _, stamp := range onBase[1:] {
		if releasestamp.Compare(stamp, latest) > 0 {
			latest = stamp
		}
	}

	composed := base + "." + build

	if build == "0" {
		violations = append(violations, fmt.Sprintf(
			"the counter reads 0 while %s is recorded under docs/%s/: the next publish would reuse a "+
				"build number that is already taken", latest, releasestamp.DocsDirName))

		return violations
	}

	if composed != latest {
		relation := "behind"

		if releasestamp.Compare(composed, latest) > 0 {
			relation = "ahead of"
		}

		violations = append(violations, fmt.Sprintf(
			"the counter composes %s, which is %s the latest recorded release %s (recorded on this "+
				"base: %s)", composed, relation, latest, strings.Join(onBase, ", ")))
	}

	return violations
}

// TestPublishedCounterRule controls counterViolations in both directions, on the shapes the
// repository has actually been in. No tree is read.
func TestPublishedCounterRule(t *testing.T) {
	for _, c := range []struct {
		name   string
		base   string
		build  string
		onBase []string
		want   string // "" for clean, else a fragment the violation must carry
	}{
		{"the hop state, counter reset", "1.24.13", "0", nil, ""},
		{"the hop state, counter carried", "1.24.13", "3", nil, "NO release is recorded"},
		{"a published line naming its latest", "1.23.12", "3", []string{"1.23.12.1", "1.23.12.2", "1.23.12.3"}, ""},
		{"a published line lagging its record", "1.23.12", "2", []string{"1.23.12.1", "1.23.12.2", "1.23.12.3"}, "behind"},
		{"a published line ahead of its record", "1.23.12", "4", []string{"1.23.12.1", "1.23.12.2", "1.23.12.3"}, "ahead of"},
		{"a reset counter over a published base", "1.23.12", "0", []string{"1.23.12.1"}, "already taken"},
		{"an unreadable version.props", "", "", nil, "does not declare both"},
		// ⚠ The two-digit revision, and it is the arm that exercises the comparator: under a STRING
		// compare 1.23.12.9 is the latest of these, so the guard would call the CORRECT counter
		// "behind" its own record and go red on a clean tree.
		{"a two-digit revision is the latest", "1.23.12", "10", []string{"1.23.12.9", "1.23.12.10"}, ""},
	} {
		t.Run(c.name, func(t *testing.T) {
			got := counterViolations(c.base, c.build, c.onBase)

			if c.want == "" {
				if len(got) != 0 {
					t.Fatalf("want clean, got %v", got)
				}

				return
			}

			if len(got) == 0 {
				t.Fatalf("want a violation carrying %q, got none", c.want)
			}

			if !strings.Contains(strings.Join(got, "\n"), c.want) {
				t.Fatalf("want a violation carrying %q, got %v", c.want, got)
			}
		})
	}
}

// TestPublishedCounterMatchesTheRecordedReleases is the arm that holds THIS tree.
func TestPublishedCounterMatchesTheRecordedReleases(t *testing.T) {
	root := repoRootFromPackageDir(t)
	propsPath := filepath.Join(root, "src", "version.props")

	contents, err := os.ReadFile(propsPath)

	if err != nil {
		t.Fatalf("failed to read %s: %v", propsPath, err)
	}

	base := releasestamp.StdLibVersion(string(contents))
	build := releasestamp.BuildNumber(string(contents))

	snapshots := releasestamp.SnapshotsDir(root)

	// ⚠ ANTI-VACUITY. Every assertion below is over a directory listing, and an empty listing is
	// indistinguishable from a clean tree unless the root itself is checked: a guard pointed at the
	// wrong path would report "nothing published on this base" forever and never go red. This
	// repository has published releases, so the root exists and holds some.
	if info, err := os.Stat(snapshots); err != nil || !info.IsDir() {
		t.Fatalf("VACUOUS: the snapshot root %s is not a directory (%v) -- every arm below would pass "+
			"on an empty listing", snapshots, err)
	}

	all := releasestamp.RecordedStamps(snapshots)

	if len(all) == 0 {
		t.Fatalf("VACUOUS: %s records no release at all, which this repository's history contradicts", snapshots)
	}

	onBase := releasestamp.OnBase(snapshots, base)

	if violations := counterViolations(base, build, onBase); len(violations) != 0 {
		t.Fatalf("%s is out of step with the published record:\n  %s\n\n"+
			"  base %s · counter %s · recorded on this base: %s · newest recorded overall: %s\n"+
			"  H2 resets the counter at the version bump; H11.2 holds existence-plus-monotonicity.",
			propsPath, strings.Join(violations, "\n  "), base, build, describeStamps(onBase),
			releasestamp.Newest(snapshots))
	}

	t.Logf("base %s · counter %s · recorded on this base %s · newest recorded overall %s · %d releases recorded",
		base, build, describeStamps(onBase), releasestamp.Newest(snapshots), len(all))
}

func describeStamps(stamps []string) string {
	if len(stamps) == 0 {
		return "none"
	}

	return strings.Join(stamps, ", ")
}
