//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:34:48 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace link {
namespace @internal
{
    public static partial class loadxcoff_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct xcoffBiobuf
        {
            // Value of the xcoffBiobuf struct
            private readonly bio.Reader m_value;
            
            public xcoffBiobuf(bio.Reader value) => m_value = value;

            // Enable implicit conversions between bio.Reader and xcoffBiobuf struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator xcoffBiobuf(bio.Reader value) => new xcoffBiobuf(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator bio.Reader(xcoffBiobuf value) => value.m_value;
            
            // Enable comparisons between nil and xcoffBiobuf struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(xcoffBiobuf value, NilType nil) => value.Equals(default(xcoffBiobuf));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(xcoffBiobuf value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, xcoffBiobuf value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, xcoffBiobuf value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator xcoffBiobuf(NilType nil) => default(xcoffBiobuf);
        }
    }
}}}}
