//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:31:45 UTC
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
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Type1
        {
            // Value of the Type1 struct
            private readonly nint m_value;
            
            public Type1(nint value) => m_value = value;

            // Enable implicit conversions between nint and Type1 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Type1(nint value) => new Type1(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(Type1 value) => value.m_value;
            
            // Enable comparisons between nil and Type1 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Type1 value, NilType nil) => value.Equals(default(Type1));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Type1 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Type1 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Type1 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Type1(NilType nil) => default(Type1);
        }
    }
}