//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:26:16 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class types2_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct StdSizes
        {
            // Constructors
            public StdSizes(NilType _)
            {
                this.WordSize = default;
                this.MaxAlign = default;
            }

            public StdSizes(long WordSize = default, long MaxAlign = default)
            {
                this.WordSize = WordSize;
                this.MaxAlign = MaxAlign;
            }

            // Enable comparisons between nil and StdSizes struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(StdSizes value, NilType nil) => value.Equals(default(StdSizes));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(StdSizes value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, StdSizes value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, StdSizes value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator StdSizes(NilType nil) => default(StdSizes);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static StdSizes StdSizes_cast(dynamic value)
        {
            return new StdSizes(value.WordSize, value.MaxAlign);
        }
    }
}}}}