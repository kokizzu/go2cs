//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:26:24 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using syntax = go.cmd.compile.@internal.syntax_package;
using fmt = go.fmt_package;
using atomic = go.sync.atomic_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class types2_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Chan
        {
            // Constructors
            public Chan(NilType _)
            {
                this.dir = default;
                this.elem = default;
            }

            public Chan(ChanDir dir = default, Type elem = default)
            {
                this.dir = dir;
                this.elem = elem;
            }

            // Enable comparisons between nil and Chan struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Chan value, NilType nil) => value.Equals(default(Chan));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Chan value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Chan value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Chan value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Chan(NilType nil) => default(Chan);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Chan Chan_cast(dynamic value)
        {
            return new Chan(value.dir, value.elem);
        }
    }
}}}}