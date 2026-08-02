// readmeValidationBadge.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Every converted standard-library package's NuGet README carries exactly ONE validation badge, and
// that badge is the package's honesty contract — the first thing a visitor sees on nuget.org and in
// the repository:
//
//	green   <m>/<t> validated   Go's own test suite for this package was converted, run under the
//	                            Go-semantics test host and compared verdict for verdict against
//	                            `go test -json`. The badge LINKS its proof: the versioned page under
//	                            docs/validation/<version>/, which is the per-test differential.
//	orange  not yet validated   The package's Go sources DO define Test functions; they have not
//	                            been put through the pipeline yet. Never shown on a test-less
//	                            package — that would invent a debt that does not exist.
//	grey    none to validate    The package's Go sources define no Test functions at all, so there
//	                            is nothing for a green badge to ever claim.
//
// The three states partition the corpus exactly, which is what makes the badge auditable: a census
// over the emitted READMEs must reproduce the roster's own denominator (the packages whose Go
// sources define Test functions) with no package left unclassified.
//
// FALLBACK (load-bearing for reproducibility): the badge is composed from two things that live in
// the REPOSITORY, not in the conversion — src/version.props (the published version, which pins the
// proof URL) and docs/validation/current/ (the counts). A conversion rooted anywhere else — a bare
// temp -go2cspath root, a deployed GOPATH runtime root — can locate neither and emits NO badge line
// at all rather than a half-composed URL. This is why a reconvert that must reproduce the committed
// READMEs byte-identically has to seed version.props and docs/validation alongside src/core; see
// CLAUDE.md's corpus-mechanics seed step.

package main

import (
	"fmt"
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"path/filepath"
	"regexp"
	"strings"
	"unicode"
)

const (
	// validationSiteURL is the published documentation site (Jekyll over docs/), where the proof
	// pages the badges link to are served as .html.
	validationSiteURL = "https://go2cs.net"

	// validationRosterURL is the living roster every non-green badge points at — the honest landing
	// place for "why isn't this one green yet?".
	validationRosterURL = validationSiteURL + "/ValidatedTestPackages.html"

	// versionPropsFileName is the single source of truth for the published package version, read
	// from the same go2cs root the project references resolve against.
	versionPropsFileName = "version.props"

	// testProjectFileSuffix identifies a package's COMMITTED converted test project — the artifact
	// the validated-package commit policy banks beside the production code, and therefore the
	// on-disk signal that this package's suite has been through the pipeline.
	testProjectFileSuffix = ".tests.csproj"
)

// The published version lives in version.props as two elements; these mirror push-nuget.ps1's own
// regexes so the converter and the release script read the file the same way.
var (
	goStdLibVersionPattern = regexp.MustCompile(`<GoStdLibVersion>([^<]+)</GoStdLibVersion>`)
	goBuildNumberPattern   = regexp.MustCompile(`<GoBuildNumber>([^<]+)</GoBuildNumber>`)
)

// readmeValidationBadgeLine returns the one-line validation badge for a converted stdlib package, or
// "" when this conversion has no repository context to compose an honest badge from.
//
// projectPath is the package's OUTPUT directory, projectName its dotted id (`path.filepath`, which
// is both the NuGet id suffix and the proof page's flat file name), and sourceDir the directory its
// Go sources came from.
func readmeValidationBadgeLine(projectPath string, projectName string, sourceDir string) string {
	root := findGo2CSRootAbove(projectPath)

	if root == "" {
		return ""
	}

	version := publishedPackageVersion(root)

	if version == "" {
		return ""
	}

	currentPath := filepath.Join(filepath.Dir(root), "docs", validationDocsDirName, validationCurrentDirName)

	if info, err := os.Stat(currentPath); err != nil || !info.IsDir() {
		return ""
	}

	// GREEN requires BOTH signals to agree: the package's converted test project is committed beside
	// it (the pipeline ran and its results were banked) AND its proof page states the totals the
	// badge is about to claim. They are 1:1 across the corpus; requiring both means a badge can
	// never claim a number no committed evidence backs, and any disagreement shows up as a census
	// miscount rather than as a wrong badge.
	if hasCommittedTestProject(projectPath, projectName) {
		if matched, disclosed, ok := validatedPackageTotals(currentPath, projectName); ok {
			return validationBadge(
				fmt.Sprintf("%d%%2F%d_validated", matched, matched+disclosed),
				"brightgreen",
				fmt.Sprintf("%s/%s/%s/%s.html", validationSiteURL, validationDocsDirName, version, projectName))
		}
	}

	// Without the Go sources the orange/grey distinction cannot be made honestly, and guessing
	// either way would misstate the package. Say nothing instead.
	if sourceDir == "" {
		return ""
	}

	if packageDeclaresGoTests(sourceDir) {
		return validationBadge("not_yet_validated", "orange", validationRosterURL)
	}

	return validationBadge("none_to_validate", "lightgrey", validationRosterURL)
}

// validationBadge renders one shields.io badge as a Markdown image link. The message is already
// shields-encoded by the caller (spaces as underscores, "/" as %2F).
func validationBadge(message string, color string, target string) string {
	return fmt.Sprintf("[![Go tests](https://img.shields.io/badge/Go_tests-%s-%s?logo=go)](%s)", message, color, target)
}

// publishedPackageVersion reads the four-part published version (`1.23.1.2`) from the go2cs root's
// version.props — the same single source push-nuget.ps1 bumps. Returns "" when the file is absent
// (a conversion outside a repository checkout) or does not carry both elements.
func publishedPackageVersion(root string) string {
	contents, err := os.ReadFile(filepath.Join(root, versionPropsFileName))

	if err != nil {
		return ""
	}

	base := firstSubmatch(goStdLibVersionPattern, string(contents))
	build := firstSubmatch(goBuildNumberPattern, string(contents))

	if base == "" || build == "" {
		return ""
	}

	return base + "." + build
}

func firstSubmatch(pattern *regexp.Regexp, contents string) string {
	if match := pattern.FindStringSubmatch(contents); match != nil {
		return strings.TrimSpace(match[1])
	}

	return ""
}

// hasCommittedTestProject reports whether the package's converted test project is banked beside its
// production code — `<dot-id>.tests.csproj`, written by the -tests pipeline and committed only for
// a package whose suite validated.
func hasCommittedTestProject(projectPath string, projectName string) bool {
	_, err := os.Stat(filepath.Join(projectPath, projectName+testProjectFileSuffix))

	return err == nil
}

// validatedPackageTotals reads the matched/disclosed counts off the package's living proof page.
// The page is the converter's own output and parseProofTotals is derived from the very format
// string that renders it, so the badge cannot drift from the evidence it links.
func validatedPackageTotals(currentPath string, dotID string) (int, int, bool) {
	contents, err := os.ReadFile(filepath.Join(currentPath, dotID+".md"))

	if err != nil {
		return 0, 0, false
	}

	return parseProofTotals(string(contents))
}

// packageDeclaresGoTests reports whether the package's Go sources declare any test function — the
// roster's own denominator rule, applied to the GOROOT package directory.
//
// The files are PARSED rather than scanned lexically: `func Test...` appears inside string literals
// and doc comments throughout the standard library's own test sources, and a badge that claims a
// package has tests because a comment mentioned one is worse than no badge. Parse errors are
// tolerated (the partial AST is still inspected) so one unparseable file cannot silently demote a
// package that plainly has tests.
func packageDeclaresGoTests(sourceDir string) bool {
	entries, err := os.ReadDir(sourceDir)

	if err != nil {
		return false
	}

	fileSet := token.NewFileSet()

	for _, entry := range entries {
		if entry.IsDir() || !strings.HasSuffix(entry.Name(), "_test.go") {
			continue
		}

		file, _ := parser.ParseFile(fileSet, filepath.Join(sourceDir, entry.Name()), nil, parser.SkipObjectResolution)

		if file == nil {
			continue
		}

		for _, decl := range file.Decls {
			funcDecl, ok := decl.(*ast.FuncDecl)

			if !ok || funcDecl.Recv != nil || funcDecl.Name == nil {
				continue
			}

			if isGoTestFunctionName(funcDecl.Name.Name) {
				return true
			}
		}
	}

	return false
}

// isGoTestFunctionName applies `go test`'s own naming rule: a test is `Test` optionally followed by
// a suffix that does NOT start with a lower-case letter (`TestFoo`, `Test_foo` and bare `Test` are
// tests; `Testify` is not).
func isGoTestFunctionName(name string) bool {
	const prefix = "Test"

	if !strings.HasPrefix(name, prefix) {
		return false
	}

	suffix := name[len(prefix):]

	if suffix == "" {
		return true
	}

	return !unicode.IsLower([]rune(suffix)[0])
}
