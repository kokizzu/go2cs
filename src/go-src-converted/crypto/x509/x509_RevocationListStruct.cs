//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 04:55:09 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using bytes = go.bytes_package;
using crypto = go.crypto_package;
using dsa = go.crypto.dsa_package;
using ecdsa = go.crypto.ecdsa_package;
using ed25519 = go.crypto.ed25519_package;
using elliptic = go.crypto.elliptic_package;
using rsa = go.crypto.rsa_package;
using sha1 = go.crypto.sha1_package;
using _sha256_ = go.crypto.sha256_package;
using _sha512_ = go.crypto.sha512_package;
using pkix = go.crypto.x509.pkix_package;
using asn1 = go.encoding.asn1_package;
using pem = go.encoding.pem_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using io = go.io_package;
using big = go.math.big_package;
using net = go.net_package;
using url = go.net.url_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using time = go.time_package;
using utf8 = go.unicode.utf8_package;
using cryptobyte = go.golang.org.x.crypto.cryptobyte_package;
using cryptobyte_asn1 = go.golang.org.x.crypto.cryptobyte.asn1_package;
using go;

#nullable enable

namespace go {
namespace crypto
{
    public static partial class x509_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial struct RevocationList
        {
            // Constructors
            public RevocationList(NilType _)
            {
                this.SignatureAlgorithm = default;
                this.RevokedCertificates = default;
                this.Number = default;
                this.ThisUpdate = default;
                this.NextUpdate = default;
                this.ExtraExtensions = default;
            }

            public RevocationList(SignatureAlgorithm SignatureAlgorithm = default, slice<pkix.RevokedCertificate> RevokedCertificates = default, ref ptr<big.Int> Number = default, time.Time ThisUpdate = default, time.Time NextUpdate = default, slice<pkix.Extension> ExtraExtensions = default)
            {
                this.SignatureAlgorithm = SignatureAlgorithm;
                this.RevokedCertificates = RevokedCertificates;
                this.Number = Number;
                this.ThisUpdate = ThisUpdate;
                this.NextUpdate = NextUpdate;
                this.ExtraExtensions = ExtraExtensions;
            }

            // Enable comparisons between nil and RevocationList struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(RevocationList value, NilType nil) => value.Equals(default(RevocationList));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(RevocationList value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, RevocationList value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, RevocationList value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator RevocationList(NilType nil) => default(RevocationList);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public static RevocationList RevocationList_cast(dynamic value)
        {
            return new RevocationList(value.SignatureAlgorithm, value.RevokedCertificates, ref value.Number, value.ThisUpdate, value.NextUpdate, value.ExtraExtensions);
        }
    }
}}