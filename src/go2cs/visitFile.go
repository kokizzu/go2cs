// visitFile.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"log"
	"sort"
	"strings"
)

const TypeAliasMarker = ">>MARKER:TYPEALIASES<<"
const UsingsMarker = ">>MARKER:USINGS<<"

// ImportInitMarker reserves the top of the file's class body for the imported-package
// module-initializer hooks (writeImportInit). The import declarations are visited as part of
// the decl walk that follows the class-open line, so the hooks are only known afterwards — and
// they must lead the class body, ahead of the file's own `init` functions.
const ImportInitMarker = ">>MARKER:IMPORTINITS<<"

func (v *Visitor) Visit(node ast.Node) ast.Visitor {
	if node != nil {
		if commentGroup, ok := node.(*ast.CommentGroup); ok {
			for _, comment := range commentGroup.List {
				delete(v.standAloneComments, comment.Slash)
			}
		}
	}

	return v
}

func (v *Visitor) visitFile(file *ast.File) {
	// Recover the contextual types go/types drops inside constant expressions (see
	// markUntypedConstContexts) before any expression conversion renders a literal
	v.markUntypedConstContexts(file)

	if v.options.includeComments {
		// Create standalone comments map
		for _, commentGroup := range file.Comments {
			for _, comment := range commentGroup.List {
				v.standAloneComments[comment.Slash] = comment.Text
			}
		}

		// Remove CommentGroup instances that exist as AST nodes from standalone comments map
		ast.Walk(v, file)

		for pos := range v.standAloneComments {
			v.sortedCommentPos = append(v.sortedCommentPos, pos)
		}

		sort.Slice(v.sortedCommentPos, func(i, j int) bool {
			return v.sortedCommentPos[i] < v.sortedCommentPos[j]
		})

		v.writeDoc(file.Doc, file.Package)
	}

	// Derive package name
	packageLock.Lock()

	if len(packageName) == 0 {
		packageName = file.Name.Name
	} else {
		if packageName != file.Name.Name {
			log.Fatalf("Multiple package names encountered: %s and %s", packageName, file.Name.Name)
		}
	}

	packageLock.Unlock()

	v.writeOutput(TypeAliasMarker)
	v.writeOutputLn("namespace %s;", packageNamespace)
	v.outputBuilder.WriteString(v.newline)

	packageClassName := getSanitizedImport(fmt.Sprintf("%s%s", packageName, PackageSuffix))
	if v.options.testClassNameOverride != "" {
		packageClassName = v.options.testClassNameOverride
		productionClassName := getSanitizedImport(v.options.testProductionName + PackageSuffix)
		v.addRequiredUsing(fmt.Sprintf("static %s", globalQualifyRooted(packageNamespace+"."+productionClassName)))
	}
	if v.options.testWhiteboxReference && v.options.testExternalVariant {
		v.addRequiredUsing(fmt.Sprintf("static %s", globalQualifyRooted(packageNamespace+"."+v.options.testInternalBridgeName)))
	}

	v.writeOutput(UsingsMarker)
	v.writeOutputLn("partial class %s {", packageClassName)
	v.writeOutput(ImportInitMarker)

	for _, decl := range file.Decls {
		v.visitDecl(decl)
	}

	// Splice the blank-import hooks in HERE — every import spec has been visited (imports are a
	// file's first declarations), and the trailing-newline bookkeeping below inspects the buffer's
	// LAST characters. An unreplaced marker is opaque to needsNewLine, so a file whose class body is
	// EMPTY (a doc-only or `//go:build ignore` source — hash/crc32/gen.cs, os/signal/doc.cs,
	// regexp/syntax/doc.cs, strconv/doc.cs) ended in the marker text rather than a blank line and
	// gained a spurious second one. Replacing before that check keeps a hookless file byte-identical.
	v.replaceMarker(ImportInitMarker, v.importInits.String())

	if v.options.includeComments {
		// Add any remaining standalone comments
		postCodeComments := strings.Builder{}
		v.writeDocString(&postCodeComments, nil, file.FileEnd)
		v.outputBuilder.WriteString(v.newline)

		if postCodeComments.Len() > 0 {
			v.writeOutputLn(postCodeComments.String())
		} else {
			if v.needsNewLine(v.outputBuilder.String()) {
				v.outputBuilder.WriteString(v.newline)
			}
		}
	} else {
		if v.needsNewLine(v.outputBuilder.String()) {
			v.outputBuilder.WriteString(v.newline)
		}
	}

	v.writeOutputLn("} // end %s", packageClassName)

	// Take the completed body as text so the deferred markers below can be substituted. The using
	// directives and type aliases are only KNOWN once the whole file has been visited, so they are
	// emitted as markers during the walk and resolved here.
	fileText := v.outputBuilder.String()

	// Supply the file-local `using <alias> = <namespace>;` for any foreign package whose type this file
	// referenced in short-alias form (`pkg.Type`, `@unsafe.Pointer`) but did not import under its
	// canonical name — an inferred type, or a blank/dot/renamed import (CS0246). Skipped for a package
	// the file already imports canonically, so a normal import emits its alias exactly once (no
	// duplicate, no churn); a non-canonical alias (`using t = time_package;`) coexists with the added
	// canonical one (`using time = time_package;`) without conflict.
	for _, importPath := range v.referencedForeignPackages.Keys() {
		// referencedForeignPackages is keyed by pkg.Path() (unprefixed for GOROOT-vendored
		// packages), while canonicalAliasImported records the RESOLVED on-disk path — check
		// both forms, or a vendored import's canonical alias emits twice (CS1537).
		if v.canonicalAliasImported.Contains(importPath) || v.canonicalAliasImported.Contains(resolveGorootVendoredPath(importPath)) {
			continue
		}

		alias, namespace := packageUsingAlias(importPath)

		// The alias is collision-renamed (`Δunicode`) when a same-named CHILD namespace is visible from
		// the import closure (`go.unicode`, present once unicode/utf8 is transitively imported):
		// getAliasedTypeName renders this file's short-form type references through that SAME renamed
		// qualifier (`Δunicode.Range16`), so the SUPPLIED canonical using must carry the rename too, or the
		// reference binds an alias that was never emitted (CS0246) — and the bare `using unicode =
		// unicode_package;` would itself collide with the child namespace (CS0576). Surfaced by a -tests
		// external variant that DOT-imports the package under test (unicode's letter_test) yet still
		// references its types qualified; importQualifier is a no-op for any package that isn't renamed, so
		// a non-colliding supplied alias is byte-identical.
		alias = getSanitizedImport(importQualifier(alias))

		// Skip when the canonical alias collides with one a real import already bound to a DIFFERENT
		// namespace: cryptobyte's asn1.go imports `encoding_asn1 "encoding/asn1"` (referenced by type,
		// so it lands here) AND the subpackage `.../cryptobyte/asn1` (unaliased → alias `asn1`), so
		// synthesizing `using asn1 = encoding.asn1_package` duplicates the subpackage's `using asn1`
		// (CS1537) and mis-binds `asn1.X`. The parent is in scope via its `encoding_asn1` alias, so the
		// canonical one is unneeded here. (A non-colliding canonical alias is still supplied, so an
		// inferred type reference to a non-canonically-imported package keeps working — no churn.)
		if v.importAliasesEmitted.Contains(alias) {
			continue
		}

		v.addRequiredUsing(fmt.Sprintf("%s = %s", alias, namespace))
	}

	// Ensure using ortder is consistent
	requiredUsings := v.requiredUsings.Keys()
	sort.Strings(requiredUsings)

	for _, requiredUsing := range requiredUsings {
		v.packageImports.WriteString(fmt.Sprintf("using %s;%s", requiredUsing, v.newline))
	}

	if v.packageImports.Len() > 0 {
		v.packageImports.WriteString(v.newline)
	}

	fileText = strings.ReplaceAll(fileText, UsingsMarker, v.packageImports.String())

	if v.typeAliasDeclarations.Len() > 0 {
		v.typeAliasDeclarations.WriteString(v.newline)
		fileText = strings.ReplaceAll(fileText, TypeAliasMarker, v.typeAliasDeclarations.String())
	} else {
		fileText = strings.ReplaceAll(fileText, TypeAliasMarker, "")
	}

	v.outputBuilder.Reset()
	v.outputBuilder.WriteString(fileText)
}

func (v *Visitor) needsNewLine(text string) bool {
	newLineLen := len(v.newline)
	lastChars := text[len(text)-(newLineLen*2):]

	return lastChars != v.newline+v.newline
}
