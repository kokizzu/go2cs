//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:53:35 UTC
// </auto-generated>
//---------------------------------------------------------
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using go;

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class ssa_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial struct eqclass
        {
            // Value of the eqclass struct
            private readonly slice<ref Value> m_value;

            public eqclass(slice<ref Value> value) => m_value = value;

            // Enable implicit conversions between slice<ref Value> and eqclass struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator eqclass(slice<ref Value> value) => new eqclass(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator slice<ref Value>(eqclass value) => value.m_value;
            
            // Enable comparisons between nil and eqclass struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(eqclass value, NilType nil) => value.Equals(default(eqclass));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(eqclass value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, eqclass value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, eqclass value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator eqclass(NilType nil) => default(eqclass);
        }
    }
}}}}