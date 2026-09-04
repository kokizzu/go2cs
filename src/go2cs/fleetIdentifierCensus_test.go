// fleetIdentifierCensus_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"bytes"
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"testing"
)

// The owner's standing security order (2026-09-01) keeps real machine names and other
// internal-infrastructure identifiers off every pushed surface: fleet machines are referred to by
// nickname only, and real hostnames, UNC paths carrying them, share names, non-public usernames and
// profile paths stay off GitHub entirely. Both public tips were scrubbed the day the order landed.
//
// It came back. A census on 2026-09-04 found the pattern REINTRODUCED in tracked, already-pushed
// records -- profile paths carrying real account names across a dozen docs, plus a real machine name
// in the fleet roster table -- three days after the scrub, and it was found only because a census
// happened to be commissioned. That is the shape this guard exists for: nothing in the tree could
// see it, so the failure mode is silent, it recurs on whoever writes the next provisioning table or
// pastes the next shell transcript, and the cost of noticing is a human remembering to look.
//
// This is the same invariant-in-the-cheapest-place move projitemsIntegrity_test.go makes: it runs in
// the converter's own `go test ./...`, which every lane already pays for, so a reintroduction is a
// red converter suite at the merge rather than a scrub weeks later.
//
// TWO PASSES, because neither sees what the other does.
//
//	Pass 1, PATH-ANCHORED and structural: a profile directory or network prefix whose identifier
//	segment is not a placeholder. It needs no list of names -- it is the SHAPE that is forbidden --
//	so it catches an account nobody has told this guard about, including a new machine's.
//
//	Pass 2, DENIED TOKEN: a known fleet identifier used OUTSIDE any path -- a directory listing's
//	owner/group column, an "account X" parenthetical, a machine name in a roster row. A
//	path-anchored pattern cannot reach those by construction, and in the measured case five
//	line-hits in five files were visible ONLY to this pass, the roster's machine name among them.
//
// The denylist is stored as SALT-FREE SHA-256 of the lowercased token, never as plaintext: a guard
// that spelled the identifiers it forbids would put them on the pushed surface itself, which is the
// thing being prevented. Hashes are checked against whole tokens and against each dot/hyphen
// component, so a machine name and the account name inside it both match.
//
// What is deliberately NOT flagged: the owner's PUBLIC name, e-mail and GitHub handle. Those are
// published attribution, not infrastructure, and the order names the latter. They are cleared by
// FILE below rather than by line, because line numbers drift and a stale line number silently
// disarms a guard (route #8's shape).

// fleetFinding is one hit. It carries the path, the line and the KIND -- never the offending text,
// so a failing run's own output cannot put an identifier into a build log.
type fleetFinding struct {
	Path string
	Line int
	Kind string
}

func (f fleetFinding) String() string {
	return fmt.Sprintf("%s:%d [%s]", f.Path, f.Line, f.Kind)
}

// fleetDeniedToken is one forbidden identifier, stored as the SHA-256 of its lowercased form.
//
// Len is carried WITH the hash rather than derived beside it, because the scan hashes only tokens of
// a denied length -- that is what keeps this pass cheap over a corpus this size -- and a length kept
// in a second list is a length that can silently disagree with its hash, which would disarm the entry
// while every test still passed. One struct, one entry, no way to add half of it.
//
// Adding a machine or an account spells nothing:
//
//	echo -n "<token>" | tr A-Z a-z | sha256sum
type fleetDeniedToken struct {
	Len  int
	Hash string
	What string
}

var fleetDeniedTokens = []fleetDeniedToken{
	{7, "20befabea93592064aad4d07e1af70c5d6859667e1edffdb591accf60e2993ee", "fleet account name"},
	{8, "deff430814c33ac000dbdf4bd1061321b8387df004594375c947fabf73d3acc1", "fleet account name"},
	{13, "1070b0f89514d6852350c53ac7682edcb2d41f38d66fb95c340cdec08802c74e", "fleet machine name"},
}

// fleetDeniedIndex groups the denylist by token length, so a line's tokens are hashed only when
// their length can possibly match.
func fleetDeniedIndex(toks []fleetDeniedToken) map[int]map[string]string {
	idx := map[int]map[string]string{}
	for _, t := range toks {
		if idx[t.Len] == nil {
			idx[t.Len] = map[string]string{}
		}
		idx[t.Len][t.Hash] = t.What
	}
	return idx
}

// fleetPlaceholderSegments are the redacted or generic segments a profile/network path is ALLOWED to
// carry. Anything opening with a substitution sigil (<, %, $, {, [, () is accepted too.
var fleetPlaceholderSegments = map[string]bool{
	"user": true, "users": true, "username": true, "user-name": true, "youruser": true,
	"profile": true, "profile-root": true, "home": true, "root": true, "name": true,
	"host": true, "hostname": true, "server": true, "share": true, "machine": true,
	"public": true, "default": true, "all users": true, "programdata": true,
	"userprofile": true, "%userprofile%": true, "unc": true, "...": true,
	"foo": true, "bar": true, "baz": true, "example": true, "redacted": true, "placeholder": true,
	"go": true, "gopher": true, "runner": true, "agent": true, "ubuntu": true, "vagrant": true,
	"administrator": true, "admin": true, "ci": true, "build": true, "dev": true,
}

// fleetClearedSegment clears one inspected, non-fleet segment in one file. Keyed by path AND
// segment rather than by line, so an edit above it cannot silently disarm the entry. Every entry
// below was read by eye during the 2026-09-04 census; the reason is the record of that reading.
//
// These twenty-two hits are every path-anchored hit in the tree outside docs/** at the time of the
// scrub, and they collapse to the pairs below because the generic UNC placeholders repeat verbatim
// across the converted platform flavours of the same upstream file.
var fleetClearedSegments = map[string]string{
	// A deliberately fictitious illustrative account in an emitted-XML example.
	"docs/ConversionStrategies-Reference.md|mason": "fictitious name in a documentation example",
	// Upstream Go doc comments and fixtures, carried verbatim into the converted corpus.
	"src/core/runtime/traceback.cs|rsc":           "upstream Go doc comment: an example traceback path",
	"src/core/os/user/linux/lookup_unix.cs|kevin": "upstream Go fixture: a passwd-format line in a doc comment",
}

// fleetClearedTokenFiles clears the DENIED-TOKEN pass for files that legitimately carry the owner's
// PUBLIC name or handle. Cleared by file, with the reason; pass 1 still applies to every one of them,
// so a profile path appearing in these files is still caught.
var fleetClearedTokenFiles = map[string]string{
	"AUTHORS":                                        "published attribution: the owner's public name and e-mail",
	"docs/_config.yml":                               "published site configuration: the owner's public name",
	"src/go2cs/winres/winres.json":                   "published copyright string: the owner's public name",
	"src/go2cs/.vscode/settings.json":                "spell-check dictionary word: the owner's public given name",
	"docs/PLAN-nugetgo.md":                           "a package-registry organisation name that is public on the registry",
	"docs/phase4/SESSION-ROLL-2026-09-01-EVENING.md": "address-style norms using the owner's public given name",
}

var (
	// A profile root followed by its identifier segment, either separator, either platform.
	fleetProfileRe = regexp.MustCompile("(?i)(?:users[\\\\/]+|/home/)([^\\\\/\\s\"'`,;:)\\]}>*|]+)")
	// A UNC prefix followed by a host segment, two characters minimum.
	//
	// The leading group is a hand-rolled lookbehind: RE2 has none, and without it every ESCAPED
	// backslash in a Go or C# string literal reads as a UNC prefix -- "core\\testing\\x",
	// "%SystemRoot%\\system32\\" and a captured "stderr:\\n0x7f..." all matched before this was
	// added, and a guard whose steady state is dozens of false positives is a guard nobody leaves
	// switched on.
	//
	// The rule is positive rather than subtractive, because subtracting only word characters still
	// admitted the `%`- and `:`-preceded cases above: a UNC prefix BEGINS a path token, so it must
	// sit at the start of the line or after whitespace, a quote or an opening delimiter. Submatch 2
	// is the host.
	fleetNetworkRe = regexp.MustCompile("(^|[\\s\"'`(\\[=,])\\\\\\\\([A-Za-z][A-Za-z0-9._-]+)\\\\")
)

// fleetIsUpstreamFixture reports whether a path is converted-upstream or captured test DATA, where a
// generic profile path or UNC example is the content itself: Go's own suites are full of them, and
// the converted corpus carries them verbatim.
//
// Only the STRUCTURAL pass is skipped for these. The denied-token pass still runs, so a real fleet
// identifier reaching a fixture -- an absolute source path baked into an emission, say -- is still
// caught. Skipping both would be the blind spot; skipping neither is 2,500 false positives.
func fleetIsUpstreamFixture(path string) bool {
	p := strings.ToLower(path)
	return strings.Contains(p, "/testdata/") ||
		strings.HasSuffix(p, "_test.cs") ||
		strings.HasSuffix(p, "_test.cs.auto") ||
		strings.HasSuffix(p, ".test")
}

func fleetHash(s string) string {
	sum := sha256.Sum256([]byte(strings.ToLower(s)))
	return hex.EncodeToString(sum[:])
}

func fleetIsPlaceholder(seg string) bool {
	// A single character is a stand-in, not an account: the converter's own spelling guards write
	// `C:\Users\u\sdk\...` precisely to avoid naming one. The denied-token pass covers real names
	// whatever their length, so nothing is lost here.
	if len(seg) < 2 {
		return true
	}
	if strings.ContainsAny(seg[:1], "<%${[(") {
		return true
	}
	return fleetPlaceholderSegments[strings.ToLower(seg)]
}

// scanFleetIdentifiers runs both passes over one file's bytes. denied is a parameter rather than a
// package global so the positive control can drive the denied-token pass with a synthetic entry,
// exercising this exact code path without any test spelling a real identifier.
func scanFleetIdentifiers(path string, content []byte, denied map[int]map[string]string) []fleetFinding {
	if bytes.IndexByte(content, 0) >= 0 {
		return nil // binary
	}
	var out []fleetFinding
	clearedTokens := fleetClearedTokenFiles[path] != ""
	structural := !fleetIsUpstreamFixture(path)

	// Lines are walked in place rather than through strings.Split: this runs over every tracked
	// file in the repository, and materialising a slice of every line of the converted corpus cost
	// more than the matching did.
	rest := content
	for n := 1; len(rest) > 0 || n == 1; n++ {
		var line []byte
		if i := bytes.IndexByte(rest, '\n'); i >= 0 {
			line, rest = rest[:i], rest[i+1:]
		} else {
			line, rest = rest, nil
		}
		if len(line) == 0 {
			if rest == nil {
				break
			}
			continue
		}

		if structural {
			// Cheap prefilters. The regexes are the expensive part and almost no line can match
			// either, so ask a substring question first: a profile path needs a profile root, and a
			// network path needs a doubled backslash.
			if fleetHasFold(line, "users") || bytes.Contains(line, []byte("/home/")) {
				for _, m := range fleetProfileRe.FindAllSubmatch(line, -1) {
					fleetConsiderSegment(&out, path, n, "profile-path", string(m[1]))
				}
			}
			if bytes.Contains(line, []byte(`\\`)) {
				for _, m := range fleetNetworkRe.FindAllSubmatch(line, -1) {
					fleetConsiderSegment(&out, path, n, "network-path", string(m[2]))
				}
			}
		}
		if clearedTokens {
			continue
		}
		if fleetLineHasDeniedToken(line, denied) {
			out = append(out, fleetFinding{path, n, "denied-token"})
		}
	}
	return out
}

func fleetConsiderSegment(out *[]fleetFinding, path string, line int, kind, seg string) {
	if fleetIsPlaceholder(seg) {
		return
	}
	if _, ok := fleetClearedSegments[path+"|"+strings.ToLower(seg)]; ok {
		return
	}
	*out = append(*out, fleetFinding{path, line, kind})
}

// fleetHasFold is a case-insensitive substring test that does not allocate a lowered copy of the
// line; needle must already be lowercase ASCII.
func fleetHasFold(hay []byte, needle string) bool {
	n := len(needle)
	for i := 0; i+n <= len(hay); i++ {
		ok := true
		for j := 0; j < n; j++ {
			c := hay[i+j]
			if c >= 'A' && c <= 'Z' {
				c += 'a' - 'A'
			}
			if c != needle[j] {
				ok = false
				break
			}
		}
		if ok {
			return true
		}
	}
	return false
}

// fleetLineHasDeniedToken walks identifier-shaped runs by hand -- a regexp tokenizer over every line
// of the corpus was the single most expensive thing this guard did -- and hashes a run only when its
// length is one a denied token could have. Each hyphen/dot component is tested as well as the whole
// run, so a machine name and the account name inside it both match.
func fleetLineHasDeniedToken(line []byte, denied map[int]map[string]string) bool {
	isTok := func(c byte) bool {
		return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' ||
			c == '.' || c == '_' || c == '-'
	}
	for i := 0; i < len(line); {
		if !isTok(line[i]) {
			i++
			continue
		}
		j := i
		for j < len(line) && isTok(line[j]) {
			j++
		}
		tok := line[i:j]
		if fleetTokenDenied(tok, denied) {
			return true
		}
		for k, p := 0, 0; k <= len(tok); k++ {
			if k == len(tok) || tok[k] == '-' || tok[k] == '.' {
				if k > p && fleetTokenDenied(tok[p:k], denied) {
					return true
				}
				p = k + 1
			}
		}
		i = j
	}
	return false
}

func fleetTokenDenied(tok []byte, denied map[int]map[string]string) bool {
	byHash, ok := denied[len(tok)]
	if !ok {
		return false
	}
	_, hit := byHash[fleetHash(string(tok))]
	return hit
}

// scanFleetTree scans an explicit list of paths relative to root. Factored out so the positive
// control drives the SAME walk -- binary skip, line splitting and both passes -- over a temporary
// tree, rather than testing a reimplementation of it.
func scanFleetTree(root string, rel []string, denied map[int]map[string]string) ([]fleetFinding, int) {
	var out []fleetFinding
	read := 0
	for _, p := range rel {
		content, err := os.ReadFile(filepath.Join(root, filepath.FromSlash(p)))
		if err != nil {
			continue // unreadable or a submodule entry; never a pass by omission, see the count assert
		}
		read++
		out = append(out, scanFleetIdentifiers(p, content, denied)...)
	}
	sort.Slice(out, func(i, j int) bool {
		if out[i].Path != out[j].Path {
			return out[i].Path < out[j].Path
		}
		return out[i].Line < out[j].Line
	})
	return out, read
}

// repoRootFromPackageDir walks up from src\go2cs to the repository root.
func repoRootFromPackageDir(t *testing.T) string {
	t.Helper()
	wd, err := os.Getwd()
	if err != nil {
		t.Fatalf("cannot determine the working directory: %v", err)
	}
	root := filepath.Dir(filepath.Dir(wd)) // src\go2cs -> src -> repo root
	if _, err := os.Stat(filepath.Join(root, ".git")); err != nil {
		t.Fatalf("repository root not found above %s: %v", wd, err)
	}
	return root
}

// TestNoFleetIdentifiersInTrackedFiles is the guard. It enumerates TRACKED files only -- an
// untracked scratch file is nobody's pushed surface -- and reads them from disk.
//
// The files it reads live outside this module (docs\, src\core\, the repository root), and cmd/go
// drops out-of-module files from the test input hash, so a cached PASS here would survive a
// reintroduction: run the converter suite with -count=1, which is what every gate in this repo
// already does after a harness-only change.
func TestNoFleetIdentifiersInTrackedFiles(t *testing.T) {
	root := repoRootFromPackageDir(t)

	cmd := exec.Command("git", "-C", root, "ls-files", "-z")
	out, err := cmd.Output()
	if err != nil {
		// An instrument that cannot enumerate must not report success.
		t.Fatalf("git ls-files failed in %s: %v", root, err)
	}
	var files []string
	for _, p := range strings.Split(string(out), "\x00") {
		if p != "" {
			files = append(files, p)
		}
	}
	if len(files) < 1000 {
		t.Fatalf("git ls-files returned %d paths, which is too few to be this repository -- "+
			"the enumeration is broken, and a guard that scans nothing passes everything", len(files))
	}

	findings, read := scanFleetTree(root, files, fleetDeniedIndex(fleetDeniedTokens))
	if read < len(files)*9/10 {
		t.Fatalf("read only %d of %d tracked files; the scan has a hole", read, len(files))
	}
	if len(findings) == 0 {
		return
	}

	byFile := map[string]int{}
	for _, f := range findings {
		byFile[f.Path]++
	}
	names := make([]string, 0, len(byFile))
	for p := range byFile {
		names = append(names, p)
	}
	sort.Strings(names)

	var b strings.Builder
	fmt.Fprintf(&b, "%d fleet-identifier hit(s) in %d tracked file(s).\n", len(findings), len(byFile))
	b.WriteString("The owner's 2026-09-01 security order keeps real machine names, non-public\n")
	b.WriteString("usernames and profile paths off every pushed surface. Substitute the identifier\n")
	b.WriteString("ALONE -- <user> for an account segment, the machine's fleet nickname for a host --\n")
	b.WriteString("leaving the rest of the line untouched, so the record still says what it said.\n")
	b.WriteString("A genuinely generic or fictitious segment belongs in fleetPlaceholderSegments or\n")
	b.WriteString("fleetClearedSegments, with the reason, NOT scrubbed.\n")
	for _, p := range names {
		fmt.Fprintf(&b, "  %4d  %s\n", byFile[p], p)
	}
	b.WriteString("Sites (text withheld deliberately -- a failing build log is a pushed surface too):\n")
	for _, f := range findings {
		fmt.Fprintf(&b, "  %s\n", f)
	}
	t.Fatal(b.String())
}

// TestFleetIdentifierScannerFiresAndRestores is the positive control. A guard that has never been
// made to fail proves nothing, and this one's steady state is silence, so its greens are worthless
// until it has been shown to go red on demand.
//
// It plants into a REAL file in a temporary tree and drives the real walk, then restores and asserts
// the restore is byte-identical -- so the control also demonstrates that a clean tree reads clean
// through the same path that reported the hit.
func TestFleetIdentifierScannerFiresAndRestores(t *testing.T) {
	// A synthetic denied token, so the control exercises the denied-token pass without any test
	// source spelling a real fleet identifier.
	const controlToken = "zzcontrolaccount"
	denied := fleetDeniedIndex([]fleetDeniedToken{{len(controlToken), fleetHash(controlToken), "control token"}})

	clean := "toolchain root at C:\\Users\\<user>\\sdk and /home/<user>/go\n" +
		"a UNC share at \\\\host\\share\\path and \\\\server\\share\n" +
		"the roster row names the machine by its nickname i9\n"

	// The planted paths are ASSEMBLED through Sprintf rather than written as literals, because this
	// file is itself a tracked file and the guard scans it: written out, the two plants below were
	// found by the guard reading its own source, which is exactly what the first green-arm run
	// reported. Exempting this file would have been the wrong fix -- it is the file most likely to
	// be edited by whoever adds the next denylist entry, so an exemption here is a permanent hole.
	// A verb leaves `%s` where the segment goes, and `%` is a substitution sigil, so the source text
	// reads as a placeholder while the RUNTIME string carries a real-looking identifier.
	const seg = "zzexampleaccount"
	const host = "zzexamplehost"

	plants := []struct {
		name string
		line string
		kind string
	}{
		{"windows profile path", fmt.Sprintf("root at C:\\Users\\%s\\sdk\n", seg), "profile-path"},
		{"posix home path", fmt.Sprintf("root at /home/%s/go\n", seg), "profile-path"},
		{"unc host", fmt.Sprintf("share at \\\\%s\\public\\x\n", host), "network-path"},
		{"bare denied token", "owner column reads " + controlToken + " here\n", "denied-token"},
		{"denied token inside a machine name", "row names " + controlToken + "-desk2\n", "denied-token"},
	}

	for _, p := range plants {
		t.Run(p.name, func(t *testing.T) {
			dir := t.TempDir()
			rel := "docs/phase4/CONTROL-record.md"
			full := filepath.Join(dir, filepath.FromSlash(rel))
			if err := os.MkdirAll(filepath.Dir(full), 0o755); err != nil {
				t.Fatal(err)
			}
			if err := os.WriteFile(full, []byte(clean), 0o644); err != nil {
				t.Fatal(err)
			}
			original, err := os.ReadFile(full)
			if err != nil {
				t.Fatal(err)
			}

			// GREEN before: the clean record, whose placeholders and nickname are exactly what a
			// scrubbed file looks like, must not fire. Without this arm a scanner that flagged
			// everything would pass the red arm below.
			if got, _ := scanFleetTree(dir, []string{rel}, denied); len(got) != 0 {
				t.Fatalf("clean record fired: %v", got)
			}

			// RED: plant, and require a hit of the RIGHT kind on the RIGHT line.
			if err := os.WriteFile(full, append(append([]byte{}, original...), []byte(p.line)...), 0o644); err != nil {
				t.Fatal(err)
			}
			got, _ := scanFleetTree(dir, []string{rel}, denied)
			if len(got) == 0 {
				t.Fatalf("planted %s was NOT detected -- this guard cannot go red", p.name)
			}
			wantLine := strings.Count(clean, "\n") + 1
			found := false
			for _, f := range got {
				if f.Kind == p.kind && f.Line == wantLine && f.Path == rel {
					found = true
				}
			}
			if !found {
				t.Fatalf("planted %s detected as %v, want kind %q on line %d", p.name, got, p.kind, wantLine)
			}

			// GREEN after restore, and the restore is byte-identical.
			if err := os.WriteFile(full, original, 0o644); err != nil {
				t.Fatal(err)
			}
			back, err := os.ReadFile(full)
			if err != nil {
				t.Fatal(err)
			}
			if !bytes.Equal(back, original) {
				t.Fatal("restore is not byte-identical")
			}
			if got, _ := scanFleetTree(dir, []string{rel}, denied); len(got) != 0 {
				t.Fatalf("restored record still fires: %v", got)
			}
		})
	}
}

// TestFleetIdentifierClearancesAreLive keeps the two allowlists honest. An entry whose file has gone
// (a record renamed, a corpus file relocated by a layout change) is dead weight that reads as
// diligence, and a cleared SEGMENT that no longer appears in its file is a clearance covering
// nothing -- the shape that lets a guard go quietly vacuous.
func TestFleetIdentifierClearancesAreLive(t *testing.T) {
	root := repoRootFromPackageDir(t)
	for key, reason := range fleetClearedSegments {
		parts := strings.SplitN(key, "|", 2)
		if len(parts) != 2 || reason == "" {
			t.Fatalf("malformed clearance %q (want \"path|segment\" and a reason)", key)
		}
		content, err := os.ReadFile(filepath.Join(root, filepath.FromSlash(parts[0])))
		if err != nil {
			t.Errorf("cleared segment names a file that is gone: %s (%v) -- retire the entry", parts[0], err)
			continue
		}
		if !bytes.Contains(bytes.ToLower(content), []byte(strings.ToLower(parts[1]))) {
			t.Errorf("cleared segment %q no longer appears in %s -- retire the entry", parts[1], parts[0])
		}
	}
	for path, reason := range fleetClearedTokenFiles {
		if reason == "" {
			t.Fatalf("clearance for %s has no reason", path)
		}
		if _, err := os.Stat(filepath.Join(root, filepath.FromSlash(path))); err != nil {
			t.Errorf("cleared file is gone: %s (%v) -- retire the entry", path, err)
		}
	}
}
