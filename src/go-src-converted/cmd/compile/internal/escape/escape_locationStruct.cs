//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:22:33 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using math = go.math_package;
using strings = go.strings_package;
using @base = go.cmd.compile.@internal.@base_package;
using ir = go.cmd.compile.@internal.ir_package;
using logopt = go.cmd.compile.@internal.logopt_package;
using typecheck = go.cmd.compile.@internal.typecheck_package;
using types = go.cmd.compile.@internal.types_package;
using src = go.cmd.@internal.src_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class escape_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct location
        {
            // Constructors
            public location(NilType _)
            {
                this.n = default;
                this.curfn = default;
                this.edges = default;
                this.loopDepth = default;
                this.resultIndex = default;
                this.derefs = default;
                this.walkgen = default;
                this.dst = default;
                this.dstEdgeIdx = default;
                this.queued = default;
                this.escapes = default;
                this.transient = default;
                this.paramEsc = default;
                this.captured = default;
                this.reassigned = default;
                this.addrtaken = default;
            }

            public location(ir.Node n = default, ref ptr<ir.Func> curfn = default, slice<edge> edges = default, nint loopDepth = default, nint resultIndex = default, nint derefs = default, uint walkgen = default, ref ptr<location> dst = default, nint dstEdgeIdx = default, bool queued = default, bool escapes = default, bool transient = default, leaks paramEsc = default, bool captured = default, bool reassigned = default, bool addrtaken = default)
            {
                this.n = n;
                this.curfn = curfn;
                this.edges = edges;
                this.loopDepth = loopDepth;
                this.resultIndex = resultIndex;
                this.derefs = derefs;
                this.walkgen = walkgen;
                this.dst = dst;
                this.dstEdgeIdx = dstEdgeIdx;
                this.queued = queued;
                this.escapes = escapes;
                this.transient = transient;
                this.paramEsc = paramEsc;
                this.captured = captured;
                this.reassigned = reassigned;
                this.addrtaken = addrtaken;
            }

            // Enable comparisons between nil and location struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(location value, NilType nil) => value.Equals(default(location));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(location value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, location value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, location value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator location(NilType nil) => default(location);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static location location_cast(dynamic value)
        {
            return new location(value.n, ref value.curfn, value.edges, value.loopDepth, value.resultIndex, value.derefs, value.walkgen, ref value.dst, value.dstEdgeIdx, value.queued, value.escapes, value.transient, value.paramEsc, value.captured, value.reassigned, value.addrtaken);
        }
    }
}}}}