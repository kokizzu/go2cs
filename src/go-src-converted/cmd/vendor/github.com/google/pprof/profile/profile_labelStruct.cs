//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:37:10 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using gzip = go.compress.gzip_package;
using fmt = go.fmt_package;
using io = go.io_package;
using ioutil = go.io.ioutil_package;
using math = go.math_package;
using filepath = go.path.filepath_package;
using regexp = go.regexp_package;
using sort = go.sort_package;
using strings = go.strings_package;
using sync = go.sync_package;
using time = go.time_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace google {
namespace pprof
{
    public static partial class profile_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct label
        {
            // Constructors
            public label(NilType _)
            {
                this.keyX = default;
                this.strX = default;
                this.numX = default;
                this.unitX = default;
            }

            public label(long keyX = default, long strX = default, long numX = default, long unitX = default)
            {
                this.keyX = keyX;
                this.strX = strX;
                this.numX = numX;
                this.unitX = unitX;
            }

            // Enable comparisons between nil and label struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(label value, NilType nil) => value.Equals(default(label));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(label value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, label value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, label value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator label(NilType nil) => default(label);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static label label_cast(dynamic value)
        {
            return new label(value.keyX, value.strX, value.numX, value.unitX);
        }
    }
}}}}}}