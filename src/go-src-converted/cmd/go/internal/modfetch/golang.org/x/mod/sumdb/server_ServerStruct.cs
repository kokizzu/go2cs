//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 05:47:33 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using context = go.context_package;
using http = go.net.http_package;
using os = go.os_package;
using strings = go.strings_package;
using lazyregexp = go.golang.org.x.mod.@internal.lazyregexp_package;
using module = go.golang.org.x.mod.module_package;
using tlog = go.golang.org.x.mod.sumdb.tlog_package;
using go;

#nullable enable

namespace go {
namespace golang.org {
namespace x {
namespace mod
{
    public static partial class sumdb_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Server
        {
            // Constructors
            public Server(NilType _)
            {
                this.ops = default;
            }

            public Server(ServerOps ops = default)
            {
                this.ops = ops;
            }

            // Enable comparisons between nil and Server struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Server value, NilType nil) => value.Equals(default(Server));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Server value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Server value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Server value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Server(NilType nil) => default(Server);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Server Server_cast(dynamic value)
        {
            return new Server(value.ops);
        }
    }
}}}}