//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:41:07 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using bytes = go.bytes_package;
using sha256 = go.crypto.sha256_package;
using base64 = go.encoding.base64_package;
using binary = go.encoding.binary_package;
using errors = go.errors_package;
using fmt = go.fmt_package;
using io = go.io_package;
using strconv = go.strconv_package;
using strings = go.strings_package;
using unicode = go.unicode_package;
using utf8 = go.unicode.utf8_package;
using ed25519 = go.golang.org.x.crypto.ed25519_package;
using go;

#nullable enable

namespace go {
namespace cmd {
namespace vendor {
namespace golang.org {
namespace x {
namespace mod {
namespace sumdb
{
    public static partial class note_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct UnknownVerifierError
        {
            // Constructors
            public UnknownVerifierError(NilType _)
            {
                this.Name = default;
                this.KeyHash = default;
            }

            public UnknownVerifierError(@string Name = default, uint KeyHash = default)
            {
                this.Name = Name;
                this.KeyHash = KeyHash;
            }

            // Enable comparisons between nil and UnknownVerifierError struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(UnknownVerifierError value, NilType nil) => value.Equals(default(UnknownVerifierError));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(UnknownVerifierError value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, UnknownVerifierError value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, UnknownVerifierError value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator UnknownVerifierError(NilType nil) => default(UnknownVerifierError);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static UnknownVerifierError UnknownVerifierError_cast(dynamic value)
        {
            return new UnknownVerifierError(value.Name, value.KeyHash);
        }
    }
}}}}}}}