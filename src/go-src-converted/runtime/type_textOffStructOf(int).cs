//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:27:31 UTC
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
        private partial struct textOff
        {
            // Value of the textOff struct
            private readonly int m_value;
            
            public textOff(int value) => m_value = value;

            // Enable implicit conversions between int and textOff struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator textOff(int value) => new textOff(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator int(textOff value) => value.m_value;
            
            // Enable comparisons between nil and textOff struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(textOff value, NilType nil) => value.Equals(default(textOff));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(textOff value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, textOff value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, textOff value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator textOff(NilType nil) => default(textOff);
        }
    }
}
