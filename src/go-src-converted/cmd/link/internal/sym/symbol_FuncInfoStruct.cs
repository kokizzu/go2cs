//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 10:02:56 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using objabi = go.cmd.@internal.objabi_package;
using sys = go.cmd.@internal.sys_package;
using elf = go.debug.elf_package;
using fmt = go.fmt_package;
using log = go.log_package;
using go;

namespace go {
namespace cmd {
namespace link {
namespace @internal
{
    public static partial class sym_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct FuncInfo
        {
            // Constructors
            public FuncInfo(NilType _)
            {
                this.Args = default;
                this.Locals = default;
                this.Autom = default;
                this.Pcsp = default;
                this.Pcfile = default;
                this.Pcline = default;
                this.Pcinline = default;
                this.Pcdata = default;
                this.Funcdata = default;
                this.Funcdataoff = default;
                this.File = default;
                this.InlTree = default;
            }

            public FuncInfo(int Args = default, int Locals = default, slice<Auto> Autom = default, Pcdata Pcsp = default, Pcdata Pcfile = default, Pcdata Pcline = default, Pcdata Pcinline = default, slice<Pcdata> Pcdata = default, slice<ref Symbol> Funcdata = default, slice<long> Funcdataoff = default, slice<ref Symbol> File = default, slice<InlinedCall> InlTree = default)
            {
                this.Args = Args;
                this.Locals = Locals;
                this.Autom = Autom;
                this.Pcsp = Pcsp;
                this.Pcfile = Pcfile;
                this.Pcline = Pcline;
                this.Pcinline = Pcinline;
                this.Pcdata = Pcdata;
                this.Funcdata = Funcdata;
                this.Funcdataoff = Funcdataoff;
                this.File = File;
                this.InlTree = InlTree;
            }

            // Enable comparisons between nil and FuncInfo struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(FuncInfo value, NilType nil) => value.Equals(default(FuncInfo));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(FuncInfo value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, FuncInfo value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, FuncInfo value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator FuncInfo(NilType nil) => default(FuncInfo);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static FuncInfo FuncInfo_cast(dynamic value)
        {
            return new FuncInfo(value.Args, value.Locals, value.Autom, value.Pcsp, value.Pcfile, value.Pcline, value.Pcinline, value.Pcdata, value.Funcdata, value.Funcdataoff, value.File, value.InlTree);
        }
    }
}}}}