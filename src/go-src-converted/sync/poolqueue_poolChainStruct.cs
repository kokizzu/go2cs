//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:24:07 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using atomic = go.sync.atomic_package;
using @unsafe = go.@unsafe_package;

#nullable enable

namespace go
{
    public static partial class sync_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct poolChain
        {
            // Constructors
            public poolChain(NilType _)
            {
                this.head = default;
                this.tail = default;
            }

            public poolChain(ref ptr<poolChainElt> head = default, ref ptr<poolChainElt> tail = default)
            {
                this.head = head;
                this.tail = tail;
            }

            // Enable comparisons between nil and poolChain struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(poolChain value, NilType nil) => value.Equals(default(poolChain));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(poolChain value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, poolChain value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, poolChain value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator poolChain(NilType nil) => default(poolChain);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static poolChain poolChain_cast(dynamic value)
        {
            return new poolChain(ref value.head, ref value.tail);
        }
    }
}