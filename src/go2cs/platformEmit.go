// platformEmit.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the MULTI-PLATFORM EMISSION — the run that PRODUCES layout L3 (increment 2 of
// docs/phase4/DESIGN-multiplatform-corpus.md; platformLayout.go owns what L3 IS, and every rule for
// honoring it afterwards).
//
//	go2cs -stdlib -comments -platforms windows/amd64,linux/amd64,darwin/amd64 -go2cspath <repo>\src
//
// converts once per target and merges the three emissions into ONE tree, per L3: an artifact that is
// byte-identical on every target is written flat in its package directory, and anything else — a
// name whose content varies, or one only some targets emit at all — is written to that package's
// `<goos>/` subfolder, once per emitting target.
//
// **It is increment 1's census with a write step, deliberately.** The staging, the seeding ritual,
// the sentinel-modification-time "did this run emit it", the hand-own marker gate and the four-way
// classification are all reused verbatim from platformCensus.go / platformManifest.go — because the
// question a merge has to answer ("does this file vary across platforms?") is exactly the question
// the census already answers, and answering it twice in two places is how the two would drift. The
// design's §4.2 requirement rides along for free: the axis is computed from what the converter WROTE
// for each target, never from `go list` file sets, which are wrong in both directions.
//
// Three things this file is careful about:
//
//  1. **The seed root and the destination are the same tree.** Every read of the corpus (the seed
//     hashes, the hand-own census, the per-target staging copies) completes before the first byte of
//     the merge is written, so the run is a read-then-write, not a read-while-write.
//
//  2. **The merge is additive plus TARGETED removal.** It writes only the artifacts the runs
//     emitted, and removes only the OTHER candidate locations of those same artifacts (the flat copy
//     of a file that became per-GOOS, or the per-GOOS copies of one that became shared). Nothing
//     else in the corpus is touched — not hand-owned files, not committed test sources, not
//     `.gitignore`, not the root attribution files.
//
//  3. **It is idempotent.** Running it twice over the same corpus is a no-op: the staging roots are
//     seeded from an already-L3 tree, layout adoption reproduces that tree file for file, the
//     classification is taken over LOGICAL paths (a per-GOOS file classifies under its package, not
//     under `<pkg>/<goos>`), and every write goes through needToWriteFile.

package main

import (
	"fmt"
	"os"
	"path"
	"path/filepath"
	"strings"
)

// runPlatformEmission is the entry point for a `-platforms <two or more targets>` conversion: stage
// each target, classify the emissions, and merge them into the -go2cspath corpus as layout L3.
func runPlatformEmission(options Options, stageDir string, packageFilter []string) error {
	targets := options.targetPlatforms

	if len(targets) < 2 {
		return fmt.Errorf("multi-platform emission needs at least two -platforms targets (got %d)", len(targets))
	}

	// L3 keys its source folders by GOOS, so two targets sharing one GOOS would write into the same
	// folder and the second would silently overwrite the first. The GOARCH axis is real (design §5:
	// 16 packages) and is increment 5's subject; refuse rather than emit a tree that looks fine.
	seenGOOS := map[string]string{}

	for _, target := range targets {
		goos := goosOfTarget(target)

		if previous, exists := seenGOOS[goos]; exists {
			return fmt.Errorf("targets %q and %q share GOOS %q: layout L3 keys its source folders by GOOS, "+
				"so one emission per GOOS is the most this can write (the GOARCH axis is a later increment)",
				previous, target, goos)
		}

		seenGOOS[goos] = target
	}

	rootPath, err := filepath.Abs(options.go2csPath)

	if err != nil {
		return fmt.Errorf("failed to resolve output root %q: %w", options.go2csPath, err)
	}

	// The root is BOTH the seed every staging root is copied from and the destination the merge
	// writes into, so it has to be a real converted tree — an empty root would seed nothing, which
	// is the failure mode CLAUDE.md's reconvert ritual exists to prevent (every hand-owned file
	// would emit as a plain .cs).
	if !isGo2CSRoot(rootPath) {
		return fmt.Errorf("output root %q is not a converted go2cs tree (no %s under it); "+
			"a multi-platform emission seeds each target's staging root from it before merging back into it",
			rootPath, filepath.Join("core", "golib", "golib.csproj"))
	}

	stageDir, removeStage, err := resolveEmissionStageDir(stageDir)

	if err != nil {
		return err
	}

	if removeStage {
		defer os.RemoveAll(stageDir)
	}

	fmt.Printf("\nMulti-platform emission (layout L3)\n")
	fmt.Printf("  corpus root: %s\n", rootPath)
	fmt.Printf("  staging dir: %s\n", stageDir)
	fmt.Printf("  targets:     %s\n\n", strings.Join(targets, ", "))

	seed, err := readCensusSeed(filepath.Join(rootPath, "core"))

	if err != nil {
		return err
	}

	fmt.Printf("  seed holds %d files, %d of them hand-owned ([module: GoManualConversion])\n\n",
		len(seed.hashes), len(seed.markedFiles))

	emissions := make([]*platformEmission, 0, len(targets))

	// STRICTLY SEQUENTIAL, one fresh root each — the r41 never-convert-twice-into-one-root rule.
	for i, target := range targets {
		fmt.Printf("=== [%d/%d] emission target %s\n", i+1, len(targets), target)

		emission, err := runCensusTarget(options, rootPath, stageDir, target, packageFilter, seed)

		if err != nil {
			return fmt.Errorf("emission target %s failed: %w", target, err)
		}

		// A staging root whose seeding did not take measures a corpus that does not exist, and here
		// it would be MERGED into the real one. Fatal, not a footnote.
		if len(emission.markerGateViolations) > 0 {
			return fmt.Errorf("target %s emitted %d hand-owned file(s) as plain .cs — the staging root was not seeded correctly; "+
				"first offender: %s", target, len(emission.markerGateViolations), emission.markerGateViolations[0])
		}

		emissions = append(emissions, emission)
	}

	return mergePlatformEmissions(rootPath, targets, emissions)
}

// resolveEmissionStageDir resolves the staging directory: the caller's -platform-stage when given
// (kept afterwards, so a corpus-wide run can be inspected), otherwise a fresh temporary directory
// this run owns and removes. Reports whether the caller owns cleanup.
func resolveEmissionStageDir(stageDir string) (string, bool, error) {
	if stageDir = strings.TrimSpace(stageDir); len(stageDir) > 0 {
		resolved, err := filepath.Abs(stageDir)

		if err != nil {
			return "", false, fmt.Errorf("failed to resolve staging directory %q: %w", stageDir, err)
		}

		if err := os.MkdirAll(resolved, 0755); err != nil {
			return "", false, fmt.Errorf("failed to create staging directory %q: %w", resolved, err)
		}

		return resolved, false, nil
	}

	resolved, err := os.MkdirTemp("", "go2cs-platform-emit-")

	if err != nil {
		return "", false, fmt.Errorf("failed to create a staging directory: %w", err)
	}

	return resolved, true, nil
}

// mergedArtifact is one logical artifact's merge plan: where each emitting target's copy was staged.
type mergedArtifact struct {
	class   string
	staged  map[string]string // target -> absolute path of that target's staged copy
	emitted []string          // emitting targets, in target order
}

// mergePlatformEmissions writes the classified emissions into the corpus as layout L3.
func mergePlatformEmissions(rootPath string, targets []string, emissions []*platformEmission) error {
	coreDir := filepath.Join(rootPath, "core")

	// Classification is over LOGICAL paths — `<pkg>/<file>`, with any per-GOOS folder segment
	// already folded away by snapshotConvertedRoot — so an already-L3 corpus reclassifies to exactly
	// what it reclassified to when it was flat. Without that folding a second run would nest
	// `<pkg>/windows/windows/<file>`.
	hashesByTarget := make([]map[string]string, 0, len(targets))
	stagedByTarget := make([]map[string]string, 0, len(targets))

	for _, emission := range emissions {
		hashes := map[string]string{}
		staged := map[string]string{}

		for _, rawPath := range emission.emittedByExtension(".cs") {
			state := emission.artifacts[rawPath]
			logical := state.logicalPath(rawPath)
			hashes[logical] = state.hash
			staged[logical] = filepath.Join(emission.root, "core", filepath.FromSlash(rawPath))
		}

		hashesByTarget = append(hashesByTarget, hashes)
		stagedByTarget = append(stagedByTarget, staged)
	}

	classes := classifyPlatformEmissions(targets, hashesByTarget)
	plans := make(map[string]mergedArtifact, len(classes))

	for logical, class := range classes {
		plan := mergedArtifact{class: class.class, staged: map[string]string{}, emitted: class.emitters}

		for i, target := range targets {
			if stagedPath, ok := stagedByTarget[i][logical]; ok {
				plan.staged[target] = stagedPath
			}
		}

		plans[logical] = plan
	}

	written, removed, unchanged := 0, 0, 0
	layoutPackages := map[string]bool{}

	for _, logical := range sortedKeys(func() map[string]bool {
		keys := map[string]bool{}
		for logical := range plans {
			keys[logical] = true
		}
		return keys
	}()) {
		plan := plans[logical]
		pkg := artifactPackage(logical)

		// The root attribution files (core/VERSION, core/LICENSE, core/README.md …) belong to no
		// package and are re-copied verbatim by every conversion; they are not platform artifacts
		// and the merge leaves the corpus's own copies alone.
		if pkg == "." {
			continue
		}

		fileName := path.Base(logical)
		flatPath := filepath.Join(coreDir, filepath.FromSlash(logical))

		if plan.class == artifactIdentical {
			// Shared on every target: one flat copy, and the per-GOOS copies (if this artifact used
			// to vary) go away.
			changed, err := copyMergedFile(plan.staged[targets[0]], flatPath)

			if err != nil {
				return err
			}

			countWrite(changed, &written, &unchanged)

			for _, target := range targets {
				gone, err := removeIfPresent(filepath.Join(coreDir, filepath.FromSlash(pkg), goosOfTarget(target), fileName))

				if err != nil {
					return err
				}

				if gone {
					removed++
				}
			}

			continue
		}

		// Varies, or is emitted by only some targets: one copy per emitting target, in that
		// target's GOOS folder — and the flat copy, if the artifact used to be shared, goes away.
		for _, target := range plan.emitted {
			goos := goosOfTarget(target)
			changed, err := copyMergedFile(plan.staged[target], filepath.Join(coreDir, filepath.FromSlash(pkg), goos, fileName))

			if err != nil {
				return err
			}

			countWrite(changed, &written, &unchanged)
		}

		layoutPackages[pkg] = true

		gone, err := removeIfPresent(flatPath)

		if err != nil {
			return err
		}

		if gone {
			removed++
		}
	}

	projectFiles, companionWritten, err := mergeCompanionArtifacts(coreDir, emissions)

	if err != nil {
		return err
	}

	written += companionWritten

	// Every package that ended up with per-GOOS sources needs the conditioned <Compile Include>.
	// A single-target conversion adds it from the tree's own shape (projectFileWriter), but this
	// run is what CREATES that shape, so the first time a package becomes L3 the block is applied
	// here — after the sources have moved, so the predicate reads the finished layout.
	blockAdded := 0

	for _, pkg := range sortedKeys(layoutPackages) {
		added, err := applyLayoutBlockToProjectFile(filepath.Join(coreDir, filepath.FromSlash(pkg)))

		if err != nil {
			return err
		}

		if added {
			blockAdded++
		}
	}

	fmt.Printf("\n=== Layout L3 merge ===\n\n")
	fmt.Printf("Packages carrying per-GOOS sources   %6d\n", len(layoutPackages))
	fmt.Printf("Artifacts written                    %6d\n", written)
	fmt.Printf("Artifacts already current            %6d\n", unchanged)
	fmt.Printf("Stale copies removed                 %6d\n", removed)
	fmt.Printf("Project files given the L3 block     %6d\n", blockAdded)

	for _, pkg := range sortedKeys(layoutPackages) {
		fmt.Printf("  %s\n", pkg)
	}

	if len(projectFiles) > 0 {
		fmt.Printf("\n⚠ %d package(s) emitted a project file that DIFFERS across targets. The per-GOOS\n", len(projectFiles))
		fmt.Printf("  <ProjectReference> blocks those need are increment 3; the first target's project file\n")
		fmt.Printf("  was merged, so their non-%s builds will reference the wrong import set:\n", goosOfTarget(targets[0]))

		for _, pkg := range projectFiles {
			fmt.Printf("  %s\n", pkg)
		}
	}

	return nil
}

// countWrite tallies one merge write as changed or already-current.
func countWrite(changed bool, written *int, unchanged *int) {
	if changed {
		*written++
	} else {
		*unchanged++
	}
}

// mergeCompanionArtifacts merges the non-`.cs` artifacts a conversion emits — the project file, the
// per-package README, the packaging icons, and the `.cs.auto` review siblings. These stay FLAT: one
// package has one project file, one README and one icon regardless of platform.
//
// Returns the packages whose project file differs across targets (the conditioned
// <ProjectReference> work increment 3 owns) and the number of files written.
func mergeCompanionArtifacts(coreDir string, emissions []*platformEmission) ([]string, int, error) {
	names := map[string]bool{}
	written := 0

	for _, emission := range emissions {
		for rawPath, state := range emission.artifacts {
			if state.emitted && filepath.Ext(rawPath) != ".cs" && artifactPackage(rawPath) != "." {
				names[rawPath] = true
			}
		}
	}

	differing := map[string]bool{}

	for _, rawPath := range sortedKeys(names) {
		source := ""

		for _, emission := range emissions {
			if state, ok := emission.artifacts[rawPath]; ok && state.emitted {
				source = filepath.Join(emission.root, "core", filepath.FromSlash(rawPath))
				break
			}
		}

		if len(source) == 0 {
			continue
		}

		if strings.EqualFold(filepath.Ext(rawPath), ".csproj") && companionDiffersAcrossTargets(rawPath, emissions) {
			differing[artifactPackage(rawPath)] = true
		}

		changed, err := copyMergedFile(source, filepath.Join(coreDir, filepath.FromSlash(rawPath)))

		if err != nil {
			return nil, written, err
		}

		if changed {
			written++
		}
	}

	return sortedKeys(differing), written, nil
}

// companionDiffersAcrossTargets reports whether the targets that hold this artifact hold different
// bytes for it. Seeded (un-emitted) copies count: a target that did not rewrite its project file
// still HAS the correct one, which is exactly the reading buildCsprojSummary takes.
func companionDiffersAcrossTargets(rawPath string, emissions []*platformEmission) bool {
	first := ""

	for _, emission := range emissions {
		state, ok := emission.artifacts[rawPath]

		if !ok {
			continue
		}

		if len(first) == 0 {
			first = state.hash
			continue
		}

		if state.hash != first {
			return true
		}
	}

	return false
}

// applyLayoutBlockToProjectFile adds the L3 blocks to a package's project file when the package now
// carries per-GOOS sources and the file does not already say so. Reports whether it changed.
func applyLayoutBlockToProjectFile(packageDir string) (bool, error) {
	if !packageCarriesPlatformLayout(packageDir) {
		return false, nil
	}

	entries, err := os.ReadDir(packageDir)

	if err != nil {
		return false, fmt.Errorf("failed to read package directory %q: %w", packageDir, err)
	}

	for _, entry := range entries {
		if entry.IsDir() || !strings.EqualFold(filepath.Ext(entry.Name()), ".csproj") {
			continue
		}

		projectFileName := filepath.Join(packageDir, entry.Name())
		contents, err := os.ReadFile(projectFileName)

		if err != nil {
			return false, fmt.Errorf("failed to read project file %q: %w", projectFileName, err)
		}

		updated := []byte(applyPlatformLayoutBlocks(string(contents), projectFileName))

		if !needToWriteFile(projectFileName, updated) {
			return false, nil
		}

		if err := os.WriteFile(projectFileName, updated, 0644); err != nil {
			return false, fmt.Errorf("failed to write project file %q: %w", projectFileName, err)
		}

		return true, nil
	}

	return false, nil
}

// copyMergedFile copies a staged artifact to its merged destination, creating directories as
// needed and skipping the write when the bytes are already there (so timestamps stay meaningful and
// a re-run is a genuine no-op). Reports whether anything was written.
func copyMergedFile(source string, destination string) (bool, error) {
	contents, err := os.ReadFile(source)

	if err != nil {
		return false, fmt.Errorf("failed to read staged artifact %q: %w", source, err)
	}

	if !needToWriteFile(destination, contents) {
		return false, nil
	}

	if err := os.MkdirAll(filepath.Dir(destination), 0755); err != nil {
		return false, fmt.Errorf("failed to create output directory %q: %w", filepath.Dir(destination), err)
	}

	if err := os.WriteFile(destination, contents, 0644); err != nil {
		return false, fmt.Errorf("failed to write merged artifact %q: %w", destination, err)
	}

	return true, nil
}

// removeIfPresent deletes a stale copy of an artifact that moved between the flat and per-GOOS
// locations. Reports whether anything was removed.
func removeIfPresent(filePath string) (bool, error) {
	if _, err := os.Stat(filePath); err != nil {
		if os.IsNotExist(err) {
			return false, nil
		}

		return false, fmt.Errorf("failed to inspect %q: %w", filePath, err)
	}

	if err := os.Remove(filePath); err != nil {
		return false, fmt.Errorf("failed to remove stale artifact %q: %w", filePath, err)
	}

	return true, nil
}

// normalizeArtifactLogicalPaths folds a per-GOOS folder segment out of every artifact path, so the
// classification and the merge speak in `<pkg>/<file>` regardless of which layout the tree they were
// snapshotted from is in. A path is folded only when its directory holds no project file and its
// PARENT does — `internal/syscall/windows` is a converted package whose own name is a GOOS, and
// folding it would move its sources into `internal/syscall`.
func normalizeArtifactLogicalPaths(artifacts map[string]artifactState) {
	packageDirs := map[string]bool{}

	for relPath := range artifacts {
		if strings.EqualFold(path.Ext(relPath), ".csproj") {
			packageDirs[path.Dir(relPath)] = true
		}
	}

	for relPath, state := range artifacts {
		state.logical = relPath
		dir := path.Dir(relPath)

		if !packageDirs[dir] && isKnownGOOS(path.Base(dir)) && packageDirs[path.Dir(dir)] {
			state.logical = path.Join(path.Dir(dir), path.Base(relPath))
		}

		artifacts[relPath] = state
	}
}
