//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:10:38 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bufio = go.bufio_package;
using bytes = go.bytes_package;
using abi = go.cmd.compile.@internal.abi_package;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using html = go.html_package;
using buildcfg = go.@internal.buildcfg_package;
using os = go.os_package;
using filepath = go.path.filepath_package;
using sort = go.sort_package;
using strings = go.strings_package;
using @base = go.cmd.compile.@internal.@base_package;
using ir = go.cmd.compile.@internal.ir_package;
using liveness = go.cmd.compile.@internal.liveness_package;
using objw = go.cmd.compile.@internal.objw_package;
using reflectdata = go.cmd.compile.@internal.reflectdata_package;
using ssa = go.cmd.compile.@internal.ssa_package;
using staticdata = go.cmd.compile.@internal.staticdata_package;
using typecheck = go.cmd.compile.@internal.typecheck_package;
using types = go.cmd.compile.@internal.types_package;
using obj = go.cmd.@internal.obj_package;
using x86 = go.cmd.@internal.obj.x86_package;
using objabi = go.cmd.@internal.objabi_package;
using src = go.cmd.@internal.src_package;
using sys = go.cmd.@internal.sys_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class ssagen_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct u642fcvtTab
        {
            // Constructors
            public u642fcvtTab(NilType _)
            {
                this.leq = default;
                this.cvt2F = default;
                this.and = default;
                this.rsh = default;
                this.or = default;
                this.add = default;
                this.one = default;
            }

            public u642fcvtTab(ssa.Op leq = default, ssa.Op cvt2F = default, ssa.Op and = default, ssa.Op rsh = default, ssa.Op or = default, ssa.Op add = default, Func<ptr<state>, ptr<types.Type>, long, ptr<ssa.Value>> one = default)
            {
                this.leq = leq;
                this.cvt2F = cvt2F;
                this.and = and;
                this.rsh = rsh;
                this.or = or;
                this.add = add;
                this.one = one;
            }

            // Enable comparisons between nil and u642fcvtTab struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(u642fcvtTab value, NilType nil) => value.Equals(default(u642fcvtTab));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(u642fcvtTab value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, u642fcvtTab value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, u642fcvtTab value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator u642fcvtTab(NilType nil) => default(u642fcvtTab);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static u642fcvtTab u642fcvtTab_cast(dynamic value)
        {
            return new u642fcvtTab(value.leq, value.cvt2F, value.and, value.rsh, value.or, value.add, value.one);
        }
    }
}}}}