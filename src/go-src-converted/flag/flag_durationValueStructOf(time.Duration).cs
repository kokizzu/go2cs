//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:38:25 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class flag_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct durationValue
        {
            // Value of the durationValue struct
            private readonly time.Duration m_value;
            
            public durationValue(time.Duration value) => m_value = value;

            // Enable implicit conversions between time.Duration and durationValue struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator durationValue(time.Duration value) => new durationValue(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator time.Duration(durationValue value) => value.m_value;
            
            // Enable comparisons between nil and durationValue struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(durationValue value, NilType nil) => value.Equals(default(durationValue));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(durationValue value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, durationValue value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, durationValue value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator durationValue(NilType nil) => default(durationValue);
        }
    }
}
