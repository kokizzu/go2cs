//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:42:53 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace go
{
    public static partial class strconv_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct extFloat
        {
            // Constructors
            public extFloat(NilType _)
            {
                this.mant = default;
                this.exp = default;
                this.neg = default;
            }

            public extFloat(ulong mant = default, long exp = default, bool neg = default)
            {
                this.mant = mant;
                this.exp = exp;
                this.neg = neg;
            }

            // Enable comparisons between nil and extFloat struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(extFloat value, NilType nil) => value.Equals(default(extFloat));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(extFloat value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, extFloat value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, extFloat value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator extFloat(NilType nil) => default(extFloat);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private static extFloat extFloat_cast(dynamic value)
        {
            return new extFloat(value.mant, value.exp, value.neg);
        }
    }
}