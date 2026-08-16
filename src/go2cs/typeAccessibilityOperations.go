// typeAccessibilityOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"strings"
	"unicode"
	"unicode/utf8"
)

// packageEmittedTypeAccess holds one condensed, single-line C# partial declaration per `[GoType]`
// type this conversion pass emits — `public partial interface Closer {}`, `internal partial struct
// dirEntry {}` — carrying the access modifier the type will actually have, and the type's MOVABLE
// extended attributes ahead of it (`[GoValueClone("h")] internal partial struct wrapper {}`).
// writePackageInfoFile renders it into package_info.cs's `<TypeAccessibility>` section (inside the
// package class, where the types are nested), which PINS each type's accessibility IN SOURCE ahead
// of source generation: the `[GoType]` declaration itself stays bare so it reads like the Go
// original, and a C# nested type with no modifier is PRIVATE until go2cs-gen's own partial supplies
// one — which a generator cannot see while it is running (see the TypeAccessibility prose in any
// package_info.cs and the Source Generators section of docs/ConversionStrategies-Reference.md).
//
// The attributes ride here for the SAME readability reason the modifier does: they are machinery
// the reader of the converted code never needs, and C# unions the attributes of every partial
// declaration, so a consumer that reads them off the type — runtime reflection, or a generator
// resolving the symbol's declarations — sees no difference. Which attributes qualify, and why the
// rest cannot move, is classified in the Extended Attributes section of
// docs/ConversionStrategies-Reference.md.
//
// The RENDERED LINE is the identity, exactly like the other package_info.cs sections, so the
// writer's merge path (the -tests seeded files) unions the two sides without re-deriving anything.
// Reset per package/variant by resetPackageState; written under packageLock.
var packageEmittedTypeAccess HashSet[string]

// TypeAccessibilitySection names the package_info.cs marker section that carries the condensed
// accessibility-pinning partial declarations (see packageEmittedTypeAccess).
const TypeAccessibilitySection = "TypeAccessibility"

// typeAccessibilityIndent is the indentation of the section and its entries: unlike the other
// package_info.cs sections — assembly attributes and `global using` directives, which live at file
// scope — these are TYPE declarations and must be nested in the package class, so the section sits
// inside the class body.
const typeAccessibilityIndent = "    "

// typeAccessibilityProseLines returns the section's explanatory comment — deliberately
// DESCRIPTIVE prose (what the declarations do), not historical rationale: this text persists in
// every package_info.cs. The deeper generator-can't-see-its-own-output story lives in the Source
// Generators section of docs/ConversionStrategies-Reference.md.
func typeAccessibilityProseLines() []string {
	return []string{
		typeAccessibilityIndent + "// C# nested types declared with no access modifier are always private, and the",
		typeAccessibilityIndent + "// `[GoType]` declarations in this package's converted sources are deliberately",
		typeAccessibilityIndent + "// bare so they read more like the original Go code. The real accessibility for",
		typeAccessibilityIndent + "// the types - public for a Go-exported name, internal otherwise - are defined",
		typeAccessibilityIndent + "// via declarations below.",
	}
}

// legacyTypeAccessibilityFirstLine identifies the first line of the section's ORIGINAL prose block
// (2026-07-25, pre-condensing) so ensureTypeAccessibilitySection can migrate a persisted file to
// the current wording in place.
const legacyTypeAccessibilityFirstLine = "// A C# nested type declared with no access modifier is PRIVATE"

// typeAccessibilitySectionLines returns the section's explanatory prose and its marker delimiters,
// in the style of the ImportedTypeAliases / ExportedTypeAliases / InterfaceImplementations blocks
// that precede it. This is the ONLY definition of the block: it is inserted into the package info
// file on demand (see ensureTypeAccessibilitySection) rather than carried in
// package_info-template.txt, so a template-generated file and a pre-existing one that predates the
// section end up byte-identical.
func typeAccessibilitySectionLines() []string {
	return append(typeAccessibilityProseLines(),
		"",
		typeAccessibilityIndent+"// <"+TypeAccessibilitySection+">",
		typeAccessibilityIndent+"// </"+TypeAccessibilitySection+">",
	)
}

// ensureTypeAccessibilitySection returns packageInfoLines with the TypeAccessibility prose and
// marker section present, inserting it at the top of the FIRST package class's body when absent.
// The class body is where the section must live — its entries are type declarations, and the types
// they name are nested in that class. Two callers rely on the insertion: a package info file
// generated before the section existed (every file in a tree converted by an older go2cs), and the
// -tests seed files, which compose their own contents rather than using the shared template.
func ensureTypeAccessibilitySection(packageInfoLines []string) []string {
	openTag := "<" + TypeAccessibilitySection + ">"

	markerIndex := -1

	for i, line := range packageInfoLines {
		if strings.Contains(line, openTag) {
			markerIndex = i
			break
		}
	}

	if markerIndex >= 0 {
		// Section already present; converge its prose on the current wording (migrateProseBlock).
		return migrateProseBlock(packageInfoLines, legacyTypeAccessibilityFirstLine, openTag, typeAccessibilityProseLines())
	}

	insertIndex := -1
	sawClassDeclaration := false

	for i, line := range packageInfoLines {
		trimmed := strings.TrimSpace(line)

		if strings.Contains(trimmed, "partial class ") {
			sawClassDeclaration = true

			// An Allman-braced declaration puts the opening brace on the next line; a K&R one
			// (never emitted today, but cheap to honor) ends the declaration line with it.
			if strings.HasSuffix(trimmed, "{") {
				insertIndex = i + 1
				break
			}

			continue
		}

		if sawClassDeclaration && trimmed == "{" {
			insertIndex = i + 1
			break
		}
	}

	if insertIndex < 0 {
		return packageInfoLines
	}

	block := typeAccessibilitySectionLines()

	return append(packageInfoLines[:insertIndex], append(block, packageInfoLines[insertIndex:]...)...)
}

// generatedTypeScope mirrors go2cs-gen's Common.GetScope — the rule TypeGenerator uses to pick the
// access modifier of the partial it emits for a `[GoType]` declaration that carries none. The two
// MUST agree: both parts name the same type, and C# rejects partial declarations with conflicting
// accessibility (CS0262). Note this is deliberately NOT getAccess: GetScope reads the C# identifier
// verbatim, so a Δ collision-rename (a Greek capital) reads as exported where getAccess strips the
// prefix first. Mirroring the generator keeps the corpus's effective accessibility unchanged — the
// section only moves WHERE the modifier is written, never WHAT it is.
func generatedTypeScope(identifier string) string {
	if identifier == "" {
		return "internal"
	}

	first, size := utf8.DecodeRuneInString(identifier)

	// A '_'-prefixed identifier (other than the bare blank '_') is unexported → internal.
	if first == '_' {
		if size == len(identifier) {
			return "public"
		}

		return "internal"
	}

	if unicode.IsUpper(first) {
		return "public"
	}

	return "internal"
}

// typeAccessibilityKey is the sort key of a `<TypeAccessibility>` entry: the declaration with its
// movable-attribute prefix stripped. The section sorts on THIS rather than on the whole line, so an
// entry carrying attributes still sits with its accessibility/kind/name peers — a stamped type keeps
// the place it would have had unstamped, instead of being pushed into a separate leading block by
// the '[' that now starts its line.
func typeAccessibilityKey(line string) string {
	for strings.HasPrefix(line, "[") {
		end := attributeGroupEnd(line)

		if end < 0 {
			break
		}

		line = strings.TrimLeft(line[end+1:], " ")
	}

	return line
}

// attributeGroupEnd returns the index of the ']' closing the attribute group that starts at line[0],
// or -1 when the group is unterminated. Brackets inside a quoted argument (a Go name a stamp carries
// verbatim) do not nest the scan, and a backslash escapes the next character within such a string.
func attributeGroupEnd(line string) int {
	depth := 0
	inString := false

	for i := 0; i < len(line); i++ {
		switch ch := line[i]; {
		case inString && ch == '\\':
			i++
		case ch == '"':
			inString = !inString
		case inString:
			// Any bracket here is argument text, not structure.
		case ch == '[':
			depth++
		case ch == ']':
			depth--

			if depth == 0 {
				return i
			}
		}
	}

	return -1
}

// recordTypeAccessibility records the accessibility of one emitted `[GoType]` declaration for the
// package_info.cs `<TypeAccessibility>` section, and RELOCATES the declaration's movable extended
// attributes onto that record. kind is the C# type kind as emitted ("struct", "class" or
// "interface"), identifier the emitted (already sanitized) C# name, typeParams the declaration's
// type-parameter list ("<K, V>", or "" for a non-generic type — constraints are deliberately
// omitted, a partial declaration may leave them to another part), access the explicit modifier the
// converter emitted inline ("public ", from the publicization pre-pass) or "" to let the
// generator's own name-based rule decide, and attrs the movable attribute stamps the declaration
// would otherwise carry inline, each already trailing-spaced (`[GoValueClone("h")] `).
//
// The RETURN VALUE is what the caller must still write inline: empty when the record absorbed the
// attributes, attrs verbatim when no record is written. That is the same where-is-it-written split
// the access modifier already makes — the two must move together, since the record is the only
// declaration that survives to carry them.
//
// A file whose destination `.cs` is a HAND-OWNED manual conversion ([module: GoManualConversion])
// is skipped: the converter's emission for it goes to the non-compiled `.cs.auto` sibling, so the
// declarations that actually compile are the hand-written ones — their kind, name and modifier are
// the author's to choose, and a generated section entry could contradict them (CS0261/CS0262) or
// conjure a phantom empty type the hand-written file never declares. A -tests bridge unit
// (testInlineTypeAccess) is skipped for its own reason: its metadata anchor can be a different test
// class, where an accessibility-only partial would declare a second type. Both keep the attributes
// on the declaration, where they read as they always have.
func (v *Visitor) recordTypeAccessibility(kind string, identifier string, typeParams string, access string, attrs string) string {
	if v.manualConversion || v.options.testInlineTypeAccess || identifier == "" {
		return attrs
	}

	if access == "" {
		access = generatedTypeScope(identifier) + " "
	}

	line := fmt.Sprintf("%s%spartial %s %s%s {}", attrs, access, kind, identifier, typeParams)

	packageLock.Lock()
	packageEmittedTypeAccess.Add(line)
	packageLock.Unlock()

	return ""
}

// localTypeAccess returns the access modifier a FUNCTION-LOCAL type must carry in the internal
// white-box test bridge, or "" when the caller's own rule applies.
//
// A type declared inside a function body has NO Go exportedness. The export convention governs
// PACKAGE-LEVEL identifiers; a function-local `S8` is exactly as unreachable from outside its
// function as `embed2` is, and Go draws no distinction between them. go2cs hoists such a type to
// package scope under a `<Func>_<name>` identifier, and both name-based accessibility rules then
// read an export meaning into a name that never carried one:
//
//   - the bridge arm of visitTypeSpec asks generatedTypeScope for the LOCAL name, so the siblings
//     declared by one function split between public and internal;
//   - a lifted ANONYMOUS struct/interface carries no modifier at all, so go2cs-gen's GetScope reads
//     the MANGLED name and inherits the case of the ENCLOSING FUNCTION — `TestEncoderSetEscapeHTML_type`
//     is public because the Test function is.
//
// C#'s accessibility-consistency rule then rejects the mixture. encoding/json's
// TestUnmarshalEmbeddedUnexported declares `S8` beside `embed2` and makes one a field of the other:
// a public S8 over an internal embed2 is CS0053, with CS0050/CS0051 following on the accessors and
// constructor go2cs-gen generates for the promotion, and CS0052 on the third shape (a lifted
// anonymous struct over the package-level unexported `strMarshaler`). That is the ENTIRE compile
// wall of the package's suite — 76 errors, four codes, one cause.
//
// `internal` is both faithful and sufficient: no Go consumer outside the function can name the type,
// and every emitted C# consumer — the hoisted siblings and the converted function body — compiles
// into the same test assembly. Writing it INLINE is what makes go2cs-gen follow: the generator
// honors a modifier the declaration already carries and falls back to its own name rule only for a
// bare one (measured — `internal partial struct TestUnmarshalEmbeddedUnexported_embed2` is
// reproduced verbatim in the generated part, while the bare lifts were regenerated `public`).
//
// Deliberately scoped to the bridge. The PRODUCTION path leaves access empty and lets
// recordTypeAccessibility pin generatedTypeScope of the MANGLED name, which gives every local type
// of one function the SAME modifier — uniform, and consistent for that reason rather than by
// design. The identical latent mixture exists there (a function-local struct with an exported field
// of a package-level unexported type), but no corpus package exhibits it today, and flipping
// production local types to internal would move a public value adapter's operand out from under it.
// Measured and boarded rather than changed speculatively.
func (v *Visitor) localTypeAccess() string {
	if v.inFunction && v.options.testInlineTypeAccess {
		return "internal "
	}

	return ""
}

// packagePublicizedTypes holds unexported named types in the package that must be emitted as
// `public` because they are used as the type of an exported (public) struct field. C# requires a
// field's type to be at least as accessible as the field itself, so an exported field of an
// unexported type would otherwise fail with CS0052 (and its generated constructor with CS0051).
//
// Populated by a synchronous per-package pre-pass (collectPublicizedTypes), then read-only during
// concurrent file visiting. Keyed by the type's *types.TypeName object, interned per type.
var packagePublicizedTypes map[types.Object]bool

// packagePublicizedLiftedTypes holds anonymous struct/interface types that go2cs LIFTS to a
// synthesized `…ᴛN` named type and that must be emitted `public` because they appear in the
// parameter/result signature of a publicized interface's method (or an exported method/func/
// delegate), where they would otherwise be less accessible than the now-public member (CS0050/
// CS0051). Signature positions only — an exported FIELD/VAR of an anonymous struct is the separate
// CS0052 domain and is left to the named-only walker (see collectSignatureTypes).
//
// Unlike packagePublicizedTypes these have no *types.Object to key on — the lift is a synthesized
// name over a raw anonymous types.Type — so they are interned by the anonymous type itself, with any
// enclosing alias stripped (types.Unalias). The archetype is testing's `type corpusEntry =
// struct{…}`: the ALIAS targets an anonymous struct that lifts to `corpusEntryᴛ1`, referenced by the
// publicized `testDeps` interface's fuzzing methods (CoordinateFuzzing/RunFuzzWorker/ReadCorpus).
//
// Populated by the same synchronous pre-pass as packagePublicizedTypes, then read-only during file
// visiting; consulted at the lift's emission (visitStructType → isPublicizedLiftedType).
var packagePublicizedLiftedTypes map[types.Type]bool

// collectPublicizedTypes records every unexported named type that is referenced by an exported
// field of any package-level struct (directly, or through a pointer/slice/array/map/channel
// element). Scanning the exported fields of every struct in one pass is sufficient: a publicized
// struct's own exported-field types are already covered because that struct was itself scanned.
func collectPublicizedTypes(pkg *types.Package) {
	if packagePublicizedTypes == nil {
		packagePublicizedTypes = map[types.Object]bool{}
	}

	if packagePublicizedLiftedTypes == nil {
		packagePublicizedLiftedTypes = map[types.Type]bool{}
	}

	scope := pkg.Scope()

	defer cascadePublicizedMethodTypes()

	for _, name := range scope.Names() {
		obj := scope.Lookup(name)

		switch obj := obj.(type) {
		case *types.TypeName:
			// An EXPORTED defined type over an unexported NAMED type — `type EncoderBuffer
			// encoder` (image/png). Its [GoType("encoder")] wrapper's Value property, ctor,
			// and implicit operators all expose the wrapped `encoder`; a public wrapper over
			// an internal type is CS0051/CS0053/CS0056/CS0057 (and C# conversion operators
			// MUST be public, CS0558, so an internal wrapper is not an option). Publicize the
			// written RHS to match the wrapper's accessibility.
			if obj.Exported() {
				collectPublicizedWrapperRHS(obj, pkg)

				// An EXPORTED type's EXPORTED methods are emitted public; an unexported
				// param/result type is then less accessible than the public method
				// (crypto/internal/bigmod's `func (x *Nat) Equal(y *Nat) choice`, choice
				// unexported → CS0050). Publicize those signature types. The fixpoint cascade
				// below then carries them through any further exported-method chains.
				if named, ok := obj.Type().(*types.Named); ok {
					collectMethodSignatureUnexportedTypes(named, pkg)
				}

				// An EXPORTED named FUNC type is emitted as a public C# delegate; an unexported
				// type in its signature is then less accessible than the delegate (CS0059,
				// x/text/unicode/bidi's `type Option func(*options)` → `public delegate void
				// Option(ж<options> _)`). Publicize those signature types like an exported method's.
				if sig, ok := obj.Type().Underlying().(*types.Signature); ok {
					if params := sig.Params(); params != nil {
						for i := range params.Len() {
							collectSignatureTypes(params.At(i).Type(), pkg)
						}
					}

					if results := sig.Results(); results != nil {
						for i := range results.Len() {
							collectSignatureTypes(results.At(i).Type(), pkg)
						}
					}
				}
			}

			structType, ok := obj.Type().Underlying().(*types.Struct)

			if !ok {
				continue
			}

			for i := range structType.NumFields() {
				field := structType.Field(i)

				// Only an exported field forces its type to be at least as accessible; an
				// unexported (internal) field of an internal type is fine.
				if !field.Exported() {
					continue
				}

				collectUnexportedNamedTypes(field.Type(), pkg)
			}
		case *types.Var, *types.Const:
			// An EXPORTED package-level var (or typed const) of an unexported type — Go's
			// `var ErrNetClosing = errNetClosing{}` (internal/poll) — emits a public static
			// field whose type must be at least as accessible (CS0052). Go consumers can
			// legally hold the value and call its exported methods, so publicizing the type
			// is the faithful mapping.
			if obj.Exported() {
				collectUnexportedNamedTypes(obj.Type(), pkg)
			}
		case *types.Func:
			// An EXPORTED function's parameter/result types face the same rule on the public
			// static method (CS0050/CS0051) — `func Peek() snapshot` with `snapshot`
			// unexported.
			if !obj.Exported() {
				continue
			}

			if sig, ok := obj.Type().(*types.Signature); ok {
				if params := sig.Params(); params != nil {
					for i := range params.Len() {
						collectSignatureTypes(params.At(i).Type(), pkg)
					}
				}

				if results := sig.Results(); results != nil {
					for i := range results.Len() {
						collectSignatureTypes(results.At(i).Type(), pkg)
					}
				}
			}
		}
	}
}

// cascadePublicizedMethodTypes extends the publicized set through method signatures: a publicized
// type's EXPORTED methods are emitted public (see the receiver-access logic in visitFuncDecl), so
// their parameter/result types face the same accessibility rule (CS0050/CS0051). Runs to a fixpoint
// since each newly publicized type exposes its own exported methods.
func cascadePublicizedMethodTypes() {
	for {
		before := len(packagePublicizedTypes)

		for obj := range packagePublicizedTypes {
			named, ok := types.Unalias(obj.Type()).(*types.Named)

			if !ok {
				continue
			}

			collectMethodSignatureUnexportedTypes(named, obj.Pkg())

			// A publicized WRAPPER (an unexported `type A b` reached here through the field/
			// method cascade) also exposes its wrapped RHS through the public wrapper —
			// propagate through the RHS, same as the exported-wrapper seed below.
			if tn, ok := obj.(*types.TypeName); ok {
				collectPublicizedWrapperRHS(tn, obj.Pkg())
			}
		}

		if len(packagePublicizedTypes) == before {
			break
		}
	}
}

// collectMethodSignatureUnexportedTypes publicizes the unexported named types that appear in
// the EXPORTED methods' parameter/result signatures of a named type whose methods are emitted
// public (an exported type, or an unexported-but-publicized one). An exported method returning
// an unexported type is CS0050 (crypto/internal/bigmod's `Nat.Equal() choice`); a param of one
// is CS0051.
func collectMethodSignatureUnexportedTypes(named *types.Named, pkg *types.Package) {
	for i := range named.NumMethods() {
		method := named.Method(i)

		// A CONCRETE method's emitted accessibility tracks Go exportedness (an unexported
		// method emits `internal static … sockaddr(this ж<SockaddrInet4> …)`), so an
		// unexported one exposes nothing public and is gated out.
		collectSignatureUnexportedTypes(method, pkg, false)
	}

	// A defined INTERFACE type's methods live on its UNDERLYING *types.Interface, not on the Named
	// (named.NumMethods() is 0 for an interface). When such an interface is publicized — an unexported
	// interface reached through an exported surface, emitted `public` (testing's `testDeps` through
	// `MainStart(deps testDeps, …)`) — its methods become PUBLIC interface members, so the unexported
	// named types in their parameter/result signatures must be publicized too, or they are less
	// accessible than the public member (CS0051 param, CS0050 result — testDeps.CoordinateFuzzing's
	// `corpusEntry`). The cascade fixpoint then propagates through those types in turn.
	if iface, ok := named.Underlying().(*types.Interface); ok {
		for i := range iface.NumMethods() {
			// An INTERFACE member is emitted with NO access modifier (see visitInterfaceType),
			// and a C# interface member with none is implicitly PUBLIC — Go's case convention
			// does not survive into the emitted surface. So the member of a public interface is
			// public whether or not the Go method is exported, and its signature types must be
			// publicized either way. syscall's `Sockaddr` is the archetype: the sealing method
			// `sockaddr() (unsafe.Pointer, _Socklen, error)` is deliberately unexported so only
			// the package can implement the interface, yet the emitted member returns the
			// unexported `_Socklen` from a public interface — CS0050 on every unix flavor.
			// (Windows spells the same method with `int32`, so the corpus never saw it.)
			collectSignatureUnexportedTypes(iface.Method(i), pkg, true)
		}
	}
}

// collectSignatureUnexportedTypes publicizes the unexported named types (and lifted anonymous
// struct/interface types) in a method's parameter/result signature, when the method is emitted as a
// PUBLIC C# member. memberAlwaysPublic states that the emitted member's accessibility does NOT track
// Go exportedness — true for an interface member, which carries no access modifier and is therefore
// implicitly public whatever the Go method's case. It is the EMITTED C# surface, not the Go
// exportedness, that decides what must be lifted.
func collectSignatureUnexportedTypes(method *types.Func, pkg *types.Package, memberAlwaysPublic bool) {
	if !memberAlwaysPublic && !method.Exported() {
		return
	}

	sig, ok := method.Type().(*types.Signature)

	if !ok {
		return
	}

	if params := sig.Params(); params != nil {
		for j := range params.Len() {
			collectSignatureTypes(params.At(j).Type(), pkg)
		}
	}

	if results := sig.Results(); results != nil {
		for j := range results.Len() {
			collectSignatureTypes(results.At(j).Type(), pkg)
		}
	}
}

// collectPublicizedWrapperRHS publicizes the written RHS of a defined type whose [GoType]
// wrapper is emitted public (exported, or unexported-but-publicized). `type EncoderBuffer
// encoder` exposes `encoder` through the wrapper's Value/ctor/implicit operators, so the
// wrapped named type must be at least as accessible. This also reaches through an UNNAMED
// composite RHS to its element types: `type ringElement [256]fieldElement` exposes
// `fieldElement` through the wrapper's indexer/Value/ToSpan, so the array's element type must
// be publicized too (crypto/internal/mlkem768's fieldElement, CS0050/CS0051/CS0053/CS0054/
// CS0056/CS0057). collectUnexportedNamedTypes peels pointer/slice/array/map/chan to the element
// but has NO struct case, so a struct RHS stays a no-op — an exported field of an unexported
// type is the CS0052 domain and is intentionally left internal.
func collectPublicizedWrapperRHS(tn *types.TypeName, pkg *types.Package) {
	if packageTypeSpecRHS == nil {
		return
	}

	rhs, ok := packageTypeSpecRHS[tn]

	if !ok || rhs == nil {
		return
	}

	collectUnexportedNamedTypes(types.Unalias(rhs), pkg)
}

// receiverTypeIsPublicized reports whether the (possibly pointer-wrapped) receiver type is an
// unexported named type that is nonetheless emitted `public` (see packagePublicizedTypes). Its
// exported methods must then be public too, or a cross-assembly caller holding the value through
// the exported var/field/return cannot call them (encoding/binary's BigEndian.Uint32 — CS1061).
func receiverTypeIsPublicized(t types.Type) bool {
	if packagePublicizedTypes == nil {
		return false
	}

	if ptr, ok := t.(*types.Pointer); ok {
		t = ptr.Elem()
	}

	if named, ok := types.Unalias(t).(*types.Named); ok {
		return packagePublicizedTypes[named.Obj()]
	}

	return false
}

// collectUnexportedNamedTypes records the unexported named types of this package that the given
// type references, peeling pointer/slice/array/map/channel wrappers to reach the element types.
func collectUnexportedNamedTypes(t types.Type, pkg *types.Package) {
	switch t := t.(type) {
	case *types.Named:
		obj := t.Obj()

		if obj.Pkg() == pkg && !obj.Exported() {
			packagePublicizedTypes[obj] = true
		}
	case *types.Pointer:
		collectUnexportedNamedTypes(t.Elem(), pkg)
	case *types.Slice:
		collectUnexportedNamedTypes(t.Elem(), pkg)
	case *types.Array:
		collectUnexportedNamedTypes(t.Elem(), pkg)
	case *types.Map:
		collectUnexportedNamedTypes(t.Key(), pkg)
		collectUnexportedNamedTypes(t.Elem(), pkg)
	case *types.Chan:
		collectUnexportedNamedTypes(t.Elem(), pkg)
	case *types.Signature:
		// A FUNC-typed element of an exported field/var — `var SupportedKDFs =
		// map[uint16]func() *hkdfKDF` (crypto/internal/hpke), `var F func() snapshot`, or a
		// []func(x internalT) field — emits a public field whose type embeds the func's
		// parameter/result types (`map<uint16, Func<ж<hkdfKDF>>>`). C# requires those to be at
		// least as accessible as the public field, so an unexported named type reachable ONLY
		// through the signature must be publicized too, or it is less accessible than the field
		// (CS0052). Peeling stops at Signature in the wrapper cases above, so walk the params and
		// results back through the same named-only recursion (which handles a nested func result
		// in turn). A lifted anonymous struct/interface in the signature is the CS0050/CS0051
		// signature domain (collectSignatureTypes), not this exported-field CS0052 walk.
		if params := t.Params(); params != nil {
			for i := range params.Len() {
				collectUnexportedNamedTypes(params.At(i).Type(), pkg)
			}
		}

		if results := t.Results(); results != nil {
			for i := range results.Len() {
				collectUnexportedNamedTypes(results.At(i).Type(), pkg)
			}
		}
	}
}

// collectSignatureTypes is the SIGNATURE-context counterpart of collectUnexportedNamedTypes: in
// addition to publicizing unexported named types, it publicizes the LIFTED anonymous struct/
// interface types that appear in a PUBLIC method/func/delegate signature. A public callable whose
// parameter/result is a less-accessible type is CS0050/CS0051 — and an anonymous struct/interface
// written (or aliased) in the signature is lifted to a synthesized `…ᴛN` type that defaults to
// internal (testing's `type corpusEntry = struct{…}` alias in the publicized `testDeps` interface;
// TypeConversionInterfaceParam's inline `Process(struct{…})`). This is deliberately NOT folded into
// collectUnexportedNamedTypes: an exported FIELD/VAR of an anonymous struct is the CS0052 domain and
// is intentionally left to the named-only walker (a public struct/var over an internal anon field
// type is legal when its own enclosing type is internal), so only signature positions lift here.
func collectSignatureTypes(t types.Type, pkg *types.Package) {
	switch t := t.(type) {
	case *types.Named:
		obj := t.Obj()

		if obj.Pkg() == pkg && !obj.Exported() {
			packagePublicizedTypes[obj] = true
		}
	case *types.Alias:
		// A type ALIAS in the signature (`type corpusEntry = struct{…}`). Strip the alias and route
		// the underlying type back through the switch: an anonymous struct/interface target lifts and
		// is recorded below; a named target is recorded via the *types.Named arm.
		collectSignatureTypes(types.Unalias(t), pkg)
	case *types.Struct:
		collectPublicizedLiftedType(t, pkg)
	case *types.Interface:
		collectPublicizedLiftedType(t, pkg)
	case *types.Pointer:
		collectSignatureTypes(t.Elem(), pkg)
	case *types.Slice:
		collectSignatureTypes(t.Elem(), pkg)
	case *types.Array:
		collectSignatureTypes(t.Elem(), pkg)
	case *types.Map:
		collectSignatureTypes(t.Key(), pkg)
		collectSignatureTypes(t.Elem(), pkg)
	case *types.Chan:
		collectSignatureTypes(t.Elem(), pkg)
	}
}

// collectPublicizedLiftedType records an anonymous struct/interface type that go2cs lifts to a
// synthesized `…ᴛN` named type and that must be emitted `public` because it appears in a public
// callable signature (a publicized interface method, or an exported method/func/delegate). A lifted
// type carries no *types.Object, so it is interned by the anonymous type itself and consulted at
// emission (isPublicizedLiftedType). A NAMED type is not routed here — it carries its own
// accessibility and is handled by packagePublicizedTypes.
func collectPublicizedLiftedType(t types.Type, pkg *types.Package) {
	if t == nil || packagePublicizedLiftedTypes[t] {
		return
	}

	packagePublicizedLiftedTypes[t] = true

	// A publicized lifted struct's EXPORTED fields become public members, so a field whose type is
	// less accessible than the now-public struct is CS0052. Publicize those field types too (a
	// named unexported field type; or a nested lifted anonymous struct — hence collectSignatureTypes,
	// which the recursion guard above keeps finite).
	if st, ok := t.(*types.Struct); ok {
		for i := range st.NumFields() {
			field := st.Field(i)

			if !field.Exported() {
				continue
			}

			collectSignatureTypes(field.Type(), pkg)
		}
	}
}

// isPublicizedLiftedType reports whether the lifted anonymous type must be emitted `public` (see
// packagePublicizedLiftedTypes). Consulted at the lift's emission in visitStructType; the
// alias-stripped form is also checked so a key interned under either representation matches.
func isPublicizedLiftedType(t types.Type) bool {
	if packagePublicizedLiftedTypes == nil || t == nil {
		return false
	}

	if packagePublicizedLiftedTypes[t] {
		return true
	}

	return packagePublicizedLiftedTypes[types.Unalias(t)]
}

// signatureReferencesUnexportedProductionType reports whether a function/method signature references,
// in any parameter or result position (peeling pointer/slice/array/map/channel wrappers to the
// element), an unexported named type of pkg that is declared in a PRODUCTION (non-test) file.
//
// It is the MIRROR of the production publicization framework, used for test-file-declared symbols:
// production emits an unexported type as `internal` and is converted independently of (and before)
// the test files, so a test file's EXPORTED helper — Go's `func NewDecimal(uint64) *decimal` in
// strconv's internal_test.go — would be a public method whose result type is the less-accessible
// internal production `decimal` (CS0050). In the recompile test model the test assembly is self-
// contained (production + internal + external test files compile into ONE assembly, no cross-assembly
// consumer of a test symbol), so downgrading such a helper to `internal` is both correct and
// sufficient — internal is at least as restrictive as any accessibility the helper references, and
// every caller (other test files) lives in the same assembly.
//
// The PRODUCTION-file restriction is essential: an unexported type declared in a TEST file (sort's
// `multiSorter` in example_multi_test.go, returned by the exported `OrderedBy`) is publicized AND
// re-emitted `public` within this same test pass by the framework above, so its exported referrer
// compiles as public with no CS0050 — flipping it to internal would be a needless emission change.
// Only a production-declared unexported type stays internal-on-disk and forces the downgrade.
func (v *Visitor) signatureReferencesUnexportedProductionType(sig *types.Signature, pkg *types.Package) bool {
	if sig == nil {
		return false
	}

	if params := sig.Params(); params != nil {
		for i := range params.Len() {
			if v.typeReferencesUnexportedProductionNamed(params.At(i).Type(), pkg) {
				return true
			}
		}
	}

	if results := sig.Results(); results != nil {
		for i := range results.Len() {
			if v.typeReferencesUnexportedProductionNamed(results.At(i).Type(), pkg) {
				return true
			}
		}
	}

	return false
}

// typeReferencesUnexportedProductionNamed reports whether t is, or wraps (through pointer/slice/array/
// map/channel), an unexported named type of pkg declared in a production (non-test) file — the read-
// only counterpart of collectUnexportedNamedTypes, with the production-file gate described on
// signatureReferencesUnexportedProductionType.
func (v *Visitor) typeReferencesUnexportedProductionNamed(t types.Type, pkg *types.Package) bool {
	switch t := t.(type) {
	case *types.Named:
		obj := t.Obj()
		return obj.Pkg() == pkg && !obj.Exported() && !v.isTestFileDecl(obj.Pos())
	case *types.Pointer:
		return v.typeReferencesUnexportedProductionNamed(t.Elem(), pkg)
	case *types.Slice:
		return v.typeReferencesUnexportedProductionNamed(t.Elem(), pkg)
	case *types.Array:
		return v.typeReferencesUnexportedProductionNamed(t.Elem(), pkg)
	case *types.Map:
		return v.typeReferencesUnexportedProductionNamed(t.Key(), pkg) || v.typeReferencesUnexportedProductionNamed(t.Elem(), pkg)
	case *types.Chan:
		return v.typeReferencesUnexportedProductionNamed(t.Elem(), pkg)
	}

	return false
}

// testDeclaredValueAccess applies the test-file accessibility downgrade to a package-level VAR or
// CONST — the exact mirror of the one visitFuncDecl applies to an exported test-file free function,
// and it rests on the same reasoning (see signatureReferencesUnexportedProductionType): production
// emits an unexported type `internal` and is converted independently of, and before, the test files,
// so a PUBLIC test-declared field over one is CS0052 — its type is less accessible than the field.
// The recompile test model puts production + internal + external test files in ONE self-contained
// assembly, so `internal` is both correct and sufficient: every caller is a sibling test file.
//
// Go's `var Options = options` in internal/cpu's export_test.go is the archetype — `options` is
// `[]option` and `option` is an unexported production struct. The field was harmless while the
// white-box bridge class carried no access modifier (a top-level C# class with none is internal, so
// its `public` members were internal in effect); it became CS0052 the moment the bridge gained the
// `public static partial` declaration it needs to host extension methods, which is why this rule
// only surfaced with that emission and not with the FUNC rule it mirrors.
func (v *Visitor) testDeclaredValueAccess(access string, pos token.Pos, valueType types.Type) string {
	if access != "public" || !v.isTestFileDecl(pos) {
		return access
	}

	if !v.typeReferencesUnexportedProductionNamed(valueType, v.pkg) {
		return access
	}

	return "internal"
}

// isTestFileDecl reports whether the declaration at pos originates from a Go test file (`*_test.go`).
// Used to gate test-only accessibility handling; resolves the source filename through the visitor's
// FileSet so it is independent of which file the visitor currently has open.
func (v *Visitor) isTestFileDecl(pos token.Pos) bool {
	if v.fset == nil || !pos.IsValid() {
		return false
	}

	return strings.HasSuffix(strings.ToLower(v.fset.Position(pos).Filename), "_test.go")
}

// isPublicizedType reports whether the named type identified by ident must be emitted as public
// (it is used as the type of an exported struct field; see packagePublicizedTypes).
func (v *Visitor) isPublicizedType(ident *ast.Ident) bool {
	if ident == nil || packagePublicizedTypes == nil {
		return false
	}

	obj := v.info.Defs[ident]

	if obj == nil {
		obj = v.info.Uses[ident]
	}

	return obj != nil && packagePublicizedTypes[obj]
}
