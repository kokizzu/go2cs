// converterStaleness_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"os"
	"path/filepath"
	"testing"
	"time"
)

// The stale-binary self-check (converterStaleness.go), guarded POSITIVE CONTROL FIRST.
//
// The ordering is deliberate and is the whole point: a probe that never fires would satisfy every
// "stays quiet" assertion trivially, so the fire case is asserted before the quiet ones. Neuter
// newestConverterInput's comparison and TestConverterStalenessDetectsTouchedSource goes red
// immediately, which is what makes the quiet-case tests worth anything.

// newestConverterInput must SEE a source file touched after the binary — the condition that
// produced the 2026-08-30 std.reflect artifacts from a binary predating 433e9e4e0.
func TestConverterStalenessDetectsTouchedSource(t *testing.T) {
	root := syntheticConverterRoot(t)

	older := time.Now().Add(-2 * time.Hour)
	touchAll(t, root, older)

	// One source file edited after the build — the incident's shape.
	edited := filepath.Join(root, "visitFuncDecl.go")
	touch(t, edited, time.Now())

	newest, newestPath := newestConverterInput(root)

	if newestPath == "" {
		t.Fatal("newestConverterInput found no build inputs at all in a synthetic converter root")
	}

	if filepath.Base(newestPath) != "visitFuncDecl.go" {
		t.Errorf("expected the touched source to be newest, got %q", filepath.Base(newestPath))
	}

	if !newest.After(older) {
		t.Errorf("newest time %v did not advance past the baseline %v", newest, older)
	}
}

// Route #5's half: an EMBEDDED ASSET is a build input even though it is not a .go file. Editing a
// csproj template changes every project the converter emits while touching no Go source, so a
// .go-only scan would report "up to date" over a changed emission.
func TestConverterStalenessSeesEmbeddedAssets(t *testing.T) {
	root := syntheticConverterRoot(t)

	touchAll(t, root, time.Now().Add(-2*time.Hour))
	touch(t, filepath.Join(root, "csproj-template.xml"), time.Now())

	_, newestPath := newestConverterInput(root)

	if filepath.Base(newestPath) != "csproj-template.xml" {
		t.Errorf("an embedded asset must count as a build input (route #5); newest was %q", filepath.Base(newestPath))
	}
}

// go.mod and go.sum are inputs for the same reason: a `go` directive bump or a dependency change
// alters the built binary exactly as a source edit does.
func TestConverterStalenessSeesModuleFiles(t *testing.T) {
	root := syntheticConverterRoot(t)

	touchAll(t, root, time.Now().Add(-2*time.Hour))
	touch(t, filepath.Join(root, "go.sum"), time.Now())

	if _, newestPath := newestConverterInput(root); filepath.Base(newestPath) != "go.sum" {
		t.Errorf("go.sum must count as a build input; newest was %q", filepath.Base(newestPath))
	}
}

// `bin` holds the executable itself, so walking it would compare the binary against its own
// timestamp and report every fresh build as stale — the check would then cry wolf on every run.
func TestConverterStalenessIgnoresBuildOutput(t *testing.T) {
	root := syntheticConverterRoot(t)

	touchAll(t, root, time.Now().Add(-2*time.Hour))

	binDir := filepath.Join(root, "bin")

	// A .go file inside bin/ is the adversarial case: right extension, wrong directory.
	writeFile(t, filepath.Join(binDir, "go2cs.exe"), "binary")
	writeFile(t, filepath.Join(binDir, "generated.go"), "package main\n")
	touch(t, filepath.Join(binDir, "go2cs.exe"), time.Now())
	touch(t, filepath.Join(binDir, "generated.go"), time.Now())

	_, newestPath := newestConverterInput(root)

	if filepath.Base(newestPath) == "go2cs.exe" || filepath.Base(newestPath) == "generated.go" {
		t.Errorf("bin/ must be skipped or every fresh build reports itself stale; newest was %q", newestPath)
	}
}

// The source root is identified by MARKER FILES, never by directory name — a binary that merely
// sits in some `bin` directory must not be measured against whatever module it lands next to.
func TestConverterSourceRootRequiresBothMarkers(t *testing.T) {
	root := t.TempDir()

	if isConverterSourceRoot(root) {
		t.Error("an empty directory is not a converter source root")
	}

	writeFile(t, filepath.Join(root, "main.go"), "package main\n")

	if isConverterSourceRoot(root) {
		t.Error("main.go alone is not enough — any Go program has one")
	}

	writeFile(t, filepath.Join(root, "go.mod"), "module somethingelse\n\ngo 1.23.12\n")

	if isConverterSourceRoot(root) {
		t.Error("a DIFFERENT module must not be claimed as this converter's source")
	}

	writeFile(t, filepath.Join(root, "go.mod"), "module go2cs\n\ngo 1.23.12\n")

	if !isConverterSourceRoot(root) {
		t.Error("main.go plus `module go2cs` is this converter's source root")
	}
}

// A deployed or relocated binary with no adjacent source tree is legitimate and normal, and must be
// SILENT rather than warn about a tree it cannot see.
func TestAdjacentConverterSourceAbsentWhenDeployed(t *testing.T) {
	deployed := filepath.Join(t.TempDir(), "tools", "go2cs.exe")

	if err := os.MkdirAll(filepath.Dir(deployed), 0o755); err != nil {
		t.Fatal(err)
	}

	writeFile(t, deployed, "not really an executable")

	if dir, ok := adjacentConverterSource(deployed); ok {
		t.Errorf("a deployed binary has no converter source beside it, got %q", dir)
	}
}

// --- helpers -------------------------------------------------------------------------------------

// syntheticConverterRoot builds a miniature converter source tree: marker files, a nested internal/
// package, and an embedded asset named by a //go:embed directive.
func syntheticConverterRoot(t *testing.T) string {
	t.Helper()

	root := t.TempDir()

	writeFile(t, filepath.Join(root, "go.mod"), "module go2cs\n\ngo 1.23.12\n")
	writeFile(t, filepath.Join(root, "go.sum"), "")
	writeFile(t, filepath.Join(root, "main.go"), "package main\n\nfunc main() {}\n")
	writeFile(t, filepath.Join(root, "visitFuncDecl.go"), "package main\n")
	writeFile(t, filepath.Join(root, "csproj-template.xml"), "<Project />\n")
	writeFile(t, filepath.Join(root, "embeddedTemplates.go"),
		"package main\n\nimport _ \"embed\"\n\n//go:embed csproj-template.xml\nvar template string\n")
	writeFile(t, filepath.Join(root, "internal", "stdlibmeta", "meta.go"), "package stdlibmeta\n")

	return root
}

// writeFile is linkStagedFixtures_test.go's helper, reused rather than duplicated — same package,
// same signature, same MkdirAll-then-write semantics.

func touch(t *testing.T, path string, when time.Time) {
	t.Helper()

	if err := os.Chtimes(path, when, when); err != nil {
		t.Fatal(err)
	}
}

// touchAll backdates every file in the tree, so a subsequent touch is unambiguously the newest.
func touchAll(t *testing.T, root string, when time.Time) {
	t.Helper()

	err := filepath.WalkDir(root, func(path string, entry os.DirEntry, err error) error {
		if err != nil || entry.IsDir() {
			return err
		}

		return os.Chtimes(path, when, when)
	})

	if err != nil {
		t.Fatal(err)
	}
}
