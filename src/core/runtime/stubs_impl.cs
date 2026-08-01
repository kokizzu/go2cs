// stubs_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Bodies for the runtime's assembly primitives that DO have an exact managed form.
//
// runtime/stubs.go declares a large family of functions implemented in `.s`; go2cs emits each as a
// bodyless `partial` and the PartialStubGenerator fills it with a throwing stub. That default is
// right for genuine raw metal (duffcopy, the retpoline thunks, the write barriers) — but a few of
// them are not raw metal at all once the g/m/p model is gone, and stubbing THOSE turns working
// code into a crash. Only those few are implemented here; every other assembly stub deliberately
// keeps throwing, so an unported path fails loudly instead of silently doing nothing.
//
// Hand-owned: there is no stubs_impl.go, so a -stdlib reconvert never regenerates this file.

using System.Threading;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    // systemstack runs fn on the system stack. Go's own contract already says that when the caller
    // is ALREADY on a system stack (g0 or gsignal), systemstack "calls fn directly and returns" —
    // and in the managed model there is exactly one stack per goroutine and no g0 to switch to, so
    // that branch is the only branch. This is a faithful implementation, not an approximation.
    internal static partial void systemstack(Action fn) => fn();

    // procyield spins for the given number of iterations, emitting the architecture's pause hint.
    // Thread.SpinWait is the CLR's spelling of exactly that.
    internal static partial void procyield(uint32 cycles) => Thread.SpinWait((int)cycles);

    // NOT implemented here, on purpose:
    //   mcall(fn)     — parks the current goroutine and runs fn on the system stack, never
    //                   returning to the caller. Its only callers are the scheduler's own
    //                   continuations (gosched_m, park_m, goexit0, exitsyscall0), and there is no
    //                   managed answer at THIS layer: the managed runtime has no g to park and no
    //                   run queue to hand it to. The public entry points that reach it are
    //                   reimplemented one level up instead (managed_impl.cs).
    //   getg()        — returns the current g. Left throwing while no reachable path needs it, so
    //                   that any path that does surfaces as a loud, locatable failure rather than
    //                   quietly operating on a fabricated goroutine descriptor.
    //   getcallerpc / getcallersp / getclosureptr / getfp — read the caller's machine registers;
    //                   the managed equivalent (a StackTrace walk) answers a different question
    //                   and would make Go's PC arithmetic silently wrong.
}
