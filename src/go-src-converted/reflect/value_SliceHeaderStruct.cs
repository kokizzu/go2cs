//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:41:52 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using abi = go.@internal.abi_package;
using itoa = go.@internal.itoa_package;
using unsafeheader = go.@internal.unsafeheader_package;
using math = go.math_package;
using runtime = go.runtime_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class reflect_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct SliceHeader
        {
            // Constructors
            public SliceHeader(NilType _)
            {
                this.Data = default;
                this.Len = default;
                this.Cap = default;
            }

            public SliceHeader(System.UIntPtr Data = default, nint Len = default, nint Cap = default)
            {
                this.Data = Data;
                this.Len = Len;
                this.Cap = Cap;
            }

            // Enable comparisons between nil and SliceHeader struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(SliceHeader value, NilType nil) => value.Equals(default(SliceHeader));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(SliceHeader value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, SliceHeader value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, SliceHeader value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator SliceHeader(NilType nil) => default(SliceHeader);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static SliceHeader SliceHeader_cast(dynamic value)
        {
            return new SliceHeader(value.Data, value.Len, value.Cap);
        }
    }
}