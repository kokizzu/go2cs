//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:53:31 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using token = go.go.token_package;
using atomic = go.sync.atomic_package;
using go;

#nullable enable

namespace go {
namespace go
{
    public static partial class types_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Pointer
        {
            // Constructors
            public Pointer(NilType _)
            {
                this.@base = default;
            }

            public Pointer(Type @base = default)
            {
                this.@base = @base;
            }

            // Enable comparisons between nil and Pointer struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Pointer value, NilType nil) => value.Equals(default(Pointer));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Pointer value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Pointer value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Pointer value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Pointer(NilType nil) => default(Pointer);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Pointer Pointer_cast(dynamic value)
        {
            return new Pointer(value.@base);
        }
    }
}}