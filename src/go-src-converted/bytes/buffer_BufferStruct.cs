//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:28:11 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using io = go.io_package;
using utf8 = go.unicode.utf8_package;

#nullable enable

namespace go
{
    public static partial class bytes_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Buffer
        {
            // Constructors
            public Buffer(NilType _)
            {
                this.buf = default;
                this.off = default;
                this.lastRead = default;
            }

            public Buffer(slice<byte> buf = default, nint off = default, readOp lastRead = default)
            {
                this.buf = buf;
                this.off = off;
                this.lastRead = lastRead;
            }

            // Enable comparisons between nil and Buffer struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Buffer value, NilType nil) => value.Equals(default(Buffer));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Buffer value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Buffer value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Buffer value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Buffer(NilType nil) => default(Buffer);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Buffer Buffer_cast(dynamic value)
        {
            return new Buffer(value.buf, value.off, value.lastRead);
        }
    }
}