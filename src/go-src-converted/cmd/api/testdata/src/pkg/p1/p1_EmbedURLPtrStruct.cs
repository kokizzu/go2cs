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
        public partial struct EmbedURLPtr
        {
            // Constructors
            public EmbedURLPtr(NilType _)
            {
                this.ptr<URL> = default;
            }

            public EmbedURLPtr(ref ptr<URL> ptr<URL> = default)
            {
                this.ptr<URL> = ptr<URL>;
            }

            // Enable comparisons between nil and EmbedURLPtr struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(EmbedURLPtr value, NilType nil) => value.Equals(default(EmbedURLPtr));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(EmbedURLPtr value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, EmbedURLPtr value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, EmbedURLPtr value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator EmbedURLPtr(NilType nil) => default(EmbedURLPtr);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static EmbedURLPtr EmbedURLPtr_cast(dynamic value)
        {
            return new EmbedURLPtr(ref value.ptr<URL>);
        }
    }
}}}}}}