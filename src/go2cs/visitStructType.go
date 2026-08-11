// visitStructType.go - Gbtc
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
)

const StructPrefixMarker = ">>MARKER:STRUCT_%s_PREFIX<<"

// Handles struct types in the context of a TypeSpec, ValueSpec, or FieldList
func (v *Visitor) visitStructType(structType *ast.StructType, identType types.Type, name string, doc *ast.CommentGroup, lifted bool, target *strings.Builder) (structTypeName string) {
	var preLiftIndentLevel int
	var structPrefix *strings.Builder
	var liftedIsPublicized bool

	// Intra-function type declarations are not allowed in C#
	if lifted {
		// A lift can arrive with an EMPTY name — an anonymous struct in a call-argument slot
		// whose parameter is unnamed (builtin `new(struct{ types.Type })`, go/internal/
		// gccgoimporter's reserved). An empty name would declare `partial struct  {` and
		// register "" for every reference to the type — a whole-package syntax cascade. Fall
		// back to the generic "type" the other anonymous-type call sites pass
		// (convStructType/convStarExpr).
		if name == "" {
			name = "type"
		}

		structSignatureType := v.getType(structType, false)

		// Structurally IDENTICAL anonymous struct types are ONE Go type: repeated textual
		// occurrences of `struct{ A Struct }` inside a function must lift to a SINGLE C# type,
		// or reflect.Type identity splits per occurrence (encoding/binary's TestSizeStructCache
		// counts descriptor-cache entries — Go adds ONE for four occurrences). A NAMED local
		// declaration keeps per-declaration identity and never dedupes. Function-scoped: the
		// cross-function/package-level anonymous split is a recorded residual.
		var anonLiftKey string

		if _, isAnonStruct := identType.(*types.Struct); isAnonStruct && v.inFunction && structSignatureType != nil {
			anonLiftKey = v.currentFuncName + "\x00" + structSignatureType.String()

			if existing, ok := v.liftedAnonStructNames[anonLiftKey]; ok {
				v.liftedTypeMap[identType] = existing
				v.liftedTypeMap[structSignatureType] = existing
				return existing
			}
		}

		if v.inFunction {
			if target == nil {
				target = &strings.Builder{}
			}

			if !strings.HasPrefix(name, v.currentFuncName+"_") {
				name = fmt.Sprintf("%s_%s", v.currentFuncName, name)
			}

			preLiftIndentLevel = v.indentLevel
			v.indentLevel = 0
		}

		structTypeName = v.getUniqueLiftedTypeName(name)
		v.liftedTypeMap[identType] = structTypeName
		v.liftedTypeMap[structSignatureType] = structTypeName

		if anonLiftKey != "" {
			v.liftedAnonStructNames[anonLiftKey] = structTypeName
		}

		// Package-level lifted structs are shared across the package so other files
		// can resolve cross-file references to this anonymous type (function-local
		// lifts are file/function-scoped and stay out of the shared registry).
		if !v.inFunction && structSignatureType != nil {
			registerDynamicTypeName(structSignatureType.String(), structTypeName)
		}

		// A lifted anonymous struct referenced by a PUBLICIZED interface method (or an exported
		// method/func/delegate) signature must itself be emitted `public`, or it is less accessible
		// than the public member (CS0050/CS0051 — testing's `type corpusEntry = struct{…}` alias
		// lifts to `corpusEntryᴛ1`, referenced by the public `testDeps` fuzzing methods). The lift
		// has no *types.Object, so the publicize pre-pass records the anonymous type itself.
		liftedIsPublicized = isPublicizedLiftedType(identType) || isPublicizedLiftedType(structSignatureType)
	} else {
		structTypeName = name
	}

	if target == nil {
		target = v.outputBuilder

		if !v.inFunction {
			target.WriteString(v.newline)
		}
	}

	structTypeName = getSanitizedIdentifier(structTypeName)
	typeParams, constraints := v.getGenericDefinition(identType)

	if len(constraints) == 0 {
		constraints = " "
	} else {
		constraints = fmt.Sprintf("%s%s%s", constraints, v.newline, v.indent(v.indentLevel))
	}

	if !v.inFunction {
		structPrefix = &strings.Builder{}
	}

	structPrefixMarker := fmt.Sprintf(StructPrefixMarker, structTypeName)
	target.WriteString(structPrefixMarker)
	v.writeDocString(target, doc, structType.Pos())

	var dynamic string

	if lifted {
		dynamic = "(\"dyn\")"
	}

	// A lifted function-local NAMED type carries its original Go name so the reflection
	// bridge's %T / Type.String() prints Go's `binary.Person`, never the function-prefixed
	// lifted identifier (encoding/binary's TestNoFixedSize asserts the exact error text). A
	// separate attribute, never a [GoType] definition token — the TypeGenerator matches the
	// definition slot by exact string (I2.R R-8). Anonymous lifts have no Go name to stamp.
	var localNameAttr string

	if lifted && v.inFunction {
		if named, ok := identType.(*types.Named); ok {
			localNameAttr = fmt.Sprintf("[GoLocalName(\"%s\")] ", named.Obj().Name())
		}
	}

	// Consume any pending publicized-type access modifier (an unexported type used as an
	// exported field). Only the top-level type declaration carries it; nested/anonymous lifts do
	// not, so read and clear before visiting fields (which may recurse into this function).
	access := v.pendingTypeAccess
	v.pendingTypeAccess = ""

	// A lifted anonymous type carries no pendingTypeAccess (only a top-level TypeSpec sets it), so a
	// lift reached through a public surface is publicized here instead (see liftedIsPublicized).
	if liftedIsPublicized && access == "" {
		access = "public "
	}

	// A struct carrying FIXED-SIZE ARRAY fields (directly, or through another such struct) is not
	// completely copied by a plain C# struct assignment — `array<T>` is a struct over a shared T[]
	// backing, so the copy's array writes reach back into the source. Name those fields for
	// go2cs-gen, which generates the struct's IGoValueClone `Clone()`; every Go by-value copy site
	// appends it (typeNeedsValueClone / arrayCloneOperations.go). A struct that needs nothing is
	// unstamped and unchanged.
	var valueCloneAttr string

	if cloneFields := structValueCloneFields(identType); len(cloneFields) > 0 {
		quotedFields := make([]string, len(cloneFields))

		for i, fieldName := range cloneFields {
			quotedFields[i] = fmt.Sprintf("%q", fieldName)
		}

		valueCloneAttr = fmt.Sprintf("[GoValueClone(%s)] ", strings.Join(quotedFields, ", "))
	}

	// Both stamps are MOVABLE: their consumers read them off the TYPE (go2cs-gen resolves the
	// symbol's declarations, golib's reflection bridge reads the runtime Type), and C# unions the
	// attributes of every partial declaration — so they belong on the package_info.cs accessibility
	// record, out of the reader's way, and the `[GoType]` declaration keeps only what identifies it.
	inlineAttrs := v.recordTypeAccessibility("struct", structTypeName, typeParams, access, localNameAttr+valueCloneAttr)

	v.writeStringLn(target, "[GoType%s] %s%spartial struct %s%s%s{", dynamic, inlineAttrs, access, structTypeName, typeParams, constraints)
	v.indentLevel++

	var prevNameDiscardedCount int

	for _, field := range structType.Fields.List {
		v.writeDocString(target, field.Doc, field.Pos())

		if field.Tag != nil {
			v.writeString(target, "[GoTag(")
			target.WriteString(v.convBasicLit(field.Tag, BasicLitContext{u8StringOK: false, spanTargetUnsupported: true}))
			target.WriteString(")]")
			target.WriteString(v.newline)
		}

		var indentOffset int

		if v.inFunction {
			indentOffset = 1
		} else {
			indentOffset = -1
		}

		// Lift the anonymous struct/interface the FIELD's declared type reaches, at ANY depth of its
		// composition — `struct{…}`, `*struct{…}` and `[N]struct{…}`, and equally the composed
		// `[N]*struct{…}`, `[]*struct{…}`, `map[K]struct{…}` and `chan struct{…}` — so the field
		// declaration resolves to a named type (`array<Composed_Ptrs>`) instead of the raw,
		// un-compilable Go `struct{…}` text. This arm used to peel the field type BY HAND, one
		// container level per kind, which is the same one-level shallowness that produced net's
		// CS1031 cascade at the declaration sites; it now shares those sites' recursive descent
		// (extractStructType / extractInterfaceType, both of which already exclude the empty
		// struct/interface — an empty `interface{}` field must map to `any`, never to a marker
		// interface nothing implements). getAliasQualifiedTypeName resolves each composed element through
		// liftedTypeMap. Struct is probed first, matching the previous arm order.
		//
		// A field type carrying an anonymous literal always NAMES the field (an embedded field is a
		// type name by the Go spec, never a literal), so the name guard only skips cases that cannot
		// arise — it is what lets the lift name stay `<struct>_<field>` for every shape.
		if len(field.Names) > 0 {
			if subStructType, subStructIdentType := v.extractStructType(field.Type); subStructType != nil && !v.liftedTypeExists(subStructType) {
				v.indentLevel += indentOffset
				v.visitStructType(subStructType, subStructIdentType, fmt.Sprintf("%s_%s", structTypeName, field.Names[0].Name), field.Comment, true, structPrefix)
				v.indentLevel -= indentOffset

				if structPrefix != nil {
					structPrefix.WriteString(v.newline)
				}

				// Sub-struct tracking (addImplicitSubStructConversions) describes the field's OWN
				// declared type, so it records the two DIRECT shapes it has always recorded: the
				// field IS the anonymous struct, or a pointer straight to it. A struct reached
				// through a slice/array/map/chan element is not the field's type and is not tracked.
				var trackedType types.Type

				if _, isDirect := field.Type.(*ast.StructType); isDirect {
					trackedType = subStructIdentType
				} else if ptrType, isPointer := field.Type.(*ast.StarExpr); isPointer && ptrType.X == ast.Expr(subStructType) {
					trackedType = v.getExprType(ptrType)
				}

				if trackedType != nil {
					v.subStructTypes[identType] = append(v.subStructTypes[identType], trackedType)
				}
			} else if interfaceType, interfaceIdentType := v.extractInterfaceType(field.Type); interfaceType != nil && !v.liftedTypeExists(interfaceType) {
				v.indentLevel += indentOffset
				v.visitInterfaceType(interfaceType, interfaceIdentType, fmt.Sprintf("%s_%s", structTypeName, field.Names[0].Name), field.Comment, true, structPrefix)
				v.indentLevel -= indentOffset

				if structPrefix != nil {
					structPrefix.WriteString(v.newline)
				}
			}
		}

		fieldType := v.getType(field.Type, false)
		goTypeName := v.getAliasQualifiedTypeName(fieldType, false)
		goFullTypeName := v.getFullyQualifiedTypeName(fieldType, false)
		csFullTypeName := convertToCSTypeName(goFullTypeName)

		// The fully-qualified form for emission INTO this source file's body. csFullTypeName is a
		// RELATIVE dotted name (`io.fs_package.FS`); when its leading segment is also imported as a
		// package alias in this file (`using io = io_package;`) C# binds it to that TYPE alias, so the
		// name resolves to the nonexistent nested type `io_package.fs_package.FS` (CS0426). Root-qualify
		// (`go.io.fs_package.FS`) so the leading segment resolves as the child NAMESPACE it names. The
		// unqualified csFullTypeName is kept below as the promotedInterfaceImplementations map KEY, which
		// feeds generator-consumed strings that live in alias-less files (where the relative form
		// resolves and the key must stay stable).
		csEmitTypeName := rootQualifyIfAmbiguous(csFullTypeName)

		// For the actual NAMED-field declaration, prefer the readable file-local package alias
		// (`atomic.Int32` over `sync.atomic_package.Int32`) when this file imports the type's
		// package — keeping the emitted field visually close to the Go source. The fully-qualified
		// csFullTypeName is retained for promotion/interface registration below, which feeds
		// generator-consumed strings that live in alias-less files. (Embedded fields keep the full
		// form for their promoted accessors; only the named-field branch uses the display name.)
		goDisplayTypeName := v.getScopeCheckedTypeName(fieldType)
		csDisplayTypeName := convertToCSTypeName(goDisplayTypeName)

		// A func-typed field whose signature names a type from a MULTI-SEGMENT import path
		// (`Values func([]reflect.Value, *rand.Rand)`, where `rand` is `math/rand`) must be
		// rendered structurally as an Action/Func delegate via getCSharpTypeName. The string-based
		// getAliasQualifiedTypeName/convertToCSTypeName path stringifies the signature as
		// `func([]reflect.Value, *math/rand.Rand)` and then feeds the slash-bearing import path to
		// convertImportPathToNamespace, which splits on '/' and emits the dotted `math.rand.Rand` —
		// but `math` aliases to `math_package`, so `math.rand` resolves to the non-existent
		// `math_package.rand` (CS0426). getCSharpTypeName recurses through the signature per element,
		// qualifying each named type by its package NAME (`rand.Rand`), the alias the file imports.
		//
		// A VARIADIC func-typed field reroutes too: the string path cannot render a variadic
		// signature at all — getAliasQualifiedTypeName's '..' strip reduces the ellipsis of
		// `JoinPath func(elem ...string) string` (go/build's Context) to `.string`, emitting the
		// unparseable `Func<.@string, @string>` (CS1031 + CS1003 ×2), and even unstripped it has
		// no variadic lowering. Structurally the field renders the golib variadic delegate family
		// (`Funcꓸꓸꓸ<@string, @string>` — see iifeDelegateType), which loose-arg, empty and spread
		// calls through the field all bind.
		//
		// Every other signature keeps the display path: a func field with no cross-package import —
		// `func(string) (importPath string, ok bool)` — preserves its named tuple elements
		// (structural rendering drops them). Compiling correctness for the broken cases is worth
		// the lost tuple names in the rare rerouted field.
		if sig, isSignature := fieldType.(*types.Signature); isSignature && (sig.Variadic() || strings.Contains(goDisplayTypeName, "/")) {
			csDisplayTypeName = v.getCSharpTypeName(fieldType)
		}

		displayLenDeviation := token.Pos(len(csDisplayTypeName) - len(goDisplayTypeName))
		typeLenDeviation := token.Pos(len(csFullTypeName) - len(goFullTypeName))

		var arrayInitializer string

		if arrayType, ok := field.Type.(*ast.ArrayType); ok {
			if arrayType.Len != nil {
				arrayInitializer = fmt.Sprintf(" = new(%s)", v.arrayZeroValueArgs(v.convExpr(arrayType.Len, nil), fieldType))
			}
		}

		if field.Names == nil {
			// Check for promoted fields
			var ident *ast.Ident
			var ok bool

			var isIdentFieldType bool
			var selectorType bool

			// A GENERIC embed (`node[K, V]` — internal/concurrent's entry) arrives as an
			// IndexExpr/IndexListExpr over the base type expression; unwrap it (in both the
			// plain and pointer forms below) so the embed emits — it was silently DROPPED
			// (the struct lost the field entirely, every promoted access CS0117).
			unwrapGeneric := func(expr ast.Expr) ast.Expr {
				switch index := expr.(type) {
				case *ast.IndexExpr:
					return index.X
				case *ast.IndexListExpr:
					return index.X
				}

				return expr
			}

			if ident, ok = unwrapGeneric(field.Type).(*ast.Ident); ok {
				isIdentFieldType = true
			} else if ptrType, ok := field.Type.(*ast.StarExpr); ok {
				if ident, ok = unwrapGeneric(ptrType.X).(*ast.Ident); ok {
					isIdentFieldType = true
				}
			}

			if !isIdentFieldType {
				if selectorExpr, ok := unwrapGeneric(field.Type).(*ast.SelectorExpr); ok {
					if ident, ok = selectorExpr.X.(*ast.Ident); ok {
						isIdentFieldType = true
						selectorType = true
					}
				} else if ptrType, ok := field.Type.(*ast.StarExpr); ok {
					if selectorExpr, ok := unwrapGeneric(ptrType.X).(*ast.SelectorExpr); ok {
						if ident, ok = selectorExpr.X.(*ast.Ident); ok {
							isIdentFieldType = true
							selectorType = true
						}
					}
				}
			}

			if !isIdentFieldType {
				continue
			}

			// A generic embed's MEMBER NAME is the base type name (Go promotes entry[K,V]'s
			// embedded node[K,V] through the selector `.node`), so strip the type arguments —
			// and do it BEFORE the selector dot-strip: the arguments may contain qualified
			// types whose dots otherwise win the LastIndex (uniqueMap's
			// `*concurrent.HashTrieMap[T, weak.Pointer[T]]` named its member `Pointer`).
			if bracketIndex := strings.Index(goTypeName, "["); bracketIndex != -1 {
				goTypeName = goTypeName[:bracketIndex]
			}

			// An embedded field's NAME is the UNQUALIFIED type name (Go spec), so strip any package
			// qualifier. A selector embed (`io.Writer`) carries it explicitly; a DOT-IMPORTED ident
			// embed does too once resolved — io_test's `import . "io"` + embedded `ReaderFrom` reaches
			// here as a bare *ast.Ident whose getAliasQualifiedTypeName still renders the (collision-renamed)
			// package qualifier `Δio.ReaderFrom`. Gating the strip on selectorType left that qualifier
			// in the field name (`Δio.ReaderFrom`), whose dot is a C# syntax error (CS1003/CS1026).
			// Strip whenever a qualifier survives, covering both forms; a same-package embed has no
			// dot, so this is a no-op there (byte-identical).
			if dotIndex := strings.LastIndex(goTypeName, "."); dotIndex != -1 {
				// Get the unqualified name of the embedded type
				goTypeName = goTypeName[dotIndex+1:]
			}

			// Lookup identity to determine if it's an interface — for a SELECTOR embed
			// (io.Writer) resolve the SEL, not the package ident: a cross-package
			// INTERFACE embed otherwise took the promoted-STRUCT property form, and the
			// generator tried to construct the interface (archive/tar's lifted
			// `struct{ io.Writer }`, CS0144 ×8 + CS1929 ×4).
			identObj := v.info.ObjectOf(ident)

			if selectorType {
				if selectorExpr, ok := unwrapGeneric(field.Type).(*ast.SelectorExpr); ok {
					identObj = v.info.ObjectOf(selectorExpr.Sel)
				} else if ptrType, ok := field.Type.(*ast.StarExpr); ok {
					if selectorExpr, ok := unwrapGeneric(ptrType.X).(*ast.SelectorExpr); ok {
						identObj = v.info.ObjectOf(selectorExpr.Sel)
					}
				}
			}

			if identObj == nil {
				continue // Could not find the object of ident
			}

			identType := identObj.Type().Underlying()

			// An EMBEDDED field's member name is the unqualified type name (Go spec), so it can
			// equal the ENCLOSING struct's own name — io_test.go's `type Buffer struct{
			// bytes.Buffer }` derives the member `Buffer` inside struct `Buffer`, which C# forbids
			// (CS0542). Apply the same disambiguation marker the NAMED-field path below uses: the
			// ACCESS sites already emit the renamed form (structFieldBoxName / convIdent run
			// typeCollidingFieldName for any field whose name equals its enclosing type, embedded
			// or not — `rb.of(Buffer.ᏑΔBuffer)`), so only this declaration was out of step.
			// Both sides compare RAW, mirroring the named-field compare.
			embedName := getCoreSanitizedIdentifier(goTypeName)

			if strings.TrimPrefix(embedName, "@") == strings.TrimPrefix(strings.TrimPrefix(structTypeName, ShadowVarMarker), "@") {
				embedName = typeCollidingFieldName(embedName)
			}

			if _, ok := identType.(*types.Interface); ok {
				// Add to promoted interface implementations
				packageLock.Lock()

				if promotions, exists := promotedInterfaceImplementations[csFullTypeName]; exists {
					promotions.Add(structTypeName)
				} else {
					promotedInterfaceImplementations[csFullTypeName] = NewHashSet([]string{structTypeName})
				}

				packageLock.Unlock()

				v.writeString(target, "%s %s %s;", getAccess(goTypeName), csEmitTypeName, embedName)
			} else {
				var handled bool

				if _, ok := identObj.(*types.PkgName); !ok {
					if ptrType, ok := identType.(*types.Pointer); ok {
						if _, ok = ptrType.Elem().(*types.Named); !ok {
							v.writeString(target, "%s %s %s;", getAccess(goTypeName), csEmitTypeName, embedName)
							handled = true
						}
					} else if _, ok = identType.(*types.Struct); !ok {
						if _, ok := identObj.Type().(*types.Named); !ok {
							v.writeString(target, "%s %s %s;", getAccess(goTypeName), csEmitTypeName, embedName)
							handled = true
						}
					}
				}

				// Handle promoted struct implementations
				if !handled {
					v.writeString(target, "%s partial ref %s %s { get; }", getAccess(goTypeName), csEmitTypeName, embedName)
				}
			}

			v.writeCommentString(target, field.Comment, field.Type.End()+typeLenDeviation)
			target.WriteString(v.newline)
		} else {
			// Match the Go source's line grouping for readability: when a single Go field
			// declaration groups multiple names (`x, y int`), emit one combined C# line
			// (`internal nint x, y;`). This is only safe when every name shares the same
			// access modifier and emitted type and none needs per-name special handling —
			// blank `_` (renamed per occurrence), a name colliding with the struct type
			// (Δ-marker rename), or a per-field array initializer (` = new(N)`). The names in
			// one field group already share field.Type/Tag/Comment, so only access and the
			// per-name renames can diverge. When any apply, fall back to one line per name.
			canCombine := len(field.Names) > 1 && arrayInitializer == ""

			if canCombine {
				groupAccess := getAccess(field.Names[0].Name)

				for _, ident := range field.Names {
					fieldName := getCoreSanitizedIdentifier(ident.Name)

					if fieldName == "_" || fieldName == structTypeName || getAccess(ident.Name) != groupAccess {
						canCombine = false
						break
					}
				}
			}

			if canCombine {
				fieldNames := make([]string, len(field.Names))

				for i, ident := range field.Names {
					fieldNames[i] = getCoreSanitizedIdentifier(ident.Name)
				}

				v.writeString(target, "%s %s %s;", getAccess(field.Names[0].Name), csDisplayTypeName, strings.Join(fieldNames, ", "))
				v.writeCommentString(target, field.Comment, field.Type.End()+displayLenDeviation)
				target.WriteString(v.newline)
			} else {
				for _, ident := range field.Names {
					fieldName := getCoreSanitizedIdentifier(ident.Name)

					if fieldName == "_" {
						for range prevNameDiscardedCount {
							fieldName = fieldName + "_"
						}

						prevNameDiscardedCount++
					} else if strings.TrimPrefix(fieldName, "@") == strings.TrimPrefix(strings.TrimPrefix(structTypeName, ShadowVarMarker), "@") {
						// C# forbids a member sharing its enclosing type's name (CS0542), so rename a
						// field whose name equals the struct type with the disambiguation marker. Field
						// accesses are renamed to match (see convSelectorExpr / convIdent). Both sides
						// compare RAW (escape/rename markers stripped): net parse.go's `type file
						// struct{ file *os.File }` renames the TYPE to Δfile (CS9056) and escapes the
						// FIELD to @file — the literal compare missed, declaring `@file` while every
						// access site emitted `Δfile` (CS1061 ×3).
						fieldName = typeCollidingFieldName(fieldName)
					}

					v.writeString(target, "%s %s %s%s;", getAccess(ident.Name), csDisplayTypeName, fieldName, arrayInitializer)
					v.writeCommentString(target, field.Comment, field.Type.End()+displayLenDeviation)
					target.WriteString(v.newline)
				}
			}
		}
	}

	v.indentLevel--
	v.writeStringLn(target, "}")

	if structPrefix == nil {
		v.replaceMarkerString(target, structPrefixMarker, "")
	} else {
		v.replaceMarkerString(target, structPrefixMarker, structPrefix.String())
	}

	if lifted && v.inFunction {
		if v.currentFuncPrefix.Len() > 0 {
			v.currentFuncPrefix.WriteString(v.newline)
		}

		v.currentFuncPrefix.WriteString(target.String())
		target.Reset()
		v.indentLevel = preLiftIndentLevel
	}

	return
}

// structHasPromotedEmbeds reports whether the type's underlying struct carries at least one
// embedded field that the generated C# stores in a constructor-initialized readonly `ж<T>` box
// (the StructTypeTemplate "Promoted Struct References"). A `default`-valued instance of such a
// struct has null boxes, so the first promoted-member access throws NullReferenceException —
// an uninitialized declaration must render `new T(nil)` instead of `default!`. The decision
// mirrors the embedded-field emission above: an embed renders as a `partial ref` promotion
// (and thus a box) unless it is a same-package interface, a builtin non-named embed (`int`),
// or a pointer to a non-named type; a CROSS-PACKAGE embed always takes the promotion path
// (the selector-type branch above bypasses every plain-field case, interfaces included).
func (v *Visitor) structHasPromotedEmbeds(t types.Type) bool {
	if t == nil {
		return false
	}

	st, ok := t.Underlying().(*types.Struct)

	if !ok {
		return false
	}

	for i := range st.NumFields() {
		field := st.Field(i)

		if !field.Anonymous() {
			continue
		}

		fieldType := field.Type()

		// Resolve the embed's named type, through one syntactic pointer (`*X`).
		named, _ := types.Unalias(fieldType).(*types.Named)

		if named == nil {
			if ptr, isPtr := fieldType.(*types.Pointer); isPtr {
				named, _ = types.Unalias(ptr.Elem()).(*types.Named)
			}
		}

		// A cross-package embed always renders as a promoted box.
		if named != nil && named.Obj().Pkg() != nil && named.Obj().Pkg() != v.pkg {
			return true
		}

		// Same-package `*X` embed: any named pointee promotes (struct underlying and named
		// non-struct both take the partial-ref path); `*int` (builtin pointee) stays plain.
		if ptr, isPtr := fieldType.(*types.Pointer); isPtr {
			if _, isNamed := types.Unalias(ptr.Elem()).(*types.Named); isNamed {
				return true
			}

			continue
		}

		underlying := fieldType.Underlying()

		// A same-package interface embed renders as a plain interface field — no box.
		if _, isInterface := underlying.(*types.Interface); isInterface {
			continue
		}

		// A named-pointer-type embed (`type P *T`) promotes only when the pointee is named.
		if ptr, isPtr := underlying.(*types.Pointer); isPtr {
			if _, isNamed := types.Unalias(ptr.Elem()).(*types.Named); isNamed {
				return true
			}

			continue
		}

		// A value embed promotes when its underlying is a struct or the embed itself is a
		// named type (`type RCode int` embeds as a partial-ref box despite the basic core).
		if _, isStruct := underlying.(*types.Struct); isStruct {
			return true
		}

		if named != nil {
			return true
		}
	}

	return false
}

// structZeroValueNeedsConstruction reports whether a struct type's zero value default(T) is
// BROKEN — it has a promoted-embed box (constructor-allocated) or a fixed-size array field
// (`= new(N)` field initializer that default(T) skips), directly or through a nested value-struct
// field — so `var z T` must run the generated parameterless constructor (`new()`) rather than
// emit `default!`. Mirrors go2cs-gen StructTypeTemplate.NeedsConstruction; a false result keeps the
// existing `default!`/bare emission. The top-level promoted-embed case is routed to `new(nil)` by
// the caller's earlier structHasPromotedEmbeds check — this predicate still recurses for it so a
// NESTED field whose own type carries a promoted embed (or array) also constructs.
func (v *Visitor) structZeroValueNeedsConstruction(t types.Type) bool {
	return v.structZeroValueNeedsConstructionRec(t, map[*types.Struct]bool{})
}

func (v *Visitor) structZeroValueNeedsConstructionRec(t types.Type, seen map[*types.Struct]bool) bool {
	if t == nil {
		return false
	}

	st, ok := t.Underlying().(*types.Struct)

	if !ok {
		return false
	}

	// Go forbids value-type embedding cycles (infinite size), so a cycle cannot actually occur —
	// the guard is purely defensive.
	if seen[st] {
		return false
	}

	seen[st] = true

	// Any promoted embed surfaces as a constructor-allocated `ж<T>` box — default leaves it null.
	if v.structHasPromotedEmbeds(t) {
		return true
	}

	for i := range st.NumFields() {
		field := st.Field(i)

		if field.Name() == "_" {
			continue
		}

		fieldType := field.Type()

		// A reference field keeps its nil zero value (correct — a nil pointer/slice/map/chan/func
		// matches Go), so it never forces construction; skipping it also stops the recursion from
		// descending through a self-referential pointer field.
		if isInherentlyHeapAllocatedType(fieldType) {
			continue
		}

		// A fixed-size array field (`[N]T` → golib array<T>) carries a `= new(N)` field initializer
		// that default(T) skips, leaving a null backing.
		if _, isArray := fieldType.Underlying().(*types.Array); isArray {
			return true
		}

		// A nested value-struct field whose own type needs construction.
		if v.structZeroValueNeedsConstructionRec(fieldType, seen) {
			return true
		}
	}

	return false
}
