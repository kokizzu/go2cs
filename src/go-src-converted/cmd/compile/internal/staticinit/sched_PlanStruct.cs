//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:25:02 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using @base = go.cmd.compile.@internal.@base_package;
using ir = go.cmd.compile.@internal.ir_package;
using reflectdata = go.cmd.compile.@internal.reflectdata_package;
using staticdata = go.cmd.compile.@internal.staticdata_package;
using typecheck = go.cmd.compile.@internal.typecheck_package;
using types = go.cmd.compile.@internal.types_package;
using obj = go.cmd.@internal.obj_package;
using src = go.cmd.@internal.src_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class staticinit_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Plan
        {
            // Constructors
            public Plan(NilType _)
            {
                this.E = default;
            }

            public Plan(slice<Entry> E = default)
            {
                this.E = E;
            }

            // Enable comparisons between nil and Plan struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Plan value, NilType nil) => value.Equals(default(Plan));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Plan value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Plan value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Plan value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Plan(NilType nil) => default(Plan);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Plan Plan_cast(dynamic value)
        {
            return new Plan(value.E);
        }
    }
}}}}