//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:30:55 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using context = go.context_package;
using json = go.encoding.json_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using buildcfg = go.@internal.buildcfg_package;
using exec = go.@internal.execabs_package;
using lazyregexp = go.@internal.lazyregexp_package;
using io = go.io_package;
using fs = go.io.fs_package;
using log = go.log_package;
using rand = go.math.rand_package;
using os = go.os_package;
using filepath = go.path.filepath_package;
using regexp = go.regexp_package;
using runtime = go.runtime_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using sync = go.sync_package;
using time = go.time_package;
using @base = go.cmd.go.@internal.@base_package;
using cache = go.cmd.go.@internal.cache_package;
using cfg = go.cmd.go.@internal.cfg_package;
using fsys = go.cmd.go.@internal.fsys_package;
using load = go.cmd.go.@internal.load_package;
using modload = go.cmd.go.@internal.modload_package;
using str = go.cmd.go.@internal.str_package;
using trace = go.cmd.go.@internal.trace_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace go {
namespace @internal
{
    public static partial class work_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct vetConfig
        {
            // Constructors
            public vetConfig(NilType _)
            {
                this.ID = default;
                this.Compiler = default;
                this.Dir = default;
                this.ImportPath = default;
                this.GoFiles = default;
                this.NonGoFiles = default;
                this.IgnoredFiles = default;
                this.ImportMap = default;
                this.PackageFile = default;
                this.Standard = default;
                this.PackageVetx = default;
                this.VetxOnly = default;
                this.VetxOutput = default;
                this.SucceedOnTypecheckFailure = default;
            }

            public vetConfig(@string ID = default, @string Compiler = default, @string Dir = default, @string ImportPath = default, slice<@string> GoFiles = default, slice<@string> NonGoFiles = default, slice<@string> IgnoredFiles = default, map<@string, @string> ImportMap = default, map<@string, @string> PackageFile = default, map<@string, bool> Standard = default, map<@string, @string> PackageVetx = default, bool VetxOnly = default, @string VetxOutput = default, bool SucceedOnTypecheckFailure = default)
            {
                this.ID = ID;
                this.Compiler = Compiler;
                this.Dir = Dir;
                this.ImportPath = ImportPath;
                this.GoFiles = GoFiles;
                this.NonGoFiles = NonGoFiles;
                this.IgnoredFiles = IgnoredFiles;
                this.ImportMap = ImportMap;
                this.PackageFile = PackageFile;
                this.Standard = Standard;
                this.PackageVetx = PackageVetx;
                this.VetxOnly = VetxOnly;
                this.VetxOutput = VetxOutput;
                this.SucceedOnTypecheckFailure = SucceedOnTypecheckFailure;
            }

            // Enable comparisons between nil and vetConfig struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(vetConfig value, NilType nil) => value.Equals(default(vetConfig));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(vetConfig value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, vetConfig value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, vetConfig value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator vetConfig(NilType nil) => default(vetConfig);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static vetConfig vetConfig_cast(dynamic value)
        {
            return new vetConfig(value.ID, value.Compiler, value.Dir, value.ImportPath, value.GoFiles, value.NonGoFiles, value.IgnoredFiles, value.ImportMap, value.PackageFile, value.Standard, value.PackageVetx, value.VetxOnly, value.VetxOutput, value.SucceedOnTypecheckFailure);
        }
    }
}}}}