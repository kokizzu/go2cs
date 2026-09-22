// coreReferencesResolve_test.go - Gbtc
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
	"os/exec"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"testing"
)

// A committed project file that names a core/ directory the tree no longer has is invisible to every
// pipeline run: a converting -tests action rewrites the tests csproj before it builds, so nothing that
// the campaign runs ever resolves the stale reference. It surfaces only when someone builds the
// committed file by hand, as an unresolved ProjectReference. The 1.23 -> 1.24 hop removed std package
// directories (runtime/internal/math, crypto/internal/nistec, ...) and left 44 such references in 41
// committed tests csprojs (coord-queue-q84; i9's census at 45c6b94465, C2's b779b440a3). This guard
// holds that set: it may shrink as the H8 regeneration rewrites those files, and it may never grow.
//
// "Exists" means the TREE HAS TRACKED FILES under the directory, never os.Stat. Measured on a lane box:
// src/core/crypto/internal/nistec survives as an EMPTY directory git does not track, so a stat-based
// check reads it as present and drops its three references -- a guard whose verdict depends on the
// clone's history rather than on the commit.
//
// A reference's directory is the referenced path with a trailing FILE name removed. A file is
// recognised by its extension, not by any dot in the last segment, because core/ directory names can
// carry dots (vendor/golang.org).
//
// POSITIVE CONTROL: TestCoreReferenceScannerFires drives the same scanner over a synthetic tracked
// tree: an absent reference in both slash spellings, an absent directory that exists EMPTY on disk,
// and a present reference. The first two must be reported and the third must not.

// coreReferencePattern matches a $(go2csPath)core reference in either slash spelling, up to the first
// quote, semicolon, angle bracket or whitespace.
var coreReferencePattern = regexp.MustCompile(`\$\(go2csPath\)core[/\\]([^"';<>\s]+)`)

var coreReferenceFileExtensions = []string{".csproj", ".projitems", ".props", ".targets", ".cs", ".dll", ".json"}

// staleCoreReferences is the declared set: "<tracked project file> -> <absent core directory>", both
// relative to src/core. Every row is a tests csproj the H8 regeneration rewrites. When a row's
// reference is gone, delete the row IN THE SAME COMMIT; a new absent reference is never added here --
// fix the file instead.
var staleCoreReferences = []string{
	"crypto/tls/crypto.tls.tests.csproj -> crypto/internal/mlkem768",
	"crypto/tls/crypto.tls.tests.csproj -> runtime/internal/math",
	"fmt/fmt.tests.csproj -> runtime/internal/math",
	"internal/trace/internal.trace.tests.csproj -> runtime/internal/math",
	"math/rand/math.rand.tests.csproj -> runtime/internal/math",
	"mime/multipart/mime.multipart.tests.csproj -> runtime/internal/math",
	"net/http/net.http.tests.csproj -> runtime/internal/math",
	"time/time.tests.csproj -> runtime/internal/math",
}

type coreReferenceScan struct {
	projectFiles int
	references   int
	trackedDirs  int
	absent       []string // "<project file> -> <directory>", relative to src/core, sorted, distinct
}

// scanCoreReferences reads every tracked .csproj/.projitems under src/ from root and reports each core/
// directory it names that has no tracked file under it. tracked is `git ls-files` output (forward
// slashes, relative to root).
func scanCoreReferences(t *testing.T, root string, tracked []string) coreReferenceScan {
	t.Helper()

	dirs := map[string]bool{}
	var projects []string

	for _, p := range tracked {
		if rest, ok := strings.CutPrefix(p, "src/core/"); ok {
			for d := filepath.ToSlash(filepath.Dir(rest)); d != "." && d != "/"; d = filepath.ToSlash(filepath.Dir(d)) {
				dirs[d] = true
			}
		}

		if strings.HasPrefix(p, "src/") && (strings.HasSuffix(p, ".csproj") || strings.HasSuffix(p, ".projitems")) {
			projects = append(projects, p)
		}
	}

	scan := coreReferenceScan{projectFiles: len(projects), trackedDirs: len(dirs)}
	seen := map[string]bool{}

	for _, p := range projects {
		content, err := os.ReadFile(filepath.Join(root, filepath.FromSlash(p)))

		if err != nil {
			t.Fatalf("cannot read tracked project file %s: %v", p, err)
		}

		for _, m := range coreReferencePattern.FindAllStringSubmatch(string(content), -1) {
			scan.references++

			dir := strings.TrimSuffix(strings.ReplaceAll(m[1], `\`, "/"), "/")

			if slash := strings.LastIndex(dir, "/"); slash >= 0 {
				for _, ext := range coreReferenceFileExtensions {
					if strings.HasSuffix(strings.ToLower(dir[slash+1:]), ext) {
						dir = dir[:slash]
						break
					}
				}
			}

			if dirs[dir] {
				continue
			}

			row := strings.TrimPrefix(p, "src/core/") + " -> " + dir

			if !seen[row] {
				seen[row] = true
				scan.absent = append(scan.absent, row)
			}
		}
	}

	sort.Strings(scan.absent)

	return scan
}

func gitTrackedFiles(t *testing.T, root string) []string {
	t.Helper()

	out, err := exec.Command("git", "-C", root, "ls-files", "-z").Output()

	if err != nil {
		t.Fatalf("git ls-files failed in %s: %v", root, err)
	}

	var files []string

	for _, p := range strings.Split(string(out), "\x00") {
		if p != "" {
			files = append(files, p)
		}
	}

	return files
}

// TestCommittedCoreReferencesResolve is the guard.
func TestCommittedCoreReferencesResolve(t *testing.T) {
	root := repoRootFromPackageDir(t)
	tracked := gitTrackedFiles(t, root)

	if len(tracked) < 1000 {
		t.Fatalf("git ls-files returned %d paths, too few to be this repository; a guard that scans nothing passes everything", len(tracked))
	}

	scan := scanCoreReferences(t, root, tracked)

	if scan.projectFiles < 300 || scan.references < 1000 || scan.trackedDirs < 300 {
		t.Fatalf("VACUOUS: %d project files, %d core references, %d tracked core directories; the scan is measuring nothing",
			scan.projectFiles, scan.references, scan.trackedDirs)
	}

	t.Logf("project files %d · core references %d · tracked core directories %d · references to an absent directory %d",
		scan.projectFiles, scan.references, scan.trackedDirs, len(scan.absent))

	declared := map[string]bool{}

	for _, row := range staleCoreReferences {
		if declared[row] {
			t.Fatalf("staleCoreReferences declares %q twice", row)
		}

		declared[row] = true
	}

	measured := map[string]bool{}

	for _, row := range scan.absent {
		measured[row] = true

		if !declared[row] {
			t.Errorf("UNDECLARED STALE REFERENCE: %s\n"+
				"a committed project file names a core/ directory with no tracked files under it. Fix the reference "+
				"(or regenerate the tests csproj); never add it to staleCoreReferences, which only shrinks", row)
		}
	}

	for _, row := range staleCoreReferences {
		if !measured[row] {
			t.Errorf("DECLARED BUT NOT MEASURED: %s\n"+
				"the reference resolves now or is gone. Delete its row from staleCoreReferences IN THE SAME COMMIT "+
				"that changed the project file", row)
		}
	}

	t.Logf("declared %d · measured %d", len(staleCoreReferences), len(scan.absent))
}

// TestCoreReferenceScannerFires is the positive control: the same scanner over a synthetic tracked tree.
func TestCoreReferenceScannerFires(t *testing.T) {
	root := t.TempDir()

	write := func(rel, content string) {
		full := filepath.Join(root, filepath.FromSlash(rel))

		if err := os.MkdirAll(filepath.Dir(full), 0o755); err != nil {
			t.Fatal(err)
		}

		if err := os.WriteFile(full, []byte(content), 0o644); err != nil {
			t.Fatal(err)
		}
	}

	write("src/core/present/present.csproj", "<Project />\n")
	write("src/core/vendor/golang.org/x/dotted/vendor.golang.org.x.dotted.csproj", "<Project />\n")
	write("src/core/consumer/consumer.tests.csproj", `<Project>
  <ItemGroup>
    <ProjectReference Include="$(go2csPath)core/present/present.csproj" />
    <ProjectReference Include="$(go2csPath)core/vendor/golang.org/x/dotted/vendor.golang.org.x.dotted.csproj" />
    <ProjectReference Include="$(go2csPath)core/gone/forward/gone.forward.csproj" />
    <ProjectReference Include="$(go2csPath)core\gone\back\gone.back.csproj" />
    <ProjectReference Include="$(go2csPath)core/emptyondisk/emptyondisk.csproj" />
  </ItemGroup>
</Project>
`)

	// Present on disk, tracked by nothing: the trap a stat-based check falls into.
	if err := os.MkdirAll(filepath.Join(root, "src", "core", "emptyondisk"), 0o755); err != nil {
		t.Fatal(err)
	}

	for _, args := range [][]string{{"init", "-q"}, {"add", "."}} {
		if out, err := exec.Command("git", append([]string{"-C", root}, args...)...).CombinedOutput(); err != nil {
			t.Fatalf("git %v failed: %v\n%s", args, err, out)
		}
	}

	scan := scanCoreReferences(t, root, gitTrackedFiles(t, root))

	want := []string{
		"consumer/consumer.tests.csproj -> emptyondisk",
		"consumer/consumer.tests.csproj -> gone/back",
		"consumer/consumer.tests.csproj -> gone/forward",
	}

	if scan.references != 5 {
		t.Fatalf("control matched %d references, want 5; the pattern regressed", scan.references)
	}

	if fmt.Sprint(scan.absent) != fmt.Sprint(want) {
		t.Fatalf("control reported %v\nwant %v", scan.absent, want)
	}
}
