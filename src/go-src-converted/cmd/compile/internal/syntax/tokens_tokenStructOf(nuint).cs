//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:27:07 UTC
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
namespace compile {
namespace @internal
{
    public static partial class syntax_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct token
        {
            // Value of the token struct
            private readonly nuint m_value;
            
            public token(nuint value) => m_value = value;

            // Enable implicit conversions between nuint and token struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator token(nuint value) => new token(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nuint(token value) => value.m_value;
            
            // Enable comparisons between nil and token struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(token value, NilType nil) => value.Equals(default(token));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(token value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, token value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, token value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator token(NilType nil) => default(token);
        }
    }
}}}}
