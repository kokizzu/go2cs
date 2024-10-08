//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:25:55 UTC
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
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct MemProfileRecord
        {
            // Constructors
            public MemProfileRecord(NilType _)
            {
                this.AllocBytes = default;
                this.FreeBytes = default;
                this.AllocObjects = default;
                this.FreeObjects = default;
                this.Stack0 = default;
            }

            public MemProfileRecord(long AllocBytes = default, long FreeBytes = default, long AllocObjects = default, long FreeObjects = default, array<System.UIntPtr> Stack0 = default)
            {
                this.AllocBytes = AllocBytes;
                this.FreeBytes = FreeBytes;
                this.AllocObjects = AllocObjects;
                this.FreeObjects = FreeObjects;
                this.Stack0 = Stack0;
            }

            // Enable comparisons between nil and MemProfileRecord struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(MemProfileRecord value, NilType nil) => value.Equals(default(MemProfileRecord));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(MemProfileRecord value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, MemProfileRecord value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, MemProfileRecord value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator MemProfileRecord(NilType nil) => default(MemProfileRecord);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static MemProfileRecord MemProfileRecord_cast(dynamic value)
        {
            return new MemProfileRecord(value.AllocBytes, value.FreeBytes, value.AllocObjects, value.FreeObjects, value.Stack0);
        }
    }
}