//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:30:02 UTC
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
    public static partial class fsys_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct fakeDir
        {
            // Value of the fakeDir struct
            private readonly @string m_value;
            
            public fakeDir(@string value) => m_value = value;

            // Enable implicit conversions between @string and fakeDir struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator fakeDir(@string value) => new fakeDir(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator @string(fakeDir value) => value.m_value;
            
            // Enable comparisons between nil and fakeDir struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(fakeDir value, NilType nil) => value.Equals(default(fakeDir));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(fakeDir value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, fakeDir value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, fakeDir value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator fakeDir(NilType nil) => default(fakeDir);
        }
    }
}}}}
