//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:53:31 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using token = go.go.token_package;
using atomic = go.sync.atomic_package;
using go;

#nullable enable

namespace go {
namespace go
{
    public static partial class types_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Struct
        {
            // Constructors
            public Struct(NilType _)
            {
                this.fields = default;
                this.tags = default;
            }

            public Struct(slice<ptr<Var>> fields = default, slice<@string> tags = default)
            {
                this.fields = fields;
                this.tags = tags;
            }

            // Enable comparisons between nil and Struct struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Struct value, NilType nil) => value.Equals(default(Struct));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Struct value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Struct value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Struct value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Struct(NilType nil) => default(Struct);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Struct Struct_cast(dynamic value)
        {
            return new Struct(value.fields, value.tags);
        }
    }
}}