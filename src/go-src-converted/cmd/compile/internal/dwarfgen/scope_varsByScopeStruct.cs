//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:14:25 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using sort = go.sort_package;
using @base = go.cmd.compile.@internal.@base_package;
using ir = go.cmd.compile.@internal.ir_package;
using dwarf = go.cmd.@internal.dwarf_package;
using obj = go.cmd.@internal.obj_package;
using src = go.cmd.@internal.src_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class dwarfgen_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct varsByScope
        {
            // Constructors
            public varsByScope(NilType _)
            {
                this.vars = default;
                this.scopes = default;
            }

            public varsByScope(slice<ptr<dwarf.Var>> vars = default, slice<ir.ScopeID> scopes = default)
            {
                this.vars = vars;
                this.scopes = scopes;
            }

            // Enable comparisons between nil and varsByScope struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(varsByScope value, NilType nil) => value.Equals(default(varsByScope));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(varsByScope value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, varsByScope value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, varsByScope value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator varsByScope(NilType nil) => default(varsByScope);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static varsByScope varsByScope_cast(dynamic value)
        {
            return new varsByScope(value.vars, value.scopes);
        }
    }
}}}}