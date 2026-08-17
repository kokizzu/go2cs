// GoEqualityComparer.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;

namespace go;

/// <summary>
/// Projects Go value equality — <see cref="builtin.AreEqual(object?, object?)"/> — as an
/// <see cref="IEqualityComparer{T}"/>, for the containers that compare keys themselves.
/// </summary>
/// <typeparam name="T">Key type; an interface type or <see cref="object"/> (see <see cref="GoEqualityComparer.ForKeys{T}"/>).</typeparam>
/// <remarks>
/// <para>
/// Go compares interface values by (dynamic type, dynamic value), and that ONE relation serves both
/// <c>==</c> and map-key lookup — a Go map keyed by an interface finds an entry under exactly the
/// values <c>==</c> calls equal. In the conversion those two had diverged: emitted <c>==</c> routes
/// through <see cref="builtin.AreEqual(object?, object?)"/>, which unwraps the generated adapters, while
/// <c>map&lt;TKey, TValue&gt;</c>'s backing <see cref="Dictionary{TKey, TValue}"/> used the DEFAULT
/// comparer and compared the wrappers.
/// </para>
/// <para>
/// That gap is observable because an interface value's wrapper is not stable: the same Go dynamic value
/// is presented through whichever adapter the static interface it is currently held in calls for, so
/// asserting an <c>Object</c> to a narrower <c>dependency</c> yields a DIFFERENT wrapper object over the
/// same receiver box. Under the default comparer the asserted value could no longer find its own entry
/// in the map it came out of. That is the shape <c>go/types</c>' <c>initorder.dependencyGraph</c> is
/// built on — <c>M[dependency]</c> assembled from the keys of <c>objMap[Object]</c>, then <c>objMap</c>
/// indexed with the asserted value — where the missed lookup returned a nil <c>*declInfo</c> and the next
/// field access nil-panicked the whole type checker. Guarded by the <c>InterfaceAssertionMapKey</c>
/// behavioral test.
/// </para>
/// <para>
/// Deliberately delegating to <see cref="builtin.AreEqual(object?, object?)"/> rather than restating the
/// relation: that method is golib's single definition of Go equality, and it already carries the three
/// adapter tiers, the dynamic-type check and the IEEE-754 float rule. A second copy here is exactly the
/// drift this fix exists to remove.
/// </para>
/// </remarks>
internal sealed class GoEqualityComparer<T> : IEqualityComparer<T>
{
    /// <summary>Gets the singleton comparer for <typeparamref name="T"/>.</summary>
    public static readonly GoEqualityComparer<T> Default = new();

    private GoEqualityComparer()
    {
    }

    /// <inheritdoc />
    public bool Equals(T? x, T? y)
    {
        object? left = GoEqualityComparer.RootOf(x);
        object? right = GoEqualityComparer.RootOf(y);

        // Unwrapping is idempotent, so handing AreEqual the roots asks it exactly the question it
        // would have asked itself. The identity leg short-circuits the reflective operator lookup
        // for the dominant case — a key probed against itself — and cannot change an answer: two
        // references to one instance are equal under every tier below it.
        return ReferenceEquals(left, right) || builtin.AreEqual(left, right);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Hashes the UNWRAPPED root, which is what keeps the hash consistent with <see cref="Equals"/>:
    /// equal keys unwrap to values of one dynamic type that the type's own <c>==</c>/<c>Equals</c>
    /// calls equal, so they hash alike. It is also the rule the compile-time adapters
    /// <c>ImplementGenerator</c> emits already use (<c>m_box.GetHashCode()</c>) — the runtime shells
    /// simply never had it.
    /// </remarks>
    public int GetHashCode(T obj)
    {
        return GoEqualityComparer.RootOf(obj)?.GetHashCode() ?? 0;
    }
}

/// <summary>
/// Non-generic helpers for <see cref="GoEqualityComparer{T}"/>.
/// </summary>
internal static class GoEqualityComparer
{
    /// <summary>
    /// Gets the Go equality comparer to back a map keyed by <typeparamref name="T"/>, or <c>null</c> to
    /// keep <see cref="Dictionary{TKey, TValue}"/>'s default.
    /// </summary>
    /// <typeparam name="T">Map key type.</typeparam>
    /// <remarks>
    /// Scoped to the key types that can actually CARRY an adapter — an interface, or <c>any</c>. A
    /// concrete key (<c>@string</c>, an integer, a converted struct) is never wrapped, so it would
    /// answer identically while giving up <see cref="EqualityComparer{T}"/>'s devirtualized fast path;
    /// the test is a JIT-time constant per instantiation, so the choice costs nothing at run time.
    /// </remarks>
    public static IEqualityComparer<T>? ForKeys<T>()
    {
        if (typeof(T).IsInterface || typeof(T) == typeof(object))
            return GoEqualityComparer<T>.Default;

        // A FLOAT-kinded key needs Go's IEEE rule, which Dictionary's default comparer deliberately
        // does NOT use: BCL `Double.Equals` reports NaN equal to NaN so that a NaN stored in a
        // collection can be found again, while Go's map applies `==` unchanged — so a NaN key is
        // never equal to anything, INCLUDING an existing NaN key. `m[NaN] = 1` twice therefore
        // stores TWO entries in Go and one here, and in Go neither can ever be read back or deleted.
        // fmt's own TestSprintf reads that difference out: `%v` of `map[float64]int{NaN: 1, NaN: 1}`
        // printed `map[NaN:1]` against Go's `map[NaN:1 NaN:1]`.
        //
        // The comparers below are per-representation and non-boxing, so an ordinary float-keyed map
        // keeps a direct call where routing it through the interface arm above would box every
        // probe. `==` is the whole implementation, because C#'s float `==` IS the IEEE relation Go's
        // map applies; the hash stays the type's own, and a NaN that hashes consistently while
        // comparing unequal builds exactly the same-bucket/never-equal chain Go's map builds for it.
        //
        // Scoped to the raw representations, with the residual stated rather than covered
        // speculatively (the r39d rule): a NAMED float type's wrapper, and a struct or array that
        // CONTAINS a float, still compare through their generated equality, which inherits the BCL
        // rule. No measured consumer reaches those, and covering them would mean routing every
        // struct-keyed map through the reflective relation.
        if (typeof(T) == typeof(double))
            return (IEqualityComparer<T>)(object)s_float64Keys;

        if (typeof(T) == typeof(float))
            return (IEqualityComparer<T>)(object)s_float32Keys;

        if (typeof(T) == typeof(System.Numerics.Complex))
            return (IEqualityComparer<T>)(object)s_complex128Keys;

        if (typeof(T) == typeof(complex64))
            return (IEqualityComparer<T>)(object)s_complex64Keys;

        return null;
    }

    private static readonly Float64KeyComparer s_float64Keys = new();
    private static readonly Float32KeyComparer s_float32Keys = new();
    private static readonly Complex128KeyComparer s_complex128Keys = new();
    private static readonly Complex64KeyComparer s_complex64Keys = new();

    private sealed class Float64KeyComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => x == y;

        public int GetHashCode(double value) => value.GetHashCode();
    }

    private sealed class Float32KeyComparer : IEqualityComparer<float>
    {
        public bool Equals(float x, float y) => x == y;

        public int GetHashCode(float value) => value.GetHashCode();
    }

    private sealed class Complex128KeyComparer : IEqualityComparer<System.Numerics.Complex>
    {
        public bool Equals(System.Numerics.Complex x, System.Numerics.Complex y) => x == y;

        public int GetHashCode(System.Numerics.Complex value) => value.GetHashCode();
    }

    private sealed class Complex64KeyComparer : IEqualityComparer<complex64>
    {
        public bool Equals(complex64 x, complex64 y) => x == y;

        public int GetHashCode(complex64 value) => value.GetHashCode();
    }

    /// <summary>
    /// Unwraps the generated adapter tiers to the Go dynamic value beneath them.
    /// </summary>
    /// <param name="value">Value to unwrap.</param>
    /// <remarks>
    /// Mirrors the unwrap sequence at the head of <see cref="builtin.AreEqual(object?, object?)"/> —
    /// interface adapters to exhaustion, then the pointer tier, then the value tier — including its
    /// null guard on <see cref="IValueAdapter.Value"/>, which keeps an adapter over a nil named-func
    /// delegate in its wrapper view exactly as Go keeps such an interface non-nil.
    /// </remarks>
    public static object? RootOf(object? value)
    {
        while (value is IInterfaceAdapter interfaceAdapter)
            value = interfaceAdapter.Value;

        if (value is IжAdapter pointerAdapter)
            value = pointerAdapter.Box;

        if (value is IValueAdapter { Value: not null } valueAdapter)
            value = valueAdapter.Value;

        return value;
    }
}
