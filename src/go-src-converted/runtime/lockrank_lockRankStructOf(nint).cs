//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:24:34 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct lockRank
        {
            // Value of the lockRank struct
            private readonly nint m_value;
            
            public lockRank(nint value) => m_value = value;

            // Enable implicit conversions between nint and lockRank struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator lockRank(nint value) => new lockRank(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(lockRank value) => value.m_value;
            
            // Enable comparisons between nil and lockRank struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(lockRank value, NilType nil) => value.Equals(default(lockRank));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(lockRank value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, lockRank value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, lockRank value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator lockRank(NilType nil) => default(lockRank);
        }
    }
}
