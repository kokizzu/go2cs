// validationProofPages.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file turns the compare oracle's per-test differential — the record that today lives only in
// the gitignored go2cs_test_comparison.json — into a committed, human-readable PROOF PAGE per
// validated package (docs/validation/current/<dot-id>.md) plus a generated roster index.
// Design: docs/Phase4/DESIGN-validation-proof-pages.md.
//
// Two properties are load-bearing:
//
//  1. DETERMINISM — identical inputs render byte-identical output. Every map is walked through a
//     sorted key slice; Go's map iteration order must never reach the page. The converter is
//     byte-deterministic and this feature is not allowed to be the exception.
//
//  2. CONTENT STABILITY — a routine re-validation sweep must leave docs/validation untouched. The
//     page's only volatile text is its provenance line (validation date + converter commit); the
//     writer compares everything ELSE against the file already on disk and skips the write when it
//     is unchanged. Provenance therefore moves only when a VERDICT moves — i.e. at banking — which
//     keeps the proof pages out of every sweep's drift report by construction rather than by
//     membership in some restore-unless-rebanked class.

package main

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"time"
)

const (
	validationDocsDirName    = "validation"
	validationCurrentDirName = "current"
	validationIndexFileName  = "index.md"

	// proofProvenancePrefix identifies the ONE volatile line of a proof page. Everything else is
	// the content-stable portion the writer compares before deciding to rewrite.
	proofProvenancePrefix = "*Validated "

	// proofTotalsFormat is the single authority for the shape of the page's totals line. The
	// renderer writes it and the README badge emitter reads it back — proofTotalsPattern below is
	// DERIVED from this very string, so a badge can never claim counts in a shape the page no
	// longer renders.
	proofTotalsFormat = "**%d matched · %d disclosed**"

	go2csRepositoryURL = "https://github.com/ritchiecarroll/go2cs"
)

// proofTotalsPattern matches a rendered totals line, built from proofTotalsFormat by escaping its
// literal text and turning each %d into a capture group. It is deliberately not anchored at the end:
// the rendered line continues with the Go version and platform.
var proofTotalsPattern = regexp.MustCompile(`^` + strings.ReplaceAll(regexp.QuoteMeta(proofTotalsFormat), `%d`, `(\d+)`))

// parseProofTotals recovers the matched and disclosed counts from a rendered proof page. CRs are
// ignored so a page smudged to CRLF by autocrlf reads the same as freshly rendered text.
func parseProofTotals(page string) (int, int, bool) {
	for _, line := range strings.Split(strings.ReplaceAll(page, "\r", ""), "\n") {
		match := proofTotalsPattern.FindStringSubmatch(line)

		if match == nil {
			continue
		}

		matched, matchedErr := strconv.Atoi(match[1])
		disclosed, disclosedErr := strconv.Atoi(match[2])

		if matchedErr != nil || disclosedErr != nil {
			continue
		}

		return matched, disclosed, true
	}

	return 0, 0, false
}

// proofPageProvenance carries the page header's non-verdict facts. Date and commit are the volatile
// pair (stripped before the stability compare); import path, Go version and platform are CONTENT —
// a Go-version or platform change is a different claim and must rewrite the page.
type proofPageProvenance struct {
	importPath string
	goVersion  string
	platform   string
	date       string
	commit     string
}

// validationProofDotID maps a package import path to its page name — "/" becomes "." so the pages
// live flat under current/ (mirroring the NuGet ids: io, path.filepath, math.rand.v2).
func validationProofDotID(importPath string) string {
	return strings.ReplaceAll(importPath, "/", ".")
}

// validationProofImportPath is the inverse used by the index generator, which only has filenames to
// work with. Exact for every Go standard-library import path (no element of one contains a dot).
func validationProofImportPath(dotID string) string {
	return strings.ReplaceAll(dotID, ".", "/")
}

// escapeProofCell makes an arbitrary manifest string safe inside a Markdown table cell: a literal
// pipe would end the column and an embedded newline would end the row.
func escapeProofCell(value string) string {
	value = strings.ReplaceAll(value, "\r\n", " ")
	value = strings.ReplaceAll(value, "\n", " ")
	value = strings.ReplaceAll(value, "|", `\|`)

	return strings.TrimSpace(value)
}

// proofVerdictNames returns the sorted union of the two verdict maps — every test the comparison
// saw, from either side. On a validated comparison the two key sets are identical (a one-sided row
// is a mismatch and never validates), but the union is what makes the renderer honest if that ever
// changes.
func proofVerdictNames(comparison testComparison) []string {
	seen := HashSet[string]{}
	names := make([]string, 0, len(comparison.Go)+len(comparison.CSharp))

	for name := range comparison.Go {
		if seen.Add(name) {
			names = append(names, name)
		}
	}

	for name := range comparison.CSharp {
		if seen.Add(name) {
			names = append(names, name)
		}
	}

	sort.Strings(names)

	return names
}

// proofDisclosedNames returns the sorted tests whose two verdicts DISAGREE. On a validated
// comparison that set is exactly the disclosed-divergent set: every non-agreeing pair the oracle
// did not reclassify against a pinned signature is a mismatch, and a mismatch never validates. It
// is derived from the verdicts rather than parsed back out of the comparison's formatted
// `name (class): reason` strings, so a disclosed ancestor rolled up from disclosed subtests (which
// has no manifest entry of its own, and therefore an empty class and reason) is still counted.
func proofDisclosedNames(comparison testComparison, names []string) []string {
	disclosed := make([]string, 0, len(comparison.Disclosed))

	for _, name := range names {
		if comparison.Go[name] != comparison.CSharp[name] {
			disclosed = append(disclosed, name)
		}
	}

	return disclosed
}

// proofVerdict renders one side's terminal status, or an em dash when that side never reported the
// test at all (only reachable on a comparison that did not validate).
func proofVerdict(results map[string]string, name string) string {
	if status, ok := results[name]; ok {
		return status
	}

	return "&mdash;"
}

// renderValidationProofPage renders the whole page. It is a pure function of its arguments — no
// clock, no filesystem, no git — so the golden test compares text, and so re-rendering the same
// comparison twice is byte-identical by construction.
func renderValidationProofPage(provenance proofPageProvenance, comparison testComparison, disclosures map[string]testDisclosure) string {
	names := proofVerdictNames(comparison)
	disclosed := proofDisclosedNames(comparison, names)

	skipped := 0
	for _, name := range names {
		if comparison.Go[name] == "skip" && comparison.CSharp[name] == "skip" {
			skipped++
		}
	}

	var page strings.Builder

	fmt.Fprintf(&page, "# `%s` — validation proof\n\n", provenance.importPath)
	fmt.Fprintf(&page, "Go's own `%s` test suite, converted to C# by [go2cs](%s), built against the converted standard\n", provenance.importPath, go2csRepositoryURL)
	page.WriteString("library, run under the Go-semantics test host, and compared verdict for verdict against a clean\n")
	page.WriteString("`go test -json` baseline of the same sources. This page is generated by the converter from that\n")
	fmt.Fprintf(&page, "comparison — it is the evidence behind the `%s` row in\n", provenance.importPath)
	page.WriteString("[Validated Test Packages](../../ValidatedTestPackages.md).\n\n")

	// The provenance line — the page's only volatile text. Everything below it is compared before
	// a rewrite, so a re-validation that reproduces the same verdicts never touches this file.
	if provenance.commit != "" {
		fmt.Fprintf(&page, "%s%s · converter `%s`*\n\n", proofProvenancePrefix, provenance.date, provenance.commit)
	} else {
		fmt.Fprintf(&page, "%s%s*\n\n", proofProvenancePrefix, provenance.date)
	}

	fmt.Fprintf(&page, proofTotalsFormat+" — Go %s, `%s`, converted package\n",
		len(names)-len(disclosed), len(disclosed), provenance.goVersion, provenance.platform)
	fmt.Fprintf(&page, "[`src/core/%s`](%s/tree/master/src/core/%s).\n", provenance.importPath, go2csRepositoryURL, provenance.importPath)

	if skipped > 0 {
		fmt.Fprintf(&page, "\nBoth runtimes skip %d of the matched tests identically.\n", skipped)
	}

	page.WriteString("\n## Verdicts\n\n")
	page.WriteString("| Test | `go test` | go2cs |\n")
	page.WriteString("|:--|:--:|:--:|\n")

	disclosedNames := HashSet[string]{}
	for _, name := range disclosed {
		disclosedNames.Add(name)
	}

	for _, name := range names {
		csharp := proofVerdict(comparison.CSharp, name)

		if disclosedNames.Contains(name) {
			csharp += " ([disclosed](#disclosed-divergences))"
		}

		fmt.Fprintf(&page, "| `%s` | %s | %s |\n", escapeProofCell(name), proofVerdict(comparison.Go, name), csharp)
	}

	if len(disclosed) > 0 {
		page.WriteString("\n## Disclosed divergences\n\n")
		page.WriteString("A disclosed divergence is a specific Go assertion the managed CLR *provably cannot* satisfy — not\n")
		page.WriteString("a skipped test and not a tolerance. Each one is pinned by exact failure signature in the package's\n")
		fmt.Fprintf(&page, "hand-owned [`go2cs_test_disclosures.json`](%s/blob/master/src/core/%s/go2cs_test_disclosures.json);\n",
			go2csRepositoryURL, provenance.importPath)
		page.WriteString("a disclosed test that fails any *other* way is still a hard mismatch.\n\n")
		page.WriteString("| Test | Class | Pinned reason |\n")
		page.WriteString("|:--|:--|:--|\n")

		for _, name := range disclosed {
			disclosure, pinned := disclosures[name]
			class, reason := disclosure.Class, disclosure.Reason

			if !pinned {
				// A parent whose Go=pass/C#=fail divergence is purely the roll-up of disclosed
				// subtests carries no manifest entry of its own (matchTerminalStatuses' aggregation
				// rule), so say what it actually is rather than rendering two empty cells.
				class = "aggregate"
				reason = "no failure text of its own — the roll-up of this test's disclosed subtests"
			}

			fmt.Fprintf(&page, "| `%s` | `%s` | %s |\n", escapeProofCell(name), escapeProofCell(class), escapeProofCell(reason))
		}
	}

	if len(comparison.Excluded) > 0 {
		excluded := make([]string, len(comparison.Excluded))
		copy(excluded, comparison.Excluded)
		sort.Strings(excluded)

		page.WriteString("\n## Excluded declarations\n\n")
		page.WriteString("Declarations filtered from **both** sides of the comparison, and therefore not claimed above:\n")
		page.WriteString("`Benchmark`, `Fuzz` and `Example` declarations the converted host does not execute, plus any\n")
		page.WriteString("test requiring a capability the managed runtime does not provide — a `testing` member the host\n")
		page.WriteString("has not implemented, or a platform behavior it provably cannot reproduce. Each is named with\n")
		page.WriteString("the capability it needs.\n\n")

		for _, entry := range excluded {
			fmt.Fprintf(&page, "- %s\n", strings.TrimSpace(entry))
		}
	}

	return page.String()
}

// renderValidationIndex renders the roster page from the dot-ids present under current/.
func renderValidationIndex(dotIDs []string) string {
	sorted := make([]string, len(dotIDs))
	copy(sorted, dotIDs)
	sort.Strings(sorted)

	var page strings.Builder

	page.WriteString("# Validation proofs\n\n")
	page.WriteString("One page per validated package — the full per-test differential behind its row in\n")
	page.WriteString("[Validated Test Packages](../ValidatedTestPackages.md): every eligible `Test` function, `go test`'s\n")
	page.WriteString("verdict and go2cs's, side by side. The converter writes these pages itself, from the same\n")
	page.WriteString("comparison record that decides whether a package validates at all.\n\n")
	page.WriteString("Pages under `current/` are living proof — regenerated only when a package's verdicts change.\n")
	page.WriteString("Versioned sibling directories are frozen publication snapshots: written once at release and never\n")
	page.WriteString("rewritten, so the proof link for a published package stays the proof as of that binary.\n\n")

	if len(sorted) == 0 {
		page.WriteString("*No proof pages have been generated yet.*\n")

		return page.String()
	}

	page.WriteString("| Package | Proof | Converted package |\n")
	page.WriteString("|:--|:--|:--|\n")

	for _, dotID := range sorted {
		importPath := validationProofImportPath(dotID)
		fmt.Fprintf(&page, "| `%s` | [`%s.md`](%s/%s.md) | [`src/core/%s`](%s/tree/master/src/core/%s) |\n",
			importPath, dotID, validationCurrentDirName, dotID, importPath, go2csRepositoryURL, importPath)
	}

	return page.String()
}

// proofStableContent strips a page down to the text the stability rule compares: the provenance
// line is removed, and CRs are dropped so a file smudged to CRLF by autocrlf still compares equal
// to freshly rendered text.
func proofStableContent(page string) string {
	lines := strings.Split(strings.ReplaceAll(page, "\r", ""), "\n")
	stable := make([]string, 0, len(lines))

	for _, line := range lines {
		if strings.HasPrefix(line, proofProvenancePrefix) {
			continue
		}

		stable = append(stable, line)
	}

	return strings.Join(stable, "\n")
}

// writeStableDocFile writes page to fileName unless the file already holds the same content-stable
// text — the rule that keeps routine sweeps out of docs/validation. Returns whether it wrote.
// Output is CRLF, matching the converter's other generated text (README.md) and the docs tree on
// disk, so autocrlf has nothing to smudge.
func writeStableDocFile(fileName string, page string) (bool, error) {
	if existing, err := os.ReadFile(fileName); err == nil {
		if proofStableContent(string(existing)) == proofStableContent(page) {
			return false, nil
		}
	}

	contents := []byte(strings.ReplaceAll(page, "\n", "\r\n"))

	if err := os.WriteFile(fileName, contents, 0644); err != nil {
		return false, fmt.Errorf("failed to write validation proof file \"%s\": %s", fileName, err)
	}

	return true, nil
}

// writeValidationProofPage renders and writes one package's proof page under docsPath, then
// regenerates the roster index from whatever pages now exist. Both writes are stability-gated, so
// a re-validation reproducing the same verdicts leaves the whole directory untouched.
func writeValidationProofPage(docsPath string, provenance proofPageProvenance, comparison testComparison, disclosures map[string]testDisclosure) error {
	currentPath := filepath.Join(docsPath, validationDocsDirName, validationCurrentDirName)

	if err := os.MkdirAll(currentPath, 0755); err != nil {
		return err
	}

	dotID := validationProofDotID(provenance.importPath)
	page := renderValidationProofPage(provenance, comparison, disclosures)

	if _, err := writeStableDocFile(filepath.Join(currentPath, dotID+".md"), page); err != nil {
		return err
	}

	return writeValidationIndex(docsPath)
}

// writeValidationIndex regenerates the roster from the pages on disk. It is computed on every
// validated run rather than only when a page changed: identical content means needToWriteFile-style
// gating writes nothing, and a missing or stale index heals itself.
func writeValidationIndex(docsPath string) error {
	validationPath := filepath.Join(docsPath, validationDocsDirName)

	entries, err := os.ReadDir(filepath.Join(validationPath, validationCurrentDirName))

	if err != nil {
		return err
	}

	var dotIDs []string

	for _, entry := range entries {
		if entry.IsDir() || !strings.HasSuffix(entry.Name(), ".md") {
			continue
		}

		dotIDs = append(dotIDs, strings.TrimSuffix(entry.Name(), ".md"))
	}

	_, err = writeStableDocFile(filepath.Join(validationPath, validationIndexFileName), renderValidationIndex(dotIDs))

	return err
}

// emitValidationProofPage publishes the proof page for a package that has just validated. It is a
// silent no-op when the conversion is not rooted in a go2cs repository checkout — the tree root is
// found by the same upward walk the test pipeline uses to self-locate $(go2csPath) (the root
// holding core/golib), and docs/ is that root's sibling; a bare temp -go2cspath conversion, or a
// deployed GOPATH runtime root, has no docs tree to publish into and simply skips.
func emitValidationProofPage(outputPath string, comparison testComparison, manifest testManifest, disclosures map[string]testDisclosure, options Options) error {
	if manifest.PackageImportPath == "" {
		return nil
	}

	root := findGo2CSRootAbove(outputPath)

	if root == "" {
		return nil
	}

	docsPath := filepath.Join(filepath.Dir(root), "docs")

	if info, err := os.Stat(docsPath); err != nil || !info.IsDir() {
		return nil
	}

	goVersion := strings.TrimPrefix(manifest.GoVersion, "go")

	if goVersion == "" {
		goVersion = goVersionFromToolchain()
	}

	provenance := proofPageProvenance{
		importPath: manifest.PackageImportPath,
		goVersion:  goVersion,
		platform:   options.targetPlatform,
		date:       time.Now().UTC().Format("2006-01-02"),
		commit:     shortGitRevision(filepath.Dir(root)),
	}

	return writeValidationProofPage(docsPath, provenance, comparison, disclosures)
}

// goVersionFromToolchain is the fallback when the test manifest recorded no Go version.
func goVersionFromToolchain() string {
	if version := goVersion(); version != "" {
		return version
	}

	return "unknown"
}

// shortGitRevision abbreviates gitRevision to the 9 characters the repository's own `git log
// --oneline` shows. An empty result (no checkout, no git) drops the element from the page.
func shortGitRevision(path string) string {
	revision := gitRevision(path)

	if len(revision) > 9 {
		return revision[:9]
	}

	return revision
}
