//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:42:29 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace go {
namespace @internal
{
    public static partial class aliases_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct T0 : IArray
        {
            // Value of the T0 struct
            private readonly array<nint> m_value;
            
            public nint Length => ((IArray)m_value).Length;

            object? IArray.this[nint index]
            {
                get => ((IArray)m_value)[index];
                set => ((IArray)m_value)[index] = value;
            }

            public ref nint this[nint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref m_value[index];
            }

            public IEnumerator GetEnumerator() => ((IEnumerable)m_value).GetEnumerator();

            public object Clone() => ((ICloneable)m_value).Clone();

            public T0(array<nint> value) => m_value = value;

            // Enable implicit conversions between array<nint> and T0 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T0(array<nint> value) => new T0(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator array<nint>(T0 value) => value.m_value;
            
            // Enable comparisons between nil and T0 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(T0 value, NilType nil) => value.Equals(default(T0));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(T0 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, T0 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, T0 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T0(NilType nil) => default(T0);
        }
    }
}}}
