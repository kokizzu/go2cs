//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:43:28 UTC
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
namespace @internal
{
    public static partial class goobj_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Aux : IArray
        {
            // Value of the Aux struct
            private readonly array<byte> m_value;
            
            public nint Length => ((IArray)m_value).Length;

            object? IArray.this[nint index]
            {
                get => ((IArray)m_value)[index];
                set => ((IArray)m_value)[index] = value;
            }

            public ref byte this[nint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref m_value[index];
            }

            public IEnumerator GetEnumerator() => ((IEnumerable)m_value).GetEnumerator();

            public object Clone() => ((ICloneable)m_value).Clone();

            public Aux(array<byte> value) => m_value = value;

            // Enable implicit conversions between array<byte> and Aux struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Aux(array<byte> value) => new Aux(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator array<byte>(Aux value) => value.m_value;
            
            // Enable comparisons between nil and Aux struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Aux value, NilType nil) => value.Equals(default(Aux));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Aux value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Aux value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Aux value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Aux(NilType nil) => default(Aux);
        }
    }
}}}
