//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:55:01 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using types = go.cmd.compile.@internal.types_package;
using objabi = go.cmd.@internal.objabi_package;
using src = go.cmd.@internal.src_package;
using fmt = go.fmt_package;
using @unsafe = go.@unsafe_package;
using go;

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class ssa_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct dentry
        {
            // Constructors
            public dentry(NilType _)
            {
                this.@out = default;
                this.@in = default;
            }

            public dentry(array<register> @out = default, array<array<register>> @in = default)
            {
                this.@out = @out;
                this.@in = @in;
            }

            // Enable comparisons between nil and dentry struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(dentry value, NilType nil) => value.Equals(default(dentry));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(dentry value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, dentry value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, dentry value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator dentry(NilType nil) => default(dentry);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static dentry dentry_cast(dynamic value)
        {
            return new dentry(value.@out, value.@in);
        }
    }
}}}}