//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:42:31 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using token = go.go.token_package;
using sort = go.sort_package;
using go;

#nullable enable

namespace go {
namespace go
{
    public static partial class types_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct unifier
        {
            // Constructors
            public unifier(NilType _)
            {
                this.check = default;
                this.exact = default;
                this.x = default;
                this.y = default;
                this.types = default;
            }

            public unifier(ref ptr<Checker> check = default, bool exact = default, tparamsList x = default, tparamsList y = default, slice<Type> types = default)
            {
                this.check = check;
                this.exact = exact;
                this.x = x;
                this.y = y;
                this.types = types;
            }

            // Enable comparisons between nil and unifier struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(unifier value, NilType nil) => value.Equals(default(unifier));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(unifier value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, unifier value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, unifier value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator unifier(NilType nil) => default(unifier);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static unifier unifier_cast(dynamic value)
        {
            return new unifier(ref value.check, value.exact, value.x, value.y, value.types);
        }
    }
}}