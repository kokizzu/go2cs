//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:42:30 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace go {
namespace @internal
{
    public static partial class aliases_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct C6
        {
            // Value of the C6 struct
            private readonly C5 m_value;
            
            public C6(C5 value) => m_value = value;

            // Enable implicit conversions between C5 and C6 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator C6(C5 value) => new C6(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator C5(C6 value) => value.m_value;
            
            // Enable comparisons between nil and C6 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(C6 value, NilType nil) => value.Equals(default(C6));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(C6 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, C6 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, C6 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator C6(NilType nil) => default(C6);
        }
    }
}}}
