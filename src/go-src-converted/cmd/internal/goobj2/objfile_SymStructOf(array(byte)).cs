//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 05:08:50 UTC
// </auto-generated>
//---------------------------------------------------------
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace @internal
{
    public static partial class goobj2_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Sym
        {
            // Value of the Sym struct
            private readonly array<byte> m_value;

            public Sym(array<byte> value) => m_value = value;

            // Enable implicit conversions between array<byte> and Sym struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Sym(array<byte> value) => new Sym(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator array<byte>(Sym value) => value.m_value;
            
            // Enable comparisons between nil and Sym struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Sym value, NilType nil) => value.Equals(default(Sym));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Sym value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Sym value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Sym value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Sym(NilType nil) => default(Sym);
        }
    }
}}}