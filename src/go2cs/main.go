// main.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// go2cs converts Go source code into C# that a Go developer can read and follow.
//
// This file is the ENTRY POINT and nothing else: it resolves the Go toolchain paths, parses the
// command line, and dispatches to exactly one of three modes —
//
//	-stdlib    convert the Go standard library      (stdLibConverter.go)
//	-recurse   convert an end-user module + its deps (moduleConverter.go)
//	 default   convert one file or one package       (conversionDriver.go)
//
// Everything main() needs lives beside it under a name that says what it owns; start with these:
//
//	commandLineOptions.go   the Options struct, the custom flag types, build-tag resolution
//	conversionDriver.go     processConversion — the order the passes run in for ONE package
//	visitorState.go         the Visitor: the per-file walker every conv*/visit* file extends
//	packageGlobalState.go   the registries concurrently-visited files publish to each other
//
// The conversion itself is spread across visit*.go (AST node -> C# declaration or statement) and
// conv*.go (expression or type -> C# text), with the analysis passes those depend on in
// *Operations.go. docs/Architecture.md has the full taxonomy.
package main

import (
	"flag"
	"fmt"
	"go/build"
	"io"
	"log"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"time"
)

func main() {
	var goRoot, goPath, go2csPath string
	var err error

	// Resolve GOROOT and GOPATH variables, any defined environment
	// variables will take precedence over derived values and command
	// line flags will override all
	if goRoot = os.Getenv("GOROOT"); len(goRoot) == 0 {
		if goRoot, err = getGoEnv("GOROOT"); err != nil {
			goRoot = runtime.GOROOT()
		}

		if len(goRoot) == 0 {
			log.Fatalln("Failed to resolve GOROOT path")
		}

		os.Setenv("GOROOT", goRoot)
	}

	if goPath = os.Getenv("GOPATH"); len(goPath) == 0 {
		if goPath, err = getGoEnv("GOPATH"); err != nil {
			goPath = build.Default.GOPATH
		}

		if len(goPath) == 0 {
			log.Fatalln("Failed to resolve GOPATH path")
		}

		os.Setenv("GOPATH", goPath)
	}

	// Resolve GO2CSPATH environment variable
	if go2csPath = os.Getenv("GO2CSPATH"); len(go2csPath) == 0 {
		homeDir, err := os.UserHomeDir()

		if err != nil {
			homeDir = strings.TrimSuffix(strings.TrimSuffix(goPath, "go"), string(os.PathSeparator))
		}

		go2csPath = filepath.Join(homeDir, "go2cs")

		os.Setenv("GO2CSPATH", go2csPath)
	}

	// Define command line flags for options
	commandLine := flag.NewFlagSet(os.Args[0], flag.ContinueOnError)
	commandLine.SetOutput(io.Discard)

	goRootCmd := commandLine.String("goroot", goRoot, "Path to Go root directory")
	goPathCmd := commandLine.String("gopath", goPath, "Path to Go path directory")
	go2csPathCmd := commandLine.String("go2cspath", go2csPath, "Path to C# converted code")
	convertStdLibCmd := commandLine.Bool("stdlib", false, "Convert Go standard library (implies -tags purego by default; pass an explicit -tags to override)")
	convertTestsCmd := commandLine.Bool("tests", false, "Convert eligible Go package tests and emit a runnable test host project")
	testActionCmd := commandLine.String("test-action", "convert", "Converted-test action: convert, build, run, compare, or all")
	testTimeoutCmd := commandLine.Duration("test-timeout", 2*time.Minute, "Timeout for each converted-test child process (build/run/compare)")
	var recurseVal recurseMode
	commandLine.Var(&recurseVal, "recurse", "Recursively convert an end-user module and its third-party dependencies (references the pre-converted standard library); use -recurse=nuget to reference the published go2cs NuGet packages (go.<pkg>/go.lib/go.gen) instead of local project references")
	targetPlatformCmd := commandLine.String("platforms", fmt.Sprintf("%s/%s", runtime.GOOS, runtime.GOARCH), "Target platform for conversion, format: os/arch")
	buildTagsCmd := commandLine.String("tags", "", "Comma-separated build tags applied when loading packages, e.g. -tags purego to select the portable Go implementations over assembly ones (with -stdlib, purego is applied by default and any explicit -tags value replaces it)")
	indentSpacesCmd := commandLine.Int("indent", 4, "Number of spaces for indentation")
	preferVarDeclCmd := commandLine.Bool("var", true, "Prefer \"var\" declarations")
	useChannelOperatorsCmd := commandLine.Bool("uco", true, fmt.Sprintf("Use channel operators: %s / %s", ChannelLeftOp, ChannelRightOp))
	includeCommentsCmd := commandLine.Bool("comments", false, "Include comments in output")
	parseCgoTargetsCmd := commandLine.Bool("cgo", false, "Parse cgo targets")
	showParseTreeCmd := commandLine.Bool("tree", false, "Show parse tree")
	csprojFileCmd := commandLine.String("csproj", "", "Path to custom .csproj template file")
	debugModeCmd := commandLine.Bool("debug", false, "Enable debug mode")

	var positionals []string
	positionals, err = parseArgsInterspersed(commandLine, os.Args[1:])

	// Pin go/build's resolver to the converter's robustly-resolved GOROOT/GOPATH. build.Default is
	// initialized at package-init from the start-up environment, which can be empty or stale in a
	// child process (e.g. the behavioral runner Execs go2cs.exe with a sparse env) — leaving
	// build.Import unable to find even stdlib packages like "fmt". Without this, getImportPackageInfo
	// falls through to the local-module path and emits a machine-specific absolute GOROOT reference.
	build.Default.GOROOT = *goRootCmd
	build.Default.GOPATH = *goPathCmd

	var inputFilePath string
	var convertStdLib bool

	if err == nil {
		convertStdLib = *convertStdLibCmd
	}

	if !convertStdLib && len(positionals) > 0 {
		inputFilePath = strings.TrimSpace(positionals[0])
	}

	// A bare `-stdlib` conversion (the whole-library corpus) applies `-tags purego` by default; the
	// converted standard library is defined to reproduce Go built with that tag (see
	// defaultStdLibBuildTags). Detect whether the caller passed `-tags` at all: if they did — even
	// `-tags=` to clear it — that is a deliberate override and we honor it verbatim.
	//
	// `-tests` gets the SAME purego default: a `-tests` run reconverts the package's PRODUCTION
	// sources and recompiles them into the test assembly, so it must select the exact same source
	// files the committed converted stdlib tree was built from (that tree is defined as Go built with
	// -tags purego). Without this, a stdlib package whose asm variant and pure-Go variant are gated
	// `!purego`/`purego` (crypto/subtle's xor_amd64.go declares a bodyless `func xorBytes` the .s
	// provides, while xor_generic.go declares the same func WITH a body) has BOTH files converted and
	// collides — CS0111 duplicate member — and the regenerated production .cs diverges from the
	// committed purego emission. A managed C# runtime can never execute the hand-written .s assembly,
	// so purego (the portable pure-Go implementations) is the correct default for a `-tests` run too.
	// `-recurse`/single-file conversions stay tag-neutral (their build tags govern).
	tagsExplicit := false
	commandLine.Visit(func(f *flag.Flag) {
		if f.Name == "tags" {
			tagsExplicit = true
		}
	})

	buildTags := resolveBuildTags(convertStdLib, *convertTestsCmd, tagsExplicit, parseBuildTags(*buildTagsCmd))

	if err != nil || (!convertStdLib && len(inputFilePath) == 0) {
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %s\n", err)
		}

		fmt.Fprintln(os.Stderr, `
 Usage: go2cs [options] <input_dir> [output_dir]
 
 Options:`)

		commandLine.SetOutput(nil)
		commandLine.PrintDefaults()

		fmt.Fprintln(os.Stderr, `
Examples:
  go2cs -indent 2 -var=false example.go conv/example.cs
  go2cs example.go
  go2cs -cgo=true input_dir output_dir
  go2cs package_dir
  go2cs -tests package_dir                  # Convert production sources and package tests
  go2cs -tests -test-action all package_dir # Convert, build, run, and compare with go test
  go2cs -stdlib                           # Convert the entire Go standard library (applies -tags purego by default)
  go2cs -stdlib fmt io/ioutil strings     # Convert specific standard library packages
  go2cs -recurse module_dir               # Convert a module + its third-party deps (references stdlib)
  go2cs -recurse module_dir output_root   # Same, with generated src/pkg trees isolated under output_root
  go2cs -recurse=nuget module_dir         # Same, but reference the go2cs stdlib from NuGet (go.*, no deploy-core)
  go2cs -stdlib -comments -tags purego    # Explicit form of the default: the portable Go crypto over the assembly ones
  go2cs -stdlib -tags=                    # Opt OUT of the purego default (reproduce the asm-backed default build)
 `)
		os.Exit(1)
	}

	options := Options{
		goRoot:              *goRootCmd,
		goPath:              *goPathCmd,
		go2csPath:           *go2csPathCmd,
		convertStdLib:       convertStdLib,
		convertTests:        *convertTestsCmd,
		testAction:          strings.ToLower(strings.TrimSpace(*testActionCmd)),
		testTimeout:         *testTimeoutCmd,
		recurse:             recurseVal.enabled,
		nugetRefs:           recurseVal.nuget,
		targetPlatform:      *targetPlatformCmd,
		buildTags:           buildTags,
		tagsExplicit:        tagsExplicit,
		indentSpaces:        *indentSpacesCmd,
		preferVarDecl:       *preferVarDeclCmd,
		useChannelOperators: *useChannelOperatorsCmd,
		includeComments:     *includeCommentsCmd,
		parseCgoTargets:     *parseCgoTargetsCmd,
		showParseTree:       *showParseTreeCmd,
		debugMode:           *debugModeCmd,
	}

	if options.convertTests {
		// -tests and -recurse compose badly today (the recursive module walk has its own
		// conversion driver and output routing); convert the module first, then its packages'
		// tests individually. Revisit when a recursive test-conversion mode is designed.
		if options.recurse {
			log.Fatalln("-tests cannot be combined with -recurse: convert the module first, then convert its package tests individually")
		}

		switch options.testAction {
		case "convert", "build", "run", "compare", "all":
		default:
			log.Fatalf("Invalid -test-action %q: expected convert, build, run, compare, or all\n", options.testAction)
		}

		if options.testTimeout <= 0 {
			log.Fatalln("-test-timeout must be greater than zero")
		}
	}

	// Load custom .csproj template if specified
	if *csprojFileCmd != "" {
		var err error
		csprojTemplate, err = os.ReadFile(*csprojFileCmd)

		if err != nil {
			log.Fatalf("Failed to read custom .csproj template file \"%s\": %s\n", *csprojFileCmd, err)
		}
	}

	if convertStdLib {
		// Initialize standard library converter
		converter := NewStdLibConverter(options)

		// Check if specific packages are specified
		var packageFilter []string

		if len(positionals) > 0 {
			packageFilter = make([]string, len(positionals))

			for i := range positionals {
				packageFilter[i] = strings.TrimSpace(positionals[i])
			}

			fmt.Printf("Only converting specified packages: %s\n", strings.Join(packageFilter, ", "))
		}

		// Run the conversion process
		if err := converter.ScanAndConvertFiltered(packageFilter); err != nil {
			log.Fatalf("Standard library conversion failed: %v", err)
		}
	} else {
		// Check if the input is a file or a directory
		fileInfo, err := os.Stat(inputFilePath)

		if err != nil {
			log.Fatalf("Failed to access input file path \"%s\": %s\n", inputFilePath, err)
		}

		isDir := fileInfo.IsDir()

		if !isDir {
			inputFilePath = filepath.Dir(inputFilePath)
		}

		var outputFilePath string

		// If the user has provided a second argument, we will use it as the output directory or file
		if len(positionals) > 1 {
			outputFilePath = strings.TrimSpace(positionals[1])
		} else {
			outputFilePath = inputFilePath
		}

		inputFilePath, err = filepath.Abs(inputFilePath)

		if err != nil {
			log.Fatalf("Failed to get absolute file path \"%s\": %s\n", inputFilePath, err)
			return
		}

		if options.recurse {
			// Recursive end-user conversion: convert the input module AND every third-party
			// dependency package in its transitive closure, in dependency order, referencing the
			// pre-converted standard library. The stdlib is not converted. A supplied second
			// positional is the writable recurse output root; without one, preserve the established
			// layout under -go2cspath.
			options.recurseOutputRoot = options.go2csPath

			if len(positionals) > 1 {
				options.recurseOutputRoot, err = filepath.Abs(outputFilePath)

				if err != nil {
					log.Fatalf("Failed to get absolute recurse output path %q: %v\n", outputFilePath, err)
				}
			}

			converter := NewModuleConverter(options)

			if err := converter.ConvertModule(inputFilePath); err != nil {
				log.Fatalf("Recursive module conversion failed: %s\n", err)
			}
		} else {
			// -tests always preserves comments: test conversions are derivative works of the
			// package's Go sources, and the per-file Go copyright/license header MUST survive
			// into the emitted *_test.cs (the same license requirement as -stdlib -comments) —
			// plus the doc comments are what keep the converted suite reviewable.
			if options.convertTests {
				options.includeComments = true

				// Resolve the output to an ABSOLUTE path up front so every downstream path is
				// absolute: the test .tests.csproj passed to `dotnet build`/`run` (executeTestAction
				// joins it onto this outputPath and runs from it), and the $(go2csPath) root walk
				// below. A relative output argument — the README's `src/core/...` — left
				// `dotnet run --project <relative>` resolving against the wrong working directory
				// (MSB1009), and left the located root relative so the emitted csproj's relative
				// go2csPath (via filepath.Rel, which needs matching absoluteness) came out broken and
				// non-reproducible. Absolute here makes the whole -tests flow invocation-independent.
				if abs, absErr := filepath.Abs(outputFilePath); absErr == nil {
					outputFilePath = abs
				}

				// Self-locate the project-reference root when the configured go2csPath is not
				// itself a valid root (no core\golib — e.g. the ~/go2cs default on a bare clone
				// with no deploy-core staging) and the OUTPUT lands inside a go2cs source tree.
				// Mutated HERE, before conversion AND the test actions, so the manifest's digest
				// (which folds the options) is written and validated with the same root — the
				// documented two-argument validation command then works from a clone with no
				// flags or environment setup. An explicitly configured WORKING root wins.
				if !isGo2CSRoot(options.go2csPath) {
					if root := findGo2CSRootAbove(outputFilePath); root != "" {
						options.go2csPath = root
					}
				}
			}

			// -tests: convert-and-hook runs for the convert/all actions (processConversion ends
			// by converting the package's tests); build/run/compare act on EXISTING artifacts
			// (manifest-validated) without reconverting.
			if !options.convertTests || options.testAction == "convert" || options.testAction == "all" {
				// The production sources about to be converted are RECOMPILED into the test
				// assembly, so their usings must be qualified against the UNION of the production
				// and _test.go reference closures — collect the test half first.
				if options.convertTests {
					collectSiblingTestClosure(inputFilePath, options)
				}

				processConversion(inputFilePath, isDir, outputFilePath, options)
			}

			if options.convertTests && options.testAction != "convert" {
				if err := executeTestAction(inputFilePath, outputFilePath, options); err != nil {
					log.Fatalf("Converted test action failed: %v\n", err)
				}
			}
		}
	}
}
