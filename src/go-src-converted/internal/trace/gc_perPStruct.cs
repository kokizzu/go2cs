//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 05:52:57 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using heap = go.container.heap_package;
using math = go.math_package;
using sort = go.sort_package;
using strings = go.strings_package;
using time = go.time_package;
using go;

#nullable enable

namespace go {
namespace @internal
{
    public static partial class trace_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct perP
        {
            // Constructors
            public perP(NilType _)
            {
                this.gc = default;
                this.series = default;
            }

            public perP(long gc = default, long series = default)
            {
                this.gc = gc;
                this.series = series;
            }

            // Enable comparisons between nil and perP struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(perP value, NilType nil) => value.Equals(default(perP));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(perP value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, perP value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, perP value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator perP(NilType nil) => default(perP);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static perP perP_cast(dynamic value)
        {
            return new perP(value.gc, value.series);
        }
    }
}}