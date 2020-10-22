//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 04:47:10 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using cpu = go.@internal.cpu_package;
using atomic = go.runtime.@internal.atomic_package;
using sys = go.runtime.@internal.sys_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct spanSet
        {
            // Constructors
            public spanSet(NilType _)
            {
                this.spineLock = default;
                this.spine = default;
                this.spineLen = default;
                this.spineCap = default;
                this.index = default;
            }

            public spanSet(mutex spineLock = default, unsafe.Pointer spine = default, System.UIntPtr spineLen = default, System.UIntPtr spineCap = default, headTailIndex index = default)
            {
                this.spineLock = spineLock;
                this.spine = spine;
                this.spineLen = spineLen;
                this.spineCap = spineCap;
                this.index = index;
            }

            // Enable comparisons between nil and spanSet struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(spanSet value, NilType nil) => value.Equals(default(spanSet));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(spanSet value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, spanSet value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, spanSet value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator spanSet(NilType nil) => default(spanSet);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static spanSet spanSet_cast(dynamic value)
        {
            return new spanSet(value.spineLock, value.spine, value.spineLen, value.spineCap, value.index);
        }
    }
}