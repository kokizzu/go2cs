//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:13:55 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using syntax = go.cmd.compile.@internal.syntax_package;
using types2 = go.cmd.compile.@internal.types2_package;
using binary = go.encoding.binary_package;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using token = go.go.token_package;
using io = go.io_package;
using big = go.math.big_package;
using sort = go.sort_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class importer_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct iimporter
        {
            // Constructors
            public iimporter(NilType _)
            {
                this.ipath = default;
                this.version = default;
                this.stringData = default;
                this.stringCache = default;
                this.pkgCache = default;
                this.declData = default;
                this.pkgIndex = default;
                this.typCache = default;
                this.interfaceList = default;
            }

            public iimporter(@string ipath = default, nint version = default, slice<byte> stringData = default, map<ulong, @string> stringCache = default, map<ulong, ptr<types2.Package>> pkgCache = default, slice<byte> declData = default, map<ptr<types2.Package>, map<@string, ulong>> pkgIndex = default, map<ulong, types2.Type> typCache = default, slice<ptr<types2.Interface>> interfaceList = default)
            {
                this.ipath = ipath;
                this.version = version;
                this.stringData = stringData;
                this.stringCache = stringCache;
                this.pkgCache = pkgCache;
                this.declData = declData;
                this.pkgIndex = pkgIndex;
                this.typCache = typCache;
                this.interfaceList = interfaceList;
            }

            // Enable comparisons between nil and iimporter struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(iimporter value, NilType nil) => value.Equals(default(iimporter));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(iimporter value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, iimporter value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, iimporter value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator iimporter(NilType nil) => default(iimporter);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static iimporter iimporter_cast(dynamic value)
        {
            return new iimporter(value.ipath, value.version, value.stringData, value.stringCache, value.pkgCache, value.declData, value.pkgIndex, value.typCache, value.interfaceList);
        }
    }
}}}}