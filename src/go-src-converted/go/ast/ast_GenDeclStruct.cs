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
        public partial struct GenDecl
        {
            // Constructors
            public GenDecl(NilType _)
            {
                this.Doc = default;
                this.TokPos = default;
                this.Tok = default;
                this.Lparen = default;
                this.Specs = default;
                this.Rparen = default;
            }

            public GenDecl(ref ptr<CommentGroup> Doc = default, token.Pos TokPos = default, token.Token Tok = default, token.Pos Lparen = default, slice<Spec> Specs = default, token.Pos Rparen = default)
            {
                this.Doc = Doc;
                this.TokPos = TokPos;
                this.Tok = Tok;
                this.Lparen = Lparen;
                this.Specs = Specs;
                this.Rparen = Rparen;
            }

            // Enable comparisons between nil and GenDecl struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(GenDecl value, NilType nil) => value.Equals(default(GenDecl));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(GenDecl value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, GenDecl value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, GenDecl value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator GenDecl(NilType nil) => default(GenDecl);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static GenDecl GenDecl_cast(dynamic value)
        {
            return new GenDecl(ref value.Doc, value.TokPos, value.Tok, value.Lparen, value.Specs, value.Rparen);
        }
    }
}}