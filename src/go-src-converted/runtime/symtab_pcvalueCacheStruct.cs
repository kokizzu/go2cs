//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:27:14 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using atomic = go.runtime.@internal.atomic_package;
using sys = go.runtime.@internal.sys_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct pcvalueCache
        {
            // Constructors
            public pcvalueCache(NilType _)
            {
                this.entries = default;
            }

            public pcvalueCache(array<array<pcvalueCacheEnt>> entries = default)
            {
                this.entries = entries;
            }

            // Enable comparisons between nil and pcvalueCache struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(pcvalueCache value, NilType nil) => value.Equals(default(pcvalueCache));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(pcvalueCache value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, pcvalueCache value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, pcvalueCache value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator pcvalueCache(NilType nil) => default(pcvalueCache);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static pcvalueCache pcvalueCache_cast(dynamic value)
        {
            return new pcvalueCache(value.entries);
        }
    }
}