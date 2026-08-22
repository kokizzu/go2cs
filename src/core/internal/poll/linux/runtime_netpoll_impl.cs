// runtime_netpoll_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of internal/poll's TEN //go:linkname entry points into the Go
// runtime's network poller (fd_poll_runtime.go) for the LINUX flavor: a poller that owns no
// poller. Every descriptor is un-armable, so every descriptor degrades to the BLOCKING path --
// which is exactly the fallback Go itself takes when epoll refuses a descriptor.
//
// WHY THIS FILE EXISTS -- measured, not inferred. On Linux, os.newFile marks every OpenFile, Pipe
// and socket descriptor `pollable` (os/linux/file_unix.cs:167) and calls FD.Init(…, true) ->
// pollDesc.init -> serverInit.Do(runtime_pollServerInit) -> runtime_pollOpen. The Windows flavor
// never takes that path for files (its os passes pollable:false for every file, pipe and console),
// which is why 159/159 banked rows validate there while the 2026-08-21 Linux census measured 61 of
// the 67 residual rows dying on the very first fixture read:
//
//   System.NotImplementedException: runtime_pollServerInit: external (assembly or cgo) function is
//   not implemented  at sync.Once.Do -> poll.init -> os.newFile -> os.Open -> mustLoadFile
//
// (docs/phase4/BOARD-next-validation-candidates.md, "Linux measurement campaign Part 3", wall W1.)
// The throwing stubs were the PartialStubGenerator's answer to ten bodyless partials that only
// windows/runtime_netpoll_impl.cs fills; this file is the linux flavor's answer, in the shape the
// census priced: ServerInit a no-op, pollOpen returning a not-pollable errno.
//
// WHAT GO DOES WITH A DESCRIPTOR EPOLL REFUSES, AND WHY THAT IS THE CONTRACT HERE.
// runtime.netpollopen is one epoll_ctl(EPOLL_CTL_ADD) (runtime/netpoll_epoll.go:49). For a regular
// file or a directory the kernel answers EPERM, pollOpen returns (0, EPERM), pollDesc.init returns
// errnoErr(EPERM) (fd_poll_runtime.cs:49-52), FD.Init sets isBlocking = 1 and hands the error back
// (linux/fd_unix.cs:60-65), and os.newFile restores the blocking mode it had set and carries on
// (file_unix.go: "An error here indicates a failure to register with the netpoll system. That can
// happen for a file descriptor that is not supported by epoll/kqueue; for example, disk files on
// Linux systems. We assume that any real error will show up in later I/O."). From then on
// runtimeCtx == 0: pollable() is false, prepare/evict/waitCanceled/close return early, wait() is
// never reached because a blocking read(2) returns data or EOF rather than EAGAIN, and SetDeadline
// answers ErrNoDeadline. That is Go's OWN behavior for every regular file on Linux. This file does
// nothing but answer "un-armable" for EVERY descriptor, so the whole converted fallback runs as
// written -- zero os/poll edits -- and the per-GOOS file rides the existing
// <Compile Include="$(GoTargetOS)/*.cs" /> glob (internal.poll.csproj): no csproj change, exactly
// as the Windows flavor landed (NETPOLL-S1, 3f79fec70).
//
// WHAT DEGRADES, STATED RATHER THAN LEFT TO BE DISCOVERED. Go arms pipes, FIFOs, ttys and sockets;
// this flavor cannot yet -- there is no readiness poller (epoll has no CLR surface, and fd_unix.go's
// wait-then-retry-syscall shape wants a different managed mechanism from the Windows completion
// model: docs/phase4/DESIGN-netpoll-managed-poller.md §8) -- so those descriptors take the blocking
// path too, with exactly the semantics Go documents for files that do not support SetDeadline:
// a read blocks the calling thread (a go2cs goroutine IS a managed thread, so that is one blocked
// thread, not a stalled scheduler), SetDeadline returns ErrNoDeadline, and Close cannot cancel an
// I/O already in flight (os.File.Close: "On files that support SetDeadline, any pending I/O
// operations will be canceled" -- these do not). For os.Pipe that is the one VISIBLE change from
// Go on Linux: Close on the read end does not unblock a Read blocked in read(2); the read returns
// when the write end closes. (The PipeCloseUnblocksRead behavioral test names the shape; it faulted
// on this flavor before this file, at the stub.) For a net socket FD.Init's error is NOT discarded:
// net.netFD.init propagates it, so Dial/Listen return "operation not permitted" on this flavor
// rather than a NotImplementedException -- an honest error, not a working socket. The socket half
// of Linux is the readiness-poller design above, a separate arc; nothing here pre-empts it, and
// when it lands pollOpen's errno arm becomes what it is in Go: the answer for files alone.
//
// THE ERRNO IS GO'S, NOT INVENTED. EPERM is what epoll_ctl(2) answers for "the target file fd does
// not support epoll … for example, a regular file or a directory" -- the errno Go's netpoll produces
// for exactly the class this file models. It is invisible on every os path (newFile tests
// pollErr != nil and discards the value) and surfaces only through net, above. No fstat
// discrimination is made: every descriptor is un-armable here for the same reason, so one answer is
// the truthful one, and it costs no syscall.
//
// SCOPE. Linux only. darwin's os already keeps regular files and directories out of kqueue by
// fstat (file_unix.go's *BSD branch) but reaches these stubs for pipes and sockets; that corpus does
// not build today (docs/phase4/DESIGN-multiplatform-corpus.md §12: os/dir.cs, 19 pre-existing
// errors) and so cannot be measured, and an unmeasured copy is not shipped. When it builds, this
// file is its remedy byte for byte -- and L3's merge leaves two per-GOOS copies of a principal-less
// companion alone (platformHandOwn.go resolves a placement only for a companion whose principal was
// emitted). Windows keeps its own poller in windows/runtime_netpoll_impl.cs, untouched.

using System;
using System.Threading;
using Stopwatch = System.Diagnostics.Stopwatch;

// Hand-owned (no runtime_netpoll_impl.go exists, so a reconvert never regenerates this file);
// marked per the hand-own rules so a -stdlib run cannot emit a Go version over it.
[module: go.GoManualConversion]

namespace go.@internal;

partial class poll_package
{
    // ---- 1. runtime_pollServerInit ---------------------------------------------------------------

    // Go initializes the platform poller here (netpollGenericInit -> netpollinit -> epoll_create1).
    // There is no poller to create; what remains is the sequencing fact the Windows flavor keeps
    // too, so the two flavors make the same promise: pollOpen is reached only after serverInit.Do
    // has run this (fd_poll_runtime.cs:48). Idempotent; the caller's sync.Once runs it once anyway.
    internal static partial void runtime_pollServerInit()
    {
        Volatile.Write(ref pollServerInitialized, true);
    }

    private static bool pollServerInitialized;

    // ---- 2. runtime_pollOpen ---------------------------------------------------------------------

    // The one body that carries the whole design. Go's contract: (ctx, 0) when epoll_ctl accepted
    // the descriptor, (0, errno) when it did not, and internal/poll converts a nonzero errno with
    // errnoErr(syscall.Errno(errno)) (fd_poll_runtime.cs:49-52). With no poller every descriptor is
    // the second case, with the errno epoll itself gives Go for an un-armable descriptor.
    internal static partial (uintptr, nint) runtime_pollOpen(uintptr fd)
    {
        // internal/poll reaches pollOpen only through serverInit.Do(runtime_pollServerInit), so a
        // false here is a sequencing regression in converted code, not a user-reachable state.
        if (!Volatile.Read(ref pollServerInitialized))
            throw new InvalidOperationException("runtime: pollOpen before pollServerInit");

        return (0, unArmableErrno);
    }

    // EPERM, bound from the converted syscall package rather than spelled as 1, so the value is the
    // same constant Go's own netpollopen would have returned through (runtime/netpoll_epoll.go:54).
    private static readonly nint unArmableErrno = (nint)(nuint)go.syscall_package.EPERM;

    // ---- 3-8. The ctx-taking contracts -----------------------------------------------------------

    // Every caller in fd_poll_runtime.cs guards `pd.runtimeCtx == 0` before reaching these, and
    // pollOpen above never issues a ctx, so none of them can run: close/evict/waitCanceled return
    // early, prepare returns nil, wait returns "waiting for unsupported file type", setDeadlineImpl
    // returns ErrNoDeadline. A body that can only execute on a ctx this poller never minted throws
    // rather than returning a code that would be a lie -- the same loud-over-silent choice the
    // Windows flavor makes for pollOpen-before-ServerInit.
    internal static partial void runtime_pollClose(uintptr ctx) =>
        throw Unreachable(nameof(runtime_pollClose), ctx);

    internal static partial nint runtime_pollWait(uintptr ctx, nint mode) =>
        throw Unreachable(nameof(runtime_pollWait), ctx);

    internal static partial void runtime_pollWaitCanceled(uintptr ctx, nint mode) =>
        throw Unreachable(nameof(runtime_pollWaitCanceled), ctx);

    internal static partial nint runtime_pollReset(uintptr ctx, nint mode) =>
        throw Unreachable(nameof(runtime_pollReset), ctx);

    internal static partial void runtime_pollSetDeadline(uintptr ctx, int64 d, nint mode) =>
        throw Unreachable(nameof(runtime_pollSetDeadline), ctx);

    internal static partial void runtime_pollUnblock(uintptr ctx) =>
        throw Unreachable(nameof(runtime_pollUnblock), ctx);

    private static InvalidOperationException Unreachable(string contract, uintptr ctx) =>
        new($"runtime: {contract} reached with ctx {ctx} on the linux flavor, whose pollOpen never " +
            "issues one -- internal/poll guards every such call on runtimeCtx != 0 (fd_poll_runtime.cs)");

    // ---- 9. runtime_isPollServerDescriptor -------------------------------------------------------

    // Report whether fd IS the poller's own descriptor. No epoll instance was ever created, so no fd
    // is it. The only consumer is the test-only IsPollDescriptor (fd_poll_runtime.cs:203), which is
    // linkname-pinned public surface (go.dev/issue/67401): os/exec's TestMain asks it of every open
    // fd at exit, so it is answered rather than stubbed.
    internal static partial bool runtime_isPollServerDescriptor(uintptr fd) => false;

    // ---- 10. runtimeNano -------------------------------------------------------------------------

    // //go:linkname runtimeNano runtime.nanotime -- monotonic ns on an arbitrary epoch. Declared in
    // the FLAT shared file (fd_poll_runtime.cs:18), so a body is owed on every GOOS that fills the
    // other nine; the windows flavor's shape verbatim (sync/runtime_impl.cs before it).
    private static readonly long nanotimeBase = Stopwatch.GetTimestamp();

    internal static partial int64 runtimeNano() =>
        unchecked((long)((Stopwatch.GetTimestamp() - nanotimeBase) * (1_000_000_000.0 / Stopwatch.Frequency)));
}
