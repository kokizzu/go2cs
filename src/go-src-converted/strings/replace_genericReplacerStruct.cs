//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:41:12 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using io = go.io_package;
using sync = go.sync_package;

#nullable enable

namespace go
{
    public static partial class strings_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct genericReplacer
        {
            // Constructors
            public genericReplacer(NilType _)
            {
                this.root = default;
                this.tableSize = default;
                this.mapping = default;
            }

            public genericReplacer(trieNode root = default, nint tableSize = default, array<byte> mapping = default)
            {
                this.root = root;
                this.tableSize = tableSize;
                this.mapping = mapping;
            }

            // Enable comparisons between nil and genericReplacer struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(genericReplacer value, NilType nil) => value.Equals(default(genericReplacer));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(genericReplacer value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, genericReplacer value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, genericReplacer value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator genericReplacer(NilType nil) => default(genericReplacer);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static genericReplacer genericReplacer_cast(dynamic value)
        {
            return new genericReplacer(value.root, value.tableSize, value.mapping);
        }
    }
}