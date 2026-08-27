// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using System.Collections.Generic;
using System.Runtime.InteropServices;
using go;

// Hand-finished conversion of signal_unix.go's OS-handler-INSTALL layer — sigenable/sigdisable/
// sigignore — over .NET PosixSignalRegistration, the Linux flavor of the os/signal bridge.
//
// WHY. signal_enable/signal_disable/signal_ignore (sigqueue.go, auto-converted and UNTOUCHED) do the
// sig.wanted/ignored bookkeeping and then call one of these three to reach the kernel. The auto bodies
// install Go's own sigtramp via setsig -> sysSigaction -> rt_sigaction, and sigenable/sigdisable first
// hand off to ensureSigM's goroutine over rt_sigprocmask. Both syscalls are unimplemented external
// stubs on the CLR — the CLR OWNS Linux signal handling (its own SIGSEGV/SIGCHLD/SIGTERM handlers,
// signals for GC/thread suspension), and there is no native Go trampoline to install — so every
// signal.Notify/Ignore threw (rt_sigaction) or threw on a background goroutine (rt_sigprocmask). That
// is the os/exec-family wall: TestWaitInterrupt/*, TestSIGQUIT, TestSIGCHLD.
//
// THE BRIDGE. .NET exposes exactly the async-notify primitive os/signal needs: PosixSignalRegistration
// delivers SIGINT/SIGQUIT/SIGTERM/SIGCHLD/SIGHUP/SIGCONT/SIGWINCH to a managed handler, and a handler
// that sets ctx.Cancel suppresses the default disposition (probe-confirmed, 2026-08-27). So the install
// layer registers, and the handler feeds the signal into the EXISTING sigqueue via sigsend — the same
// path the native sighandler used. sigsend re-checks sig.wanted itself, which is why ONE handler serves
// both Notify and Ignore: after Notify (wanted=1) sigsend delivers to the os/signal channel; after
// Ignore (wanted=0) sigsend drops it and ctx.Cancel has already suppressed the default. The whole
// delivery machinery below sigsend — signal_recv, the note wakeup, the channel — stays auto.
//
// ensureSigM and its enableSigChan/maskUpdatedChan handshake are ELIDED, not reimplemented: they were
// the protocol of the rt_sigprocmask goroutine, and PosixSignalRegistration owns its own delivery
// thread and mask. Those members remain in the auto file, now unreferenced.
//
// THE RESIDUAL. .NET's PosixSignal enum is a fixed set. Signals with no member — SIGUSR1/2, SIGPIPE,
// the real-time signals — cannot be registered and stay the honest rt_sigaction refusal (MapPosixSignal
// returns null; the install is a no-op and any test that needs them stays disclosed). SIGKILL/SIGSTOP
// are uncatchable in both runtimes by design. The wall bisects exactly at the enum boundary.
//
// PLACEMENT. The three names are registered goosLinux in manualConversionFuncs (manualTypeOperations.go),
// so a Linux -stdlib emission drops the auto bodies to placeholders and this file supplies them; the
// other ~1,440 lines of signal_unix.cs keep reconverting. Darwin's copy stays auto until its own arc.
// Design: docs/phase4/DESIGN-signal-posix-bridge.md.
[module: GoManualConversion]

namespace go;

using atomic = @internal.runtime.atomic_package;
using @internal;
using @internal.runtime;

partial class runtime_package
{
    // The live registrations, keyed by system signal number. Guarded by s_sigPosixLock because
    // os/signal.enableSignal/disableSignal already serialize on the handlers lock, but a converted
    // caller reaching signal_enable off that path must not race the dictionary.
    private static readonly object s_sigPosixLock = new object();
    private static readonly Dictionary<int, PosixSignalRegistration> s_sigPosixRegs = new Dictionary<int, PosixSignalRegistration>();

    // MapPosixSignal maps a Linux/amd64 signal number to the .NET PosixSignal member that carries it,
    // or null when no member exists (the rt_sigaction residual). Numbers are the stable Linux ABI
    // values mirrored by defs_linux_amd64.cs (_SIGHUP=1, _SIGINT=2, _SIGQUIT=3, _SIGTERM=15,
    // _SIGCHLD=17, _SIGCONT=18, _SIGWINCH=28).
    private static PosixSignal? MapPosixSignal(uint32 sig)
    {
        switch ((int)sig)
        {
            case 1:  return PosixSignal.SIGHUP;
            case 2:  return PosixSignal.SIGINT;
            case 3:  return PosixSignal.SIGQUIT;
            case 15: return PosixSignal.SIGTERM;
            case 17: return PosixSignal.SIGCHLD;
            case 18: return PosixSignal.SIGCONT;
            case 28: return PosixSignal.SIGWINCH;
            default: return null;
        }
    }

    // installPosixSignal (called under s_sigPosixLock) creates or replaces the registration for sig.
    // The handler suppresses the default disposition (a handler is installed) and feeds the existing
    // sigqueue; sigsend re-checks sig.wanted, so this one handler is correct for Notify and Ignore.
    private static void installPosixSignal(uint32 sig, PosixSignal ps)
    {
        int key = (int)sig;
        if (s_sigPosixRegs.TryGetValue(key, out PosixSignalRegistration existing))
        {
            existing.Dispose();
            s_sigPosixRegs.Remove(key);
        }
        uint32 s = sig;
        s_sigPosixRegs[key] = PosixSignalRegistration.Create(ps, ctx =>
        {
            ctx.Cancel = true;
            sigsend(s);
        });
    }

    // sigenable enables the Go signal handler to catch the signal sig.
    // It is only called while holding the os/signal.handlers lock,
    // via os/signal.enableSignal and signal_enable.
    internal static void sigenable(uint32 sig)
    {
        if (sig >= (uint32)len(sigtable))
        {
            return;
        }
        // SIGPROF is handled specially for profiling.
        if (sig == _SIGPROF)
        {
            return;
        }
        var t = Ꮡsigtable.at<sigTabT>((nint)(sig));
        if ((int32)((~t).flags & (int32)_SigNotify) != 0)
        {
            PosixSignal? ps = MapPosixSignal(sig);
            if (ps is null)
            {
                return; // residual: no PosixSignal member — stays the rt_sigaction refusal
            }
            lock (s_sigPosixLock)
            {
                atomic.Cas(ᏑhandlingSig.at<uint32>((nint)(sig)), 0, 1);
                installPosixSignal(sig, ps.Value);
            }
        }
    }

    // sigdisable disables the Go signal handler for the signal sig.
    // It is only called while holding the os/signal.handlers lock,
    // via os/signal.disableSignal and signal_disable. Stop/Reset returns the signal to DEFAULT
    // handling, so the registration is DISPOSED, not merely detached — disposing the last
    // registration restores the previous (default) disposition, so default-death-after-Reset holds.
    internal static void sigdisable(uint32 sig)
    {
        if (sig >= (uint32)len(sigtable))
        {
            return;
        }
        if (sig == _SIGPROF)
        {
            return;
        }
        var t = Ꮡsigtable.at<sigTabT>((nint)(sig));
        if ((int32)((~t).flags & (int32)_SigNotify) != 0)
        {
            lock (s_sigPosixLock)
            {
                int key = (int)sig;
                if (s_sigPosixRegs.TryGetValue(key, out PosixSignalRegistration reg))
                {
                    reg.Dispose();
                    s_sigPosixRegs.Remove(key);
                }
                atomic.Store(ᏑhandlingSig.at<uint32>((nint)(sig)), 0);
            }
        }
    }

    // sigignore ignores the signal sig.
    // It is only called while holding the os/signal.handlers lock,
    // via os/signal.ignoreSignal and signal_ignore. The registration is kept (Cancel suppresses the
    // default); delivery is gated by sig.wanted, which signal_ignore has already cleared, so sigsend
    // drops the signal — which IS ignore's observable behavior.
    internal static void sigignore(uint32 sig)
    {
        if (sig >= (uint32)len(sigtable))
        {
            return;
        }
        if (sig == _SIGPROF)
        {
            return;
        }
        var t = Ꮡsigtable.at<sigTabT>((nint)(sig));
        if ((int32)((~t).flags & (int32)_SigNotify) != 0)
        {
            PosixSignal? ps = MapPosixSignal(sig);
            if (ps is null)
            {
                return;
            }
            lock (s_sigPosixLock)
            {
                atomic.Store(ᏑhandlingSig.at<uint32>((nint)(sig)), 0);
                installPosixSignal(sig, ps.Value);
            }
        }
    }
}
