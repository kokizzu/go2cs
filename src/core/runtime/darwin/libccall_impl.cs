// libccall_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// runtime.libcCall — darwin's dispatch bottom, realized on golib's platform-neutral dispatcher.
//
// Every libc trampoline in sys_darwin.cs is reached as libcCall(FuncPCABI0(x_trampoline), &args).
// Go's body switches to the g0 stack, records the caller's g/pc/sp in m.libcall* for the profiler's
// traceback, and jumps through asmcgocall. Its converted form opens with getg() and ends in
// asmcgocall — four bodyless intrinsics (getg, getcallerpc, getcallersp, asmcgocall) that the
// generator stubs with a throw and that no hand-own implements — so a REAL function pointer handed to
// it dies one line in, before the call. That is why the class-B resolver alone could not have
// unblocked darwin, and why this file exists (docs/phase4/DESIGN-darwin-run-layer-2.md §2.2).
//
// WHAT IS DROPPED, AND WHY THAT IS THE FAITHFUL ANSWER. The g0 switch and the libcall* bookkeeping
// exist so a profiler signal landing mid-call can unwind the Go stack; the managed host has no
// signal-driven profiler walking Go frames and no g0 — the same judgment syscall_linux_impl.cs
// records for entersyscall/exitsyscall one platform over. It is WEAKER here than there, and the
// design says so (§7.5): linux had a running flavour to confirm it, darwin has none yet, so this is
// an assumption recorded, not a measurement, and the first mac dispatch is what moves it.
//
// WHAT IS DONE. `fn` is the exported symbol's real address (FuncPCABI0 resolved the trampoline
// through the package's `//go:cgo_import_dynamic` records — the class-B pass binds runtime's spelling,
// `libc_fcntl` <-> `fcntl_trampoline`, as of the same cut). `arg` is `unsafe.Pointer(&args)` over a
// lifted per-call-site struct; its number resolves back to the box through the pointer-provenance
// record (ж's uintptr operator pins and registers reference-free storage), and the box's type gives
// the layout Go's trampoline unpacks by offset: leading fields are the inputs, `ret`/`errno` the
// outputs. GoLibcCall.DispatchArgsStruct performs exactly that and writes the outcome back through
// the box, so the converted caller reads `args.ret` as it always did.
//
// WHAT IS REFUSED, LOUDLY AND BY NAME, NEVER WITH A DEFAULT. An argument whose number resolves to
// no box (a reference-bearing args struct — mmap_args, mach_vm_region_args, proc_regionfilename_args
// carry managed pointers and have no pinnable storage, so they never register), a field the
// dispatcher cannot place in a register, or a null function pointer. A wrong layout would hand libc
// garbage in registers; a throw that names the symbol is the honest answer.
//
// Hand-owned (no libccall_impl.go exists, so a reconvert never regenerates this file). Declared
// NON-partial: the registry displacement leaves a comment placeholder in sys_libc.cs, not a bodyless
// partial, so this file owns the whole declaration — the nanotime_impl.cs shape beside it.
[module: go.GoManualConversion]

namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

// The library Go's own pragma names for __error (sys_darwin.cs: `//go:cgo_import_dynamic libc_error
// __error "/usr/lib/libSystem.B.dylib"`). __error has no trampoline — Go's assembly calls it
// directly — so it is resolved by symbol rather than through a record.
private const string libSystemPath = "/usr/lib/libSystem.B.dylib";

private static nint s_libcCallErrnoReader;

internal static int32 libcCall(@unsafe.Pointer fn, @unsafe.Pointer arg) {
    nint entryPoint = unchecked((nint)(nuint)(uintptr)fn);

    if (entryPoint == 0) {
        throw new InvalidOperationException("go2cs: libcCall: the function pointer is null — FuncPCABI0 did not resolve the trampoline");
    }

    string symbol = GoCgoDynamicImports.SymbolOf(entryPoint) ?? $"0x{entryPoint:x}";

    object? argsBox = arg == nil ? null : ManagedPointerTokens.Resolve((nuint)(uintptr)arg);

    if (argsBox is null) {
        throw new InvalidOperationException(
            $"go2cs: libcCall({symbol}): the argument pointer does not resolve to a managed args box — " +
            "a reference-bearing args struct has no pinnable storage and cannot be dispatched by layout; " +
            "the per-symbol layout record is the remedy");
    }

    if (s_libcCallErrnoReader == 0) {
        s_libcCallErrnoReader = GoCgoDynamicImports.Resolve("__error", libSystemPath);
    }

    GoLibcCall.DispatchArgsStruct(entryPoint, argsBox, s_libcCallErrnoReader, symbol);

    // Go's asmcgocall returns the trampoline's AX; no caller in sys_darwin.cs reads libcCall's result
    // (each reads its args struct instead), so 0 is what the converted callers already discard.
    return 0;
}

} // end runtime_package
