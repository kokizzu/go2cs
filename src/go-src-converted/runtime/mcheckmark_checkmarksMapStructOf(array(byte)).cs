//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:25:06 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

#nullable enable

namespace go
{
    public static partial class runtime_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct checkmarksMap : IArray
        {
            // Value of the checkmarksMap struct
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

            public checkmarksMap(array<byte> value) => m_value = value;

            // Enable implicit conversions between array<byte> and checkmarksMap struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator checkmarksMap(array<byte> value) => new checkmarksMap(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator array<byte>(checkmarksMap value) => value.m_value;
            
            // Enable comparisons between nil and checkmarksMap struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(checkmarksMap value, NilType nil) => value.Equals(default(checkmarksMap));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(checkmarksMap value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, checkmarksMap value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, checkmarksMap value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator checkmarksMap(NilType nil) => default(checkmarksMap);
        }
    }
}
