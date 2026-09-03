// cgoDynamicImports_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/ast"
	"go/parser"
	"go/token"
	"os"
	"sort"
	"strings"
	"testing"
)

// The class-B emission's guards. `collectCgoDynamicImports` decides which
// `//go:cgo_import_dynamic` pragmas become `GoCgoImportDynamic` records, and every property it has
// was chosen by measurement over Go 1.23.12 rather than by preference — so each test below pins one
// of those measurements, and names the emission it would break.
//
// They assert the DECISION — the `cgoDynamicImports` map the pass writes — rather than the emitted
// text, except where the test is specifically about the text. A guard that greps one emitted file
// for an assembly-level property goes silently vacuous the moment that property relocates
// (FALSE-GREEN route #8), and this section's whole reason to exist is that its records are read
// from assembly METADATA, not from any particular file.

// parseCgoTestFiles parses source snippets into the []*ast.File shape the pass consumes. Comments
// are parsed explicitly: the pass reads pragmas out of file.Comments, so a parse that dropped them
// would make every assertion here vacuously true in the "mints nothing" direction.
func parseCgoTestFiles(t *testing.T, sources ...string) []*ast.File {
	t.Helper()

	fset := token.NewFileSet()
	files := make([]*ast.File, 0, len(sources))

	for i, source := range sources {
		file, err := parser.ParseFile(fset, "cgo_test_source.go", source, parser.ParseComments)

		if err != nil {
			t.Fatalf("source %d failed to parse: %s", i, err)
		}

		files = append(files, file)
	}

	return files
}

// collectCgoTestRecords runs the pass over sources with the package-scoped map isolated, and returns
// what it bound. Restoring the global both ways matters for the same reason the linkname guards
// restore theirs: a record left behind by another test would be measured as this one's result.
func collectCgoTestRecords(t *testing.T, sources ...string) map[string]cgoDynamicImportRecord {
	t.Helper()

	saved := cgoDynamicImports
	cgoDynamicImports = nil

	t.Cleanup(func() { cgoDynamicImports = saved })

	collectCgoDynamicImports(parseCgoTestFiles(t, sources...))

	return cgoDynamicImports
}

func sortedRecordKeys(records map[string]cgoDynamicImportRecord) []string {
	keys := make([]string, 0, len(records))

	for key := range records {
		keys = append(keys, key)
	}

	sort.Strings(keys)

	return keys
}

// TestCgoDynamicImportsGateOnLibraryShape pins the predicate: a record is minted only when the
// pragma names an ABSOLUTE library path, which over the 1650 such pragmas in Go 1.23.12 outside
// cmd/ and vendor/ selects exactly the darwin population and nothing else.
//
// Each excluded case is a real shape from that census, not an invention: windows names
// `kernel32.dll` (51 records, reached through P/Invoke already), openbsd/solaris name `libc.so`,
// aix names `libc.a/shr_64.o`, and runtime/race's 196 darwin records name nothing at all. Both
// included cases are real too — libSystem is the `.dylib` shape and CoreFoundation the
// framework shape that carries no `.dylib` suffix, which is why the gate reads the path rather
// than the extension.
//
// Red-provable by relaxing the `^/` test in parseCgoImportDynamic: the four excluded rows appear.
func TestCgoDynamicImportsGateOnLibraryShape(t *testing.T) {
	const source = `package p

//go:cgo_import_dynamic libc_included_dylib included_dylib "/usr/lib/libSystem.B.dylib"
//go:cgo_import_dynamic libc_included_framework included_framework "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation"
//go:cgo_import_dynamic libc_excluded_windows excluded_windows "kernel32.dll"
//go:cgo_import_dynamic libc_excluded_bsd excluded_bsd "libc.so"
//go:cgo_import_dynamic libc_excluded_aix excluded_aix "libc.a/shr_64.o"
//go:cgo_import_dynamic libc_excluded_empty excluded_empty ""

func libc_included_dylib_trampoline()
func libc_included_framework_trampoline()
func libc_excluded_windows_trampoline()
func libc_excluded_bsd_trampoline()
func libc_excluded_aix_trampoline()
func libc_excluded_empty_trampoline()
`

	records := collectCgoTestRecords(t, source)

	want := []string{"libc_included_dylib_trampoline", "libc_included_framework_trampoline"}
	got := sortedRecordKeys(records)

	if strings.Join(got, ",") != strings.Join(want, ",") {
		t.Fatalf("gate selected %v, want %v — every excluded row is a real non-darwin shape from the census, and a record for one would resolve a symbol no darwin host exports", got, want)
	}

	if lib := records["libc_included_framework_trampoline"].library; !strings.HasSuffix(lib, "/CoreFoundation") {
		t.Errorf("framework record carries library %q — the 28 crypto/x509/internal/macos records have no .dylib suffix, so a suffix-keyed gate drops exactly them", lib)
	}
}

// TestCgoDynamicImportsBindingIsMechanical pins the binding rule and, more importantly, its
// BOUNDARY. `trampoline == local + "_trampoline"` holds for 297 of 297 declarations outside
// runtime and 0 of 43 inside it, where 37 bind on the SYMBOL instead and 6 have no darwin pragma at
// all. This converter deliberately mints nothing for either runtime shape rather than reaching them
// with a normalizer that would cover 334 of 340 and guess at the rest.
//
// Red-provable by adding a symbol-keyed fallback to collectCgoDynamicImports: the runtime row binds
// and this test names it.
func TestCgoDynamicImportsBindingIsMechanical(t *testing.T) {
	const source = `package p

// The syscall/macos/internal-syscall shape: local + "_trampoline" is the declaration.
//go:cgo_import_dynamic libc_getgroups getgroups "/usr/lib/libSystem.B.dylib"

// The runtime shape: the declaration is named for the SYMBOL, not the local.
//go:cgo_import_dynamic libc_pthread_attr_init pthread_attr_init "/usr/lib/libSystem.B.dylib"

// A pragma whose trampoline this package does not declare.
//go:cgo_import_dynamic libc_orphan orphan "/usr/lib/libSystem.B.dylib"

func libc_getgroups_trampoline()
func pthread_attr_init_trampoline()

// A trampoline with no pragma at all — runtime's nanotime/walltime/raiseproc class, which is Go's
// own assembly and must stay class C.
func nanotime_trampoline()

// A bodyless declaration that is not a trampoline at all.
func somethingElse()
`

	records := collectCgoTestRecords(t, source)

	if got := sortedRecordKeys(records); strings.Join(got, ",") != "libc_getgroups_trampoline" {
		t.Fatalf("bound %v, want only libc_getgroups_trampoline", got)
	}

	if _, bound := records["pthread_attr_init_trampoline"]; bound {
		t.Error("bound runtime's symbol-named trampoline — that correspondence lives in the .s file this converter does not read, so binding it here is a guess")
	}

	if _, bound := records["nanotime_trampoline"]; bound {
		t.Error("bound a trampoline with no pragma — it is Go's own assembly and must stay class C, where the resolver throws loudly rather than inventing an address")
	}

	if record := records["libc_getgroups_trampoline"]; record.symbol != "getgroups" {
		t.Errorf("record carries symbol %q, want getgroups — the symbol, not the local, is what the library exports", record.symbol)
	}
}

// TestCgoDynamicImportsIgnoresBodiedDeclarations pins that a trampoline is BODYLESS. The record's
// meaning is "this stub has no managed body, resolve it dynamically"; a function that has a body
// needs no resolution and would be given a foreign address.
//
// Red-provable by dropping the `funcDecl.Body != nil` test.
func TestCgoDynamicImportsIgnoresBodiedDeclarations(t *testing.T) {
	const source = `package p

//go:cgo_import_dynamic libc_hasbody hasbody "/usr/lib/libSystem.B.dylib"

func libc_hasbody_trampoline() { println("managed") }
`

	if records := collectCgoTestRecords(t, source); len(records) != 0 {
		t.Fatalf("bound %v for a trampoline that HAS a body — a resolved address would displace real managed code", sortedRecordKeys(records))
	}
}

// TestCgoDynamicImportsTrimsGoSStrayQuote pins the one upstream typo the census found:
// runtime/sys_darwin.go:670-671 end their library argument in a stray second quote, which Go's own
// linker tolerates. Untrimmed it would ship a path with a quote in it into a C# string literal.
//
// Red-provable by trimming only the first and last character instead of every quote.
func TestCgoDynamicImportsTrimsGoSStrayQuote(t *testing.T) {
	const source = `package p

//go:cgo_import_dynamic libc_mach_vm_region mach_vm_region "/usr/lib/libSystem.B.dylib""

func libc_mach_vm_region_trampoline()
`

	records := collectCgoTestRecords(t, source)
	record, bound := records["libc_mach_vm_region_trampoline"]

	if !bound {
		t.Fatal("the stray-quote row bound nothing — Go carries two of these and both are real libSystem imports")
	}

	if record.library != "/usr/lib/libSystem.B.dylib" {
		t.Errorf("library is %q, want /usr/lib/libSystem.B.dylib with the stray quote gone", record.library)
	}

	if line := cgoDynamicImportLine(record); strings.Count(line, "\"") != 6 {
		t.Errorf("emitted line %q does not carry exactly three unescaped string literals — a quote survived into the emission", line)
	}
}

// TestCgoDynamicImportsSectionAppearsOnlyWithRecords pins the footprint invariant, and it is the
// one property here that is about a FILE rather than the map: a package with no records must come
// out byte-identical to what it went in as.
//
// This is what makes the windows and linux emissions unchanged BY CONSTRUCTION rather than by a
// two-seeded diff that happens to come back empty — 4 packages of the corpus carry any dynamic
// imports, and only on darwin, so an unconditionally-emitted section would put marker lines into
// every package_info.cs of every platform flavor.
//
// Red-provable by removing the `len(cgoDynamicImports) == 0` early return in applyCgoDynamicImports.
func TestCgoDynamicImportsSectionAppearsOnlyWithRecords(t *testing.T) {
	before := []string{
		"// <GoSourcePositionMaps>",
		"// </GoSourcePositionMaps>",
		"",
		"namespace go;",
		"",
		"public static partial class p_package {",
		"}",
	}

	saved := cgoDynamicImports
	t.Cleanup(func() { cgoDynamicImports = saved })

	cgoDynamicImports = nil

	after := applyCgoDynamicImports(append([]string(nil), before...), "package_info.cs", false)

	if strings.Join(after, "\n") != strings.Join(before, "\n") {
		t.Fatalf("a package with no records was rewritten:\n%s", strings.Join(after, "\n"))
	}

	// POSITIVE CONTROL: the same call with one record must produce the section, or the assertion
	// above is satisfied by a function that never emits anything at all.
	cgoDynamicImports = map[string]cgoDynamicImportRecord{
		"libc_getgroups_trampoline": {
			trampoline: "libc_getgroups_trampoline",
			symbol:     "getgroups",
			library:    "/usr/lib/libSystem.B.dylib",
		},
	}

	populated := strings.Join(applyCgoDynamicImports(append([]string(nil), before...), "package_info.cs", false), "\n")

	if !strings.Contains(populated, "<"+CgoDynamicImportsSection+">") {
		t.Fatal("control failed: a package WITH a record emitted no section, so the no-record assertion above proves nothing")
	}

	if !strings.Contains(populated, `GoCgoImportDynamic("libc_getgroups_trampoline", "getgroups", "/usr/lib/libSystem.B.dylib")`) {
		t.Errorf("control emitted a section without its record:\n%s", populated)
	}

	// The section is created above the namespace declaration, the same anchor the position-map
	// section uses, so a migrated file and a fresh one agree.
	if strings.Index(populated, "<"+CgoDynamicImportsSection+">") > strings.Index(populated, "namespace go;") {
		t.Error("section was created BELOW the namespace declaration, where an assembly attribute is a compile error")
	}
}

// TestBothDriversCollectCgoDynamicImports pins that the pre-pass runs in BOTH conversion drivers.
//
// This is the guard for a defect the cut actually had. `applyCgoDynamicImports` rewrites an
// existing section from what THIS run bound, so a driver that emits package info without ever
// binding does not merely skip the records — it EMPTIES a section the other driver populated. The
// `-tests` driver mirrors processConversion's package-wide analysis sequence by hand rather than
// sharing it, so "the sequence is mirrored" is a convention, and a convention that is only written
// down is one a later pass can be added outside of.
//
// It reads the SOURCE rather than converting, for the same reason ConverterBuildInputs' guards do:
// the property is "both call sites exist", and the conversion that would demonstrate it needs a
// darwin package with cgo pragmas, which no unit test should have to stage.
//
// Red-provable by deleting either call.
func TestBothDriversCollectCgoDynamicImports(t *testing.T) {
	for _, driver := range []string{"conversionDriver.go", "testConversion.go"} {
		source, err := os.ReadFile(driver)

		if err != nil {
			t.Fatalf("%s: %s", driver, err)
		}

		if !strings.Contains(string(source), "collectCgoDynamicImports") {
			t.Errorf("%s never binds cgo dynamic imports — a package_info write from this driver would EMPTY a section the other driver populated, which is worse than emitting nothing", driver)
		}
	}
}
