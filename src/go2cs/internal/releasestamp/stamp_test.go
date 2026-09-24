// stamp_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package releasestamp

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// These control the SELECTION surface -- which directory names are releases, which belong to a given
// base, and in what order they come back. The RESOLUTION rule on top of them (composed stamp, else
// newest, else none) is controlled from the badge emitter's own arms in
// TestPublishedStampFollowsTheRecordedSnapshot, where the defect it cures is visible.

// snapshotsDir writes a docs/validation/ shaped directory holding exactly the named entries. A name
// ending in ".md" is written as a FILE, so the arms see the real mixture the repository has.
func snapshotsDir(t *testing.T, names ...string) string {
	t.Helper()

	dir := filepath.Join(t.TempDir(), "docs", DocsDirName)

	if err := os.MkdirAll(dir, 0o755); err != nil {
		t.Fatalf("failed to build the snapshot root: %v", err)
	}

	for _, name := range names {
		if strings.HasSuffix(name, ".md") {
			if err := os.WriteFile(filepath.Join(dir, name), []byte("#\n"), 0o644); err != nil {
				t.Fatalf("failed to write %s: %v", name, err)
			}

			continue
		}

		if err := os.MkdirAll(filepath.Join(dir, name), 0o755); err != nil {
			t.Fatalf("failed to write %s: %v", name, err)
		}
	}

	return dir
}

func TestRecordedStampsSelectsAndOrders(t *testing.T) {
	// The real repository's mixture at 1.24.13: nine release directories, the working proof set and
	// the index file. Deliberately supplied out of order.
	dir := snapshotsDir(t, "1.23.12.2", "index.md", "1.23.1.7", CurrentDirName, "1.23.12.10", "1.23.1.2")

	got := RecordedStamps(dir)
	want := []string{"1.23.1.2", "1.23.1.7", "1.23.12.2", "1.23.12.10"}

	if strings.Join(got, " ") != strings.Join(want, " ") {
		t.Fatalf("RecordedStamps = %v, want %v", got, want)
	}

	// ⚠ Both non-release entries must be gone for DIFFERENT reasons: `current` by name, because it
	// is the one non-release directory that is always expected; `index.md` by shape, because it is
	// not a directory AND not a stamp. An arm that only counted would pass with either rule broken.
	for _, name := range got {
		if name == CurrentDirName || strings.HasSuffix(name, ".md") {
			t.Errorf("RecordedStamps returned the non-release entry %q", name)
		}
	}

	// ⚠ A LEXICAL MAXIMUM OVER THIS SET RETURNS 1.23.12.2 -- checked, not assumed: the strings order
	// 1.23.1.2 < 1.23.1.7 < 1.23.12.10 < 1.23.12.2, because "10" sorts below "2" one character at a
	// time. The two-digit revision is what makes this set able to tell the two implementations apart.
	if Newest(dir) != "1.23.12.10" {
		t.Errorf("Newest = %q, want 1.23.12.10 (a lexical maximum would return 1.23.12.2)", Newest(dir))
	}

	if stamps := RecordedStamps(filepath.Join(dir, "does-not-exist")); stamps != nil {
		t.Errorf("an absent snapshot root must yield nil, got %v", stamps)
	}

	if Newest(filepath.Join(dir, "does-not-exist")) != "" {
		t.Errorf("an absent snapshot root has no newest release")
	}
}

// TestOnBaseSplitsComponentsNotStrings is the arm that the H2 counter guard rests on: it asks which
// releases carry the base version.props names. A PREFIX test answers that wrongly in two directions
// at once, and both wrong answers are plausible.
func TestOnBaseSplitsComponentsNotStrings(t *testing.T) {
	dir := snapshotsDir(t, "1.23.12.1", "1.23.12.3", "1.23.1.7", "1.24.130.1", "1.24.13.2", "1.24.13", "1.24.13.2.1")

	t.Run("a longer base is not a member of a shorter one", func(t *testing.T) {
		// ⚠ `1.24.130.1` begins with the characters of `1.24.13`. Under a prefix test the guard would
		// read a publish on the new base that never happened, and go green on a carried counter --
		// the exact state this whole seat cures.
		got := OnBase(dir, "1.24.13")

		if strings.Join(got, " ") != "1.24.13.2" {
			t.Fatalf("OnBase(1.24.13) = %v, want [1.24.13.2]", got)
		}
	})

	t.Run("a deeper stamp is not a release on this base either", func(t *testing.T) {
		// ⚠ THE SECOND HALF OF THE RULE, and the half a dotted-prefix test still gets wrong:
		// `1.24.13.2.1` begins with `1.24.13.` and is NOT a release on 1.24.13 -- it carries two
		// further components where a release carries one. len(parts) == len(want)+1 says so; neither
		// HasPrefix(stamp, base) nor HasPrefix(stamp, base+".") does.
		for _, stamp := range OnBase(dir, "1.24.13") {
			if stamp == "1.24.13.2.1" {
				t.Error("OnBase returned a stamp two components deeper than a release on this base")
			}
		}
	})

	t.Run("the base itself is not one of its own releases", func(t *testing.T) {
		// `1.24.13` is a directory here and IS a stamp by shape, but it carries no revision
		// component, so it is not a release ON that base. len(parts) == len(want)+1 is what says so.
		for _, stamp := range OnBase(dir, "1.24.13") {
			if stamp == "1.24.13" {
				t.Error("OnBase returned the bare base as a release on itself")
			}
		}
	})

	t.Run("a base with several releases comes back oldest first", func(t *testing.T) {
		got := OnBase(dir, "1.23.12")

		if strings.Join(got, " ") != "1.23.12.1 1.23.12.3" {
			t.Fatalf("OnBase(1.23.12) = %v, want [1.23.12.1 1.23.12.3]", got)
		}
	})

	t.Run("an unpublished base has none", func(t *testing.T) {
		if got := OnBase(dir, "1.25.1"); got != nil {
			t.Fatalf("OnBase(1.25.1) = %v, want none", got)
		}
	})

	t.Run("an empty base matches nothing rather than everything", func(t *testing.T) {
		if got := OnBase(dir, ""); got != nil {
			t.Fatalf(`OnBase("") = %v, want none -- an unread version.props must not look published`, got)
		}
	})
}

func TestVersionPropsElementsAreReadAsWritten(t *testing.T) {
	props := "<Project>\r\n  <PropertyGroup>\r\n    <GoStdLibVersion>1.24.13</GoStdLibVersion>\r\n" +
		"    <GoBuildNumber>0</GoBuildNumber>\r\n    <Version>$(GoStdLibVersion).$(GoBuildNumber)</Version>\r\n" +
		"  </PropertyGroup>\r\n</Project>\r\n"

	if got := StdLibVersion(props); got != "1.24.13" {
		t.Errorf("StdLibVersion = %q, want 1.24.13", got)
	}

	// ⚠ The counter is read as TEXT, never as a number: "0" and "" are different answers and the
	// callers act differently on them. A reader returning 0 for an absent element would report an
	// unpublished base where the truth is an unreadable file.
	if got := BuildNumber(props); got != "0" {
		t.Errorf("BuildNumber = %q, want \"0\"", got)
	}

	if got := BuildNumber("<Project />"); got != "" {
		t.Errorf("an absent counter must read \"\", got %q", got)
	}

	if got := StdLibVersion("<Project />"); got != "" {
		t.Errorf("an absent base must read \"\", got %q", got)
	}

	// The $(…) composition line carries both element names as text and must not be mistaken for a
	// declaration of either.
	if got := StdLibVersion("<Version>$(GoStdLibVersion).$(GoBuildNumber)</Version>"); got != "" {
		t.Errorf("the composition line is not a declaration, got %q", got)
	}
}
