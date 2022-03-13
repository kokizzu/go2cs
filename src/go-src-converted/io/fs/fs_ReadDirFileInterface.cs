//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:08:01 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using oserror = go.@internal.oserror_package;
using time = go.time_package;
using utf8 = go.unicode.utf8_package;
using go;

#nullable enable
#pragma warning disable CS0660, CS0661

namespace go {
namespace io
{
    public static partial class fs_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial interface ReadDirFile
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ReadDirFile As<T>(in T target) => (ReadDirFile<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ReadDirFile As<T>(ptr<T> target_ptr) => (ReadDirFile<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static ReadDirFile? As(object target) =>
                typeof(ReadDirFile<>).CreateInterfaceHandler<ReadDirFile>(target);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public class ReadDirFile<T> : ReadDirFile
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

            public ReadDirFile(in T target) => m_target = target;

            public ReadDirFile(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate (slice<DirEntry>, error) ReadDirByPtr(ptr<T> value, nint n);
            private delegate (slice<DirEntry>, error) ReadDirByVal(T value, nint n);

            private static readonly ReadDirByPtr? s_ReadDirByPtr;
            private static readonly ReadDirByVal? s_ReadDirByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (slice<DirEntry>, error) ReadDir(nint n)
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_ReadDirByPtr is null || !m_target_is_ptr)
                    return s_ReadDirByVal!(target, n);

                return s_ReadDirByPtr(m_target_ptr!, n);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static ReadDirFile()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("ReadDir");

                if (extensionMethod is not null)
                    s_ReadDirByPtr = extensionMethod.CreateStaticDelegate(typeof(ReadDirByPtr)) as ReadDirByPtr;

                extensionMethod = targetType.GetExtensionMethod("ReadDir");

                if (extensionMethod is not null)
                    s_ReadDirByVal = extensionMethod.CreateStaticDelegate(typeof(ReadDirByVal)) as ReadDirByVal;

                if (s_ReadDirByPtr is null && s_ReadDirByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement ReadDirFile.ReadDir method", new Exception("ReadDir"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator ReadDirFile<T>(in ptr<T> target_ptr) => new ReadDirFile<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator ReadDirFile<T>(in T target) => new ReadDirFile<T>(target);

            // Enable comparisons between nil and ReadDirFile<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(ReadDirFile<T> value, NilType nil) => Activator.CreateInstance<ReadDirFile<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(ReadDirFile<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, ReadDirFile<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, ReadDirFile<T> value) => value != nil;
        }
    }
}}

namespace go
{
    public static class fs_ReadDirFileExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.io.fs_package.ReadDirFile target)
        {
            try
            {
                return ((go.io.fs_package.ReadDirFile<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.io.fs_package.ReadDirFile target, out T result)
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
        public static object? _(this go.io.fs_package.ReadDirFile target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.io.fs_package.ReadDirFile<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.io.fs_package.ReadDirFile target, Type type, out object? result)
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