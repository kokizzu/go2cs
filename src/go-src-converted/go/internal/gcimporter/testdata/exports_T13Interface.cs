//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:42:20 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ast = go.go.ast_package;
using go;

#nullable enable
#pragma warning disable CS0660, CS0661

namespace go {
namespace go {
namespace @internal
{
    public static partial class exports_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial interface T13
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static T13 As<T>(in T target) => (T13<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static T13 As<T>(ptr<T> target_ptr) => (T13<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static T13? As(object target) =>
                typeof(T13<>).CreateInterfaceHandler<T13>(target);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public class T13<T> : T13
        {
            private T m_target = default!;
            private readonly ptr<T>? m_target_ptr;
            private readonly bool m_target_is_ptr;

            public ref T Target
            {
                get
                {
                    if (m_target_is_ptr && m_target_ptr is not null)
                        return ref m_target_ptr.val;

                    return ref m_target;
                }
            }

            public T13(in T target) => m_target = target;

            public T13(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate float m1ByPtr(ptr<T> value);
            private delegate float m1ByVal(T value);

            private static readonly m1ByPtr? s_m1ByPtr;
            private static readonly m1ByVal? s_m1ByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float m1()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_m1ByPtr is null || !m_target_is_ptr)
                    return s_m1ByVal!(target);

                return s_m1ByPtr(m_target_ptr!);
            }

            private delegate float m2ByPtr(ptr<T> value, nint _p0);
            private delegate float m2ByVal(T value, nint _p0);

            private static readonly m2ByPtr? s_m2ByPtr;
            private static readonly m2ByVal? s_m2ByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float m2(nint _p0)
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_m2ByPtr is null || !m_target_is_ptr)
                    return s_m2ByVal!(target, _p0);

                return s_m2ByPtr(m_target_ptr!, _p0);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static T13()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("m1");

                if (extensionMethod is not null)
                    s_m1ByPtr = extensionMethod.CreateStaticDelegate(typeof(m1ByPtr)) as m1ByPtr;

                extensionMethod = targetType.GetExtensionMethod("m1");

                if (extensionMethod is not null)
                    s_m1ByVal = extensionMethod.CreateStaticDelegate(typeof(m1ByVal)) as m1ByVal;

                if (s_m1ByPtr is null && s_m1ByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement T13.m1 method", new Exception("m1"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("m2");

                if (extensionMethod is not null)
                    s_m2ByPtr = extensionMethod.CreateStaticDelegate(typeof(m2ByPtr)) as m2ByPtr;

                extensionMethod = targetType.GetExtensionMethod("m2");

                if (extensionMethod is not null)
                    s_m2ByVal = extensionMethod.CreateStaticDelegate(typeof(m2ByVal)) as m2ByVal;

                if (s_m2ByPtr is null && s_m2ByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement T13.m2 method", new Exception("m2"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator T13<T>(in ptr<T> target_ptr) => new T13<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator T13<T>(in T target) => new T13<T>(target);

            // Enable comparisons between nil and T13<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(T13<T> value, NilType nil) => Activator.CreateInstance<T13<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(T13<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, T13<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, T13<T> value) => value != nil;
        }
    }
}}}

namespace go
{
    public static class exports_T13Extensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.go.@internal.gcimporter.exports_package.T13 target)
        {
            try
            {
                return ((go.go.@internal.gcimporter.exports_package.T13<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.go.@internal.gcimporter.exports_package.T13 target, out T result)
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

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static object? _(this go.go.@internal.gcimporter.exports_package.T13 target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.go.@internal.gcimporter.exports_package.T13<>).GetExplicitGenericConversionOperator(type));

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

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _(this go.go.@internal.gcimporter.exports_package.T13 target, Type type, out object? result)
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