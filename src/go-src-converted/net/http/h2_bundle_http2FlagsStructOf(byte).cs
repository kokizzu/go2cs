//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:37:08 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace net
{
    public static partial class http_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct http2Flags
        {
            // Value of the http2Flags struct
            private readonly byte m_value;
            
            public http2Flags(byte value) => m_value = value;

            // Enable implicit conversions between byte and http2Flags struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator http2Flags(byte value) => new http2Flags(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator byte(http2Flags value) => value.m_value;
            
            // Enable comparisons between nil and http2Flags struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(http2Flags value, NilType nil) => value.Equals(default(http2Flags));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(http2Flags value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, http2Flags value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, http2Flags value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator http2Flags(NilType nil) => default(http2Flags);
        }
    }
}}
