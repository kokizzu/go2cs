//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:52:15 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using token = go.go.token_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace go
{
    public static partial class ast_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct ChanType
        {
            // Constructors
            public ChanType(NilType _)
            {
                this.Begin = default;
                this.Arrow = default;
                this.Dir = default;
                this.Value = default;
            }

            public ChanType(token.Pos Begin = default, token.Pos Arrow = default, ChanDir Dir = default, Expr Value = default)
            {
                this.Begin = Begin;
                this.Arrow = Arrow;
                this.Dir = Dir;
                this.Value = Value;
            }

            // Enable comparisons between nil and ChanType struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ChanType value, NilType nil) => value.Equals(default(ChanType));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ChanType value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ChanType value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ChanType value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ChanType(NilType nil) => default(ChanType);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static ChanType ChanType_cast(dynamic value)
        {
            return new ChanType(value.Begin, value.Arrow, value.Dir, value.Value);
        }
    }
}}