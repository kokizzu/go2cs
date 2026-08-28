// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion of trace.go's StartTrace, the Linux flavor.
//
// Go's execution tracer is a serialization of the scheduler: StartTrace stops the world through
// semacquire, whose first step is getg — the current goroutine's g structure, a per-thread runtime
// object the CLR does not have — so the converted body's first act was an unimplemented-intrinsic
// THROW, and every runtime/trace.Start surfaced as an infrastructure error rather than a result
// (os/signal's TestSignalTrace is the measured consumer). StartTrace returns an error by
// signature, and a capability the host cannot provide is honestly an ERROR, not a crash — the
// same fidelity call as AllThreadsSyscall answering ENOTSUP the way Go's own cgo builds do. The
// hand-own therefore answers a named tracing-not-supported error; runtime/trace.Start propagates
// it unchanged, callers observe a clean Go-shaped failure, and a test asserting trace output
// fails with a signature a disclosure manifest can pin. StopTrace and the rest of the tracer
// stay auto: they are unreachable while StartTrace refuses.
//
// The name is registered goosLinux in manualConversionFuncs (manualTypeOperations.go), so a Linux
// -stdlib emission drops the auto body to a placeholder and this file supplies it.
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
}
