// converterStaleness.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"bufio"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

// ---------------------------------------------------------------------------------------------
// THE STALE-BINARY SELF-CHECK — CLAUDE.md's false-green route #1, closed from INSIDE the binary.
//
// WHY IT IS HERE AND NOT IN A CALLER
//   Three harnesses already refuse to run a stale converter: BehavioralRunner, BehavioralTestBase
//   and PerformanceRunner all consult ConverterBuildInputs.IsConverterStale, and CNR plus the
//   validated sweep's default arm rebuild unconditionally. A 2026-08-31 census found the paths that
//   consult NOTHING, and they share one property — there is no caller to instrument:
//
//     1. `go2cs -tests -test-action convert|all` run by hand from a lane shell. THIS IS THE
//        INCIDENT PATH: it produced a `std.reflect.csproj` pair on 2026-08-30 from a binary
//        predating 433e9e4e0's GOROOT normalization, wrote them beside the committed project, and
//        exited 0. The artifacts survived ten hours and then aborted an unrelated CNR's
//        solution-integrity preflight on a duplicate `reflect` graph node — which is the only
//        reason anyone noticed.
//     2. `go2cs <pkg>` / `go2cs -stdlib` run by hand.
//     3. `run-validated-sweep.ps1 -SkipBuild`, whose `Test-Path` asks existence, never freshness.
//
//   The caller in cases 1 and 2 is a human at a shell, and IsConverterStale is C# this binary
//   cannot call. So the check moves inside: one probe at startup covers every invocation path that
//   exists or ever will, including the two no external predicate can reach.
//
// WHY IT WARNS AND NEVER REFUSES
//   Exactly the -go2cspath unresolved-root precedent: loud, once per run, on stderr, and
//   deliberately NOT fatal. A deliberately pinned binary is legitimate (bisecting a converter
//   regression is exactly that), and a deployed binary sitting away from any source tree is
//   legitimate and normal. Refusing would break both to catch a mistake a warning names just as
//   well — and a converter that refuses to run is a worse failure than one that says why it might
//   be wrong.
//
// WHY NO TOOLCHAIN COMPARISON (route #4's half)
//   Tempting, because runtime.Version() is free here where the C# must shell out to
//   `go version <exe>`. Skipped on purpose: the only cheap thing to compare it against in-process
//   is go.mod's `go` directive, which is a MINIMUM-version declaration and not a build pin, so any
//   legitimately newer toolchain would false-warn. Route #4 is already answered exactly — and
//   authoritatively — by the harness predicate comparing the embedded stamp against the live
//   `go env GOVERSION`. A weaker, false-positive-prone duplicate inside the binary would be worse
//   than not having one, so this covers the mtime half only and says so.
//
// THE INPUT SET mirrors ConverterBuildInputs.cs, which is the ONE definition of what the binary is
// built from: every .go under the converter source root (internal/ included), the //go:embed assets
// those files name, and go.mod/go.sum. Route #5 is why it is not a top-level *.go scan — editing a
// csproj template or stdlib-metadata.txt changes every project the converter emits while touching
// no .go file at all.
// ---------------------------------------------------------------------------------------------

// staleCheckOnce keeps the probe and its warning to once per process, however many times a
// conversion path asks.
var staleCheckOnce sync.Once

// warnIfConverterStale emits a single stderr warning when this executable is older than any of the
// converter sources it was built from. Silent when no converter source tree sits adjacent to the
// executable (a deployed or relocated binary), and silent when anything cannot be answered — this
// is advisory, so an unanswerable probe says nothing rather than guessing.
func warnIfConverterStale() {
	staleCheckOnce.Do(func() {
		executable, err := os.Executable()

		if err != nil {
			return
		}

		sourceDir, ok := adjacentConverterSource(executable)

		if !ok {
			return
		}

		info, err := os.Stat(executable)

		if err != nil {
			return
		}

		newest, newestPath := newestConverterInput(sourceDir)

		if newestPath == "" || !newest.After(info.ModTime()) {
			return
		}

		showWarning("go2cs is OLDER than its own sources - %q was modified after this binary was built,\n"+
			"         so this run emits the PREVIOUS converter's output while reporting success.\n"+
			"         Rebuild with \"go build -o bin/go2cs%s .\" in %s, or ignore this if the binary is pinned deliberately.",
			filepath.Base(newestPath), hostExeSuffix(), sourceDir)
	})
}

// hostExeSuffix is this host's executable suffix, so the remedy line names a command that works
// where it is read rather than a Windows one everywhere.
func hostExeSuffix() string {
	if strings.EqualFold(filepath.Ext(os.Args[0]), ".exe") {
		return ".exe"
	}

	return ""
}

// adjacentConverterSource resolves the converter source root from the executable's own location -
// the `bin/go2cs<exe>` to `src/go2cs` walk, the same shape resolveGo2CSPath already uses to
// self-locate a go2cs root. It is confirmed by MARKER FILES rather than by name, so a binary that
// merely happens to sit in some `bin` directory is never measured against an unrelated tree.
func adjacentConverterSource(executable string) (string, bool) {
	if candidate := filepath.Dir(filepath.Dir(executable)); isConverterSourceRoot(candidate) {
		return candidate, true
	}

	// A binary built into the source root itself (`go build` with no -o) has one less level
	// between it and the sources.
	if candidate := filepath.Dir(executable); isConverterSourceRoot(candidate) {
		return candidate, true
	}

	return "", false
}

// isConverterSourceRoot reports whether dir is THIS converter's source, not merely a Go module.
// Both markers are required: go.mod alone would match any module the binary happened to sit inside.
func isConverterSourceRoot(dir string) bool {
	if info, err := os.Stat(filepath.Join(dir, "main.go")); err != nil || info.IsDir() {
		return false
	}

	data, err := os.ReadFile(filepath.Join(dir, "go.mod"))

	if err != nil {
		return false
	}

	for _, line := range strings.Split(string(data), "\n") {
		if strings.TrimSpace(line) == "module go2cs" {
			return true
		}
	}

	return false
}

// newestConverterInput returns the most recent modification time across every build input under
// root, and the path that carried it. An empty path means nothing was readable.
func newestConverterInput(root string) (time.Time, string) {
	var (
		newest     time.Time
		newestPath string
	)

	consider := func(path string) {
		if info, err := os.Stat(path); err == nil && !info.IsDir() && info.ModTime().After(newest) {
			newest, newestPath = info.ModTime(), path
		}
	}

	_ = filepath.WalkDir(root, func(path string, entry os.DirEntry, err error) error {
		if err != nil {
			return nil
		}

		if entry.IsDir() {
			if path != root && isSkippedInputDirectory(entry.Name()) {
				return filepath.SkipDir
			}

			return nil
		}

		if !strings.HasSuffix(entry.Name(), ".go") {
			return nil
		}

		consider(path)

		for _, asset := range embeddedAssetPaths(path) {
			consider(asset)
		}

		return nil
	})

	for _, moduleFile := range []string{"go.mod", "go.sum"} {
		consider(filepath.Join(root, moduleFile))
	}

	return newest, newestPath
}

// isSkippedInputDirectory mirrors ConverterBuildInputs.cs: the three Go itself ignores when loading
// packages, plus the build output. `bin` is where the executable is written, so walking it would
// compare the binary against itself.
func isSkippedInputDirectory(name string) bool {
	return name == "" || name[0] == '.' || name[0] == '_' ||
		strings.EqualFold(name, "testdata") ||
		strings.EqualFold(name, "bin") ||
		strings.EqualFold(name, "obj")
}

// embeddedAssetPaths resolves the files a Go source file names in //go:embed directives, relative
// to that file's own directory. A directory match expands to its contents, which is what go:embed
// itself does. Derived from the directives rather than listed anywhere, so an asset added tomorrow
// is covered the day it is written - the same reasoning ConverterBuildInputs.cs gives, and the
// directive FORMS both rely on are pinned by embeddedAssets_test.go.
func embeddedAssetPaths(goFile string) []string {
	file, err := os.Open(goFile)

	if err != nil {
		return nil
	}

	defer file.Close()

	var (
		assets  []string
		fileDir = filepath.Dir(goFile)
		scanner = bufio.NewScanner(file)
	)

	scanner.Buffer(make([]byte, 0, 64*1024), 1024*1024)

	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())

		if !strings.HasPrefix(line, "//go:embed") {
			continue
		}

		for _, pattern := range strings.Fields(strings.TrimPrefix(line, "//go:embed")) {
			pattern = strings.Trim(pattern, "\"")
			pattern = strings.TrimPrefix(pattern, "all:")

			if pattern == "" {
				continue
			}

			matches, globErr := filepath.Glob(filepath.Join(fileDir, filepath.FromSlash(pattern)))

			if globErr != nil {
				continue
			}

			for _, match := range matches {
				info, statErr := os.Stat(match)

				if statErr != nil {
					continue
				}

				if !info.IsDir() {
					assets = append(assets, match)
					continue
				}

				_ = filepath.WalkDir(match, func(path string, entry os.DirEntry, walkErr error) error {
					if walkErr == nil && !entry.IsDir() {
						assets = append(assets, path)
					}

					return nil
				})
			}
		}
	}

	return assets
}
