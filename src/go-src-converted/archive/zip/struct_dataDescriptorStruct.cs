//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:42:37 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fs = go.io.fs_package;
using path = go.path_package;
using time = go.time_package;
using go;

#nullable enable

namespace go {
namespace archive
{
    public static partial class zip_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct dataDescriptor
        {
            // Constructors
            public dataDescriptor(NilType _)
            {
                this.crc32 = default;
                this.compressedSize = default;
                this.uncompressedSize = default;
            }

            public dataDescriptor(uint crc32 = default, ulong compressedSize = default, ulong uncompressedSize = default)
            {
                this.crc32 = crc32;
                this.compressedSize = compressedSize;
                this.uncompressedSize = uncompressedSize;
            }

            // Enable comparisons between nil and dataDescriptor struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(dataDescriptor value, NilType nil) => value.Equals(default(dataDescriptor));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(dataDescriptor value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, dataDescriptor value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, dataDescriptor value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator dataDescriptor(NilType nil) => default(dataDescriptor);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static dataDescriptor dataDescriptor_cast(dynamic value)
        {
            return new dataDescriptor(value.crc32, value.compressedSize, value.uncompressedSize);
        }
    }
}}