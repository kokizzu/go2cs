// funcpc_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Reflection;

namespace go.@internal;

// ---------------------------------------------------------------------------------------------
// THE TWO ENTRY POINTS INTO THE SYNTHETIC-PC REGISTRY.
// Design record: docs/phase4/DESIGN-synthetic-pc-registry.md.
//
// WHAT THESE USED TO DO, AND WHY IT WAS THE WORST POSSIBLE ANSWER
//   Both bodies were `return default` — every function's PC was 0. That compiles, returns a
//   plausible value, and is wrong, which is exactly why the hole stayed invisible for the life of
//   the corpus: `internal/abi`'s own TestFuncPC compared FuncPCABI0(FuncPCTestFn) against
//   FuncPCTestFnAddr and PASSED, because both were zero. Go passes the same test for the opposite
//   reason — a real assembly address on both arms — so the verdicts matched and the row banked. A
//   silent wrong answer that agrees with the oracle by coincidence is the failure mode this file
//   now exists to prevent.
//
// HOW THE ARGUMENT ARRIVES
//   `any` is System.Object and every call site passes a bare method group — `FuncPCABI0(clone)`,
//   `FuncPCABIInternal(chansend)`. Since C# 10 that is a NATURAL DELEGATE TYPE conversion (the
//   compiler reports CS8974 at each site), so `f` is a real System.Delegate and `f.Method` is the
//   target's MethodInfo. That is what lets the whole mechanism live in a body rather than needing
//   a converter change.
//
// THE THREE CLASSES, AND WHY ONLY ONE OF THEM GETS A NUMBER
//   A — a converted Go function with a managed body. It exists, it can be symbolized, and a
//       synthetic PC is exactly the right answer. This is the population the registry serves.
//   B — darwin's dylib trampolines, whose result is CALLED. They need a real code address from
//       NativeLibrary.GetExport, not a token; a token would be fatal the moment it is invoked.
//   C — Go's own assembly routines (goexit, mstart, sigtramp, methodValueCall, ...). There is no
//       dylib to resolve them from and no managed equivalent to point at. Nothing honest exists.
//
//   B and C share the only property visible here — no managed body — so this file cannot tell them
//   apart, and deliberately does not try: it refuses both, loudly, and darwin's resolution arm slots
//   in later keyed on the cgo_import_dynamic pragma map. A number returned to either would be the
//   `return default` defect wearing better clothes.
// ---------------------------------------------------------------------------------------------

partial class abi_package
{
    // Implementation of FuncPCABI0
    public static partial uintptr FuncPCABI0(any f) => FuncPC(f, nameof(FuncPCABI0));

    // Implementation of FuncPCABIInternal
    public static partial uintptr FuncPCABIInternal(any f) => FuncPC(f, nameof(FuncPCABIInternal));

    private static uintptr FuncPC(any f, string entryPoint)
    {
        if (f is not Delegate target)
        {
            throw new ArgumentException(
                $"{entryPoint}: expected a func value, got {(f is null ? "nil" : f.GetType().FullName)}", nameof(f));
        }

        MethodInfo method = target.Method;

        // NotSupported rather than NotImplemented, and the distinction is the point: for a Go
        // assembly routine there is no managed body to write and no address to hand back, so this
        // can never be "implemented later" the way a missing feature can. The message is the
        // signature a disclosure matches on, so it names the function.
        if (method.IsDefined(typeof(GoExternalStubAttribute), inherit: false))
        {
            throw new NotSupportedException(
                $"{entryPoint}: no program counter exists for {GoSyntheticPC.GoNameOf(method)} — " +
                "it is an external (assembly or cgo) function with no managed body in this corpus");
        }

        return GoSyntheticPC.Of(method);
    }
}
