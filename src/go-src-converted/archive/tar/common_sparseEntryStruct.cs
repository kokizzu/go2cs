//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:23:49 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using fmt = go.fmt_package;
using fs = go.io.fs_package;
using math = go.math_package;
using path = go.path_package;
using reflect = go.reflect_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using time = go.time_package;
using go;

#nullable enable

namespace go {
namespace archive
{
    public static partial class tar_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct sparseEntry
        {
            // Constructors
            public sparseEntry(NilType _)
            {
                this.Offset = default;
                this.Length = default;
            }

            public sparseEntry(long Offset = default, long Length = default)
            {
                this.Offset = Offset;
                this.Length = Length;
            }

            // Enable comparisons between nil and sparseEntry struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(sparseEntry value, NilType nil) => value.Equals(default(sparseEntry));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(sparseEntry value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, sparseEntry value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, sparseEntry value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator sparseEntry(NilType nil) => default(sparseEntry);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static sparseEntry sparseEntry_cast(dynamic value)
        {
            return new sparseEntry(value.Offset, value.Length);
        }
    }
}}