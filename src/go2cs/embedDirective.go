// embedDirective.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

// `//go:embed` support. Go's toolchain resolves the directive's patterns at BUILD time and the
// linker writes the bytes into the binary; the variable is initialized before any code runs. The
// converter had none of this: it preserved the directive as a comment and emitted the variable
// UNINITIALIZED, so crypto/internal/fips140test's TestACVP read a zero-byte config and
// internal/trace/traceviewer — a PRODUCTION package — shipped an embed.FS serving an empty tree.
// Sized at docs/phase4/DATA-embed-directives-1.24.13.md (42 real directives over 41 variables in
// 27 files at go1.24.13; 19 of them corpus-eligible, across four packages).
//
// THE SHAPE, ruled 2026-09-22:
//
//  1. The bytes become `<EmbeddedResource>` items with a deterministic LogicalName,
//     `go.embed/<import-path>[_test]/<slash-relative-path>`. Not the fixture-copy mechanism
//     (testConversion.go's `<None … CopyToOutputDirectory ExcludeFromSingleFile="true">`): that is
//     cwd-dependent and ExcludeFromSingleFile is the negation of what //go:embed promises.
//  2. The variable gets a STATIC FIELD INITIALIZER at its declaration, so the emitted C# still
//     reads like the Go and the value exists before the package class's .cctor completes.
//  3. THE WHOLE PATTERN WALK HAPPENS HERE, at conversion time — globs, directories, the hidden
//     rules, `all:`, the empty-directory drop, the synthesized directory entries, and the final
//     (dir, elem) order. That is the load-bearing decision: every rule that can be gotten subtly
//     wrong is on the side of the seam a Go-only lane can arm red-first, and the C# constructor
//     (the hand-owned src/core/embed/embed_impl.cs, the one file that may set FS.files) is left
//     with no decisions in it.
//  4. go2cs-gen is not involved at any point: no attribute, no record, no generated partial.
//
// ⚠ THE HIDDEN RULES ARE ASYMMETRIC, and that asymmetry is the whole difficulty. `//go:embed
// testdata` (a directory pattern) excludes every name beginning with '.' or '_' at every level.
// `//go:embed testdata/*` (a glob) INCLUDES `.hidden` and `_hidden` — the glob's own level matches
// them — but the walk BELOW each match is hidden-excluding again, so `.more` and `_more` inside
// `testdata/.hidden` stay out. One pattern kind, two rules, one level apart. Measured against
// embedtest's TestHidden, which asserts exactly those two lists.
package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"io/fs"
	"os"
	"path"
	"path/filepath"
	"sort"

	"golang.org/x/tools/go/packages"
	"strconv"
	"strings"
)

const (
	// embedResourceRoot prefixes every emitted LogicalName. A fixed root keeps embedded payloads in
	// one namespace no consumer of the assembly can collide with by accident.
	embedResourceRoot = "go.embed/"

	// embedTestHalfSuffix distinguishes the TEST half's resources from the production half's. Both
	// halves compile into ONE assembly under the recompile and white-box models, and
	// embed/internal/embedtest declares `global` in BOTH halves over overlapping patterns — so
	// without this the two variables would fight over one resource identity. In Go they are two
	// packages with two embeddings, so two identities is the faithful answer, not a workaround.
	embedTestHalfSuffix = "_test"
)

// embedEntry is one entry of a resolved directive: a file, or a synthesized directory marker whose
// Name carries the trailing slash embed.FS's own layout requires.
type embedEntry struct {
	Name     string // slash-separated, relative to the package directory; a directory ends in "/"
	SourceOS string // the on-disk path (OS separators); empty for a synthesized directory
	IsDir    bool
}

// embedTarget is one `//go:embed` variable, resolved.
type embedTarget struct {
	VarName    string       // the Go name, for diagnostics
	Patterns   []string     // as written, across every directive line
	Entries    []embedEntry // files and directory markers, in embed.FS's (dir, elem) order
	ImportPath string       // the Go import path the LogicalName prefix is built from
	TestHalf   bool
}

// embedPatternsFromDocs reads every `//go:embed` pattern from the given comment groups.
//
// ⚠ BOTH GROUPS ARE READ, and that is not belt-and-braces. visitGenDecl hands visitValueSpec the
// GENDECL's doc, which is where a bare `//go:embed p` + `var x T` puts it — but inside a
// parenthesized `var ( … )` block the directive attaches to the ValueSpec's OWN doc, and that is
// the form embed/internal/embedtest uses for 11 of its 16 variables. Reading one group finds a
// third of the corpus's directives.
//
// ⚠ AND THE SCAN IS INDENT-TOLERANT AND MULTI-LINE. A directive inside a parenthesized block is
// indented, so a `^//go:embed` test misses it (measured over GOROOT at 1.24.13: a line-start grep
// finds 34 of the 42 real directives — it misses fips140test's, the live blocker). Go also permits
// several directive lines over one variable, their patterns concatenated; embedtest has one.
// Patterns may be quoted or back-quoted, which go/build's own parseGoEmbed handles and a
// whitespace split does not: embedtest spells five of them `"testdata/hello.txt"`.
func embedPatternsFromDocs(docs ...*ast.CommentGroup) []string {
	var patterns []string

	for _, doc := range docs {
		if doc == nil {
			continue
		}

		for _, comment := range doc.List {
			rest, ok := strings.CutPrefix(strings.TrimSpace(comment.Text), "//go:embed")

			if !ok {
				continue
			}

			// "//go:embedx" is not a directive; a real one is followed by space or nothing.
			if rest != "" && !strings.ContainsAny(rest[:1], " \t") {
				continue
			}

			patterns = append(patterns, parseEmbedPatterns(rest)...)
		}
	}

	return patterns
}

// parseEmbedPatterns splits a directive's argument text into patterns, honouring the quoting forms
// the spec allows. Mirrors go/build's parseGoEmbed closely enough for the emission; an unterminated
// quote yields what was parsed so far and the caller's resolve step then reports the miss by name,
// rather than this silently treating the rest of the line as one pattern.
func parseEmbedPatterns(args string) []string {
	var patterns []string

	for {
		args = strings.TrimLeft(args, " \t")

		if args == "" {
			return patterns
		}

		switch args[0] {
		case '`':
			end := strings.Index(args[1:], "`")

			if end < 0 {
				return patterns
			}

			patterns = append(patterns, args[1:1+end])
			args = args[1+end+1:]
		case '"':
			end := 1

			for ; end < len(args); end++ {
				if args[end] == '\\' {
					end++
					continue
				}

				if args[end] == '"' {
					break
				}
			}

			if end >= len(args) {
				return patterns
			}

			unquoted, err := strconv.Unquote(args[:end+1])

			if err != nil {
				return patterns
			}

			patterns = append(patterns, unquoted)
			args = args[end+1:]
		default:
			if cut := strings.IndexAny(args, " \t"); cut >= 0 {
				patterns = append(patterns, args[:cut])
				args = args[cut:]
				continue
			}

			return append(patterns, args)
		}
	}
}

// embedNameHidden reports whether a path element is hidden by Go's embed rules: a name beginning
// with '.' or '_'. The rule is about the ELEMENT, never the whole path, which is why the caller
// applies it per walk step and not once over the joined name.
func embedNameHidden(name string) bool {
	return strings.HasPrefix(name, ".") || strings.HasPrefix(name, "_")
}

// resolveEmbedPatterns walks a directive's patterns against the package directory and returns the
// entries in embed.FS's own order. Returns an error naming the offending pattern when one matches
// nothing — the build fails in Go for exactly that, and a converter that quietly emitted an empty
// FS would reproduce the defect this file exists to remove.
func resolveEmbedPatterns(packageDir string, patterns []string) ([]embedEntry, error) {
	if len(patterns) == 0 {
		return nil, fmt.Errorf("no patterns")
	}

	files := map[string]string{} // slash-relative name -> on-disk path

	for _, pattern := range patterns {
		bare, allowHidden := strings.CutPrefix(pattern, "all:")

		if bare == "" || strings.HasPrefix(bare, "/") || strings.HasPrefix(bare, "../") || bare == ".." {
			return nil, fmt.Errorf("invalid pattern %q", pattern)
		}

		matched := 0

		// A glob matches AT ITS OWN LEVEL without the hidden rule — `testdata/*` legitimately names
		// `.hidden`. Everything BELOW a match is walked with the rule back on unless `all:` said
		// otherwise. A pattern with no meta character is a plain path and is likewise taken as
		// named, whether it is a file or a directory.
		var candidates []string

		if strings.ContainsAny(bare, "*?[") {
			globbed, err := filepath.Glob(filepath.Join(packageDir, filepath.FromSlash(bare)))

			if err != nil {
				return nil, fmt.Errorf("invalid pattern %q: %w", pattern, err)
			}

			candidates = globbed
		} else {
			candidate := filepath.Join(packageDir, filepath.FromSlash(bare))

			if _, err := os.Stat(candidate); err == nil {
				candidates = []string{candidate}
			}
		}

		for _, candidate := range candidates {
			info, err := os.Lstat(candidate)

			if err != nil {
				continue
			}

			// Symbolic links and irregular files are never embedded — the spec says so, and a
			// link would otherwise smuggle bytes from outside the module into the assembly.
			if info.Mode()&fs.ModeSymlink != 0 {
				continue
			}

			if !info.IsDir() {
				if !info.Mode().IsRegular() {
					continue
				}

				rel, err := filepath.Rel(packageDir, candidate)

				if err != nil {
					continue
				}

				files[filepath.ToSlash(rel)] = candidate
				matched++
				continue
			}

			added, err := walkEmbedDirectory(packageDir, candidate, allowHidden, files)

			if err != nil {
				return nil, err
			}

			matched += added
		}

		if matched == 0 {
			// An EMPTY DIRECTORY is not an error, it is ignored ("Matches for empty directories
			// are ignored"), so a pattern naming one contributes nothing and must not fail here.
			if len(candidates) > 0 {
				continue
			}

			return nil, fmt.Errorf("pattern %q matches no files", pattern)
		}
	}

	if len(files) == 0 {
		return nil, nil
	}

	return embedEntriesInFSOrder(files), nil
}

// walkEmbedDirectory adds every non-hidden regular file under root, returning how many it added.
// The hidden rule is applied PER ELEMENT at every level below the named root: the root itself was
// named (or glob-matched) by the caller and is included whatever it is called.
func walkEmbedDirectory(packageDir, root string, allowHidden bool, files map[string]string) (int, error) {
	added := 0

	err := filepath.WalkDir(root, func(current string, entry fs.DirEntry, err error) error {
		if err != nil {
			return err
		}

		if current != root && !allowHidden && embedNameHidden(entry.Name()) {
			if entry.IsDir() {
				return fs.SkipDir
			}

			return nil
		}

		if entry.IsDir() {
			return nil
		}

		info, err := entry.Info()

		if err != nil || info.Mode()&fs.ModeSymlink != 0 || !info.Mode().IsRegular() {
			return nil
		}

		rel, err := filepath.Rel(packageDir, current)

		if err != nil {
			return nil
		}

		files[filepath.ToSlash(rel)] = current
		added++

		return nil
	})

	return added, err
}

// embedEntriesInFSOrder turns the resolved file set into embed.FS's `files` list: every file, plus
// a synthesized marker for every directory on the path to one, sorted the way the struct comment in
// src/core/embed/embed.cs specifies — by (dir, elem), where dir is the parent (or ".") and elem the
// base, with a directory's own name carrying a trailing slash.
//
// ⚠ THE ORDER IS THE CONTRACT, not a tidiness: embed.FS's ReadDir does a BINARY SEARCH over this
// list and relies on a directory's contents being one contiguous run. A plain lexical sort over the
// full names puts "q/s/t" between "q/s/" and "q/v" and the search silently reads a short directory.
//
// An empty directory contributes no marker, because no file's path passes through it — which is
// exactly the spec's "matches for empty directories are ignored", falling out of the construction
// rather than needing a rule of its own.
func embedEntriesInFSOrder(files map[string]string) []embedEntry {
	directories := map[string]bool{}

	for name := range files {
		for parent := path.Dir(name); parent != "." && parent != "/"; parent = path.Dir(parent) {
			directories[parent] = true
		}
	}

	entries := make([]embedEntry, 0, len(files)+len(directories))

	for name, source := range files {
		entries = append(entries, embedEntry{Name: name, SourceOS: source})
	}

	for name := range directories {
		entries = append(entries, embedEntry{Name: name + "/", IsDir: true})
	}

	sort.Slice(entries, func(i, j int) bool {
		leftDir, leftElem := embedSplitName(entries[i].Name)
		rightDir, rightElem := embedSplitName(entries[j].Name)

		if leftDir != rightDir {
			return leftDir < rightDir
		}

		return leftElem < rightElem
	})

	return entries
}

// embedSplitName is the converter's half of embed.split: "q/s/" -> ("q", "s"), "p" -> (".", "p").
func embedSplitName(name string) (dir, elem string) {
	trimmed := strings.TrimSuffix(name, "/")

	if slash := strings.LastIndex(trimmed, "/"); slash >= 0 {
		return trimmed[:slash], trimmed[slash+1:]
	}

	return ".", trimmed
}

// embedResourcePrefix composes the LogicalName prefix for a package half.
func embedResourcePrefix(importPath string, testHalf bool) string {
	if testHalf {
		importPath += embedTestHalfSuffix
	}

	return embedResourceRoot + importPath + "/"
}

// embedResourceName is the LogicalName of one embedded FILE. Directory markers carry no bytes and
// therefore no resource — they exist only in the emitted entry list.
func embedResourceName(prefix, entryName string) string {
	return prefix + entryName
}

// ---------------------------------------------------------------------------------------------
// The per-package registry and the emission.
// ---------------------------------------------------------------------------------------------

// embedTargets accumulates every resolved `//go:embed` variable of the package currently being
// converted, in declaration order. Reset by resetPackageState with the other package registries;
// read by the project writers, which need the file list long after the declarations were emitted.
// Guarded by packageLock like the registries beside it.
var embedTargets []embedTarget

// registerEmbedTarget records one resolved directive.
func registerEmbedTarget(target embedTarget) {
	packageLock.Lock()
	embedTargets = append(embedTargets, target)
	packageLock.Unlock()
}

// currentEmbedTargets returns a copy of the package's registered targets.
func currentEmbedTargets() []embedTarget {
	packageLock.Lock()
	defer packageLock.Unlock()

	targets := make([]embedTarget, len(embedTargets))
	copy(targets, embedTargets)

	return targets
}

// resetEmbedTargets clears the registry between packages.
func resetEmbedTargets() {
	packageLock.Lock()
	embedTargets = nil
	packageLock.Unlock()
}

// embedInitializerKind classifies a declared variable's type into the three shapes the spec allows.
// It is asked of the GO type's underlying form, so a NAMED type (`type EmbedString string`) and an
// element-named slice (`[]T` where `type T byte`) classify as their base — the emitted expression
// then carries the converter's ordinary conversion to the declared type.
type embedInitializerKind int

const (
	embedKindNone embedInitializerKind = iota
	embedKindFS
	embedKindString
	embedKindBytes
)

// embedInitializerExpr renders the C# initializer for one target.
//
//	embed.FS   -> ΔEmbedFS(asm, prefix, [names…])   — the ordered entry list, directory markers
//	                                                  included; the helper looks up only the files.
//	string     -> ΔEmbedString(asm, prefix, name)
//	[]byte     -> ΔEmbedBytes<elem>(asm, prefix, name)
//
// The helpers live in the HAND-OWNED src/core/embed/embed_impl.cs, which is the only file that may
// set FS.files — it is unexported, so no other package class can reach it. That is an existing
// hand-own kind (CLAUDE.md, *One tree*), not a new mechanism.
//
// ⚠ A string or []byte variable takes exactly ONE pattern and therefore one file: the spec says so
// ("The //go:embed line for a variable of type string or []byte can have only a single pattern"),
// and the caller has already refused anything else, so this may index the entry list.
func embedInitializerExpr(kind embedInitializerKind, target embedTarget, packageClass, elementType string) string {
	prefix := embedResourcePrefix(target.ImportPath, target.TestHalf)
	assembly := fmt.Sprintf("typeof(%s).Assembly", packageClass)

	// ⚠ FULLY QUALIFIED, NEVER THE BARE `embed.` ALIAS. The alias exists only where the Go file
	// imports embed UNDER A NAME — and the spec REQUIRES a BLANK import (`_ "embed"`) for a string
	// or []byte variable, since nothing in the file names the package. crypto/internal/fips140test
	// is that file: its acvp_test.cs went from compiling to CS0103 'embed' at (84,48), a corpus
	// REGRESSION of one package introduced by this seat's first cut. The qualified form needs no
	// import at all and cannot be shadowed, which is what makes it the durable answer rather than
	// minting an alias the Go file never asked for.
	//
	// Census at 1.24.13: 22 files blank-import embed, 21 of them under cmd/ (outside the converted
	// corpus); crypto/internal/fips140test/acvp_test.go is the ONE corpus instance.
	embedPackage := globalQualifyRooted(RootNamespace + "." + getSanitizedImport("embed"+PackageSuffix))

	switch kind {
	case embedKindFS:
		names := make([]string, 0, len(target.Entries))

		for _, entry := range target.Entries {
			names = append(names, strconv.Quote(entry.Name))
		}

		return fmt.Sprintf("%s.ΔEmbedFS(%s, %s, [%s])", embedPackage, assembly, strconv.Quote(prefix), strings.Join(names, ", "))
	case embedKindString:
		return fmt.Sprintf("%s.ΔEmbedString(%s, %s, %s)", embedPackage, assembly, strconv.Quote(prefix), strconv.Quote(target.Entries[0].Name))
	case embedKindBytes:
		return fmt.Sprintf("%s.ΔEmbedBytes<%s>(%s, %s, %s)", embedPackage, elementType, assembly, strconv.Quote(prefix), strconv.Quote(target.Entries[0].Name))
	}

	return ""
}

// embedResourceItems renders the `<EmbeddedResource>` ItemGroup for a set of targets, or "" when
// none carries a file. `relativeTo` is the directory the emitted project file sits in, so the
// Include path is relative to the csproj exactly as the fixture items beside it are.
//
// A file reached by TWO targets (embedtest embeds overlapping patterns) is emitted ONCE per half:
// MSBuild refuses a duplicate EmbeddedResource item, and the LogicalName is a function of the half
// and the path alone, so one item serves every variable that names it.
func embedResourceItemLines(targets []embedTarget) string {
	seen := map[string]bool{}
	var items strings.Builder

	for _, target := range targets {
		prefix := embedResourcePrefix(target.ImportPath, target.TestHalf)

		for _, entry := range target.Entries {
			if entry.IsDir || entry.SourceOS == "" {
				continue
			}

			logical := embedResourceName(prefix, entry.Name)

			if seen[logical] {
				continue
			}

			seen[logical] = true

			// ⚠ THE INCLUDE NAMES THE STAGED COPY, which is why stageEmbedPayloads exists at all.
			// Pointing the item at the Go SOURCE tree instead produced
			// `Include="../../../..(×10)/<goroot>/src/crypto/internal/fips140test/acvp_capabilities.json"`
			// on the first emission read: a path that is different on every machine, that puts a
			// GOROOT spelling into a committed project file, and that cannot survive the package
			// being consumed from a package feed. The entry name IS the relative path, because the
			// payload is staged at the same position under the output package directory the
			// project file sits in.
			items.WriteString(fmt.Sprintf("\r\n    <EmbeddedResource Include=\"%s\" LogicalName=\"%s\" />",
				escapeXMLAttributeValue(entry.Name), escapeXMLAttributeValue(logical)))
		}
	}

	return items.String()
}

// embedResourceItemGroup wraps those items in their own labelled ItemGroup, for a project file that
// has no fixture group to join.
func embedResourceItemGroup(targets []embedTarget) string {
	items := embedResourceItemLines(targets)

	if items == "" {
		return ""
	}

	return "\r\n\r\n  <!-- //go:embed payloads. LogicalName is `go.embed/<import-path>[_test]/<path>`, which the\r\n" +
		"       emitted static field initializers read back through embed's ΔEmbed* helpers. An\r\n" +
		"       EmbeddedResource rather than a copied file: //go:embed promises the bytes are IN the\r\n" +
		"       assembly, which is what survives a single-file publish and Native AOT. -->\r\n" +
		"  <ItemGroup Label=\"GoEmbeddedResources\">" + items + "\r\n  </ItemGroup>"
}

// embedPayloadPaths returns every on-disk payload file the targets resolved to, sorted. The test
// conversion's input digest and the staleness inputs both read it: an embedded file is a
// conversion INPUT exactly as a `*_impl.cs` companion is, and without it editing one would leave a
// prior comparison looking valid — the emission would still carry yesterday's bytes.
func embedPayloadPaths(targets []embedTarget) []string {
	seen := map[string]bool{}
	var paths []string

	for _, target := range targets {
		for _, entry := range target.Entries {
			if entry.IsDir || entry.SourceOS == "" || seen[entry.SourceOS] {
				continue
			}

			seen[entry.SourceOS] = true
			paths = append(paths, entry.SourceOS)
		}
	}

	sort.Strings(paths)

	return paths
}

// embedTargetForSpec resolves the `//go:embed` directive on a package-level var spec, if there is
// one, and registers it. Returns the target and the initializer kind, or embedKindNone when the
// spec carries no directive.
//
// Refusals are LOUD and then INERT: a directive the converter cannot honour warns by name and the
// variable keeps its old uninitialized emission, which is exactly what every package got before
// this file existed. A silent skip would put the defect back with no way to see it, and a fatal
// would fail a whole conversion over one variable.
func (v *Visitor) embedTargetForSpec(valueSpec *ast.ValueSpec, declDoc *ast.CommentGroup, goType types.Type) (embedTarget, embedInitializerKind, string) {
	patterns := embedPatternsFromDocs(declDoc, valueSpec.Doc)

	if len(patterns) == 0 {
		return embedTarget{}, embedKindNone, ""
	}

	if len(valueSpec.Names) != 1 {
		showWarning("//go:embed must precede a single variable declaration; %d names declared", len(valueSpec.Names))
		return embedTarget{}, embedKindNone, ""
	}

	varName := valueSpec.Names[0].Name
	sourceFile := v.fset.Position(valueSpec.Pos()).Filename

	if sourceFile == "" {
		showWarning("//go:embed on \"%s\": the declaring file has no position, so its patterns cannot be resolved", varName)
		return embedTarget{}, embedKindNone, ""
	}

	kind, elementGoType := embedKindForType(goType)

	if kind == embedKindNone {
		showWarning("//go:embed on \"%s\": type must be a string type, a slice of a byte type, or embed.FS", varName)
		return embedTarget{}, embedKindNone, ""
	}

	entries, err := resolveEmbedPatterns(filepath.Dir(sourceFile), patterns)

	if err != nil {
		showWarning("//go:embed on \"%s\": %s", varName, err)
		return embedTarget{}, embedKindNone, ""
	}

	fileCount := 0

	for _, entry := range entries {
		if !entry.IsDir {
			fileCount++
		}
	}

	// The spec: a string or []byte variable takes ONE pattern and matches ONE file. Refusing here
	// is what lets embedInitializerExpr index the entry list, and it is the same refusal the Go
	// build makes rather than a convenience.
	if kind != embedKindFS && (len(patterns) != 1 || fileCount != 1) {
		showWarning("//go:embed on \"%s\": a string or byte-slice variable takes one pattern matching one file (%d patterns, %d files)", varName, len(patterns), fileCount)
		return embedTarget{}, embedKindNone, ""
	}

	// A SCALAR TARGET KEEPS ONLY ITS FILE. Directory markers are an embed.FS concept — Go embeds
	// exactly the one file for a string or []byte variable — and a pattern naming a file in a
	// SUBDIRECTORY synthesizes one, so the entry list is not a singleton even when the file set is.
	// Found by this function's own refusal on the first real emission read: eight of
	// embedtest's variants spell `testdata/hello.txt` and every one of them was refused
	// ("resolved to 2 entries where one file was required"), which is the loud path doing its job
	// on a real defect rather than on a bad directive.
	if kind != embedKindFS {
		entries = embedScalarEntries(entries)
	}

	target := embedTarget{
		VarName:    varName,
		Patterns:   patterns,
		Entries:    entries,
		ImportPath: v.embedImportPath(),
		TestHalf:   strings.HasSuffix(filepath.Base(sourceFile), "_test.go"),
	}

	registerEmbedTarget(target)

	// The ELEMENT type is rendered through the converter's own type renderer rather than from the
	// Go spelling: `[]EmbedUint8`'s element is a package-local named type whose C# name carries the
	// sanitization and qualification only that renderer knows.
	elementType := ""

	if elementGoType != nil {
		elementType = v.getCSharpTypeName(elementGoType)
	}

	return target, kind, elementType
}

// embedImportPath is the import path the LogicalName prefix is built from. v.pkg's path is the
// package under conversion; for a `-tests` variant go/packages reports the SUFFIXED forms
// ("crypto/internal/fips140test [crypto/internal/fips140test.test]" and "…_test"), so the path is
// normalised back to the production one and the half is carried by TestHalf instead — two ways of
// spelling the same distinction would eventually disagree.
func (v *Visitor) embedImportPath() string {
	if v.pkg == nil {
		return ""
	}

	importPath := v.pkg.Path()

	if bracket := strings.Index(importPath, " ["); bracket >= 0 {
		importPath = importPath[:bracket]
	}

	return strings.TrimSuffix(importPath, embedTestHalfSuffix)
}

// embedKindForType classifies a declared type, returning the C# element type for the byte-slice
// kind. The classification is over the UNDERLYING type, so `type EmbedString string` is a string
// kind and `[]T` over `type T byte` is a byte-slice kind whose element is T — the two shapes
// embedtest's TestAliases asserts, alongside `[]uint8`, `[]EmbedUint8` and `EmbedBytes`.
func embedKindForType(goType types.Type) (embedInitializerKind, types.Type) {
	if goType == nil {
		return embedKindNone, nil
	}

	if named, ok := types.Unalias(goType).(*types.Named); ok {
		if obj := named.Obj(); obj != nil && obj.Pkg() != nil && obj.Pkg().Path() == "embed" && obj.Name() == "FS" {
			return embedKindFS, nil
		}
	}

	switch underlying := goType.Underlying().(type) {
	case *types.Basic:
		if underlying.Info()&types.IsString != 0 {
			return embedKindString, nil
		}
	case *types.Slice:
		if elem, ok := underlying.Elem().Underlying().(*types.Basic); ok && (elem.Kind() == types.Byte || elem.Kind() == types.Uint8) {
			return embedKindBytes, underlying.Elem()
		}
	}

	return embedKindNone, nil
}

// embedBaseCSTypeName is the C# type a ΔEmbed* helper returns, so the caller can tell whether the
// declared type needs a conversion. Empty for the FS kind, which is always exactly embed.FS.
func embedBaseCSTypeName(kind embedInitializerKind, elementType string) string {
	switch kind {
	case embedKindString:
		return "@string"
	case embedKindBytes:
		return "slice<" + elementType + ">"
	}

	return ""
}

// registerProductionEmbedTargets resolves the PRODUCTION half's directives into the registry
// without emitting anything, for the one case the ordinary path cannot reach: under the RECOMPILE
// test model the production `.cs` files are compile items of the TEST assembly, so the production
// field initializers run there and look their resources up in THAT assembly — but the production
// csproj's ItemGroup belongs to a different one. Without this the production half's variables find
// nothing at test time, which is the original defect wearing a different hat.
//
// Only the entries are wanted: the initializers were already rendered, correctly, during the
// production pass. Under a reference model the production assembly is separate and carries its own
// resources, so the caller does not ask.
//
// ⚠ NO CORPUS PACKAGE REACHES THIS TODAY — the census found the only production directive in
// internal/trace/traceviewer, which has no recompile-model test project, and all 18 other
// corpus-eligible directives are in `_test.go` files. It is written because the emission path is
// one path by ruling, and named here so the absence of a gate behind it is visible.
func registerProductionEmbedTargets(pkg *packages.Package) {
	if pkg == nil || pkg.Fset == nil {
		return
	}

	importPath := pkg.PkgPath

	for _, file := range pkg.Syntax {
		for _, decl := range file.Decls {
			genDecl, ok := decl.(*ast.GenDecl)

			if !ok || genDecl.Tok != token.VAR {
				continue
			}

			for _, spec := range genDecl.Specs {
				valueSpec, ok := spec.(*ast.ValueSpec)

				if !ok {
					continue
				}

				patterns := embedPatternsFromDocs(genDecl.Doc, valueSpec.Doc)

				if len(patterns) == 0 {
					continue
				}

				sourceFile := pkg.Fset.Position(valueSpec.Pos()).Filename

				if sourceFile == "" || strings.HasSuffix(filepath.Base(sourceFile), "_test.go") {
					continue
				}

				entries, err := resolveEmbedPatterns(filepath.Dir(sourceFile), patterns)

				if err != nil || len(entries) == 0 {
					continue
				}

				name := ""

				if len(valueSpec.Names) > 0 {
					name = valueSpec.Names[0].Name
				}

				registerEmbedTarget(embedTarget{VarName: name, Patterns: patterns, Entries: entries, ImportPath: importPath})
			}
		}
	}
}

// stageEmbedPayloads copies every resolved payload into the output package directory, at the same
// relative position it holds in the Go package, so the emitted `<EmbeddedResource Include>` is a
// LOCAL relative path. This is what "the payload enters the corpus" means: the bytes are committed
// beside the converted sources, exactly as a staged test fixture is, and the project file carries
// no spelling of the machine it was converted on.
//
// Idempotent and churn-free: a payload already staged with identical bytes is left alone, so a
// reconvert of an unchanged package moves nothing (the same discipline needToWriteFile applies to
// every other emitted artifact). A payload that is ALSO a staged test fixture — embedtest's
// `testdata/*.txt` are both — lands at the identical path with identical bytes, so the two stagers
// agree by construction rather than by ordering.
func stageEmbedPayloads(targets []embedTarget, outputDir string) error {
	if outputDir == "" {
		return nil
	}

	for _, target := range targets {
		for _, entry := range target.Entries {
			if entry.IsDir || entry.SourceOS == "" {
				continue
			}

			data, err := os.ReadFile(entry.SourceOS)

			if err != nil {
				return fmt.Errorf("read //go:embed payload \"%s\": %w", entry.Name, err)
			}

			destination := filepath.Join(outputDir, filepath.FromSlash(entry.Name))

			if !needToWriteFile(destination, data) {
				continue
			}

			if err := os.MkdirAll(filepath.Dir(destination), 0755); err != nil {
				return fmt.Errorf("stage //go:embed payload \"%s\": %w", entry.Name, err)
			}

			if err := os.WriteFile(destination, data, 0644); err != nil {
				return fmt.Errorf("stage //go:embed payload \"%s\": %w", entry.Name, err)
			}
		}
	}

	return nil
}

// embedScalarEntries reduces a resolved entry list to the ONE file a string or []byte target
// embeds. Directory markers are an embed.FS concept — Go embeds exactly the file for a scalar — and
// a pattern naming a file in a SUBDIRECTORY synthesizes one, so the list is not a singleton even
// when the file set is.
//
// Extracted from embedTargetForSpec so it can be armed: as an inline loop the reduction was
// reachable only through a Visitor, and a plant that deleted it reddened nothing. It is also where
// the first emission read landed — eight of embed/internal/embedtest's variants spell
// `testdata/hello.txt` and every one was refused ("resolved to 2 entries where one file was
// required") until this existed.
func embedScalarEntries(entries []embedEntry) []embedEntry {
	for _, entry := range entries {
		if !entry.IsDir {
			return []embedEntry{entry}
		}
	}

	return nil
}
