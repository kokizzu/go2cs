//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:09:28 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using atomic = go.runtime.@internal.atomic_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct heapStatsAggregate
        {
            // Constructors
            public heapStatsAggregate(NilType _)
            {
                this.heapStatsDelta = default;
                this.inObjects = default;
                this.numObjects = default;
                this.totalAllocated = default;
                this.totalFreed = default;
                this.totalAllocs = default;
                this.totalFrees = default;
            }

            public heapStatsAggregate(heapStatsDelta heapStatsDelta = default, ulong inObjects = default, ulong numObjects = default, ulong totalAllocated = default, ulong totalFreed = default, ulong totalAllocs = default, ulong totalFrees = default)
            {
                this.heapStatsDelta = heapStatsDelta;
                this.inObjects = inObjects;
                this.numObjects = numObjects;
                this.totalAllocated = totalAllocated;
                this.totalFreed = totalFreed;
                this.totalAllocs = totalAllocs;
                this.totalFrees = totalFrees;
            }

            // Enable comparisons between nil and heapStatsAggregate struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(heapStatsAggregate value, NilType nil) => value.Equals(default(heapStatsAggregate));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(heapStatsAggregate value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, heapStatsAggregate value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, heapStatsAggregate value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator heapStatsAggregate(NilType nil) => default(heapStatsAggregate);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static heapStatsAggregate heapStatsAggregate_cast(dynamic value)
        {
            return new heapStatsAggregate(value.heapStatsDelta, value.inObjects, value.numObjects, value.totalAllocated, value.totalFreed, value.totalAllocs, value.totalFrees);
        }
    }
}