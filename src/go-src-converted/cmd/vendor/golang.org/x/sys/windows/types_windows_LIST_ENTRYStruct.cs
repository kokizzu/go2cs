//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:30:43 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using net = go.net_package;
using syscall = go.syscall_package;
using @unsafe = go.@unsafe_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace sys
{
    public static partial class windows_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct LIST_ENTRY
        {
            // Constructors
            public LIST_ENTRY(NilType _)
            {
                this.Flink = default;
                this.Blink = default;
            }

            public LIST_ENTRY(ref ptr<LIST_ENTRY> Flink = default, ref ptr<LIST_ENTRY> Blink = default)
            {
                this.Flink = Flink;
                this.Blink = Blink;
            }

            // Enable comparisons between nil and LIST_ENTRY struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(LIST_ENTRY value, NilType nil) => value.Equals(default(LIST_ENTRY));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(LIST_ENTRY value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, LIST_ENTRY value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, LIST_ENTRY value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator LIST_ENTRY(NilType nil) => default(LIST_ENTRY);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static LIST_ENTRY LIST_ENTRY_cast(dynamic value)
        {
            return new LIST_ENTRY(ref value.Flink, ref value.Blink);
        }
    }
}}}}}}