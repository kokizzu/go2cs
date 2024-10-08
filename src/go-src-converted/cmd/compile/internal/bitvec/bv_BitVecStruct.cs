//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:59:01 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bits = go.math.bits_package;
using @base = go.cmd.compile.@internal.@base_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class bitvec_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct BitVec
        {
            // Constructors
            public BitVec(NilType _)
            {
                this.N = default;
                this.B = default;
            }

            public BitVec(int N = default, slice<uint> B = default)
            {
                this.N = N;
                this.B = B;
            }

            // Enable comparisons between nil and BitVec struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(BitVec value, NilType nil) => value.Equals(default(BitVec));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(BitVec value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, BitVec value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, BitVec value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator BitVec(NilType nil) => default(BitVec);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static BitVec BitVec_cast(dynamic value)
        {
            return new BitVec(value.N, value.B);
        }
    }
}}}}