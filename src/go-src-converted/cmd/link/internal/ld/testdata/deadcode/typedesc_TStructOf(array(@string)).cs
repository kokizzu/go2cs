//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:35:38 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class main_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct T : IArray
        {
            // Value of the T struct
            private readonly array<@string> m_value;
            
            public nint Length => ((IArray)m_value).Length;

            object? IArray.this[nint index]
            {
                get => ((IArray)m_value)[index];
                set => ((IArray)m_value)[index] = value;
            }

            public ref @string this[nint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref m_value[index];
            }

            public IEnumerator GetEnumerator() => ((IEnumerable)m_value).GetEnumerator();

            public object Clone() => ((ICloneable)m_value).Clone();

            public T(array<@string> value) => m_value = value;

            // Enable implicit conversions between array<@string> and T struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T(array<@string> value) => new T(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator array<@string>(T value) => value.m_value;
            
            // Enable comparisons between nil and T struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(T value, NilType nil) => value.Equals(default(T));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(T value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, T value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, T value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T(NilType nil) => default(T);
        }
    }
}
