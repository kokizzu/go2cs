// GoArrayDimsAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Carries the Go DIMENSION of a fixed-size-array parameter, outermost first
/// (<c>[4][8]byte</c> ⇒ <c>[GoArrayDims(4, 8)]</c>).
/// </summary>
/// <remarks>
/// <para>
/// A Go array's length is part of its TYPE, and it is the one part the managed emission cannot
/// carry: <c>[32]byte</c> renders as <see cref="array{T}"/> and C# has no const generic parameter
/// to hold the 32. Everywhere else the reflection bridge recovers it from a live source instead —
/// a value reveals its own length (<c>GoReflect.ArrayDimsOfValue</c>), and a struct FIELD recovers
/// it from the declaring type's zero instance, because the converter emits the dimension as a field
/// initializer (<c>= new(32)</c>) the generated parameterless constructor runs.
/// </para>
/// <para>
/// A func PARAMETER has neither: there is no value at a type-only position and no initializer to
/// read, and the emitted delegate type is a bare <c>Func&lt;array&lt;byte&gt;, bool&gt;</c> shared by
/// <c>func([32]byte) bool</c> and <c>func([64]byte) bool</c> alike. So the parameter position is
/// where the datum has to live. <c>GoReflect.FuncParamDims</c> reads it back off the delegate
/// INSTANCE (<c>Delegate.Method.GetParameters()</c>), which resolves for every shape go2cs emits —
/// a declared func used as a method group, a non-capturing lambda, a capturing lambda's
/// display-class method, and a natural-typed lambda — and <c>abi.TypeOf</c> stamps it as descriptor
/// cargo so <c>reflect.Type.In(i)</c> hands out an array type that knows its length.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class GoArrayDimsAttribute(params int[] dims) : Attribute
{
    /// <summary>The Go array dimensions, outermost first.</summary>
    public int[] Dims { get; } = dims;
}
