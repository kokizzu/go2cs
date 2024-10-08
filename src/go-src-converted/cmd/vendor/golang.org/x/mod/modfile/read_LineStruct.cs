//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:40:51 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using os = go.os_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace mod
{
    public static partial class modfile_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        [PromotedStruct(typeof(Comments))]
        public partial struct Line
        {
            // Comments structure promotion - sourced from value copy
            private readonly ptr<Comments> m_CommentsRef;

            private ref Comments Comments_val => ref m_CommentsRef.Value;

            public ref slice<Comment> Before => ref m_CommentsRef.Value.Before;

            public ref slice<Comment> Suffix => ref m_CommentsRef.Value.Suffix;

            public ref slice<Comment> After => ref m_CommentsRef.Value.After;

            // Constructors
            public Line(NilType _)
            {
                this.m_CommentsRef = new ptr<Comments>(new Comments(nil));
                this.Start = default;
                this.Token = default;
                this.InBlock = default;
                this.End = default;
            }

            public Line(Comments Comments = default, Position Start = default, slice<@string> Token = default, bool InBlock = default, Position End = default)
            {
                this.m_CommentsRef = new ptr<Comments>(Comments);
                this.Start = Start;
                this.Token = Token;
                this.InBlock = InBlock;
                this.End = End;
            }

            // Enable comparisons between nil and Line struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Line value, NilType nil) => value.Equals(default(Line));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Line value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Line value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Line value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Line(NilType nil) => default(Line);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Line Line_cast(dynamic value)
        {
            return new Line(value.Comments, value.Start, value.Token, value.InBlock, value.End);
        }
    }
}}}}}}