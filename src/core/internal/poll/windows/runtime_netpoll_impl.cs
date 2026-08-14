// runtime_netpoll_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of internal/poll's TEN //go:linkname entry points into the Go
// runtime's network poller (fd_poll_runtime.go). Design and ruling:
// docs/phase4/DESIGN-netpoll-managed-poller.md -- §9 RULED 2026-08-13, all eight recommendations
// ratified as the implementation contract.
//
// WHY A HAND-OWN AND NOT A WIRE-UP. The counterparts EXIST in the converted runtime
// (runtime/netpoll.cs carries poll_runtime_pollServerInit and all nine others with their
// //go:linkname comments intact), but wiring them would not be sufficient. The SHALLOW wall is
// asmstdcall -- netpollinit bottoms out in stdcall4(_CreateIoCompletionPort, …), the hand-written
// assembly go2cs has never implemented. The DEEP wall is the scheduler: behind netpollinit the
// bodies consume runtime mutexes with lock-rank bookkeeping, pollcache over persistentalloc, the
// runtime timer engine, gopark with a commit callback, goready, and g-pointer CAS protocols. None
// of that exists under the CLR and none CAN -- a go2cs goroutine is a managed thread the CLR
// schedules, so there is no g to park and no P to hand a ready g to. Decisively, Go's poller is only
// HALF an API: the other half (netpoll(delta), netpollBreak, netpollready) is called by the
// scheduler itself from findrunnable and sysmon. Under go2cs nothing would ever pump it -- a
// perfectly-wired conversion would initialize an IOCP and then block forever, because the thread Go
// dedicates to draining it IS the scheduler.
//
// So this file takes the fork runtime/managed_impl.cs states verbatim: "where a Go mechanism has no
// managed counterpart but its PUBLIC CONTRACT does, reimplement the CONTRACT at the API boundary and
// never emulate the mechanism… Everything below these entry points stays auto-converted and simply
// becomes unreachable." runtime/netpoll.cs and runtime/windows/netpoll_windows.cs stay converted and
// DEAD; zero runtime edits. The mechanical shape is the sibling runtime_sema_impl.cs: an _impl.cs
// beside the converted package, [module: go.GoManualConversion], bodies for the bodyless partials
// (which is what suppresses the PartialStubGenerator's throwing stubs).
//
// THE CONTRACT IS NARROW AND PINNED. The four error codes are shared constants declared on BOTH
// sides with "These must match" comments (fd_poll_runtime.cs:119-127, runtime/netpoll.cs:44-50), and
// execIO PANICS on any wait error outside that set (fd_windows.cs:207) -- so the bodies below return
// exactly pollNoError/pollErrClosing/pollErrTimeout and nothing else. pollErrNotPollable is never
// produced on the Windows path (it comes from the unix eventErr bit) and stays reserved. The ten
// linknames are one of Go's most compatibility-pinned internal surfaces: Go 1.23 rewrote
// netpoll_windows.go's internals while all ten crossed unchanged.
//
// ONE WAITER PER MODE, BY CONTRACT. "Concurrent calls to netpollblock in the same mode are
// forbidden, as pollDesc can hold only a single waiting goroutine for each mode"
// (runtime/netpoll.cs:577-578). fdMutex -- already operational above this layer via
// runtime_sema_impl.cs -- serializes readers and writers, so a mode needs a single-slot gate, not a
// queue. That is also why ONE Monitor replaces Go's lock-free rg/wg CAS choreography: Go splits
// pollDesc state across atomics because netpollcheckerr runs where the pd lock cannot be taken and
// netpollblock parks through gopark's publication protocol. Neither constraint survives the
// boundary -- every managed caller is an ordinary blocked thread -- so the CAS protocol would be
// emulation of a mechanism whose reason evaporated. (Same reduction runtime_sema_impl.cs made: "a
// bucket is just a count plus a FIFO waiter queue.")
//
// THE LOAD-BEARING SEPARATION: WAKE vs READINESS. A completion sets Ready and pulses; a deadline
// firing and pollUnblock pulse WITHOUT setting Ready. Go encodes this as pdReady-vs-pdNil-wake, and
// it is what makes waitCanceled's ignore-errors loop correct -- that loop waits for a COMPLETION
// specifically, ignoring the very timeout that woke its caller. Getting it wrong is not subtle: it
// either hangs the cancel path or reports a timeout as delivered data.
//
// SCOPE. Windows-first, per the design's §8: the corpus compiles at $(GoTargetOS)=windows, and the
// Linux internal/poll is a READINESS-model consumer (fd_unix.go retries the syscall after pd.wait)
// that wants a different managed mechanism entirely. This file lives in windows/ and rides the
// existing <Compile Include="$(GoTargetOS)/*.cs" /> glob (internal.poll.csproj) -- no csproj change.
// Other GOOS keep today's throwing stubs.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using Stopwatch = System.Diagnostics.Stopwatch;

// NOTE ON System.Threading.Timeout: internal/poll declares its own Timeout member (fd.go's
// deadline-exceeded error predicate), and member lookup inside poll_package beats both the using
// directive above and a using ALIAS -- so the timer sentinel is spelled out in full at each use
// below. That is not a style choice; anything shorter resolves to the method and fails CS0119.

// CA1416 reports ThreadPoolBoundHandle as "reachable on all platforms" because internal.poll targets
// net9.0 with no RID. That premise is false for THIS file by construction: it lives in windows/ and
// is compiled only when $(GoTargetOS)==windows selects that folder (internal.poll.csproj's L3 glob),
// which is the same mechanism every other per-GOOS source in the corpus relies on. Annotating the
// bodies [SupportedOSPlatform("windows")] instead would only push the warning onto their callers --
// converted code (pollDesc.init) that carries no platform annotations and must not grow any.
#pragma warning disable CA1416

// Hand-owned (no runtime_netpoll_impl.go exists, so a reconvert never regenerates this file);
// marked per the hand-own rules so a -stdlib run cannot emit a Go version over it.
[module: go.GoManualConversion]

namespace go.@internal;

partial class poll_package
{
    // ---- Mode ------------------------------------------------------------------------------------
    //
    // internal/poll names a mode with the RUNE, not an enum: pd.prepare((rune)'r', …),
    // pd.wait((rune)'w', …), and setDeadlineImpl's combined 'r'+'w' (fd_poll_runtime.cs:150).
    // Reset and Wait only ever see a single mode; SetDeadline is the one that also takes the sum.

    // `int` rather than `nint`: C# has no native-int constant (CS0283), and an int literal converts
    // implicitly at every comparison below, where the incoming mode is nint.
    private const int pollModeRead = 'r';                // 114
    private const int pollModeWrite = 'w';               // 119
    private const int pollModeBoth = pollModeRead + pollModeWrite;   // 233

    // ---- State -----------------------------------------------------------------------------------

    // Per-mode state. Go splits the equivalent across pd.rg/pd.wg (the waiter slot), pd.rd/pd.wd (the
    // deadline), pd.rseq/pd.wseq (the stale-timer guard) and pd.rt/pd.wt (the runtime timer); the
    // fields below are the same five facts under one lock.
    private sealed class ManagedPollMode
    {
        // A completion arrived and has not been consumed yet. Go's pdReady.
        internal bool Ready;

        // The deadline for this mode passed and has not since been re-set. STICKY: Go models it as
        // rd < 0 published into the info bits, and every subsequent prepare/wait in the mode returns
        // pollErrTimeout until a LATER SetDeadline rewrites the mode's deadline -- to zero (clears),
        // to the future (re-arms), or to the past (re-expires). net's deadline-dependent tests assert
        // exactly this shape; getting it wrong reads as "connection permanently broken after one
        // timeout" or as "timeout not sticky", both behaviorally loud.
        internal bool Expired;

        // Invalidates timer callbacks that lost a race. Go's rseq/wseq, bumped on every deadline
        // change and on unblock, checked by the fired callback under the pd lock. This is NOT
        // optional hardening in managed land: Timer.Change/Dispose do not synchronize with an
        // in-flight callback, so without the check a reset deadline can expire a fresh one.
        internal long Generation;

        // The Generation the currently-armed Timer was armed under. A callback is INERT unless the
        // two still agree: every deadline change bumps Generation, and only an arm republishes it,
        // so a callback that fired against a deadline since CLEARED or since superseded by an
        // unblock finds a mismatch and returns. Without this, SetReadDeadline(zero) followed by a
        // late callback from the deadline it cleared would expire the mode anyway -- the exact
        // "a reset deadline can expire a fresh one" failure §5 point 5 names.
        internal long ArmedGeneration = -1;

        // The desc this mode belongs to, so a timer callback can lock the gate its setters hold.
        // Assigned once at construction; never null after that.
        internal ManagedPollDesc Owner = null!;

        // Absolute due time in the runtimeNano epoch, meaningful only while a deadline is armed.
        // Kept because System.Threading.Timer's due time is a ~49.7-day uint of milliseconds while
        // Go's ceiling is ~292 years: a longer deadline is armed at the ceiling and RE-ARMED on fire
        // against this value under the generation check. Both ceilings are "never" for a socket
        // deadline -- this is an honesty note, not a behavior change.
        internal long DueNanos;

        // Created lazily on first arm, reused by Change (Go's pd.rt.modify), disposed at pollClose.
        internal Timer? Deadline;
    }

    private sealed class ManagedPollDesc
    {
        // ONE lock. §4.1 prices why lock-free is not owed here.
        internal readonly object Gate = new();

        // pollUnblock ran. Sticky for the desc's lifetime -- Go's re-open reset does not apply,
        // because a fresh desc is allocated per pollOpen (see the pollOpen commentary on fdseq).
        internal bool Closing;

        internal readonly ManagedPollMode Read = new();
        internal readonly ManagedPollMode Write = new();

        internal ManagedPollDesc()
        {
            Read.Owner = this;
            Write.Owner = this;
        }

        // The socket handle this desc was opened for. Bookkeeping only -- nothing routes by it.
        internal nuint Fd;

        // The CLR's own IOCP association for this socket (OQ1 mechanism (a)). Held for the desc's
        // lifetime and disposed at pollClose, BEFORE internal/poll closes the socket
        // (fd_windows.cs:378 runs pd.close() ahead of CloseFunc precisely so a poller can
        // unregister first). Disposing it does not close the handle: the SafeHandle below is
        // non-owning and ThreadPoolBoundHandle.Dispose releases only its own association.
        internal ThreadPoolBoundHandle? Bound;
        internal SafeHandle? BoundHandle;
    }

    // ctx token -> desc. Go returns the *pollDesc itself as a uintptr and defends staleness with
    // fdseq, because it REUSES pollDescs out of a pollcache (runtime/netpoll.cs:199-203, 319-321).
    // The managed side allocates a fresh desc per open and retires the token at close, so there is
    // no reuse for an ABA to exploit and that machinery is deliberately absent. Tokens start at 1;
    // 0 is internal/poll's own "no ctx" sentinel (pollDesc.runtimeCtx == 0 short-circuits every pd
    // call, fd_poll_runtime.cs:57-117), so it must never be minted.
    private static readonly ConcurrentDictionary<uintptr, ManagedPollDesc> pollTable = new();
    private static long nextPollToken;

    private static ManagedPollDesc? descFor(uintptr ctx) =>
        pollTable.TryGetValue(ctx, out ManagedPollDesc? desc) ? desc : null;

    private static ManagedPollMode modeState(ManagedPollDesc desc, nint mode) =>
        mode == pollModeWrite ? desc.Write : desc.Read;

    // ---- 1. runtime_pollServerInit ---------------------------------------------------------------

    // Go initializes the platform poller here (netpollGenericInit -> netpollinit -> an IOCP it will
    // drain from the scheduler). Under mechanism (a) the completion port is the CLR's own and needs
    // no creation, no drain thread, no shutdown story -- so this reduces to state initialization.
    // The caller wraps it in serverInit.Do (fd_poll_runtime.cs:48), and it is idempotent anyway.
    internal static partial void runtime_pollServerInit()
    {
        Volatile.Write(ref pollServerInitialized, true);
    }

    private static bool pollServerInitialized;

    // ---- 2. runtime_pollOpen ---------------------------------------------------------------------

    // Register fd with the poller; return an opaque nonzero ctx, or (0, errno) on failure so
    // pollDesc.init's errnoErr(syscall.Errno(errno)) path stays live (fd_poll_runtime.cs:49-52).
    //
    // THE BIND IS THE ARC'S ONE REAL OQ1 RISK, so it happens here rather than being deferred to the
    // first submit: ThreadPoolBoundHandle.BindHandle associates the socket with the CLR's IO
    // completion port, and §4.2 names "a handle the CLR refuses to bind" as the single condition
    // that would send the design to fallback mechanism (b). Binding at open means the first
    // net.Listen answers that question, with a real kernel, before any IO machinery is built on top.
    //
    // The handle is wrapped NON-OWNING: internal/poll closes the socket itself in FD.destroy
    // (CloseFunc right after pd.close()), so a SafeHandle that released it would double-close.
    internal static partial (uintptr, nint) runtime_pollOpen(uintptr fd)
    {
        // internal/poll reaches pollOpen only through serverInit.Do(runtime_pollServerInit), so a
        // false here is a sequencing regression in converted code, not a user-reachable state.
        if (!Volatile.Read(ref pollServerInitialized))
            throw new InvalidOperationException("runtime: pollOpen before pollServerInit");

        nuint handle = fd;
        ManagedPollDesc desc = new(){ Fd = handle };

        try
        {
            SafeHandle safeHandle = new NonOwningHandle(handle);
            desc.BoundHandle = safeHandle;
            desc.Bound = ThreadPoolBoundHandle.BindHandle(safeHandle);
        }
        catch (Exception ex)
        {
            // Report the failure the way Go's netpollopen does -- as an errno the caller converts
            // with errnoErr -- rather than letting an exception escape into converted code that has
            // no catch for it. ERROR_INVALID_PARAMETER is what CreateIoCompletionPort answers for a
            // handle that cannot be associated, which is the shape every BindHandle failure takes.
            desc.BoundHandle?.Dispose();
            return (0, ex is ArgumentException ? errorInvalidParameter : errorInvalidHandle);
        }

        // Interlocked, not the dictionary's own count: tokens must never be reused within a run.
        uintptr ctx = (uintptr)(nuint)(ulong)Interlocked.Increment(ref nextPollToken);
        pollTable[ctx] = desc;
        return (ctx, 0);
    }

    private const int errorInvalidHandle = 6;          // ERROR_INVALID_HANDLE
    private const int errorInvalidParameter = 87;      // ERROR_INVALID_PARAMETER

    // A SafeHandle over a descriptor somebody else owns. ThreadPoolBoundHandle.BindHandle takes a
    // SafeHandle (it keeps the handle alive across the association), but internal/poll's FD owns the
    // socket's lifetime end to end, so this wrapper must never release it.
    private sealed class NonOwningHandle : SafeHandle
    {
        internal NonOwningHandle(nuint value)
            : base(unchecked((nint)(-1)), ownsHandle: false)
        {
            SetHandle(unchecked((nint)value));
        }

        public override bool IsInvalid => handle == unchecked((nint)(-1)) || handle == 0;

        protected override bool ReleaseHandle() => true;
    }

    // ---- 3. runtime_pollClose --------------------------------------------------------------------

    // Unregister. Legal only after unblock -- Go throws "runtime: close polldesc w/o unblock"
    // (runtime/netpoll.cs:295-305), and that assert guards internal/poll's OWN sequencing, which is
    // unchanged converted code: FD.Close calls pd.evict() before decref -> destroy -> pd.close().
    // Kept as an InvalidOperationException so a future sequencing regression is loud rather than a
    // silently leaked association.
    //
    // Go's companion asserts (a still-blocked reader/writer on a closing desc) are NOT reproduced,
    // and their absence is reasoned rather than overlooked: they exist to catch pollcache reuse
    // corruption, and pollUnblock's PulseAll releases every waiter before Closing can be observed by
    // anything. There is no cache and no reuse here for them to defend.
    internal static partial void runtime_pollClose(uintptr ctx)
    {
        ManagedPollDesc? desc = descFor(ctx);

        if (desc is null)
            return;

        lock (desc.Gate)
        {
            if (!desc.Closing)
                throw new InvalidOperationException("runtime: close polldesc w/o unblock");

            stopDeadlineLocked(desc.Read);
            stopDeadlineLocked(desc.Write);

            desc.Read.Deadline?.Dispose();
            desc.Write.Deadline?.Dispose();
            desc.Read.Deadline = null;
            desc.Write.Deadline = null;

            // Before internal/poll closes the socket, per FD.destroy's ordering comment.
            desc.Bound?.Dispose();
            desc.BoundHandle?.Dispose();
            desc.Bound = null;
            desc.BoundHandle = null;
        }

        pollTable.TryRemove(ctx, out _);
    }

    // ---- 4. runtime_pollWait ---------------------------------------------------------------------

    // Block until IO-ready in mode, or return the deadline/closing code. Called by execIO after
    // ERROR_IO_PENDING (fd_windows.cs:188).
    internal static partial nint runtime_pollWait(uintptr ctx, nint mode)
    {
        ManagedPollDesc? desc = descFor(ctx);

        // The FD was closed under this caller. Go's fdseq answers a stale ctx the same way its
        // closing check does; pollErrClosing is the code execIO is prepared for (it maps to
        // errClosing, one of the three netpollErr values execIO does not panic on).
        if (desc is null)
            return pollErrClosing;

        return pollBlock(desc, modeState(desc, mode), ignoreErrors: false);
    }

    // ---- 5. runtime_pollWaitCanceled -------------------------------------------------------------

    // Block until IO-ready IGNORING deadline and closing -- Windows-only, issued by execIO after a
    // CancelIoEx (fd_windows.cs:219). Go's netpollblock(waitio: true).
    //
    // LIVENESS, which is the whole reason this entry point exists: after a timeout wake execIO still
    // owns a kernel-pending overlapped operation, so it cancels and must then HARVEST -- it may not
    // abandon the op, because the kernel still holds the buffers. The wait terminates because the
    // kernel ALWAYS posts a completion for a cancelled overlapped operation (ERROR_OPERATION_ABORTED),
    // and if the op won the race instead, CancelIoEx answers ERROR_NOT_FOUND with the completion
    // already in flight; execIO handles both (fd_windows.cs:212-231). The one configuration that
    // would break this is an op whose completion packet was SKIPPED reaching the cancel path -- and
    // it cannot: skip-on-success (fd.skipSyncNotif) suppresses the packet only for a submit that
    // returned synchronous success, and that path returns from execIO before any wait
    // (fd_windows.cs:171-177). Every path that CAN reach a wait has a completion packet guaranteed
    // in flight. That invariant is what OQ5 ratified keeping, and it gets a dedicated S2 test.
    internal static partial void runtime_pollWaitCanceled(uintptr ctx, nint mode)
    {
        ManagedPollDesc? desc = descFor(ctx);

        if (desc is null)
            return;

        pollBlock(desc, modeState(desc, mode), ignoreErrors: true);
    }

    // The shared wait loop. Mirrors netpollblock + poll_runtime_pollWait's retry
    // (runtime/netpoll.cs:355-374, 556-613) with the parking replaced by Monitor.Wait.
    private static nint pollBlock(ManagedPollDesc desc, ManagedPollMode mode, bool ignoreErrors)
    {
        lock (desc.Gate)
        {
            while (true)
            {
                // READINESS IS CONSUMED FIRST, ahead of both error checks. Go does the same
                // (netpollblock consumes pdReady before netpollcheckerr, runtime/netpoll.cs:585-589)
                // and it is a real behavior, not an ordering accident: a completion that RACED the
                // deadline is still delivered to the caller, matching Go's preference for returning
                // real IO over a same-instant timeout.
                if (mode.Ready)
                {
                    mode.Ready = false;
                    return pollNoError;
                }

                if (!ignoreErrors)
                {
                    // Check order is fixed: closing beats timeout (netpollcheckerr, :539-554).
                    if (desc.Closing)
                        return pollErrClosing;

                    if (mode.Expired)
                        return pollErrTimeout;
                }

                // Woken without readiness by a deadline, an unblock, or a deadline RESET that
                // superseded the one that woke us -- Go's comment for the same retry: "Can happen if
                // timeout has fired and unblocked us, but before we had a chance to run, timeout has
                // been reset. Pretend it has not happened and retry."
                Monitor.Wait(desc.Gate);
            }
        }
    }

    // ---- 6. runtime_pollReset --------------------------------------------------------------------

    // Clear consumed readiness; fail fast if closing or expired. Called by execIO before EVERY
    // submit (fd_windows.cs:164). Order is netpollcheckerr's: closing, then timeout, then clear.
    internal static partial nint runtime_pollReset(uintptr ctx, nint mode)
    {
        ManagedPollDesc? desc = descFor(ctx);

        if (desc is null)
            return pollErrClosing;

        ManagedPollMode m = modeState(desc, mode);

        lock (desc.Gate)
        {
            if (desc.Closing)
                return pollErrClosing;

            if (m.Expired)
                return pollErrTimeout;

            m.Ready = false;
            return pollNoError;
        }
    }

    // ---- 7. runtime_pollSetDeadline --------------------------------------------------------------

    // Arm, replace or clear the read and/or write deadline. `d` is a RELATIVE ns duration, already
    // normalized by setDeadlineImpl (fd_poll_runtime.cs:168-174): d > 0 arms, d == 0 clears, and
    // d < 0 means already expired -- setDeadlineImpl rewrites an exactly-now deadline to -1 so that
    // "right now" is never confused with "no deadline". mode is 'r', 'w', or 'r'+'w'.
    //
    // Go's single-combo-timer optimization (netpoll.cs:410-414) is NOT reproduced: two timers with
    // the same due time are observationally equivalent, and the combo machinery exists to save a
    // RUNTIME timer -- a resource the managed side is not short of.
    internal static partial void runtime_pollSetDeadline(uintptr ctx, int64 d, nint mode)
    {
        ManagedPollDesc? desc = descFor(ctx);

        if (desc is null)
            return;

        lock (desc.Gate)
        {
            // Go returns without touching anything once the desc is closing (:388-391).
            if (desc.Closing)
                return;

            bool wake = false;

            if (mode == pollModeRead || mode == pollModeBoth)
                wake |= applyDeadlineLocked(desc.Read, d);

            if (mode == pollModeWrite || mode == pollModeBoth)
                wake |= applyDeadlineLocked(desc.Write, d);

            // A deadline set in the PAST fires NOW, against the CURRENT waiter (:448-458): wake the
            // blocked mode without setting Ready, so its loop re-checks and returns pollErrTimeout,
            // and execIO then runs the cancel-and-harvest path.
            if (wake)
                Monitor.PulseAll(desc.Gate);
        }
    }

    // Returns true when this call expired the mode immediately (a deadline in the past), which is
    // the only case that needs a wake. Caller holds the gate.
    private static bool applyDeadlineLocked(ManagedPollMode mode, int64 d)
    {
        // Every deadline change invalidates whatever timer is in flight for this mode, whether it is
        // being replaced, cleared or expired. This is Go's rseq/wseq bump, and here it is what makes
        // "deadline REPLACED while blocked -- old never fires, new does" hold.
        mode.Generation++;
        stopDeadlineLocked(mode);

        if (d > 0)
        {
            // Re-set to the future: clears a previous expiry (the sticky flag's only exit besides a
            // clear) and arms.
            mode.Expired = false;
            mode.DueNanos = runtimeNano() + d;
            armDeadlineLocked(mode);
            return false;
        }

        if (d == 0)
        {
            // No deadline. Clears a previous expiry -- this is what SetReadDeadline(time.Time{})
            // means, and the S2 matrix asserts the read that follows it succeeds.
            mode.Expired = false;
            return false;
        }

        // d < 0: already expired.
        mode.Expired = true;
        return true;
    }

    // Stops the timer AND revokes the armed generation, so a callback already in flight -- which
    // Timer.Change explicitly does not wait for -- finds the mismatch and returns.
    private static void stopDeadlineLocked(ManagedPollMode mode)
    {
        mode.ArmedGeneration = -1;
        mode.Deadline?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
    }

    // Timer's due time is milliseconds in a uint, so the longest single arm is ~49.7 days; a longer
    // Go deadline is armed at the ceiling and re-armed on fire against DueNanos. Both are "never"
    // for a socket deadline.
    private const long maxTimerMillis = uint.MaxValue - 2;

    private static void armDeadlineLocked(ManagedPollMode mode)
    {
        long remainingNanos = mode.DueNanos - runtimeNano();
        long dueMillis = remainingNanos <= 0 ? 0 : Math.Min(remainingNanos / 1_000_000, maxTimerMillis);

        // One Timer serves every arm of this mode for the desc's lifetime (Go's pd.rt.modify reuses
        // its timer the same way); the arm is identified by the generation republished below.
        mode.Deadline ??= new Timer(static state => deadlineFired((ManagedPollMode)state!), mode, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

        mode.ArmedGeneration = mode.Generation;
        mode.Deadline.Change(dueMillis, System.Threading.Timeout.Infinite);
    }

    // The armed deadline fired. Everything here is FORCED into Go's rseq/wseq shape by .NET
    // semantics rather than chosen: Timer.Change and Timer.Dispose do not synchronize with an
    // in-flight callback, so a callback that lost a race to a reset or an unblock must detect that
    // and return, or a reset deadline expires a fresh one.
    private static void deadlineFired(ManagedPollMode mode)
    {
        ManagedPollDesc desc = mode.Owner;

        lock (desc.Gate)
        {
            // Go's rseq/wseq check, verbatim in intent: this callback speaks for the arm whose
            // generation was published at Change time, and any deadline change or unblock since has
            // bumped Generation past it. Losing the race means doing NOTHING -- the state the winner
            // installed is the live one.
            if (mode.ArmedGeneration != mode.Generation || desc.Closing)
                return;

            long remainingNanos = mode.DueNanos - runtimeNano();

            // The arm was clamped to Timer's ~49.7-day ceiling (or the timer fired early): re-arm
            // for the remainder rather than expiring a deadline that has not arrived.
            if (remainingNanos > 0)
            {
                armDeadlineLocked(mode);
                return;
            }

            mode.ArmedGeneration = -1;
            mode.Expired = true;

            // Wake WITHOUT setting Ready -- the waiter re-checks and returns pollErrTimeout.
            Monitor.PulseAll(desc.Gate);
        }
    }

    // ---- 8. runtime_pollUnblock ------------------------------------------------------------------

    // Mark closing, wake both waiters with pollErrClosing, stop both timers. Reached from FD.Close
    // via pd.evict() (fd_windows.cs:406), always before destroy -> pd.close().
    //
    // Go throws on a second unblock ("runtime: unblock on closing polldesc") because a repeat there
    // indicates pollcache reuse corruption. That hazard does not exist here -- fresh desc per open,
    // no cache -- so this is idempotent instead, which is also what §4.1's "sticky forever" means.
    internal static partial void runtime_pollUnblock(uintptr ctx)
    {
        ManagedPollDesc? desc = descFor(ctx);

        if (desc is null)
            return;

        lock (desc.Gate)
        {
            if (desc.Closing)
                return;

            desc.Closing = true;

            // Invalidate both modes' in-flight timer callbacks, then stop them.
            desc.Read.Generation++;
            desc.Write.Generation++;
            stopDeadlineLocked(desc.Read);
            stopDeadlineLocked(desc.Write);

            // Wake WITHOUT setting Ready, exactly as the deadline path does.
            Monitor.PulseAll(desc.Gate);
        }
    }

    // ---- 9. runtime_isPollServerDescriptor -------------------------------------------------------

    // Report whether fd IS the poller's own descriptor. Under mechanism (a) there is no exposed
    // poll-server descriptor -- the completion port is the CLR's, and a Go program cannot
    // legitimately hold it -- so the honest answer is false for every fd. OQ1 ruled this explicitly.
    // The only consumer is the test-only IsPollDescriptor (fd_poll_runtime.cs:203), which is
    // linkname-pinned public surface (go.dev/issue/67401) and so is answered rather than stubbed.
    internal static partial bool runtime_isPollServerDescriptor(uintptr fd) => false;

    // ---- 10. runtimeNano -------------------------------------------------------------------------

    // //go:linkname runtimeNano runtime.nanotime -- monotonic ns on an arbitrary epoch. The
    // sync/runtime_impl.cs:248-251 shape verbatim; it is declared in the FLAT shared file
    // (fd_poll_runtime.cs:18) and so is needed on every GOOS, but the body can live here for as long
    // as windows/ is the only platform with real bodies for the other nine.
    private static readonly long nanotimeBase = Stopwatch.GetTimestamp();

    internal static partial int64 runtimeNano() =>
        unchecked((long)((Stopwatch.GetTimestamp() - nanotimeBase) * (1_000_000_000.0 / Stopwatch.Frequency)));

    // ---- The completion sink ---------------------------------------------------------------------

    // The ONLY writer of Ready, and the seam the submit half of this arc (S2) drives: when the kernel
    // completes an overlapped operation internal/poll submitted, the CLR's IO thread pool runs the
    // record's callback, which lands here.
    //
    // RECORDED SEAM (design §4.3, for S2). The overlapped submissions live in the `syscall` and
    // `internal/syscall/windows` packages, which internal/poll REFERENCES -- so they cannot call back
    // into this package directly. The plumbing S2 lands is therefore a push: this package installs a
    // per-handle readiness sink into the submit seam's registry at pollOpen, and the registry's
    // completion callback invokes it. The contract layer above is unaffected either way, which is
    // what §4.2 means by the choice being confined to pollServerInit/pollOpen and the callback's
    // plumbing.
    //
    // ⚠ HARD CONSTRAINT THE BIND IMPOSES ON S2, recorded here because it is not obvious from the
    // design text and is undefined behavior rather than a wrong answer. Because pollOpen associates
    // the socket with the CLR's OWN completion port, every overlapped operation issued on that socket
    // must carry a NativeOverlapped allocated from THAT bound handle
    // (ThreadPoolBoundHandle.AllocateNativeOverlapped). The CLR's IO thread pool resolves a completion
    // by reading its own state off the NativeOverlapped it handed out; a FOREIGN overlapped arriving
    // at that port is not an error it reports, it is memory it misreads.
    //
    // This is not confined to the S2 wrappers. internal/poll has a live path that submits an
    // overlapped operation WITHOUT going through execIO at all: FD.WSAIoctl
    // (windows/sockopt_windows.cs:11) passes the CALLER's syscall.Overlapped straight to
    // syscall.WSAIoctl, and internal/poll's own TestWSASocketConflict drives exactly that shape --
    // fd.Init("tcp", pollable: true) and then a SIO_TCP_INFO WSAIoctl with a caller-owned OVERLAPPED
    // and event. Before the bind that test died at Init, so it never reached the ioctl; it does now.
    // Whether the completion is actually posted there is a question to MEASURE, not to reason about
    // (an unconnected socket may well fail the ioctl synchronously, in which case no packet is
    // queued), which is why the internal/poll row is measured at S1 rather than only at S2.
    internal static void netpollReady(uintptr ctx, nint mode)
    {
        ManagedPollDesc? desc = descFor(ctx);

        if (desc is null)
            return;

        ManagedPollMode m = modeState(desc, mode);

        lock (desc.Gate)
        {
            m.Ready = true;
            Monitor.PulseAll(desc.Gate);
        }
    }
}
