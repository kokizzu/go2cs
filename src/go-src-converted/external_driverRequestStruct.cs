//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:01:35 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bytes = go.bytes_package;
using json = go.encoding.json_package;
using fmt = go.fmt_package;
using os = go.os_package;
using exec = go.os.exec_package;
using strings = go.strings_package;
using go;

#nullable enable

namespace go {
namespace golang.org {
namespace x {
namespace tools {
namespace go
{
    public static partial class packages_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct driverRequest
        {
            // Constructors
            public driverRequest(NilType _)
            {
                this.Mode = default;
                this.Env = default;
                this.BuildFlags = default;
                this.Tests = default;
                this.Overlay = default;
            }

            public driverRequest(LoadMode Mode = default, slice<@string> Env = default, slice<@string> BuildFlags = default, bool Tests = default, map<@string, slice<byte>> Overlay = default)
            {
                this.Mode = Mode;
                this.Env = Env;
                this.BuildFlags = BuildFlags;
                this.Tests = Tests;
                this.Overlay = Overlay;
            }

            // Enable comparisons between nil and driverRequest struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(driverRequest value, NilType nil) => value.Equals(default(driverRequest));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(driverRequest value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, driverRequest value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, driverRequest value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator driverRequest(NilType nil) => default(driverRequest);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static driverRequest driverRequest_cast(dynamic value)
        {
            return new driverRequest(value.Mode, value.Env, value.BuildFlags, value.Tests, value.Overlay);
        }
    }
}}}}}