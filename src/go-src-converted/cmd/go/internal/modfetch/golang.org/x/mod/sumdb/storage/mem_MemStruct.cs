//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 05:47:34 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using context = go.context_package;
using errors = go.errors_package;
using rand = go.math.rand_package;
using sync = go.sync_package;
using go;

#nullable enable

namespace go {
namespace golang.org {
namespace x {
namespace mod {
namespace sumdb
{
    public static partial class storage_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Mem
        {
            // Constructors
            public Mem(NilType _)
            {
                this.mu = default;
                this.table = default;
            }

            public Mem(sync.RWMutex mu = default, map<@string, @string> table = default)
            {
                this.mu = mu;
                this.table = table;
            }

            // Enable comparisons between nil and Mem struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Mem value, NilType nil) => value.Equals(default(Mem));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Mem value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Mem value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Mem value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Mem(NilType nil) => default(Mem);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Mem Mem_cast(dynamic value)
        {
            return new Mem(value.mu, value.table);
        }
    }
}}}}}