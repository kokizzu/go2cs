//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 04:49:54 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bufio = go.bufio_package;
using bytes = go.bytes_package;
using fmt = go.fmt_package;
using io = go.io_package;
using runtime = go.runtime_package;
using sort = go.sort_package;
using strings = go.strings_package;
using sync = go.sync_package;
using tabwriter = go.text.tabwriter_package;
using time = go.time_package;
using @unsafe = go.@unsafe_package;
using go;

#nullable enable

namespace go {
namespace runtime
{
    public static partial class pprof_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct runtimeProfile
        {
            // Constructors
            public runtimeProfile(NilType _)
            {
                this.stk = default;
                this.labels = default;
            }

            public runtimeProfile(slice<runtime.StackRecord> stk = default, slice<unsafe.Pointer> labels = default)
            {
                this.stk = stk;
                this.labels = labels;
            }

            // Enable comparisons between nil and runtimeProfile struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(runtimeProfile value, NilType nil) => value.Equals(default(runtimeProfile));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(runtimeProfile value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, runtimeProfile value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, runtimeProfile value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator runtimeProfile(NilType nil) => default(runtimeProfile);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static runtimeProfile runtimeProfile_cast(dynamic value)
        {
            return new runtimeProfile(value.stk, value.labels);
        }
    }
}}