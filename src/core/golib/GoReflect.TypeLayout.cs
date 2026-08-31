// GoReflect.TypeLayout.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using go.golib;
using static go2cs.Symbols;

namespace go;

// ---------------------------------------------------------------------------------------------
// TYPE LAYOUT — the SHAPE facts a Go type descriptor carries: size, alignment, array dimensions,
// and a func's parameter and result lists.
//
// WHAT LIVES HERE
//   `GoSizeOf`/`GoAlignOf` (what gets stamped into a synthesized descriptor's `Size_`/`Align_`),
//   the array-dimension recovery those need, and `TryFuncShape` (`rtype.NumIn`/`In`/`NumOut`/
//   `Out`/`IsVariadic`, and `Value.Call`'s argument marshalling).
//
// THESE ARE GO'S NUMBERS, NOT THE CLR'S — THAT IS THE WHOLE POINT
//   `GoSizeOf` returns the amd64 size of the GO type, computed from Go's own rules, and it has to,
//   because the managed representation's real size is unrelated: a Go `[]T` is 24 bytes while
//   `slice<T>` is 32; a Go `[2]byte` is 2 while `array<byte>` is one reference to a backing store.
//   (`@string` happens to be 16 bytes like a Go `string` now that it carries an offset/length
//   window, but that is a coincidence of the window's shape, not something a caller may rely on.)
//   Anything that reads a size and expects Go's answer — encoding/binary's `sizeof` is the
//   demonstrated consumer — would get nonsense from `Marshal.SizeOf` or `Unsafe.SizeOf`. Struct
//   sizes follow Go's alignment rules over the PROJECTED Go fields (see GoReflect.FieldAccess.cs),
//   which is best-effort composite fidelity and recorded as such.
//
// THE WALK DESCENDS ONLY THROUGH STRUCTS AND ARRAYS, WHICH IS WHAT MAKES IT FINITE
//   Every other kind is a fixed-size header and is answered without looking inside it — a pointer,
//   slice, map, chan, interface or func field is 8/24/8/8/16/8 bytes whatever it refers to. That is
//   Go's own layout rule, and it is the entire termination argument: only value types recurse, and
//   C# forbids a value type from containing itself (CS0523). It held only once `KindOf` stopped
//   calling an unrecognized managed REFERENCE a struct — until 2026-08-15 a `sync.Mutex`'s
//   `SemaphoreSlim` gate sent this walk into the BCL's own object graph (`SemaphoreSlim` →
//   `TaskNode` → `TaskNode`), where it exhausted the stack and killed the process.
//
//   UNIFIED 2026-08-09 (r56a): `unsafe.Sizeof` now answers through THIS rule too (see
//   core/unsafe/unsafe.cs), so a Go size has one definition in the runtime rather than two. The
//   named consumer the deferral (I2.R R-14) was waiting for arrived as three packages at once —
//   debug/macho, internal/xcoff and go/internal/gccgoimporter all reach `unsafe.Sizeof` through
//   `internal/saferio.SliceCap[E]` with E bound to a managed type, where the old `Marshal.SizeOf`
//   rule does not merely disagree with Go, it throws.
//
// WHY DIMENSIONS ARE RECOVERED FROM A VALUE AND NOT READ FROM THE TYPE
//   `array<T>` carries its element type and not its LENGTH, so the managed type alone cannot tell
//   `[4]T` from `[]T` — which is why size, name and zero-construction all take an optional dims
//   vector rather than deriving one. Dims come from a live value (walking the first element for the
//   nested case) or, for a struct FIELD, from a cached zero instance of the declaring struct: the
//   converter emits the Go dimension as a field initializer (`= new(4)`, nested
//   `new(128, () => new(4))`) that the generated parameterless constructor runs, so the dimension
//   is already sitting in the emitted C# and needs no attribute. An empty outer array is the one
//   case that stays unknowable — there is no first element to ask — and it answers `null` rather
//   than guessing.
//
//   A func PARAMETER is the position where neither source exists — no value, no initializer — and
//   the emitted delegate type is a bare `Func<array<byte>, bool>` that `func([32]byte) bool` and
//   `func([64]byte) bool` share. That one position therefore DOES need an attribute, and gets it:
//   the converter stamps `[GoArrayDims(32)]` on the parameter and `FuncParamDims` reads it back off
//   the delegate INSTANCE. See GoArrayDimsAttribute.
//
// FUNC SHAPE IS READ OFF `Invoke`, AND THE MULTI-RETURN RULE IS UNAMBIGUOUS
//   A `void` return is zero Go results; a `ValueTuple` return is Go's multi-return, unpacked to one
//   result per element; anything else is one result. That rule is safe precisely because a
//   converted Go struct is NEVER emitted as a `ValueTuple` — the converter mints a named struct —
//   so a tuple in return position can only have come from a multi-return signature. Variadic is
//   detected from the golib variadic delegate families, whose `params Span<T>` tail is reported as
//   Go's `[]T`.
// ---------------------------------------------------------------------------------------------
public static partial class GoReflect
{
    // -------- Go sizes (descriptor Size_/Align_ stamping; binary's sizeof reads scalars only) --------

    /// <summary>
    /// The Go (amd64) size of the type <paramref name="t"/> represents, or -1 when it cannot be
    /// known (an array whose length the managed type does not carry). Struct sizes follow Go's
    /// alignment rules over the PROJECTED Go fields — best-effort composite fidelity, recorded;
    /// the demonstrated consumer (encoding/binary's sizeof) reads only the scalar kinds.
    /// <c>unsafe.Sizeof</c> answers through this same rule (unified 2026-08-09), so a descriptor's
    /// stamped size and an <c>unsafe.Sizeof</c> of the same type can never disagree.
    /// </summary>
    public static nint GoSizeOf(Type t, nint[]? arrayDims = null)
    {
        return goSizeOf(t, arrayDims, 0);
    }

    private static nint goSizeOf(Type t, nint[]? arrayDims, int depth)
    {
        if (depth > MaxLayoutDepth)
            return -1;

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

                nint elemSize = goSizeOf(ElementType(t)!, arrayDims.Length > 1 ? arrayDims[1..] : null, depth + 1);
                return elemSize < 0 ? -1 : elemSize * arrayDims[0];
            }
            case Struct:
                return structLayoutOf(t, depth).Size;
            default:
                return -1;
        }
    }

    /// <summary>
    /// The Go (amd64) byte OFFSET of each projected Go field of the struct type
    /// <paramref name="t"/>, in <see cref="GoFields"/> order — or <c>null</c> when
    /// <paramref name="t"/> is not a struct kind, or when any field's Go size cannot be known
    /// (an array whose length the managed type does not carry), since one unknown size makes
    /// every LATER offset a guess rather than an answer.
    /// </summary>
    /// <remarks>
    /// Offsets, <see cref="GoSizeOf"/> and <see cref="GoAlignOf"/> all read the SAME memoized layout
    /// pass, so a descriptor's stamped <c>Size_</c>, its <c>Align_</c> and its fields'
    /// <c>Offset</c>s can never disagree about one struct. The demonstrated consumer
    /// is internal/abi's synthesized <c>StructType()</c> specialization, which is what
    /// <c>unique.buildStructCloneSeq</c> walks to find the string offsets inside a value.
    /// </remarks>
    public static nint[]? GoFieldOffsets(Type t)
    {
        return KindOf(t) == Struct && structLayoutOf(t, 0) is { Size: >= 0 } layout ? layout.Offsets : null;
    }

    /// <summary>The Go (amd64) alignment of a type (struct = max field alignment; array = element alignment).</summary>
    public static nint GoAlignOf(Type t)
    {
        return goAlignOf(t, 0);
    }

    private static nint goAlignOf(Type t, int depth)
    {
        if (depth > MaxLayoutDepth)
            return 8;

        switch (KindOf(t))
        {
            case Bool or Int8 or Uint8: return 1;
            case Int16 or Uint16: return 2;
            case Int32 or Uint32 or Float32 or Complex64: return 4;
            case Array: return ElementType(t) is { } elem ? goAlignOf(elem, depth + 1) : 8;
            case Struct: return structLayoutOf(t, depth).Align;
            default: return 8;
        }
    }

    // -------- the one struct layout walk (offsets, size and alignment from a single pass) --------

    /// <summary>A struct's Go layout: per-field offsets, the aligned total size (-1 when unknowable), and the struct's own alignment.</summary>
    private readonly record struct StructLayout(nint[] Offsets, nint Size, nint Align);

    private static readonly ConcurrentDictionary<Type, StructLayout> s_structLayouts = new();

    // A recursion ceiling no LEGAL graph can reach, kept as a safety net rather than an algorithm.
    // Only Struct and Array recurse, Struct is answered for value types alone, and C# forbids a value
    // type from containing itself transitively (CS0523) — so the depth of this walk is bounded by a
    // real nesting depth the compiler already had to accept. Tripping the cap therefore means the
    // CLASSIFICATION is wrong somewhere, and the honest answer to that is "size unknown" (the r39d
    // rule: a descriptor field that cannot be read truthfully stays unpopulated), never a stack
    // overflow — which takes the whole process, and with it every verdict the run had not yet
    // produced. That is exactly what a managed reference classified as Struct once cost here.
    private const int MaxLayoutDepth = 128;

    // The one Go struct layout walk. Offsets, size and alignment come out of a SINGLE pass and are
    // memoized per type, so no two of them can describe different shapes, and a struct reached once
    // per field of every enclosing struct is walked once in total.
    //
    // Alignment is accumulated over every field even after a size becomes unknowable, because the two
    // questions are independent: an array whose dims the managed type does not carry has no knowable
    // size, while its alignment is its element's and stays an answer.
    private static StructLayout structLayoutOf(Type t, int depth)
    {
        if (s_structLayouts.TryGetValue(t, out StructLayout cached))
            return cached;

        if (depth > MaxLayoutDepth)
            return new StructLayout([], -1, 8);

        GoFieldInfo[] fields = GoFields(t);
        nint[] offsets = new nint[fields.Length];
        nint size = 0;
        nint maxAlign = 1;
        bool sizeKnown = true;

        for (int i = 0; i < fields.Length; i++)
        {
            GoFieldInfo field = fields[i];
            nint align = goAlignOf(field.Type, depth + 1);
            maxAlign = align > maxAlign ? align : maxAlign;

            if (!sizeKnown)
                continue;

            nint[]? dims = KindOf(field.Type) == Array ? field.ArrayDims : null;
            nint fieldSize = goSizeOf(field.Type, dims, depth + 1);

            if (fieldSize < 0)
            {
                sizeKnown = false;
                continue;
            }

            size = (size + align - 1) / align * align;
            offsets[i] = size;
            size += fieldSize;
        }

        StructLayout layout = sizeKnown
            ? new StructLayout(offsets, (size + maxAlign - 1) / maxAlign * maxAlign, maxAlign)
            : new StructLayout([], -1, maxAlign);

        s_structLayouts[t] = layout;
        return layout;
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

    /// <summary>
    /// The array dims of the value BEHIND a live pointer — <c>*[3]int</c> reports <c>[3]</c> — or
    /// null when <paramref name="value"/> is not a pointer to an array whose length a source knows.
    /// </summary>
    /// <remarks>
    /// A POINTER descriptor carries its POINTEE's dims unshifted (a pointer has no length of its
    /// own), which is the rule <c>abi.Type.Elem</c> and <c>rtype.Elem</c> already apply when they
    /// hand the cargo down. Nothing was populating it: <c>abi.TypeOf</c> measured dims for an ARRAY
    /// value only, so <c>reflect.TypeOf(new([3]int)).Elem()</c> described a dimension-LESS
    /// <c>[N]int</c> and <c>reflect.New</c> of it allocated a ZERO-length array. That is not a
    /// cosmetic loss — the fresh value then has a different Type from the one it is supposed to
    /// mirror, so <c>reflect.DeepEqual(new([3]int), reflect.New(typ).Interface())</c> is false, which
    /// is the precondition encoding/json's whole TestUnmarshal table checks before every subtest.
    /// </remarks>
    public static nint[]? PointeeArrayDims(object? value)
    {
        object? box = value;

        while (box is IInterfaceAdapter { Value: not null } interfaceAdapter)
            box = interfaceAdapter.Value;

        if (box is IжAdapter { Box: not null } pointerAdapter)
            box = pointerAdapter.Box;

        // A nil pointer has nothing to measure, and an opaque managed handle has no pointee at all
        // (the descent rule's value-side twin — see TryPointerBoxElement).
        if (box is null || box is INilPointer { IsNilPointer: true } ||
            !TryPointerBoxElement(box.GetType(), out Type? pointee) || KindOf(pointee) != Array)
        {
            return null;
        }

        return ArrayDimsOfValue(ReadPointerSlot(box));
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

    /// <summary>
    /// The array dims a STRUCT FIELD carries as a converter STAMP — <c>[GoArrayDims]</c>, the dims
    /// the field's descriptor hands down through <c>Elem()</c> — or null when the field carries none.
    /// </summary>
    /// <remarks>
    /// The stamp exists for the two hops <see cref="FieldArrayDims"/>' zero-instance route cannot
    /// see: a nil <c>ж&lt;array&lt;T&gt;&gt;</c> has no pointee to measure, and a nil map has no
    /// entry whose element could reveal a length. Both are ordinary shapes at a DECODE target —
    /// encoding/gob reaches <c>*[3]float64</c> and <c>map[[2]string][2]*float64</c> fields through
    /// <c>reflect.Type.Field(i)</c> on a struct nothing has populated yet — so the datum has to be in
    /// the emitted C#, exactly as it is for a func parameter.
    /// </remarks>
    public static nint[]? FieldStampedDims(FieldInfo field)
    {
        return field.GetCustomAttributes(typeof(GoArrayDimsAttribute), false) is [GoArrayDimsAttribute { Dims.Length: > 0 } stamped]
            ? toNintDims(stamped.Dims)
            : null;
    }

    /// <summary>
    /// The array dims of a map-typed STRUCT FIELD's KEY, from the converter's
    /// <c>[GoMapKeyDims]</c> stamp — what <c>reflect.Type.Key()</c> hands down — or null when the
    /// field carries none.
    /// </summary>
    public static nint[]? FieldMapKeyDims(FieldInfo field)
    {
        return field.GetCustomAttributes(typeof(GoMapKeyDimsAttribute), false) is [GoMapKeyDimsAttribute { Dims.Length: > 0 } stamped]
            ? toNintDims(stamped.Dims)
            : null;
    }

    private static nint[] toNintDims(int[] dims)
    {
        nint[] result = new nint[dims.Length];

        for (int i = 0; i < dims.Length; i++)
            result[i] = dims[i];

        return result;
    }

    // The 64-bit twin, for [GoArrayDims]. A Go array length is Go's `int`, 64-bit on a 64-bit
    // platform, and the standard library uses the full range — runtime's `*[1<<50 - 1]byte`
    // unbounded-array idiom. nint is already the destination width here, so the widening costs
    // nothing: this overload exists because the CARRIER widened, not because the target did.
    private static nint[] toNintDims(long[] dims)
    {
        nint[] result = new nint[dims.Length];

        for (int i = 0; i < dims.Length; i++)
            result[i] = (nint)dims[i];

        return result;
    }

    // -------- channel direction (abi.Type.ChanDir; rtype.String; assignability's chan arm) --------
    //
    // The same cargo shape as the array dims above, at the same finite set of positions, and for
    // the same reason: a Go channel's DIRECTION is part of its type, and `chan T`, `chan<- T` and
    // `<-chan T` all emit as one `channel<T>`. Each source below answers for the one position the
    // others cannot reach — a live value, the value behind a pointer, and a struct field's
    // initializer-borne zero. Unstamped is not a failure: it is a channel nothing narrowed, and
    // the bridge reports it as bidirectional, which is what it has always reported.

    /// <summary>
    /// The Go channel direction carried by a LIVE channel value, or
    /// <see cref="GoChanDir.Unstamped"/> when the value is not a channel or no source stamped one.
    /// </summary>
    public static GoChanDir ChanDirOfValue(object? value)
    {
        object? box = value;

        while (box is IInterfaceAdapter { Value: not null } interfaceAdapter)
            box = interfaceAdapter.Value;

        return box is IChannel channel ? channel.Direction : GoChanDir.Unstamped;
    }

    /// <summary>
    /// The Go channel direction of the value BEHIND a live pointer — <c>*chan&lt;- string</c>
    /// reports <see cref="GoChanDir.Send"/> — or <see cref="GoChanDir.Unstamped"/> when there is
    /// no such pointee.
    /// </summary>
    /// <remarks>
    /// A POINTER descriptor carries its pointee's direction unshifted, exactly as it carries the
    /// pointee's array dims (a pointer has no direction of its own), so <c>Elem()</c> hands the
    /// cargo straight down. This is the position <c>new(chan&lt;- string)</c> occupies, and the
    /// reason a NIL channel has to be able to carry a direction at all: the pointee is the zero
    /// value of a directional type, so there is no core to read and no length to measure — only
    /// the direction the converter seeded into the value.
    /// </remarks>
    public static GoChanDir PointeeChanDir(object? value)
    {
        object? box = value;

        while (box is IInterfaceAdapter { Value: not null } interfaceAdapter)
            box = interfaceAdapter.Value;

        if (box is IжAdapter { Box: not null } pointerAdapter)
            box = pointerAdapter.Box;

        if (box is null || box is INilPointer { IsNilPointer: true } ||
            !TryPointerBoxElement(box.GetType(), out Type? pointee) || KindOf(pointee) != Chan)
        {
            return GoChanDir.Unstamped;
        }

        return ChanDirOfValue(ReadPointerSlot(box));
    }

    /// <summary>
    /// The Go channel direction of a channel-typed STRUCT FIELD, recovered from a cached zero
    /// instance of the declaring struct — the converter emits the direction as a field initializer
    /// (<c>= channel&lt;@string&gt;.SendOnly</c>) that the generated parameterless constructor runs,
    /// which is the same route <see cref="FieldArrayDims"/> takes for an array field's length.
    /// </summary>
    public static GoChanDir FieldChanDir(Type declaringType, FieldInfo field)
    {
        if (!declaringType.IsValueType)
            return GoChanDir.Unstamped;

        object? zero = s_zeroInstances.GetOrAdd(declaringType, static t => Activator.CreateInstance(t));
        return zero is null ? GoChanDir.Unstamped : ChanDirOfValue(field.GetValue(zero));
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

        ins = new Type[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            ins[i] = parameters[i].ParameterType;

        // A trailing `Span<T>` IS the variadic tail, and testing for it is what makes this exact.
        // The golib variadic delegate families (`Funcꓸꓸꓸ`/`Actionꓸꓸꓸ`) are only ONE of the shapes a
        // converted variadic func value takes: a declared `func(string, ...int)` used as a method
        // group in an `any` position acquires C#'s NATURAL delegate type instead, whose name carries
        // no family marker at all — so the name test reported it non-variadic and `In(1)` handed back
        // a raw `Span<int>`, which then rendered as `func(string, Span\`1)`. A `Span<T>` parameter
        // cannot arise any other way in converted code (Go has no such type and the converter emits
        // one only for a variadic tail), so the shape test subsumes the name test rather than
        // widening it.
        bool spanTail = ins.Length > 0 && ins[^1] is { IsGenericType: true } tail &&
                        tail.GetGenericTypeDefinition() == typeof(Span<>);

        string name = delegateType.Name;
        isVariadic = spanTail ||
                     name.StartsWith("Func" + EllipsisOperator, StringComparison.Ordinal) ||
                     name.StartsWith("Action" + EllipsisOperator, StringComparison.Ordinal);

        if (spanTail)
            ins[^1] = typeof(slice<>).MakeGenericType(ins[^1].GetGenericArguments()[0]);

        Type ret = invoke.ReturnType;

        if (ret == typeof(void))
            outs = Type.EmptyTypes;
        else if (IsValueTuple(ret))
            outs = FlattenValueTuple(ret);
        else
            outs = [ret];

        return true;
    }

    /// <summary>Whether <paramref name="type"/> is a <c>System.ValueTuple</c> instantiation.</summary>
    private static bool IsValueTuple(Type type)
    {
        return type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// The Go RESULT list a multi-return delegate's tuple carries, flattened through any nesting.
    /// </summary>
    /// <remarks>
    /// A ValueTuple holds at most SEVEN values inline; an eighth generic argument is TRest, itself a
    /// ValueTuple carrying the remainder, and the nesting repeats. <c>GetGenericArguments</c> alone
    /// therefore answers the tuple's SHAPE, not Go's result list: for a nine-result func it returns
    /// eight entries whose last is <c>ValueTuple&lt;T8, T9&gt;</c> — one short, and with a type in the
    /// final slot that is not a Go result at all.
    ///
    /// That is a wrong ANSWER rather than a refusal, which is what made it costly: reflect's
    /// <c>NumOut</c>/<c>Out</c> read this list, and MakeFunc compares <c>len(results)</c> against it,
    /// so a Go func returning nine values met "reflect: wrong return count from function created by
    /// MakeFunc" — an error naming the caller's own return statement for a miscount this made.
    /// Measured on reflect's TestReflectMakeFuncCallABI, the suite's largest mismatch family.
    ///
    /// Walking TRest is the whole fix, and it is the exact mirror of the packing side in
    /// reflect/makefunc_impl.cs — one side reads the shape, the other builds it, and they have to
    /// agree about where the seam is.
    /// </remarks>
    private static Type[] FlattenValueTuple(Type tuple)
    {
        // Count first, then fill: two cheap walks over a chain that is at most a few links long,
        // and no collection type this file does not already import.
        int count = 0;
        Type level = tuple;

        while (true)
        {
            Type[] elements = level.GetGenericArguments();

            // Eight arguments means the last is TRest — but only when it is itself a tuple. A
            // legitimate eight-element shape whose final argument is an ordinary type is not a nest
            // and must be taken whole.
            if (elements.Length == 8 && IsValueTuple(elements[7]))
            {
                count += 7;
                level = elements[7];
                continue;
            }

            count += elements.Length;
            break;
        }

        Type[] flattened = new Type[count];
        int next = 0;
        level = tuple;

        while (true)
        {
            Type[] elements = level.GetGenericArguments();

            if (elements.Length == 8 && IsValueTuple(elements[7]))
            {
                for (int i = 0; i < 7; i++)
                    flattened[next++] = elements[i];

                level = elements[7];
                continue;
            }

            for (int i = 0; i < elements.Length; i++)
                flattened[next++] = elements[i];

            return flattened;
        }
    }

    // -------- variadic invocation (Value.Call over a `params Span<T>` tail) --------

    /// <summary>
    /// The most fixed parameters a variadic func value can carry ahead of its tail — golib's
    /// <c>Actionꓸꓸꓸ</c>/<c>Funcꓸꓸꓸ</c> families stop there, mirroring the BCL Action/Func arities.
    /// </summary>
    public const int MaxVariadicFixedParameters = 8;

    // The two families, indexed BY FIXED-PARAMETER COUNT: entry `n` takes n fixed parameters plus
    // the tail (and, for Func, the result). See golib variadic.cs for the declarations.
    private static readonly Type[] s_variadicActionFamily =
    [
        typeof(Actionꓸꓸꓸ<>), typeof(Actionꓸꓸꓸ<,>), typeof(Actionꓸꓸꓸ<,,>), typeof(Actionꓸꓸꓸ<,,,>),
        typeof(Actionꓸꓸꓸ<,,,,>), typeof(Actionꓸꓸꓸ<,,,,,>), typeof(Actionꓸꓸꓸ<,,,,,,>),
        typeof(Actionꓸꓸꓸ<,,,,,,,>), typeof(Actionꓸꓸꓸ<,,,,,,,,>)
    ];

    private static readonly Type[] s_variadicFuncFamily =
    [
        typeof(Funcꓸꓸꓸ<,>), typeof(Funcꓸꓸꓸ<,,>), typeof(Funcꓸꓸꓸ<,,,>), typeof(Funcꓸꓸꓸ<,,,,>),
        typeof(Funcꓸꓸꓸ<,,,,,>), typeof(Funcꓸꓸꓸ<,,,,,,>), typeof(Funcꓸꓸꓸ<,,,,,,,>),
        typeof(Funcꓸꓸꓸ<,,,,,,,,>), typeof(Funcꓸꓸꓸ<,,,,,,,,,>)
    ];

    // One per delegate TYPE: the family type its instances rebind onto, and the closed trampoline
    // that performs the typed call. Both are functions of the type alone, so they cache together.
    private sealed record VariadicInvoker(Type FamilyType, Func<Delegate, object?[], Array, object?> Call);

    private static readonly ConcurrentDictionary<Type, VariadicInvoker> s_variadicInvokers = new();

    /// <summary>
    /// Calls a converted Go VARIADIC func value whose tail is already packed into
    /// <paramref name="tail"/> (a <c>TArg[]</c> of the tail's element type), and returns the raw
    /// delegate result — <c>null</c> for a no-result func, a <c>ValueTuple</c> for a Go
    /// multi-return, exactly the shape the non-variadic path receives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because NO reflective invoke path can call one. A converted Go variadic lowers
    /// its tail to <c>params Span&lt;TArg&gt;</c> (see golib variadic.cs), <c>Span&lt;T&gt;</c> is a
    /// ref struct, and both <see cref="Delegate.DynamicInvoke"/> and <see cref="MethodBase.Invoke"/>
    /// marshal their arguments through <c>object?[]</c> — which a ref struct cannot enter. Nor can
    /// an expression tree stand in: <c>System.Linq.Expressions</c> rejects a byref-like type
    /// outright, so the sibling method-value binder's <c>Expression.Lambda</c> approach
    /// (GoReflect.MethodSets.cs) does not generalize here.
    /// </para>
    /// <para>
    /// So the call is made in TYPED code — one small generic trampoline per family arity, closed
    /// over the delegate's own parameter types by <c>MakeGenericMethod</c> and cached as an ordinary
    /// delegate (the <c>elementBoxViaAt</c> idiom in GoReflect.FieldAccess.cs). Inside a trampoline
    /// the tail is a <c>TArg[]</c> and its conversion to <c>Span&lt;TArg&gt;</c> is an ordinary
    /// one, so nothing is ever boxed.
    /// </para>
    /// <para>
    /// The trampoline casts to the golib FAMILY delegate, and the value being called is not always
    /// one: a variadic func literal in an <c>any</c> slot, or a declared variadic used as a method
    /// group there, acquires C#'s NATURAL delegate type instead — the same shape difference
    /// <see cref="TryFuncShape"/> had to stop reading off the type NAME. Those rebind onto the
    /// family, which is exact rather than a best-effort match: the family's type arguments are
    /// constructed FROM this delegate's own <c>Invoke</c> signature, so the two agree by
    /// construction and only the nominal type differs. A delegate that already IS its family type
    /// skips the rebind.
    /// </para>
    /// <para>
    /// The rebind RETARGETS through <c>Invoke</c> — the family delegate closes over the original
    /// delegate as its receiver — rather than re-binding the original's own target and method. That
    /// is what makes it total. A delegate the BRIDGE ITSELF built is expression-compiled
    /// (<c>Value.Method</c> binds a receiver that way, and a variadic method value lands here), and
    /// a compiled lambda's <c>Method</c> is not a runtime <see cref="MethodInfo"/>, which
    /// <see cref="Delegate.CreateDelegate(Type, object, MethodInfo)"/> rejects outright
    /// ("MethodInfo must be a runtime MethodInfo object"). <c>Invoke</c> is always a real method on
    /// a real delegate type, so this form has no such blind spot — and it carries a multicast
    /// invocation list intact, which re-binding a single target and method silently would not.
    /// </para>
    /// <para>
    /// A direct typed call also means a panic inside the callee propagates natively, rather than
    /// arriving wrapped in a <see cref="TargetInvocationException"/> the caller must unwrap.
    /// </para>
    /// </remarks>
    public static object? InvokeVariadic(Delegate del, object?[] fixedArgs, Array tail)
    {
        Type delegateType = del.GetType();
        VariadicInvoker invoker = s_variadicInvokers.GetOrAdd(delegateType, static t => buildVariadicInvoker(t));
        Delegate bound = delegateType == invoker.FamilyType ? del : rebindToVariadicFamily(del, invoker.FamilyType);

        return invoker.Call(bound, fixedArgs, tail);
    }

    private static VariadicInvoker buildVariadicInvoker(Type delegateType)
    {
        MethodInfo? invoke = delegateType.GetMethod("Invoke");
        ParameterInfo[] parameters = invoke?.GetParameters() ?? [];

        if (invoke is null || parameters.Length == 0 ||
            parameters[^1].ParameterType is not { IsGenericType: true } tailParameter ||
            tailParameter.GetGenericTypeDefinition() != typeof(Span<>))
        {
            throw new NotImplementedException(
                $"reflect: '{GoTypeName(delegateType)}' is not a variadic func value (its Invoke has no Span<T> tail)");
        }

        int fixedCount = parameters.Length - 1;
        bool hasResult = invoke.ReturnType != typeof(void);

        if (fixedCount > MaxVariadicFixedParameters)
        {
            throw new NotImplementedException(
                $"reflect: calling a variadic func value with {fixedCount} fixed parameters is not implemented — golib's " +
                $"Actionꓸꓸꓸ/Funcꓸꓸꓸ families stop at {MaxVariadicFixedParameters} (no demonstrated consumer beyond that)");
        }

        // <T1..Tn, TArg[, TResult]> — the family's type arguments ARE this delegate's own parameter
        // types, which is what makes the rebind above exact rather than a signature search.
        Type[] typeArguments = new Type[fixedCount + (hasResult ? 2 : 1)];

        for (int i = 0; i < fixedCount; i++)
            typeArguments[i] = parameters[i].ParameterType;

        typeArguments[fixedCount] = tailParameter.GetGenericArguments()[0];

        if (hasResult)
            typeArguments[^1] = invoke.ReturnType;

        Type familyType = (hasResult ? s_variadicFuncFamily : s_variadicActionFamily)[fixedCount].MakeGenericType(typeArguments);

        MethodInfo trampoline = typeof(GoReflect)
            .GetMethod($"{(hasResult ? "callVariadicFunc" : "callVariadicAction")}{fixedCount}", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeArguments);

        return new VariadicInvoker(familyType, trampoline.CreateDelegate<Func<Delegate, object?[], Array, object?>>());
    }

    private static Delegate rebindToVariadicFamily(Delegate del, Type familyType)
    {
        try
        {
            return Delegate.CreateDelegate(familyType, del, "Invoke");
        }
        catch (Exception ex) when (ex is ArgumentException or MethodAccessException or MissingMethodException)
        {
            throw new NotImplementedException(
                $"reflect: the variadic func value '{GoTypeName(del.GetType())}' could not be rebound onto golib's " +
                $"'{familyType.Name}' family delegate", ex);
        }
    }

    // The eighteen trampolines. Each makes the same three moves — cast to the family type, unbox the
    // fixed arguments, hand the tail array over as a Span — and differs only in arity, so they are
    // written out rather than generated: the closed set IS golib's variadic family (variadic.cs).

    private static object? callVariadicAction0<TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<TArg>)d)(new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction1<T1, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, TArg>)d)((T1)a[0]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction2<T1, T2, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, TArg>)d)((T1)a[0]!, (T2)a[1]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction3<T1, T2, T3, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, T3, TArg>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction4<T1, T2, T3, T4, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, T3, T4, TArg>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction5<T1, T2, T3, T4, T5, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, T3, T4, T5, TArg>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction6<T1, T2, T3, T4, T5, T6, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, T3, T4, T5, T6, TArg>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, (T6)a[5]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction7<T1, T2, T3, T4, T5, T6, T7, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, T3, T4, T5, T6, T7, TArg>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, (T6)a[5]!, (T7)a[6]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicAction8<T1, T2, T3, T4, T5, T6, T7, T8, TArg>(Delegate d, object?[] a, Array t)
    { ((Actionꓸꓸꓸ<T1, T2, T3, T4, T5, T6, T7, T8, TArg>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, (T6)a[5]!, (T7)a[6]!, (T8)a[7]!, new Span<TArg>((TArg[])t)); return null; }

    private static object? callVariadicFunc0<TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<TArg, TResult>)d)(new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc1<T1, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, TArg, TResult>)d)((T1)a[0]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc2<T1, T2, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc3<T1, T2, T3, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, T3, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc4<T1, T2, T3, T4, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, T3, T4, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc5<T1, T2, T3, T4, T5, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, T3, T4, T5, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc6<T1, T2, T3, T4, T5, T6, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, T3, T4, T5, T6, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, (T6)a[5]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc7<T1, T2, T3, T4, T5, T6, T7, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, T3, T4, T5, T6, T7, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, (T6)a[5]!, (T7)a[6]!, new Span<TArg>((TArg[])t)); }

    private static object? callVariadicFunc8<T1, T2, T3, T4, T5, T6, T7, T8, TArg, TResult>(Delegate d, object?[] a, Array t)
    { return ((Funcꓸꓸꓸ<T1, T2, T3, T4, T5, T6, T7, T8, TArg, TResult>)d)((T1)a[0]!, (T2)a[1]!, (T3)a[2]!, (T4)a[3]!, (T5)a[4]!, (T6)a[5]!, (T7)a[6]!, (T8)a[7]!, new Span<TArg>((TArg[])t)); }

    /// <summary>
    /// The per-parameter Go array dimensions of a converted func VALUE — one entry per parameter,
    /// null where that parameter is not a fixed-size array — or null when nothing is carried.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dimension of a `[32]byte` parameter cannot be read from the delegate TYPE (see the file
    /// header), so the converter stamps it on the parameter as <see cref="GoArrayDimsAttribute"/>
    /// and this reads it back off the delegate's target method. <c>Delegate.Method</c> resolves to
    /// the real declaration for every shape go2cs emits — a declared func used as a method group, a
    /// non-capturing lambda, a capturing lambda's display-class method, and a natural-typed lambda —
    /// so one read covers them all.
    /// </para>
    /// <para>
    /// The arity guard is what keeps it honest. A delegate whose target method's parameter list does
    /// not line up one-for-one with <c>Invoke</c>'s — an OPEN instance delegate carries the receiver
    /// as an extra leading parameter, and the bridge's own method values are expression-compiled
    /// closures with no attributes at all — is answered <c>null</c> rather than mis-indexed. That is
    /// the r39d rule in its usual form: a descriptor field that cannot be read truthfully stays
    /// unpopulated, and a dims-less array descriptor is a state the bridge already handles.
    /// </para>
    /// </remarks>
    public static nint[]?[]? FuncParamDims(object? funcValue)
    {
        if (funcValue is not Delegate d || d.Method is not { } method)
            return null;

        ParameterInfo[] declared = method.GetParameters();

        if (declared.Length == 0 || d.GetType().GetMethod("Invoke") is not { } invoke ||
            invoke.GetParameters().Length != declared.Length)
        {
            return null;
        }

        return paramDims(declared);
    }

    /// <summary>
    /// The per-parameter Go array dimensions of the <paramref name="index"/>'th method of
    /// <paramref name="t"/>'s method set — the same cargo <see cref="FuncParamDims"/> reads for a
    /// func value, for the func type <c>reflect.Type.Method(i).Type</c> reports.
    /// </summary>
    /// <remarks>
    /// A method needs its own reader because its func type is built from the method TABLE
    /// (<see cref="GoMethodFuncType"/> over the <c>MethodInfo</c>'s parameters) and never passes
    /// through a delegate instance, so the delegate route above has nothing to read. No arity guard
    /// is owed here for the same reason: the delegate type is synthesized FROM this parameter list,
    /// receiver included, so the indices line up with <c>In(i)</c> by construction — which is Go's
    /// own shape for a method type, receiver first.
    /// </remarks>
    public static nint[]?[]? MethodParamDims(Type? t, int index)
    {
        return paramDims(MethodAt(t, index).Method.GetParameters());
    }

    private static nint[]?[]? paramDims(ParameterInfo[] declared)
    {
        nint[]?[]? dims = null;

        for (int i = 0; i < declared.Length; i++)
        {
            if (declared[i].GetCustomAttributes(typeof(GoArrayDimsAttribute), false) is not [GoArrayDimsAttribute { Dims.Length: > 0 } stamped])
                continue;

            dims ??= new nint[]?[declared.Length];
            dims[i] = toNintDims(stamped.Dims);
        }

        return dims;
    }
}
