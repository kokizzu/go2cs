//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:37:26 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace ianlancetaylor
{
    public static partial class demangle_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct InitializerList
        {
            // Constructors
            public InitializerList(NilType _)
            {
                this.Type = default;
                this.Exprs = default;
            }

            public InitializerList(AST Type = default, AST Exprs = default)
            {
                this.Type = Type;
                this.Exprs = Exprs;
            }

            // Enable comparisons between nil and InitializerList struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(InitializerList value, NilType nil) => value.Equals(default(InitializerList));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(InitializerList value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, InitializerList value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, InitializerList value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator InitializerList(NilType nil) => default(InitializerList);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static InitializerList InitializerList_cast(dynamic value)
        {
            return new InitializerList(value.Type, value.Exprs);
        }
    }
}}}}}