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

// TestProjectNameSurvivesGoFileFreeContainerDir guards issue #35 at its source: a project name must be
// the package's full import path, and the upward module search must not surrender it to a container
// directory that happens to hold no .go files of its own.
//
// `internal/`, a service tree's `endpoints/` parent, a proto grouping like `xds/core/` — a Go module is
// full of directories whose only content is subdirectories. The walk treated the first such ancestor as
// a stop and named the project after the leaf segment alone, so cloud.google.com/go/bigquery's
// datatransfer/apiv1 and storage/apiv1 both emitted `apiv1.csproj`. Visual Studio rejects a solution
// holding two projects with the same name and gives no reason, so the whole generated .slnx silently
// fails to open (issue #35); the reporter's one conversion had 175 such projects in 49 name families.
// The collapse is not merely a naming nuisance either: the namespace is derived from the same parts, so
// a truncated `internal/errors` lands on go.errors_package — the converted standard library's own class.
//
// The fixture is the reporter's exact shape: one module, two go-file-free container directories, two
// leaf packages that share a final segment.
func TestProjectNameSurvivesGoFileFreeContainerDir(t *testing.T) {
	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	root := t.TempDir()

	if err := os.WriteFile(filepath.Join(root, "go.mod"), []byte("module cloud.google.com/go/bigquery\n\ngo 1.23\n"), 0o600); err != nil {
		t.Fatal(err)
	}

	if err := os.WriteFile(filepath.Join(root, "bigquery.go"), []byte("package bigquery\n"), 0o600); err != nil {
		t.Fatal(err)
	}

	// datatransfer/ and storage/ hold no .go files — only the apiv1 package below each.
	for _, leaf := range []string{filepath.Join("datatransfer", "apiv1"), filepath.Join("storage", "apiv1")} {
		dir := filepath.Join(root, leaf)

		if err := os.MkdirAll(dir, 0o755); err != nil {
			t.Fatal(err)
		}

		if err := os.WriteFile(filepath.Join(dir, "client.go"), []byte("package apiv1\n"), 0o600); err != nil {
			t.Fatal(err)
		}
	}

	cases := []struct {
		dir    string
		want   string
		wantNS string
	}{
		{
			dir:    filepath.Join(root, "datatransfer", "apiv1"),
			want:   "cloud.google.com.go.bigquery.datatransfer.apiv1",
			wantNS: RootNamespace + ".cloud.google.com.go.bigquery.datatransfer",
		},
		{
			dir:    filepath.Join(root, "storage", "apiv1"),
			want:   "cloud.google.com.go.bigquery.storage.apiv1",
			wantNS: RootNamespace + ".cloud.google.com.go.bigquery.storage",
		},
	}

	names := make(map[string]string, len(cases))

	for _, tc := range cases {
		projectName, namespace := getProjectName(tc.dir, Options{goRoot: goRoot})

		if projectName != tc.want {
			t.Errorf("getProjectName(%q) project name = %q, want %q", tc.dir, projectName, tc.want)
		}

		if namespace != tc.wantNS {
			t.Errorf("getProjectName(%q) namespace = %q, want %q", tc.dir, namespace, tc.wantNS)
		}

		// The invariant the solution actually needs: distinct packages get distinct project names.
		if prior, dup := names[projectName]; dup {
			t.Errorf("project name %q emitted for both %q and %q — a duplicate .slnx project name", projectName, prior, tc.dir)
		}

		names[projectName] = tc.dir
	}
}

// TestProjectNameFallsBackWhenNoModuleRoot pins the arm the issue-#35 fix must NOT disturb: with no
// go.mod and no main.go anywhere above it, there is no import path to recover, so the name stays the
// leaf-relative one the walk composes on the way up. The go-file-free ancestor is a FALLBACK here,
// which is exactly what it stopped being in the module case.
func TestProjectNameFallsBackWhenNoModuleRoot(t *testing.T) {
	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	root := t.TempDir()
	pkgDir := filepath.Join(root, "example.com", "foo", "bar")

	if err := os.MkdirAll(pkgDir, 0o755); err != nil {
		t.Fatal(err)
	}

	// foo/ has Go files, example.com/ does not, and nothing above declares a module.
	if err := os.WriteFile(filepath.Join(root, "example.com", "foo", "foo.go"), []byte("package foo\n"), 0o600); err != nil {
		t.Fatal(err)
	}

	if err := os.WriteFile(filepath.Join(pkgDir, "bar.go"), []byte("package bar\n"), 0o600); err != nil {
		t.Fatal(err)
	}

	projectName, namespace := getProjectName(pkgDir, Options{goRoot: goRoot})

	if want := "foo.bar"; projectName != want {
		t.Errorf("project name = %q, want %q", projectName, want)
	}

	// The bare root, because this arm joins its segments with "." before the separator split that
	// builds the namespace — so the qualification never reaches it. Asserted as-is rather than
	// corrected: it is pre-existing behavior on a path that cannot produce issue #35's collision (a
	// tree with no module root is converted one project at a time), and the declaration and
	// reference sides derive it from the same call, so they agree.
	if want := RootNamespace; namespace != want {
		t.Errorf("namespace = %q, want %q", namespace, want)
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
