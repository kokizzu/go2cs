//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:41:39 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using unsafeheader = go.@internal.unsafeheader_package;
using strconv = go.strconv_package;
using sync = go.sync_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class reflect_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        [PromotedStruct(typeof(funcType))]
        private partial struct funcTypeFixed4
        {
            // funcType structure promotion - sourced from value copy
            private readonly ptr<funcType> m_funcTypeRef;

            private ref funcType funcType_val => ref m_funcTypeRef.Value;

            public ref ushort inCount => ref m_funcTypeRef.Value.inCount;

            public ref ushort outCount => ref m_funcTypeRef.Value.outCount;

            // Constructors
            public funcTypeFixed4(NilType _)
            {
                this.m_funcTypeRef = new ptr<funcType>(new funcType(nil));
                this.args = default;
            }

            public funcTypeFixed4(funcType funcType = default, array<ptr<rtype>> args = default)
            {
                this.m_funcTypeRef = new ptr<funcType>(funcType);
                this.args = args;
            }

            // Enable comparisons between nil and funcTypeFixed4 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(funcTypeFixed4 value, NilType nil) => value.Equals(default(funcTypeFixed4));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(funcTypeFixed4 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, funcTypeFixed4 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, funcTypeFixed4 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator funcTypeFixed4(NilType nil) => default(funcTypeFixed4);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static funcTypeFixed4 funcTypeFixed4_cast(dynamic value)
        {
            return new funcTypeFixed4(value.funcType, value.args);
        }
    }
}