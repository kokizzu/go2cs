// csprojTemplate_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"encoding/xml"
	"fmt"
	"io"
	"strings"
	"testing"
	"time"
)

// The embedded csproj templates are emitted verbatim into every converted project, so a malformed
// one breaks the whole corpus at once and only surfaces at COMPILE time — the behavioral suite
// caught an `--` inside an XML comment (illegal per the XML spec, MSB4025 "An XML comment cannot
// contain '--'") 457 s into a full run, in all 495 projects simultaneously. These guards are the
// same check in ~10 ms, before the exe is even used.
//
// The templates are not valid XML on their own: csproj-template.xml is a printf format string and
// test-csproj-template.xml carries `>>MARKER:…<<` placeholders. Both are substituted here exactly
// as the emitters do, so what is validated is what actually reaches disk.

// renderCsprojTemplate applies the same two-stage substitution writeProjectFile does: the printf
// verbs first, then the post-render markers. validationPack is the stdlib-only VALIDATION.md pack
// block, which collapses to "" for every other conversion — both forms reach disk, so both are
// validated here.
func renderCsprojTemplate(outputType string, reference string, validationPack string) string {
	contents := fmt.Sprintf(string(csprojTemplate),
		outputType,
		"go",
		"TestProject",
		time.Now().Year(),
		"false",
		reference,
	)

	return strings.ReplaceAll(contents, ValidationPackMarker, validationPack)
}

func TestCsprojTemplateEmitsWellFormedXml(t *testing.T) {
	contents := renderCsprojTemplate("Exe", `    <ProjectReference Include="$(go2csPath)core\fmt\fmt.csproj" />`, "")

	if strings.Contains(contents, ">>MARKER:") {
		t.Fatalf("csproj-template.xml has an unsubstituted marker; update renderCsprojTemplate")
	}

	if err := assertWellFormedXml(contents); err != nil {
		t.Fatalf("csproj-template.xml does not emit well-formed XML: %v", err)
	}
}

// The VALIDATION.md pack block is built in Go and injected into every converted stdlib .csproj, so a
// malformed one breaks the whole published corpus at pack time. This is that block, in place.
func TestCsprojTemplateWithValidationPackEmitsWellFormedXml(t *testing.T) {
	block := validationPackBlock(`H:\Projects\go2cs\src\core\path\filepath\path.filepath.csproj`, Options{convertStdLib: true})

	if !strings.Contains(block, `path.filepath.md`) {
		t.Fatalf("the validation pack block does not name the package's proof sheet: %s", block)
	}

	contents := renderCsprojTemplate("Library", `    <ProjectReference Include="$(go2csPath)core\fmt\fmt.csproj" />`, block)

	if !strings.Contains(contents, `PackagePath="VALIDATION.md"`) {
		t.Fatal("the validation pack block was not substituted into the template")
	}

	if err := assertWellFormedXml(contents); err != nil {
		t.Fatalf("csproj template with the validation pack block does not emit well-formed XML: %v", err)
	}
}

// A non-stdlib conversion must emit the .csproj it always did: the marker's whole line collapses to
// the blank line the template has always had between the README and source-generator sections.
// Behavioral-test and -recurse output is byte-compared against goldens, so "no block" is not enough
// — the surrounding bytes have to be unchanged too.
func TestValidationPackMarkerCollapsesToBlankLine(t *testing.T) {
	if block := validationPackBlock(`C:\out\src\Tests\Behavioral\Arrays\Arrays.csproj`, Options{}); block != "" {
		t.Fatalf("a non-stdlib conversion emitted a validation pack block: %q", block)
	}

	contents := renderCsprojTemplate("Library", "", "")

	if !strings.Contains(contents, "</ItemGroup>\r\n\r\n  <!-- Expose output of source generators as local files -->") {
		t.Fatal("collapsing the validation pack marker did not leave the template's original blank line")
	}
}

// The friend-assembly grant is inserted AFTER template rendering — never as a template verb, so a
// user-supplied `-csproj` template (which cannot know about the slot) keeps rendering correctly —
// and this guard validates the INSERTED document, the shape that actually reaches disk for every
// package with build-selected in-package tests.
func TestCsprojTemplateWithFriendAssemblyAccessEmitsWellFormedXml(t *testing.T) {
	contents := renderCsprojTemplate("Exe", `    <ProjectReference Include="$(go2csPath)core\fmt\fmt.csproj" />`, "")

	contents = insertFriendAssemblyAccess(contents)

	if !strings.Contains(contents, `<InternalsVisibleTo Include="$(AssemblyName).tests" />`) {
		t.Fatal("the friend-assembly ItemGroup was not inserted")
	}

	if err := assertWellFormedXml(contents); err != nil {
		t.Fatalf("csproj template with friend-assembly access does not emit well-formed XML: %v", err)
	}
}

func TestTestCsprojTemplateEmitsWellFormedXml(t *testing.T) {
	// Same substitution as writeTestProject (testConversion.go), which replaces each marker.
	contents := string(testCsprojTemplate)

	for marker, value := range map[string]string{
		TestRootNamespaceMarker:     "go",
		TestAssemblyNameMarker:      "TestProject.tests",
		TestGo2CSRelativePathMarker: `..\..\`,
		TestCompileItemsMarker:      "\r\n    <Compile Include=\"value.cs\" />",
		TestFixtureItemsMarker:      "",
		TestProjectReferencesMarker: "\r\n    <ProjectReference Include=\"$(go2csPath)core\\testing\\testing.csproj\" />",
	} {
		contents = strings.ReplaceAll(contents, marker, value)
	}

	if strings.Contains(contents, ">>MARKER:") {
		t.Fatalf("test-csproj-template.xml has an unsubstituted marker; update this guard's marker map")
	}

	if err := assertWellFormedXml(contents); err != nil {
		t.Fatalf("test-csproj-template.xml does not emit well-formed XML: %v", err)
	}
}

// A reference path is the one part of a rendered csproj built from user-controlled text rather than
// from the template or from a Go import path. Under `-recurse` it starts as an absolute path beneath
// the user's output root and is only made relative when filepath.Rel succeeds, which it cannot do
// across Windows volumes — so an output root like `D:\R&D\out` reaches the emitter with its `&`
// intact. writeProjectFile escapes it; this guard is that escape, with its own negative control, so
// a future edit cannot quietly drop the call and still pass.
func TestProjectReferenceWithXmlSpecialsStaysWellFormed(t *testing.T) {
	reference := `..\..\pkg\R&D\"lib"\<lib>.csproj`

	escaped := renderCsprojTemplate("Library", fmt.Sprintf(`    <ProjectReference Include="%s" />`, escapeXMLAttributeValue(reference)), "")

	if err := assertWellFormedXml(escaped); err != nil {
		t.Fatalf("an escaped reference path does not emit well-formed XML: %v", err)
	}

	// Negative control: the same path unescaped must NOT parse. Without this the test would pass
	// even if escapeXMLAttributeValue became the identity function.
	unescaped := renderCsprojTemplate("Library", fmt.Sprintf(`    <ProjectReference Include="%s" />`, reference), "")

	if err := assertWellFormedXml(unescaped); err == nil {
		t.Fatal("an unescaped reference path parsed as well-formed XML, so this guard proves nothing")
	}
}

// assertWellFormedXml streams the document through encoding/xml, which enforces the comment rules
// MSBuild's loader enforces (no `--` in a comment body, no trailing `-`).
func assertWellFormedXml(contents string) error {
	decoder := xml.NewDecoder(strings.NewReader(contents))

	for {
		_, err := decoder.Token()

		if err == io.EOF {
			return nil
		}

		if err != nil {
			return err
		}
	}
}
