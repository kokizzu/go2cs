//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:01:29 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using sha256 = go.crypto.sha256_package;
using gob = go.encoding.gob_package;
using json = go.encoding.json_package;
using flag = go.flag_package;
using fmt = go.fmt_package;
using token = go.go.token_package;
using io = go.io_package;
using ioutil = go.io.ioutil_package;
using log = go.log_package;
using os = go.os_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using analysis = go.golang.org.x.tools.go.analysis_package;
using go;

#nullable enable

namespace go {
namespace golang.org {
namespace x {
namespace tools {
namespace go {
namespace analysis {
namespace @internal
{
    public static partial class analysisflags_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct jsonError
        {
            // Constructors
            public jsonError(NilType _)
            {
                this.Err = default;
            }

            public jsonError(@string Err = default)
            {
                this.Err = Err;
            }

            // Enable comparisons between nil and jsonError struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(jsonError value, NilType nil) => value.Equals(default(jsonError));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(jsonError value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, jsonError value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, jsonError value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator jsonError(NilType nil) => default(jsonError);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static jsonError jsonError_cast(dynamic value)
        {
            return new jsonError(value.Err);
        }
    }
}}}}}}}