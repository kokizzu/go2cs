//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:59:07 UTC
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
    public static partial class types_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct fmtMode
        {
            // Value of the fmtMode struct
            private readonly nint m_value;
            
            public fmtMode(nint value) => m_value = value;

            // Enable implicit conversions between nint and fmtMode struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator fmtMode(nint value) => new fmtMode(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(fmtMode value) => value.m_value;
            
            // Enable comparisons between nil and fmtMode struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(fmtMode value, NilType nil) => value.Equals(default(fmtMode));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(fmtMode value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, fmtMode value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, fmtMode value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator fmtMode(NilType nil) => default(fmtMode);
        }
    }
}}}}
