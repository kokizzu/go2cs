//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:42:26 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using io = go.io_package;
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
        private partial struct regFileReader
        {
            // Constructors
            public regFileReader(NilType _)
            {
                this.r = default;
                this.nb = default;
            }

            public regFileReader(io.Reader r = default, long nb = default)
            {
                this.r = r;
                this.nb = nb;
            }

            // Enable comparisons between nil and regFileReader struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(regFileReader value, NilType nil) => value.Equals(default(regFileReader));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(regFileReader value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, regFileReader value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, regFileReader value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator regFileReader(NilType nil) => default(regFileReader);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static regFileReader regFileReader_cast(dynamic value)
        {
            return new regFileReader(value.r, value.nb);
        }
    }
}}