//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:47:23 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using sync = go.sync_package;
using go;

#nullable enable

namespace go {
namespace vendor {
namespace golang.org {
namespace x {
namespace text {
namespace unicode
{
    public static partial class norm_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct nfkcTrie
        {
            // Constructors
            public nfkcTrie(NilType _)
            {
            }
            // Enable comparisons between nil and nfkcTrie struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(nfkcTrie value, NilType nil) => value.Equals(default(nfkcTrie));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(nfkcTrie value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, nfkcTrie value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, nfkcTrie value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator nfkcTrie(NilType nil) => default(nfkcTrie);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static nfkcTrie nfkcTrie_cast(dynamic value)
        {
            return new nfkcTrie();
        }
    }
}}}}}}