package main

import (
	"fmt"
	"go/version"
	"os"
	"path/filepath"
	"runtime"
	"strings"

	"golang.org/x/mod/modfile"
)

// Go 1.21 gave a module the power to demand a toolchain NEWER than the one the user invoked, and the
// go command satisfies that demand by re-execing a downloaded toolchain whose GOROOT lives in the
// module cache. go/packages inherits that choice — it loads from the module directory
// (moduleConverter.go's cfg.Dir) — while the converter resolved GOROOT from the AMBIENT environment,
// so the two could describe different standard libraries entirely.
//
// That divergence is not cosmetic: every "is this package standard library?" decision keys off
// GOROOT (ModuleConverter.classify, getImportPackageInfo, getLocalModulePackageInfo,
// resolveGorootVendoredPath), so a mismatch reclassifies the WHOLE standard library as third-party
// and emits references to projects that were never generated — while the run exits 0. Root cause,
// measurements and the reproduction: docs/phase4/FINDING-toolchain-goroot-divergence.md.
//
// Asking `go env GOROOT` from the loader's directory is what makes the two agree — the same move
// loaderReleaseTags already makes for GOVERSION, and for the same reason. It costs a subprocess
// (~300ms on Windows), which is why toolchainSwitchPossible gates it: nothing in the converted
// corpus and no behavioral test asks for a newer toolchain, so that cost stays at exactly zero for
// every run that could not have been affected.

// normalizeGoVersion puts a Go version into the `goX.Y[.Z]` form go/version accepts, tolerating the
// two spellings that reach us: `go env GOVERSION` and go.mod's `toolchain` carry the `go` prefix,
// go.mod's `go` directive does not. Returns "" for anything not a recognizable release version —
// a devel toolchain, a malformed directive, an empty string — leaving callers on their fallback.
func normalizeGoVersion(goVersion string) string {
	goVersion = strings.TrimSpace(goVersion)

	if goVersion == "" {
		return ""
	}

	if !strings.HasPrefix(goVersion, "go") {
		goVersion = "go" + goVersion
	}

	if !version.IsValid(goVersion) {
		return ""
	}

	return goVersion
}

// moduleToolchainRequest returns the newest Go version the go.mod at moduleRoot asks for — the
// higher of its `go` and `toolchain` directives — in normalized `goX.Y.Z` form, or "" when there is
// no readable go.mod or neither directive names a usable version.
//
// Parsing is LAX on purpose. Strict Parse rejects any directive newer than the x/mod this converter
// was built against, which is exactly the forward-compatible case this check exists to serve: a
// go.mod written by the newer toolchain we are trying to detect. A go.mod whose SYNTAX is broken
// still yields "" — there is nothing to read — and the caller falls back to the ambient GOROOT,
// which is the pre-existing behavior.
func moduleToolchainRequest(moduleRoot string) string {
	if moduleRoot == "" {
		return ""
	}

	goModPath := filepath.Join(moduleRoot, "go.mod")
	contents, err := os.ReadFile(goModPath)

	if err != nil {
		return ""
	}

	parsed, err := modfile.ParseLax(goModPath, contents, nil)

	if err != nil || parsed == nil {
		return ""
	}

	request := ""

	if parsed.Go != nil {
		request = normalizeGoVersion(parsed.Go.Version)
	}

	if toolchain := normalizeGoVersion(toolchainDirective(parsed)); toolchain != "" {
		if request == "" || version.Compare(toolchain, request) > 0 {
			request = toolchain
		}
	}

	return request
}

// toolchainDirective reads the `toolchain` line out of a laxly-parsed go.mod.
//
// It cannot come from parsed.Toolchain: ParseLax discards every directive outside
// go/module/retract/require before it reaches the semantic layer (x/mod modfile/rule.go's
// non-strict gate), so that field is ALWAYS nil here no matter what the file says. Skipping this
// and trusting parsed.Toolchain reads `go 1.21` + `toolchain go1.25.0` as a 1.21 request — the
// shape `go mod tidy` writes whenever it bumps a module — and silently disables the GOROOT
// re-resolution in the most common case that needs it. The retained syntax tree still holds the
// line, so read it from there.
func toolchainDirective(parsed *modfile.File) string {
	if parsed == nil || parsed.Syntax == nil {
		return ""
	}

	for _, statement := range parsed.Syntax.Stmt {
		line, isLine := statement.(*modfile.Line)

		if !isLine || len(line.Token) < 2 || line.Token[0] != "toolchain" {
			continue
		}

		return line.Token[1]
	}

	return ""
}

// toolchainSwitchPossible reports whether the go command might run a toolchain other than the one
// running this converter when it loads from moduleRoot. It is the cheap precondition that keeps the
// GOROOT re-resolution free for the overwhelming majority of runs: reading one go.mod versus
// spawning `go env`.
//
// Two documented triggers, both checked without a subprocess: an explicit GOTOOLCHAIN naming a
// specific toolchain, and a module asking for a release newer than the running one. A GOTOOLCHAIN
// persisted by `go env -w` rather than set in the environment is NOT visible here — that case falls
// back to the ambient GOROOT, which is the pre-existing behavior, so the gate can only ever improve
// on it.
func toolchainSwitchPossible(moduleRoot string) bool {
	// `auto` and `local` are the two selectors that cannot by themselves force a different
	// toolchain; anything else names one (`go1.24.0`, `path`, `go1.24.0+auto`).
	if selector := strings.TrimSpace(os.Getenv("GOTOOLCHAIN")); selector != "" && selector != "auto" && selector != "local" {
		return true
	}

	request := moduleToolchainRequest(moduleRoot)

	if request == "" {
		return false
	}

	running := normalizeGoVersion(runtime.Version())

	if running == "" {
		// A devel or otherwise unrecognizable running toolchain: we cannot prove a switch is
		// impossible, so pay the subprocess rather than guess.
		return true
	}

	return version.Compare(request, running) > 0
}

// resolveLoaderGoRoot returns the GOROOT and GOVERSION of the toolchain the go command actually
// selects when loading packages from inputPath, together with whether that differs from the ambient
// GOROOT the converter resolved at start-up. The version is returned alongside because both describe
// the same toolchain and both are wrong together: GOROOT decides what counts as standard library,
// GOVERSION becomes the emitted $(GoStdLibVersion). The version is "" when it could not be read,
// which leaves goVersion() on its own ambient lookup.
//
// inputPath may name a directory or a single file; the module root is found by walking up from it.
// Every failure path returns the ambient GOROOT unchanged — an unreachable go command, an
// unparseable answer, or no module context at all are all legitimate (converting standalone code is
// a supported mode), and none of them is a reason to abandon a GOROOT that is usually correct.
func resolveLoaderGoRoot(inputPath string, ambientGoRoot string) (string, string, bool) {
	if strings.TrimSpace(inputPath) == "" {
		return ambientGoRoot, "", false
	}

	inputDir := inputPath

	if info, err := os.Stat(inputPath); err == nil && !info.IsDir() {
		inputDir = filepath.Dir(inputPath)
	}

	moduleRoot := moduleRootDir(inputDir)

	if moduleRoot == "" || !toolchainSwitchPossible(moduleRoot) {
		return ambientGoRoot, "", false
	}

	resolved, err := getGoEnvFrom(moduleRoot, "GOROOT")

	if err != nil {
		return ambientGoRoot, "", false
	}

	resolved = strings.TrimSpace(resolved)

	if resolved == "" || samePath(resolved, ambientGoRoot) {
		return ambientGoRoot, "", false
	}

	// Only now — with a switch confirmed — is the second subprocess worth spending.
	loaderVersion := ""

	if value, versionErr := getGoEnvFrom(moduleRoot, "GOVERSION"); versionErr == nil {
		loaderVersion = strings.TrimSpace(value)
	}

	return resolved, loaderVersion, true
}

// publishedStdLibRelease is the Go release this converter emits go.<pkg> NuGet references for. It is
// the release the binary itself was BUILT with, which is the same thing: the repository's
// version.props tracks its go.mod, so a go2cs built on Go 1.23.1 is the converter whose corpus ships
// as go.<pkg> 1.23.1.x. Reading it from runtime keeps the two in step with no build plumbing and no
// file to go stale.
func publishedStdLibRelease() string {
	return version.Lang(normalizeGoVersion(runtime.Version()))
}

// checkNuGetStdLibCompatibility rejects a -recurse=nuget conversion whose standard library comes from
// a DIFFERENT Go release than the one go2cs publishes packages for.
//
// Without it the run succeeds and hands back a project that cannot restore: the emitted
// $(GoStdLibVersion) names a release, and every package that release does not contain — the six
// crypto/* and weak packages Go 1.24 added, in the reported case — resolves to nothing. The user
// meets the problem as a wall of NU1101s naming packages they never heard of, at restore time, with
// nothing pointing back at the toolchain. Both numbers are known here, before a single file is
// written, so this is the honest place to stop.
//
// Only the language release is compared: 1.23.1 versus 1.23.5 is the same standard library and the
// floating revision in $(GoStdLibVersion) already covers it. A converting release that cannot be
// parsed is not grounds to refuse — it means the toolchain could not be interrogated, which is a
// legitimate standalone-conversion state.
func checkNuGetStdLibCompatibility(convertingRelease string, publishedRelease string) error {
	converting := version.Lang(normalizeGoVersion(convertingRelease))

	if converting == "" || publishedRelease == "" || converting == publishedRelease {
		return nil
	}

	return fmt.Errorf(
		"-recurse=nuget cannot be satisfied: go2cs publishes its converted standard library as go.<pkg> packages for %s, "+
			"but the module being converted resolves to %s. Packages that %s added or moved have no published counterpart, "+
			"so the generated project would fail to restore (NU1101) on exactly those. "+
			"Either pin the module and its dependencies to a %s-compatible set, or drop -recurse=nuget and reference a "+
			"locally converted standard library instead",
		publishedRelease, converting, converting, publishedRelease)
}
