//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:27:24 UTC
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
        private partial struct traceBufPtr
        {
            // Value of the traceBufPtr struct
            private readonly System.UIntPtr m_value;
            
            public traceBufPtr(System.UIntPtr value) => m_value = value;

            // Enable implicit conversions between System.UIntPtr and traceBufPtr struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator traceBufPtr(System.UIntPtr value) => new traceBufPtr(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator System.UIntPtr(traceBufPtr value) => value.m_value;
            
            // Enable comparisons between nil and traceBufPtr struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(traceBufPtr value, NilType nil) => value.Equals(default(traceBufPtr));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(traceBufPtr value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, traceBufPtr value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, traceBufPtr value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator traceBufPtr(NilType nil) => default(traceBufPtr);
        }
    }
}
