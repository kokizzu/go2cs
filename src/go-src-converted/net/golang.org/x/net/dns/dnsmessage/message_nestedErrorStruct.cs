//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:15:38 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using fmt = go.fmt_package;
using go;

#nullable enable

namespace go {
namespace golang.org {
namespace x {
namespace net {
namespace dns
{
    public static partial class dnsmessage_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct nestedError
        {
            // Constructors
            public nestedError(NilType _)
            {
                this.s = default;
                this.err = default;
            }

            public nestedError(@string s = default, error err = default)
            {
                this.s = s;
                this.err = err;
            }

            // Enable comparisons between nil and nestedError struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(nestedError value, NilType nil) => value.Equals(default(nestedError));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(nestedError value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, nestedError value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, nestedError value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nestedError(NilType nil) => default(nestedError);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static nestedError nestedError_cast(dynamic value)
        {
            return new nestedError(value.s, value.err);
        }
    }
}}}}}