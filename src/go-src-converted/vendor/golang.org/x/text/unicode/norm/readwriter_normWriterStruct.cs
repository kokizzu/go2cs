//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:47:09 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using io = go.io_package;
using go;

#nullable enable

namespace go {
namespace vendor {
namespace golang.org {
namespace x {
namespace text {
namespace unicode
{
    public static partial class norm_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct normWriter
        {
            // Constructors
            public normWriter(NilType _)
            {
                this.rb = default;
                this.w = default;
                this.buf = default;
            }

            public normWriter(reorderBuffer rb = default, io.Writer w = default, slice<byte> buf = default)
            {
                this.rb = rb;
                this.w = w;
                this.buf = buf;
            }

            // Enable comparisons between nil and normWriter struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(normWriter value, NilType nil) => value.Equals(default(normWriter));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(normWriter value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, normWriter value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, normWriter value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator normWriter(NilType nil) => default(normWriter);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static normWriter normWriter_cast(dynamic value)
        {
            return new normWriter(value.rb, value.w, value.buf);
        }
    }
}}}}}}