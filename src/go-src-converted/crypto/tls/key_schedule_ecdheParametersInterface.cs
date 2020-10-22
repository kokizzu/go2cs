//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 04:56:00 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using elliptic = go.crypto.elliptic_package;
using hmac = go.crypto.hmac_package;
using errors = go.errors_package;
using hash = go.hash_package;
using io = go.io_package;
using big = go.math.big_package;
using cryptobyte = go.golang.org.x.crypto.cryptobyte_package;
using curve25519 = go.golang.org.x.crypto.curve25519_package;
using hkdf = go.golang.org.x.crypto.hkdf_package;
using go;

#nullable enable
#pragma warning disable CS0660, CS0661

namespace go {
namespace crypto
{
    public static partial class tls_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial interface ecdheParameters
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ecdheParameters As<T>(in T target) => (ecdheParameters<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ecdheParameters As<T>(ptr<T> target_ptr) => (ecdheParameters<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ecdheParameters? As(object target) =>
                typeof(ecdheParameters<>).CreateInterfaceHandler<ecdheParameters>(target);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private class ecdheParameters<T> : ecdheParameters
        {
            private T m_target = default!;
            private readonly ptr<T>? m_target_ptr;
            private readonly bool m_target_is_ptr;

            public ref T Target
            {
                get
                {
                    if (m_target_is_ptr && !(m_target_ptr is null))
                        return ref m_target_ptr.val;

                    return ref m_target;
                }
            }

            public ecdheParameters(in T target) => m_target = target;

            public ecdheParameters(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate slice<byte> CurveIDByPtr(ptr<T> value);
            private delegate slice<byte> CurveIDByVal(T value);

            private static readonly CurveIDByPtr? s_CurveIDByPtr;
            private static readonly CurveIDByVal? s_CurveIDByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public slice<byte> CurveID()
            {
                T target = m_target;

                if (m_target_is_ptr && !(m_target_ptr is null))
                    target = m_target_ptr.val;

                if (s_CurveIDByPtr is null || !m_target_is_ptr)
                    return s_CurveIDByVal!(target);

                return s_CurveIDByPtr(m_target_ptr);
            }

            private delegate slice<byte> PublicKeyByPtr(ptr<T> value);
            private delegate slice<byte> PublicKeyByVal(T value);

            private static readonly PublicKeyByPtr? s_PublicKeyByPtr;
            private static readonly PublicKeyByVal? s_PublicKeyByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public slice<byte> PublicKey()
            {
                T target = m_target;

                if (m_target_is_ptr && !(m_target_ptr is null))
                    target = m_target_ptr.val;

                if (s_PublicKeyByPtr is null || !m_target_is_ptr)
                    return s_PublicKeyByVal!(target);

                return s_PublicKeyByPtr(m_target_ptr);
            }

            private delegate slice<byte> SharedKeyByPtr(ptr<T> value, slice<byte> peerPublicKey);
            private delegate slice<byte> SharedKeyByVal(T value, slice<byte> peerPublicKey);

            private static readonly SharedKeyByPtr? s_SharedKeyByPtr;
            private static readonly SharedKeyByVal? s_SharedKeyByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public slice<byte> SharedKey(slice<byte> peerPublicKey)
            {
                T target = m_target;

                if (m_target_is_ptr && !(m_target_ptr is null))
                    target = m_target_ptr.val;

                if (s_SharedKeyByPtr is null || !m_target_is_ptr)
                    return s_SharedKeyByVal!(target, peerPublicKey);

                return s_SharedKeyByPtr(m_target_ptr, peerPublicKey);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format;

            [DebuggerStepperBoundary]
            static ecdheParameters()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("CurveID");

                if (!(extensionMethod is null))
                    s_CurveIDByPtr = extensionMethod.CreateStaticDelegate(typeof(CurveIDByPtr)) as CurveIDByPtr;

                extensionMethod = targetType.GetExtensionMethod("CurveID");

                if (!(extensionMethod is null))
                    s_CurveIDByVal = extensionMethod.CreateStaticDelegate(typeof(CurveIDByVal)) as CurveIDByVal;

                if (s_CurveIDByPtr is null && s_CurveIDByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement ecdheParameters.CurveID method", new Exception("CurveID"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("PublicKey");

                if (!(extensionMethod is null))
                    s_PublicKeyByPtr = extensionMethod.CreateStaticDelegate(typeof(PublicKeyByPtr)) as PublicKeyByPtr;

                extensionMethod = targetType.GetExtensionMethod("PublicKey");

                if (!(extensionMethod is null))
                    s_PublicKeyByVal = extensionMethod.CreateStaticDelegate(typeof(PublicKeyByVal)) as PublicKeyByVal;

                if (s_PublicKeyByPtr is null && s_PublicKeyByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement ecdheParameters.PublicKey method", new Exception("PublicKey"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("SharedKey");

                if (!(extensionMethod is null))
                    s_SharedKeyByPtr = extensionMethod.CreateStaticDelegate(typeof(SharedKeyByPtr)) as SharedKeyByPtr;

                extensionMethod = targetType.GetExtensionMethod("SharedKey");

                if (!(extensionMethod is null))
                    s_SharedKeyByVal = extensionMethod.CreateStaticDelegate(typeof(SharedKeyByVal)) as SharedKeyByVal;

                if (s_SharedKeyByPtr is null && s_SharedKeyByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement ecdheParameters.SharedKey method", new Exception("SharedKey"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator ecdheParameters<T>(in ptr<T> target_ptr) => new ecdheParameters<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator ecdheParameters<T>(in T target) => new ecdheParameters<T>(target);

            // Enable comparisons between nil and ecdheParameters<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ecdheParameters<T> value, NilType nil) => Activator.CreateInstance<ecdheParameters<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ecdheParameters<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ecdheParameters<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ecdheParameters<T> value) => value != nil;
        }
    }
}}

namespace go
{
    public static class tls_ecdheParametersExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.crypto.tls_package.ecdheParameters target)
        {
            try
            {
                return ((go.crypto.tls_package.ecdheParameters<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.crypto.tls_package.ecdheParameters target, out T result)
        {
            try
            {
                result = target._<T>();
                return true;
            }
            catch (PanicException)
            {
                result = default!;
                return false;
            }
        }

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static object? _(this go.crypto.tls_package.ecdheParameters target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.crypto.tls_package.ecdheParameters<>).GetExplicitGenericConversionOperator(type));

                if (conversionOperator is null)
                    throw new PanicException($"interface conversion: failed to create converter for {GetGoTypeName(target.GetType())} to {GetGoTypeName(type)}");

                dynamic? result = conversionOperator.Invoke(null, new object[] { target });
                return result?.Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(type)}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _(this go.crypto.tls_package.ecdheParameters target, Type type, out object? result)
        {
            try
            {
                result = target._(type);
                return true;
            }
            catch (PanicException)
            {
                result = type.IsValueType ? Activator.CreateInstance(type) : null;
                return false;
            }
        }
    }
}