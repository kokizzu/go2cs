//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:44:00 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using image = go.image_package;
using color = go.image.color_package;
using imageutil = go.image.@internal.imageutil_package;
using go;

#nullable enable
#pragma warning disable CS0660, CS0661

namespace go {
namespace image
{
    public static partial class draw_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial interface Quantizer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static Quantizer As<T>(in T target) => (Quantizer<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static Quantizer As<T>(ptr<T> target_ptr) => (Quantizer<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static Quantizer? As(object target) =>
                typeof(Quantizer<>).CreateInterfaceHandler<Quantizer>(target);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public class Quantizer<T> : Quantizer
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

            public Quantizer(in T target) => m_target = target;

            public Quantizer(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate color.Palette QuantizeByPtr(ptr<T> value, color.Palette p, image.Image m);
            private delegate color.Palette QuantizeByVal(T value, color.Palette p, image.Image m);

            private static readonly QuantizeByPtr? s_QuantizeByPtr;
            private static readonly QuantizeByVal? s_QuantizeByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public color.Palette Quantize(color.Palette p, image.Image m)
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_QuantizeByPtr is null || !m_target_is_ptr)
                    return s_QuantizeByVal!(target, p, m);

                return s_QuantizeByPtr(m_target_ptr!, p, m);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static Quantizer()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("Quantize");

                if (extensionMethod is not null)
                    s_QuantizeByPtr = extensionMethod.CreateStaticDelegate(typeof(QuantizeByPtr)) as QuantizeByPtr;

                extensionMethod = targetType.GetExtensionMethod("Quantize");

                if (extensionMethod is not null)
                    s_QuantizeByVal = extensionMethod.CreateStaticDelegate(typeof(QuantizeByVal)) as QuantizeByVal;

                if (s_QuantizeByPtr is null && s_QuantizeByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement Quantizer.Quantize method", new Exception("Quantize"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator Quantizer<T>(in ptr<T> target_ptr) => new Quantizer<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator Quantizer<T>(in T target) => new Quantizer<T>(target);

            // Enable comparisons between nil and Quantizer<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Quantizer<T> value, NilType nil) => Activator.CreateInstance<Quantizer<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Quantizer<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Quantizer<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Quantizer<T> value) => value != nil;
        }
    }
}}

namespace go
{
    public static class draw_QuantizerExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.image.draw_package.Quantizer target)
        {
            try
            {
                return ((go.image.draw_package.Quantizer<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.image.draw_package.Quantizer target, out T result)
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
        public static object? _(this go.image.draw_package.Quantizer target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.image.draw_package.Quantizer<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.image.draw_package.Quantizer target, Type type, out object? result)
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