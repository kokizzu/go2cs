//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:32:20 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using io = go.io_package;
using fs = go.io.fs_package;
using url = go.net.url_package;
using os = go.os_package;
using filepath = go.path.filepath_package;
using strings = go.strings_package;
using sync = go.sync_package;
using time = go.time_package;
using @base = go.cmd.go.@internal.@base_package;
using cfg = go.cmd.go.@internal.cfg_package;
using lockedfile = go.cmd.go.@internal.lockedfile_package;
using web = go.cmd.go.@internal.web_package;
using module = go.golang.org.x.mod.module_package;
using sumdb = go.golang.org.x.mod.sumdb_package;
using note = go.golang.org.x.mod.sumdb.note_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace go {
namespace @internal
{
    public static partial class modfetch_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct dbClient
        {
            // Constructors
            public dbClient(NilType _)
            {
                this.key = default;
                this.name = default;
                this.direct = default;
                this.once = default;
                this.@base = default;
                this.baseErr = default;
            }

            public dbClient(@string key = default, @string name = default, ref ptr<url.URL> direct = default, sync.Once once = default, ref ptr<url.URL> @base = default, error baseErr = default)
            {
                this.key = key;
                this.name = name;
                this.direct = direct;
                this.once = once;
                this.@base = @base;
                this.baseErr = baseErr;
            }

            // Enable comparisons between nil and dbClient struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(dbClient value, NilType nil) => value.Equals(default(dbClient));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(dbClient value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, dbClient value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, dbClient value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator dbClient(NilType nil) => default(dbClient);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static dbClient dbClient_cast(dynamic value)
        {
            return new dbClient(value.key, value.name, ref value.direct, value.once, ref value.@base, value.baseErr);
        }
    }
}}}}