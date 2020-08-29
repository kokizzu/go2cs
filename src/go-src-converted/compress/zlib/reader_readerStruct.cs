//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:46:03 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bufio = go.bufio_package;
using flate = go.compress.flate_package;
using errors = go.errors_package;
using hash = go.hash_package;
using adler32 = go.hash.adler32_package;
using io = go.io_package;
using go;

namespace go {
namespace compress
{
    public static partial class zlib_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct reader
        {
            // Constructors
            public reader(NilType _)
            {
                this.r = default;
                this.decompressor = default;
                this.digest = default;
                this.err = default;
                this.scratch = default;
            }

            public reader(flate.Reader r = default, io.ReadCloser decompressor = default, hash.Hash32 digest = default, error err = default, array<byte> scratch = default)
            {
                this.r = r;
                this.decompressor = decompressor;
                this.digest = digest;
                this.err = err;
                this.scratch = scratch;
            }

            // Enable comparisons between nil and reader struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(reader value, NilType nil) => value.Equals(default(reader));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(reader value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, reader value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, reader value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator reader(NilType nil) => default(reader);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static reader reader_cast(dynamic value)
        {
            return new reader(value.r, value.decompressor, value.digest, value.err, value.scratch);
        }
    }
}}