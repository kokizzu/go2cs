// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion of trace.go's StartTrace, the Windows flavor.
//
// Go's execution tracer is a serialization of the scheduler: StartTrace stops the world through
// semacquire, whose first step is getg — the current goroutine's g structure, a per-thread runtime
// object the CLR does not have — so the converted body's first act was an unimplemented-intrinsic
// THROW, and every runtime/trace.Start surfaced as an infrastructure error rather than a result
// (runtime's own TestCrashWhileTracing is the measured consumer here). StartTrace returns an error
// by signature, and a capability the host cannot provide is honestly an ERROR, not a crash — the
// same fidelity call as AllThreadsSyscall answering ENOTSUP the way Go's own cgo builds do. The
// hand-own therefore answers a named tracing-not-supported error; runtime/trace.Start propagates
// it unchanged, callers observe a clean Go-shaped failure, and a test asserting trace output
// fails with a signature a disclosure manifest can pin.
//
// ⚠ This file previously claimed "StopTrace and the rest of the tracer stay auto: they are
// unreachable while StartTrace refuses." That was MEASURED FALSE on 2026-09-02 and StopTrace is
// hand-owned below. The witness is runtime/trace's own suite: TestTraceDoubleStart's FIRST
// statement (trace_test.go line 39) is a bare Stop() before any Start, so runtime/trace.Stop →
// runtime.StopTrace → traceAdvance → semacquire → semacquire1 → getg() threw, and that row read
// infrastructure-error where TestTraceStartStop next to it read a clean fail. ReadTrace and the
// rest of the tracer DO stay auto, for the reason the old sentence gave: they are reached only
// from the goroutine trace.Start spawns after it succeeds, which on this host it never does.
//
// The name is registered goosWindowsLinux in manualConversionFuncs (manualTypeOperations.go), so a
// Windows -stdlib emission drops the auto body to a placeholder and this file supplies it — content
// identical to linux/trace_impl.cs, since the error text is not platform-specific.
[module: GoManualConversion]

namespace go;

partial class runtime_package
{
    // StartTrace enables tracing for the current process.
    // While tracing, the data will be buffered and available via [runtime.ReadTrace].
    // StartTrace returns an error if tracing is already enabled.
    // Most clients should use the [runtime/trace] package or the [testing] package's
    // -test.trace flag instead of calling StartTrace directly.
    public static error StartTrace()
    {
        return ((errorString)("tracing is not supported: the go2cs managed runtime has no execution tracer"u8));
    }

    // StopTrace stops tracing, if it was previously enabled.
    // StopTrace only returns after all the reads for the trace have completed.
    //
    // Tracing is never previously enabled on this host — StartTrace above always refuses — so
    // Go's own conditional is what makes doing nothing the faithful answer rather than a
    // convenience, and the signature carries no error channel through which anything else could
    // be said. Both halves of the contract hold vacuously: there is no tracing to stop, and there
    // are no outstanding reads to wait for, since ReadTrace is reached only from the goroutine
    // runtime/trace.Start spawns after it succeeds. Nothing observable is skipped either: the auto
    // body is traceAdvance(true), whose own gen == 0 check releases the semaphore and returns
    // immediately when tracing was never started — it simply never reached that check, because
    // its first statement is semacquire, and semacquire1 opens on getg().
    public static void StopTrace()
    {
    }
}
