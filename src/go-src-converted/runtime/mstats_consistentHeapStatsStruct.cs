//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:10:11 UTC
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
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct consistentHeapStats
        {
            // Constructors
            public consistentHeapStats(NilType _)
            {
                this.stats = default;
                this.gen = default;
                this.noPLock = default;
            }

            public consistentHeapStats(array<heapStatsDelta> stats = default, uint gen = default, mutex noPLock = default)
            {
                this.stats = stats;
                this.gen = gen;
                this.noPLock = noPLock;
            }

            // Enable comparisons between nil and consistentHeapStats struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(consistentHeapStats value, NilType nil) => value.Equals(default(consistentHeapStats));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(consistentHeapStats value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, consistentHeapStats value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, consistentHeapStats value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator consistentHeapStats(NilType nil) => default(consistentHeapStats);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static consistentHeapStats consistentHeapStats_cast(dynamic value)
        {
            return new consistentHeapStats(value.stats, value.gen, value.noPLock);
        }
    }
}