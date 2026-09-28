// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished supplement (runtime panic VALUES bridge — no Go counterpart file).
//
// Go's compiler lowers an integer division to a zero check plus a call to runtime.panicdivide(),
// which panics with runtime.divideError — a value whose dynamic type is the unexported
// runtime.errorString, and therefore satisfies runtime.Error. go2cs instead lets the CLR raise
// DivideByZeroException and maps it to a Go panic in golib (RuntimeErrorPanic.TryAsPanic), so an
// IMPLICIT division panic carried only the message text: recover()'s value failed the
// `err.(runtime.Error)` assertion that math/bits' TestDiv32PanicZero makes (Div32 divides without
// an explicit zero guard, unlike Div/Div64 which panic(divideError) themselves).
//
// golib sits UNDER this package and cannot name divideError, so the dependency is inverted: golib
// exposes the hook and the runtime registers its own canonical value here. That keeps the panic
// value identical whether it was raised explicitly by panicdivide() or implicitly by the hardware
// trap — which is exactly Go's own invariant.
//
// The same inversion serves the two value classes golib RAISES by name rather than by trap: a
// runtime.plainError (nil-map write, close of a closed or nil channel, send on a closed channel,
// makechan's size check) and a runtime.boundsError (an index out of range, goPanicIndex's value).
// golib raised their TEXT as a string, so recover() yielded a `string` where Go yields a value that
// satisfies runtime.Error -- runtime's TestRuntimePanicWithRuntimeError asserts exactly that on six
// such panics. The panic text is unchanged for every non-negative index (both types' Error() print
// what golib printed); a negative index now prints Go's `index out of range [-1]`, without a length.
// Sizing: docs/phase4/CENSUS-runtime-error-factories-go1.24.13.md.
//
// This file has no `<name>.go` counterpart, so a -stdlib reconvert never emits over it; the module
// marker states the ownership explicitly and matches the other hand-owned runtime files.
[module: GoManualConversion]

namespace go;

using System;
using System.Runtime.CompilerServices;
using go.golib;

public static partial class runtime_package
{
    [ModuleInitializer]
    internal static void ᴛRegisterRuntimePanicValues()
    {
        // Deferred to first use: divideError is a static of this package, and reading it during
        // module initialization would force this type's static constructor to run ahead of the
        // rest of the package's own initialization order.
        RuntimeErrorPanic.IntegerDivideByZeroValue = static () => divideError;

        // Constructed per panic, as Go constructs them: neither touches a static of this package, so
        // registering them here runs nothing ahead of the package's own initialization.
        RuntimeErrorPanic.PlainErrorValue = static message => (error)(plainError)(@string)message;
        RuntimeErrorPanic.BoundsErrorValue = static (x, y, signed, code) =>
            (error)new boundsError(x: x, signed: signed, y: (nint)y, code: (boundsErrorCode)code);
    }
}
