// GoStructSynthesis.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using static go2cs.Symbols;

namespace go;

// ---------------------------------------------------------------------------------------------
// RUNTIME STRUCT SYNTHESIS — the one place a Go type is built for which no C# declaration exists.
//
// WHY THERE IS A FILE HERE AT ALL
//   Every other question the reflection bridge answers is a FUNCTION OF AN EXISTING System.Type:
//   KindOf, GoFields, structLayoutOf, GoTypeName, FieldAliasBox, ZeroValueOf. `reflect.StructOf`
//   is the one caller that asks for a Go type nothing declared, so it needs a System.Type that
//   does not exist yet — and the bridge has no synthesis direction. `PointerTo` and `ArrayOf` get
//   away with `MakeGenericType` because ж<T> and array<T> ARE the Go type; a struct has no generic
//   container to instantiate. So a real CLR value type is minted here, with System.Reflection.Emit.
//
// THE POINT IS THAT NOTHING DOWNSTREAM CHANGES
//   Once the type exists, `abi.synthType` describes it exactly as it describes a converted struct,
//   and GoFields / GoFieldOffsets / structLayoutOf / FieldAliasBox / ZeroValueOf /
//   haveIdenticalUnderlyingType / GoTypeName / canonType all run UNMODIFIED, because not one of
//   them asks where a System.Type came from. That is the whole argument for this mechanism over a
//   descriptor-only synthetic type: a synthetic answer and the converted answer cannot disagree
//   when there is only one path. See docs/phase4/DESIGN-reflect-structof.md §3.
//
// WHAT THE MINT HAS TO CARRY, AND WHY EACH PIECE
//   [GoType("dyn")] on the type          a StructOf result is a Go ANONYMOUS struct: Name() must be
//                                        "" and String() must render structurally. HasGoName and
//                                        GoTypeName both gate on this stamp.
//   a PARAMETERLESS CONSTRUCTOR          an ARRAY field's Go LENGTH. This is the piece that is easy
//                                        to get backwards: collectGoFields reads an array field's
//                                        dims from a cached ZERO INSTANCE (FieldArrayDims over
//                                        Activator.CreateInstance), because in converted code the
//                                        converter emits the length as a field INITIALIZER
//                                        (`= new(4)`) that the generated parameterless constructor
//                                        runs. A TypeBuilder struct has no field initializers, so
//                                        without this constructor every synthesized array field
//                                        would measure length ZERO — silently, 0 being a legal
//                                        length, and invisibly to encoding/gob's own depth-limit
//                                        test, which asserts on the DECODER's error over the wire
//                                        type graph and discards the encoder's. The [GoArrayDims]
//                                        stamp is NOT this route: it is the pointer-hop and
//                                        map-element-hop route, where no zero instance has anything
//                                        to measure. The two cover disjoint cases and both are
//                                        emitted.
//   [GoTag] on a field                   StructField.Tag (goTagOf).
//   [GoArrayDims] / [GoMapKeyDims]       the pointer-hop and map hops (FieldStampedDims /
//                                        FieldMapKeyDims) — stamped under exactly the converter's
//                                        own rule, see fieldCargoDims in src/go2cs/fieldDimsCargo.go.
//   a `ʗ`-prefixed CLR field name        StructField.Anonymous. collectGoFields decides embeddedness
//                                        from the NAME PREFIX and from nothing else, so an embed
//                                        emitted under its plain name is silently not embedded —
//                                        and a String()-based guard cannot see it, since an embedded
//                                        field and a same-named regular field render identically.
//   nesting in a `<pkg>_package` class   StructField.PkgPath for an unexported field, which
//                                        GoPackagePath derives from the DECLARING class's namespace
//                                        plus name and from nothing else.
//
// THE INTERN HOLDS THE MINT
//   Go's StructOf is interned — `StructOf(f) == StructOf(f)` is type identity, and encoding/gob
//   keys `map[reflect.Type]gobType` on the result, so a fresh descriptor per call would make every
//   recursion a cache miss. The intern here is a plain lock rather than a ConcurrentDictionary
//   factory ON PURPOSE: GetOrAdd runs its factory CONCURRENTLY on racing threads and discards the
//   losers' work, and the work here is DefineType, whose duplicate name THROWS (measured: 3 of 4
//   racing threads failed). So the mint itself is what the lock covers, not just the lookup.
//
// THE KEY IS NOT THE FIELD TYPES
//   `ArrayOf(1, int)` and `ArrayOf(2, int)` are ONE CLR type (array<nint>) — the length lives only
//   in descriptor cargo — so a shape key over System.Types alone would intern `struct{F [1]int}`
//   and `struct{F [2]int}` together. Each field therefore contributes its descriptor's dims
//   rendering, and that rendering is produced by abi.descriptorDimsKey and handed in
//   (GoSynthField.DimsKey) rather than restated here, so the two can never diverge. golib sits
//   BELOW internal/abi and cannot call it, which is why the string arrives as cargo.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// One Go struct field as <c>reflect.StructOf</c> hands it to <see cref="GoStructSynthesis"/>.
/// </summary>
/// <remarks>
/// <see cref="DimsKey"/> is the field's descriptor-cargo rendering from <c>abi.descriptorDimsKey</c>,
/// supplied by the caller because golib cannot reference <c>internal/abi</c>. It participates in the
/// shape key and in nothing else.
/// </remarks>
public readonly struct GoSynthField
{
    /// <summary>The GO field name — <c>"_"</c> for a blank field, the embedded type's name for an embed.</summary>
    public readonly string Name;

    /// <summary>The field's managed type (the type the descriptor carries, never a box).</summary>
    public readonly Type Type;

    /// <summary>The Go struct tag, or <c>""</c> when the field carries none.</summary>
    public readonly string Tag;

    /// <summary>Whether this is an EMBEDDED (Go <c>Anonymous</c>) field.</summary>
    public readonly bool Embedded;

    /// <summary>What <c>Elem()</c> hands down for this field's type — its own dims for an array, the pointee's or the map element's otherwise.</summary>
    public readonly nint[]? ArrayDims;

    /// <summary>What <c>Key()</c> hands down for a map field.</summary>
    public readonly nint[]? KeyDims;

    /// <summary>The <c>abi.descriptorDimsKey</c> rendering of this field's descriptor cargo.</summary>
    public readonly string DimsKey;

    public GoSynthField(string name, Type type, string tag, bool embedded, nint[]? arrayDims, nint[]? keyDims, string dimsKey)
    {
        Name = name;
        Type = type;
        Tag = tag;
        Embedded = embedded;
        ArrayDims = arrayDims;
        KeyDims = keyDims;
        DimsKey = dimsKey;
    }
}

/// <summary>
/// Mints a real CLR value type per synthesized Go struct type, so <c>reflect.StructOf</c> can hand
/// back a <c>reflect.Type</c> the rest of the bridge describes exactly as it describes a converted
/// struct.
/// </summary>
public static class GoStructSynthesis
{
    // Everything below is guarded by this one lock: the shape intern, the module, the pkgpath
    // containers, the seed table and the mint. It is held across DefineType/CreateType, which is
    // the point — see the header note on AM-4.
    private static readonly object s_mintLock = new();

    private static readonly Dictionary<string, Type> s_byShape = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, TypeBuilder> s_containers = new(StringComparer.Ordinal);
    private static readonly HashSet<string> s_createdContainers = new(StringComparer.Ordinal);

    private static ModuleBuilder? s_module;
    private static int s_nextTypeId;
    private static int s_nextSeedSlot;

    // The dynamic assembly is AssemblyBuilderAccess.Run — NOT RunAndCollect — because
    // collectibility is unreachable rather than unwanted: a dozen-plus Type-keyed caches in this
    // bridge (s_goFields, s_structLayouts, s_zeroInstances, s_descriptors, s_canonTypeCache, the
    // naming / method-set / field-accessor / adapter-binding caches) root every synthesized type
    // for the life of the process, so a collectible AssemblyLoadContext could never actually unload
    // one. Working set is indistinguishable between the two modes when measured in isolated
    // processes (27.0 MB either way at 10k types).
    private const string DynamicAssemblyName = "go2cs.SynthesizedStructs";

    /// <summary>
    /// The ONE dynamic module golib mints into — this file's structs and
    /// <see cref="GoDelegateSynthesis"/>'s func types alike.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The assembly NAME is the load-bearing part, and it is why there is one module rather than two:
    /// every converted csproj carries exactly one friend grant for a golib mint —
    /// <c>&lt;InternalsVisibleTo Include="go2cs.SynthesizedStructs" /&gt;</c>, emitted by the
    /// converter (<c>projectFileWriter.go</c>) and by the test-project template — which is what lets
    /// a minted type's field or signature name an <c>internal</c> converted type, i.e. an unexported
    /// Go type. A second dynamic assembly would need a second grant in every generated csproj and a
    /// corpus-wide regen to carry it, for nothing: type identity is (assembly, name), and the two
    /// mints name from separate counters under separate prefixes (<c>structᴛN</c>, <c>funcᴛN</c>).
    /// </para>
    /// <para>
    /// A monitor is reentrant, so the mint below reaches this while already holding the lock — which
    /// is correct, and is why the creation lives here rather than being duplicated at both callers.
    /// </para>
    /// </remarks>
    internal static ModuleBuilder SharedModule
    {
        get
        {
            lock (s_mintLock)
            {
                return s_module ??= AssemblyBuilder
                    .DefineDynamicAssembly(new AssemblyName(DynamicAssemblyName), AssemblyBuilderAccess.Run)
                    .DefineDynamicModule(DynamicAssemblyName);
            }
        }
    }

    // A synthesized ARRAY field's zero value, reached from the emitted parameterless constructor by
    // slot. It is a fresh value per call, never a shared prototype: ZeroValueOf builds real backing
    // storage for a dims-carrying array, and handing every instance one instance of it would alias
    // storage across values of the same synthesized type. Reads race with mints, so the table is
    // concurrent even though every WRITE happens under s_mintLock.
    private static readonly ConcurrentDictionary<int, FieldSeed> s_seeds = new();

    private readonly struct FieldSeed
    {
        public readonly Type Type;
        public readonly nint[] Dims;

        public FieldSeed(Type type, nint[] dims)
        {
            Type = type;
            Dims = dims;
        }
    }

    /// <summary>
    /// The zero value of a synthesized struct's array-kinded field — called from the emitted
    /// parameterless constructor, which is what makes <c>GoReflect.FieldArrayDims</c> able to
    /// measure the field's Go length off a zero instance.
    /// </summary>
    /// <remarks>
    /// Public because emitted IL calls it; it is not part of the synthesis API.
    /// </remarks>
    public static object? FieldSeedValue(int slot)
    {
        FieldSeed seed = s_seeds[slot];
        return GoReflect.ZeroValueOf(seed.Type, seed.Dims);
    }

    /// <summary>
    /// The single seam behind which all CLR type minting sits: returns the synthesized value type
    /// for a Go struct shape, minting it once and interning it thereafter.
    /// </summary>
    /// <param name="fields">The Go fields, in Go DECLARATION order.</param>
    /// <param name="pkgPath">
    /// The Go import path qualifying the struct's unexported fields, or <c>""</c> when every field
    /// is exported. A non-empty value costs one extra <c>DefineType</c> for the package container
    /// the field's <c>PkgPath</c> is derived from; the common case pays nothing.
    /// </param>
    public static Type SynthesizeStructType(ReadOnlySpan<GoSynthField> fields, string pkgPath)
    {
        string shape = shapeKey(fields, pkgPath);

        lock (s_mintLock)
        {
            if (s_byShape.TryGetValue(shape, out Type? existing))
                return existing;

            Type minted = mint(fields, pkgPath);
            s_byShape[shape] = minted;
            return minted;
        }
    }

    // Go interns a struct type on the ordered list of (name, pkgpath, type, tag, embedded). Two of
    // those need care here: the TYPE is not separated by its System.Type alone (a Go array's length
    // is descriptor cargo, so [1]int and [2]int share array<nint>), which is what DimsKey carries;
    // and the pkgpath is a property of the whole call, since Go requires every unexported field of
    // one StructOf to share it.
    private static string shapeKey(ReadOnlySpan<GoSynthField> fields, string pkgPath)
    {
        // Two separators no Go identifier, import path, tag or assembly-qualified type name can
        // contain, so no combination of field values can render as another shape's key.
        const char FieldSeparator = '\u0001';
        const char PartSeparator = '\u0002';

        StringBuilder builder = new(64);

        builder.Append(pkgPath).Append(FieldSeparator);

        foreach (GoSynthField field in fields)
        {
            builder.Append(field.Name).Append(PartSeparator)
                   .Append(field.Type.AssemblyQualifiedName ?? field.Type.FullName ?? field.Type.Name).Append(PartSeparator)
                   .Append(field.DimsKey).Append(PartSeparator)
                   .Append(field.Tag).Append(PartSeparator)
                   .Append(field.Embedded ? '1' : '0').Append(FieldSeparator);
        }

        return builder.ToString();
    }

    // Called under s_mintLock.
    private static Type mint(ReadOnlySpan<GoSynthField> fields, string pkgPath)
    {
        ModuleBuilder module = SharedModule;

        string typeName = "structᴛ" + (++s_nextTypeId).ToString(System.Globalization.CultureInfo.InvariantCulture);

        // SequentialLayout mirrors what the converter emits and is not load-bearing: Go offsets come
        // from structLayoutOf, never from the CLR's layout. Sealed + ValueType is: KindOf's last arm
        // classifies a value type as Struct and everything reference-typed that reaches it as
        // Pointer, so a class here would be a Go pointer.
        const TypeAttributes StructAttributes =
            TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit;

        TypeBuilder tb;
        TypeBuilder? container = null;

        if (pkgPath.Length == 0)
        {
            tb = module.DefineType("go.synth." + typeName, TypeAttributes.Public | StructAttributes, typeof(ValueType));
        }
        else
        {
            container = containerFor(pkgPath);
            tb = container.DefineNestedType(typeName, TypeAttributes.NestedPublic | StructAttributes, typeof(ValueType));
        }

        // The stamp that makes this a Go ANONYMOUS struct: Name() answers "", String() renders
        // structurally through goStructTypeString, and Type.IsDynamicType() enrols it in the
        // struct-to-struct dynamic conversion builtin.TryTypeAssert performs between unnamed
        // structs of the same shape — which is correct, a StructOf result BEING an anonymous
        // struct, and is guarded rather than merely argued (GolibTests).
        tb.SetCustomAttribute(new CustomAttributeBuilder(s_goTypeCtor, new object[] { "dyn" }));

        // Only the SEEDED fields are collected: the constructor below is the one thing emitted after
        // the field walk, and it touches nothing else.
        List<(FieldBuilder field, int slot)>? seeded = null;
        int blanks = 0;

        foreach (GoSynthField field in fields)
        {
            FieldBuilder fb = tb.DefineField(clrFieldName(field, ref blanks), field.Type, FieldAttributes.Public);

            if (field.Tag.Length > 0)
                fb.SetCustomAttribute(new CustomAttributeBuilder(s_goTagCtor, new object[] { field.Tag }));

            // The stamps mirror the converter's own rule (fieldCargoDims): a field that IS an array
            // carries its dims in the constructor below and is NOT stamped — stamping it would put
            // the same datum in two places — while a pointer hop's or a map's dims have no value to
            // measure and must be in the metadata.
            bool isArray = GoReflect.KindOf(field.Type) == GoReflect.Array;

            if (isArray)
            {
                if (field.ArrayDims is { Length: > 0 } dims)
                {
                    int slot = s_nextSeedSlot++;
                    s_seeds[slot] = new FieldSeed(field.Type, dims);
                    (seeded ??= new List<(FieldBuilder, int)>()).Add((fb, slot));
                }
            }
            else
            {
                if (field.ArrayDims is { Length: > 0 } stamped)
                    fb.SetCustomAttribute(new CustomAttributeBuilder(s_goArrayDimsCtor, new object[] { toLongDims(stamped) }));

                if (field.KeyDims is { Length: > 0 } keyStamped)
                    fb.SetCustomAttribute(new CustomAttributeBuilder(s_goMapKeyDimsCtor, new object[] { toIntDims(keyStamped) }));
            }
        }

        if (seeded is not null)
            emitZeroConstructor(tb, seeded);

        Type minted = tb.CreateType();

        // The container must be CREATED, once, or the nested type's DeclaringType cannot be loaded
        // and GoPackagePath throws instead of answering — measured. Creating it AFTER the nested
        // type is fine, and so is defining further nested types into it afterwards, which is what
        // makes one container per import path (rather than one per type) workable.
        if (container is not null && s_createdContainers.Add(pkgPath))
            container.CreateType();

        return minted;
    }

    // The Go declaration order IS the emitted field order, and CLR metadata order is DefineField
    // order, so GoFields reads them back in Go order with no reordering: reorderToGoDeclarationOrder
    // looks for an ALL-FIELDS constructor and finds only the parameterless one, so it keeps metadata
    // order — which is already right. (The draft of this design proposed emitting an all-fields
    // constructor for ordering; it is a no-op, and would have added a fragile bijection between
    // parameter names and field names for nothing.)
    private static string clrFieldName(GoSynthField field, ref int blanks)
    {
        // An EMBEDDED field is recognized downstream by its `ʗ` prefix and by nothing else — not by
        // an attribute, and not by name/type coincidence. Emitting an embed under its plain name
        // makes StructField.Anonymous silently false.
        if (field.Embedded)
            return CapturedVarMarker + field.Name;

        // Go permits repeated BLANK fields in one StructOf (its duplicate check exempts "_"), and
        // the CLR does not permit two fields of one name. The converter meets the same problem in
        // declared code and answers it the same way — `_`, `__`, `___` — and collectGoFields maps
        // any all-underscore name back to Go's "_".
        if (field.Name == "_")
            return new string('_', ++blanks);

        return field.Name;
    }

    // ldarg.0 / ldc.i4 slot / call FieldSeedValue / unbox.any <fieldType> / stfld <field>, per
    // seeded field. Activator.CreateInstance on a struct with a parameterless constructor RUNS it
    // (measured), which is the whole mechanism: FieldArrayDims then measures a real length off the
    // cached zero instance, exactly as it does for a converted struct's `= new(N)` initializer.
    private static void emitZeroConstructor(TypeBuilder tb, List<(FieldBuilder field, int slot)> seeded)
    {
        ConstructorBuilder cb = tb.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            Type.EmptyTypes);

        ILGenerator il = cb.GetILGenerator();

        foreach ((FieldBuilder field, int slot) in seeded)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, slot);
            il.Emit(OpCodes.Call, s_fieldSeedValue);
            il.Emit(OpCodes.Unbox_Any, field.FieldType);
            il.Emit(OpCodes.Stfld, field);
        }

        il.Emit(OpCodes.Ret);
    }

    // GoPackagePath(t) is GoPackageClassPath(t.DeclaringType), which is the declaring class's
    // NAMESPACE (minus the `go` emission root, dots to slashes) plus the class name with `_package`
    // trimmed. So the container for "encoding/gob" is class `gob_package` in namespace
    // `go.encoding` — NOT `go.encoding.gob.gob_package`, which is the obvious spelling and yields
    // "encoding/gob/gob".
    private static TypeBuilder containerFor(string pkgPath)
    {
        if (s_containers.TryGetValue(pkgPath, out TypeBuilder? existing))
            return existing;

        int slash = pkgPath.LastIndexOf('/');
        string parent = slash < 0 ? "" : pkgPath[..slash].Replace('/', '.');
        string pkg = slash < 0 ? pkgPath : pkgPath[(slash + 1)..];
        string name = parent.Length == 0 ? "go." + pkg + "_package" : "go." + parent + "." + pkg + "_package";

        TypeBuilder container = s_module!.DefineType(name,
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class);

        s_containers[pkgPath] = container;
        return container;
    }

    private static long[] toLongDims(nint[] dims)
    {
        long[] result = new long[dims.Length];

        for (int i = 0; i < dims.Length; i++)
            result[i] = dims[i];

        return result;
    }

    private static int[] toIntDims(nint[] dims)
    {
        int[] result = new int[dims.Length];

        for (int i = 0; i < dims.Length; i++)
            result[i] = (int)dims[i];

        return result;
    }

    private static readonly ConstructorInfo s_goTypeCtor = typeof(GoTypeAttribute).GetConstructor([typeof(string)])!;
    private static readonly ConstructorInfo s_goTagCtor = typeof(GoTagAttribute).GetConstructor([typeof(string)])!;
    private static readonly ConstructorInfo s_goArrayDimsCtor = typeof(GoArrayDimsAttribute).GetConstructor([typeof(long[])])!;
    private static readonly ConstructorInfo s_goMapKeyDimsCtor = typeof(GoMapKeyDimsAttribute).GetConstructor([typeof(int[])])!;
    private static readonly MethodInfo s_fieldSeedValue = typeof(GoStructSynthesis).GetMethod(nameof(FieldSeedValue), BindingFlags.Public | BindingFlags.Static)!;
}
