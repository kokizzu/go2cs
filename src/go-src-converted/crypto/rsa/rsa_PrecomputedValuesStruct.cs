//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:30:59 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using crypto = go.crypto_package;
using rand = go.crypto.rand_package;
using subtle = go.crypto.subtle_package;
using errors = go.errors_package;
using hash = go.hash_package;
using io = go.io_package;
using math = go.math_package;
using big = go.math.big_package;
using go;

namespace go {
namespace crypto
{
    public static partial class rsa_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct PrecomputedValues
        {
            // Constructors
            public PrecomputedValues(NilType _)
            {
                this.Dp = default;
                this.Dq = default;
                this.Qinv = default;
                this.CRTValues = default;
            }

            public PrecomputedValues(ref ptr<big.Int> Dp = default, ref ptr<big.Int> Dq = default, ref ptr<big.Int> Qinv = default, slice<CRTValue> CRTValues = default)
            {
                this.Dp = Dp;
                this.Dq = Dq;
                this.Qinv = Qinv;
                this.CRTValues = CRTValues;
            }

            // Enable comparisons between nil and PrecomputedValues struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(PrecomputedValues value, NilType nil) => value.Equals(default(PrecomputedValues));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(PrecomputedValues value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, PrecomputedValues value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, PrecomputedValues value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PrecomputedValues(NilType nil) => default(PrecomputedValues);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static PrecomputedValues PrecomputedValues_cast(dynamic value)
        {
            return new PrecomputedValues(ref value.Dp, ref value.Dq, ref value.Qinv, value.CRTValues);
        }
    }
}}