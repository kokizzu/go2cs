//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:00:38 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using @base = go.cmd.compile.@internal.@base_package;
using types = go.cmd.compile.@internal.types_package;
using src = go.cmd.@internal.src_package;
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
        [PromotedStruct(typeof(miniStmt))]
        public partial struct AssignOpStmt
        {
            // miniStmt structure promotion - sourced from value copy
            private readonly ptr<miniStmt> m_miniStmtRef;

            private ref miniStmt miniStmt_val => ref m_miniStmtRef.Value;

            public ref Nodes init => ref m_miniStmtRef.Value.init;

            // Constructors
            public AssignOpStmt(NilType _)
            {
                this.m_miniStmtRef = new ptr<miniStmt>(new miniStmt(nil));
                this.X = default;
                this.AsOp = default;
                this.Y = default;
                this.IncDec = default;
            }

            public AssignOpStmt(miniStmt miniStmt = default, Node X = default, Op AsOp = default, Node Y = default, bool IncDec = default)
            {
                this.m_miniStmtRef = new ptr<miniStmt>(miniStmt);
                this.X = X;
                this.AsOp = AsOp;
                this.Y = Y;
                this.IncDec = IncDec;
            }

            // Enable comparisons between nil and AssignOpStmt struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(AssignOpStmt value, NilType nil) => value.Equals(default(AssignOpStmt));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(AssignOpStmt value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, AssignOpStmt value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, AssignOpStmt value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator AssignOpStmt(NilType nil) => default(AssignOpStmt);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static AssignOpStmt AssignOpStmt_cast(dynamic value)
        {
            return new AssignOpStmt(value.miniStmt, value.X, value.AsOp, value.Y, value.IncDec);
        }
    }
}}}}