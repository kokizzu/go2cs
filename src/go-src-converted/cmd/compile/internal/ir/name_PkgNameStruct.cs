//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:00:30 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using @base = go.cmd.compile.@internal.@base_package;
using types = go.cmd.compile.@internal.types_package;
using obj = go.cmd.@internal.obj_package;
using objabi = go.cmd.@internal.objabi_package;
using src = go.cmd.@internal.src_package;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class ir_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct PkgName
        {
            // Constructors
            public PkgName(NilType _)
            {
                this.miniNode = default;
                this.sym = default;
                this.Pkg = default;
                this.Used = default;
            }

            public PkgName(miniNode miniNode = default, ref ptr<types.Sym> sym = default, ref ptr<types.Pkg> Pkg = default, bool Used = default)
            {
                this.miniNode = miniNode;
                this.sym = sym;
                this.Pkg = Pkg;
                this.Used = Used;
            }

            // Enable comparisons between nil and PkgName struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(PkgName value, NilType nil) => value.Equals(default(PkgName));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(PkgName value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, PkgName value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, PkgName value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PkgName(NilType nil) => default(PkgName);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static PkgName PkgName_cast(dynamic value)
        {
            return new PkgName(value.miniNode, ref value.sym, ref value.Pkg, value.Used);
        }
    }
}}}}