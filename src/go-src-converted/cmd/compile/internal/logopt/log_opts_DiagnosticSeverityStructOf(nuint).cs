//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:47:48 UTC
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
    public static partial class logopt_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct DiagnosticSeverity
        {
            // Value of the DiagnosticSeverity struct
            private readonly nuint m_value;
            
            public DiagnosticSeverity(nuint value) => m_value = value;

            // Enable implicit conversions between nuint and DiagnosticSeverity struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator DiagnosticSeverity(nuint value) => new DiagnosticSeverity(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nuint(DiagnosticSeverity value) => value.m_value;
            
            // Enable comparisons between nil and DiagnosticSeverity struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(DiagnosticSeverity value, NilType nil) => value.Equals(default(DiagnosticSeverity));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(DiagnosticSeverity value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, DiagnosticSeverity value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, DiagnosticSeverity value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator DiagnosticSeverity(NilType nil) => default(DiagnosticSeverity);
        }
    }
}}}}