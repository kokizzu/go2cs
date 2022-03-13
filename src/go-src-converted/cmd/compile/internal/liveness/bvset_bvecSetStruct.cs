//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 23:10:39 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bitvec = go.cmd.compile.@internal.bitvec_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class liveness_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct bvecSet
        {
            // Constructors
            public bvecSet(NilType _)
            {
                this.index = default;
                this.uniq = default;
            }

            public bvecSet(slice<nint> index = default, slice<bitvec.BitVec> uniq = default)
            {
                this.index = index;
                this.uniq = uniq;
            }

            // Enable comparisons between nil and bvecSet struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(bvecSet value, NilType nil) => value.Equals(default(bvecSet));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(bvecSet value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, bvecSet value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, bvecSet value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator bvecSet(NilType nil) => default(bvecSet);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static bvecSet bvecSet_cast(dynamic value)
        {
            return new bvecSet(value.index, value.uniq);
        }
    }
}}}}