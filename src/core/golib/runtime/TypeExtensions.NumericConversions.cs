// TypeExtensions.NumericConversions.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;

namespace go.golib;

// ---------------------------------------------------------------------------------------------
// NUMERIC CONVERSIONS — boxed scalar in, Go scalar out.
//
// WHAT LIVES HERE
//   The conversions the runtime needs when it holds a value only as `object`/`IConvertible` and
//   must produce the Go-typed form: ConvertToType (the element conversion behind `copy` between
//   compatible element types and slice base-type conversion), TryCastAsInteger (any boxed integral
//   widened to ulong), and the TypeCode numeric predicate the two rest on.
//
// WHY LADDERS OF CASES AND NOT REFLECTION
//   Each of these is a switch over a small closed set — TypeCode, or the boxed value's runtime
//   type — and every arm is a direct conversion. A `Convert.ChangeType`-style rewrite, or a
//   Type-keyed dictionary of converters, would replace a jump table with a hash probe plus a
//   delegate call on paths that run PER ELEMENT: `builtin.copy` reaches ConvertToType once for
//   every element it copies. The repetition is the point; leave it alone.
//
// THE `IsNumericType` LIST IS BROADER THAN GO'S "NUMERIC" — DELIBERATELY
//   It answers TRUE for Boolean and Decimal, neither of which is a Go numeric kind. This is a
//   .NET-side "does this TypeCode name a fixed-width scalar I can convert without allocating"
//   predicate, not a Go type classification. The Go-facing kind question is
//   `GoReflect.KindOf`/`IsComparable`, which never routes through here. Do not "fix" this list to
//   match Go: it would change what the conversion helpers accept without making any Go-visible
//   answer more correct.
//
// NOT TO BE CONFUSED WITH builtin.ConvertToType
//   `builtin.ConvertToType<T>` (builtin.TypeParamConversions.cs) is a different method with the
//   same name and the opposite direction of travel: it takes a raw integer and produces a value of
//   an unconstrained TYPE PARAMETER T, for converted generic code. This one takes an already-boxed
//   IConvertible and produces the boxed Go representation of whatever it already is. A search for
//   the name finds both; the parameter shape tells them apart.
// ---------------------------------------------------------------------------------------------
public static partial class TypeExtensions
{
    /// <summary>
    /// Returns a Go type equivalent to the specified value.
    /// </summary>
    /// <param name="value">An object that implements the <see cref="IConvertible" /> interface.</param>
    /// <returns>A Go type whose value is equivalent to <paramref name="value"/>.</returns>
    public static object ConvertToType<T>(in T? value) where T : IConvertible
    {
        if (value is null)
            return nil;

        return value.GetTypeCode() switch
        {
            TypeCode.Boolean => value.ToBoolean(null),
            TypeCode.Char => (rune)value.ToChar(null),
            TypeCode.SByte => value.ToSByte(null),
            TypeCode.Byte => value.ToByte(null),
            TypeCode.Int16 => value.ToInt16(null),
            TypeCode.UInt16 => value.ToUInt16(null),
            TypeCode.Int32 => value.ToInt32(null),
            TypeCode.UInt32 => value.ToUInt32(null),
            TypeCode.Int64 => value.ToInt64(null),
            TypeCode.UInt64 => value.ToUInt64(null),
            TypeCode.Single => value.ToSingle(null),
            TypeCode.Double => value.ToDouble(null),
            _ => (@string)value.ToString(null)
        };
    }

    /// <summary>
    /// Tries to cast input value as an integer.
    /// </summary>
    /// <param name="value">Value to try to cast.</param>
    /// <param name="integer">Casted value.</param>
    /// <returns><c>true</c> if cast succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryCastAsInteger(this object value, out ulong integer)
    {
        switch (value)
        {
            case char charVal:
                integer = charVal;
                return true;
            case bool boolVal:
                integer = boolVal ? 1UL : 0UL;
                return true;
            case sbyte sbyteVal:
                integer = (ulong)sbyteVal;
                return true;
            case byte byteVal:
                integer = byteVal;
                return true;
            case short shortVal:
                integer = (ulong)shortVal;
                return true;
            case ushort ushortVal:
                integer = ushortVal;
                return true;
            case int intVal:
                integer = (ulong)intVal;
                return true;
            case uint uintVal:
                integer = uintVal;
                return true;
            case long longVal:
                integer = (ulong)longVal;
                return true;
            case ulong ulongVal:
                integer = ulongVal;
                return true;
        }

        integer = 0;
        return false;
    }

    /// <summary>
    /// Tries to cast input value as an integer.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="value">Value to try to cast.</param>
    /// <param name="integer">Casted value.</param>
    /// <returns><c>true</c> if cast succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryCastAsInteger<T>(this T value, out ulong integer) where T : unmanaged, IConvertible
    {
        return ((object)value).TryCastAsInteger(out integer);
    }

    /// <summary>
    /// Determines if <see cref="IConvertible"/> <paramref name="value"/> is a numeric type.
    /// </summary>
    /// <param name="value">Value to check.</param>
    /// <returns><c>true</c> is <paramref name="value"/> is a numeric type; otherwise, <c>false</c>.</returns>
    public static bool IsNumeric(this IConvertible? value)
    {
        return value is not null && value.GetTypeCode().IsNumericType();
    }

    /// <summary>
    /// Determines if <paramref name="typeCode"/> is a numeric type, i.e., one of:
    /// <see cref="TypeCode.Boolean"/>, <see cref="TypeCode.SByte"/>, <see cref="TypeCode.Byte"/>,
    /// <see cref="TypeCode.Int16"/>, <see cref="TypeCode.UInt16"/>, <see cref="TypeCode.Int32"/>,
    /// <see cref="TypeCode.UInt32"/>, <see cref="TypeCode.Int64"/>, <see cref="TypeCode.UInt64"/>
    /// <see cref="TypeCode.Single"/>, <see cref="TypeCode.Double"/> or <see cref="TypeCode.Decimal"/>.
    /// </summary>
    /// <param name="typeCode"><see cref="TypeCode"/> value to check.</param>
    /// <returns><c>true</c> if <paramref name="typeCode"/> is a numeric type; otherwise, <c>false</c>.</returns>
    public static bool IsNumericType(this TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.Boolean => true,
            TypeCode.SByte => true,
            TypeCode.Byte => true,
            TypeCode.Int16 => true,
            TypeCode.UInt16 => true,
            TypeCode.Int32 => true,
            TypeCode.UInt32 => true,
            TypeCode.Int64 => true,
            TypeCode.UInt64 => true,
            TypeCode.Single => true,
            TypeCode.Double => true,
            TypeCode.Decimal => true,
            _ => false
        };
    }
}
