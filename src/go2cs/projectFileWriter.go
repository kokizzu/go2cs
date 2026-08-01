// projectFileWriter.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns everything the converter writes that is NOT C# source: the .csproj, the icons
// and publish profiles beside it, and the mechanics of getting an output file onto disk at all.
//
// The .csproj is what makes converted code buildable — it carries the golib runtime reference, the
// go2cs-gen analyzer reference, and one ProjectReference per imported Go package — so this file is
// effectively the converter's build-system emitter.
//
// needToWriteFile is the reason a reconvert of an unchanged package leaves the tree clean: a file
// whose bytes would not change is not rewritten, so timestamps (and any up-to-date check built on
// them) stay meaningful.

package main

import (
	"bytes"
	"fmt"
	"go/types"
	"os"
	"os/exec"
	"path"
	"path/filepath"
	"sort"
	"strconv"
	"strings"
	"time"
)

func getGoEnv(name string) (string, error) {
	cmd := exec.Command("go", "env", name)
	var out bytes.Buffer

	cmd.Stdout = &out
	err := cmd.Run()

	if err != nil {
		return "", fmt.Errorf("failed to get Go environment %s: %w", name, err)
	}

	return strings.TrimSpace(out.String()), nil
}

// prepareProjectFiles writes the project related files for the given project name and path,
// and returns project file contents with template parameters to be written to a file later.
func prepareProjectFiles(projectName string, packageNamespace string, projectPath string) (string, string, error) {
	// Make sure project path ends with a directory separator
	projectPath = strings.TrimRight(projectPath, string(filepath.Separator)) + string(filepath.Separator)

	// Ensure project directory exists
	if err := os.MkdirAll(projectPath, 0755); err != nil {
		return "", "", fmt.Errorf("failed to create project directory \"%s\": %s", projectPath, err)
	}

	iconFileName := projectPath + "go2cs.ico"

	// Check if icon file needs to be written
	if needToWriteFile(iconFileName, iconFileBytes) {
		iconFile, err := os.Create(iconFileName)

		if err != nil {
			return "", "", fmt.Errorf("failed to create icon file \"%s\": %s", iconFileName, err)
		}

		defer iconFile.Close()

		_, err = iconFile.Write(iconFileBytes)

		if err != nil {
			return "", "", fmt.Errorf("failed to write to icon file \"%s\": %s", iconFileName, err)
		}
	}

	// Generate project file contents
	projectFileContents := fmt.Sprintf(string(csprojTemplate),
		OutputTypeMarker,
		packageNamespace,
		projectName,
		time.Now().Year(),
		UnsafeMarker,
		ProjectReferenceMarker,
	)

	if hasSiblingInternalTestFiles {
		projectFileContents = insertFriendAssemblyAccess(projectFileContents)
	}

	projectFileName := projectPath + projectName + ".csproj"

	return projectFileName, projectFileContents, nil
}

// insertFriendAssemblyAccess grants the package's colocated test assembly access to its internal
// members. Inserted AFTER template rendering — never as a template verb — so the template keeps its
// historical verb count and a user-supplied `-csproj` template (which cannot know about the slot)
// renders exactly as before; Sprintf would otherwise append a `%!s(EXTRA …)` diagnostic into every
// generated project. Anchored on the first closing PropertyGroup, which every usable template has.
func insertFriendAssemblyAccess(projectFileContents string) string {
	const anchor = "</PropertyGroup>"
	const friendItemGroup = "\r\n\r\n  <!-- Same-package Go tests run in a separate assembly but retain package-private access. -->\r\n  <ItemGroup>\r\n    <InternalsVisibleTo Include=\"$(AssemblyName).tests\" />\r\n  </ItemGroup>"

	idx := strings.Index(projectFileContents, anchor)

	if idx < 0 {
		return projectFileContents
	}

	insertAt := idx + len(anchor)
	return projectFileContents[:insertAt] + friendItemGroup + projectFileContents[insertAt:]
}

func writeProjectFile(projectFileName string, projectFileContents string, outputFilePath string, pkg *types.Package, options Options) error {
	// Get assembly output type from the package details
	outputType := getAssemblyOutputType(pkg)

	// Replace the output type marker with the actual output type
	newContents := []byte(strings.ReplaceAll(string(projectFileContents), OutputTypeMarker, outputType))

	// Replace the unsafe code marker with the actual unsafe code setting
	newContents = []byte(strings.ReplaceAll(string(newContents), UnsafeMarker, strconv.FormatBool(usesUnsafeCode)))

	// Go's `go build` names an executable after the LAST element of the main package's import path
	// (`example.com/colordemo` → `colordemo`), not the full dotted module path. Mirror that for an
	// app's AssemblyName so the emitted exe matches `go build`'s name. LIBRARY assemblies keep the full
	// dotted name — their DLL/NuGet PackageId identity ($(AssemblyName)) must stay unique across the
	// package graph (e.g. github.com.fatih.color). Only the AssemblyName changes; the .csproj filename
	// (the project's identity in the solution/references) is left on the full path.
	if outputType == "Exe" {
		fullName := strings.TrimSuffix(filepath.Base(projectFileName), ".csproj")

		if idx := strings.LastIndex(fullName, "."); idx >= 0 {
			lastSegment := fullName[idx+1:]
			newContents = []byte(strings.ReplaceAll(string(newContents),
				"<AssemblyName>"+fullName+"</AssemblyName>",
				"<AssemblyName>"+lastSegment+"</AssemblyName>"))
		}
	}

	// -recurse=nuget: reference the published go2cs NuGet packages (go.<pkg> stdlib, go.lib runtime,
	// go.gen analyzer) instead of local $(go2csPath) project references. Gated OFF the stdlib
	// self-conversion — its packages must reference each other locally to build the very assemblies that
	// get published — mirroring the README-emission gate below (options.convertStdLib).
	emitNuGet := options.nugetRefs && !options.convertStdLib

	// The golib runtime and the go2cs-gen analyzer are FIXED ProjectReferences hardcoded in
	// csproj-template.xml (NOT part of the ProjectReferenceMarker block), so swap them here. Match strings
	// must stay in sync with csproj-template.xml (~line 78 analyzer, ~line 118 golib); TestRecurseNuGetReferences
	// guards against drift. The analyzer keeps PrivateAssets="all" (go.gen is a DevelopmentDependency
	// analyzer package, delivered under analyzers/dotnet/cs — analyzer-only, no compile/runtime asset).
	if emitNuGet {
		newContents = []byte(strings.ReplaceAll(string(newContents),
			`<ProjectReference Include="$(go2csPath)core\golib\golib.csproj" />`,
			`<PackageReference Include="go.lib" Version="$(GoStdLibVersion)" />`))
		newContents = []byte(strings.ReplaceAll(string(newContents),
			`<ProjectReference Include="$(go2csPath)gen\go2cs-gen\go2cs-gen.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="All" />`,
			`<PackageReference Include="go.gen" Version="$(GoStdLibVersion)" PrivateAssets="all" />`))
	}

	// Extract project references from imports
	packageInfoMap := getImportPackageInfo(projectImports.Keys(), options)
	projectReferences := &strings.Builder{}

	// Ensure project references are sorted so that the project file output is deterministic
	references := make([]string, 0, len(packageInfoMap))

	// References to converted stdlib packages are emitted as `$(go2csPath)core\...`; a LOCAL/USER
	// module reference (getLocalModulePackageInfo) is an ABSOLUTE path, which is rewritten here
	// relative to THIS project's directory so the generated .csproj is portable. projectDir is made
	// absolute first — projectFileName can be relative (a relative output path), which would make
	// filepath.Rel fail against the absolute reference and leave a machine-specific path behind.
	projectDir := filepath.Dir(projectFileName)

	if absDir, absErr := filepath.Abs(projectDir); absErr == nil {
		projectDir = absDir
	}

	// Under -recurse=nuget, stdlib imports become go.<name> NuGet PackageReferences; the app's own
	// converted packages (main-module + third-party, IsStdLib=false) stay local ProjectReferences.
	var packageIds []string

	for _, info := range packageInfoMap {
		reference := info.ProjectReference

		if len(reference) == 0 {
			continue
		}

		// Load imported type aliases for the current package, if not already loaded — needed regardless of
		// whether this import is emitted as a ProjectReference or a NuGet PackageReference.
		loadImportedTypeAliases(info)

		if emitNuGet && info.IsStdLib {
			// PackageId is `go.` + the referenced project's AssemblyName. That AssemblyName is the .csproj
			// base name — the dotted import path, uniform across every resolver (e.g.
			// net\http\net.http.csproj → go.net.http). Derive it from the csproj base name, NOT
			// info.PackageName (a class-qualification name that differs for non-stdlib packages).
			packageIds = append(packageIds, "go."+strings.TrimSuffix(filepath.Base(reference), ".csproj"))
			continue
		}

		if filepath.IsAbs(reference) {
			if rel, relErr := filepath.Rel(projectDir, reference); relErr == nil {
				reference = rel
			}
		}

		// Track project references
		references = append(references, reference)
	}

	sort.Strings(references)
	sort.Strings(packageIds)

	// Build reference XML — NuGet PackageReferences first (stdlib/runtime/analyzer under -recurse=nuget),
	// then local ProjectReferences; both share the one ItemGroup ProjectReferenceMarker. When not in NuGet
	// mode packageIds is empty and this is byte-identical to the prior ProjectReference-only output.
	for _, packageID := range packageIds {
		projectReferences.WriteString(fmt.Sprintf("\r\n    <PackageReference Include=\"%s\" Version=\"$(GoStdLibVersion)\" />", packageID))
	}

	for _, reference := range references {
		projectReferences.WriteString(fmt.Sprintf("\r\n    <ProjectReference Include=\"%s\" />", reference))
	}

	// Replace the project reference marker with the actual project references
	newContents = []byte(strings.ReplaceAll(string(newContents), ProjectReferenceMarker, projectReferences.String()))

	// Check if project file needs to be written
	if needToWriteFile(projectFileName, newContents) {
		// Write project file atomically
		err := os.WriteFile(projectFileName, newContents, 0644)

		if err != nil {
			return fmt.Errorf("failed to write project file: %s", err)
		}
	}

	// For executable projects, write OS-specific publish profiles
	if outputType == "Exe" {
		err := writePublishProfiles(outputFilePath)

		if err != nil {
			return fmt.Errorf("failed to write publish profiles for project \"%s\": %s", outputFilePath, err)
		}
	}

	// For library projects, write package files, like icon
	if outputType == "Library" {
		err := writePackageFiles(outputFilePath)

		if err != nil {
			return fmt.Errorf("failed to write package files for project \"%s\": %s", outputFilePath, err)
		}

		// Emit the per-package NuGet README from the package's Go doc. Gated to stdlib conversions:
		// the README is a NuGet-packaging artifact for the published stdlib, and emitting it for
		// behavioral-test / example / single-project conversions would only litter their dirs.
		if options.convertStdLib {
			projectName := strings.TrimSuffix(filepath.Base(projectFileName), ".csproj")

			if err := writeReadmeFile(outputFilePath, projectName, packageDoc); err != nil {
				return fmt.Errorf("failed to write README file for project \"%s\": %s", outputFilePath, err)
			}
		}
	}

	return nil
}

func writePackageFiles(projectPath string) error {
	// Make sure project path ends with a directory separator
	projectPath = strings.TrimRight(projectPath, string(filepath.Separator)) + string(filepath.Separator)

	pngFileName := projectPath + "go2cs.png"

	// Check if icon file needs to be written
	if needToWriteFile(pngFileName, pngFileBytes) {
		iconFile, err := os.Create(pngFileName)

		if err != nil {
			return fmt.Errorf("failed to create package icon file \"%s\": %s", pngFileName, err)
		}

		defer iconFile.Close()

		_, err = iconFile.Write(pngFileBytes)

		if err != nil {
			return fmt.Errorf("failed to write to package icon file \"%s\": %s", pngFileName, err)
		}
	}

	return nil
}

func writePublishProfiles(projectPath string) error {
	// Make sure "Properties/PublishProfiles" directory exists
	publishProfilesDir := filepath.Join(projectPath, "Properties", "PublishProfiles")

	if err := os.MkdirAll(publishProfilesDir, 0755); err != nil {
		return fmt.Errorf("failed to create directory \"%s\": %s", publishProfilesDir, err)
	}

	// Get list of publish profiles
	profiles, err := publishProfiles.ReadDir("profiles")

	if err != nil {
		return fmt.Errorf("failed to read publish profiles: %s", err)
	}

	// Write each publish profile file
	for _, profile := range profiles {
		profileBytes, err := publishProfiles.ReadFile(path.Join("profiles", profile.Name()))

		if err != nil {
			return fmt.Errorf("failed to read publish profile \"%s\": %s", profile.Name(), err)
		}

		profileFileName := filepath.Join(publishProfilesDir, profile.Name())

		// Check if profile file already exists - user may change default parameters, so we don't overwrite
		if _, err := os.Stat(profileFileName); err == nil {
			continue
		}

		profileFile, err := os.Create(profileFileName)

		if err != nil {
			return fmt.Errorf("failed to create publish profile \"%s\": %s", profileFileName, err)
		}

		defer profileFile.Close()

		_, err = profileFile.Write(profileBytes)

		if err != nil {
			return fmt.Errorf("failed to write to publish profile \"%s\": %s", profileFileName, err)
		}
	}

	return nil
}

func needToWriteFile(fileName string, fileBytes []byte) bool {
	existingFileBytes, err := os.ReadFile(fileName)

	if err != nil {
		return true
	}

	return !bytes.Equal(existingFileBytes, fileBytes)
}

func (v *Visitor) writeOutputFile(outputFileName string) error {
	outputFile, err := os.Create(outputFileName)

	if err != nil {
		return fmt.Errorf("failed to create output source file \"%s\": %s", outputFileName, err)
	}

	defer outputFile.Close()

	_, err = outputFile.WriteString(v.outputBuilder.String())

	if err != nil {
		return fmt.Errorf("failed to write to output source file \"%s\": %s", outputFileName, err)
	}

	return nil
}

func getAssemblyOutputType(pkg *types.Package) string {
	if hasMainFunction(pkg) {
		return "Exe"
	}

	return "Library"
}

func hasMainFunction(pkg *types.Package) bool {
	if pkg == nil {
		return false
	}

	// First check if this is a main package
	if pkg.Name() != "main" {
		return false
	}

	// Look through all objects in the package scope
	scope := pkg.Scope()
	mainObj := scope.Lookup("main")

	if mainObj == nil {
		return false
	}

	// Check if it's a function
	mainFunc, ok := mainObj.(*types.Func)

	if !ok {
		return false
	}

	// Get the function's type
	funcType, ok := mainFunc.Type().(*types.Signature)

	if !ok {
		return false
	}

	// main function should have no parameters and no return values
	return funcType.Params().Len() == 0 && funcType.Results().Len() == 0
}
