// readme.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/doc/comment"
	"os"
	"path/filepath"
	"strings"
	"sync"
)

// extractPackageDoc returns the package-level Go doc comment (the comment group attached to the
// `package` clause) as godoc-markup TEXT, for use as a NuGet package README.
//
// It reads ast.File.Doc directly — a pure read — rather than go/doc.NewFromFiles, which takes
// ownership of and may mutate the AST that the converter subsequently visits. The per-file BSD
// license header is a separate comment group (blank-line-separated from the package clause), so it
// is naturally excluded; only the package documentation is returned.
//
// Rendering to Markdown is deliberately NOT done here. This runs during package-state capture, where
// the only thing in scope is the AST; renderPackageDoc runs at emission time, where the package's
// source directory and the conversion options are — and those are exactly what resolving a doc
// link to a fully-qualified URL needs. Capture reads, emission renders.
func extractPackageDoc(files []*ast.File) string {
	var docs []string

	for _, file := range files {
		if file.Doc == nil {
			continue
		}

		// CommentGroup.Text() strips the // or /* */ markers and cleans the text.
		if text := strings.TrimSpace(file.Doc.Text()); text != "" {
			docs = append(docs, text)
		}
	}

	if len(docs) == 0 {
		return ""
	}

	return strings.Join(docs, "\n\n")
}

// renderPackageDoc renders a package's godoc markup to the GitHub-flavored Markdown the README
// carries, resolving every doc link to a fully-qualified URL on the way out.
//
// sourceDir is the package's own Go source directory, which is what names the package being
// converted: stdLibImportPath recovers its import path from where the sources sit under GOROOT/src,
// for the same reason the Docs badge does it that way rather than trusting the loader's PkgPath.
// That import path is what the two same-package doc-link forms resolve against.
func renderPackageDoc(packageDoc string, sourceDir string, options Options) string {
	if strings.TrimSpace(packageDoc) == "" {
		return ""
	}

	importPath := stdLibImportPath(sourceDir, options.goRoot)
	version := goVersion()

	var parser comment.Parser
	var printer comment.Printer

	// Suppress the "{#hdr-...}" heading-anchor suffix — NuGet's Markdown renderer shows it literally.
	printer.HeadingID = func(*comment.Heading) string { return "" }

	// Resolve doc links to fully-qualified, version-pinned URLs. Without this the printer falls
	// back to DocLink.DefaultURL against an empty base and emits site-root-relative links that are
	// dead on GitHub, on Pages and on nuget.org alike — see readmeDocLinks.go.
	printer.DocLinkURL = func(link *comment.DocLink) string {
		return resolveDocLinkURL(link, importPath, version, options.goRoot)
	}

	return strings.TrimSpace(string(printer.Markdown(parser.Parse(packageDoc))))
}

var goVersionOnce sync.Once
var goVersionValue string

// goVersion returns the active Go toolchain version without the "go" prefix (e.g. "1.23.1"),
// resolved once from `go env GOVERSION`. Returns "" if it cannot be determined.
//
// "Active" means the toolchain that LOADED the packages, which is not always the one on PATH — see
// pinGoVersion.
func goVersion() string {
	goVersionOnce.Do(func() {
		if value, err := getGoEnv("GOVERSION"); err == nil {
			goVersionValue = strings.TrimPrefix(strings.TrimSpace(value), "go")
		}
	})

	return goVersionValue
}

// pinGoVersion fixes the release goVersion reports, for the case where the module being converted
// selects a toolchain other than the one on PATH (see resolveLoaderGoRoot). It must be called before
// the first goVersion() — main does, right after resolving GOROOT — and is a no-op afterwards.
//
// This matters beyond cosmetics: under -recurse=nuget the reported release becomes the emitted
// $(GoStdLibVersion), i.e. the VERSION of every go.<pkg> package the generated project restores. Left
// ambient, a switched run converts the newer toolchain's standard library while asking NuGet for the
// PATH toolchain's release — two different standard libraries in one project, and the mismatch only
// surfaces as unresolvable package ids at restore time.
func pinGoVersion(resolved string) {
	resolved = strings.TrimPrefix(strings.TrimSpace(resolved), "go")

	if resolved == "" {
		return
	}

	goVersionOnce.Do(func() {
		goVersionValue = resolved
	})
}

// writeReadmeFile emits a README.md into a converted library package directory, wrapping the
// package's Go doc (rendered here, by renderPackageDoc) so the NuGet package carries readable docs. It is
// idempotent via needToWriteFile, mirroring how the icon and .csproj files are written, and uses
// CRLF line endings to match the converter's other generated text output (and avoid autocrlf churn).
//
// Between the attribution blockquote and the package's own documentation sits the badge paragraph —
// two lines, Tests + Docs then the Source pair, per readmeBadgeLine, each badge omitted when this
// conversion cannot compose an honest one. sourceDir is the package's Go source directory, which the
// badges read: the validation badge to see the `_test.go` files the conversion itself never compiles,
// the docs and Go source badges to recover the package's Go import path.
//
// The CRLF pass at the bottom is what carries the badge paragraph's internal hard break to disk, so
// readmeBadgeLine composes that break with a bare \n and lets this one conversion cover it.
func writeReadmeFile(projectPath string, projectName string, packageDoc string, sourceDir string, options Options) error {
	projectPath = strings.TrimRight(projectPath, string(filepath.Separator)) + string(filepath.Separator)
	readmeFileName := projectPath + "README.md"

	var builder strings.Builder

	builder.WriteString(fmt.Sprintf("# go.%s\n\n", projectName))
	builder.WriteString("> C# package converted from the Go standard library by [go2cs](https://github.com/ritchiecarroll/go2cs).\n\n")

	if badges := readmeBadgeLine(projectPath, projectName, sourceDir, options); badges != "" {
		builder.WriteString(badges)
		builder.WriteString("\n\n")
	}

	if rendered := renderPackageDoc(packageDoc, sourceDir, options); rendered != "" {
		builder.WriteString(rendered)
		builder.WriteString("\n\n")
	}

	builder.WriteString("---\n\n")
	builder.WriteString("Copyright 2009 The Go Authors. All rights reserved. This C# package is converted from Go standard library source; use of that source is governed by a BSD-style license that can be found in the [LICENSE](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/LICENSE) file. The go2cs conversion itself is distributed under the MIT license.\n")

	contents := []byte(strings.ReplaceAll(builder.String(), "\n", "\r\n"))

	if needToWriteFile(readmeFileName, contents) {
		if err := os.WriteFile(readmeFileName, contents, 0644); err != nil {
			return fmt.Errorf("failed to write README file \"%s\": %s", readmeFileName, err)
		}
	}

	return nil
}
