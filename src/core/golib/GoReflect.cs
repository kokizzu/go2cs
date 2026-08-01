// GoReflect.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using go.golib;
using static go2cs.Symbols;

namespace go;

/// <summary>
/// Managed backing for the native reflection bridge (Phase 4). Go's <c>reflect</c>/<c>internal/abi</c>
/// read an interface's <c>{type,data}</c> words through <c>unsafe.Pointer</c> to reach a runtime type
/// descriptor that has no managed form; this helper reconstructs the Go <c>reflect.Kind</c> of a value
/// from its managed <see cref="Type"/>, and provides a descriptor↔<see cref="Type"/> side table so the
/// hand-owned <c>abi.TypeOf</c> entry point can carry the real managed type on the (otherwise synthetic)
/// <c>abi.Type</c> box. See <c>docs/Phase4/DESIGN-reflection-bridge.md</c>.
/// </summary>
public static class GoReflect
{
    // Go reflect.Kind numbering (internal/abi and reflect define these identically, 0..26).
    public const int Invalid = 0, Bool = 1, Int = 2, Int8 = 3, Int16 = 4, Int32 = 5, Int64 = 6;
    public const int Uint = 7, Uint8 = 8, Uint16 = 9, Uint32 = 10, Uint64 = 11, Uintptr = 12;
    public const int Float32 = 13, Float64 = 14, Complex64 = 15, Complex128 = 16;
    public const int Array = 17, Chan = 18, Func = 19, Interface = 20, Map = 21, Pointer = 22;
    public const int Slice = 23, String = 24, Struct = 25, UnsafePointer = 26;

    // Maps each synthetic abi.Type descriptor box to the managed Type it stands for, so the hand-owned
    // reflect Type/Value methods (String/Name/Elem/Field/...) can recover Go type info from System.Type.
    // Keyed on the box object identity; weak so descriptors are not pinned.
    private static readonly ConditionalWeakTable<object, Type> s_sysTypes = new();

    /// <summary>Records the managed <see cref="Type"/> that a synthetic abi.Type descriptor box stands for.</summary>
    public static void Register(object descriptorBox, Type sysType)
    {
        if (descriptorBox is null || sysType is null)
            return;

        s_sysTypes.AddOrUpdate(descriptorBox, sysType);
    }

    /// <summary>Recovers the managed <see cref="Type"/> a descriptor box stands for, or <c>null</c>.</summary>
    public static Type? SysTypeOf(object? descriptorBox)
    {
        return descriptorBox is not null && s_sysTypes.TryGetValue(descriptorBox, out Type? t) ? t : null;
    }

    /// <summary>
    /// Resolves the managed <see cref="Type"/> that stands for a value's GO DYNAMIC TYPE, seeing
    /// through the runtime's interface-carrier wrappers: an <see cref="IInterfaceAdapter"/> chain
    /// unwraps to the original dynamic value, and a pointer-sourced <see cref="IжAdapter"/> yields
    /// the receiver box's type (<c>ж&lt;T&gt;</c> — Go's dynamic type is the <c>*T</c>, never the
    /// adapter class). The reflection bridge's <c>TypeOf</c>/<c>ValueOf</c> classify THIS type, so
    /// an adapter-held <c>*T</c> and a raw <c>ж&lt;T&gt;</c> intern to the same canonical
    /// <c>reflect.Type</c> and compare assignable by identity, exactly as in Go (R10).
    /// </summary>
    public static Type GoDynamicTypeOf(object value)
    {
        while (value is IInterfaceAdapter { Value: not null } interfaceAdapter)
            value = interfaceAdapter.Value;

        if (value is IжAdapter { Box: not null } pointerAdapter)
            return pointerAdapter.Box.GetType();

        // A value-sourced adapter (IValueAdapter) wraps a COPY of a struct this assembly cannot
        // partial — Go's dynamic type is that struct, never the adapter class.
        if (value is IValueAdapter { Value: not null } valueAdapter)
            return valueAdapter.Value.GetType();

        return value.GetType();
    }

    /// <summary>
    /// Classifies a managed <see cref="Type"/> to its Go <c>reflect.Kind</c> ordinal. A NAMED Go type
    /// (<c>type Celsius float64</c> → <c>[GoType("num:float64")]</c>, or a wrapper struct) reports its
    /// UNDERLYING kind, matching Go — the name is recovered separately from the type itself. A
    /// generated interface-implementation adapter CLASS classifies as the Go dynamic type it stands
    /// for (<c>*T</c> → Pointer; a value-sourced ᴠ-adapter → the wrapped struct's kind), mirroring
    /// <see cref="GoTypeName"/>'s unwrap (R10) — though value-level callers should prefer
    /// <see cref="GoDynamicTypeOf"/>, which resolves instance state the type alone cannot.
    /// </summary>
    public static int KindOf(Type? t)
    {
        if (t is null)
            return Invalid;

        if (TryAdapterWrappedType(t, out Type? adapterWrapped, out bool adapterPointerSourced))
            return adapterPointerSourced ? Pointer : KindOf(adapterWrapped);

        // Fast, exact matches for the built-in Go scalar representations.
        if (t == typeof(bool)) return Bool;
        if (t == typeof(nint)) return Int;                 // Go int
        if (t == typeof(sbyte)) return Int8;
        if (t == typeof(short)) return Int16;
        if (t == typeof(int)) return Int32;                // Go int32 / rune
        if (t == typeof(long)) return Int64;
        if (t == typeof(nuint)) return Uint;               // Go uint
        if (t == typeof(byte)) return Uint8;
        if (t == typeof(ushort)) return Uint16;
        if (t == typeof(uint)) return Uint32;
        if (t == typeof(ulong)) return Uint64;
        if (t == typeof(float)) return Float32;
        if (t == typeof(double)) return Float64;
        if (t == typeof(Complex)) return Complex128;
        // golib complex64 is its own hand-written struct (no [GoType] marker), not a narrowing
        // of System.Numerics.Complex — without this arm it classified as Struct and the
        // reflect walkers enumerated its private float fields (binary's Struct.Complex64).
        if (t == typeof(complex64)) return Complex64;
        if (t == typeof(@string)) return String;
        // `any` — the empty interface. System.Object reports IsInterface false, so without
        // this arm it fell through to Struct.
        if (t == typeof(object)) return Interface;
        // A C# System.String can reach reflection where a Go string literal boxed in a
        // deliberately-uncast position (a variadic `...any` argument) — a bare `"a"` rather than
        // `(@string)"a"`; treat it as a Go string so reflect.TypeOf(it).Kind() == String (fmt's doPrint
        // inter-argument spacing depends on it).
        if (t == typeof(string)) return String;
        if (t == typeof(uintptr)) return Uintptr;

        // A named numeric / string wrapper carries its underlying kind in [GoType("num:<kind>")] or
        // [GoType("@string")]; report the underlying kind (Name() recovers the name elsewhere).
        if (TryGoTypeDefinitionKind(t, out int defKind))
            return defKind;

        // golib generic containers → their Go kind, detected by open generic definition.
        if (t.IsGenericType)
        {
            Type gd = t.GetGenericTypeDefinition();

            if (gd == typeof(slice<>)) return Slice;
            if (gd == typeof(array<>)) return Array;
            if (gd == typeof(map<,>)) return Map;
            if (gd == typeof(channel<>)) return Chan;
            if (gd == typeof(ж<>)) return Pointer;
        }

        // @unsafe.Pointer is a CONCRETE class `: ж<uintptr>` (not a generic ж<T>, so the check above
        // misses it, and not a [GoType("num:uintptr")] value wrapper). golib cannot name the unsafe
        // package's type directly, so detect it structurally: a reference type whose base is ж<uintptr>.
        if (t.BaseType == typeof(ж<uintptr>)) return UnsafePointer;

        // A NAMED container type (`type S []byte`, `type M map[K]V`, `type P *T`, ...) is a
        // generated wrapper struct/class implementing the golib container interface — classify
        // STRUCTURALLY, never by parsing the [GoType] definition token (a converter-rendered C#
        // type string). Order matters: ISlice derives from IArray, so slices probe first.
        if (typeof(ISlice).IsAssignableFrom(t)) return Slice;
        if (typeof(IMap).IsAssignableFrom(t)) return Map;
        if (typeof(IChannel).IsAssignableFrom(t)) return Chan;
        if (typeof(IArray).IsAssignableFrom(t)) return Array;
        if (typeof(INilPointer).IsAssignableFrom(t) && !t.IsValueType) return Pointer;

        if (typeof(Delegate).IsAssignableFrom(t)) return Func;

        if (t.IsInterface) return Interface;

        // A converted Go struct is a [GoType] value type; anything else value-typed still reports Struct.
        if (t.IsValueType) return Struct;

        // Reference-typed converted types (classes) that are none of the above are treated as pointers
        // to their referent in the Go model (rare on the fmt path); default to Struct otherwise.
        return Struct;
    }

    /// <summary>
    /// Reports whether values of the Go type that <paramref name="t"/> represents are comparable
    /// (usable with <c>==</c> / as a map key). Mirrors Go's rule: slices, maps and funcs are not
    /// comparable, nor is any struct or array that (transitively) contains one; every other kind —
    /// bool, numbers, string, pointer, channel, interface, unsafe.Pointer — is. The reflection bridge
    /// uses this to populate <c>abi.Type.Equal</c> on a synthesized descriptor, which both
    /// <c>reflect.Type.Comparable</c> and <c>internal/reflectlite</c>'s <c>Comparable</c> read as their
    /// comparability signal (<c>Equal != nil</c>) — and <c>errors.Is</c> gates its equality match on the
    /// latter, so a wrong answer here makes <c>errors.Is(err, sentinel)</c> silently return false.
    /// </summary>
    public static bool IsComparable(Type? t)
    {
        if (t is null)
            return false;

        switch (KindOf(t))
        {
            case Slice:
            case Map:
            case Func:
                return false;
            case Array:
                return IsComparable(ElementType(t));
            case Struct:
                return StructFieldsComparable(t);
            default:
                return true;
        }
    }

    // A struct is comparable iff every field is (Go). Recurses over the converted [GoType] struct's
    // instance fields; a field of slice/map/func kind — or a nested struct/array that contains one —
    // makes the whole struct non-comparable. Pointer/interface/chan fields stay comparable without
    // recursing into their referents, so this terminates (Go forbids a struct containing itself by value).
    private static bool StructFieldsComparable(Type t)
    {
        foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!IsComparable(f.FieldType))
                return false;
        }

        return true;
    }

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

    /// <summary>
    /// Identifies a generated interface-implementation adapter class and recovers the Go dynamic
    /// type it stands in for: a pointer-sourced adapter (<see cref="IжAdapter"/>, wrapping the
    /// receiver box <c>ж&lt;T&gt;</c>) yields <c>T</c> with <paramref name="pointerSourced"/>
    /// <c>true</c>; a value-sourced foreign adapter (<c>{Struct}ᴠ{Iface}</c>, wrapping a copy)
    /// yields the struct type. Both are recovered from the adapter's single one-parameter
    /// constructor, so no name parsing of the wrapped type is involved.
    /// </summary>
    public static bool TryAdapterWrappedType(Type? t, [NotNullWhen(true)] out Type? wrapped, out bool pointerSourced)
    {
        wrapped = null;
        pointerSourced = false;

        if (t is null || t.IsValueType)
            return false;

        if (typeof(IжAdapter).IsAssignableFrom(t))
        {
            // The template's only constructor takes the wrapped receiver box ж<T>.
            foreach (ConstructorInfo ctor in t.GetConstructors())
            {
                ParameterInfo[] parameters = ctor.GetParameters();

                if (parameters.Length == 1 && parameters[0].ParameterType is { IsGenericType: true } boxType &&
                    boxType.GetGenericTypeDefinition() == typeof(ж<>))
                {
                    wrapped = boxType.GetGenericArguments()[0];
                    pointerSourced = true;
                    return true;
                }
            }

            return false;
        }

        // A value-sourced adapter declares IValueAdapter; its only constructor takes the wrapped
        // struct by value. (This was a NAME probe for the ᴠ infix plus a package-class nesting
        // check until the marker existed — the marker is exact where the name was a heuristic, and
        // it is the same signal the equality / type-assert / type-switch paths now gate on.)
        if (!typeof(IValueAdapter).IsAssignableFrom(t))
            return false;

        foreach (ConstructorInfo ctor in t.GetConstructors())
        {
            ParameterInfo[] parameters = ctor.GetParameters();

            if (parameters.Length == 1 && parameters[0].ParameterType.IsValueType)
            {
                wrapped = parameters[0].ParameterType;
                return true;
            }
        }

        return false;
    }

    private static readonly ConcurrentDictionary<Type, GoTypeAttribute?> s_goTypeMarkers = new();
    private static readonly ConcurrentDictionary<Type, GoLocalNameAttribute?> s_goLocalNames = new();

    /// <summary>The type's own <c>[GoType]</c> marker, or <c>null</c> when it carries none.</summary>
    /// <remarks>
    /// Memoized per type, like <c>s_dynamicTypes</c> in <c>runtime/TypeExtensions</c> and for the
    /// same reason: which attributes a type declares is an IMMUTABLE fact about the type, but every
    /// read materializes fresh attribute instances — measured against live golib at 361 ns and
    /// 200 bytes for this one probe, and the reads sit under callers that cache nothing of their own
    /// (<see cref="KindOf"/> under every <c>reflect.ValueOf</c>/<c>Value.Field</c>/<c>Value.Elem</c>,
    /// <see cref="TryConvertTo"/> and <see cref="TryUnwrapWrapperValue"/> under the whole
    /// <c>Value.Set</c>/<c>SetMapIndex</c>/<c>Call</c>/<c>Convert</c> marshalling surface), so the
    /// cost was paid per VALUE rather than per type. Deliberately NOT registered with any
    /// assembly-load cache clear: unlike an extension-method scan, a loaded type's own attributes
    /// cannot change. Both markers are <c>AllowMultiple=false</c>, so the single declared attribute
    /// is the whole answer.
    /// </remarks>
    private static GoTypeAttribute? goTypeMarkerOf(Type t)
    {
        return s_goTypeMarkers.GetOrAdd(t, static type =>
            type.GetCustomAttributes(typeof(GoTypeAttribute), false) is [GoTypeAttribute marker] ? marker : null);
    }

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

    /// <summary>
    /// The Go element type of a managed container <see cref="Type"/> — <c>slice&lt;T&gt;</c>/
    /// <c>array&lt;T&gt;</c>/<c>channel&lt;T&gt;</c>/<c>ж&lt;T&gt;</c> → <c>T</c>, <c>map&lt;K,V&gt;</c> → <c>V</c>
    /// — for <c>reflect.Type.Elem()</c>; <c>null</c> if <paramref name="t"/> has no element type.
    /// </summary>
    public static Type? ElementType(Type? t)
    {
        if (t is null) return null;

        // A pointer-sourced adapter class stands for *T — its element type is T (R10), matching
        // the KindOf/GoTypeName unwrap.
        if (TryAdapterWrappedType(t, out Type? adapterWrapped, out bool adapterPointerSourced) && adapterPointerSourced)
            return adapterWrapped;

        if (t.IsGenericType)
        {
            Type gd = t.GetGenericTypeDefinition();
            Type[] a = t.GetGenericArguments();

            if (gd == typeof(map<,>)) return a[1];
            if (gd == typeof(slice<>) || gd == typeof(array<>) || gd == typeof(channel<>) || gd == typeof(ж<>)) return a[0];
        }

        // A NAMED container wrapper answers through the golib container interface it implements
        // (`type S []byte` → byte); a named pointer wrapper through IPointer<T>.
        if (ContainerInterfaceArguments(t, typeof(IMap<,>)) is { } mapArgs) return mapArgs[1];
        if (ContainerInterfaceArguments(t, typeof(ISlice<>)) is { } sliceArgs) return sliceArgs[0];
        if (ContainerInterfaceArguments(t, typeof(IArray<>)) is { } arrayArgs) return arrayArgs[0];
        if (ContainerInterfaceArguments(t, typeof(IChannel<>)) is { } chanArgs) return chanArgs[0];
        if (!t.IsValueType && ContainerInterfaceArguments(t, typeof(IPointer<>)) is { } ptrArgs) return ptrArgs[0];

        return null;
    }

    /// <summary>
    /// The Go KEY type of a map — <c>map&lt;K,V&gt;</c> (or a named map wrapper) → <c>K</c>;
    /// <c>null</c> if <paramref name="t"/> is not a map type. For <c>reflect.Type.Key()</c>.
    /// </summary>
    public static Type? KeyType(Type? t)
    {
        if (t is null) return null;

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(map<,>))
            return t.GetGenericArguments()[0];

        return ContainerInterfaceArguments(t, typeof(IMap<,>)) is { } mapArgs ? mapArgs[0] : null;
    }

    // Resolves the closed generic container interface a named wrapper implements
    // (ISlice<T>/IMap<K,V>/IArray<T>/IChannel<T>/IPointer<T>) and returns its type arguments,
    // or null. The raw golib containers are matched by open generic definition BEFORE this is
    // consulted, so this only ever answers for generated wrapper types.
    private static Type[]? ContainerInterfaceArguments(Type t, Type genericInterfaceDefinition)
    {
        foreach (Type ifc in t.GetInterfaces())
        {
            if (ifc.IsGenericType && ifc.GetGenericTypeDefinition() == genericInterfaceDefinition)
                return ifc.GetGenericArguments();
        }

        return null;
    }

    // ==== Phase-3 write-back helpers (shared by the reflect and reflectlite bridges) ====

    // Cached ref-accessor reads/writes of a ж<T> box's value slot, keyed by the closed box type.
    // ValueSlot (not Value) both ways: reads must not panic on a slot HOLDING a nil value (Go's
    // `*(&p)` yields nil, no dereference), and writes assign THROUGH the ref-returning property so
    // they land in the real slot — field-ref and array-element alias boxes included, which is what
    // Field(i)/Index(i) addressability builds on in the next increment. Writes to a structurally
    // nil box panic exactly like Go's nil-pointer store (blessing condition Q1a: the canonical
    // typed-nil singleton is write-protected).
    private static readonly ConcurrentDictionary<Type, Func<object, object?>> s_slotReaders = new();
    private static readonly ConcurrentDictionary<Type, Action<object, object?>> s_slotWriters = new();

    /// <summary>Reads the value held by a pointer box — a closed <c>ж&lt;T&gt;</c> or a generated named-pointer
    /// wrapper (<c>IPointer&lt;T&gt;</c>) — nil-safe (a nil box reads as the zero value).</summary>
    public static object? ReadPointerSlot(object box)
    {
        return s_slotReaders.GetOrAdd(box.GetType(), static boxType =>
        {
            (bool viaInterface, Type elem) = slotAccessorShape(boxType);
            MethodInfo reader = typeof(GoReflect).GetMethod(viaInterface ? nameof(readSlotViaInterface) : nameof(readSlot), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(elem);
            return reader.CreateDelegate<Func<object, object?>>();
        })(box);
    }

    /// <summary>Writes a value through a pointer box's slot ref (panics Go-style on a nil box).</summary>
    public static void WritePointerSlot(object box, object? value)
    {
        s_slotWriters.GetOrAdd(box.GetType(), static boxType =>
        {
            (bool viaInterface, Type elem) = slotAccessorShape(boxType);
            MethodInfo writer = typeof(GoReflect).GetMethod(viaInterface ? nameof(writeSlotViaInterface) : nameof(writeSlot), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(elem);
            return writer.CreateDelegate<Action<object, object?>>();
        })(box, value);
    }

    // A raw closed ж<T> uses the ValueSlot pair; a generated named-pointer wrapper (a non-generic
    // class implementing IPointer<T>) routes through the interface's ref-returning Value.
    private static (bool viaInterface, Type elemType) slotAccessorShape(Type boxType)
    {
        if (boxType.IsGenericType && boxType.GetGenericTypeDefinition() == typeof(ж<>))
            return (false, boxType.GetGenericArguments()[0]);

        Type[]? args = ContainerInterfaceArguments(boxType, typeof(IPointer<>));

        if (args is null)
            throw new InvalidOperationException($"Not a pointer box type: {boxType}");

        return (true, args[0]);
    }

    private static object? readSlot<T>(object box)
    {
        return ((ж<T>)box).ValueSlot;
    }

    private static void writeSlot<T>(object box, object? value)
    {
        ж<T> typed = (ж<T>)box;

        if (typed.IsNilPointer)
            throw RuntimeErrorPanic.NilPointerDereference();

        typed.ValueSlot = (T)value!;
    }

    private static object? readSlotViaInterface<T>(object box)
    {
        IPointer<T> typed = (IPointer<T>)box;

        // STRUCTURAL nil first: there is no storage to read, so the pointee reads as the zero value.
        // Every other box resolves its REAL storage through Value — including a struct-field or
        // array-element reference, whose own `m_val` is an unused default. That default is the trap:
        // a nil test that PEEKS AT THE VALUE calls such a box nil whenever the referenced field's
        // type is a reference type, and hands back default(T) in place of the field's actual value.
        // So ж<T>.IsNull is STRUCTURAL for those kinds, and the case it still answers by value — a
        // standard box whose reference-typed pointee is legitimately null — has default(T) as the
        // correct answer anyway, which is why the fallback on the last line is safe.
        if (box is INilPointer { IsNilPointer: true })
            return default(T);

        return typed.IsNull ? default(T) : typed.Value;
    }

    private static void writeSlotViaInterface<T>(object box, object? value)
    {
        if (box is INilPointer { IsNilPointer: true })
            throw RuntimeErrorPanic.NilPointerDereference();

        ((IPointer<T>)box).Value = (T)value!;
    }

    /// <summary>
    /// The canonical typed nil instance for a closed <c>ж&lt;T&gt;</c> pointer type (<see cref="ж{T}.NilBox"/>),
    /// resolved from the runtime <see cref="Type"/> — what <c>reflect.Zero</c> of a pointer kind yields.
    /// </summary>
    public static object? CanonicalNilPointer(Type pointerType)
    {
        return s_canonicalNils.GetOrAdd(pointerType, static t =>
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ж<>))
                return t.GetProperty(nameof(ж<int>.NilBox), BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

            // A generated NAMED pointer wrapper exposes its canonical typed nil as NilInstance
            // (declared internal by the template — probe both visibilities).
            return t.GetProperty("NilInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        });
    }

    private static readonly ConcurrentDictionary<Type, object?> s_canonicalNils = new();

    /// <summary>
    /// Go's <c>implements</c> relation over managed types: <paramref name="ifaceType"/> is an
    /// interface that <paramref name="valueType"/> satisfies nominally OR structurally by Go
    /// method-set rules — the SAME probe the emitted <c>_&lt;T&gt;</c> asserts use, so reflection
    /// and direct asserts can never disagree about a method set.
    /// </summary>
    /// <remarks>
    /// Go's EMPTY interface (<c>interface{}</c> / <c>any</c>) is emitted as <c>object</c>, which is
    /// not a CLR interface — but it IS the interface with no methods, and every Go type satisfies
    /// it. <see cref="Type.IsAssignableFrom"/> already answers that (boxing included); only the
    /// <see cref="Type.IsInterface"/> gate stood in the way, so <c>reflect.Type.AssignableTo</c> /
    /// <c>Implements</c> answered FALSE for every type against <c>interface{}</c> — gob's
    /// <c>decodeInterface</c> rejected every concrete value it had just decoded ("gob: int is not
    /// assignable to type interface {}", TestInterfaceBasic / TestInterfacePointer /
    /// TestNestedInterfaces).
    /// </remarks>
    public static bool GoImplements(Type? ifaceType, Type? valueType)
    {
        if (ifaceType is null || valueType is null)
            return false;

        if (!ifaceType.IsInterface && ifaceType != typeof(object))
            return false;

        return ifaceType.IsAssignableFrom(valueType) || valueType.StructurallyImplements(ifaceType);
    }

    /// <summary>
    /// Marshals a bridge Value's live source object for assignment into a destination slot of
    /// <paramref name="dstType"/> under Go assignability (identity, or interface-implements) —
    /// the write half of <c>reflect.Value.Set</c>. Returns false when Go would panic
    /// ("value of type X is not assignable to type Y").
    /// </summary>
    public static bool TryMarshalAssignable(object? src, Type dstType, out object? marshalled)
    {
        marshalled = null;

        // `any` destination — Go's EMPTY interface holds the value DIRECTLY, and every type is
        // assignable to it. System.Object reports IsInterface false (the same trap KindOf and
        // TryConvertTo already carry an explicit arm for), so without this the interface arm below
        // never fired and reflect.Value.Set into an `any` slot rejected every concrete value gob
        // had just decoded ("reflect.Set: value of type int is not assignable to type
        // interface {}" — TestInterfaceBasic / TestInterfacePointer / TestNestedInterfaces).
        if (dstType == typeof(object))
        {
            marshalled = src;
            return true;
        }

        // A valid-but-nil SOURCE: assignable to an interface destination (stays the nil
        // interface) and to any REFERENCE-typed slot (a null slot value IS the nil pointer/
        // func — X2.3 equality treats null-reference and the canonical nil box as one nil);
        // never to a value-typed slot (a nil-able container STRUCT's nil is its default value,
        // which is a non-null source).
        if (src is null)
        {
            if (dstType.IsInterface)
                return true;

            marshalled = null;
            return !dstType.IsValueType;
        }

        // Identity — including a pointer-sourced interface value unwrapping to its receiver box
        // (Go: the interface holds the *T) and the canonical typed-nil box of the same type.
        object dynamicSrc = src;

        while (dynamicSrc is IInterfaceAdapter { Value: not null } interfaceAdapter)
            dynamicSrc = interfaceAdapter.Value;

        if (dynamicSrc is IжAdapter { Box: not null } pointerAdapter)
            dynamicSrc = pointerAdapter.Box;
        else if (dynamicSrc is IValueAdapter { Value: not null } valueAdapter)
            dynamicSrc = valueAdapter.Value;

        if (dynamicSrc.GetType() == dstType)
        {
            marshalled = dynamicSrc;
            return true;
        }

        // Go assignability's named↔unnamed rule: identical UNDERLYING types with at least one
        // side unnamed. A raw value assigning into a NAMED wrapper slot constructs the wrapper
        // through its generated single-argument constructor (`reflect.New(TestPtrAlias.Elem())`
        // yields a raw *int; `Set` into a `type TestPtrAlias *int` slot wraps the SAME box, so
        // pointer identity and write-through survive); a named wrapper assigning into its raw
        // underlying slot unwraps. Two DIFFERENT named types never match either arm (Go-correct).
        ConstructorInfo? dstWrapperCtor = wrapperConstructorOf(dstType);

        if (dstWrapperCtor is not null && dstWrapperCtor.GetParameters()[0].ParameterType == dynamicSrc.GetType())
        {
            marshalled = dstWrapperCtor.Invoke([dynamicSrc]);
            return true;
        }

        if (TryUnwrapWrapperValue(dynamicSrc, out object? unwrappedSrc) && unwrappedSrc.GetType() == dstType)
        {
            marshalled = unwrappedSrc;
            return true;
        }

        // Interface destination: nominal instance passes through unchanged (preserving the
        // original interface value's identity); otherwise the golib assert machinery builds the
        // duck-typed wrapper — the same route emitted `_<T>` asserts take.
        if (dstType.IsInterface)
        {
            if (dstType.IsInstanceOfType(src))
            {
                marshalled = src;
                return true;
            }

            if (builtin.TryTypeAssert(src, dstType, out object? wrapped))
            {
                marshalled = wrapped;
                return true;
            }
        }

        return false;
    }

    // ==== Phase-3 increment 2: construction, conversion, size/dims, func shape, field projection ====
    // (Design + adversarial-review ledger: docs/Phase4/DESIGN-reflection-bridge-phase3-plan.md, I2.R.)

    // -------- zero construction (reflect.Zero / reflect.New / SetZero share ONE rule) --------

    /// <summary>
    /// The boxed Go ZERO value for a managed type: pointer kinds yield the canonical typed-nil
    /// instance (one nil encoding system-wide); interface/func kinds a null reference (Go's nil
    /// interface has no type; a nil func is a null delegate); slice/map/chan kinds their nil
    /// container STRUCT default (never <see cref="Activator"/> — the golib containers' explicit
    /// parameterless constructors allocate NON-nil backings); array kinds a backing sized from
    /// <paramref name="arrayDims"/> when known; everything else a default instance (whose field
    /// initializers ARE the Go zero — a blank `_ [4]byte` field materializes its length).
    /// </summary>
    public static object? ZeroValueOf(Type t, nint[]? arrayDims = null)
    {
        switch (KindOf(t))
        {
            case Pointer:
            case UnsafePointer:
                return CanonicalNilPointer(t);
            case Interface:
            case Func:
                return null;
            case String:
                // A NAMED string type's zero is the zero WRAPPER, not a raw @string (the slot
                // is wrapper-typed); raw @string keeps the explicit empty-string form.
                return t == typeof(@string) || t == typeof(string) ? (@string)"" : Activator.CreateInstance(t);
            case Slice:
            case Map:
            case Chan:
                return DefaultValueOf(t);
            case Array:
                return arrayDims is { Length: > 0 } && t.IsGenericType ? MakeSizedArray(t, arrayDims, 0) : DefaultValueOf(t);
            default:
                return Activator.CreateInstance(t);
        }
    }

    // Cached boxed default(T) — the nil form of the golib container STRUCTS (a null reference is
    // NOT the nil map/chan/slice; the zero struct is).
    private static readonly ConcurrentDictionary<Type, Func<object?>> s_defaultFactories = new();

    /// <summary>The boxed <c>default(T)</c> for a managed type (cached factory).</summary>
    public static object? DefaultValueOf(Type t)
    {
        return s_defaultFactories.GetOrAdd(t, static ct =>
            typeof(GoReflect).GetMethod(nameof(defaultOf), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(ct).CreateDelegate<Func<object?>>())();
    }

    private static object? defaultOf<T>()
    {
        return default(T);
    }

    /// <summary>
    /// Constructs a raw <c>array&lt;E&gt;</c> with real backing storage sized from a dims vector
    /// (nested dims build nested element factories, mirroring the converter's own
    /// <c>new(128, () =&gt; new(4))</c> field-initializer form).
    /// </summary>
    public static object MakeSizedArray(Type arrayType, nint[] dims, int level)
    {
        Type? elem = ElementType(arrayType);

        if (elem is null)
            throw new InvalidOperationException($"MakeSizedArray: {arrayType} has no element type.");

        if (level >= dims.Length - 1 || KindOf(elem) != Array)
            return Activator.CreateInstance(arrayType, dims[level])!;

        MethodInfo factoryMaker = typeof(GoReflect).GetMethod(nameof(sizedArrayElementFactory), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(elem);

        object elementFactory = factoryMaker.Invoke(null, [elem, dims, level + 1])!;

        return Activator.CreateInstance(arrayType, dims[level], elementFactory)!;
    }

    private static Func<E> sizedArrayElementFactory<E>(Type elemType, nint[] dims, int level)
    {
        return () => (E)MakeSizedArray(elemType, dims, level)!;
    }

    // -------- container construction (reflect.MakeSlice / MakeMap; named wrappers included) --------

    private static readonly ConcurrentDictionary<Type, Func<nint, nint, object>> s_containerMakers = new();

    /// <summary>
    /// Constructs a golib container (or a generated NAMED container wrapper) through its
    /// <c>ISupportMake</c> surface — the same construction <c>make()</c> emissions use, so
    /// <c>reflect.MakeSlice(namedSliceType, …)</c> yields the WRAPPER, exactly Go's named result.
    /// </summary>
    public static object MakeContainer(Type containerType, nint p1 = 0, nint p2 = -1)
    {
        return s_containerMakers.GetOrAdd(containerType, static ct =>
            typeof(GoReflect).GetMethod(nameof(makeSupported), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(ct).CreateDelegate<Func<nint, nint, object>>())(p1, p2);
    }

    private static object makeSupported<T>(nint p1, nint p2) where T : ISupportMake<T>
    {
        return T.Make(p1, p2)!;
    }

    // -------- pointer-box construction (reflect.New) --------

    private static readonly ConcurrentDictionary<Type, Func<object?, object>> s_boxMakers = new();

    /// <summary>A fresh heap box <c>ж&lt;T&gt;</c> holding <paramref name="value"/> — <c>reflect.New</c>'s allocation.</summary>
    public static object NewPointerBox(Type pointeeType, object? value)
    {
        return s_boxMakers.GetOrAdd(pointeeType, static pt =>
            typeof(GoReflect).GetMethod(nameof(newBox), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(pt).CreateDelegate<Func<object?, object>>())(value);
    }

    private static object newBox<T>(object? value)
    {
        return value is null ? new ж<T>(default(T)!) : new ж<T>((T)value);
    }

    // -------- Go convertibility (Set{Int,Uint,Float,Complex,String,Bool} + future Convert) --------

    /// <summary>
    /// Go's CONVERTIBILITY relation over boxed managed values — the single conversion rule the
    /// reflection Set* family routes through (assignability, then named-wrapper construction via
    /// the wrapper's generated single-argument constructor, then the kinded scalar conversions
    /// with Go semantics: integer stores TRUNCATE to the destination width, floats/complex narrow).
    /// </summary>
    public static bool TryConvertTo(object? src, Type dstType, out object? result)
    {
        result = null;

        // `any` destination: everything (including nil) passes through unchanged.
        if (dstType == typeof(object))
        {
            result = src;
            return true;
        }

        if (src is null)
            return dstType.IsInterface;

        if (TryMarshalAssignable(src, dstType, out result))
            return true;

        // A named-wrapper source converts through its underlying value first.
        if (TryUnwrapWrapperValue(src, out object? unwrapped) && TryConvertTo(unwrapped, dstType, out result))
            return true;

        // A named-wrapper destination constructs through its generated single-argument
        // constructor (parameter type discovered, never assumed primitive — golib struct
        // underlyings like @string/uintptr/complex64 included).
        ConstructorInfo? wrapperCtor = wrapperConstructorOf(dstType);

        if (wrapperCtor is not null)
        {
            Type underlying = wrapperCtor.GetParameters()[0].ParameterType;

            if (underlying != dstType && TryConvertTo(src, underlying, out object? underlyingValue))
            {
                result = wrapperCtor.Invoke([underlyingValue]);
                return true;
            }

            return false;
        }

        result = coerceScalar(KindOf(dstType), dstType, src);
        return result is not null;
    }

    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> s_wrapperConstructors = new();

    // The generated wrapper constructor taking the underlying value (never the NilType form).
    // Memoized WHOLE, not just its [GoType] gate: the answer is one per-type-immutable
    // ConstructorInfo, and TryConvertTo reaches this per VALUE from reflect.Value.Set /
    // SetMapIndex / Call / Convert — so the constructor scan was repeated per call too.
    private static ConstructorInfo? wrapperConstructorOf(Type t)
    {
        return s_wrapperConstructors.GetOrAdd(t, static type =>
        {
            if (goTypeMarkerOf(type) is null)
                return null;

            foreach (ConstructorInfo ctor in type.GetConstructors())
            {
                ParameterInfo[] parameters = ctor.GetParameters();

                if (parameters.Length == 1 && parameters[0].ParameterType != typeof(NilType))
                    return ctor;
            }

            return null;
        });
    }

    /// <summary>Unwraps a generated named-type wrapper value to its single underlying field value.</summary>
    public static bool TryUnwrapWrapperValue(object src, [NotNullWhen(true)] out object? underlying)
    {
        underlying = null;
        Type t = src.GetType();

        if (goTypeMarkerOf(t) is not { Definition.Length: > 0 } def || def.Definition == "dyn")
            return false;

        FieldInfo? valueField = t.GetField("m_value", BindingFlags.Instance | BindingFlags.NonPublic);

        if (valueField is null)
            return false;

        underlying = valueField.GetValue(src);
        return underlying is not null;
    }

    // Go scalar conversion into a destination kind: integers truncate (unchecked), floats and
    // complex narrow — exactly the reflect Set{Int,Uint,Float,Complex} contract (verified against
    // Go 1.23 reflect: no overflow panic on Set*).
    private static object? coerceScalar(int dstKind, Type dstType, object src)
    {
        switch (dstKind)
        {
            case Bool:
                return src is bool b ? b : null;
            case String:
                return src switch { @string gs => (object)gs, string s => (@string)s, _ => null };
            case Complex128:
                return src switch { Complex c => c, complex64 c64 => (Complex)c64, _ => null };
            case Complex64:
                return src switch
                {
                    Complex c => new complex64((float)c.Real, (float)c.Imaginary),
                    complex64 c64 => c64,
                    _ => null
                };
            case Float32:
                return tryWideFloat(src, out double f32) ? (float)f32 : null;
            case Float64:
                return tryWideFloat(src, out double f64) ? f64 : null;
        }

        if (!tryWideInteger(src, out long wide))
            return null;

        return dstKind switch
        {
            Int => (nint)wide,
            Int8 => (sbyte)wide,
            Int16 => (short)wide,
            Int32 => (int)wide,
            Int64 => wide,
            Uint => unchecked((nuint)wide),
            Uint8 => unchecked((byte)wide),
            Uint16 => unchecked((ushort)wide),
            Uint32 => unchecked((uint)wide),
            Uint64 => unchecked((ulong)wide),
            Uintptr => new uintptr(unchecked((nuint)wide)),
            _ => null
        };
    }

    private static bool tryWideInteger(object src, out long wide)
    {
        switch (src)
        {
            case long l: wide = l; return true;
            case ulong ul: wide = unchecked((long)ul); return true;
            case nint n: wide = n; return true;
            case nuint nu: wide = unchecked((long)nu); return true;
            case int i: wide = i; return true;
            case uint u: wide = u; return true;
            case short s: wide = s; return true;
            case ushort us: wide = us; return true;
            case sbyte sb: wide = sb; return true;
            case byte bt: wide = bt; return true;
            case uintptr up: wide = unchecked((long)up.Value); return true;
            case double d: wide = unchecked((long)d); return true;
            case float f: wide = unchecked((long)f); return true;
            default: wide = 0; return false;
        }
    }

    private static bool tryWideFloat(object src, out double wide)
    {
        switch (src)
        {
            case double d: wide = d; return true;
            case float f: wide = f; return true;
            default:
                bool ok = tryWideInteger(src, out long l);
                wide = l;
                return ok;
        }
    }

    // -------- Go sizes (descriptor Size_/Align_ stamping; binary's sizeof reads scalars only) --------

    /// <summary>
    /// The Go (amd64) size of the type <paramref name="t"/> represents, or -1 when it cannot be
    /// known (an array whose length the managed type does not carry). Struct sizes follow Go's
    /// alignment rules over the PROJECTED Go fields — best-effort composite fidelity, recorded;
    /// the demonstrated consumer (encoding/binary's sizeof) reads only the scalar kinds.
    /// NOTE: unsafe.Sizeof currently answers via Marshal.SizeOf — a separate rule; unification
    /// onto this one is deferred with a named consumer (I2.R R-14).
    /// </summary>
    public static nint GoSizeOf(Type t, nint[]? arrayDims = null)
    {
        switch (KindOf(t))
        {
            case Bool or Int8 or Uint8: return 1;
            case Int16 or Uint16: return 2;
            case Int32 or Uint32 or Float32: return 4;
            case Int or Uint or Int64 or Uint64 or Uintptr or Float64 or Complex64: return 8;
            case Complex128 or String or Interface: return 16;
            case Slice: return 24;
            case Pointer or UnsafePointer or Map or Chan or Func: return 8;
            case Array:
            {
                if (arrayDims is not { Length: > 0 })
                    return -1;

                nint elemSize = GoSizeOf(ElementType(t)!, arrayDims.Length > 1 ? arrayDims[1..] : null);
                return elemSize < 0 ? -1 : elemSize * arrayDims[0];
            }
            case Struct:
            {
                nint size = 0, maxAlign = 1;

                foreach (GoFieldInfo field in GoFields(t))
                {
                    nint[]? dims = KindOf(field.Type) == Array ? field.ArrayDims : null;
                    nint fieldSize = GoSizeOf(field.Type, dims);

                    if (fieldSize < 0)
                        return -1;

                    nint align = GoAlignOf(field.Type);
                    maxAlign = align > maxAlign ? align : maxAlign;
                    size = (size + align - 1) / align * align + fieldSize;
                }

                return (size + maxAlign - 1) / maxAlign * maxAlign;
            }
            default:
                return -1;
        }
    }

    /// <summary>The Go (amd64) alignment of a type (struct = max field alignment; array = element alignment).</summary>
    public static nint GoAlignOf(Type t)
    {
        switch (KindOf(t))
        {
            case Bool or Int8 or Uint8: return 1;
            case Int16 or Uint16: return 2;
            case Int32 or Uint32 or Float32 or Complex64: return 4;
            case Array: return ElementType(t) is { } elem ? GoAlignOf(elem) : 8;
            case Struct:
            {
                nint maxAlign = 1;

                foreach (GoFieldInfo field in GoFields(t))
                {
                    nint align = GoAlignOf(field.Type);
                    maxAlign = align > maxAlign ? align : maxAlign;
                }

                return maxAlign;
            }
            default:
                return 8;
        }
    }

    // -------- array dimension recovery (descriptor cargo; canonType interning is NOT widened) --------

    private static readonly ConcurrentDictionary<Type, object?> s_zeroInstances = new();

    /// <summary>
    /// The array dims of a LIVE array value (nested dims walk the first element), or null when
    /// unknown (a null/zero-length backing cannot reveal nested dims).
    /// </summary>
    public static nint[]? ArrayDimsOfValue(object? value)
    {
        if (value is not IArray arr)
            return null;

        nint length = arr.Length;
        Type? elem = ElementType(value.GetType());

        if (elem is null || KindOf(elem) != Array)
            return [length];

        if (length == 0)
            return null; // nested dims unknowable from an empty outer

        object? first = firstArrayElement(value, elem);
        nint[]? inner = ArrayDimsOfValue(first);

        return inner is null ? null : [length, .. inner];
    }

    private static object? firstArrayElement(object arrayValue, Type elemType)
    {
        MethodInfo reader = typeof(GoReflect).GetMethod(nameof(readFirstElement), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(elemType);
        return reader.Invoke(null, [arrayValue]);
    }

    private static object? readFirstElement<E>(object arrayValue)
    {
        return arrayValue is IArray<E> typed ? typed[0] : null;
    }

    /// <summary>
    /// The array dims of an array-typed STRUCT FIELD, recovered from a cached zero instance of
    /// the declaring struct — the converter emits the Go dimension as a field initializer
    /// (<c>= new(4)</c>, nested <c>new(128, () =&gt; new(4))</c>) that the generated parameterless
    /// constructor runs, so the dims are already in the emitted C# with no attribute needed.
    /// </summary>
    public static nint[]? FieldArrayDims(Type declaringType, FieldInfo field)
    {
        if (!declaringType.IsValueType)
            return null;

        object? zero = s_zeroInstances.GetOrAdd(declaringType, static t => Activator.CreateInstance(t));
        return zero is null ? null : ArrayDimsOfValue(field.GetValue(zero));
    }

    // -------- func shape (rtype.NumIn/In/NumOut/Out/IsVariadic; Value.Call) --------

    /// <summary>
    /// The Go func shape of a converted delegate type, derived from its <c>Invoke</c> signature:
    /// a <c>void</c> return is zero results, a <c>ValueTuple</c> return is Go's multi-return
    /// (a converted Go struct is never a ValueTuple, so the rule is unambiguous), anything else
    /// one result. A golib variadic family delegate (<c>Funcꓸꓸꓸ</c>/<c>Actionꓸꓸꓸ</c>, whose tail
    /// is <c>params Span&lt;T&gt;</c>) reports variadic with the tail as Go's <c>[]T</c>.
    /// </summary>
    public static bool TryFuncShape(Type delegateType, [NotNullWhen(true)] out Type[]? ins, [NotNullWhen(true)] out Type[]? outs, out bool isVariadic)
    {
        ins = null;
        outs = null;
        isVariadic = false;

        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            return false;

        MethodInfo? invoke = delegateType.GetMethod("Invoke");

        if (invoke is null)
            return false;

        ParameterInfo[] parameters = invoke.GetParameters();
        string name = delegateType.Name;
        isVariadic = name.StartsWith("Func" + EllipsisOperator, StringComparison.Ordinal) ||
                     name.StartsWith("Action" + EllipsisOperator, StringComparison.Ordinal);

        ins = new Type[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            ins[i] = parameters[i].ParameterType;

        if (isVariadic && ins.Length > 0 && ins[^1] is { IsGenericType: true } tail && tail.GetGenericTypeDefinition() == typeof(Span<>))
            ins[^1] = typeof(slice<>).MakeGenericType(tail.GetGenericArguments()[0]);

        Type ret = invoke.ReturnType;

        if (ret == typeof(void))
            outs = Type.EmptyTypes;
        else if (ret.IsGenericType && ret.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
            outs = ret.GetGenericArguments();
        else
            outs = [ret];

        return true;
    }

    // -------- Go struct-field projection (embeds, named-struct wrappers, blanks, companions) --------

    /// <summary>A struct's Go-visible field: its Go name, static Go type, exportedness, and access path.</summary>
    public readonly struct GoFieldInfo
    {
        /// <summary>The Go field name (`_` for a blank field, the embed's type name for an embedded field).</summary>
        public readonly string Name;

        /// <summary>The field's STATIC Go type (an embedded field reports the embedded type, not its backing box).</summary>
        public readonly Type Type;

        /// <summary>Go exportedness (uppercase first rune of <see cref="Name"/>).</summary>
        public readonly bool Exported;

        /// <summary>Array dims when <see cref="Type"/> is an array kind and the declaring zero instance reveals them.</summary>
        public readonly nint[]? ArrayDims;

        // The C# access path from the declaring struct: each step is an instance field; a step
        // whose IsBoxHop flag is set holds a ж<T> promoted-embed box the path derefs through.
        internal readonly FieldInfo[] Path;
        internal readonly bool[] BoxHop;

        internal GoFieldInfo(string name, Type type, nint[]? arrayDims, FieldInfo[] path, bool[] boxHop)
        {
            Name = name;
            Type = type;
            Exported = name.Length > 0 && name != "_" && char.IsUpper(name[0]);
            ArrayDims = arrayDims;
            Path = path;
            BoxHop = boxHop;
        }

        /// <summary>Reads this field's value from a live struct instance (path-following, boxed).</summary>
        public object? Read(object structValue)
        {
            object? current = structValue;

            for (int i = 0; i < Path.Length && current is not null; i++)
            {
                current = Path[i].GetValue(current);

                if (BoxHop[i] && current is not null)
                    current = ReadPointerSlot(current);
            }

            return current;
        }
    }

    private static readonly ConcurrentDictionary<Type, GoFieldInfo[]> s_goFields = new();

    /// <summary>
    /// The Go-visible fields of a converted struct type, in metadata order: unwraps a defined-
    /// type-over-struct wrapper's <c>m_value</c>, projects a promoted-embed backing box
    /// (<c>Ꮡʗ</c>-prefixed <c>ж&lt;T&gt;</c> field) as the embedded Go field of type <c>T</c>,
    /// maps the converter's blank renames (<c>_</c>, <c>__</c>, …) to Go's <c>"_"</c> (a REAL Go
    /// field named <c>__</c> is a documented exposure, same class as marker-shaped identifiers),
    /// and excludes compiler backing fields and <c>[GoReflectCompanion]</c>-marked bridge fields.
    /// </summary>
    public static GoFieldInfo[] GoFields(Type structType)
    {
        return s_goFields.GetOrAdd(structType, static t =>
        {
            List<GoFieldInfo> result = new();
            collectGoFields(t, [], [], result);
            return result.ToArray();
        });
    }

    private static void collectGoFields(Type t, FieldInfo[] prefixPath, bool[] prefixHops, List<GoFieldInfo> result)
    {
        FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // A defined-type-over-struct wrapper ([GoType("<underlying name>")], single private
        // m_value) exposes the UNDERLYING struct's fields on the named type in Go.
        if (fields is [{ Name: "m_value" } valueField] &&
            t.GetCustomAttributes(typeof(GoTypeAttribute), false) is [GoTypeAttribute { Definition.Length: > 0 } def] &&
            def.Definition != "dyn" && valueField.FieldType.IsValueType && KindOf(valueField.FieldType) == Struct)
        {
            collectGoFields(valueField.FieldType, [.. prefixPath, valueField], [.. prefixHops, false], result);
            return;
        }

        string embedPrefix = AddressPrefix + CapturedVarMarker;

        foreach (FieldInfo field in fields)
        {
            string name = field.Name;

            if (name.Contains("k__BackingField", StringComparison.Ordinal))
                continue;

            if (field.GetCustomAttributes(typeof(GoReflectCompanionAttribute), false).Length != 0)
                continue;

            // Promoted-embed backing box: `private readonly ж<T> ᏑʗName` → Go field `Name` of type T.
            if (name.StartsWith(embedPrefix, StringComparison.Ordinal) &&
                field.FieldType is { IsGenericType: true } boxType && boxType.GetGenericTypeDefinition() == typeof(ж<>))
            {
                string goName = name[embedPrefix.Length..];
                Type embedded = boxType.GetGenericArguments()[0];
                result.Add(new GoFieldInfo(goName, embedded, null, [.. prefixPath, field], [.. prefixHops, true]));
                continue;
            }

            string projected = name;

            if (projected.StartsWith(ShadowVarMarker, StringComparison.Ordinal))
                projected = projected[ShadowVarMarker.Length..];

            if (projected.Length > 0 && isAllUnderscores(projected))
                projected = "_";

            nint[]? dims = KindOf(field.FieldType) == Array ? FieldArrayDims(t, field) : null;
            result.Add(new GoFieldInfo(projected, field.FieldType, dims, [.. prefixPath, field], [.. prefixHops, false]));
        }
    }

    private static bool isAllUnderscores(string name)
    {
        foreach (char c in name)
        {
            if (c != '_')
                return false;
        }

        return true;
    }

    // -------- addressable alias boxes (Field(i)/Index(i) write-through; the ref-accessor contract) --------

    private static readonly ConcurrentDictionary<(Type boxType, string fieldKey), Delegate> s_fieldAccessors = new();
    private static readonly ConcurrentDictionary<Type, Func<object, Delegate, object>> s_fieldBoxMakers = new();
    private static readonly ConcurrentDictionary<(Type boxType, Type elemType), Func<object, nint, object>> s_elementBoxMakers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, int, object>> s_arrayElementBoxMakers = new();

    /// <summary>
    /// A field-alias <c>ж&lt;F&gt;</c> over a parent box's Go field: reads/writes route through the
    /// parent's <see cref="ж{T}.ValueSlot"/> and the projected field path, so nested parents
    /// (field-of-field, element parents) land in REAL storage — where <c>FieldRef&lt;T&gt;.Create</c>'s
    /// <c>m_val</c>-hardcoded IL would not. The cached accessor doubles as the ж equality-identity
    /// token, preserving Go's <c>&amp;s.f == &amp;s.f</c>.
    /// </summary>
    public static object FieldAliasBox(object parentBox, GoFieldInfo field)
    {
        Type boxType = parentBox.GetType();
        Delegate accessor = s_fieldAccessors.GetOrAdd((boxType, fieldPathKey(field)), _ => buildFieldAccessor(boxType, field));

        return s_fieldBoxMakers.GetOrAdd(field.Type, static ft =>
            typeof(GoReflect).GetMethod(nameof(makeFieldBox), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(ft).CreateDelegate<Func<object, Delegate, object>>())(parentBox, accessor);
    }

    private static object makeFieldBox<F>(object parentBox, Delegate accessor)
    {
        FieldRefFunc<F> fieldRef = (FieldRefFunc<F>)accessor;
        return new ж<F>(parentBox, fieldRef, accessor);
    }

    private static string fieldPathKey(GoFieldInfo field)
    {
        string key = "";

        for (int i = 0; i < field.Path.Length; i++)
            key += (field.BoxHop[i] ? "*" : ".") + field.Path[i].Name;

        return key;
    }

    // DynamicMethod: (object box) => ref ((ж<S>)box).ValueSlot.path... — each plain step is an
    // ldflda; a box-hop step loads the ж<E> reference and re-enters through ITS ValueSlot.
    private static Delegate buildFieldAccessor(Type boxType, GoFieldInfo field)
    {
        DynamicMethod method = new(
            name: $"goref_{field.Name}",
            returnType: field.Type.MakeByRefType(),
            parameterTypes: [typeof(object)],
            m: typeof(GoReflect).Module,
            skipVisibility: true);

        ILGenerator il = method.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, boxType);
        il.Emit(OpCodes.Callvirt, boxType.GetProperty(nameof(ж<int>.ValueSlot))!.GetGetMethod()!);

        for (int i = 0; i < field.Path.Length; i++)
        {
            if (!field.BoxHop[i])
            {
                il.Emit(OpCodes.Ldflda, field.Path[i]);
                continue;
            }

            il.Emit(OpCodes.Ldfld, field.Path[i]);
            il.Emit(OpCodes.Callvirt, field.Path[i].FieldType.GetProperty(nameof(ж<int>.ValueSlot))!.GetGetMethod()!);
        }

        il.Emit(OpCodes.Ret);

        return method.CreateDelegate(typeof(FieldRefFunc<>).MakeGenericType(field.Type));
    }

    /// <summary>
    /// An element-alias <c>ж&lt;E&gt;</c> over an ADDRESSABLE container box, via
    /// <see cref="ж{T}.at{TElem}(nint)"/> — which materializes a lazily-backed named-array
    /// wrapper on the REAL storage (the pallocBits lesson), validates the index, and yields a
    /// write-through element ref.
    /// </summary>
    public static object ElementAliasBoxOfBox(object containerBox, Type elemType, nint index)
    {
        Type boxType = containerBox.GetType();

        return s_elementBoxMakers.GetOrAdd((boxType, elemType), static key =>
        {
            Type containerType = key.boxType.GetGenericArguments()[0];
            return typeof(GoReflect).GetMethod(nameof(elementBoxViaAt), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(containerType, key.elemType).CreateDelegate<Func<object, nint, object>>();
        })(containerBox, index);
    }

    private static object elementBoxViaAt<C, E>(object box, nint index)
    {
        return ((ж<C>)box).at<E>(index);
    }

    /// <summary>
    /// An element-alias <c>ж&lt;E&gt;</c> over a DETACHED container VALUE (a slice result of
    /// <c>MakeSlice</c>, a slice read out of a slot): golib slices/arrays share their backing
    /// store across struct copies, so the ref still lands in real storage.
    /// </summary>
    public static object ElementAliasBoxOfValue(object containerValue, Type elemType, nint index)
    {
        return s_arrayElementBoxMakers.GetOrAdd(elemType, static et =>
            typeof(GoReflect).GetMethod(nameof(elementBoxOfArray), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(et).CreateDelegate<Func<object, int, object>>())(containerValue, (int)index);
    }

    private static object elementBoxOfArray<E>(object arrayValue, int index)
    {
        return new ж<E>((IArray)arrayValue, index);
    }

    private static readonly ConcurrentDictionary<Type, Func<object, nint, nint, object>> s_sliceWindowMakers = new();

    /// <summary>
    /// A <c>slice&lt;E&gt;</c> window <c>[low:high]</c> over a container value that SHARES the
    /// source's backing store (Go slice semantics — <c>reflect.Value.Slice</c>): a raw slice
    /// re-windows, a raw array wraps its backing, a named slice wrapper windows its underlying
    /// view.
    /// </summary>
    public static object SliceWindow(object container, Type elemType, nint low, nint high)
    {
        return s_sliceWindowMakers.GetOrAdd(elemType, static et =>
            typeof(GoReflect).GetMethod(nameof(sliceWindow), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(et).CreateDelegate<Func<object, nint, nint, object>>())(container, low, high);
    }

    private static object sliceWindow<E>(object container, nint low, nint high)
    {
        return container switch
        {
            slice<E> s => s.slice(low, high),
            array<E> a => new slice<E>(a, low, high),
            ISlice<E> view => new slice<E>(view).slice(low, high),
            _ => throw new InvalidOperationException($"SliceWindow: unsupported container {container.GetType()}")
        };
    }

    private static readonly ConcurrentDictionary<(Type keyType, Type elemType), Action<object, object?, object?>> s_mapSetters = new();

    /// <summary>
    /// Stores a key/value pair through a live golib map — raw <c>map&lt;K,V&gt;</c> and named map
    /// wrappers both implement <c>IDictionary&lt;K,V&gt;</c> (<c>reflect.Value.SetMapIndex</c>).
    /// </summary>
    public static void SetMapEntry(object map, Type keyType, Type elemType, object? key, object? value)
    {
        s_mapSetters.GetOrAdd((keyType, elemType), static k =>
            typeof(GoReflect).GetMethod(nameof(setMapEntry), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(k.keyType, k.elemType).CreateDelegate<Action<object, object?, object?>>())(map, key, value);
    }

    private static void setMapEntry<K, V>(object map, object? key, object? value) where K : notnull
    {
        ((IDictionary<K, V>)map)[(K)key!] = (V)value!;
    }

    // Reads a [GoType] definition string ("num:int32", "@string", "num:uintptr", ...) and maps its
    // underlying-kind token to a reflect.Kind. Returns false when the type carries no such definition
    // (a plain [GoType] struct, or a non-converted type).
    private static bool TryGoTypeDefinitionKind(Type t, out int kind)
    {
        kind = Invalid;

        if (goTypeMarkerOf(t) is not { } marker)
            return false;

        string def = marker.Definition;

        if (string.IsNullOrEmpty(def))
            return false; // a plain struct/interface marker — not a named-underlying wrapper

        string token = def.StartsWith("num:", StringComparison.Ordinal) ? def[4..] : def;

        kind = token switch
        {
            "bool" => Bool,
            // The converter renders the def token in the C# SPELLING of the underlying type, which
            // is not always the Go one, so every spelling that reaches here must map:
            //   * Go int/uint  -> nint/nuint   (num:nint is what `type X int` emits)
            //   * Go byte/rune -> byte/rune    (the predeclared aliases keep their own spelling,
            //     matching the `uint8`/`int32` aliases the generated csprojs declare)
            // A missing spelling is silent and broad: the wrapper falls through to Struct, so the
            // whole reflection bridge — fmt's %v, DeepEqual, encoding/* — sees a one-field struct.
            // `type nb byte` printed "{144}" instead of "144" (NarrowShiftVarCount).
            "int" or "nint" => Int, "int8" => Int8, "int16" => Int16, "int32" or "rune" => Int32, "int64" => Int64,
            "uint" or "nuint" => Uint, "uint8" or "byte" => Uint8, "uint16" => Uint16, "uint32" => Uint32, "uint64" => Uint64,
            "uintptr" => Uintptr,
            "float32" => Float32, "float64" => Float64,
            "complex64" => Complex64, "complex128" => Complex128,
            "@string" or "string" => String,
            _ => Invalid
        };

        return kind != Invalid;
    }
}
