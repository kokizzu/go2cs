// typeNameResolution.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns TYPE-NAME RENDERING: given a Go type, produce the string to write into C#.
//
// There are six of these and choosing the wrong one is a silent correctness bug the compiler
// cannot catch, so each name states WHO CONSUMES its output — which is the whole taxonomy:
//
//	getAliasQualifiedTypeName   the workhorse. Go-shaped name (`[]atomic.Int32`, `*Reader`) qualified
//	                            with whatever file-local package alias is in scope. Emitted into a
//	                            converted BODY, where visitFile has supplied the matching `using`.
//	                            Most-called by far, and the base every renderer below builds on.
//	getFullyQualifiedTypeName   the same Go-shaped name in its alias-free form
//	                            (`sync.atomic_package.Int32`). REQUIRED for anything a GENERATOR
//	                            consumes — GoType attribute strings and package_info records live in
//	                            files that carry no `using` directives, so an alias resolves to
//	                            nothing there.
//	getScopeCheckedTypeName     the alias-qualified form when every package it names is actually
//	                            imported by this file, and the fully-qualified form when one is not.
//	                            That check is the difference: it is the only renderer that GUARANTEES
//	                            its output resolves in the file receiving it. Body emission only —
//	                            never generator-consumed strings.
//	getCSharpTypeName           C#-shaped rather than Go-shaped: the Go->C# name mapping applied on
//	                            top of the alias-qualified form (`slice<byte>`, not `[]byte`). Reach
//	                            for it wherever the string lands in a C# TYPE position — a
//	                            declaration, an element type, a generic argument.
//	getExpressionTypeName       convenience over the workhorse: resolve an EXPRESSION to its type,
//	                            then name it.
//	getRefParameterTypeName     the C#-shaped spelling a `ref` parameter position needs — a pointer
//	                            renders as `ref T` rather than as a box.
//
// The rule of thumb: if the string lands in generated C# that a human reads, prefer the alias-
// qualified forms; if it lands in metadata a tool parses, use getFullyQualifiedTypeName.
//
// The alias plumbing (getAliasedTypeName, foreignAliasedTypeName, markDerivedTypeAliasUsed) lives
// here too, since it is what makes the short forms legal.

package main

import (
	"fmt"
	"go/ast"
	"go/types"
	"strings"
)

// getExpressionTypeName resolves expr to its type and names it with getAliasQualifiedTypeName —
// the convenience spelling for the common "I have an expression, I need its type name" caller.
//
// It carries one side effect the bare renderer cannot: a channel whose ELEMENT is an anonymous
// struct or interface must have that element lifted to a named declaration before anything can
// refer to it, so the lift happens here, once, the first time such a channel type is named.
func (v *Visitor) getExpressionTypeName(expr ast.Expr, underlying bool) string {

	if chanType, ok := expr.(*ast.ChanType); ok {
		// Check if the channel value is an anonymous struct
		if structType, exprType := v.extractStructType(chanType.Value); structType != nil && !v.liftedTypeExists(structType) {
			v.indentLevel++
			v.visitStructType(structType, exprType, "channel", nil, true, nil)
			v.indentLevel--
		}

		// Check if the channel value is an anonymous interface
		if interfaceType, exprType := v.extractInterfaceType(chanType.Value); interfaceType != nil && !v.liftedTypeExists(interfaceType) {
			v.indentLevel++
			v.visitInterfaceType(interfaceType, exprType, "channel", nil, true, nil)
			v.indentLevel--
		}
	}

	return v.getAliasQualifiedTypeName(v.getType(expr, underlying), underlying)
}

// collectTypePackages records the import paths of every foreign (non-current-package) named type
// reachable in t — directly, or as a pointer/slice/array/map/chan element, a generic type argument,
// or a func-signature parameter/result — into referencedForeignPackages, plus the pseudo-path
// "unsafe" for an unsafe.Pointer basic. These are the packages whose short-alias type names
// (`pkg.Type`, `@unsafe.Pointer`) the converter emits, so visitFile can supply the matching
// `using <alias> = <namespace>;`. A named type is recorded by its own package only — its underlying
// fields belong to its own declaration and are not emitted inline, so the walk does not recurse into
// Underlying (which also avoids cycles on recursive struct types).
func (v *Visitor) collectTypePackages(t types.Type, seen map[types.Type]bool) {
	if t == nil {
		return
	}

	if seen == nil {
		seen = map[types.Type]bool{}
	}

	if seen[t] {
		return
	}

	seen[t] = true

	switch u := t.(type) {
	case *types.Basic:
		if u.Kind() == types.UnsafePointer {
			v.referencedForeignPackages.Add("unsafe")
		}
	case *types.Named:
		if obj := u.Obj(); obj != nil {
			if pkg := obj.Pkg(); pkg != nil && pkg != v.pkg {
				v.referencedForeignPackages.Add(pkg.Path())
			}
		}

		if typeArgs := u.TypeArgs(); typeArgs != nil {
			for i := range typeArgs.Len() {
				v.collectTypePackages(typeArgs.At(i), seen)
			}
		}
	case *types.Pointer:
		v.collectTypePackages(u.Elem(), seen)
	case *types.Slice:
		v.collectTypePackages(u.Elem(), seen)
	case *types.Array:
		v.collectTypePackages(u.Elem(), seen)
	case *types.Map:
		v.collectTypePackages(u.Key(), seen)
		v.collectTypePackages(u.Elem(), seen)
	case *types.Chan:
		v.collectTypePackages(u.Elem(), seen)
	case *types.Signature:
		if params := u.Params(); params != nil {
			for i := range params.Len() {
				v.collectTypePackages(params.At(i).Type(), seen)
			}
		}

		if results := u.Results(); results != nil {
			for i := range results.Len() {
				v.collectTypePackages(results.At(i).Type(), seen)
			}
		}
	}
}

// methodlessNamedFuncSignature reports the underlying *types.Signature when t is a NON-GENERIC
// named func type with NO methods — `type releaseConn func(error)`, `context.CancelFunc`. Go treats
// such a type as freely interconvertible with its underlying `func(...)` (the name is purely
// documentary when there are no methods), but the converter would otherwise emit a distinct C#
// delegate (`ΔreleaseConn`) incompatible with the base `Action<error>` its underlying renders to —
// so a value flowing between the two (database/sql's grabConn returns `releaseConn`, queryDC takes
// `func(error)`) fails (CS1503/CS0029, and the mismatch blocks the ж-receiver overload → CS1929).
// Such a type is rendered AS its base delegate everywhere and its declaration is skipped
// (visitFuncType), making the conversions identity, exactly as Go models them. A named func type
// WITH methods keeps its distinct delegate (its method set is meaningful); a GENERIC one keeps its
// name (it is referenced as `Seq<V>`, and the type parameter must stay in scope).
func methodlessNamedFuncSignature(t types.Type) (*types.Signature, bool) {
	named, ok := types.Unalias(t).(*types.Named)

	if !ok {
		return nil, false
	}

	if named.NumMethods() != 0 || named.TypeParams() != nil {
		return nil, false
	}

	sig, ok := named.Underlying().(*types.Signature)

	if !ok {
		return nil, false
	}

	// Don't collapse when the signature references ANY named func type (its own or another) in
	// its params/results. A SELF-referential func type — `type stateFn func(*machine) stateFn`
	// (a Go state machine) — has no finite base-delegate form (`Func<M, Func<M, …>>` is infinite),
	// and a reference to ANOTHER named func type would leave that name undefined after collapse
	// (FirstClassFunctions' `strategy func(score) action`). Keeping such a type as a named
	// delegate is correct and self-consistent; only the leaves of the func-type reference graph
	// (whose signatures name no func types — database/sql's `releaseConn func(error)`, context's
	// `CancelFunc func()`) collapse.
	if signatureReferencesNamedFuncType(sig) {
		return nil, false
	}

	return sig, true
}

// signatureReferencesNamedFuncType reports whether any param/result of sig is (or, through
// pointer/slice/array/map/chan wrappers, contains) a NAMED type whose underlying is a func
// signature — i.e. a named func type. Used to keep self- or mutually-referential func types as
// named delegates (see methodlessNamedFuncSignature). Struct fields are not descended into (a
// struct-typed param does not make the delegate recursive).
func signatureReferencesNamedFuncType(sig *types.Signature) bool {
	var referencesFunc func(t types.Type) bool

	referencesFunc = func(t types.Type) bool {
		switch typ := t.(type) {
		case *types.Named:
			if _, ok := typ.Underlying().(*types.Signature); ok {
				return true
			}
		case *types.Pointer:
			return referencesFunc(typ.Elem())
		case *types.Slice:
			return referencesFunc(typ.Elem())
		case *types.Array:
			return referencesFunc(typ.Elem())
		case *types.Map:
			return referencesFunc(typ.Key()) || referencesFunc(typ.Elem())
		case *types.Chan:
			return referencesFunc(typ.Elem())
		}

		return false
	}

	tuples := []*types.Tuple{sig.Params(), sig.Results()}

	for _, tuple := range tuples {
		if tuple == nil {
			continue
		}

		for i := range tuple.Len() {
			if referencesFunc(tuple.At(i).Type()) {
				return true
			}
		}
	}

	return false
}

// getAliasQualifiedTypeName renders t as the Go-shaped type name to write into a converted body,
// qualifying foreign types with the file-local package alias (`atomic.Int32`, `[]time.Duration`).
// It is the base every other renderer in this file builds on, and the most-called of the six.
//
// "Alias-qualified" is a promise about the FORM, not about scope: the alias is emitted whether or
// not this file imports the package, because collectTypePackages records every foreign package
// touched here and visitFile then supplies the matching `using <alias> = <namespace>;`. A caller
// that cannot rely on that plumbing — because it is emitting into a generated file with no usings —
// wants getFullyQualifiedTypeName; a caller that wants the short form only when it is provably
// legal wants getScopeCheckedTypeName.
//
// isUnderlying asks for a named type's underlying representation instead of its own name.
func (v *Visitor) getAliasQualifiedTypeName(t types.Type, isUnderlying bool) string {
	if t == nil {
		return ""
	}

	// A non-generic methodless named func type renders as its base C# delegate (its underlying
	// signature), matching Go's free named↔underlying func interconversion (see
	// methodlessNamedFuncSignature). Guarded so a pointer/composite wrapper still recurses here.
	if sig, ok := methodlessNamedFuncSignature(t); ok {
		v.collectTypePackages(t, nil)
		return v.getAliasQualifiedTypeName(sig, isUnderlying)
	}

	// Register any foreign package whose type is emitted here so visitFile can supply the file-local
	// `using <alias> = <namespace>;` even when the file did not import the package under its canonical
	// name (inferred type, blank/non-canonical alias import) — see the field comment. Walks composites
	// and generics so an element/argument type (`[]time.Duration`, `map[K]abi.Kind`, unsafe.Pointer
	// inside a slice) registers too, since those are emitted through the string path below without
	// recursing into getAliasQualifiedTypeName.
	v.collectTypePackages(t, nil)

	if pointer, ok := t.(*types.Pointer); ok {
		return "*" + v.getAliasQualifiedTypeName(pointer.Elem(), isUnderlying)
	}

	// A type parameter whose constraint type-set is a single non-tilde pointer term (`[P *T]`)
	// is ERASED: the singleton type set makes P definitionally *T at every instantiation, so it
	// renders as the pointer type itself and the whole existing pointer rendering chain applies
	// (its element may itself be another type parameter, which renders by name). Gated on the
	// CURRENT function's identity set (typeParamErased) so a declined declaration's parameter —
	// a generic named type's, a receiver type parameter — keeps its bare name coherently rather
	// than half-erasing. Non-erased type parameters keep the t.String() fallthrough.
	if typeParam, ok := t.(*types.TypeParam); ok {
		if pointer, ok := v.typeParamErased(typeParam); ok {
			return "*" + v.getAliasQualifiedTypeName(pointer.Elem(), isUnderlying)
		}
	}

	// A FOREIGN type ALIAS whose TARGET lives in yet ANOTHER package — `os.FileInfo = fs.FileInfo`
	// (os/types.go, target in io/fs) — is emitted as an assembly-scoped `global using FileInfo =
	// go.io.fs_package.FileInfo;` in ITS OWN package's conversion, NOT as a member of that package's C#
	// class, so a cross-package reference `os_package.FileInfo` does not resolve (CS0426, path/filepath's
	// `os.Lstat` func value). Render the alias's TARGET instead. Gated to a DIFFERENT-package target: an
	// alias to a SAME-package type (`CrossPkgLib.Temperature = Celsius`) already resolves through the
	// existing `ꓸ` global-using alias, so it is left untouched (no churn on that mechanism).
	if alias, ok := t.(*types.Alias); ok {
		if aliasObj := alias.Obj(); aliasObj != nil && aliasObj.Pkg() != nil && aliasObj.Pkg() != v.pkg {
			if targetNamed, ok := types.Unalias(t).(*types.Named); ok {
				if targetObj := targetNamed.Obj(); targetObj != nil && targetObj.Pkg() != nil && targetObj.Pkg() != aliasObj.Pkg() {
					return v.getAliasQualifiedTypeName(targetNamed, isUnderlying)
				}
			}
		}
	}

	if name, ok := v.liftedTypeMap[t]; ok {
		return name
	}

	// An array/slice type is rendered structurally — the `[N]`/`[]` marker plus the recursively
	// resolved element — rather than via t.String() below (mirrors getFullyQualifiedTypeName). t.String()
	// yields a path-qualified string (`[]*internal/abi.Type`) whose cross-package last-segment
	// slash-strip eats everything before the slash INCLUDING a pointer marker, so the element's
	// `*` is silently dropped (`slice<abi.Type>` instead of `slice<ж<abi.Type>>` — reflect
	// CS1503 ×16, plus SILENTLY WRONG type asserts that compiled). Recursing on the element also
	// resolves a lifted element and a cross-package generic element through their own arms.
	switch composite := t.(type) {
	case *types.Array:
		return fmt.Sprintf("[%d]%s", composite.Len(), v.getAliasQualifiedTypeName(composite.Elem(), isUnderlying))
	case *types.Slice:
		return "[]" + v.getAliasQualifiedTypeName(composite.Elem(), isUnderlying)
	case *types.Chan:
		// Structural like slices/arrays so a LIFTED element resolves - `make(chan dialResult)`
		// where dialResult is a FUNCTION-LOCAL type (net dialParallel) rendered
		// `channel<dialResult>` while every composite used the lifted name (CS0246).
		elem := v.getAliasQualifiedTypeName(composite.Elem(), isUnderlying)

		switch composite.Dir() {
		case types.RecvOnly:
			return "<-chan " + elem
		case types.SendOnly:
			return "chan<- " + elem
		default:
			return "chan " + elem
		}
	case *types.Map:
		// Structural like slices/chans — the t.String() path renders a PACKAGE-LOCAL value
		// type path-qualified (`map[chan<- os.Signal]*os/signal.handler`), whose cross-package
		// slash-strip eats everything before the slash including the map header (os/signal's
		// handlers.m emitted `map<channel/*<-*/<os.Signal>*handler>, >` — CS1003 cascade ×8).
		return fmt.Sprintf("map[%s]%s", v.getAliasQualifiedTypeName(composite.Key(), isUnderlying), v.getAliasQualifiedTypeName(composite.Elem(), isUnderlying))
	case *types.Signature:
		// Structural like the composites above: t.String() embeds slash-qualified import paths
		// for cross-package elements (`func(...*go/types.Package...)`), which the string-path
		// slash heuristics mis-qualify three different ways (go/importer's gccgo importer field,
		// net/http's TLSNextProto maps, traceviewer's MutatorUtilFunc — CS0234). Rendering each
		// element recursively keeps the file's short import-alias form (see signatureTypeName).
		return v.signatureTypeName(composite, isUnderlying)
	}

	// The internal white-box variant contains production and test declarations in one Go package,
	// but emits them into separate C# classes. Qualify a production-declared type through the
	// referenced production class before the ordinary same-package branch can erase its owner.
	if named, ok := t.(*types.Named); ok {
		if bridgeName, isBridge := v.whiteboxBridgeNamedType(named); isBridge {
			return bridgeName
		}
		if productionName, isProduction := v.whiteboxProductionNamedType(named); isProduction {
			return productionName
		}
	}

	// A cross-package INSTANTIATED generic (e.g. `internal/runtime/atomic.Pointer[func(string,
	// string)]`) must be rendered structurally — `pkg.Name() + "." + Name[args…]` with each arg
	// recursively named — rather than from t.String(). The string form keeps the full import path,
	// and the slash-strip that would reduce it is skipped whenever the string contains '(' (to
	// protect func types), so a func-type type-argument leaves the full path AND the pkg.Name()
	// alias gets prepended → a doubled `atomic.@internal.runtime.atomic.Pointer` (CS0426).
	if named, ok := t.(*types.Named); ok {
		obj := named.Obj()

		if typeArgs := named.TypeArgs(); typeArgs != nil && typeArgs.Len() > 0 {
			args := make([]string, typeArgs.Len())

			for i := 0; i < typeArgs.Len(); i++ {
				if proxyName, ok := v.constraintProxyArg(named, i); ok {
					args[i] = proxyName
				} else {
					args[i] = v.getAliasQualifiedTypeName(typeArgs.At(i), false)
				}
			}

			if pkg := obj.Pkg(); pkg != nil && pkg != v.pkg {
				return fmt.Sprintf("%s.%s[%s]", importQualifier(pkg.Name()), obj.Name(), strings.Join(args, ", "))
			}

			// A SAME-PACKAGE instantiated generic must ALSO render structurally — each type ARGUMENT
			// recursively named — rather than falling through to the t.String() path. When an argument
			// is itself cross-package, t.String() path-qualifies it (`curve[*repro/sub.Item]`), and the
			// cross-package slash-strip then eats everything before the slash INCLUDING the `curve[`
			// header, dropping the wrapper (crypto/elliptic's `*nistCurve[*nistec.P224Point]` →
			// `ж<nistec.P224Point>>`, a CS1519 cascade). Rendering args via getAliasQualifiedTypeName yields their
			// short, slash-free package-qualified names, so the header survives.
			return fmt.Sprintf("%s[%s]", obj.Name(), strings.Join(args, ", "))
		}
	}

	var pkgPrefix string
	var plainPkgPrefix string
	var foreignPathPrefix string

	if named, ok := t.(*types.Named); ok {
		obj := named.Obj()
		pkg := obj.Pkg()

		// Handle builtin types with no package
		if pkg != nil && pkg != v.pkg {
			// Prefer THIS FILE's actual import alias for the type's package over the canonical
			// package name — cryptobyte's asn1.go imports `encoding/asn1` as `encoding_asn1`
			// (the vendored `.../cryptobyte/asn1` subpackage took the canonical `asn1`), so a
			// `*asn1.BitString` must render `encoding_asn1.BitString`, not `asn1.BitString` (which
			// resolves to the subpackage — CS0426). Only EXPLICITLY-aliased imports populate the map;
			// unaliased/Δ-renamed imports are absent and keep the importQualifier fallback — no churn.
			aliasQualifier := importQualifier(pkg.Name())

			if fileAlias, ok := v.importPathAliases[pkg.Path()]; ok && fileAlias != "" {
				aliasQualifier = fileAlias
			}

			pkgPrefix = aliasQualifier + "."
			plainPkgPrefix = pkg.Name() + "."
			foreignPathPrefix = pkg.Path() + "."
		}
	}

	// A NON-EMPTY anonymous struct/interface reaching this fallback was lifted by its
	// DECLARING file's visitor (this visitor's liftedTypeMap missed above — a cross-file
	// reference, or a same-file use ABOVE the declaration): resolve through the shared
	// package registry, or defer with a marker resolved after the file-visit barrier.
	// The raw t.String() fall-through below is never valid C# for these (B8: bytes'
	// `range compareTests` from bytes_test.go emitted `struct{a <>byte; …}` — CS1526
	// + ~170-error parser cascade).
	if !isUnderlying {
		if name := deferredDynamicTypeName(t); name != "" {
			return name
		}
	}

	typeName := strings.ReplaceAll(t.String(), "..", "")
	packagePathPrefix := v.pkg.Path() + "."

	// Remove the current package's path prefix from the type name. Use ReplaceAll, not a
	// single replace: a composite type (e.g. map[K]V) can name two current-package types, and
	// stripping only the first leaves a self-qualified one (which then also trips the slash
	// handling below for slash-bearing package paths like internal/platform → CS0246).
	typeName = strings.ReplaceAll(typeName, packagePathPrefix, "")

	// A FOREIGN named type's t.String() carries its full import-PATH qualifier; reduce it to the
	// package NAME here, because the generic last-segment slash-strip below assumes the path's
	// last segment IS the package qualifier. When the two differ — a major-version path tail —
	// the strip leaves the version segment behind as a phantom qualifier (`math/rand/v2.Rand` →
	// `v2.Rand`) and the alias prepend below then doubles it into `rand.v2.Rand` (`v2` read as a
	// member of class rand_package — CS0426, sort's test suite). An equal-name path reduces to
	// exactly what the slash-strip produced, so every existing emission is unchanged.
	if len(foreignPathPrefix) > 0 && strings.HasPrefix(typeName, foreignPathPrefix) {
		typeName = plainPkgPrefix + typeName[len(foreignPathPrefix):]
	}

	// The slash-strip below reduces a remaining cross-package import path to its last segment
	// (`internal/platform.Foo` → `platform.Foo`). It must NOT touch a composite type string whose
	// slashes are inside it — e.g. a func type `func(go2cs/x/sub.Record)` would be butchered to
	// `sub.Record)`. Such strings are converted structurally downstream (convertToCSFunc... +
	// convertImportPathToNamespace handle their inner package paths), so skip the strip for them.
	if !strings.HasPrefix(typeName, "func") && !strings.Contains(typeName, "(") {
		slashIndex := strings.LastIndex(typeName, "/")

		if slashIndex != -1 {
			typeName = typeName[slashIndex+1:]
		}
	}

	if len(pkgPrefix) > 0 && !strings.HasPrefix(typeName, pkgPrefix) {
		// A Δ-renamed import alias (importQualifier) diverges from the PLAIN package name that
		// t.String() carries (`sync.Pool` under alias `Δsync`): strip the plain qualifier before
		// prepending the alias, or the name doubles — `Δsync.sync.Pool`, `'sync' does not exist
		// in the type 'sync_package'` (io/syscall CS0426 ×22). When alias == plain name the
		// HasPrefix guard above already short-circuits, so this strip only fires on renames.
		if len(plainPkgPrefix) > 0 && strings.HasPrefix(typeName, plainPkgPrefix) {
			typeName = typeName[len(plainPkgPrefix):]
		}

		return pkgPrefix + typeName
	}

	return typeName
}

// getFullyQualifiedTypeName renders t as the Go-shaped type name in its alias-free form
// (`sync.atomic_package.Int32`), so the string resolves inside `namespace go;` with no `using` in
// scope. That is what generator-consumed metadata needs — GoType attribute strings and
// package_info records land in files the converter emits without any import aliases, where an
// alias-qualified name names nothing.
//
// It mirrors getAliasQualifiedTypeName arm for arm (pointers, erased pointer-core type params,
// methodless named func types, lifted anonymous types, array/slice/chan composites); the two must
// flip together, since a shape one renders and the other does not is a name that exists in only
// half the emission.
func (v *Visitor) getFullyQualifiedTypeName(t types.Type, isUnderlying bool) string {
	if t == nil {
		return ""
	}

	if pointer, ok := t.(*types.Pointer); ok {
		return "*" + v.getFullyQualifiedTypeName(pointer.Elem(), isUnderlying)
	}

	// An ERASED pointer-core type parameter renders as its pointer type here too, keeping this
	// renderer in lockstep with getAliasQualifiedTypeName (the flip-together invariant: a `P` that no longer
	// exists in the emitted generic parameter list must never leak as a bare dangling name).
	if typeParam, ok := t.(*types.TypeParam); ok {
		if pointer, ok := v.typeParamErased(typeParam); ok {
			return "*" + v.getFullyQualifiedTypeName(pointer.Elem(), isUnderlying)
		}
	}

	// A non-generic methodless named func type renders as its base C# delegate (see
	// methodlessNamedFuncSignature / the getAliasQualifiedTypeName twin).
	if sig, ok := methodlessNamedFuncSignature(t); ok {
		return v.getFullyQualifiedTypeName(sig, isUnderlying)
	}

	if name, ok := v.liftedTypeMap[t]; ok {
		return name
	}

	// An array/slice type is rendered structurally — the `[N]`/`[]` marker plus the recursively
	// resolved element — rather than via t.String() below. t.String() yields a path-qualified string
	// (`[2]internal/runtime/atomic.Pointer[…]`) whose cross-package slash-strip would also strip the
	// leading `[N]` marker, dropping the array wrapper (`atomic.Pointer<…>` instead of
	// `array<atomic.Pointer<…>>`). Recursing on the element also resolves a lifted anonymous
	// struct/interface element (liftedTypeMap is keyed by the element) and a cross-package generic.
	switch composite := t.(type) {
	case *types.Array:
		return fmt.Sprintf("[%d]%s", composite.Len(), v.getFullyQualifiedTypeName(composite.Elem(), isUnderlying))
	case *types.Slice:
		return "[]" + v.getFullyQualifiedTypeName(composite.Elem(), isUnderlying)
	case *types.Chan:
		// Mirrors getAliasQualifiedTypeName's Chan arm (lifted channel elements; net dialParallel).
		elem := v.getFullyQualifiedTypeName(composite.Elem(), isUnderlying)

		switch composite.Dir() {
		case types.RecvOnly:
			return "<-chan " + elem
		case types.SendOnly:
			return "chan<- " + elem
		default:
			return "chan " + elem
		}
	}

	if named, ok := t.(*types.Named); ok {
		if bridgeName, isBridge := v.whiteboxBridgeNamedType(named); isBridge {
			return bridgeName
		}
		if productionName, isProduction := v.whiteboxProductionNamedType(named); isProduction {
			return productionName
		}

		obj := named.Obj()
		pkg := obj.Pkg()

		// Handle builtin types with no package. Compare package IDENTITY, not NAME: two DIFFERENT
		// packages can share a Go package name (html/template and text/template are both `package
		// template`), and a name-only check (`pkg.Name() != packageName`) then treats the foreign type
		// as same-package — falling through to the t.String() path whose cross-package slash-strip drops
		// both the path segment and the `_package` class (html/template's `type FuncMap = template.FuncMap`
		// emitted a `global using FuncMap = go.template.FuncMap;`, CS0234, instead of
		// `go.text.template_package.FuncMap`). getAliasQualifiedTypeName and collectCrossPackagePaths already key on
		// identity (`pkg != v.pkg`); align this path with them.
		if pkg != nil && pkg != v.pkg {
			baseName := getSanitizedImport(packageClassPath(pkg.Path(), pkg.Name())+PackageSuffix) + "." + getSanitizedImport(obj.Name())

			// Append type arguments for an instantiated cross-package generic type (e.g.
			// atomic.Pointer[Config]). The qualified-name form above omits them, whereas the
			// local fall-through path keeps them via t.String(); without this, a boxed value
			// of such a type emits `new sync.atomic_package.Pointer()` (missing <Config>).
			if typeArgs := named.TypeArgs(); typeArgs != nil && typeArgs.Len() > 0 {
				args := make([]string, typeArgs.Len())

				for i := 0; i < typeArgs.Len(); i++ {
					if proxyName, ok := v.constraintProxyArg(named, i); ok {
						args[i] = proxyName
					} else {
						args[i] = v.getFullyQualifiedTypeName(typeArgs.At(i), isUnderlying)
					}
				}

				return baseName + "[" + strings.Join(args, ", ") + "]"
			}

			return baseName
		}

		// A SAME-PACKAGE instantiated generic renders structurally too — each type ARGUMENT recursively
		// named — otherwise the t.String() fall-through below path-qualifies a cross-package argument and
		// the slash-strip eats the `Name[` header (crypto/elliptic's embedded `nistCurve[*nistec.P256Point]`
		// → `nistec.P256Point>`, a CS1519 cascade). The current package is elided, so the bare name stands.
		if typeArgs := named.TypeArgs(); typeArgs != nil && typeArgs.Len() > 0 {
			args := make([]string, typeArgs.Len())

			for i := 0; i < typeArgs.Len(); i++ {
				if proxyName, ok := v.constraintProxyArg(named, i); ok {
					args[i] = proxyName
				} else {
					args[i] = v.getFullyQualifiedTypeName(typeArgs.At(i), isUnderlying)
				}
			}

			return obj.Name() + "[" + strings.Join(args, ", ") + "]"
		}
	}

	// Cross-file/forward reference to a lifted anonymous struct/interface: shared-registry
	// name or a deferred marker, never raw Go type text (see the getAliasQualifiedTypeName twin).
	if !isUnderlying {
		if name := deferredDynamicTypeName(t); name != "" {
			return name
		}
	}

	typeName := strings.ReplaceAll(t.String(), "..", "")
	packagePathPrefix := v.pkg.Path() + "."

	// Remove the current package's path prefix from the type name (ReplaceAll so a composite
	// type naming two current-package types doesn't keep a self-qualified one — see getAliasQualifiedTypeName).
	typeName = strings.ReplaceAll(typeName, packagePathPrefix, "")

	// Skip the cross-package last-segment strip for composite/func type strings whose slashes are
	// internal (e.g. `func(go2cs/x/sub.Record)`); they are converted structurally downstream.
	if !strings.HasPrefix(typeName, "func") && !strings.Contains(typeName, "(") {
		slashIndex := strings.LastIndex(typeName, "/")

		if slashIndex != -1 {
			typeName = typeName[slashIndex+1:]
		}
	}

	return typeName
}

// collectCrossPackagePaths gathers the import paths of every cross-package named type referenced
// (recursively) by t — through pointers, arrays/slices, maps, channels and generic type arguments.
// Used by getScopeCheckedTypeName to decide whether the file-local package aliases for those packages
// are all in scope.
func (v *Visitor) collectCrossPackagePaths(t types.Type, paths HashSet[string]) {
	switch tt := t.(type) {
	case *types.Pointer:
		v.collectCrossPackagePaths(tt.Elem(), paths)
	case *types.Array:
		v.collectCrossPackagePaths(tt.Elem(), paths)
	case *types.Slice:
		v.collectCrossPackagePaths(tt.Elem(), paths)
	case *types.Chan:
		v.collectCrossPackagePaths(tt.Elem(), paths)
	case *types.Map:
		v.collectCrossPackagePaths(tt.Key(), paths)
		v.collectCrossPackagePaths(tt.Elem(), paths)
	case *types.Named:
		if pkg := tt.Obj().Pkg(); pkg != nil && pkg != v.pkg {
			paths.Add(pkg.Path())
		}

		if typeArgs := tt.TypeArgs(); typeArgs != nil {
			for i := 0; i < typeArgs.Len(); i++ {
				v.collectCrossPackagePaths(typeArgs.At(i), paths)
			}
		}
	}
}

// getScopeCheckedTypeName resolves a type name for emission into the CURRENT source file's body,
// preferring the readable file-local package alias (`atomic.Int32`, via getAliasQualifiedTypeName)
// over the fully-qualified form (`sync.atomic_package.Int32`, via getFullyQualifiedTypeName) — but
// ONLY when every cross-package type it references is imported in this file, so the alias is
// guaranteed in scope. That check is the "scope-checked" in the name, and it is what separates this
// renderer from its two siblings: it is the one that guarantees its output resolves where it lands.
//
// When a referenced package is not imported here (e.g. a file indexing an atomic-typed array field
// without ever naming the element type → no `using atomic`), it falls back to the fully-qualified
// form, which resolves inside `namespace go;` without an alias. This keeps the converted C#
// visually close to the Go source while staying compilable. NOT for GoType attribute strings or
// other generator-consumed strings, which live in alias-less generated files and must always use
// getFullyQualifiedTypeName.
//
// Unlike its two siblings this takes no isUnderlying flag. Every caller wants the type AS DECLARED
// — the name a reader of the Go source would recognize; asking for a named type's underlying
// representation here would defeat the whole point of the function.
func (v *Visitor) getScopeCheckedTypeName(t types.Type) string {
	// A foreign renamed type resolves to the recorded imported-type alias (see
	// foreignAliasedTypeName). Doing it at this layer and not in getAliasQualifiedTypeName leaves
	// promoted-member naming, which reads the Go-shaped name, untouched.
	if aliased, ok := v.foreignAliasedTypeName(t); ok {
		return aliased
	}

	paths := HashSet[string]{}
	v.collectCrossPackagePaths(t, paths)

	for _, path := range paths.Keys() {
		if !v.importQueue.Contains(path) {
			return v.getFullyQualifiedTypeName(t, false)
		}
	}

	return v.getAliasQualifiedTypeName(t, false)
}

// markDerivedTypeAliasUsed records that an emitted reference resolved through a DERIVED imported
// type alias, which is what qualifies its `global using` for emission (see derivedTypeAliases). A
// parsed alias is unaffected — those are always emitted, used or not.
func markDerivedTypeAliasUsed(alias string) {
	packageLock.Lock()
	defer packageLock.Unlock()

	if derivedTypeAliases.Contains(alias) {
		usedDerivedTypeAliases.Add(alias)
	}
}

func getAliasedTypeName(typeName string) string {
	packageLock.Lock()
	alias, exists := importedTypeAliases[typeName]
	isConst := constImportedTypeAliases.Contains(typeName)
	packageLock.Unlock()

	if exists {
		if isConst {
			parts := strings.Split(typeName, ".")

			if len(parts) == 1 {
				return alias
			}

			return fmt.Sprintf("%s.%s", strings.Join(parts[:len(parts)-1], "."), alias)
		}

		// This reference resolves through the alias, so a DERIVED entry has earned its
		// `global using` declaration in this package's package_info.cs (see derivedTypeAliases).
		markDerivedTypeAliasUsed(typeName)

		return strings.ReplaceAll(typeName, ".", TypeAliasDot)
	}

	// The file-local using for this qualifier may be COLLISION-RENAMED (`using Δio =
	// io_package;` — `io` collides with the go.io CHILD NAMESPACE once io/fs is in the
	// reference closure): rewrite the qualifier so the type reference binds the renamed
	// alias (mime's `CharsetReader Func<@string, io.Reader, …>` field, CS0234 ×6).
	if qualifier, rest, found := strings.Cut(typeName, "."); found {
		if renamed, ok := packageImportAliasRenames[qualifier]; ok {
			// No recursion: a foreign type WITH its own rename already hit the alias map
			// above under the raw key; this qualifier rewrite is the final form.
			return renamed + "." + rest
		}
	}

	// A `_package`-QUALIFIED member (a same-package method/func name shadows the import's
	// using alias, so convIdent qualifies the package ident through its static class — the
	// compress/flate CS0119 fallback) must still consult the alias map by the PLAIN package
	// name: time Δ-renames const `Second` (const-vs-method collision with `Time.Second()`),
	// so the composed `time_package.Second` missed the `time.Second` entry and bound the
	// `Second(this Time)` extension method group instead (CS0019 ×2, crypto/tls's
	// `Config.time` method). On a CONST hit, keep the `_package` qualifier — only the
	// member is Δ-renamed inside the package class. Gated to consts: the const entries
	// exist only for collision-renamed members, while the TYPE entries cover every exported
	// type, whose raw `_package`-qualified renders already bind correctly.
	if lastDot := strings.LastIndex(typeName, "."); lastDot > 0 {
		qualifier, member := typeName[:lastDot], typeName[lastDot+1:]
		pkgSegment := qualifier[strings.LastIndex(qualifier, ".")+1:]

		if rootPackageName, isPkgQualified := strings.CutSuffix(pkgSegment, PackageSuffix); isPkgQualified {
			plainKey := rootPackageName + "." + member

			packageLock.Lock()
			alias, exists := importedTypeAliases[plainKey]
			isConst := constImportedTypeAliases.Contains(plainKey)
			packageLock.Unlock()

			if exists && isConst {
				return qualifier + "." + alias
			}
		}
	}

	// A Δ-renamed IMPORT qualifier (gif's `color` import arrives shadow-renamed `Δcolor`)
	// must consult the alias map by the RAW package name — the global-using alias
	// (`colorꓸRGBA = go.image.color_package.ΔRGBA`) resolves namespace-wide regardless of
	// the file-local rename; the composed `Δcolor.RGBA` missed the map and kept the RAW
	// foreign type name, which is Δ-renamed in image/color (CS0426 ×4, image/gif). A type
	// the map does NOT rename (Δcolor.Palette) keeps the renamed-import qualifier.
	if strings.Contains(typeName, ".") {
		parts := strings.SplitN(typeName, ".", 2)

		if raw, wasShadow := strings.CutPrefix(parts[0], ShadowVarMarker); wasShadow {
			if resolved := getAliasedTypeName(raw + "." + parts[1]); resolved != raw+"."+parts[1] {
				return resolved
			}
		}
	}

	return typeName
}

// getRefParameterTypeName renders t as the C#-shaped spelling a `ref` parameter position needs.
// A Go pointer parameter that the converter passes by reference is `ref T`, not the `ж<T>` box the
// ordinary C# rendering would produce — the box is the heap-allocated form, while a `ref` param is
// the caller's own storage.
func (v *Visitor) getRefParameterTypeName(t types.Type) string {
	typeName := v.getAliasQualifiedTypeName(t, false)

	if strings.HasPrefix(typeName, "*") {
		return fmt.Sprintf("ref %s", convertToCSTypeName(typeName[1:]))
	}

	return convertToCSTypeName(typeName)
}

// getCSharpTypeName renders t C#-shaped rather than Go-shaped: the alias-qualified Go name mapped
// through convertToCSTypeName, so `[]byte` arrives as `slice<byte>` and `*T` as `ж<T>`. Reach for
// it wherever the string lands in a C# TYPE position — a declaration, an element type, a generic
// argument — and for its siblings above wherever the string is Go-shaped text or tool-read metadata.
//
// Func types do NOT take the string path; they are built structurally from the signature, for the
// reasons the two guards below give.
func (v *Visitor) getCSharpTypeName(t types.Type) string {
	// Render a func type structurally as an Action/Func delegate. The string-based path mangles
	// func types whose parameter/result types carry slash-bearing package paths (the slash-strip in
	// getAliasQualifiedTypeName chops `func(*math/rand.Rand)` to `*math/rand.Rand)`), and emits Go field order for
	// a named multi-result tuple. iifeDelegateType builds it from the signature using getCSharpTypeName
	// per element (correct qualification; nameless tuple results). Simple func types render
	// identically to the old path, so this is zero-churn for them.
	// An ANONYMOUS func type (t itself is the signature) is expanded here; a NAMED func type WITH
	// methods (or generic / self-referential) keeps its distinct delegate name via the normal path.
	if sig, ok := t.(*types.Signature); ok {
		return v.iifeDelegateType(sig)
	}

	// A methodless named func type collapses to its base delegate (methodlessNamedFuncSignature);
	// render it structurally via iifeDelegateType — the same correct-qualification path the
	// anonymous signature above takes — rather than through convertToCSTypeName(getAliasQualifiedTypeName(t)).
	// getAliasQualifiedTypeName collapses it to its signature and emits the STRING form
	// (`func(map[string]*go/ast.Object) …`), whose string-path package-path conversion mangles a
	// slash-bearing cross-package element to a naive namespace form (`go.ast.Object` — no
	// `_package` class, no file alias): CS0234, and the resulting error-typed delegate then fails
	// the method-group conversion of a func passed to it (go/doc wraps `simpleImporter` for
	// `ast.NewPackage`'s `ast.Importer` param — `new Func<…go.ast.Object…>(simpleImporter)`, CS0123).
	// iifeDelegateType names each element via aliasedElementTypeName, so `ast.Object` keeps its alias.
	if sig, ok := methodlessNamedFuncSignature(t); ok {
		return v.iifeDelegateType(sig)
	}

	if aliased, ok := v.foreignAliasedTypeName(t); ok {
		return aliased
	}

	return convertToCSTypeName(v.getAliasQualifiedTypeName(t, false))
}

// foreignAliasedTypeName resolves a cross-package type that is RENAMED (or Go-aliased) inside
// its own package — syscall declares `ΔHandle` for its type-vs-method-colliding `Handle` — to
// the recorded imported-type alias (`syscallꓸHandle` = `go.syscall_package.ΔHandle`): the raw
// qualified render (`Δsyscall.Handle`) names a type that does not exist (CS0426 ×21,
// internal/poll's signatures, fields, conversion targets, and local declarations). This lives
// at the C#-NAME layers ONLY (getCSharpTypeName / getScopeCheckedTypeName / conversion targets), never
// in getAliasQualifiedTypeName: the Go-shaped name also feeds promoted-embed MEMBER naming, where the alias
// substitution renamed and rescoped the generated accessors (reflect CS8799 ×3 regression on
// the first cut). A type without a registered alias, and every generic instantiation, keeps
// the plain render (no churn).
func (v *Visitor) foreignAliasedTypeName(t types.Type) (string, bool) {
	named, ok := types.Unalias(t).(*types.Named)

	if !ok || (named.TypeArgs() != nil && named.TypeArgs().Len() > 0) {
		return "", false
	}

	pkg := named.Obj().Pkg()

	if pkg == nil || pkg == v.pkg {
		return "", false
	}

	plainKey := fmt.Sprintf("%s.%s", getSanitizedIdentifier(pkg.Name()), getCoreSanitizedIdentifier(named.Obj().Name()))

	packageLock.Lock()
	_, aliasExists := importedTypeAliases[plainKey]
	packageLock.Unlock()

	if !aliasExists {
		return "", false
	}

	return getAliasedTypeName(plainKey), true
}

// namedFuncTypeNameForSignature returns the C# delegate name of a package-level named func type whose
// underlying signature is identical to sig, or "" if none exists. Go's `:=` on a bare function value
// (`state := lexText`, where `lexText` returns the named func type `stateFn`) infers the variable's
// type as the UNNAMED signature `func(*lexer) stateFn`, not the named `stateFn` — so naming the local
// structurally emits a `Func<…>` delegate that is a DISTINCT C# type from the `stateFn` delegate the
// function group actually produces and that later `state = state(l)` assignments yield (CS0029, no
// implicit conversion between two delegate types). Recovering the named delegate lets the local take
// the single interconvertible delegate type (the classic self-referential state-machine func type).
func (v *Visitor) namedFuncTypeNameForSignature(sig *types.Signature) string {
	if sig == nil || v.pkg == nil {
		return ""
	}

	// A generic signature (type params or receiver) is never a plain named func type match.
	if sig.TypeParams() != nil || sig.RecvTypeParams() != nil || sig.Recv() != nil {
		return ""
	}

	scope := v.pkg.Scope()

	if scope == nil {
		return ""
	}

	for _, name := range scope.Names() {
		obj := scope.Lookup(name)

		typeName, ok := obj.(*types.TypeName)

		if !ok {
			continue
		}

		named, ok := typeName.Type().(*types.Named)

		if !ok || named.TypeParams() != nil {
			continue
		}

		underlyingSig, ok := named.Underlying().(*types.Signature)

		if !ok {
			continue
		}

		if types.Identical(underlyingSig, sig) {
			return getSanitizedIdentifier(named.Obj().Name())
		}
	}

	return ""
}

// exprIsMethodGroup reports whether expr is a bare reference to a function or method value (a C#
// method group), i.e. an identifier or selector whose resolved object is a *types.Func and which is
// not itself the call in a call expression. Such a value has no inferable delegate type under `var`.
func (v *Visitor) exprIsMethodGroup(expr ast.Expr) bool {
	var ident *ast.Ident

	switch e := expr.(type) {
	case *ast.Ident:
		ident = e
	case *ast.SelectorExpr:
		ident = e.Sel
	default:
		return false
	}

	if ident == nil {
		return false
	}

	_, isFunc := v.info.ObjectOf(ident).(*types.Func)

	return isFunc
}

func convertToCSTypeName(typeName string) string {
	fullTypeName := convertToCSFullTypeName(typeName)

	// If full type name starts with root namespace, remove it
	if strings.HasPrefix(fullTypeName, RootNamespace+".") {
		return fullTypeName[len(RootNamespace)+1:]
	}

	return fullTypeName
}

func convertToCSFullTypeName(typeName string) string {
	typeName = strings.TrimPrefix(typeName, "~")

	if strings.HasPrefix(typeName, "untyped ") {
		typeName = strings.TrimPrefix(typeName, "untyped ")

		if strings.HasPrefix(typeName, "int") || strings.HasPrefix(typeName, "uint") || typeName == "rune" || typeName == "byte" {
			return "UntypedInt"
		}

		if strings.HasPrefix(typeName, "float") {
			return "UntypedFloat"
		}

		if strings.HasPrefix(typeName, "complex") {
			return "UntypedComplex"
		}
	}

	if strings.Contains(typeName, "/") {
		// A package-qualified TYPE string carries a subpackage PATH plus a trailing `.TypeName` —
		// `io/fs.DirEntry`, `internal/abi.Type` (a func-type param rendered from t.String(), whose
		// import alias was lost). Converting the WHOLE thing as one import path drops the package CLASS
		// suffix and dots the type straight into the namespace (`io.fs.DirEntry`, CS0234 — `fs` is not a
		// namespace of `go.io`; the type lives in class `fs_package`). Split the trailing type off: the
		// package path ends at the first `.` AFTER the last path `/`, so `io/fs` → `io.fs_package` and
		// the `.DirEntry` (plus any `[…]` generic args) re-appends. Falls back to the whole-path form
		// when there is no trailing type (a bare import path).
		genericStart := strings.IndexByte(typeName, '[')
		scanEnd := len(typeName)

		if genericStart != -1 {
			scanEnd = genericStart
		}

		lastSlash := strings.LastIndex(typeName[:scanEnd], "/")
		dotAfterSlash := -1

		if lastSlash != -1 {
			dotAfterSlash = strings.IndexByte(typeName[lastSlash:scanEnd], '.')
		}

		if dotAfterSlash != -1 {
			splitAt := lastSlash + dotAfterSlash
			pkgPath := typeName[:splitAt]

			// Some callers hand a path whose last segment ALREADY carries the class suffix
			// (`sync/atomic_package.Uint32`, from a recorded `[GoType]` underlying); others hand the
			// raw path (`io/fs.DirEntry`, from a signature's t.String()). Only append the suffix when
			// it is not already present, or it doubles (`atomic_package_package`).
			suffix := PackageSuffix

			if strings.HasSuffix(pkgPath[lastSlash+1:], PackageSuffix) {
				suffix = ""
			}

			typeName = convertImportPathToNamespace(pkgPath, suffix) + typeName[splitAt:]
		} else {
			typeName = convertImportPathToNamespace(typeName, "")
		}
	}

	// Replace all `[` and `]` with `<` and `>` to handle generic types
	typeName = strings.ReplaceAll(typeName, "[", "<")
	typeName = strings.ReplaceAll(typeName, "]", ">")

	if strings.HasPrefix(typeName, "<>") {
		return fmt.Sprintf("%s.slice<%s>", RootNamespace, convertToCSTypeName(typeName[2:]))
	}

	if strings.HasPrefix(typeName, "chan ") {
		return fmt.Sprintf("%s.channel<%s>", RootNamespace, convertToCSTypeName(typeName[5:]))
	}

	if strings.HasPrefix(typeName, "chan<- ") {
		return fmt.Sprintf("%s.channel/*<-*/<%s>", RootNamespace, convertToCSTypeName(typeName[7:]))
	}

	if strings.HasPrefix(typeName, "<-chan ") {
		return fmt.Sprintf("%s./*<-*/channel<%s>", RootNamespace, convertToCSTypeName(typeName[7:]))
	}

	// Handle array types
	if strings.HasPrefix(typeName, "<") {
		return fmt.Sprintf("%s.array<%s>", RootNamespace, convertToCSTypeName(typeName[strings.Index(typeName, ">")+1:]))
	}

	if strings.HasPrefix(typeName, "map<") {
		innerType := typeName[4:]
		keyType, valueType := splitMapKeyValue(innerType)
		return fmt.Sprintf("%s.map<%s, %s>", RootNamespace, convertToCSTypeName(keyType), convertToCSTypeName(valueType))
	}

	// Find all types inside '<T1, T2>' type expressions and recurse into them for conversion
	if start := strings.Index(typeName, "<"); start != -1 {
		// Locate the matching closing '>' by bracket depth rather than the first '>' — the latter
		// mis-handles nested generics (e.g. Pointer<node<K, V>> would stop at the inner '>',
		// extract the unbalanced "node<K, V", and recurse into "node<K", slicing out of range).
		depth := 0
		end := -1

		for i := start; i < len(typeName); i++ {
			if typeName[i] == '<' {
				depth++
			} else if typeName[i] == '>' {
				depth--

				if depth == 0 {
					end = i
					break
				}
			}
		}

		// Only split when a matching '>' exists; otherwise the '<' is not a generic bracket (e.g.
		// the '<-' of a directional channel inside a func type) and is handled by a later branch.
		if end != -1 {
			subTypes := splitTopLevelTypes(typeName[start+1 : end])

			for i := range subTypes {
				// Trim BEFORE converting: splitTopLevelTypes keeps the ", " separator's space, so
				// later args arrive as " string". The conversion switch matches the type name
				// exactly (`case "string"`), so a leading space would miss it and fall through to
				// the default named-type path — emitting C# `string` (System.String) instead of
				// golib `@string`. That violates the generic `new()` constraint the converter adds
				// (CS0310) and breaks string-literal assignment (CS0029).
				subTypes[i] = convertToCSTypeName(strings.TrimSpace(subTypes[i]))
			}

			base := typeName[:start]

			// The type-vs-method collision Δ-rename keys on the BARE type name; a generic
			// instantiation reaches the default sanitize with its `<args>` attached
			// (`indirect<K, V>`), missing the map — internal/concurrent's `type indirect[K, V]`
			// vs `func (n *node[K, V]) indirect()` renamed the DECLARATION `Δindirect<K, V>`
			// while every use kept the raw name (CS0246 ×33, leaking into net/netip). Rename
			// the bare base at reassembly; a dotted (package-qualified) base never matches the
			// per-package map and keeps its exported-alias route.
			if nameCollisions[base] {
				base = getCollisionAvoidanceIdentifier(base)
			}

			typeName = fmt.Sprintf("%s<%s>%s", base, strings.Join(subTypes, ", "), typeName[end+1:])
		}
	}

	if typeName == "func()" {
		return "Action"
	}

	if strings.HasPrefix(typeName, "func(") {
		// Find the matching closing parenthesis for the parameter list
		depth := 0
		closingParenIndex := -1

		for i := 5; i < len(typeName); i++ {
			if typeName[i] == '(' {
				depth++
			} else if typeName[i] == ')' {
				depth--
				if depth == -1 {
					closingParenIndex = i
					break
				}
			}
		}

		if closingParenIndex == -1 {
			return "Action" // Malformed input (unexpected)
		}

		// Extract parameter types, handling nested functions
		paramString := typeName[5:closingParenIndex]
		paramTypes := extractTypes(paramString)

		// extractTypes already renders each parameter in C# form (a NAMED param has its type
		// converted after the name is stripped; the bare-type case is converted in place), so use
		// its output directly. Re-running convertToCSTypeName here DOUBLE-converts an already-C#
		// param — an emitted `map<@string, ж<Object>>` re-fed through the `map<` arm's
		// splitMapKeyValue mis-parses to `map<@string, ж<Object>, >` (spurious trailing empty type
		// arg → CS1031), as in go/ast's `type Importer func(imports map[string]*Object, …)`.
		csTypeNames := paramTypes

		// A VARIADIC tail (marked by extractTypes) hoists into the golib delegate FAMILY —
		// `Actionꓸꓸꓸ<…>`/`Funcꓸꓸꓸ<…>` with the variadic ELEMENT type as the last parameter
		// type argument, matching iifeDelegateType's structural lowering exactly.
		family := ""

		if count := len(csTypeNames); count > 0 {
			if elem, ok := strings.CutPrefix(csTypeNames[count-1], EllipsisOperator); ok {
				family = EllipsisOperator
				csTypeNames[count-1] = elem
			}
		}

		// Check for return type after the closing parenthesis
		remainingType := strings.TrimSpace(typeName[closingParenIndex+1:])

		if len(remainingType) > 0 {
			// Has explicit return type. A PARENTHESIZED result list may carry Go NAMED
			// results (`(importPath string, ok bool)` — go/doc/comment's LookupPackage
			// func field): split the elements, strip the Go-ordered names, and rebuild —
			// ONE result unwraps to its bare type (a C# 1-tuple is CS8124), several yield
			// the C#-ordered named tuple `(@string importPath, bool ok)`.
			csReturnType := convertToCSResultList(remainingType)

			if len(csTypeNames) > 0 {
				return fmt.Sprintf("Func%s<%s, %s>", family, strings.Join(csTypeNames, ", "), csReturnType)
			}

			return fmt.Sprintf("Func<%s>", csReturnType)
		}

		// No return type, use Action
		if len(csTypeNames) > 0 {
			return fmt.Sprintf("Action%s<%s>", family, strings.Join(csTypeNames, ", "))
		}

		return "Action"
	}

	// Handle pointer types
	if strings.HasPrefix(typeName, "*") {
		return fmt.Sprintf("%s.%s<%s>", RootNamespace, PointerPrefix, convertToCSTypeName(typeName[1:]))
	}

	switch typeName {
	case "int":
		return "nint"
	case "uint":
		return "nuint"
	case "bool":
		return "bool"
	case "byte":
		return "byte"
	case "float":
		return "float64"
	case "complex64":
		return RootNamespace + ".complex64"
	case "string":
		return RootNamespace + ".@string"
	case "interface{}":
		return "any"
	case "struct{}":
		return RootNamespace + ".EmptyStruct"
	default:
		if strings.Contains(typeName, PackageSuffix) {
			parts := strings.Split(typeName, ".")
			count := len(parts)

			if count > 1 {
				sourcePkg := strings.TrimSuffix(parts[count-2], PackageSuffix)
				targetType := parts[count-1]
				alias := fmt.Sprintf("%s.%s", sourcePkg, targetType)

				packageLock.Lock()
				aliasType, exists := importedTypeAliases[alias]
				packageLock.Unlock()

				if exists {
					return aliasType
				}
			}
		}

		return fmt.Sprintf("%s.%s", RootNamespace, getSanitizedIdentifier(getAliasedTypeName(typeName)))
	}
}

// implicitConvStructTypeName renders the C# name a GoImplicitConv attribute can carry for a
// struct-underlying type: a NAMED type's converted name, or the lifted dynamic-type name for a
// package-level anonymous struct. A lifted name whose declaring file has not been visited yet
// records the DEFERRED MARKER, resolved after the file-visit barrier when the package_info
// lines are emitted (raw Go `struct{…}` text is never attribute-safe C#).
func (v *Visitor) implicitConvStructTypeName(t types.Type) string {
	if named, ok := types.Unalias(t).(*types.Named); ok {
		return v.getCSharpTypeName(named)
	}

	// This visitor's lifted name is type-identity-keyed — precise even when two anonymous
	// structs share a structural signature (Process_data vs main_data); the shared registry
	// and the deferred marker are the cross-file fallbacks.
	if name, ok := v.liftedTypeMap[t]; ok {
		return name
	}

	signature := t.String()

	if name := lookupDynamicTypeName(signature); name != "" {
		return name
	}

	return dynamicTypeMarker(signature)
}

// resolveImplicitConvTypeName resolves a possibly-deferred implicit-conversion type name after
// the file-visit barrier (the dynamic-type registry is complete). Returns ok=false when the
// marker cannot resolve — a genuinely unlifted anonymous struct has no attribute-safe name and
// its record is dropped.
func resolveImplicitConvTypeName(name string) (string, bool) {
	if strings.HasPrefix(name, dynamicTypeMarkerPrefix) && strings.HasSuffix(name, dynamicTypeMarkerSuffix) {
		payload := name[len(dynamicTypeMarkerPrefix) : len(name)-len(dynamicTypeMarkerSuffix)]
		signature, decoded := dynamicTypeMarkerSignature(payload)

		if !decoded {
			// Dropping the record stays the right outcome — there is still no attribute-safe name
			// — but this is a corrupted marker rather than the ordinary "never lifted" case, and
			// the two want opposite responses: one is a converter bug, the other is expected. Only
			// a report tells them apart, since the drop itself looks identical.
			showWarning("Undecodable dynamic-type marker payload \"%s\" in an implicit-conversion record", payload)
			return "", false
		}

		if resolved := lookupDynamicTypeName(signature); resolved != "" {
			return resolved, true
		}

		return "", false
	}

	return name, true
}
