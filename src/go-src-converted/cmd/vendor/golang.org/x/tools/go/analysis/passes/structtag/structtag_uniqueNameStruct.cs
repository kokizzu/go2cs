//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:42:05 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using ast = go.go.ast_package;
using token = go.go.token_package;
using types = go.go.types_package;
using filepath = go.path.filepath_package;
using reflect = go.reflect_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using analysis = go.golang.org.x.tools.go.analysis_package;
using inspect = go.golang.org.x.tools.go.analysis.passes.inspect_package;
using inspector = go.golang.org.x.tools.go.ast.inspector_package;
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
    public static partial class structtag_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct uniqueName
        {
            // Constructors
            public uniqueName(NilType _)
            {
                this.key = default;
                this.name = default;
                this.level = default;
            }

            public uniqueName(@string key = default, @string name = default, nint level = default)
            {
                this.key = key;
                this.name = name;
                this.level = level;
            }

            // Enable comparisons between nil and uniqueName struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(uniqueName value, NilType nil) => value.Equals(default(uniqueName));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(uniqueName value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, uniqueName value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, uniqueName value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator uniqueName(NilType nil) => default(uniqueName);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static uniqueName uniqueName_cast(dynamic value)
        {
            return new uniqueName(value.key, value.name, value.level);
        }
    }
}}}}}}}}}