//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 10:06:50 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bytes = go.bytes_package;
using fmt = go.fmt_package;
using strings = go.strings_package;
using go;

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace ianlancetaylor
{
    public static partial class demangle_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct PtrMem
        {
            // Constructors
            public PtrMem(NilType _)
            {
                this.Class = default;
                this.Member = default;
            }

            public PtrMem(AST Class = default, AST Member = default)
            {
                this.Class = Class;
                this.Member = Member;
            }

            // Enable comparisons between nil and PtrMem struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(PtrMem value, NilType nil) => value.Equals(default(PtrMem));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(PtrMem value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, PtrMem value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, PtrMem value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PtrMem(NilType nil) => default(PtrMem);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static PtrMem PtrMem_cast(dynamic value)
        {
            return new PtrMem(value.Class, value.Member);
        }
    }
}}}}}