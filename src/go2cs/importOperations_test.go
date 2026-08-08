// importOperations_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/build"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"testing"
)

// TestPackageQualifiedName locks the imported-type-alias namespace qualification for non-stdlib deps:
// go.<result>_package must be the imported package's converted class. The last segment is the Go
// package name (which can differ from the import-path segment, e.g. go-isatty is `package isatty`),
// and a single-segment module (namespace == the root) yields just the package name.
func TestPackageQualifiedName(t *testing.T) {
	cases := []struct {
		namespace string
		pkgName   string
		want      string
	}{
		{"go.github.com.google", "uuid", "github.com.google.uuid"},
		{"go.github.com.mattn", "isatty", "github.com.mattn.isatty"},       // path segment is go-isatty; package is isatty
		{"go.github.com.mattn", "colorable", "github.com.mattn.colorable"}, // go-colorable -> colorable
		{"go.example.com", "lib", "example.com.lib"},
		{"go", "foo", "foo"}, // single-segment module: namespace is the bare root
	}

	for _, tc := range cases {
		if got := packageQualifiedName(tc.namespace, tc.pkgName); got != tc.want {
			t.Errorf("packageQualifiedName(%q, %q) = %q, want %q", tc.namespace, tc.pkgName, got, tc.want)
		}
	}
}

// TestProjectNameFromModuleDirective guards issue #33's csproj-filename failure at its source: the
// go.mod module path is a TOKEN, so it may be quoted, and gopkg.in modules write it that way —
// gopkg.in/yaml.v3's own go.mod is literally `module "gopkg.in/yaml.v3"`. Reading the line's
// remainder raw carried the quotes into the project name and thence into the csproj FILENAME, which
// Windows rejects ("gopkg.in.yaml.v3".csproj → "The filename, directory name, or volume label syntax
// is incorrect"). The invariant asserted here is not merely the expected string but that a project
// name is always a legal filename — no character the platform forbids can survive the parse.
func TestProjectNameFromModuleDirective(t *testing.T) {
	cases := []struct {
		name   string
		goMod  string
		want   string
		wantNS string
	}{
		{
			// The exact shape shipped by gopkg.in/yaml.v3@v3.0.1 — quoted module path AND quoted
			// require paths, which is what issue #33's reporter hit.
			name:   "quoted gopkg.in module path",
			goMod:  "module \"gopkg.in/yaml.v3\"\n\nrequire (\n\t\"gopkg.in/check.v1\" v0.0.0-20161208181325-20d25e280405\n)\n",
			want:   "gopkg.in.yaml.v3",
			wantNS: RootNamespace + ".gopkg.@in",
		},
		{
			name:   "unquoted module path is unchanged",
			goMod:  "module github.com/fatih/color\n\ngo 1.17\n",
			want:   "github.com.fatih.color",
			wantNS: RootNamespace + ".github.com.fatih",
		},
		{
			name:   "raw-quoted module path",
			goMod:  "module `example.com/raw`\n\ngo 1.23\n",
			want:   "example.com.raw",
			wantNS: RootNamespace + ".example.com",
		},
		{
			name:   "trailing comment is not part of the path",
			goMod:  "module example.com/commented // see issue #33\n\ngo 1.23\n",
			want:   "example.com.commented",
			wantNS: RootNamespace + ".example.com",
		},
	}

	// The fixture module directories are outside GOROOT, so the module path comes from their go.mod
	// exactly as it does for a -recurse dependency in the module cache.
	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			dir := t.TempDir()

			if err := os.WriteFile(filepath.Join(dir, "go.mod"), []byte(tc.goMod), 0o600); err != nil {
				t.Fatal(err)
			}

			projectName, namespace := getProjectName(dir, Options{goRoot: goRoot})

			if projectName != tc.want {
				t.Errorf("project name = %q, want %q", projectName, tc.want)
			}

			if namespace != tc.wantNS {
				t.Errorf("namespace = %q, want %q", namespace, tc.wantNS)
			}

			// The project name IS the csproj file name, so it must contain nothing the file system
			// forbids. This is the assertion that fails loudly for any future quoting/escaping arm
			// meant for code that leaks into a path.
			if bad := strings.IndexAny(projectName, "\"<>:|?*\\/"); bad >= 0 {
				t.Errorf("project name %q contains %q, which cannot appear in a file name", projectName, projectName[bad])
			}
		})
	}
}

// TestEmittedProjectReferenceIsHostIndependent is the F5 unit: the reference string that lands in a
// <ProjectReference Include="…"> must be the SAME on every host, and it must be forward-slashed.
//
// The pre-fix code hand-rolled the join: it replaced every "/" with a backslash and then called
// filepath.Join with a backslash-prefixed second element. On Windows filepath.Clean folded those
// injected separators into a well-formed path; on Unix filepath.Join treats a backslash as an
// ordinary filename character, so the same call emitted `$(go2csPath)core\fmt/\fmt.csproj` — silent
// at emission, a restore failure later.
//
// pathReplace hands back whichever separator the HOST produced, so the input spelling is exercised
// per-host: the Unix spelling always (filepath.ToSlash is the identity there), the Windows spelling
// only where it can actually arise.
func TestEmittedProjectReferenceIsHostIndependent(t *testing.T) {
	const want = "$(go2csPath)core/unicode/utf8/unicode.utf8.csproj"

	if got := emittedProjectReference("$(go2csPath)core/unicode/utf8", "unicode.utf8.csproj"); got != want {
		t.Errorf("emittedProjectReference(unix spelling) = %q, want %q", got, want)
	}

	if runtime.GOOS == "windows" {
		if got := emittedProjectReference(`$(go2csPath)core\unicode\utf8`, "unicode.utf8.csproj"); got != want {
			t.Errorf("emittedProjectReference(windows spelling) = %q, want %q", got, want)
		}
	}
}

// The recurse module-cache branch composes its own $(go2csPath)pkg reference from the version-free
// import path, so it is a second emission site with the same invariant.
func TestEmittedProjectReferenceForModuleCachePath(t *testing.T) {
	got := emittedProjectReference("$(go2csPath)pkg/"+"github.com/google/uuid", "github.com.google.uuid.csproj")

	if want := "$(go2csPath)pkg/github.com/google/uuid/github.com.google.uuid.csproj"; got != want {
		t.Errorf("emittedProjectReference = %q, want %q", got, want)
	}
}
