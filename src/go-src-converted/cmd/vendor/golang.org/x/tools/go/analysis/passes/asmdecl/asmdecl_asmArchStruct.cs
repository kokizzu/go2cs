//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:41:43 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using fmt = go.fmt_package;
using ast = go.go.ast_package;
using build = go.go.build_package;
using token = go.go.token_package;
using types = go.go.types_package;
using log = go.log_package;
using regexp = go.regexp_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using analysis = go.golang.org.x.tools.go.analysis_package;
using analysisutil = go.golang.org.x.tools.go.analysis.passes.@internal.analysisutil_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace tools {
namespace go {
namespace analysis {
namespace passes
{
    public static partial class asmdecl_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct asmArch
        {
            // Constructors
            public asmArch(NilType _)
            {
                this.name = default;
                this.bigEndian = default;
                this.stack = default;
                this.lr = default;
                this.sizes = default;
                this.intSize = default;
                this.ptrSize = default;
                this.maxAlign = default;
            }

            public asmArch(@string name = default, bool bigEndian = default, @string stack = default, bool lr = default, types.Sizes sizes = default, nint intSize = default, nint ptrSize = default, nint maxAlign = default)
            {
                this.name = name;
                this.bigEndian = bigEndian;
                this.stack = stack;
                this.lr = lr;
                this.sizes = sizes;
                this.intSize = intSize;
                this.ptrSize = ptrSize;
                this.maxAlign = maxAlign;
            }

            // Enable comparisons between nil and asmArch struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(asmArch value, NilType nil) => value.Equals(default(asmArch));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(asmArch value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, asmArch value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, asmArch value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator asmArch(NilType nil) => default(asmArch);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static asmArch asmArch_cast(dynamic value)
        {
            return new asmArch(value.name, value.bigEndian, value.stack, value.lr, value.sizes, value.intSize, value.ptrSize, value.maxAlign);
        }
    }
}}}}}}}}}