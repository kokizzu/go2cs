// positionMapOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"encoding/base64"
	"encoding/binary"
	"go/token"
	"path/filepath"
	"runtime"
	"strconv"
	"strings"
)

// The POSITION MAP is how a converted program answers `runtime.Caller` — and every traceback built
// on it — with the GO position its C# position was converted from, rather than with the converted
// C# position alone.
//
// It is ONE record per converted file, emitted INTO that file as an `[assembly: GoPositionMap]`
// attribute, carrying both halves of a position: the Go file's identity and the C#-line → Go-line
// table. That is deliberate and is the design's central constraint — the coordinator ruling of
// 2026-08-21 makes the pair INDIVISIBLE, because a Go file paired with a C# line
// (`log/log_test.go:69`) is a position in NEITHER tree. One record cannot supply half a position:
// a frame either has one, and reports a Go position that exists, or has none, and reports the
// honest converted `.cs` position exactly as it did before this map existed. Nothing anywhere
// composes a file from one source and a line from another.
//
// Falling out of that: a whole-file hand-own ([module: GoManualConversion]) is never re-emitted, so
// it carries no record and reports its `.cs` position — which is the honest answer, since its C#
// was written rather than converted and no line of it corresponds to a line of Go. The rule needs
// no special case; it is the absence of a record.
//
// WHY A SENTINEL RATHER THAN A LINE COUNT. The emitted line a statement lands on is not knowable
// while the statement is being emitted: visitBlockStmt swaps in a fresh builder for a nested block
// and splices it back later, hoisted declarations are spliced AHEAD of the statement that produced
// them, and the using directives and type aliases are markers resolved only once the whole file has
// been visited. So the walk writes an invisible sentinel carrying the Go LINE into the text itself,
// where every one of those movements carries it along, and finalizePositionMap reads the finished
// text once — the only point at which C# line numbers exist at all — then strips the sentinels.
//
// ⚠ THE SENTINEL IS INVISIBLE IN THE EMITTED TEXT, NOT IN THE CONVERTER'S OWN READS OF IT. A
// sentinel costs nothing to any consumer of the finished file, but the converter itself inspects
// emitted text in places — a captured block is read back as a string and rewritten before it lands
// (popBlockAppend's callers) — and those reads see it. Measured: convFuncLit decides whether a
// single-return literal collapses to an expression-bodied lambda by testing that nothing but the
// block's opening brace precedes the `return`, and an un-stripped sentinel made that test fail for
// EVERY such literal in the corpus — 110 files emitted a block body instead. Nothing was wrong with
// the map; the text simply was not neutral.
//
// So: any site that INSPECTS or REWRITES captured block text must read it through
// stripPositionSentinels; any site that merely appends it must not, or the position is lost. The
// standing guard is the corpus itself — a seeded reconvert must be byte-identical to the committed
// tree except for each file's own attribute line, and the behavioral goldens pin the collapsed form
// directly — so a future non-neutral site shows up as drift rather than as a wrong line number.
//
// Design note: docs/phase4/DESIGN-position-map.md.

// PositionMapMarker reserves the file's one-line `[assembly: GoPositionMap]` slot. It is written
// BEFORE the walk and replaced after it, and the replacement is one line exactly as the marker is,
// so the line numbers the table records are the line numbers the emitted file has.
const PositionMapMarker = ">>MARKER:POSITIONMAP<<"

// PositionSentinel delimits an in-text Go-line sentinel: SENTINEL <decimal Go line> SENTINEL. NUL
// is the one byte that cannot occur in emitted C#, because the Go compiler disallows it in source
// text, so a sentinel can never collide with converted content — and a sentinel that somehow
// survived stripping would be a hard compile error rather than silent corruption.
const PositionSentinel = "\x00"

// positionEntry is one mapped line: the emitted C# line, and the Go line it was emitted for.
type positionEntry struct {
	csLine int
	goLine int
}

// writePositionSentinel records that what is emitted next came from goPos. Called at the points
// that HAVE a Go position and can hold a frame — every statement (visitStmt) and every function
// declaration (visitFuncDecl).
func (v *Visitor) writePositionSentinel(goPos token.Pos) {
	if !goPos.IsValid() || v.fset == nil {
		return
	}

	line := v.fset.Position(goPos).Line

	if line <= 0 {
		return
	}

	v.outputBuilder.WriteString(PositionSentinel + strconv.Itoa(line) + PositionSentinel)
}

// finalizePositionMap converts the sentinels the walk left in the finished file text into this
// file's position-map attribute, and strips them. Called once per emitted file, after every marker
// substitution, with the path the file is about to be written to — the identity rules below need to
// know where the C# lands relative to the Go it came from.
func (v *Visitor) finalizePositionMap(outputFileName string) {
	text := v.outputBuilder.String()

	if !strings.Contains(text, PositionSentinel) && !strings.Contains(text, PositionMapMarker) {
		return
	}

	stripped, entries := extractPositionSentinels(text, v.newline)

	attribute := "[assembly: " + globalQualifyRooted("go.GoPositionMap") + "(" +
		csharpStringLiteral(v.goSourceIdentity(outputFileName)) + ", " +
		csharpStringLiteral(positionMapCsName(outputFileName)) + ", " +
		csharpStringLiteral(encodePositionTable(entries)) + ")]"

	v.outputBuilder.Reset()
	v.outputBuilder.WriteString(strings.ReplaceAll(stripped, PositionMapMarker, attribute))
}

// positionMapCsName is the emitted file's own name, the key the runtime matches a frame's PDB file
// name against. The `.cs.auto` review sibling of a hand-own is named for the `.cs` it reviews, so
// its record reads as the hand-own's would — the sibling is never compiled, so nothing binds it.
func positionMapCsName(outputFileName string) string {
	return strings.TrimSuffix(filepath.Base(outputFileName), ".auto")
}

// extractPositionSentinels walks the finished text ONCE, binding each sentinel to the emitted line
// of the construct it marks, and removing it.
//
// WHICH LINE a sentinel marks is not the line it sits on. A statement is emitted as "newline,
// indent, text", so the sentinel written immediately before that emission lands at the END of the
// PRECEDING line — the line its own construct has not started yet. The rule that reads this
// correctly is positional rather than syntactic: a sentinel with content still to come on its own
// line marks THAT line (a `for`/`if`/`switch` init clause, emitted mid-header), and a sentinel with
// nothing after it marks the line that follows. Measured before the rule existed, every statement
// in the corpus bound one construct too early — a function's first statement was swallowed by the
// signature line and every later statement inherited its successor's Go line.
//
// The FIRST binding of an emitted line wins, and bindings only ever advance, so the table stays
// strictly ascending in the C# line — which is what gives the runtime's predecessor search its
// meaning. On a `for` header that means the `for` statement itself wins over its own init clause,
// which is the frame Go reports.
func extractPositionSentinels(text string, newline string) (string, []positionEntry) {
	lines := strings.Split(text, newline)
	entries := make([]positionEntry, 0, len(lines))
	bound := 0

	for index, line := range lines {
		if !strings.Contains(line, PositionSentinel) {
			continue
		}

		var rebuilt strings.Builder
		var marks []positionMark

		remainder := line

		for {
			open := strings.Index(remainder, PositionSentinel)

			if open < 0 {
				break
			}

			end := strings.Index(remainder[open+1:], PositionSentinel)

			if end < 0 {
				break
			}

			end += open + 1
			goLine, err := strconv.Atoi(remainder[open+1 : end])

			rebuilt.WriteString(remainder[:open])
			remainder = remainder[end+1:]

			if err == nil {
				marks = append(marks, positionMark{
					goLine:  goLine,
					ownLine: strings.TrimSpace(stripPositionSentinels(remainder)) != "",
				})
			}
		}

		rebuilt.WriteString(remainder)
		lines[index] = rebuilt.String()

		for _, mark := range marks {
			// index is 0-based; the emitted line is index+1, and the line after it index+2.
			csLine := index + 1

			if !mark.ownLine {
				csLine++
			}

			if csLine > len(lines) || csLine <= bound {
				continue
			}

			bound = csLine
			entries = append(entries, positionEntry{csLine: csLine, goLine: mark.goLine})
		}
	}

	return strings.Join(lines, newline), entries
}

// positionMark is one sentinel read off a line: the Go line it carries, and whether the construct it
// marks starts on that same emitted line or on the next one.
type positionMark struct {
	goLine  int
	ownLine bool
}

// stripPositionSentinels removes any remaining sentinel pairs from a line fragment, so the test for
// "is there real content after this sentinel" sees the emitted text rather than a later sentinel.
func stripPositionSentinels(fragment string) string {
	for {
		open := strings.Index(fragment, PositionSentinel)

		if open < 0 {
			return fragment
		}

		end := strings.Index(fragment[open+1:], PositionSentinel)

		if end < 0 {
			return fragment
		}

		fragment = fragment[:open] + fragment[open+end+2:]
	}
}

// encodePositionTable renders the map as Base64 over a delta stream — one record per mapped line,
// in ascending C# line order.
//
// A byte with the high bit set packs a whole record: bits 6-4 are ΔcsLine-1 and bits 3-0 are the
// zig-zag ΔgoLine, which covers the overwhelmingly common step (the next C# line for the next Go
// line) in ONE byte. A 0x00 byte introduces the extended form for anything wider: an unsigned varint
// ΔcsLine-1 followed by an unsigned varint zig-zag ΔgoLine. No other byte value below 0x80 is ever
// produced, so a decoder can reject a corrupt stream rather than mis-read it.
func encodePositionTable(entries []positionEntry) string {
	buffer := make([]byte, 0, len(entries)+len(entries)/4)
	previousCs, previousGo := 0, 0

	for _, entry := range entries {
		// Two statements can share an emitted line (an init clause); the first already recorded it.
		if entry.csLine <= previousCs {
			continue
		}

		advance := uint64(entry.csLine - previousCs - 1)
		delta := int64(entry.goLine - previousGo)
		zigzag := uint64(delta<<1) ^ uint64(delta>>63)

		previousCs, previousGo = entry.csLine, entry.goLine

		if advance <= 7 && zigzag <= 15 {
			buffer = append(buffer, byte(0x80|(advance<<4)|zigzag))
			continue
		}

		buffer = append(buffer, 0x00)
		buffer = binary.AppendUvarint(buffer, advance)
		buffer = binary.AppendUvarint(buffer, zigzag)
	}

	return base64.StdEncoding.EncodeToString(buffer)
}

// goSourceIdentity spells this file's Go source the way Go itself would have baked it for the same
// build. Go decides this at COMPILE time and go2cs decides it at CONVERSION time, which is the same
// decision point: cmd/go builds the standard library with -trimpath, so a GOROOT package's frames
// name `runtime/debug/stack.go`, while an ordinary user build bakes the absolute source path.
//
// Three forms, and the middle one exists for a reason worth stating: a conversion whose output is
// COMMITTED (the behavioral corpus, and any converted package that ships as source) cannot bake an
// absolute path, because that path names a directory that does not exist on the next clone — a
// fabricated position on every machine but the converting one, and corpus-wide golden drift on
// every machine including it. Recording the bare name instead, for the case where the Go source sits
// BESIDE its emitted C#, keeps the artifact machine-independent while letting the runtime root it
// against the C# file's own compile-time directory: the absolute path Go answers, naming a file that
// genuinely exists, derived from two recorded facts rather than composed from a namespace.
//
// The three forms are distinguishable without a discriminator field, and deliberately so: the GOROOT
// form always carries a separator (it is `<import path>/<stem>.go`), the beside-the-C# form never
// does, and the absolute form is rooted.
func (v *Visitor) goSourceIdentity(outputFileName string) string {
	source, err := filepath.Abs(v.sourceFilePath)

	if err != nil {
		source = filepath.Clean(v.sourceFilePath)
	}

	// A standard library source: GOROOT/src-relative, which IS the import-path form.
	if goRoot := strings.TrimSpace(v.options.goRoot); goRoot != "" {
		goRootSrc := filepath.Join(filepath.Clean(goRoot), "src")

		if isPathUnder(source, goRootSrc) {
			if relative, relErr := filepath.Rel(goRootSrc, source); relErr == nil {
				return filepath.ToSlash(relative)
			}
		}
	}

	// Converted beside its own source: the bare name, rooted by the runtime.
	if outputFileName != "" {
		if output, absErr := filepath.Abs(outputFileName); absErr == nil && sameDirectory(filepath.Dir(source), filepath.Dir(output)) {
			return filepath.Base(source)
		}
	}

	// Anything else — a -recurse module converted into its own output root — is what Go bakes for an
	// ordinary build: the absolute source path, forward-slashed as Go spells one everywhere.
	return filepath.ToSlash(source)
}

// sameDirectory compares two directory paths the way the host filesystem does — case-insensitively
// on Windows, where the same directory is legitimately spelled several ways (pathReplaceExact's
// rationale), exactly on every other platform.
func sameDirectory(left string, right string) bool {
	left = filepath.Clean(left)
	right = filepath.Clean(right)

	if runtime.GOOS == "windows" {
		return strings.EqualFold(left, right)
	}

	return left == right
}

// csharpStringLiteral renders a C# string literal. The three inputs are a slash-separated path, a
// file name and Base64, none of which can contain a quote or a backslash — the escaping is here so
// the emission is correct by construction rather than by that argument holding.
func csharpStringLiteral(value string) string {
	replacer := strings.NewReplacer("\\", "\\\\", "\"", "\\\"")
	return "\"" + replacer.Replace(value) + "\""
}
