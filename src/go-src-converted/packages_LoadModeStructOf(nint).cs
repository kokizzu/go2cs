//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:31:51 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace golang.org {
namespace x {
namespace tools {
namespace go
{
    public static partial class packages_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct LoadMode
        {
            // Value of the LoadMode struct
            private readonly nint m_value;
            
            public LoadMode(nint value) => m_value = value;

            // Enable implicit conversions between nint and LoadMode struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator LoadMode(nint value) => new LoadMode(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(LoadMode value) => value.m_value;
            
            // Enable comparisons between nil and LoadMode struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(LoadMode value, NilType nil) => value.Equals(default(LoadMode));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(LoadMode value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, LoadMode value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, LoadMode value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator LoadMode(NilType nil) => default(LoadMode);
        }
    }
}}}}}