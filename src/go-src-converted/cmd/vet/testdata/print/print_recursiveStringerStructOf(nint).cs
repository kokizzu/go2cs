//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:35:21 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vet {
namespace testdata
{
    public static partial class print_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct recursiveStringer
        {
            // Value of the recursiveStringer struct
            private readonly nint m_value;
            
            public recursiveStringer(nint value) => m_value = value;

            // Enable implicit conversions between nint and recursiveStringer struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator recursiveStringer(nint value) => new recursiveStringer(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(recursiveStringer value) => value.m_value;
            
            // Enable comparisons between nil and recursiveStringer struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(recursiveStringer value, NilType nil) => value.Equals(default(recursiveStringer));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(recursiveStringer value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, recursiveStringer value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, recursiveStringer value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator recursiveStringer(NilType nil) => default(recursiveStringer);
        }
    }
}}}}