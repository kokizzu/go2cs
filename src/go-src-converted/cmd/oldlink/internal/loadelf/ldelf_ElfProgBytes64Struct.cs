//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 05:52:04 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bytes = go.bytes_package;
using bio = go.cmd.@internal.bio_package;
using objabi = go.cmd.@internal.objabi_package;
using sys = go.cmd.@internal.sys_package;
using loader = go.cmd.oldlink.@internal.loader_package;
using sym = go.cmd.oldlink.@internal.sym_package;
using elf = go.debug.elf_package;
using binary = go.encoding.binary_package;
using fmt = go.fmt_package;
using io = go.io_package;
using log = go.log_package;
using sort = go.sort_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace oldlink {
namespace @internal
{
    public static partial class loadelf_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct ElfProgBytes64
        {
            // Constructors
            public ElfProgBytes64(NilType _)
            {
            }
            // Enable comparisons between nil and ElfProgBytes64 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ElfProgBytes64 value, NilType nil) => value.Equals(default(ElfProgBytes64));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ElfProgBytes64 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ElfProgBytes64 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ElfProgBytes64 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ElfProgBytes64(NilType nil) => default(ElfProgBytes64);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static ElfProgBytes64 ElfProgBytes64_cast(dynamic value)
        {
            return new ElfProgBytes64();
        }
    }
}}}}