//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:14:32 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using io = go.io_package;
using strings = go.strings_package;
using time = go.time_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class @base_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Timings
        {
            // Constructors
            public Timings(NilType _)
            {
                this.list = default;
                this.events = default;
            }

            public Timings(slice<timestamp> list = default, map<nint, slice<ptr<event>>> events = default)
            {
                this.list = list;
                this.events = events;
            }

            // Enable comparisons between nil and Timings struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Timings value, NilType nil) => value.Equals(default(Timings));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Timings value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Timings value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Timings value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Timings(NilType nil) => default(Timings);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Timings Timings_cast(dynamic value)
        {
            return new Timings(value.list, value.events);
        }
    }
}}}}