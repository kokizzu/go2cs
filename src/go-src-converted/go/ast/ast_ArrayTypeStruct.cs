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
        public partial struct ArrayType
        {
            // Constructors
            public ArrayType(NilType _)
            {
                this.Lbrack = default;
                this.Len = default;
                this.Elt = default;
            }

            public ArrayType(token.Pos Lbrack = default, Expr Len = default, Expr Elt = default)
            {
                this.Lbrack = Lbrack;
                this.Len = Len;
                this.Elt = Elt;
            }

            // Enable comparisons between nil and ArrayType struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ArrayType value, NilType nil) => value.Equals(default(ArrayType));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ArrayType value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ArrayType value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ArrayType value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ArrayType(NilType nil) => default(ArrayType);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static ArrayType ArrayType_cast(dynamic value)
        {
            return new ArrayType(value.Lbrack, value.Len, value.Elt);
        }
    }
}}