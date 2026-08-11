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
                return tryStructLayout(t, out _, out nint structSize) ? structSize : -1;
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
    /// Offsets and <see cref="GoSizeOf"/> run the SAME layout walk, so a descriptor's stamped
    /// <c>Size_</c> and its fields' <c>Offset</c>s can never disagree. The demonstrated consumer
    /// is internal/abi's synthesized <c>StructType()</c> specialization, which is what
    /// <c>unique.buildStructCloneSeq</c> walks to find the string offsets inside a value.
    /// </remarks>
    public static nint[]? GoFieldOffsets(Type t)
    {
        return KindOf(t) == Struct && tryStructLayout(t, out nint[] offsets, out _) ? offsets : null;
    }

    // The one Go struct layout walk: per-field aligned offsets plus the aligned total size.
    // False (with size -1) when any field's Go size is unknowable.
    private static bool tryStructLayout(Type t, out nint[] offsets, out nint size)
    {
        GoFieldInfo[] fields = GoFields(t);
        offsets = new nint[fields.Length];
        size = 0;
        nint maxAlign = 1;

        for (int i = 0; i < fields.Length; i++)
        {
            GoFieldInfo field = fields[i];
            nint[]? dims = KindOf(field.Type) == Array ? field.ArrayDims : null;
            nint fieldSize = GoSizeOf(field.Type, dims);

            if (fieldSize < 0)
            {
                offsets = [];
                size = -1;
                return false;
            }

            nint align = GoAlignOf(field.Type);
            maxAlign = align > maxAlign ? align : maxAlign;
            size = (size + align - 1) / align * align;
            offsets[i] = size;
            size += fieldSize;
        }

        size = (size + maxAlign - 1) / maxAlign * maxAlign;
        return true;
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
        nint[]?[]? dims = null;

        for (int i = 0; i < declared.Length; i++)
        {
            if (declared[i].GetCustomAttributes(typeof(GoArrayDimsAttribute), false) is not [GoArrayDimsAttribute { Dims.Length: > 0 } stamped])
                continue;

            dims ??= new nint[]?[declared.Length];
            nint[] parsed = new nint[stamped.Dims.Length];

            for (int j = 0; j < parsed.Length; j++)
                parsed[j] = stamped.Dims[j];

            dims[i] = parsed;
        }

        return dims;
    }
}
