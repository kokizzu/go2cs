//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:53:14 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using token = go.go.token_package;
using go;

#nullable enable

namespace go {
namespace go
{
    public static partial class types_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        [PromotedStruct(typeof(@object))]
        public partial struct Func
        {
            // @object structure promotion - sourced from value copy
            private readonly ptr<@object> m_@objectRef;

            private ref @object @object_val => ref m_@objectRef.Value;

            public ref ptr<Scope> parent => ref m_@objectRef.Value.parent;

            public ref token.Pos pos => ref m_@objectRef.Value.pos;

            public ref ptr<Package> pkg => ref m_@objectRef.Value.pkg;

            public ref @string name => ref m_@objectRef.Value.name;

            public ref Type typ => ref m_@objectRef.Value.typ;

            public ref uint order_ => ref m_@objectRef.Value.order_;

            public ref color color_ => ref m_@objectRef.Value.color_;

            public ref token.Pos scopePos_ => ref m_@objectRef.Value.scopePos_;

            // Constructors
            public Func(NilType _)
            {
                this.@object = default;
                this.hasPtrRecv = default;
            }

            public Func(object @object = default, bool hasPtrRecv = default)
            {
                this.@object = @object;
                this.hasPtrRecv = hasPtrRecv;
            }

            // Enable comparisons between nil and Func struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Func value, NilType nil) => value.Equals(default(Func));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Func value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Func value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Func value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Func(NilType nil) => default(Func);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Func Func_cast(dynamic value)
        {
            return new Func(value.@object, value.hasPtrRecv);
        }
    }
}}