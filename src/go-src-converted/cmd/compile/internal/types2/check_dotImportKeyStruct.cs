//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:12:27 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using syntax = go.cmd.compile.@internal.syntax_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class types2_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct dotImportKey
        {
            // Constructors
            public dotImportKey(NilType _)
            {
                this.scope = default;
                this.obj = default;
            }

            public dotImportKey(ref ptr<Scope> scope = default, Object obj = default)
            {
                this.scope = scope;
                this.obj = obj;
            }

            // Enable comparisons between nil and dotImportKey struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(dotImportKey value, NilType nil) => value.Equals(default(dotImportKey));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(dotImportKey value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, dotImportKey value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, dotImportKey value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator dotImportKey(NilType nil) => default(dotImportKey);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static dotImportKey dotImportKey_cast(dynamic value)
        {
            return new dotImportKey(ref value.scope, value.obj);
        }
    }
}}}}