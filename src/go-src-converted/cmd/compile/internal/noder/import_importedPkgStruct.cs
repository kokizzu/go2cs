//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:13:51 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using fmt = go.fmt_package;
using buildcfg = go.@internal.buildcfg_package;
using io = go.io_package;
using os = go.os_package;
using pathpkg = go.path_package;
using runtime = go.runtime_package;
using sort = go.sort_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using @base = go.cmd.compile.@internal.@base_package;
using importer = go.cmd.compile.@internal.importer_package;
using ir = go.cmd.compile.@internal.ir_package;
using syntax = go.cmd.compile.@internal.syntax_package;
using typecheck = go.cmd.compile.@internal.typecheck_package;
using types = go.cmd.compile.@internal.types_package;
using types2 = go.cmd.compile.@internal.types2_package;
using archive = go.cmd.@internal.archive_package;
using bio = go.cmd.@internal.bio_package;
using goobj = go.cmd.@internal.goobj_package;
using objabi = go.cmd.@internal.objabi_package;
using src = go.cmd.@internal.src_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class noder_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct importedPkg
        {
            // Constructors
            public importedPkg(NilType _)
            {
                this.pos = default;
                this.path = default;
                this.name = default;
            }

            public importedPkg(src.XPos pos = default, @string path = default, @string name = default)
            {
                this.pos = pos;
                this.path = path;
                this.name = name;
            }

            // Enable comparisons between nil and importedPkg struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(importedPkg value, NilType nil) => value.Equals(default(importedPkg));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(importedPkg value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, importedPkg value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, importedPkg value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator importedPkg(NilType nil) => default(importedPkg);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static importedPkg importedPkg_cast(dynamic value)
        {
            return new importedPkg(value.pos, value.path, value.name);
        }
    }
}}}}