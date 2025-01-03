//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:30:29 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using crypto = go.crypto_package;
using aes = go.crypto.aes_package;
using cipher = go.crypto.cipher_package;
using elliptic = go.crypto.elliptic_package;
using randutil = go.crypto.@internal.randutil_package;
using sha512 = go.crypto.sha512_package;
using errors = go.errors_package;
using io = go.io_package;
using big = go.math.big_package;
using cryptobyte = go.golang.org.x.crypto.cryptobyte_package;
using asn1 = go.golang.org.x.crypto.cryptobyte.asn1_package;
using go;

#nullable enable

namespace go {
namespace crypto
{
    public static partial class ecdsa_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        private partial struct zr
        {
            // Reader.Read function promotion
            private delegate (nint, error) ReadByVal(T value, slice<byte> p);
            private delegate (nint, error) ReadByRef(ref T value, slice<byte> p);

            private static readonly ReadByVal s_ReadByVal;
            private static readonly ReadByRef s_ReadByRef;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (nint, error) Read(slice<byte> p) => s_ReadByRef?.Invoke(ref this, p) ?? s_ReadByVal?.Invoke(this, p) ?? Reader?.Read(p) ?? throw new PanicException(RuntimeErrorPanic.NilPointerDereference);
            
            [DebuggerStepperBoundary]
            static zr()
            {
                Type targetType = typeof(zr);
                MethodInfo extensionMethod;
                
                extensionMethod = targetType.GetExtensionMethodSearchingPromotions("Read");

                if (extensionMethod is not null)
                {
                    s_ReadByRef = extensionMethod.CreateStaticDelegate(typeof(ReadByRef)) as ReadByRef;

                    if (s_ReadByRef is null)
                        s_ReadByVal = extensionMethod.CreateStaticDelegate(typeof(ReadByVal)) as ReadByVal;
                }
            }

            // Constructors
            public zr(NilType _)
            {
                this.Reader = default;
            }

            public zr(io.Reader Reader = default)
            {
                this.Reader = Reader;
            }

            // Enable comparisons between nil and zr struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(zr value, NilType nil) => value.Equals(default(zr));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(zr value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, zr value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, zr value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator zr(NilType nil) => default(zr);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        private static zr zr_cast(dynamic value)
        {
            return new zr(value.Reader);
        }
    }
}}