//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:46:35 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace vendor {
namespace golang.org {
namespace x {
namespace text {
namespace secure
{
    public static partial class bidirule_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct ruleState
        {
            // Value of the ruleState struct
            private readonly byte m_value;
            
            public ruleState(byte value) => m_value = value;

            // Enable implicit conversions between byte and ruleState struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ruleState(byte value) => new ruleState(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator byte(ruleState value) => value.m_value;
            
            // Enable comparisons between nil and ruleState struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ruleState value, NilType nil) => value.Equals(default(ruleState));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ruleState value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ruleState value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ruleState value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ruleState(NilType nil) => default(ruleState);
        }
    }
}}}}}}
