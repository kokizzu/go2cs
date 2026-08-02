// readmeValidationBadge_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// badgeTree builds the minimal repository shape the badge emitter reads: a go2cs root (identified,
// as everywhere else, by core\golib\golib.csproj), src\version.props beside it, and the docs tree as
// the root's sibling. It returns the root and the package's output directory.
func badgeTree(t *testing.T, dotID string, version string) (string, string) {
	t.Helper()

	tree := t.TempDir()
	root := filepath.Join(tree, "src")
	projectPath := filepath.Join(root, "core", filepath.FromSlash(validationProofImportPath(dotID)))

	mustMkdirAll(t, filepath.Join(root, "core", "golib"))
	mustMkdirAll(t, projectPath)
	mustWriteFile(t, filepath.Join(root, "core", "golib", "golib.csproj"), "<Project />")

	if version != "" {
		parts := strings.SplitN(version, ".", 4)

		if len(parts) != 4 {
			t.Fatalf("badgeTree version %q is not a four-part version", version)
		}

		mustWriteFile(t, filepath.Join(root, versionPropsFileName), fmt.Sprintf(
			"<Project>\r\n  <PropertyGroup>\r\n    <GoStdLibVersion>%s</GoStdLibVersion>\r\n    <GoBuildNumber>%s</GoBuildNumber>\r\n  </PropertyGroup>\r\n</Project>\r\n",
			strings.Join(parts[:3], "."), parts[3]))
	}

	return root, projectPath
}

// addProofPage writes a proof page for dotID rendered by the production renderer, so the counts the
// badge reads back are counts the converter actually emits.
func addProofPage(t *testing.T, root string, dotID string, matched int, disclosed int) {
	t.Helper()

	currentPath := filepath.Join(filepath.Dir(root), "docs", validationDocsDirName, validationCurrentDirName)
	mustMkdirAll(t, currentPath)

	comparison := testComparison{Go: map[string]string{}, CSharp: map[string]string{}}

	for i := 0; i < matched; i++ {
		name := fmt.Sprintf("TestMatched%03d", i)
		comparison.Go[name] = "pass"
		comparison.CSharp[name] = "pass"
	}

	for i := 0; i < disclosed; i++ {
		name := fmt.Sprintf("TestDisclosed%03d", i)
		comparison.Go[name] = "pass"
		comparison.CSharp[name] = "fail"
	}

	page := renderValidationProofPage(proofPageProvenance{
		importPath: validationProofImportPath(dotID),
		goVersion:  "1.23.1",
		platform:   "windows/amd64",
		date:       "2026-08-02",
		commit:     "abcdef012",
	}, comparison, map[string]testDisclosure{})

	mustWriteFile(t, filepath.Join(currentPath, dotID+".md"), strings.ReplaceAll(page, "\n", "\r\n"))
}

// addGoSources writes a package source directory holding the given files, and returns its path.
func addGoSources(t *testing.T, files map[string]string) string {
	t.Helper()

	sourceDir := filepath.Join(t.TempDir(), "gosrc")
	mustMkdirAll(t, sourceDir)

	for name, contents := range files {
		mustWriteFile(t, filepath.Join(sourceDir, name), contents)
	}

	return sourceDir
}

func mustMkdirAll(t *testing.T, path string) {
	t.Helper()

	if err := os.MkdirAll(path, 0755); err != nil {
		t.Fatalf("mkdir %s: %v", path, err)
	}
}

func mustWriteFile(t *testing.T, path string, contents string) {
	t.Helper()

	if err := os.WriteFile(path, []byte(contents), 0644); err != nil {
		t.Fatalf("write %s: %v", path, err)
	}
}

// A validated package's badge is green, states matched/total (total = matched + disclosed, so the
// denominator counts every test the suite ran), and links the VERSIONED proof page — the badge IS
// the proof link, so this string is pinned verbatim.
func TestValidationBadgeGreenPinsCountsAndVersionedProofLink(t *testing.T) {
	root, projectPath := badgeTree(t, "io", "1.23.1.2")

	addProofPage(t, root, "io", 59, 2)
	mustWriteFile(t, filepath.Join(projectPath, "io"+testProjectFileSuffix), "<Project />")

	const expected = "[![Go tests](https://img.shields.io/badge/Go_tests-59%2F61_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.2/io.html)"

	if badge := readmeValidationBadgeLine(projectPath, "io", ""); badge != expected {
		t.Fatalf("green badge mismatch\n got: %s\nwant: %s", badge, expected)
	}
}

// A multi-element import path becomes a flat dot-id in BOTH the proof file name and the badge URL.
func TestValidationBadgeGreenUsesDotIDForNestedPackages(t *testing.T) {
	root, projectPath := badgeTree(t, "path.filepath", "1.23.1.2")

	addProofPage(t, root, "path.filepath", 40, 0)
	mustWriteFile(t, filepath.Join(projectPath, "path.filepath"+testProjectFileSuffix), "<Project />")

	const expected = "[![Go tests](https://img.shields.io/badge/Go_tests-40%2F40_validated-brightgreen?logo=go)](https://go2cs.net/validation/1.23.1.2/path.filepath.html)"

	if badge := readmeValidationBadgeLine(projectPath, "path.filepath", ""); badge != expected {
		t.Fatalf("green badge mismatch\n got: %s\nwant: %s", badge, expected)
	}
}

// A package that HAS a Go test suite but has not been through the pipeline says exactly that.
func TestValidationBadgeOrangeForUnvalidatedTestSuite(t *testing.T) {
	root, projectPath := badgeTree(t, "fmt", "1.23.1.2")

	addProofPage(t, root, "io", 59, 2) // some OTHER package validated; docs tree exists

	sourceDir := addGoSources(t, map[string]string{
		"fmt.go":            "package fmt\n",
		"fmt_test.go":       "package fmt_test\n\nimport \"testing\"\n\nfunc TestSprintf(t *testing.T) {}\n",
		"scan_helper.go":    "package fmt\n\nfunc helper() {}\n",
		"benchmark_test.go": "package fmt\n\nimport \"testing\"\n\nfunc BenchmarkX(b *testing.B) {}\n",
	})

	const expected = "[![Go tests](https://img.shields.io/badge/Go_tests-not_yet_validated-orange?logo=go)](https://go2cs.net/ValidatedTestPackages.html)"

	if badge := readmeValidationBadgeLine(projectPath, "fmt", sourceDir); badge != expected {
		t.Fatalf("orange badge mismatch\n got: %s\nwant: %s", badge, expected)
	}
}

// A package whose Go sources define no Test functions can never go green, so it says there is
// nothing to validate rather than implying an outstanding debt.
func TestValidationBadgeGreyForPackageWithoutTests(t *testing.T) {
	root, projectPath := badgeTree(t, "unsafe.like", "1.23.1.2")

	addProofPage(t, root, "io", 59, 2)

	sourceDir := addGoSources(t, map[string]string{
		"doc.go": "package like\n",
	})

	const expected = "[![Go tests](https://img.shields.io/badge/Go_tests-none_to_validate-lightgrey?logo=go)](https://go2cs.net/ValidatedTestPackages.html)"

	if badge := readmeValidationBadgeLine(projectPath, "unsafe.like", sourceDir); badge != expected {
		t.Fatalf("grey badge mismatch\n got: %s\nwant: %s", badge, expected)
	}
}

// The fallback: no version.props (or no docs tree) means no honest badge can be composed, so the
// README is emitted exactly as it was before badges existed rather than carrying a half-built URL.
// This is what a bare temp -go2cspath reconvert hits, and why the seed ritual seeds both.
func TestValidationBadgeOmittedWithoutRepositoryContext(t *testing.T) {
	sourceDir := addGoSources(t, map[string]string{
		"a.go":      "package a\n",
		"a_test.go": "package a\n\nimport \"testing\"\n\nfunc TestA(t *testing.T) {}\n",
	})

	t.Run("no version.props", func(t *testing.T) {
		root, projectPath := badgeTree(t, "io", "")
		addProofPage(t, root, "io", 59, 2)
		mustWriteFile(t, filepath.Join(projectPath, "io"+testProjectFileSuffix), "<Project />")

		if badge := readmeValidationBadgeLine(projectPath, "io", sourceDir); badge != "" {
			t.Fatalf("expected no badge without version.props, got: %s", badge)
		}
	})

	t.Run("no docs tree", func(t *testing.T) {
		_, projectPath := badgeTree(t, "io", "1.23.1.2")
		mustWriteFile(t, filepath.Join(projectPath, "io"+testProjectFileSuffix), "<Project />")

		if badge := readmeValidationBadgeLine(projectPath, "io", sourceDir); badge != "" {
			t.Fatalf("expected no badge without a docs tree, got: %s", badge)
		}
	})

	t.Run("no go2cs root", func(t *testing.T) {
		if badge := readmeValidationBadgeLine(t.TempDir(), "io", sourceDir); badge != "" {
			t.Fatalf("expected no badge outside a go2cs root, got: %s", badge)
		}
	})
}

// A committed test project without a proof page (or the reverse) must not produce a green badge
// claiming counts nothing backs; it falls through to the honest has-tests classification, where the
// corpus census then shows the disagreement as a miscount.
func TestValidationBadgeRequiresBothGreenSignals(t *testing.T) {
	root, projectPath := badgeTree(t, "io", "1.23.1.2")

	addProofPage(t, root, "other", 1, 0)
	mustWriteFile(t, filepath.Join(projectPath, "io"+testProjectFileSuffix), "<Project />")

	sourceDir := addGoSources(t, map[string]string{
		"io.go":      "package io\n",
		"io_test.go": "package io\n\nimport \"testing\"\n\nfunc TestIO(t *testing.T) {}\n",
	})

	badge := readmeValidationBadgeLine(projectPath, "io", sourceDir)

	if strings.Contains(badge, "brightgreen") {
		t.Fatalf("a test project with no proof page produced a green badge: %s", badge)
	}

	if !strings.Contains(badge, "not_yet_validated") {
		t.Fatalf("expected the honest orange fallback, got: %s", badge)
	}
}

// The census predicate — the rule the roster's own denominator is built on — applied to package
// source directories. `func Test...` text inside comments and string literals is everywhere in the
// standard library's test sources, so the predicate parses rather than greps.
func TestPackageDeclaresGoTests(t *testing.T) {
	tests := []struct {
		name     string
		files    map[string]string
		expected bool
	}{
		{
			name:     "no test files at all",
			files:    map[string]string{"a.go": "package a\n\nfunc TestNotATest() {}\n"},
			expected: false,
		},
		{
			name:     "test file with a test",
			files:    map[string]string{"a_test.go": "package a\n\nimport \"testing\"\n\nfunc TestThing(t *testing.T) {}\n"},
			expected: true,
		},
		{
			name:     "bare Test is a test",
			files:    map[string]string{"a_test.go": "package a\n\nimport \"testing\"\n\nfunc Test(t *testing.T) {}\n"},
			expected: true,
		},
		{
			name:     "lower-case suffix is not a test",
			files:    map[string]string{"a_test.go": "package a\n\nfunc Testify() {}\n"},
			expected: false,
		},
		{
			name:     "underscore suffix is a test",
			files:    map[string]string{"a_test.go": "package a\n\nimport \"testing\"\n\nfunc Test_thing(t *testing.T) {}\n"},
			expected: true,
		},
		{
			name:     "a method named TestX is not a test",
			files:    map[string]string{"a_test.go": "package a\n\ntype s struct{}\n\nfunc (s) TestThing() {}\n"},
			expected: false,
		},
		{
			name:     "benchmarks and examples alone are not tests",
			files:    map[string]string{"a_test.go": "package a\n\nimport \"testing\"\n\nfunc BenchmarkX(b *testing.B) {}\n\nfunc ExampleY() {}\n"},
			expected: false,
		},
		{
			name:     "a mention in a comment or string is not a test",
			files:    map[string]string{"a_test.go": "package a\n\n// func TestFake(t *testing.T) is documented here.\nvar src = `func TestAlsoFake(t *testing.T) {}`\n"},
			expected: false,
		},
		{
			name: "found in the second file",
			files: map[string]string{
				"a_test.go": "package a\n\nfunc helper() {}\n",
				"b_test.go": "package a\n\nimport \"testing\"\n\nfunc TestThing(t *testing.T) {}\n",
			},
			expected: true,
		},
	}

	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			if actual := packageDeclaresGoTests(addGoSources(t, test.files)); actual != test.expected {
				t.Fatalf("packageDeclaresGoTests = %v, want %v", actual, test.expected)
			}
		})
	}

	if packageDeclaresGoTests(filepath.Join(t.TempDir(), "missing")) {
		t.Fatal("a nonexistent source directory reported tests")
	}
}

// parseProofTotals is derived from the renderer's own format string; this is the round trip that
// keeps them honest.
func TestParseProofTotalsRoundTripsTheRenderer(t *testing.T) {
	comparison, disclosures := loadProofFixture(t)
	page := renderValidationProofPage(fixtureProvenance(), comparison, disclosures)

	names := proofVerdictNames(comparison)
	expectedDisclosed := len(proofDisclosedNames(comparison, names))
	expectedMatched := len(names) - expectedDisclosed

	matched, disclosed, ok := parseProofTotals(page)

	if !ok {
		t.Fatal("the renderer's own page did not parse")
	}

	if matched != expectedMatched || disclosed != expectedDisclosed {
		t.Fatalf("parsed %d matched / %d disclosed, want %d / %d", matched, disclosed, expectedMatched, expectedDisclosed)
	}

	// CRLF on disk must read the same as freshly rendered text.
	if crlfMatched, crlfDisclosed, crlfOK := parseProofTotals(strings.ReplaceAll(page, "\n", "\r\n")); !crlfOK || crlfMatched != matched || crlfDisclosed != disclosed {
		t.Fatalf("a CRLF page parsed differently: %d/%d ok=%v", crlfMatched, crlfDisclosed, crlfOK)
	}

	if _, _, ok := parseProofTotals("# not a proof page\n\nnothing to see\n"); ok {
		t.Fatal("a page with no totals line parsed anyway")
	}
}

// The badge's dot-id and the proof page's file name are the same mapping, in both directions.
func TestValidationProofDotIDRoundTrip(t *testing.T) {
	for _, importPath := range []string{"io", "path/filepath", "math/rand/v2", "crypto/internal/fips140/aes"} {
		dotID := validationProofDotID(importPath)

		if strings.Contains(dotID, "/") {
			t.Fatalf("dot-id %q still holds a path separator", dotID)
		}

		if actual := validationProofImportPath(dotID); actual != importPath {
			t.Fatalf("round trip of %q produced %q", importPath, actual)
		}
	}
}
