//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:25:00 UTC
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
    public static partial class walk_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct initGenType
        {
            // Value of the initGenType struct
            private readonly byte m_value;
            
            public initGenType(byte value) => m_value = value;

            // Enable implicit conversions between byte and initGenType struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator initGenType(byte value) => new initGenType(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator byte(initGenType value) => value.m_value;
            
            // Enable comparisons between nil and initGenType struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(initGenType value, NilType nil) => value.Equals(default(initGenType));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(initGenType value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, initGenType value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, initGenType value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator initGenType(NilType nil) => default(initGenType);
        }
    }
}}}}
