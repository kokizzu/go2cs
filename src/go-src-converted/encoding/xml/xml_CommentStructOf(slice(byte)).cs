//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 August 29 08:36:12 UTC
// </auto-generated>
//---------------------------------------------------------
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using go;

namespace go {
namespace encoding
{
    public static partial class xml_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct Comment
        {
            // Value of the Comment struct
            private readonly slice<byte> m_value;

            public Comment(slice<byte> value) => m_value = value;

            // Enable implicit conversions between slice<byte> and Comment struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Comment(slice<byte> value) => new Comment(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator slice<byte>(Comment value) => value.m_value;
            
            // Enable comparisons between nil and Comment struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Comment value, NilType nil) => value.Equals(default(Comment));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Comment value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Comment value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Comment value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Comment(NilType nil) => default(Comment);
        }
    }
}}