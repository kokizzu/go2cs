//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:32:22 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace @internal
{
    public static partial class objabi_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct count
        {
            // Value of the count struct
            private readonly nint m_value;
            
            public count(nint value) => m_value = value;

            // Enable implicit conversions between nint and count struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator count(nint value) => new count(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(count value) => value.m_value;
            
            // Enable comparisons between nil and count struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(count value, NilType nil) => value.Equals(default(count));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(count value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, count value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, count value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator count(NilType nil) => default(count);
        }
    }
}}}