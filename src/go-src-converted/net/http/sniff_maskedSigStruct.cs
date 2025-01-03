//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:37:33 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using binary = go.encoding.binary_package;
using go;

#nullable enable

namespace go {
namespace net
{
    public static partial class http_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct maskedSig
        {
            // Constructors
            public maskedSig(NilType _)
            {
                this.mask = default;
                this.pat = default;
                this.skipWS = default;
                this.ct = default;
            }

            public maskedSig(slice<byte> mask = default, slice<byte> pat = default, bool skipWS = default, @string ct = default)
            {
                this.mask = mask;
                this.pat = pat;
                this.skipWS = skipWS;
                this.ct = ct;
            }

            // Enable comparisons between nil and maskedSig struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(maskedSig value, NilType nil) => value.Equals(default(maskedSig));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(maskedSig value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, maskedSig value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, maskedSig value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator maskedSig(NilType nil) => default(maskedSig);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static maskedSig maskedSig_cast(dynamic value)
        {
            return new maskedSig(value.mask, value.pat, value.skipWS, value.ct);
        }
    }
}}