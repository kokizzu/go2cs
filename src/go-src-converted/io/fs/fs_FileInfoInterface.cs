//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 05:23:49 UTC
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
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial interface FileInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static FileInfo As<T>(in T target) => (FileInfo<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static FileInfo As<T>(ptr<T> target_ptr) => (FileInfo<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static FileInfo? As(object target) =>
                typeof(FileInfo<>).CreateInterfaceHandler<FileInfo>(target);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public class FileInfo<T> : FileInfo
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

            public FileInfo(in T target) => m_target = target;

            public FileInfo(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate void NameByPtr(ptr<T> value);
            private delegate void NameByVal(T value);

            private static readonly NameByPtr? s_NameByPtr;
            private static readonly NameByVal? s_NameByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Name()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_NameByPtr is null || !m_target_is_ptr)
                {
                    s_NameByVal!(target);
                    return;
                }

                s_NameByPtr(m_target_ptr!);
                return;
            }

            private delegate void SizeByPtr(ptr<T> value);
            private delegate void SizeByVal(T value);

            private static readonly SizeByPtr? s_SizeByPtr;
            private static readonly SizeByVal? s_SizeByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Size()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_SizeByPtr is null || !m_target_is_ptr)
                {
                    s_SizeByVal!(target);
                    return;
                }

                s_SizeByPtr(m_target_ptr!);
                return;
            }

            private delegate void ModeByPtr(ptr<T> value);
            private delegate void ModeByVal(T value);

            private static readonly ModeByPtr? s_ModeByPtr;
            private static readonly ModeByVal? s_ModeByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Mode()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_ModeByPtr is null || !m_target_is_ptr)
                {
                    s_ModeByVal!(target);
                    return;
                }

                s_ModeByPtr(m_target_ptr!);
                return;
            }

            private delegate void ModTimeByPtr(ptr<T> value);
            private delegate void ModTimeByVal(T value);

            private static readonly ModTimeByPtr? s_ModTimeByPtr;
            private static readonly ModTimeByVal? s_ModTimeByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ModTime()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_ModTimeByPtr is null || !m_target_is_ptr)
                {
                    s_ModTimeByVal!(target);
                    return;
                }

                s_ModTimeByPtr(m_target_ptr!);
                return;
            }

            private delegate void IsDirByPtr(ptr<T> value);
            private delegate void IsDirByVal(T value);

            private static readonly IsDirByPtr? s_IsDirByPtr;
            private static readonly IsDirByVal? s_IsDirByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void IsDir()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_IsDirByPtr is null || !m_target_is_ptr)
                {
                    s_IsDirByVal!(target);
                    return;
                }

                s_IsDirByPtr(m_target_ptr!);
                return;
            }

            private delegate void SysByPtr(ptr<T> value);
            private delegate void SysByVal(T value);

            private static readonly SysByPtr? s_SysByPtr;
            private static readonly SysByVal? s_SysByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Sys()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_SysByPtr is null || !m_target_is_ptr)
                {
                    s_SysByVal!(target);
                    return;
                }

                s_SysByPtr(m_target_ptr!);
                return;
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static FileInfo()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("Name");

                if (extensionMethod is not null)
                    s_NameByPtr = extensionMethod.CreateStaticDelegate(typeof(NameByPtr)) as NameByPtr;

                extensionMethod = targetType.GetExtensionMethod("Name");

                if (extensionMethod is not null)
                    s_NameByVal = extensionMethod.CreateStaticDelegate(typeof(NameByVal)) as NameByVal;

                if (s_NameByPtr is null && s_NameByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement FileInfo.Name method", new Exception("Name"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("Size");

                if (extensionMethod is not null)
                    s_SizeByPtr = extensionMethod.CreateStaticDelegate(typeof(SizeByPtr)) as SizeByPtr;

                extensionMethod = targetType.GetExtensionMethod("Size");

                if (extensionMethod is not null)
                    s_SizeByVal = extensionMethod.CreateStaticDelegate(typeof(SizeByVal)) as SizeByVal;

                if (s_SizeByPtr is null && s_SizeByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement FileInfo.Size method", new Exception("Size"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("Mode");

                if (extensionMethod is not null)
                    s_ModeByPtr = extensionMethod.CreateStaticDelegate(typeof(ModeByPtr)) as ModeByPtr;

                extensionMethod = targetType.GetExtensionMethod("Mode");

                if (extensionMethod is not null)
                    s_ModeByVal = extensionMethod.CreateStaticDelegate(typeof(ModeByVal)) as ModeByVal;

                if (s_ModeByPtr is null && s_ModeByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement FileInfo.Mode method", new Exception("Mode"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("ModTime");

                if (extensionMethod is not null)
                    s_ModTimeByPtr = extensionMethod.CreateStaticDelegate(typeof(ModTimeByPtr)) as ModTimeByPtr;

                extensionMethod = targetType.GetExtensionMethod("ModTime");

                if (extensionMethod is not null)
                    s_ModTimeByVal = extensionMethod.CreateStaticDelegate(typeof(ModTimeByVal)) as ModTimeByVal;

                if (s_ModTimeByPtr is null && s_ModTimeByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement FileInfo.ModTime method", new Exception("ModTime"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("IsDir");

                if (extensionMethod is not null)
                    s_IsDirByPtr = extensionMethod.CreateStaticDelegate(typeof(IsDirByPtr)) as IsDirByPtr;

                extensionMethod = targetType.GetExtensionMethod("IsDir");

                if (extensionMethod is not null)
                    s_IsDirByVal = extensionMethod.CreateStaticDelegate(typeof(IsDirByVal)) as IsDirByVal;

                if (s_IsDirByPtr is null && s_IsDirByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement FileInfo.IsDir method", new Exception("IsDir"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("Sys");

                if (extensionMethod is not null)
                    s_SysByPtr = extensionMethod.CreateStaticDelegate(typeof(SysByPtr)) as SysByPtr;

                extensionMethod = targetType.GetExtensionMethod("Sys");

                if (extensionMethod is not null)
                    s_SysByVal = extensionMethod.CreateStaticDelegate(typeof(SysByVal)) as SysByVal;

                if (s_SysByPtr is null && s_SysByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement FileInfo.Sys method", new Exception("Sys"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator FileInfo<T>(in ptr<T> target_ptr) => new FileInfo<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator FileInfo<T>(in T target) => new FileInfo<T>(target);

            // Enable comparisons between nil and FileInfo<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(FileInfo<T> value, NilType nil) => Activator.CreateInstance<FileInfo<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(FileInfo<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, FileInfo<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, FileInfo<T> value) => value != nil;
        }
    }
}}

namespace go
{
    public static class fs_FileInfoExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.io.fs_package.FileInfo target)
        {
            try
            {
                return ((go.io.fs_package.FileInfo<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.io.fs_package.FileInfo target, out T result)
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
        public static object? _(this go.io.fs_package.FileInfo target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.io.fs_package.FileInfo<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.io.fs_package.FileInfo target, Type type, out object? result)
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