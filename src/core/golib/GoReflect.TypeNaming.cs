// GoReflect.TypeNaming.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Numerics;
using static go2cs.Symbols;

namespace go;

// ---------------------------------------------------------------------------------------------
// TYPE NAMING — what a converted type is CALLED in Go.
//
// WHAT LIVES HERE
//   Everything behind `reflect.Type.String()`, `Name()`, `PkgPath()` and `fmt`'s `%T`: the Go
//   source spelling of a managed type, the package qualification that turns
//   `go.main_package.Point` into `main.Point`, and the import path that turns
//   `go.encoding.gob_package.N2` into `"encoding/gob"`.
//
// WHY THIS IS RECONSTRUCTION AND NOT LOOKUP
//   Go's own reflect reads a type's NAME out of a runtime type descriptor the compiler emitted.
//   There is no such descriptor here — a converted type is an ordinary CLR type — so the Go name
//   has to be rebuilt from what the converter DID leave behind: the managed nesting
//   (`namespace go.<parents>` + class `<pkg>_package` + the type nested inside it), the
//   `[GoPackage]` / `[GoLocalName]` stamps, and the golib container generics. Every rule in this
//   file is one step of that reconstruction, and each one is exact for the shapes the converter
//   emits and only for those.
//
// THE STAMP OUTRANKS THE NAME
//   `goPackageNameOf` prefers the class's `[GoPackage]` stamp and only falls back to trimming
//   `_package` off the class name. They agree for every ordinary converted package; where they
//   disagree the stamp is right, and the case is not exotic — a `-tests` white-box bridge is class
//   `binary_internal_test_package` yet Go-declares its contents in `package binary`. Trimming
//   would report `binary_internal_test.Person`, and encoding/binary's own tests assert the type
//   name inside an error string and name their subtests from it.
//
// THE TWO PLACES THE MAPPING IS NOT AN EXACT INVERSE
//   Both are naming-only losses, both are recorded rather than fixed, and both come from a package
//   whose import path's last segment is not its package name: a major-version directory
//   (`math/rand/v2` emits namespace `go.math.rand` + class `rand_package`, so `PkgPath` recovers
//   `"math/rand"`), and a module dependency whose declared package name differs from its directory.
//   Everything else round-trips exactly.
//
// ADAPTERS RENDER AS WHAT THEY STAND FOR, NEVER AS THEMSELVES
//   A generated interface-implementation adapter is a class the converter minted; Go has no such
//   type. So `GoTypeName` unwraps it — a pointer-sourced adapter renders `*T`, a value-sourced one
//   renders the wrapped struct — exactly as `KindOf` and `ElementType` unwrap it in the primary
//   file. Those three must agree; an adapter that named itself would print a C# class name from
//   `%T` and make `reflect.TypeOf` return a type no Go program can name.
//
// THE MEMOIZATION IS NOT OPTIONAL, AND IT IS NOT INVALIDATED
//   Reading a custom attribute materializes a fresh attribute instance on EVERY call — measured
//   at ~361 ns and 200 bytes for one probe — and these reads sit under callers that cache nothing
//   of their own, so the cost was being paid per VALUE printed rather than per type. Deliberately
//   NOT hooked to any assembly-load cache clear, unlike the extension-method caches in
//   runtime/TypeExtensions: a loaded type's own attributes cannot change, so there is nothing a
//   later assembly could invalidate.
// ---------------------------------------------------------------------------------------------
public static partial class GoReflect
{
    /// <summary>
    /// The Go source type string for a managed <see cref="Type"/> — what `reflect.Type.String()` and
    /// `%T` print. Recurses over the golib container types (`[]int`, `map[string]int`, `*main.Point`),
    /// maps the scalar representations to their Go spelling, and package-qualifies a named/struct type
    /// (`go.main_package.Point` → `main.Point`).
    /// </summary>
    public static string GoTypeName(Type? t)
    {
        return GoTypeName(t, null);
    }

    /// <summary>
    /// The Go source type string with ARRAY DIMS threaded (a dims-carrying array descriptor
    /// renders Go's <c>[4]uint8</c>; without dims the managed type cannot distinguish
    /// <c>[N]T</c> from <c>[]T</c> — the recorded limitation).
    /// </summary>
    public static string GoTypeName(Type? t, nint[]? arrayDims)
    {
        if (t is null) return "<nil>";

        if (arrayDims is { Length: > 0 } && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(array<>))
        {
            nint[]? innerDims = arrayDims.Length > 1 ? arrayDims[1..] : null;
            return "[" + arrayDims[0] + "]" + GoTypeName(t.GetGenericArguments()[0], innerDims);
        }

        if (t == typeof(bool)) return "bool";
        if (t == typeof(nint)) return "int";
        if (t == typeof(sbyte)) return "int8";
        if (t == typeof(short)) return "int16";
        if (t == typeof(int)) return "int32";
        if (t == typeof(long)) return "int64";
        if (t == typeof(nuint)) return "uint";
        if (t == typeof(byte)) return "uint8";
        if (t == typeof(ushort)) return "uint16";
        if (t == typeof(uint)) return "uint32";
        if (t == typeof(ulong)) return "uint64";
        if (t == typeof(uintptr)) return "uintptr";
        if (t == typeof(float)) return "float32";
        if (t == typeof(double)) return "float64";
        if (t == typeof(Complex)) return "complex128";
        if (t == typeof(complex64)) return "complex64";
        if (t == typeof(@string) || t == typeof(string)) return "string";
        if (t == typeof(object)) return "interface {}";

        if (t.IsGenericType)
        {
            Type gd = t.GetGenericTypeDefinition();
            Type[] a = t.GetGenericArguments();

            if (gd == typeof(slice<>)) return "[]" + GoTypeName(a[0]);
            if (gd == typeof(array<>)) return "[]" + GoTypeName(a[0]);   // length is not carried on the managed type
            if (gd == typeof(map<,>)) return "map[" + GoTypeName(a[0]) + "]" + GoTypeName(a[1]);
            if (gd == typeof(channel<>)) return "chan " + GoTypeName(a[0]);
            if (gd == typeof(ж<>)) return "*" + GoTypeName(a[0]);
        }

        if (t.BaseType == typeof(ж<uintptr>)) return "unsafe.Pointer";

        // A generated interface-implementation adapter stands in for the Go dynamic value it
        // wraps: a pointer-sourced ж-adapter renders as Go's *T, a value-sourced ᴠ-adapter as
        // the wrapped struct type itself — never as the adapter class.
        if (TryAdapterWrappedType(t, out Type? wrapped, out bool pointerSourced))
            return pointerSourced ? "*" + GoTypeName(wrapped) : GoTypeName(wrapped);

        return GoQualifiedName(t);
    }

    private static readonly ConcurrentDictionary<Type, GoLocalNameAttribute?> s_goLocalNames = new();

    /// <summary>The type's own <c>[GoLocalName]</c> stamp, or <c>null</c> when it carries none.</summary>
    /// <remarks>Memoized per type for the reason given on <see cref="goTypeMarkerOf"/>.</remarks>
    private static GoLocalNameAttribute? goLocalNameOf(Type t)
    {
        return s_goLocalNames.GetOrAdd(t, static type =>
            type.GetCustomAttributes(typeof(GoLocalNameAttribute), false) is [GoLocalNameAttribute localName] ? localName : null);
    }

    private static readonly ConcurrentDictionary<Type, string> s_goPackageNames = new();

    /// <summary>
    /// The GO package name a converted type's declaring class stands for, or <c>""</c> when the
    /// class is not a package class at all.
    /// </summary>
    /// <remarks>
    /// The class's own <c>[GoPackage]</c> stamp is the authority; trimming <c>_package</c> off the
    /// class NAME is only a fallback for a hand-written class that carries no stamp. The two agree
    /// for every ordinary converted package, and where they disagree the stamp is right: a `-tests`
    /// white-box bridge is class <c>binary_internal_test_package</c> yet stamped
    /// <c>[GoPackage("binary")]</c>, because the internal <c>_test.go</c> declarations it hosts are
    /// Go-declared in <c>package binary</c> — which is what <c>reflect</c> must report
    /// (<c>binary.Person</c>, not <c>binary_internal_test.Person</c>: encoding/binary's
    /// TestNoFixedSize asserts the type name inside an error string, and TestSizeAllocs names its
    /// subtests from it). Memoized per declaring type for the reason given on
    /// <see cref="goTypeMarkerOf"/> — this sits under every <c>GoTypeName</c>/<c>PkgPath</c> read.
    /// </remarks>
    private static string goPackageNameOf(Type? decl)
    {
        if (decl is null)
            return "";

        return s_goPackageNames.GetOrAdd(decl, static type =>
        {
            if (!type.Name.EndsWith(PackageSuffix, StringComparison.Ordinal))
                return "";

            if (type.GetCustomAttributes(typeof(GoPackageAttribute), false) is [GoPackageAttribute marker] &&
                marker.PackageName.Length > 0)
                return marker.PackageName;

            return type.Name[..^PackageSuffix.Length];
        });
    }

    // The package-qualified Go name of a converted named type: a converted type is nested in a
    // `<pkg>_package` class, so `go.main_package.Point` → `main.Point`. A lifted function-local
    // type prefers its stamped original Go name ([GoLocalName] — `binary.Person`, never the
    // lifted `TestNoFixedSize_Person`). A Δ-collision rename (ΔHandle) strips the marker; a type
    // with no `_package` declaring class falls back to its bare name.
    private static string GoQualifiedName(Type t)
    {
        string name = t.Name;

        if (goLocalNameOf(t) is { } localName)
            name = localName.Name;

        if (name.StartsWith(ShadowVarMarker, StringComparison.Ordinal))
            name = name[ShadowVarMarker.Length..];

        if (goPackageNameOf(t.DeclaringType) is { Length: > 0 } packageName)
            return packageName + "." + name;

        return name;
    }

    /// <summary>
    /// The Go IMPORT PATH of the package that DEFINES a converted named type
    /// (<c>go.encoding.gob_package.N2</c> → <c>"encoding/gob"</c>) — <c>reflect.Type.PkgPath</c>.
    /// Empty for a type that is not a defined Go type: a primitive, a raw container
    /// (<c>slice&lt;T&gt;</c> / <c>map&lt;K,V&gt;</c> / <c>ж&lt;T&gt;</c>), or anything not nested in a
    /// <c>&lt;pkg&gt;_package</c> class — exactly Go's rule that only a DEFINED type has a package path.
    /// </summary>
    /// <remarks>
    /// Derived from the managed nesting, which is where the converter puts the package identity: the
    /// declaring class names the package and the enclosing namespace names its parent directories
    /// (<c>go</c> is the emission root). The mapping is not a strict inverse for the two cases where
    /// the class name is not the path's last segment — a major-version directory
    /// (<c>math/rand/v2</c> emits namespace <c>go.math.rand</c> + class <c>rand_package</c>, so this
    /// recovers <c>"math/rand"</c>) and a module dependency whose package name differs from its path
    /// segment. Both are naming-only losses; the Go-visible path is exact for every other package.
    /// </remarks>
    public static string GoPackagePath(Type? t)
    {
        if (t is null)
            return "";

        if (goPackageNameOf(t.DeclaringType) is not { Length: > 0 } pkg)
            return "";

        string ns = t.Namespace ?? "";

        if (ns.Length > EmissionRootNamespace.Length + 1 && ns.StartsWith(EmissionRootNamespace + ".", StringComparison.Ordinal))
            return ns[(EmissionRootNamespace.Length + 1)..].Replace('.', '/') + "/" + pkg;

        return pkg;
    }

    // The namespace every converted package is emitted under; its dotted tail mirrors the import
    // path's parent directories (go.encoding.gob_package ⇒ "encoding/gob").
    private const string EmissionRootNamespace = "go";
}
