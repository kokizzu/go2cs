//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:35:42 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using errors = go.errors_package;
using io = go.io_package;
using fs = go.io.fs_package;
using time = go.time_package;

#nullable enable

namespace go
{
    public static partial class embed_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct file
        {
            // Constructors
            public file(NilType _)
            {
                this.name = default;
                this.data = default;
                this.hash = default;
            }

            public file(@string name = default, @string data = default, array<byte> hash = default)
            {
                this.name = name;
                this.data = data;
                this.hash = hash;
            }

            // Enable comparisons between nil and file struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(file value, NilType nil) => value.Equals(default(file));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(file value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, file value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, file value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator file(NilType nil) => default(file);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static file file_cast(dynamic value)
        {
            return new file(value.name, value.data, value.hash);
        }
    }
}