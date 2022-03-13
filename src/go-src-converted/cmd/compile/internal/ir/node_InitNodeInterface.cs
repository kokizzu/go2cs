//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 06 22:49:10 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using fmt = go.fmt_package;
using constant = go.go.constant_package;
using sort = go.sort_package;
using @base = go.cmd.compile.@internal.@base_package;
using types = go.cmd.compile.@internal.types_package;
using src = go.cmd.@internal.src_package;
using go;

#nullable enable
#pragma warning disable CS0660, CS0661

namespace go {
namespace cmd {
namespace compile {
namespace @internal
{
    public static partial class ir_package
    {
        [GeneratedCode("go2cs", "0.1.0.0")]
        public partial interface InitNode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static InitNode As<T>(in T target) => (InitNode<T>)target!;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static InitNode As<T>(ptr<T> target_ptr) => (InitNode<T>)target_ptr;

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static InitNode? As(object target) =>
                typeof(InitNode<>).CreateInterfaceHandler<InitNode>(target);
        }

        [GeneratedCode("go2cs", "0.1.0.0")]
        public class InitNode<T> : InitNode
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

            public InitNode(in T target) => m_target = target;

            public InitNode(ptr<T> target_ptr)
            {
                m_target_ptr = target_ptr;
                m_target_is_ptr = true;
            }

            private delegate ptr<Nodes> PtrInitByPtr(ptr<T> value);
            private delegate ptr<Nodes> PtrInitByVal(T value);

            private static readonly PtrInitByPtr? s_PtrInitByPtr;
            private static readonly PtrInitByVal? s_PtrInitByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ptr<Nodes> PtrInit()
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_PtrInitByPtr is null || !m_target_is_ptr)
                    return s_PtrInitByVal!(target);

                return s_PtrInitByPtr(m_target_ptr!);
            }

            private delegate ptr<Nodes> SetInitByPtr(ptr<T> value, Nodes x);
            private delegate ptr<Nodes> SetInitByVal(T value, Nodes x);

            private static readonly SetInitByPtr? s_SetInitByPtr;
            private static readonly SetInitByVal? s_SetInitByVal;

            [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ptr<Nodes> SetInit(Nodes x)
            {
                T target = m_target;

                if (m_target_is_ptr && m_target_ptr is not null)
                    target = m_target_ptr.val;

                if (s_SetInitByPtr is null || !m_target_is_ptr)
                    return s_SetInitByVal!(target, x);

                return s_SetInitByPtr(m_target_ptr!, x);
            }
            
            public string ToString(string? format, IFormatProvider? formatProvider) => format ?? GetGoTypeName(typeof(T));

            [DebuggerStepperBoundary]
            static InitNode()
            {
                Type targetType = typeof(T);
                Type targetTypeByPtr = typeof(ptr<T>);
                MethodInfo extensionMethod;

               extensionMethod = targetTypeByPtr.GetExtensionMethod("PtrInit");

                if (extensionMethod is not null)
                    s_PtrInitByPtr = extensionMethod.CreateStaticDelegate(typeof(PtrInitByPtr)) as PtrInitByPtr;

                extensionMethod = targetType.GetExtensionMethod("PtrInit");

                if (extensionMethod is not null)
                    s_PtrInitByVal = extensionMethod.CreateStaticDelegate(typeof(PtrInitByVal)) as PtrInitByVal;

                if (s_PtrInitByPtr is null && s_PtrInitByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement InitNode.PtrInit method", new Exception("PtrInit"));

               extensionMethod = targetTypeByPtr.GetExtensionMethod("SetInit");

                if (extensionMethod is not null)
                    s_SetInitByPtr = extensionMethod.CreateStaticDelegate(typeof(SetInitByPtr)) as SetInitByPtr;

                extensionMethod = targetType.GetExtensionMethod("SetInit");

                if (extensionMethod is not null)
                    s_SetInitByVal = extensionMethod.CreateStaticDelegate(typeof(SetInitByVal)) as SetInitByVal;

                if (s_SetInitByPtr is null && s_SetInitByVal is null)
                    throw new NotImplementedException($"{targetType.FullName} does not implement InitNode.SetInit method", new Exception("SetInit"));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator InitNode<T>(in ptr<T> target_ptr) => new InitNode<T>(target_ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
            public static explicit operator InitNode<T>(in T target) => new InitNode<T>(target);

            // Enable comparisons between nil and InitNode<T> interface instance
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(InitNode<T> value, NilType nil) => Activator.CreateInstance<InitNode<T>>().Equals(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(InitNode<T> value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, InitNode<T> value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, InitNode<T> value) => value != nil;
        }
    }
}}}}

namespace go
{
    public static class ir_InitNodeExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_conversionOperators = new ConcurrentDictionary<Type, MethodInfo>();

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static T _<T>(this go.cmd.compile.@internal.ir_package.InitNode target)
        {
            try
            {
                return ((go.cmd.compile.@internal.ir_package.InitNode<T>)target).Target;
            }
            catch (NotImplementedException ex)
            {
                throw new PanicException($"interface conversion: {GetGoTypeName(target.GetType())} is not {GetGoTypeName(typeof(T))}: missing method {ex.InnerException?.Message}");
            }
        }

        [GeneratedCode("go2cs", "0.1.0.0"), MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerNonUserCode]
        public static bool _<T>(this go.cmd.compile.@internal.ir_package.InitNode target, out T result)
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
        public static object? _(this go.cmd.compile.@internal.ir_package.InitNode target, Type type)
        {
            try
            {
                MethodInfo? conversionOperator = s_conversionOperators.GetOrAdd(type, _ => typeof(go.cmd.compile.@internal.ir_package.InitNode<>).GetExplicitGenericConversionOperator(type));

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
        public static bool _(this go.cmd.compile.@internal.ir_package.InitNode target, Type type, out object? result)
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