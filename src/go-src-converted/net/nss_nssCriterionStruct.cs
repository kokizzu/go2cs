//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:30:01 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using bytealg = go.@internal.bytealg_package;
using io = go.io_package;
using os = go.os_package;

#nullable enable

namespace go
{
    public static partial class net_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct nssCriterion
        {
            // Constructors
            public nssCriterion(NilType _)
            {
                this.negate = default;
                this.status = default;
                this.action = default;
            }

            public nssCriterion(bool negate = default, @string status = default, @string action = default)
            {
                this.negate = negate;
                this.status = status;
                this.action = action;
            }

            // Enable comparisons between nil and nssCriterion struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(nssCriterion value, NilType nil) => value.Equals(default(nssCriterion));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(nssCriterion value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, nssCriterion value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, nssCriterion value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nssCriterion(NilType nil) => default(nssCriterion);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static nssCriterion nssCriterion_cast(dynamic value)
        {
            return new nssCriterion(value.negate, value.status, value.action);
        }
    }
}