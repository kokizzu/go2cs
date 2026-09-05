// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion of trace.go's StartTrace and StopTrace — the platform-neutral managed
// tracer seam. This file exists as TWO copies, runtime/windows/trace_impl.cs and
// runtime/linux/trace_impl.cs, and the copies are byte-identical BY CONTRACT: a change to one is a
// change to both (the routing note at the end of this header says why).
//
// Go's execution tracer is a serialization of the scheduler: StartTrace stops the world through
// semacquire, whose first step is getg — the current goroutine's g structure, a per-thread runtime
// object the CLR does not have — so the converted body's first act was an unimplemented-intrinsic
// THROW, and every runtime/trace.Start surfaced as an infrastructure error rather than a result.
// The measured consumers are runtime's own TestCrashWhileTracing on the windows flavor and
// os/signal's TestSignalTrace on the linux flavor. StartTrace returns an error by signature, and a
// capability the host cannot provide is honestly an ERROR, not a crash — the same fidelity call as
// AllThreadsSyscall answering ENOTSUP the way Go's own cgo builds do. The hand-own therefore answers
// a named tracing-not-supported error; runtime/trace.Start propagates it unchanged, callers observe
// a clean Go-shaped failure, and a test asserting trace output fails with a signature a disclosure
// manifest can pin.
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
// Registration and routing. Both names are registered goosWindowsLinux in manualConversionFuncs
// (manualTypeOperations.go): StartTrace was widened from goosLinux to windowsLinux at 4c4e7a425
// and StopTrace joined at that scope at 138b8f7fd, both 2026-09-02. On each of those two targets
// the -stdlib emission drops the auto bodies in <goos>/trace.cs to placeholders and this file
// supplies them; the darwin emission displaces neither name (darwin/trace.cs keeps both auto
// bodies), so no darwin copy exists. Layout L3 routes a hand-own by DISPLACEMENT, not by where its
// principal is built (handOwnEmitters in platformHandOwn.go, pinned by platformHandOwn_test.go):
// trace.cs is emitted per-GOOS on all three targets, but a target needs this companion exactly
// when its own trace.cs carries the placeholders — windows and linux — so the three-target merge
// places one copy in each of those two folders and refuses, by raw byte comparison, to choose
// between two hand-maintained copies that differ. Neither the error text nor the no-op is
// platform-specific, which is why one header serves both copies verbatim. (Until Q48, 2026-09-04,
// the two headers differed in prose alone — the flavor word, the named consumer and the
// registration note — and the -platforms merge refused at master over exactly that.)
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
