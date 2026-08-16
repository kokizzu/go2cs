// RuntimeErrorPanic.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics.CodeAnalysis;

namespace go.golib;

/// <summary>
/// Represents common runtime error messages thrown in Go environment.
/// </summary>
public static class RuntimeErrorPanic
{
    private const string RuntimeErrorMessage = "runtime error: ";

    private const string NilPointerDereferenceMessage = $"{RuntimeErrorMessage}invalid memory address or nil pointer dereference";
    public static PanicException NilPointerDereference()
    {
        return new PanicException(NilPointerDereferenceMessage);
    }

    private const string IndexOutOfRangeMessage = $"{RuntimeErrorMessage}index out of range [{{0}}] with length {{1}}";
    public static PanicException IndexOutOfRange(int64 index, int64 length)
    {
        return new PanicException(string.Format(IndexOutOfRangeMessage, index, length));
    }

    private const string SliceBoundsOutOfRangeMessage = $"{RuntimeErrorMessage}slice bounds out of range ";
    public static PanicException SliceBoundsOutOfRange(int64 low, int64 high, int64 max, int64 capacity)
    {
        // Mirrors the Go runtime's message shapes for a slice expression s[low:high:max]
        string bounds;

        if (max > capacity)
            bounds = $"[::{max}] with capacity {capacity}";
        else if (high > max)
            bounds = max == capacity ? $"[:{high}] with capacity {capacity}" : $"[:{high}:{max}]";
        else if (low < 0)
            bounds = $"[{low}:]";
        else
            bounds = $"[{low}:{high}]";

        return new PanicException(SliceBoundsOutOfRangeMessage + bounds);
    }

    private const string ArrayConversionLengthMessage = $"{RuntimeErrorMessage}cannot convert slice with length {{0}} to array or pointer to array with length {{1}}";

    /// <summary>
    /// Go's panic for a <c>[N]T(s)</c> / <c>(*[N]T)(s)</c> conversion whose source slice is shorter
    /// than <c>N</c> — the message the runtime itself prints, raised as a recoverable panic.
    /// </summary>
    public static PanicException ArrayConversionLength(int64 sourceLength, int64 length)
    {
        return new PanicException(string.Format(ArrayConversionLengthMessage, sourceLength, length));
    }

    private const string IntegerDivideByZeroMessage = $"{RuntimeErrorMessage}integer divide by zero";

    /// <summary>
    /// Supplies the Go runtime's OWN divide-by-zero panic value (<c>runtime.divideError</c>, whose
    /// dynamic type is the unexported <c>runtime.errorString</c>).
    /// </summary>
    /// <remarks>
    /// Go's compiler lowers an integer division to a zero check plus <c>runtime.panicdivide()</c>, so
    /// <c>recover()</c> yields a value satisfying <c>runtime.Error</c> — math/bits' TestDiv32PanicZero
    /// asserts exactly that (<c>err.(runtime.Error)</c>) on the panic raised by the IMPLICIT hardware
    /// division in <c>Div32</c>, unlike Div/Div64 which panic explicitly. golib sits UNDER the converted
    /// <c>runtime</c> package and so cannot name that value; the runtime package registers it here
    /// (see its <c>panicvalues_impl.cs</c> bridge), leaving this layer dependency-free. When nothing has
    /// registered — a converted program that never links <c>runtime</c> — the panic falls back to the
    /// plain message below, which still reads and prints identically and only loses the type assertion.
    /// </remarks>
    public static Func<object>? IntegerDivideByZeroValue { get; set; }

    public static PanicException IntegerDivideByZero()
    {
        return new PanicException(IntegerDivideByZeroValue?.Invoke() ?? IntegerDivideByZeroMessage);
    }

    private const string UnrepresentableNativeReinterpretMessage =
        "go2cs: cannot dereference a native address as '{0}' -- the type holds a managed reference, " +
        "which reading it from raw memory would FABRICATE. This is a `(*U)(unsafe.Pointer(p))` " +
        "reinterpret the managed model cannot express as an alias (see PointerExtensions.Reinterpret): " +
        "the pointer is usable as an address, but not as a pointee. Convert the site by hand " +
        "(`[module: GoManualConversion]`), as crypto/subtle's xor_generic.cs and " +
        "vendor/golang.org/x/crypto/sha3's xor.cs are.";

    /// <summary>
    /// The refusal raised when a NATIVE-backed pointer whose pointee type carries a managed reference
    /// is dereferenced — the unrepresentable half of Go's <c>(*U)(unsafe.Pointer(p))</c>.
    /// </summary>
    /// <remarks>
    /// Raised as a panic so the failure is CONTAINED and named. Left to the CLR it is an
    /// <c>AccessViolationException</c>: a process kill with no diagnostic, no <c>recover()</c>, and a
    /// stack naming whichever consumer first touched the fabricated reference rather than the
    /// reinterpret that built it. See <c>ж&lt;T&gt;</c>'s <c>s_nativeReadFabricatesReference</c> for
    /// the full account and the sha3 witness.
    /// </remarks>
    public static PanicException UnrepresentableNativeReinterpret(Type pointeeType)
    {
        return new PanicException(string.Format(UnrepresentableNativeReinterpretMessage, readableName(pointeeType)));

        // `Type.Name` renders the witness type as "array`1", which names neither the element type nor
        // anything a reader can grep for. One level of generic arguments is what makes the message
        // point at the actual reinterpret ("array<Byte>"); this stays local rather than reaching for
        // GoReflect's Go-name machinery, which sits above golib's own error path.
        static string readableName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            string name = type.Name;
            int arity = name.IndexOf('`');

            if (arity > 0)
                name = name[..arity];

            return $"{name}<{string.Join(", ", Array.ConvertAll(type.GetGenericArguments(), arg => arg.Name))}>";
        }
    }

    private const string MakeSliceLenOutOfRangeMessage = $"{RuntimeErrorMessage}makeslice: len out of range";
    public static PanicException MakeSliceLenOutOfRange()
    {
        return new PanicException(MakeSliceLenOutOfRangeMessage);
    }

    private const string MakeSliceCapOutOfRangeMessage = $"{RuntimeErrorMessage}makeslice: cap out of range";
    public static PanicException MakeSliceCapOutOfRange()
    {
        return new PanicException(MakeSliceCapOutOfRangeMessage);
    }

    /// <summary>
    /// Converts a .NET exception that corresponds to a Go runtime panic into a <see cref="PanicException"/>,
    /// so it can be recovered with <c>recover()</c> and reported like a Go panic. Returns <c>false</c>
    /// (leaving the exception to propagate unchanged) for exceptions that are not Go runtime panics.
    /// </summary>
    /// <param name="ex">Exception to inspect.</param>
    /// <param name="panic">Resulting panic when the exception maps to a Go runtime panic.</param>
    /// <returns><c>true</c> if <paramref name="ex"/> is (or maps to) a Go panic; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This is the ONE place a .NET exception becomes a Go panic, so it is also where the panic's
    /// origin is snapshotted (<see cref="PanicException.PanicTrace"/>) — a mapped runtime error is
    /// synthesized HERE and was never thrown at the fault site, so only <paramref name="ex"/> carries
    /// those frames, and once this returns they are the caller's last chance to be recorded. Doing it
    /// at the adoption point rather than in each adopter is what lets a panic that passes through NO
    /// <c>GoFunc</c> at all — a function that never defers, so nothing wraps it — still report where
    /// it faulted. The snapshot is once-only, so a panic re-adopted by each enclosing frame keeps the
    /// innermost (deepest) origin, and nothing is computed on a non-panicking path.
    /// </remarks>
    public static bool TryAsPanic(Exception ex, [NotNullWhen(true)] out PanicException? panic)
    {
        switch (ex)
        {
            case PanicException panicException:
                panic = panicException;
                panic.CaptureThrowSite(ex);
                return true;
            case DivideByZeroException:
                // Go: integer division or modulo by zero panics with a runtime error. .NET raises
                // DivideByZeroException for the same operation; map it so recover() behaves like Go.
                panic = IntegerDivideByZero();
                panic.CaptureThrowSite(ex);
                return true;
            case NullReferenceException:
                // Go: dereferencing a nil pointer panics with "runtime error: invalid memory address
                // or nil pointer dereference" — a RECOVERABLE panic that real Go code relies on
                // (sync's TestNilPool calls Get/Put on a nil *Pool and requires recover() to catch
                // it). The converted equivalent — reading `Ꮡp.Value` through a nil ж<T>, or any nil
                // reference deref in emitted code — raises NullReferenceException, which recover()
                // could not see, so the panic escaped as an unrecoverable infrastructure error.
                // Mapping it here gives recover() Go's behavior and prints Go's message verbatim.
                panic = NilPointerDereference();
                panic.CaptureThrowSite(ex);
                return true;
            default:
                panic = null;
                return false;
        }
    }
}
