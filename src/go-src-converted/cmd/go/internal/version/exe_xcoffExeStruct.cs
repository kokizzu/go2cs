//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:29:54 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using elf = go.debug.elf_package;
using macho = go.debug.macho_package;
using pe = go.debug.pe_package;
using fmt = go.fmt_package;
using xcoff = go.@internal.xcoff_package;
using io = go.io_package;
using os = go.os_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace go {
namespace @internal
{
    public static partial class version_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct xcoffExe
        {
            // Constructors
            public xcoffExe(NilType _)
            {
                this.os = default;
                this.f = default;
            }

            public xcoffExe(ref ptr<os.File> os = default, ref ptr<xcoff.File> f = default)
            {
                this.os = os;
                this.f = f;
            }

            // Enable comparisons between nil and xcoffExe struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(xcoffExe value, NilType nil) => value.Equals(default(xcoffExe));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(xcoffExe value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, xcoffExe value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, xcoffExe value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator xcoffExe(NilType nil) => default(xcoffExe);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static xcoffExe xcoffExe_cast(dynamic value)
        {
            return new xcoffExe(ref value.os, ref value.f);
        }
    }
}}}}