//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:41:35 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using reflect = go.reflect_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace tools {
namespace go
{
    public static partial class analysis_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct CycleInRequiresGraphError
        {
            // Constructors
            public CycleInRequiresGraphError(NilType _)
            {
                this.AnalyzerNames = default;
            }

            public CycleInRequiresGraphError(map<@string, bool> AnalyzerNames = default)
            {
                this.AnalyzerNames = AnalyzerNames;
            }

            // Enable comparisons between nil and CycleInRequiresGraphError struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(CycleInRequiresGraphError value, NilType nil) => value.Equals(default(CycleInRequiresGraphError));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(CycleInRequiresGraphError value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, CycleInRequiresGraphError value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, CycleInRequiresGraphError value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator CycleInRequiresGraphError(NilType nil) => default(CycleInRequiresGraphError);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static CycleInRequiresGraphError CycleInRequiresGraphError_cast(dynamic value)
        {
            return new CycleInRequiresGraphError(value.AnalyzerNames);
        }
    }
}}}}}}}