// commandLineOptions.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the converter's COMMAND LINE: the Options struct every later stage reads, the
// custom flag types the standard flag package cannot express on its own, and the build-tag
// resolution that decides which Go source files a conversion is even allowed to see.
//
// It sits apart from main.go because "what did the user ask for" is a stable, self-contained
// question, while main() is the sequence of things done about the answer. Option PLUMBING lives
// here; the conversion those options drive lives in conversionDriver.go.

package main

import (
	"flag"
	"fmt"
	"strings"
	"time"
	"unicode"
)

type Options struct {
	goRoot              string
	goPath              string
	go2csPath           string
	convertStdLib       bool
	recurse             bool
	recurseOutputRoot   string // -recurse: writable root for generated src\ + pkg\ trees; defaults to go2csPath
	mainModulePath      string // -recurse: import path of the app (main) module; routes its packages to src\, deps to pkg\
	nugetRefs           bool   // -recurse=nuget: reference the published go2cs NuGet packages (go.<pkg>/go.lib/go.gen) instead of local $(go2csPath) project references
	targetPlatform      string
	buildTags           []string // -tags: build tags applied to package loading AND constraint evaluation
	tagsExplicit        bool     // whether -tags was passed on the command line (vs. the -stdlib purego default)
	indentSpaces        int
	preferVarDecl       bool
	useChannelOperators bool
	includeComments     bool
	parseCgoTargets     bool
	showParseTree       bool
	debugMode           bool

	// -tests conversion options (dispatch is wired in a later stage; until then these are set only
	// by the test-conversion entry points and unit tests — the flag surface stays default-off)
	convertTests           bool          // convert the package's _test.go variants into a runnable test project
	testAction             string        // convert | build | run | compare | all
	testTimeout            time.Duration // per-child-command timeout for test build/run/compare actions
	testPackagePath        string        // import path of the package under test (self-import binding, IP-3)
	testPackageName        string        // package name of the package under test (self-import binding, IP-3)
	testWhiteboxReference  bool          // internal _test.go files emit into a bridge while production is referenced
	testInternalBridgeName string        // C# class that owns internal-test declarations under whitebox reference
	testClassNameOverride  string        // per-variant emitted package class override for internal test files
	testMetadataAnchorName string        // C# class that owns test-generated adapters under reference models
	testProductionPath     string        // original package path retained after reference-mode self-binding is cleared
	testProductionName     string        // original package name retained for white-box object routing
	testExternalVariant    bool          // current variant is the external <name>_test package
	testInlineTypeAccess   bool          // internal bridge types carry accessibility on their source declaration
	testFriendAssembly     bool          // production internals may be consumed by the separate test assembly
}

// recurseMode backs the -recurse flag. It is an optional-value boolean-ish flag: it implements
// IsBoolFlag so a bare `-recurse` (or `-recurse .`) sets the mode without consuming the next argument,
// while an explicit `-recurse=<value>` selects the reference style. `-recurse` / `-recurse=true` convert an
// end-user module against LOCAL project references ($(go2csPath)core\... staged by deploy-core), and
// `-recurse=nuget` instead emits NuGet PackageReferences (go.<pkg> stdlib + go.lib runtime + go.gen
// analyzer) so a converted app restores the go2cs stack from nuget.org with no local checkout.
type recurseMode struct {
	enabled bool
	nuget   bool
}

// IsBoolFlag lets the flag package treat a bare `-recurse` as a boolean (Set("true")) rather than
// consuming the following token as its value — so `go2cs -recurse .` keeps "." as a positional.
func (r *recurseMode) IsBoolFlag() bool { return true }

func (r *recurseMode) String() string {
	if r == nil || !r.enabled {
		return "false"
	}

	if r.nuget {
		return "nuget"
	}

	return "true"
}

func (r *recurseMode) Set(value string) error {
	switch strings.ToLower(strings.TrimSpace(value)) {
	case "", "true", "1", "on":
		r.enabled, r.nuget = true, false
	case "false", "0", "off":
		r.enabled, r.nuget = false, false
	case "nuget":
		r.enabled, r.nuget = true, true
	default:
		return fmt.Errorf("invalid -recurse value %q (want: (bare) | nuget | false)", value)
	}

	return nil
}

// parseArgsInterspersed parses fs while allowing flags to appear AFTER positional arguments. Go's
// flag package stops at the first non-flag token, so an invocation like
// `go2cs -recurse . -go2cspath dir` would silently drop -go2cspath (leaving the default output
// root). This peels off one positional at a time and re-parses the remainder, so flags interspersed
// with or following positionals are still applied. Returns the positionals in order and the first
// parse error, if any. fs must use flag.ContinueOnError so a parse error is returned, not fatal.
func parseArgsInterspersed(fs *flag.FlagSet, args []string) ([]string, error) {
	var positionals []string

	for {
		if err := fs.Parse(args); err != nil {
			return positionals, err
		}

		rest := fs.Args()

		if len(rest) == 0 {
			return positionals, nil
		}

		positionals = append(positionals, rest[0])
		args = rest[1:]
	}
}

// defaultStdLibBuildTags are the build tags a bare `-stdlib` conversion applies when the caller did
// not pass an explicit `-tags`. The converted standard library is defined to reproduce Go built with
// `-tags purego`: a managed C# runtime can never execute the hand-written `.s` assembly that the
// default (amd64/arm64/…) build binds hot crypto/hash functions to, so those declarations would
// convert to throwing stubs that COMPILE but cannot RUN. `purego` selects the portable pure-Go
// variants (real bodies the transpiler can convert), making "the corpus reproduces Go -tags purego"
// a claim go2cs can actually honor. This default applies to `-stdlib` (the whole-library corpus) AND
// to `-tests` (see resolveBuildTags); `-recurse` end-user conversions and single-file/dir conversions
// stay tag-neutral so the user's own build tags govern, and an explicit `-tags` overrides it verbatim.
//
// `math_big_pure_go` is the SAME decision under a different spelling. `purego` is the tag the crypto
// and hash packages happen to use; math/big predates it and gates its own portable fallbacks on
// `math_big_pure_go` instead (arith_decl_pure.go vs arith_decl.go + arith_amd64.go). Without it the
// converter selected arith_decl.go — eight bodyless `//go:linkname`/`//go:noescape` declarations whose
// bodies live in arith_$GOARCH.s — so `addVV`, `subVV`, `addVW`, `subVW`, `shlVU`, `shrVU`,
// `mulAddVWW` and `addMulVVW` all became throwing partial stubs and EVERY big.Int/big.Float/big.Rat
// arithmetic path panicked at run time while compiling clean. arith_decl_pure.go forwards each to the
// `_g` pure-Go implementation already in arith.go, which converts and runs. Reached as a `time`
// failure (TestTruncateRound → big.Int.Mul → mulAddVWW), but the scope is all of math/big.
var defaultStdLibBuildTags = []string{"purego", "math_big_pure_go"}

// resolveBuildTags picks the effective build tags for a conversion run. A bare `-stdlib` run and a
// `-tests` run both apply the purego default (unless `-tags` was passed explicitly, even `-tags=` to
// clear it — a deliberate override honored verbatim). `-tests` needs the SAME default as `-stdlib`
// because it reconverts the package's PRODUCTION sources and recompiles them into the test assembly:
// it must select the exact same source files the committed converted stdlib tree was built from (that
// tree is Go built with `-tags purego`). Without this, a package whose asm and pure-Go variants are
// gated `!purego`/`purego` (crypto/subtle's xor_amd64.go vs xor_generic.go, both declaring xorBytes)
// gets BOTH files converted and collides (CS0111), and the regenerated production .cs diverges from
// the committed purego emission. All other conversions stay tag-neutral (explicit tags only).
func resolveBuildTags(convertStdLib, convertTests, tagsExplicit bool, explicit []string) []string {
	if (convertStdLib || convertTests) && !tagsExplicit {
		return defaultStdLibBuildTags
	}

	return explicit
}

// parseBuildTags splits a -tags value into individual build tags. Commas and whitespace are both
// accepted as separators (the go command has used each form over time), and empty fields are dropped
// so "-tags=" is indistinguishable from omitting the flag.
func parseBuildTags(value string) []string {
	tags := strings.FieldsFunc(value, func(r rune) bool {
		return r == ',' || unicode.IsSpace(r)
	})

	if len(tags) == 0 {
		return nil
	}

	return tags
}

// loaderBuildFlags renders the go/packages BuildFlags for the configured build tags. It returns nil
// when no tags are set so the default load path stays byte-for-byte what it was before -tags existed.
func (o Options) loaderBuildFlags() []string {
	if len(o.buildTags) == 0 {
		return nil
	}

	return []string{"-tags=" + strings.Join(o.buildTags, ",")}
}
