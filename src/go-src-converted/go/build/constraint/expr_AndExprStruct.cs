//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:52:25 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using go;

#nullable enable

namespace go {
namespace go {
namespace build
{
    public static partial class constraint_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct AndExpr
        {
            // Constructors
            public AndExpr(NilType _)
            {
                this.X = default;
                this.Y = default;
            }

            public AndExpr(Expr X = default, Expr Y = default)
            {
                this.X = X;
                this.Y = Y;
            }

            // Enable comparisons between nil and AndExpr struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(AndExpr value, NilType nil) => value.Equals(default(AndExpr));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(AndExpr value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, AndExpr value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, AndExpr value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator AndExpr(NilType nil) => default(AndExpr);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static AndExpr AndExpr_cast(dynamic value)
        {
            return new AndExpr(value.X, value.Y);
        }
    }
}}}