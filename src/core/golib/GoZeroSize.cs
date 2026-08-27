// GoZeroSize.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable StaticMemberInGenericType

using System;
using System.Reflection;

namespace go;

/// <summary>
/// Whether <typeparamref name="T"/> is a Go ZERO-SIZE type — one whose values carry no state and
/// therefore occupy no storage — and, when it is, the single shared element every value of it is.
/// </summary>
/// <remarks>
/// <para>
/// Go's <c>struct{}</c> (and any struct built only from such fields) has size 0, so
/// <c>make([]struct{}, n)</c> allocates NOTHING for any <c>n</c> up to <c>math.MaxInt</c> —
/// <c>mallocgc(0, …)</c> returns the address of the runtime's global <c>zerobase</c> and charges no
/// malloc. That is not a corner case dressed up as one: <c>slices.Concat</c>, <c>slices.Repeat</c> and
/// their tests use <c>[]struct{}</c> precisely BECAUSE it lets them exercise the length arithmetic at
/// <c>MaxInt</c> without touching memory. golib allocated a real backing array for the same
/// expression and panicked <c>makeslice: len out of range</c> at
/// <see cref="Array.MaxLength"/> — a length ceiling Go does not have here, because Go has nothing to
/// allocate.
/// </para>
/// <para>
/// <b>The predicate is a SHAPE question, not a size question.</b> <c>Unsafe.SizeOf&lt;T&gt;()</c>
/// cannot answer it: C# gives an empty struct one byte, so every zero-size Go type measures 1 here.
/// What survives the conversion faithfully is the FIELD SET — a Go struct is zero-size exactly when it
/// has no fields of nonzero size, and the emitted C# struct carries the same fields — so the
/// classification asks that instead, recursively. A type with no instance fields at all is the base
/// case and the common one (<see cref="EmptyStruct"/>, and every <c>[GoType] partial struct noCopy
/// { }</c> the converter emits for a named <c>struct{}</c>).
/// </para>
/// <para>
/// <b>Known and deliberate divergence:</b> Go's OTHER zero-size shape is the zero-length array
/// (<c>[0]T</c>, and <c>[N]struct{}</c>). go2cs emits a Go array as <see cref="array{T}"/>, whose
/// backing is a managed reference field, so such a type classifies here as NON-zero-size and keeps the
/// allocating path. That is the honest answer for the representation as it stands rather than a gap
/// papered over: nothing in the converted corpus builds a slice of them at a length no array could
/// hold, and claiming zero-size for a type whose C# shape genuinely carries a reference would put a
/// wrong element ref in front of every consumer.
/// </para>
/// </remarks>
/// <typeparam name="T">Element type to classify.</typeparam>
internal static class GoZeroSizeFacts<T>
{
    /// <summary>
    /// Whether <typeparamref name="T"/> occupies no storage in Go. A <c>static readonly</c> per closed
    /// <typeparamref name="T"/>, so every gate written against it folds at JIT time and no ordinary
    /// element type pays for the branch.
    /// </summary>
    internal static readonly bool IsZeroSize = Classify(typeof(T));

    /// <summary>
    /// The ONE element every zero-size value of <typeparamref name="T"/> is — golib's
    /// <c>zerobase</c>. It is also the non-null backing a storage-free slice carries, so
    /// <c>s == nil</c> stays false for a <c>make</c>d one exactly as it does for every other slice.
    /// Non-zero-size types get <see cref="Array.Empty{T}"/> and never read it.
    /// </summary>
    internal static readonly T[] Storage = IsZeroSize ? new T[1] : [];

    private static bool Classify(Type type)
    {
        // A reference is a pointer-sized value in Go's terms and in .NET's; a primitive, enum or
        // pointer has a width by definition. Only a struct can be zero-size.
        if (!type.IsValueType || type.IsPrimitive || type.IsEnum || type.IsPointer)
            return false;

        // A generic type parameter reaching here would be an open type — it cannot, since T is
        // always closed at the point a static generic field initializes.
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!Classify(field.FieldType))
                return false;
        }

        // No instance fields, or every one of them zero-size: Go's own rule, and the recursion
        // terminates because a struct cannot contain itself.
        return true;
    }
}
