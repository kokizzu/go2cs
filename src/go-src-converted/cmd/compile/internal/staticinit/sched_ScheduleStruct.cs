//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:11:42 UTC
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
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Schedule
        {
            // Constructors
            public Schedule(NilType _)
            {
                this.Out = default;
                this.Plans = default;
                this.Temps = default;
            }

            public Schedule(slice<ir.Node> Out = default, map<ir.Node, ptr<Plan>> Plans = default, map<ir.Node, ptr<ir.Name>> Temps = default)
            {
                this.Out = Out;
                this.Plans = Plans;
                this.Temps = Temps;
            }

            // Enable comparisons between nil and Schedule struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Schedule value, NilType nil) => value.Equals(default(Schedule));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Schedule value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Schedule value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Schedule value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Schedule(NilType nil) => default(Schedule);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Schedule Schedule_cast(dynamic value)
        {
            return new Schedule(value.Out, value.Plans, value.Temps);
        }
    }
}}}}