//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:30:30 UTC
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
namespace go {
namespace @internal
{
    public static partial class load_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct TargetDir
        {
            // Value of the TargetDir struct
            private readonly nint m_value;
            
            public TargetDir(nint value) => m_value = value;

            // Enable implicit conversions between nint and TargetDir struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator TargetDir(nint value) => new TargetDir(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(TargetDir value) => value.m_value;
            
            // Enable comparisons between nil and TargetDir struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(TargetDir value, NilType nil) => value.Equals(default(TargetDir));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(TargetDir value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, TargetDir value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, TargetDir value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator TargetDir(NilType nil) => default(TargetDir);
        }
    }
}}}}
