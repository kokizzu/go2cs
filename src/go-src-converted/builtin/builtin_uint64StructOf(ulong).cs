//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:42:40 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class builtin_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct uint64
        {
            // Value of the uint64 struct
            private readonly ulong m_value;
            
            public uint64(ulong value) => m_value = value;

            // Enable implicit conversions between ulong and uint64 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator uint64(ulong value) => new uint64(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ulong(uint64 value) => value.m_value;
            
            // Enable comparisons between nil and uint64 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(uint64 value, NilType nil) => value.Equals(default(uint64));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(uint64 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, uint64 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, uint64 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator uint64(NilType nil) => default(uint64);
        }
    }
}
