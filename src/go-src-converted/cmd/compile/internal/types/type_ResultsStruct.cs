//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:47:59 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using @base = go.cmd.compile.@internal.@base_package;
using src = go.cmd.@internal.src_package;
using fmt = go.fmt_package;
using sync = go.sync_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class types_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Results
        {
            // Constructors
            public Results(NilType _)
            {
                this.Types = default;
            }

            public Results(slice<ptr<Type>> Types = default)
            {
                this.Types = Types;
            }

            // Enable comparisons between nil and Results struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Results value, NilType nil) => value.Equals(default(Results));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Results value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Results value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Results value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Results(NilType nil) => default(Results);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static Results Results_cast(dynamic value)
        {
            return new Results(value.Types);
        }
    }
}}}}