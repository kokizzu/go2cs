//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:40:38 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class syscall_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Errno
        {
            // Value of the Errno struct
            private readonly System.UIntPtr m_value;
            
            public Errno(System.UIntPtr value) => m_value = value;

            // Enable implicit conversions between System.UIntPtr and Errno struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Errno(System.UIntPtr value) => new Errno(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator System.UIntPtr(Errno value) => value.m_value;
            
            // Enable comparisons between nil and Errno struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Errno value, NilType nil) => value.Equals(default(Errno));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Errno value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Errno value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Errno value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Errno(NilType nil) => default(Errno);
        }
    }
}
