//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2020 October 09 06:05:19 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static go.builtin;
using fmt = go.fmt_package;
using reflect = go.reflect_package;
using strconv = go.strconv_package;
using time = go.time_package;
using go;

#nullable enable
#pragma warning disable CS0660, CS0661

namespace go {
namespace database {
namespace sql
{
    public static partial class driver_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        private partial interface decimalDecompose
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static decimalDecompose As<T>(in T target) => (decimalDecompose<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static decimalDecompose As<T>(ptr<T> target_ptr) => (decimalDecompose<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static decimalDecompose? As(object target) =>
                typeof(decimalDecompose<>).CreateInterfaceHandler<decimalDecompose>(target);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        private class decimalDecompose<T> : decimalDecompose
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

            public decimalDecompose(in T target) => m_target = target;

            public decimalDecompose(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate (byte, bool, slice<byte>, int) DecomposeByPtr(ptr<T> value, slice<byte> buf);
            private delegate (byte, bool, slice<byte>, int) DecomposeByVal(T value, slice<byte> buf);

            private static readonly DecomposeByPtr? s_DecomposeByPtr;
            private static readonly DecomposeByVal? s_DecomposeByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (byte, bool, slice<byte>, int) Decompose(slice<byte> buf)
            {
                T target = m_target;

                if (m_target_is_ptr && !(m_target_ptr is null))
                    target = m_target_ptr.val;

                if (s_DecomposeByPtr is null || !m_target_is_ptr)
                    return s_DecomposeByVal!(target, buf);

                return s_DecomposeByPtr(m_target_ptr, buf);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format;

            [DebuggerStepperBoundary]
            static decimalDecompose()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("Decompose");

                if (!(extensionMethod is null))
                    s_DecomposeByPtr = extensionMethod.CreateStaticDelegate(typeof(DecomposeByPtr)) as DecomposeByPtr;

                extensionMethod = targetType.GetExtensionMethod("Decompose");

                if (!(extensionMethod is null))
                    s_DecomposeByVal = extensionMethod.CreateStaticDelegate(typeof(DecomposeByVal)) as DecomposeByVal;

                if (s_DecomposeByPtr is null && s_DecomposeByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement decimalDecompose.Decompose method", new Exception("Decompose"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator decimalDecompose<T>(in ptr<T> target_ptr) => new decimalDecompose<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator decimalDecompose<T>(in T target) => new decimalDecompose<T>(target);

            // Enable comparisons between nil and decimalDecompose<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(decimalDecompose<T> value, NilType nil) => Activator.CreateInstance<decimalDecompose<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(decimalDecompose<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, decimalDecompose<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, decimalDecompose<T> value) => value != nil;
        }
    }
}}}

namespace go
{
    public static class driver_decimalDecomposeExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.database.sql.driver_package.decimalDecompose target)
        {
            try
            {
                return ((go.database.sql.driver_package.decimalDecompose<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.database.sql.driver_package.decimalDecompose target, out T result)
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
        public static object? _(this go.database.sql.driver_package.decimalDecompose target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.database.sql.driver_package.decimalDecompose<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.database.sql.driver_package.decimalDecompose target, Type type, out object? result)
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