//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:17:42 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using @unsafe = go.@unsafe_package;

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct gclink
        {
            // Constructors
            public gclink(NilType _)
            {
                this.next = default;
            }

            public gclink(gclinkptr next = default)
            {
                this.next = next;
            }

            // Enable comparisons between nil and gclink struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(gclink value, NilType nil) => value.Equals(default(gclink));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(gclink value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, gclink value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, gclink value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator gclink(NilType nil) => default(gclink);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static gclink gclink_cast(dynamic value)
        {
            return new gclink(value.next);
        }
    }
}