//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:22:41 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using src = go.cmd.@internal.src_package;
using fmt = go.fmt_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class ssa_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct lineRange
        {
            // Constructors
            public lineRange(NilType _)
            {
                this.first = default;
                this.last = default;
            }

            public lineRange(uint first = default, uint last = default)
            {
                this.first = first;
                this.last = last;
            }

            // Enable comparisons between nil and lineRange struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(lineRange value, NilType nil) => value.Equals(default(lineRange));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(lineRange value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, lineRange value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, lineRange value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator lineRange(NilType nil) => default(lineRange);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static lineRange lineRange_cast(dynamic value)
        {
            return new lineRange(value.first, value.last);
        }
    }
}}}}