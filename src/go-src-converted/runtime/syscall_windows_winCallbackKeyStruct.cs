//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:12:07 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using abi = go.@internal.abi_package;
using sys = go.runtime.@internal.sys_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct winCallbackKey
        {
            // Constructors
            public winCallbackKey(NilType _)
            {
                this.fn = default;
                this.cdecl = default;
            }

            public winCallbackKey(ref ptr<funcval> fn = default, bool cdecl = default)
            {
                this.fn = fn;
                this.cdecl = cdecl;
            }

            // Enable comparisons between nil and winCallbackKey struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(winCallbackKey value, NilType nil) => value.Equals(default(winCallbackKey));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(winCallbackKey value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, winCallbackKey value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, winCallbackKey value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator winCallbackKey(NilType nil) => default(winCallbackKey);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static winCallbackKey winCallbackKey_cast(dynamic value)
        {
            return new winCallbackKey(ref value.fn, value.cdecl);
        }
    }
}