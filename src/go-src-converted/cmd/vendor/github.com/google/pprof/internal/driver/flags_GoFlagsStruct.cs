//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:36:31 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using flag = go.flag_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace github.com {
namespace google {
namespace pprof {
namespace @internal
{
    public static partial class driver_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct GoFlags
        {
            // Constructors
            public GoFlags(NilType _)
            {
                this.UsageMsgs = default;
            }

            public GoFlags(slice<@string> UsageMsgs = default)
            {
                this.UsageMsgs = UsageMsgs;
            }

            // Enable comparisons between nil and GoFlags struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(GoFlags value, NilType nil) => value.Equals(default(GoFlags));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(GoFlags value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, GoFlags value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, GoFlags value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator GoFlags(NilType nil) => default(GoFlags);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static GoFlags GoFlags_cast(dynamic value)
        {
            return new GoFlags(value.UsageMsgs);
        }
    }
}}}}}}}