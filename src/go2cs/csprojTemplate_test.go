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

func TestCsprojTemplateEmitsWellFormedXml(t *testing.T) {
	// Same substitution as writeProjectFile (main.go, `fmt.Sprintf(string(csprojTemplate), …)`).
	contents := fmt.Sprintf(string(csprojTemplate),
		"Exe",
		"go",
		"TestProject",
		time.Now().Year(),
		"false",
		`    <ProjectReference Include="$(go2csPath)core\fmt\fmt.csproj" />`,
	)

	if err := assertWellFormedXml(contents); err != nil {
		t.Fatalf("csproj-template.xml does not emit well-formed XML: %v", err)
	}
}

// The friend-assembly grant is inserted AFTER template rendering — never as a template verb, so a
// user-supplied `-csproj` template (which cannot know about the slot) keeps rendering correctly —
// and this guard validates the INSERTED document, the shape that actually reaches disk for every
// package with build-selected in-package tests.
func TestCsprojTemplateWithFriendAssemblyAccessEmitsWellFormedXml(t *testing.T) {
	contents := fmt.Sprintf(string(csprojTemplate),
		"Exe",
		"go",
		"TestProject",
		time.Now().Year(),
		"false",
		`    <ProjectReference Include="$(go2csPath)core\fmt\fmt.csproj" />`,
	)

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
