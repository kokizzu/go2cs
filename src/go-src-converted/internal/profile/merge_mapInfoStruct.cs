//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:38:45 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using sort = go.sort_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace @internal
{
    public static partial class profile_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct mapInfo
        {
            // Constructors
            public mapInfo(NilType _)
            {
                this.m = default;
                this.offset = default;
            }

            public mapInfo(ref ptr<Mapping> m = default, long offset = default)
            {
                this.m = m;
                this.offset = offset;
            }

            // Enable comparisons between nil and mapInfo struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(mapInfo value, NilType nil) => value.Equals(default(mapInfo));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(mapInfo value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, mapInfo value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, mapInfo value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator mapInfo(NilType nil) => default(mapInfo);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static mapInfo mapInfo_cast(dynamic value)
        {
            return new mapInfo(ref value.m, value.offset);
        }
    }
}}