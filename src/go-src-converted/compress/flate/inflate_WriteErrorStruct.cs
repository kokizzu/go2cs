//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:29:14 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bufio = go.bufio_package;
using io = go.io_package;
using bits = go.math.bits_package;
using strconv = go.strconv_package;
using sync = go.sync_package;
using go;

#nullable enable

namespace go {
namespace compress
{
    public static partial class flate_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct WriteError
        {
            // Constructors
            public WriteError(NilType _)
            {
                this.Offset = default;
                this.Err = default;
            }

            public WriteError(long Offset = default, error Err = default)
            {
                this.Offset = Offset;
                this.Err = Err;
            }

            // Enable comparisons between nil and WriteError struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(WriteError value, NilType nil) => value.Equals(default(WriteError));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(WriteError value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, WriteError value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, WriteError value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator WriteError(NilType nil) => default(WriteError);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static WriteError WriteError_cast(dynamic value)
        {
            return new WriteError(value.Offset, value.Err);
        }
    }
}}