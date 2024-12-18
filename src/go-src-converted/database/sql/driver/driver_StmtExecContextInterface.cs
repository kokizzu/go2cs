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
using context = go.context_package;
using errors = go.errors_package;
using reflect = go.reflect_package;
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
        public partial interface StmtExecContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static StmtExecContext As<T>(in T target) => (StmtExecContext<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static StmtExecContext As<T>(ptr<T> target_ptr) => (StmtExecContext<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static StmtExecContext? As(object target) =>
                typeof(StmtExecContext<>).CreateInterfaceHandler<StmtExecContext>(target);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public class StmtExecContext<T> : StmtExecContext
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

            public StmtExecContext(in T target) => m_target = target;

            public StmtExecContext(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate (Result, error) ExecContextByPtr(ptr<T> value, context.Context ctx, slice<NamedValue> args);
            private delegate (Result, error) ExecContextByVal(T value, context.Context ctx, slice<NamedValue> args);

            private static readonly ExecContextByPtr? s_ExecContextByPtr;
            private static readonly ExecContextByVal? s_ExecContextByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (Result, error) ExecContext(context.Context ctx, slice<NamedValue> args)
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_ExecContextByPtr is null || !m_target_is_ptr)
                    return s_ExecContextByVal!(target, ctx, args);

                return s_ExecContextByPtr(m_target_ptr!, ctx, args);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static StmtExecContext()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("ExecContext");

                if (extensionMethod is not null)
                    s_ExecContextByPtr = extensionMethod.CreateStaticDelegate(typeof(ExecContextByPtr)) as ExecContextByPtr;

                extensionMethod = targetType.GetExtensionMethod("ExecContext");

                if (extensionMethod is not null)
                    s_ExecContextByVal = extensionMethod.CreateStaticDelegate(typeof(ExecContextByVal)) as ExecContextByVal;

                if (s_ExecContextByPtr is null && s_ExecContextByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement StmtExecContext.ExecContext method", new Exception("ExecContext"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator StmtExecContext<T>(in ptr<T> target_ptr) => new StmtExecContext<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator StmtExecContext<T>(in T target) => new StmtExecContext<T>(target);

            // Enable comparisons between nil and StmtExecContext<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(StmtExecContext<T> value, NilType nil) => Activator.CreateInstance<StmtExecContext<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(StmtExecContext<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, StmtExecContext<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, StmtExecContext<T> value) => value != nil;
        }
    }
}}}

namespace go
{
    public static class driver_StmtExecContextExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.database.sql.driver_package.StmtExecContext target)
        {
            try
            {
                return ((go.database.sql.driver_package.StmtExecContext<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.2.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.database.sql.driver_package.StmtExecContext target, out T result)
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
        public static object? _(this go.database.sql.driver_package.StmtExecContext target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.database.sql.driver_package.StmtExecContext<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.database.sql.driver_package.StmtExecContext target, Type type, out object? result)
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