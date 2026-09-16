// valueCloneStampMembers_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"fmt"
	"os"
	"path"
	"path/filepath"
	"regexp"
	"sort"
	"strings"
	"testing"
)

// THE CLASS THIS GUARDS, and it cost a build on 2026-09-08.
//
// go2cs-gen's TypeGenerator reads a [GoValueClone(...)] member list and emits a member ACCESS for
// each name. A name in the stamp with no matching declaration in the stamped type is therefore
// CS1061 at build time — "does not contain a definition for 'X'" — reported against GENERATED code,
// so the error names a .g.cs file and the defect is in a hand-written one.
//
// It reached the H5 ladder as `[GoValueClone("tls","createstack","Δtrace",…)] partial struct m`
// whose field is declared `trace`: the hand-own had been re-derived from an emission produced
// BEFORE a60eb2274 (the converter cut that spells a stamped field as the DECLARATION does), so the
// stamp carried the pre-fix Δ-prefixed spelling while the field carried the post-fix one.
//
// ⚠ IT IS AN INTERNAL-CONSISTENCY CHECK ON PURPOSE. It compares the stamp against the declaration
// BESIDE IT and never against an emission, which is what makes it exact and gives it no dependency
// on a built tree, a converted corpus, or a toolchain.
//
// ⚠ IT DOES NOT DUPLICATE valueCloneFieldSpelling_test.go, and the difference is the whole reason it
// exists. That guard unit-tests the CONVERTER's spelling functions against types.Var, so it
// guarantees correct EMISSION from a60eb2274 forward. This one reads the CORPUS AS COMMITTED, so it
// also sees a stamp that was correct when it was minted and went wrong when the rule beneath it
// changed, and a hand-edit to a committed file that the converter will never rewrite. Neither can
// see the other's class.
//
// ⚠ POPULATION — RE-DERIVED 2026-09-16 AT go1.24.13, AND THE EARLIER NOTE WAS FALSIFIED.
//
// The note this replaces read: "at the 1.23.12 corpus this scans ~145 marked files and finds ONE
// stamp carrying ONE member name … on the 1.24 tree the population is 4 stamps / 13 names (the
// release adds stamped types), so the guard THICKENS at the hop." That prediction is WRONG at the
// tree it predicted. Measured at claude/version-go1.24.13 30057d0c4a, over src/core:
//
//	files carrying [GoValueClone]                            125
//	of those, [module: GoManualConversion] hand-owns            0
//
// The hop RETIRED the guard's entire population. The one hand-own that carried a stamp at 1.23 —
// internal/sync/hashtriemap_whitebox.cs — is one of the six files the hop retired, and the 125 files
// that carry stamps at 1.24 are all CONVERTED, which the old scanner skipped BY DESIGN. So the guard
// did not thicken; it went VACUOUS, and its own vacuity arm is what reported that rather than a
// green that meant nothing. The arm did its job; the design under it was keyed to the wrong
// population.
//
// ⚠ THE POPULATION IS NOW EVERY .cs FILE UNDER src/core, CONVERTED ONES INCLUDED, and that is a
// widening of PURPOSE, not just of reach. The old rationale for skipping converted files was that
// "a converted file's stamp is the converter's own output and is the converter's to get right".
// That is true of a FRESH conversion and says nothing about the tree as COMMITTED: a corpus file is
// re-converted only when someone re-converts it, hand-owned FILES live inside converted packages,
// and a hand-edit to a converted file is exactly as invisible to valueCloneFieldSpelling_test.go as
// a frozen hand-own is. The hand-own count is still reported separately, because it is the number
// the original class was about and a reader deserves to see it go to zero rather than infer it.
//
// ⚠ RESOLVING A CONVERTED STAMP IS NOT THE SAME ACT AS RESOLVING A HAND-OWN'S, and four shapes had
// to be admitted before the corpus read zero. Each one is a place where a naive scanner reports a
// defect that is not there, and all four were found by MEASURING the false positives down rather
// than by reasoning about them:
//
//	858 → the stamp sits on a FORWARD PARTIAL with an empty body. A converted stamp lives in
//	      package_info.cs as `[GoValueClone("seq","scratchBuf")] internal partial struct halfConn {}`
//	      while the members are declared in conn.cs. Resolution is against the UNION of every partial
//	      declaration of that type, never against the one the stamp happens to sit on.
//	176 → a MULTI-DECLARATOR field: `internal halfConn @in, @out;` declares two members on one line,
//	      and a matcher whose continuation set is `; = => {` sees neither of them.
//	170 → a per-GOOS directory stamps a type declared in its PARENT. Written as a FALLBACK ("look up
//	      only when the own-directory union is empty") this never fires, because a per-GOOS directory
//	      usually declares its own partial too. It has to be a UNION.
//	  2 → the generator MINTS the member. A defined type over another [GoType] is declared bodiless —
//	      `[GoType("encoder")] partial struct EncoderBuffer;` — and go2cs-gen forwards Clone() to a
//	      single minted `Value`; see src/gen/go2cs-gen/Templates/InheritedType/InheritedTypeTemplate.cs,
//	      "Set when the converter stamped [GoValueClone(\"Value\")] on this wrapper".
//	  0 → the reading.
//
// ⚠ IT READS FILES OUTSIDE THIS MODULE (src\core), and cmd/go DROPS out-of-module files from the
// test input hash — so a cached PASS would survive a reintroduction. Run the converter suite with
// -count=1, which is what every gate in this repo already does, and which the neighbouring fleet
// identifier census carries the same warning about.

var (
	handOwnMarkerRe = regexp.MustCompile(`(?m)^[\t ]*\[module:[\t ]*(?:go\.)?GoManualConversion\]`)
	valueCloneRe    = regexp.MustCompile(`\[GoValueClone\(([^)]*)\)\][^\n]*?partial\s+(?:struct|class)\s+([\p{L}_][\p{L}\p{N}_]*)`)
	quotedNameRe    = regexp.MustCompile(`"([^"]+)"`)

	// Every partial type DECLARATION, wherever it appears. This is what builds the per-directory
	// index a stamp resolves against — the stamp's own declaration is one member of that set and
	// carries no privilege.
	partialTypeDeclRe = regexp.MustCompile(`partial\s+(?:struct|class)\s+([\p{L}_][\p{L}\p{N}_]*)`)

	// A defined type whose underlying is ANOTHER [GoType] is emitted BODILESS. go2cs-gen mints its
	// members, so the declaration a stamp would be checked against does not exist in the corpus at
	// all — see the InheritedType template quoted above.
	goTypeWrapperRe = regexp.MustCompile(`\[GoType\("[^"]*"\)\][\t ]*partial\s+(?:struct|class)\s+([\p{L}_][\p{L}\p{N}_]*)[\t ]*;`)

	csIdentifierRe = regexp.MustCompile(`^@?[\p{L}_][\p{L}\p{N}_]*$`)
)

// valueCloneMintedMembers are the member names go2cs-gen MINTS on a bodiless [GoType("…")] wrapper.
// A stamp may name one of these on such a type without the corpus declaring it anywhere.
var valueCloneMintedMembers = map[string]bool{"Value": true}

// stampFinding is one stamped member with no declaration in the type it stamps.
type stampFinding struct {
	File   string
	Type   string
	Member string
}

func (f stampFinding) String() string {
	return fmt.Sprintf("%s: [GoValueClone] on %q names %q, which that type does not declare", f.File, f.Type, f.Member)
}

// valueCloneScan is one reading of the corpus, with the POPULATION beside the findings. Every count
// here is named in the vacuity message, because a zero is only a measurement when the thing that
// could have been non-zero is stated.
type valueCloneScan struct {
	Findings     []stampFinding
	Files        int // .cs files read
	StampFiles   int // files carrying at least one [GoValueClone]
	HandOwnFiles int // of those, [module: GoManualConversion] hand-owns
	Stamps       int
	Members      int
}

// bracedBodyAt returns the brace-balanced body beginning at the first '{' at or after `from`.
// Returning "" means no body was found, and a caller must SKIP rather than treat that as a finding:
// an unparsed declaration is the scanner failing to read, not the file failing to declare.
func bracedBodyAt(text string, from int) string {
	i := strings.Index(text[from:], "{")
	if i < 0 {
		return ""
	}
	i += from
	depth := 0
	for j := i; j < len(text); j++ {
		switch text[j] {
		case '{':
			depth++
		case '}':
			depth--
			if depth == 0 {
				return text[i : j+1]
			}
		}
	}
	return ""
}

// declBodyAfterName returns the body of a type declaration whose NAME ends at `from`, or "" when
// the declaration is bodiless (`[GoType("encoder")] partial struct EncoderBuffer;`). Scanning
// forward for the first '{' unconditionally is precisely what makes a bodiless declaration adopt
// the NEXT type's body, so the terminator is whichever of '{' and ';' arrives first outside any
// bracket.
func declBodyAfterName(text string, from int) string {
	depth := 0
	for i := from; i < len(text); i++ {
		switch text[i] {
		case '<', '(', '[':
			depth++
		case '>', ')', ']':
			if depth > 0 {
				depth--
			}
		case ';':
			if depth == 0 {
				return ""
			}
		case '{':
			if depth == 0 {
				return bracedBodyAt(text, i)
			}
		}
	}
	return ""
}

// declaresMember reports whether `body` declares a field or property named `name`. The name is
// matched only where a declaration can begin — after whitespace or a type-closing character — and
// only where a declaration can continue, which keeps it from matching the name inside a longer
// identifier or inside a call.
func declaresMember(body, name string) bool {
	re := regexp.MustCompile(`(^|[\s<>\]\)])` + regexp.QuoteMeta(name) + `\s*(;|=[^=]|=>|\{)`)
	return re.MatchString(body)
}

// multiDeclaratorNames returns the names declared by MULTI-DECLARATOR field lines in `body` —
// `internal halfConn @in, @out;` declares two. declaresMember cannot see them because its
// continuation set has no comma, and simply ADDING a comma to that set would make it match an
// argument inside any call (`f(a, x, y)` has `x` between a space and a comma). So the comma case is
// read as a whole LINE instead: a declarator list is a line that ends in ';' and contains no
// bracket, brace or '=', which no call or initializer can satisfy.
func multiDeclaratorNames(body string) map[string]bool {
	names := map[string]bool{}

	for _, line := range strings.Split(body, "\n") {
		line = strings.TrimSpace(line)

		if !strings.HasSuffix(line, ";") || !strings.Contains(line, ",") {
			continue
		}

		if strings.ContainsAny(line, "(){}[]=") {
			continue
		}

		parts := strings.Split(strings.TrimSuffix(line, ";"), ",")

		if len(parts) < 2 {
			continue
		}

		candidates := make([]string, 0, len(parts))
		ok := true

		for i, part := range parts {
			part = strings.TrimSpace(part)

			if i == 0 {
				// `internal halfConn @in` — the declared name is the last token; everything before
				// it is modifiers and the type.
				fields := strings.Fields(part)

				if len(fields) < 2 {
					ok = false
					break
				}

				part = fields[len(fields)-1]
			}

			if !csIdentifierRe.MatchString(part) {
				ok = false
				break
			}

			candidates = append(candidates, part)
		}

		if !ok {
			continue
		}

		for _, candidate := range candidates {
			names[candidate] = true
		}
	}

	return names
}

// valueCloneTypeIndex holds, PER DIRECTORY, every partial type declaration's body and every
// bodiless [GoType("…")] wrapper. A stamp resolves against the union over its own directory AND its
// parent — the union, not a fallback, because a per-GOOS directory usually declares its own partial
// of the type as well, and a fallback keyed on "only when the own-directory union is empty" would
// therefore never fire for the case it was written for.
type valueCloneTypeIndex struct {
	bodies   map[string]map[string][]string
	wrappers map[string]map[string]bool
}

func newValueCloneTypeIndex() *valueCloneTypeIndex {
	return &valueCloneTypeIndex{
		bodies:   map[string]map[string][]string{},
		wrappers: map[string]map[string]bool{},
	}
}

func (x *valueCloneTypeIndex) addFile(dir, text string) {
	for _, m := range partialTypeDeclRe.FindAllStringSubmatchIndex(text, -1) {
		name := text[m[2]:m[3]]
		body := declBodyAfterName(text, m[3])

		if body == "" {
			continue
		}

		if x.bodies[dir] == nil {
			x.bodies[dir] = map[string][]string{}
		}

		x.bodies[dir][name] = append(x.bodies[dir][name], body)
	}

	for _, m := range goTypeWrapperRe.FindAllStringSubmatch(text, -1) {
		if x.wrappers[dir] == nil {
			x.wrappers[dir] = map[string]bool{}
		}

		x.wrappers[dir][m[1]] = true
	}
}

// valueCloneScope returns the directories a stamp in `dir` resolves against.
//
// ⚠ The terminal case is `parent == dir` and NOTHING ELSE. An earlier form also excluded "." and
// "/", which reads as harmless — src/core's own subdirectories are never "." — and is exactly what
// makes the per-GOOS rule untestable, because a planted tree rooted at t.TempDir() has its parent
// directory spelled ".". The control below caught it; the corpus arm could not have, and a rule
// whose only proof is the corpus is a rule with no control at all.
func valueCloneScope(dir string) []string {
	parent := path.Dir(dir)

	if parent == dir {
		return []string{dir}
	}

	return []string{dir, parent}
}

func (x *valueCloneTypeIndex) declares(dir, typeName, member string) bool {
	for _, d := range valueCloneScope(dir) {
		for _, body := range x.bodies[d][typeName] {
			if declaresMember(body, member) {
				return true
			}

			if multiDeclaratorNames(body)[member] {
				return true
			}
		}

		if x.wrappers[d][typeName] && valueCloneMintedMembers[member] {
			return true
		}
	}

	return false
}

// scanValueCloneStamps returns every stamped member that its own type does not declare, over EVERY
// .cs file under root. It reads the tree twice on purpose: once to index the declarations, once to
// resolve the stamps against that index. A one-pass scanner is what produced the 858 false
// positives, because at the moment it reaches a stamp it has not yet seen the file that declares
// the type.
func scanValueCloneStamps(root string) (valueCloneScan, error) {
	var (
		scan  valueCloneScan
		index = newValueCloneTypeIndex()
		texts = map[string]string{}
		dirs  = map[string]string{}
		order []string
	)

	walkErr := filepath.Walk(root, func(p string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}

		if info.IsDir() {
			switch info.Name() {
			case "bin", "obj", "Generated":
				return filepath.SkipDir
			}

			return nil
		}

		if !strings.HasSuffix(p, ".cs") {
			return nil
		}

		raw, readErr := os.ReadFile(p)

		if readErr != nil {
			return readErr
		}

		rel, relErr := filepath.Rel(root, p)

		if relErr != nil {
			rel = p
		}

		rel = filepath.ToSlash(rel)
		dir := path.Dir(rel)

		scan.Files++
		texts[rel] = string(raw)
		dirs[rel] = dir
		order = append(order, rel)
		index.addFile(dir, string(raw))

		return nil
	})

	if walkErr != nil {
		return scan, walkErr
	}

	sort.Strings(order)

	for _, rel := range order {
		text := texts[rel]
		matches := valueCloneRe.FindAllStringSubmatchIndex(text, -1)

		if len(matches) == 0 {
			continue
		}

		scan.StampFiles++

		if handOwnMarkerRe.MatchString(text) {
			scan.HandOwnFiles++
		}

		for _, m := range matches {
			scan.Stamps++

			args := text[m[2]:m[3]]
			typeName := text[m[4]:m[5]]

			for _, nm := range quotedNameRe.FindAllStringSubmatch(args, -1) {
				scan.Members++

				if !index.declares(dirs[rel], typeName, nm[1]) {
					scan.Findings = append(scan.Findings, stampFinding{File: rel, Type: typeName, Member: nm[1]})
				}
			}
		}
	}

	sort.Slice(scan.Findings, func(i, j int) bool { return scan.Findings[i].String() < scan.Findings[j].String() })

	return scan, nil
}

// TestValueCloneStampMembersAreDeclared is the guard.
func TestValueCloneStampMembersAreDeclared(t *testing.T) {
	root := repoRootFromPackageDir(t)
	core := filepath.Join(root, "src", "core")

	if _, err := os.Stat(core); err != nil {
		t.Fatalf("converted corpus not found at %s: %v", core, err)
	}

	scan, err := scanValueCloneStamps(core)

	if err != nil {
		t.Fatalf("scanning %s: %v", core, err)
	}

	// A zero that could not have been anything else is not a measurement — and this guard has
	// already been emptied ONCE, by a hop that retired the single hand-own its old population
	// rested on. So the vacuity arm NAMES the population rather than asserting a bare non-zero:
	// whoever reads the failure gets the numbers that would let them tell a retired population
	// from a broken predicate, which is the distinction that cost this seat a cut.
	if scan.Stamps == 0 || scan.Members == 0 {
		t.Fatalf("VACUOUS: read %d .cs files under src/core and found %d file(s) carrying [GoValueClone] "+
			"(%d of them [module: GoManualConversion] hand-owns), %d stamp(s), %d member name(s); "+
			"the guard cannot fail in this state. Check the [GoValueClone] spelling and the partial-type "+
			"declaration pattern before concluding the corpus stopped stamping.",
			scan.Files, scan.StampFiles, scan.HandOwnFiles, scan.Stamps, scan.Members)
	}

	t.Logf(".cs files %d, files carrying [GoValueClone] %d (hand-owns %d), stamps %d, member names %d",
		scan.Files, scan.StampFiles, scan.HandOwnFiles, scan.Stamps, scan.Members)

	if len(scan.Findings) > 0 {
		var b strings.Builder

		for _, f := range scan.Findings {
			b.WriteString("\n  " + f.String())
		}

		t.Fatalf("%d stamped member(s) are not declared by the type they stamp — go2cs-gen emits a "+
			"member access for each, so every one is a CS1061 at build time:%s", len(scan.Findings), b.String())
	}
}

// TestValueCloneStampScannerFiresAndAdmits is the positive control, in BOTH directions: a planted
// mismatch must be reported, and a planted CORRECT stamp must not be. An admit-only control cannot
// fail on a dead scanner, and a fires-only control cannot fail on one that refuses everything.
//
// The mismatch planted is the REAL historical one — a Δ-prefixed stamp over an unprefixed field —
// rather than an invented shape, so the control also pins the glyph handling the finding depends on.
func TestValueCloneStampScannerFiresAndAdmits(t *testing.T) {
	dir := t.TempDir()

	write := func(name, body string) {
		t.Helper()

		if err := os.MkdirAll(filepath.Dir(filepath.Join(dir, name)), 0o750); err != nil {
			t.Fatalf("planting %s: %v", name, err)
		}

		if err := os.WriteFile(filepath.Join(dir, name), []byte(body), 0o600); err != nil {
			t.Fatalf("planting %s: %v", name, err)
		}
	}

	// BAD: the stamp names Δtrace, the field is trace.
	write("bad.cs", "[module: go.GoManualConversion]\n"+
		"namespace go;\n"+
		"partial class runtime_package {\n"+
		"[GoType] [GoValueClone(\"tls\", \"Δtrace\")] partial struct m {\n"+
		"    internal ж<tls> tls;\n"+
		"    internal mTraceState trace;\n"+
		"}\n"+
		"}\n")

	// GOOD: same shape, spelled as the declaration is.
	write("good.cs", "[module: go.GoManualConversion]\n"+
		"namespace go;\n"+
		"partial class runtime_package {\n"+
		"[GoType] [GoValueClone(\"tls\", \"trace\")] partial struct m2 {\n"+
		"    internal ж<tls> tls;\n"+
		"    internal mTraceState trace;\n"+
		"}\n"+
		"}\n")

	// CONVERTED, and now IN the population: an unmarked file is read exactly like a hand-own. This
	// plant is the one the old scanner skipped by design, and it must now be reported.
	write("converted.cs", "namespace go;\n"+
		"partial class runtime_package {\n"+
		"[GoType] [GoValueClone(\"Δtrace\")] partial struct m3 {\n"+
		"    internal mTraceState trace;\n"+
		"}\n"+
		"}\n")

	// CROSS-PARTIAL: the stamp sits on an empty forward partial, the members are declared in
	// another file in the same directory. This is how EVERY converted stamp in the corpus is
	// shaped, and resolving at the stamp reports both names as missing.
	write("package_info.cs", "namespace go;\n"+
		"partial class tls_package {\n"+
		"    [GoValueClone(\"seq\", \"@in\", \"@out\")] internal partial struct halfConn {}\n"+
		"}\n")
	write("conn.cs", "namespace go;\n"+
		"partial class tls_package {\n"+
		"    internal partial struct halfConn {\n"+
		"        internal array<byte> seq;\n"+
		"        internal halfConn @in, @out;\n"+
		"    }\n"+
		"}\n")

	// MINTED: a bodiless [GoType(\"target\")] wrapper declares nothing; go2cs-gen mints Value.
	write("writer.cs", "namespace go;\n"+
		"partial class png_package {\n"+
		"    [GoType(\"encoder\")] partial struct EncoderBuffer;\n"+
		"    [GoValueClone(\"Value\")] public partial struct EncoderBuffer {}\n"+
		"}\n")

	// PER-GOOS: the stamp is in a child directory, the type is declared in the parent — and the
	// child declares its OWN partial of the same type too, which is what makes a fallback useless
	// and forces the union.
	write("windows/package_info.cs", "namespace go;\n"+
		"partial class tar_package {\n"+
		"    [GoValueClone(\"hdr\")] internal partial struct reader {}\n"+
		"    internal partial struct reader {\n"+
		"        internal nint pad;\n"+
		"    }\n"+
		"}\n")
	write("reader.cs", "namespace go;\n"+
		"partial class tar_package {\n"+
		"    internal partial struct reader {\n"+
		"        internal Header hdr;\n"+
		"    }\n"+
		"}\n")

	scan, err := scanValueCloneStamps(dir)

	if err != nil {
		t.Fatalf("scanning the planted tree: %v", err)
	}

	if scan.StampFiles != 6 {
		t.Fatalf("expected 6 stamp-bearing files (converted ones INCLUDED), got %d", scan.StampFiles)
	}

	if scan.HandOwnFiles != 2 {
		t.Fatalf("expected 2 of them to be [module: GoManualConversion] hand-owns, got %d", scan.HandOwnFiles)
	}

	if scan.Stamps != 6 || scan.Members != 10 {
		t.Fatalf("expected 6 stamps / 10 member names across the plants, got %d / %d", scan.Stamps, scan.Members)
	}

	if len(scan.Findings) != 2 {
		t.Fatalf("expected exactly 2 findings (bad.cs and converted.cs), got %d: %v", len(scan.Findings), scan.Findings)
	}

	wantFindings := []stampFinding{
		{File: "bad.cs", Type: "m", Member: "Δtrace"},
		{File: "converted.cs", Type: "m3", Member: "Δtrace"},
	}

	for i, want := range wantFindings {
		if scan.Findings[i] != want {
			t.Fatalf("finding %d does not name the planted mismatch: got %+v, want %+v", i, scan.Findings[i], want)
		}
	}
}

// TestValueCloneScannerAdmitsTheFourCorpusShapes is the ANTI-OVER-MATCH control, and it is the arm
// the corpus reading rests on. Each shape below is one that a naive scanner reports as a defect
// while the build is perfectly happy, and each was found by measuring false positives DOWN against
// the real corpus (858 → 176 → 170 → 2 → 0), not by reasoning about them. Without this arm a later
// simplification of the resolver would put all four back and the corpus arm would name hundreds of
// files, which reads as corpus rot rather than as a scanner regression.
func TestValueCloneScannerAdmitsTheFourCorpusShapes(t *testing.T) {
	for _, shape := range []struct {
		name  string
		files map[string]string
	}{
		{
			name: "cross-partial: the stamp sits on an empty forward partial",
			files: map[string]string{
				"package_info.cs": "namespace go;\npartial class p { [GoValueClone(\"seq\")] internal partial struct halfConn {} }\n",
				"conn.cs":         "namespace go;\npartial class p { internal partial struct halfConn { internal array<byte> seq; } }\n",
			},
		},
		{
			name: "multi-declarator: two names on one field line",
			files: map[string]string{
				"package_info.cs": "namespace go;\npartial class p { [GoValueClone(\"@in\", \"@out\")] internal partial struct Conn {} }\n",
				"conn.cs":         "namespace go;\npartial class p { internal partial struct Conn {\n    internal halfConn @in, @out;\n} }\n",
			},
		},
		{
			name: "per-GOOS: the type is declared in the parent directory",
			files: map[string]string{
				"windows/package_info.cs": "namespace go;\npartial class p { [GoValueClone(\"hdr\")] internal partial struct reader {}\n    internal partial struct reader { internal nint pad; } }\n",
				"reader.cs":               "namespace go;\npartial class p { internal partial struct reader { internal Header hdr; } }\n",
			},
		},
		{
			name: "minted: a bodiless [GoType(\"target\")] wrapper carries Value",
			files: map[string]string{
				"writer.cs": "namespace go;\npartial class p {\n    [GoType(\"encoder\")] partial struct EncoderBuffer;\n    [GoValueClone(\"Value\")] public partial struct EncoderBuffer {}\n}\n",
			},
		},
	} {
		t.Run(shape.name, func(t *testing.T) {
			dir := t.TempDir()

			for name, body := range shape.files {
				full := filepath.Join(dir, name)

				if err := os.MkdirAll(filepath.Dir(full), 0o750); err != nil {
					t.Fatalf("planting %s: %v", name, err)
				}

				if err := os.WriteFile(full, []byte(body), 0o600); err != nil {
					t.Fatalf("planting %s: %v", name, err)
				}
			}

			scan, err := scanValueCloneStamps(dir)

			if err != nil {
				t.Fatalf("scanning the planted tree: %v", err)
			}

			if scan.Stamps == 0 || scan.Members == 0 {
				t.Fatalf("VACUOUS: the plant produced %d stamps / %d member names, so ADMITTING it proves nothing",
					scan.Stamps, scan.Members)
			}

			if len(scan.Findings) != 0 {
				t.Fatalf("this shape compiles and must be ADMITTED, got %d finding(s): %v", len(scan.Findings), scan.Findings)
			}
		})
	}
}

// TestValueCloneVacuityArmCanFire controls the VACUITY predicate itself. The corpus arm's
// non-emptiness assertion has already fired once for real — the 1.24 hop retired the one hand-own
// the old population rested on — but "it fired once in the field" is not a control, and a predicate
// that can only be proven by the event it is meant to catch is proven by nothing. A tree with files
// but no stamps must read Files > 0 and Stamps == Members == 0, which is precisely the state the
// corpus arm refuses.
func TestValueCloneVacuityArmCanFire(t *testing.T) {
	dir := t.TempDir()

	if err := os.WriteFile(filepath.Join(dir, "plain.cs"), []byte(
		"namespace go;\npartial class p { internal partial struct halfConn { internal array<byte> seq; } }\n"), 0o600); err != nil {
		t.Fatalf("planting plain.cs: %v", err)
	}

	scan, err := scanValueCloneStamps(dir)

	if err != nil {
		t.Fatalf("scanning the stamp-free tree: %v", err)
	}

	if scan.Files == 0 {
		t.Fatalf("VACUOUS CONTROL: the scanner read 0 files, so a zero stamp count says nothing about stamps")
	}

	if scan.Stamps != 0 || scan.Members != 0 {
		t.Fatalf("a stamp-free tree must read 0 stamps / 0 member names, got %d / %d", scan.Stamps, scan.Members)
	}

	if scan.StampFiles != 0 || scan.HandOwnFiles != 0 {
		t.Fatalf("a stamp-free tree must read 0 stamp-bearing files and 0 hand-owns, got %d / %d", scan.StampFiles, scan.HandOwnFiles)
	}
}
