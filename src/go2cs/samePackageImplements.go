// samePackageImplements.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file closes the one place where "the declaring assembly implements this pair" is TRUE in Go
// and FALSE in the emitted C#.
//
// Every [assembly: GoImplement<T, Iface>] record the converter writes comes from a CAST it converted
// — convertToInterfaceType records the pair it just emitted. Go, though, satisfies an interface
// structurally: `encoding/binary` declares `type bigEndian struct{}` with the whole ByteOrder method
// set and exports `var BigEndian bigEndian`, and it never once writes `var _ ByteOrder = BigEndian`.
// No cast, no record — so `binary_package.bigEndian` was emitted as a partial struct that does NOT
// implement `ByteOrder`, and every consumer minted its own `binary_bigEndianᴠByteOrder` adapter.
//
// That adapter is a SECOND IDENTITY for one Go value: reflect and fmt see the wrapper where the
// value's own type belongs, and a direct-boxed value compares unequal to an adapter-wrapped one.
// The fix is to record what Go already says is true, so the declaring assembly realizes the pair
// itself and the consumer hands over the bare value.

package main

import (
	"go/ast"
	"go/token"
	"go/types"
	"path/filepath"
	"strings"
)

// recordSamePackageValueImplements records the VALUE-form GoImplement pairs a package SATISFIES but
// never WITNESSES: a defined type and an interface, both declared in the package being converted,
// where the type's VALUE method set already implements the interface.
//
// It records through convertToInterfaceType with an EMPTY expression — the same record-only probe
// path convCompositeLit / convTypeAssertExpr / visitValueSpec already use — so a synthesized pair is
// composed, keyed and pruned exactly as a real cast would compose, key and prune it. That is the
// whole design: no second naming path can drift from the cast site's (the divergence that made the
// FOREIGN lookup miss for six weeks — see implementRecordKey).
//
// Four gates bound it, and each one is load-bearing:
//
//   - The interface is EXPORTED. A record is a CROSS-ASSEMBLY contract — it exists so another
//     assembly's cast can drop its local adapter — and no other assembly can name an unexported
//     interface, so a record for one could never be consulted. The package's own casts already
//     record whatever it needs internally.
//   - The type's underlying is NOT a *types.Signature. A named FUNC type is a C# DELEGATE, which
//     cannot be a partial struct, so ImplementGenerator emits an adapter CLASS for it instead; a
//     consumer trusting THAT record hands a bare delegate to an interface slot (CS0029 — net/http's
//     HandlerFunc → ΔHandler). This is the declaring-side half of valueRecordRealizesAsPartialStruct.
//   - Neither side is GENERIC. A type argument cannot appear in an assembly-attribute type argument
//     (CS0246), the same exclusion convertToInterfaceType's targetIsOpenGeneric makes.
//   - Both sides are declared in a file this run actually CONVERTS. go/packages and the converter's
//     own build-constraint evaluator agree on nearly every file, but a record naming a type no
//     emitted file declares is CS0246, and a package scope holds every file's declarations.
//   - Every interface method is REALIZABLE by the generator: it resolves on the type itself or
//     through at most ONE embedded field. ImplementGenerator forwards a promoted member through a
//     single embed hop and says so ("Go's promotion ambiguity rules make multi-embed satisfaction
//     rare; extend when needed"), so a deeper promotion emits a forwarder that passes the wrong
//     hop — CrossPkgUser's `rig` embeds `CrossPkgLib.Device`, which embeds `Sensor`, where Label
//     lives: `CrossPkgLib_package.Label(this.Device)` is CS1503, wanting `this.Device.Sensor`.
//     This is the same reasoning as valueRecordRealizesAsPartialStruct — record only what the
//     generator can realize — and it binds only the SPECULATIVE recorder here. A pair the source
//     actually casts is DEMANDED and still records at its cast site, promotion depth and all;
//     extending the generator is a separate increment that would relax this gate, not remove it.
//
// The POINTER method set is deliberately out of scope: `types.Implements(*T, Iface)` is the far
// larger set (548 same-package pairs in the standard library against 168 for the value set), its
// records are adapter-class EXISTENCE signals with a different trust rule, and it is owed its own
// increment with its own measured footprint.
func recordSamePackageValueImplements(fset *token.FileSet, packageTypes *types.Package, info *types.Info, options Options, globalIdentNames map[*ast.Ident]string, globalScope map[string]*types.Var, convertedFiles []FileEntry) {
	if packageTypes == nil || fset == nil || len(convertedFiles) == 0 {
		return
	}

	convertedPaths := HashSet[string]{}

	for _, entry := range convertedFiles {
		if entry.filePath != "" {
			convertedPaths.Add(strings.ToLower(filepath.Clean(entry.filePath)))
		}
	}

	declaredInConvertedFile := func(obj types.Object) bool {
		position := fset.Position(obj.Pos())

		if !position.IsValid() {
			return false
		}

		return convertedPaths.Contains(strings.ToLower(filepath.Clean(position.Filename)))
	}

	var targets, interfaces []*types.Named

	// Scope names arrive sorted, so the record order — and therefore any state a record claims — is
	// deterministic across runs.
	scope := packageTypes.Scope()

	for _, name := range scope.Names() {
		typeName, ok := scope.Lookup(name).(*types.TypeName)

		if !ok || typeName.IsAlias() || !declaredInConvertedFile(typeName) {
			continue
		}

		named, ok := typeName.Type().(*types.Named)

		if !ok || (named.TypeParams() != nil && named.TypeParams().Len() > 0) {
			continue
		}

		if iface, isInterface := named.Underlying().(*types.Interface); isInterface {
			// A CONSTRAINT interface (one carrying type terms) describes a type SET, not a method
			// set — nothing can implement it at run time, and go2cs-gen emits no interface for it.
			if typeName.Exported() && iface.NumMethods() > 0 && iface.IsMethodSet() {
				interfaces = append(interfaces, named)
			}

			continue
		}

		if _, isSignature := named.Underlying().(*types.Signature); isSignature {
			continue
		}

		targets = append(targets, named)
	}

	if len(targets) == 0 || len(interfaces) == 0 {
		return
	}

	// A scratch visitor: the record path needs the package's type/name resolution, not any one
	// file's emission state. It never visits a file and its output builders stay empty.
	visitor := newFileVisitor(fset, packageTypes, info, options, globalIdentNames, globalScope, newFileEntry(nil, "", false))

	for _, target := range targets {
		for _, ifaceNamed := range interfaces {
			iface, ok := ifaceNamed.Underlying().(*types.Interface)

			if !ok || !types.Implements(target, iface) {
				continue
			}

			if !generatorCanForwardMethodSet(target, iface, packageTypes) {
				continue
			}

			visitor.convertToInterfaceType(ifaceNamed, target, "")
		}
	}
}

// generatorCanForwardMethodSet reports whether ImplementGenerator can emit a forwarder for every
// method of iface on target. types.LookupFieldOrMethod returns one index element per embedded-field
// hop plus the final selection, so a directly declared method indexes at length 1 and a
// single-embed promotion at length 2 — the depth the generator's promoted-member arms resolve. A
// deeper path (a value embed of a value embed) forwards through the WRONG hop, so the speculative
// record is withheld rather than emitted as code that cannot compile. Promotion through an embedded
// INTERFACE is the common shape this admits (sort's `reverse` embeds `Interface`; debug/macho's
// segment types embed `LoadBytes`), and it stays admitted at depth 1.
func generatorCanForwardMethodSet(target types.Type, iface *types.Interface, pkg *types.Package) bool {
	for i := range iface.NumMethods() {
		method := iface.Method(i)

		obj, index, _ := types.LookupFieldOrMethod(target, false, pkg, method.Name())

		if obj == nil || len(index) > 2 {
			return false
		}
	}

	return true
}
