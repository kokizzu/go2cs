//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:34:59 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace crypto
{
    public static partial class x509_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct PublicKeyAlgorithm
        {
            // Value of the PublicKeyAlgorithm struct
            private readonly nint m_value;
            
            public PublicKeyAlgorithm(nint value) => m_value = value;

            // Enable implicit conversions between nint and PublicKeyAlgorithm struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PublicKeyAlgorithm(nint value) => new PublicKeyAlgorithm(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nint(PublicKeyAlgorithm value) => value.m_value;
            
            // Enable comparisons between nil and PublicKeyAlgorithm struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(PublicKeyAlgorithm value, NilType nil) => value.Equals(default(PublicKeyAlgorithm));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(PublicKeyAlgorithm value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, PublicKeyAlgorithm value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, PublicKeyAlgorithm value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator PublicKeyAlgorithm(NilType nil) => default(PublicKeyAlgorithm);
        }
    }
}}
