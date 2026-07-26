// runtime.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The BASELINE runtime package — the hand-finished stub the behavioral corpus builds against, the
// sibling of core/sync.cs. It carries only the runtime primitives the managed model genuinely
// honors, and it grows one primitive at a time as a test needs one; there is no value in stubbing
// an API that would be wrong.
//
// This is NOT the hand-owned overlay material. The `*_impl.cs` files in this same directory are the
// canonical [module: GoManualConversion] sources for the FULL conversion tree
// (src/go-src-converted/runtime) and are excluded from this project's compile — they name types that
// only exist there (m, g, mutex, note). The full tree's counterpart of the code below is that tree's
// managed_impl.cs; both trees must state the same contract because a converted program links exactly
// one of them.

using System;
using go.golib;

namespace go;

public static class runtime_package
{
    // Goexit terminates the goroutine that calls it. No other goroutine is affected. Goexit runs
    // all deferred calls before terminating the goroutine. Because Goexit is not a panic, any
    // recover calls in those deferred functions will return nil.
    public static void Goexit()
    {
        // Calling Goexit from the MAIN goroutine terminates main without returning while the
        // program keeps running its other goroutines — and crashes with "no goroutines" once they
        // all exit. That needs a live-goroutine registry and a main-thread parking protocol the
        // managed model does not have yet (docs/Phase4/DESIGN-goexit.md option A), so the
        // main-goroutine case stays GATED rather than silently doing something else: ending the
        // process here would kill goroutines Go would keep running. The gate is loud, never a no-op.
        if (!Goroutine.OnGoroutine)
        {
            throw new NotSupportedException(
                "runtime.Goexit from the main goroutine is not supported: main-goroutine Goexit " +
                "must leave the other goroutines running (see docs/Phase4/DESIGN-goexit.md). " +
                "Goexit from a goroutine is fully supported.");
        }

        // The goroutine case: unwind. GoFunc's finally-based defer machinery runs this goroutine's
        // deferred calls on the way out, recover() cannot observe the unwind (GoexitException is
        // deliberately not a PanicException), and the goroutine root swallows it — Go's three
        // documented Goexit properties, each falling out of machinery that already existed.
        throw new GoexitException();
    }
}
