// stamp.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

// Package releasestamp is the ONE definition of what a published go2cs release stamp is, where the
// repository records one, and how two of them order.
//
// It exists because three separate readers need that answer and two of them cannot see each other:
// the README badge emitter (package main), the H2 counter guard (internal/repoguard, which cannot
// import main), and the toolchain pin reader. Before this package the first two would each have
// carried their own directory scan and their own dotted-number compare.
//
// ⚠ WHY THAT MATTERS HERE SPECIFICALLY, on this lane's own record of 2026-09-19: a replica of a
// text-scanning predicate fails SILENTLY and in the flattering direction. A hand-rolled copy of
// blankCSharpLiterals lost 139 of 8230 index pairs and printed a plausible number; the numerator it
// also printed happened to be right, which is what made it believable. A guard whose rule for "is
// this release published" drifts from the emitter's rule would pass while the badges point at a
// release that never shipped -- the exact defect the guard is there to catch.
//
// The repository's record of a publish is the WRITE-ONCE snapshot directory docs/validation/<stamp>/
// (H11.5), not version.props's arithmetic: version.props states an INTENT that a later step carries
// out, and between the two the composed stamp names a tag and a proof page that do not exist.
package releasestamp

import (
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strconv"
	"strings"
)

const (
	// DocsDirName is the directory under docs/ holding one write-once snapshot per published
	// release, plus the working proof set. Package main aliases its own constant to this one.
	DocsDirName = "validation"

	// CurrentDirName is the WORKING proof set -- rewritten by every -tests run, never a published
	// release. It is skipped by name because it is the one non-release directory that is expected
	// to be there; everything else is skipped by the numeric pattern instead, so a future addition
	// needs no list maintenance.
	CurrentDirName = "current"
)

// The published version lives in version.props as two elements; these mirror push-nuget.ps1's own
// regexes so the converter, the release script and the guard read the file the same way.
var (
	stdLibVersionPattern = regexp.MustCompile(`<GoStdLibVersion>([^<]+)</GoStdLibVersion>`)
	buildNumberPattern   = regexp.MustCompile(`<GoBuildNumber>([^<]+)</GoBuildNumber>`)

	// stampPattern matches a recorded release directory name -- dotted numeric components only, so
	// `current`, `index.md` and any future non-release entry are skipped by shape rather than by a
	// name list.
	stampPattern = regexp.MustCompile(`^[0-9]+(\.[0-9]+)+$`)
)

// SnapshotsDir returns the snapshot root for a repository checkout, given the repository root (the
// directory holding docs/ and src/). One definition of the location, so a reader and a guard cannot
// disagree about where to look.
func SnapshotsDir(repoRoot string) string {
	return filepath.Join(repoRoot, "docs", DocsDirName)
}

// StdLibVersion returns the <GoStdLibVersion> base from version.props contents, or "".
func StdLibVersion(propsContents string) string {
	return firstSubmatch(stdLibVersionPattern, propsContents)
}

// BuildNumber returns the <GoBuildNumber> counter from version.props contents, or "". The counter is
// the LAST-PUBLISHED build number, so 0 means nothing has been published on the current base.
func BuildNumber(propsContents string) string {
	return firstSubmatch(buildNumberPattern, propsContents)
}

// IsStamp reports whether a name has the shape of a release stamp.
func IsStamp(name string) bool {
	return stampPattern.MatchString(name)
}

// Recorded reports whether docs/validation/<stamp>/ exists -- the tree's own record that this exact
// version was published.
func Recorded(snapshotsDir string, stamp string) bool {
	if stamp == "" {
		return false
	}

	info, err := os.Stat(filepath.Join(snapshotsDir, stamp))

	return err == nil && info.IsDir()
}

// RecordedStamps returns every release stamp recorded under snapshotsDir, ordered OLDEST FIRST by
// numeric component. A missing or unreadable directory yields nil -- callers that need to tell "no
// releases" from "wrong path" test the directory themselves; both the emitter and the guard do.
func RecordedStamps(snapshotsDir string) []string {
	entries, err := os.ReadDir(snapshotsDir)

	if err != nil {
		return nil
	}

	var stamps []string

	for _, entry := range entries {
		if !entry.IsDir() || entry.Name() == CurrentDirName || !IsStamp(entry.Name()) {
			continue
		}

		stamps = append(stamps, entry.Name())
	}

	sort.Slice(stamps, func(i int, j int) bool { return Compare(stamps[i], stamps[j]) < 0 })

	return stamps
}

// Newest returns the greatest recorded release stamp, or "" when none is recorded.
func Newest(snapshotsDir string) string {
	stamps := RecordedStamps(snapshotsDir)

	if len(stamps) == 0 {
		return ""
	}

	return stamps[len(stamps)-1]
}

// OnBase returns the recorded stamps whose base is exactly base -- that is, base plus ONE further
// component. Oldest first. `1.24.13` does not match `1.24.130.1`, because the split is on components
// and not on a string prefix.
func OnBase(snapshotsDir string, base string) []string {
	if base == "" {
		return nil
	}

	want := strings.Split(base, ".")

	var stamps []string

	for _, stamp := range RecordedStamps(snapshotsDir) {
		parts := strings.Split(stamp, ".")

		if len(parts) != len(want)+1 {
			continue
		}

		if strings.Join(parts[:len(want)], ".") == base {
			stamps = append(stamps, stamp)
		}
	}

	return stamps
}

// Compare orders two dotted release stamps by NUMERIC component, never as strings -- which is what
// the monotonicity rule of H11.2 rests on. Returns >0 when a is later, <0 when b is, 0 when equal.
//
// ⚠ THE TRAP IS NOT VISIBLE IN TODAY'S DATA, which is exactly why it is written down. This
// repository currently records 1.23.1.2-1.23.1.7 and 1.23.12.1-1.23.12.3, and a lexical maximum
// over THOSE happens to return the right answer (`1.23.12.1` > `1.23.1.7` as a string too, since
// '2' > '.'). It goes wrong the first time any component reaches two digits on the LOW side of the
// comparison:
//
//	1.23.12.10  vs  1.23.12.9   lexical picks .9  -- a counter reaching double digits, which
//	                                                 H2's per-release counter makes routine
//	1.23.12.1   vs  1.23.9.1    lexical picks 1.23.9.1 -- a two-digit patch, which 1.23.12 already is
//
// So a lexical implementation passes every check anyone would run against the current tree and
// starts targeting a superseded release on the tenth publish of a base.
func Compare(a string, b string) int {
	left := strings.Split(a, ".")
	right := strings.Split(b, ".")

	for i := 0; i < len(left) || i < len(right); i++ {
		lv, rv := 0, 0

		if i < len(left) {
			lv, _ = strconv.Atoi(left[i])
		}

		if i < len(right) {
			rv, _ = strconv.Atoi(right[i])
		}

		if lv != rv {
			return lv - rv
		}
	}

	return 0
}

// PublishedStamp resolves the release stamp a README badge may honestly target:
//
//	the composed stamp has a snapshot directory -> use it (an actually-published release)
//	it does not                                 -> use the NEWEST recorded snapshot (the last real one)
//	there are no snapshots at all               -> "" (no honest target exists)
//
// A hop therefore keeps every badge pointing at the previous release until the first package of the
// new base publishes, which is the only target that resolves for a reader clicking it today.
func PublishedStamp(propsContents string, snapshotsDir string) string {
	base := StdLibVersion(propsContents)
	build := BuildNumber(propsContents)

	if base == "" || build == "" {
		return ""
	}

	composed := base + "." + build

	if Recorded(snapshotsDir, composed) {
		return composed
	}

	return Newest(snapshotsDir)
}

func firstSubmatch(pattern *regexp.Regexp, contents string) string {
	if match := pattern.FindStringSubmatch(contents); match != nil {
		return strings.TrimSpace(match[1])
	}

	return ""
}
