//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:24:23 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;


#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct dlogPerM
        {
            // Constructors
            public dlogPerM(NilType _)
            {
            }
            // Enable comparisons between nil and dlogPerM struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(dlogPerM value, NilType nil) => value.Equals(default(dlogPerM));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(dlogPerM value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, dlogPerM value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, dlogPerM value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator dlogPerM(NilType nil) => default(dlogPerM);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static dlogPerM dlogPerM_cast(dynamic value)
        {
            return new dlogPerM();
        }
    }
}