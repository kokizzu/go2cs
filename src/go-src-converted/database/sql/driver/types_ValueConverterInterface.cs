//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:43:26 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial interface ValueConverter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ValueConverter As<T>(in T target) => (ValueConverter<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ValueConverter As<T>(ptr<T> target_ptr) => (ValueConverter<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ValueConverter? As(object target) =>
                typeof(ValueConverter<>).CreateInterfaceHandler<ValueConverter>(target);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public class ValueConverter<T> : ValueConverter
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

            public ValueConverter(in T target) => m_target = target;

            public ValueConverter(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate (Value, error) ConvertValueByPtr(ptr<T> value, object v);
            private delegate (Value, error) ConvertValueByVal(T value, object v);

            private static readonly ConvertValueByPtr? s_ConvertValueByPtr;
            private static readonly ConvertValueByVal? s_ConvertValueByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (Value, error) ConvertValue(object v)
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_ConvertValueByPtr is null || !m_target_is_ptr)
                    return s_ConvertValueByVal!(target, v);

                return s_ConvertValueByPtr(m_target_ptr!, v);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static ValueConverter()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("ConvertValue");

                if (extensionMethod is not null)
                    s_ConvertValueByPtr = extensionMethod.CreateStaticDelegate(typeof(ConvertValueByPtr)) as ConvertValueByPtr;

                extensionMethod = targetType.GetExtensionMethod("ConvertValue");

                if (extensionMethod is not null)
                    s_ConvertValueByVal = extensionMethod.CreateStaticDelegate(typeof(ConvertValueByVal)) as ConvertValueByVal;

                if (s_ConvertValueByPtr is null && s_ConvertValueByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement ValueConverter.ConvertValue method", new Exception("ConvertValue"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator ValueConverter<T>(in ptr<T> target_ptr) => new ValueConverter<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator ValueConverter<T>(in T target) => new ValueConverter<T>(target);

            // Enable comparisons between nil and ValueConverter<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ValueConverter<T> value, NilType nil) => Activator.CreateInstance<ValueConverter<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ValueConverter<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ValueConverter<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ValueConverter<T> value) => value != nil;
        }
    }
}}}

namespace go
{
    public static class driver_ValueConverterExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.database.sql.driver_package.ValueConverter target)
        {
            try
            {
                return ((go.database.sql.driver_package.ValueConverter<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.database.sql.driver_package.ValueConverter target, out T result)
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
        public static object? _(this go.database.sql.driver_package.ValueConverter target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.database.sql.driver_package.ValueConverter<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.database.sql.driver_package.ValueConverter target, Type type, out object? result)
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