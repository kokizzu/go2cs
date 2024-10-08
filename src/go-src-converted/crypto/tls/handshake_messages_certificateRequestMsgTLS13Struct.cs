//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:35:55 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using strings = go.strings_package;
using cryptobyte = go.golang.org.x.crypto.cryptobyte_package;
using go;

#nullable enable

namespace go {
namespace crypto
{
    public static partial class tls_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct certificateRequestMsgTLS13
        {
            // Constructors
            public certificateRequestMsgTLS13(NilType _)
            {
                this.raw = default;
                this.ocspStapling = default;
                this.scts = default;
                this.supportedSignatureAlgorithms = default;
                this.supportedSignatureAlgorithmsCert = default;
                this.certificateAuthorities = default;
            }

            public certificateRequestMsgTLS13(slice<byte> raw = default, bool ocspStapling = default, bool scts = default, slice<SignatureScheme> supportedSignatureAlgorithms = default, slice<SignatureScheme> supportedSignatureAlgorithmsCert = default, slice<slice<byte>> certificateAuthorities = default)
            {
                this.raw = raw;
                this.ocspStapling = ocspStapling;
                this.scts = scts;
                this.supportedSignatureAlgorithms = supportedSignatureAlgorithms;
                this.supportedSignatureAlgorithmsCert = supportedSignatureAlgorithmsCert;
                this.certificateAuthorities = certificateAuthorities;
            }

            // Enable comparisons between nil and certificateRequestMsgTLS13 struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(certificateRequestMsgTLS13 value, NilType nil) => value.Equals(default(certificateRequestMsgTLS13));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(certificateRequestMsgTLS13 value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, certificateRequestMsgTLS13 value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, certificateRequestMsgTLS13 value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator certificateRequestMsgTLS13(NilType nil) => default(certificateRequestMsgTLS13);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static certificateRequestMsgTLS13 certificateRequestMsgTLS13_cast(dynamic value)
        {
            return new certificateRequestMsgTLS13(value.raw, value.ocspStapling, value.scts, value.supportedSignatureAlgorithms, value.supportedSignatureAlgorithmsCert, value.certificateAuthorities);
        }
    }
}}