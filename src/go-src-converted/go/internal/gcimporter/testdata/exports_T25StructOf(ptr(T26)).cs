//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:02:45 UTC
// </auto-generated>
//---------------------------------------------------------
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using go;

#nullable enable

namespace go {
namespace go {
namespace @internal
{
    public static partial class exports_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct T25
        {
            // Value of the T25 struct
            private readonly ptr<T26> m_value;

            public T25(ptr<T26> value) => m_value = value;

            // Enable implicit conversions between ptr<T26> and T25 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T25(ptr<T26> value) => new T25(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ptr<T26>(T25 value) => value.m_value;
            
            // Enable comparisons between nil and T25 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(T25 value, NilType nil) => value.Equals(default(T25));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(T25 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, T25 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, T25 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T25(NilType nil) => default(T25);
        }
    }
}}}