//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:54:11 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ptwo = go.p2_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace api {
namespace testdata {
namespace src {
namespace pkg
{
    public static partial class p1_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct TPtrUnexported
        {
            // Constructors
            public TPtrUnexported(NilType _)
            {
                this.ptr<common> = default;
            }

            public TPtrUnexported(ref ptr<common> ptr<common> = default)
            {
                this.ptr<common> = ptr<common>;
            }

            // Enable comparisons between nil and TPtrUnexported struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(TPtrUnexported value, NilType nil) => value.Equals(default(TPtrUnexported));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(TPtrUnexported value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, TPtrUnexported value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, TPtrUnexported value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator TPtrUnexported(NilType nil) => default(TPtrUnexported);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static TPtrUnexported TPtrUnexported_cast(dynamic value)
        {
            return new TPtrUnexported(ref value.ptr<common>);
        }
    }
}}}}}}