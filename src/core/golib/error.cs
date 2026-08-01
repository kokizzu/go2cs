// error.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using go.golib;

// TODO: Keep error implementation updated to match best interface template pattern

namespace go;

/// <summary>
/// The built-in error interface type is the conventional interface for representing an
/// error condition, with the nil value representing no error.
/// </summary>
/// <remarks>
/// The <see cref="GoInterfaceShellAttribute"/> stamp joins <c>error</c> to the ONE runtime
/// duck-typing mechanism every converted interface uses (see <see cref="AdapterBinder"/>):
/// <see cref="error{T}"/> IS the delegate-bound generic shell, and the attribute is how the binder
/// finds it — a direct stamp rather than reflection over the static <c>As&lt;T&gt;</c> helpers below
/// closed with <c>MakeGenericMethod</c>, which is what that lookup cost before and why
/// <c>builtin.TryTypeAssert</c> no longer needs a reflective closure at all. No object shell is declared: the
/// reflective tier would have to reproduce <see cref="error{T}"/>'s <c>%v</c>/<c>%T</c> formatting
/// contract, so a VALUE-typed error still binds through the generic shell (AOT-graceful, exactly as
/// before).
/// </remarks>
[GoInterfaceShell(typeof(error<>), null, nameof(Error))]
public interface error // : IFormattable
{
    /// <summary>
    /// Get string that represents an error.
    /// </summary>
    @string Error();

    public static error As<T>(T target)
    {
        return new error<T>(target!);
    }

    public static error As<T>(ж<T> target_ptr)
    {
        return new error<T>(target_ptr);
    }
}

internal interface IErrorTarget
{
    object? TargetObject { get; }
}

public class error<T> : error, IErrorTarget
{
    private T m_target = default!;
    private readonly ж<T>? m_target_ptr;
    private readonly bool m_target_is_ptr;

    public ref T Target
    {
        get
        {
            if (m_target_is_ptr && m_target_ptr is not null)
                return ref m_target_ptr.Value;

            return ref m_target;
        }
    }

    object? IErrorTarget.TargetObject => Target;

    // Declared BY VALUE, not `in T`: an `in` parameter is `T&` in metadata, and AdapterBinder locates
    // a shell's constructor by exact parameter type (GetConstructor([valueType])) — a by-ref
    // constructor is invisible to it, so a value-typed error could not bind.
    public error(T target)
    {
        m_target = target;
    }

    public error(ж<T> target_ptr)
    {
        m_target_ptr = target_ptr;
        m_target_is_ptr = true;
    }

    private delegate @string ErrorByPtr(ж<T> value);
    private delegate @string ErrorByVal(T value);

    private static readonly ErrorByPtr? s_ErrorByPtr;
    private static readonly ErrorByVal? s_ErrorByVal;

    [DebuggerNonUserCode, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public @string Error()
    {
        T target = m_target;

        if (m_target_is_ptr && m_target_ptr is not null)
            target = m_target_ptr.Value;

        if (s_ErrorByPtr is null || !m_target_is_ptr)
            return s_ErrorByVal!(target);

        return s_ErrorByPtr(m_target_ptr!);
    }

    public string ToString(string? format, IFormatProvider? _)
    {
        switch (format)
        {
            case "T":
            {
                string typeName = GetGoTypeName<T>().Replace("_package+", ".");
                return m_target_is_ptr ? $"*{typeName}" : typeName;
            }
            case "v":
            {
                // Go's %v of an error operand calls Error() via handleMethods — NEVER the
                // `&{fields}` pointer rendering: the *T method set includes the value-receiver
                // Error, so the method wins before pointer formatting. The pointee's ToString
                // dispatches the Go method already; the old `&` prefix diverged from Go for
                // every pointer-held error (errors TestAs subtest names).
                if (m_target_is_ptr)
                    return m_target_ptr is null ? "<nil>" : m_target_ptr.Value?.ToString() ?? "<nil>";

                return m_target?.ToString() ?? "<nil>";
            }
            default:
                return ToString() ?? "<nil>";
        }
    }

    [DebuggerStepperBoundary]
    static error()
    {
        Type targetType = typeof(T);
        Type targetTypeByPtr = typeof(ж<T>);

        MethodInfo? extensionMethod = targetTypeByPtr.GetExtensionMethod("Error");

        if (extensionMethod is not null)
            s_ErrorByPtr = extensionMethod.CreateStaticDelegate(typeof(ErrorByPtr)) as ErrorByPtr;

        extensionMethod = targetType.GetExtensionMethod("Error");

        if (extensionMethod is not null)
            s_ErrorByVal = extensionMethod.CreateStaticDelegate(typeof(ErrorByVal)) as ErrorByVal;

        if (s_ErrorByPtr is null && s_ErrorByVal is null)
            throw new NotImplementedException($"{targetType.FullName} does not implement error.Error method", new Exception("Error"));
    }

    public static explicit operator error<T>(in ж<T> target_ptr)
    {
        return new error<T>(target_ptr);
    }

    public static explicit operator error<T>(in T target)
    {
        return new error<T>(target);
    }

    // Enable comparisons between nil and error<T> interface instance
    public static bool operator ==(error<T> value, NilType _)
    {
        return Activator.CreateInstance<error<T>>().Equals(value);
    }

    public static bool operator !=(error<T> value, NilType nil)
    {
        return !(value == nil);
    }

    public static bool operator ==(NilType nil, error<T> value)
    {
        return value == nil;
    }

    public static bool operator !=(NilType nil, error<T> value)
    {
        return value != nil;
    }
}

public static class errorExtensions
{
    public static T _<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields
    )] T>(this error target)
    {
        // The `error<T>` carrier is golib's own error box — built by AdapterBinder from the
        // [GoInterfaceShell] stamp on `error`, and by the `error.As` factories. It does NOT hold its
        // Go dynamic value the way every other interface value does, so unwrapping it is the ONLY
        // error-specific part of a typed-error assert.
        if (target is error<T> carrier)
            return carrier.Target;

        // Everything else an `error` can hold IS a normal Go dynamic value — a generated pointer
        // adapter (IжAdapter over the ж<X> receiver box), a value type that implements error
        // directly, an interface-to-interface adapter, or a duck-typed wrapper. Route it through
        // the ONE type-assertion machinery so an assert made on a statically-`error` operand can
        // never disagree with the same assert made on an `any`-typed one.
        //
        // Do NOT shortcut this by casting the carrier to `error<T>` directly: that only matches the
        // carrier shape, so `err.(*fs.PathError)` against a pointer-sourced error throws
        // InvalidCastException ("PathErrorжerror to error<ж<PathError>>") — which is not even a
        // recoverable Go panic, and killed os's dirFS.Open path-fixup (io/fs TestGlob,
        // TestReadDirPath, TestReadFilePath). Commit cb0f58078 closed the INTERFACE half of this
        // same defect; the dispatch above closes the CONCRETE-type half.
        return ((object)target)._<T>();
    }

    public static bool _<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields
    )] T>(this error target, out T result)
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

    // Runtime-Type form of the assert above. It shares the same machinery for the same reason:
    // resolving a Go dynamic value by reflection must not reach a different answer than resolving
    // it by type argument. (The previous body invoked `error<>`'s explicit conversion operator on
    // the interface value, which — exactly like the generic form — only ever matched the legacy
    // carrier.)
    public static object? _(this error target, Type type)
    {
        if (target is IErrorTarget errorTarget &&
            target.GetType() is { IsGenericType: true } carrierType &&
            carrierType.GetGenericTypeDefinition() == typeof(error<>) &&
            carrierType.GetGenericArguments()[0] == type)
        {
            return errorTarget.TargetObject;
        }

        if (TryTypeAssert(target, type, out object? value))
            return value;

        throw new PanicException($"interface conversion: interface {{}} is {GetGoTypeName(target)}, not {GetGoTypeName(type)}");
    }

    public static bool _(this error target, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, out object? result)
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
