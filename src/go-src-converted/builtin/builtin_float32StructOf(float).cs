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
        private partial struct float32
        {
            // Value of the float32 struct
            private readonly float m_value;
            
            public float32(float value) => m_value = value;

            // Enable implicit conversions between float and float32 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator float32(float value) => new float32(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator float(float32 value) => value.m_value;
            
            // Enable comparisons between nil and float32 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(float32 value, NilType nil) => value.Equals(default(float32));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(float32 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, float32 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, float32 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator float32(NilType nil) => default(float32);
        }
    }
}
