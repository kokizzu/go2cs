//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:23:59 UTC
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
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct ErrorHandling
        {
            // Value of the ErrorHandling struct
            private readonly nint m_value;
            
            public ErrorHandling(nint value) => m_value = value;

            // Enable implicit conversions between nint and ErrorHandling struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ErrorHandling(nint value) => new ErrorHandling(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(ErrorHandling value) => value.m_value;
            
            // Enable comparisons between nil and ErrorHandling struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ErrorHandling value, NilType nil) => value.Equals(default(ErrorHandling));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ErrorHandling value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ErrorHandling value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ErrorHandling value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ErrorHandling(NilType nil) => default(ErrorHandling);
        }
    }
}