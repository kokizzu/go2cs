//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:09:41 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct gcDrainFlags
        {
            // Value of the gcDrainFlags struct
            private readonly nint m_value;
            
            public gcDrainFlags(nint value) => m_value = value;

            // Enable implicit conversions between nint and gcDrainFlags struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator gcDrainFlags(nint value) => new gcDrainFlags(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(gcDrainFlags value) => value.m_value;
            
            // Enable comparisons between nil and gcDrainFlags struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(gcDrainFlags value, NilType nil) => value.Equals(default(gcDrainFlags));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(gcDrainFlags value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, gcDrainFlags value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, gcDrainFlags value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator gcDrainFlags(NilType nil) => default(gcDrainFlags);
        }
    }
}