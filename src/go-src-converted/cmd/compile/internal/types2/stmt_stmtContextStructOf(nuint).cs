//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:26:20 UTC
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
    public static partial class types2_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct stmtContext
        {
            // Value of the stmtContext struct
            private readonly nuint m_value;
            
            public stmtContext(nuint value) => m_value = value;

            // Enable implicit conversions between nuint and stmtContext struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator stmtContext(nuint value) => new stmtContext(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nuint(stmtContext value) => value.m_value;
            
            // Enable comparisons between nil and stmtContext struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(stmtContext value, NilType nil) => value.Equals(default(stmtContext));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(stmtContext value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, stmtContext value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, stmtContext value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator stmtContext(NilType nil) => default(stmtContext);
        }
    }
}}}}
