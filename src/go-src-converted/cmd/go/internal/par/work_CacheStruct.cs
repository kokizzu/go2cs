//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:30:11 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using rand = go.math.rand_package;
using sync = go.sync_package;
using atomic = go.sync.atomic_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace go {
namespace @internal
{
    public static partial class par_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Cache
        {
            // Constructors
            public Cache(NilType _)
            {
                this.m = default;
            }

            public Cache(sync.Map m = default)
            {
                this.m = m;
            }

            // Enable comparisons between nil and Cache struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Cache value, NilType nil) => value.Equals(default(Cache));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Cache value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Cache value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Cache value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Cache(NilType nil) => default(Cache);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Cache Cache_cast(dynamic value)
        {
            return new Cache(value.m);
        }
    }
}}}}