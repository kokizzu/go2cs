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

    // libc signal(2), used only to clear an inherited SIG_IGN so .NET will install its handler.
    // SIG_DFL is the null handler on Linux.
    private static readonly IntPtr SIG_DFL = IntPtr.Zero;
    [DllImport("libc", EntryPoint = "signal", SetLastError = true)]
    private static extern IntPtr sys_signal(int signum, IntPtr handler);

    // MapPosixSignal maps a Linux/amd64 signal number to the .NET PosixSignal value that carries it,
    // or null for the residual. Numbers are the stable Linux ABI values mirrored by
    // defs_linux_amd64.cs (_SIGHUP=1, _SIGINT=2, _SIGQUIT=3, _SIGTERM=15, _SIGCHLD=17, _SIGCONT=18,
    // _SIGWINCH=28).
    //
    // The enum members carry NEGATIVE values, and .NET's Unix implementation deliberately passes a
    // POSITIVE value through as the raw platform signal number — probe-measured 2026-08-27:
    // Create((PosixSignal)10) registers, SIGUSR1 delivers to the handler, and ctx.Cancel suppresses
    // the default death. So the wall is NOT the enum: SIGUSR1/SIGUSR2 ride the raw cast (before
    // this, TestStop/user_defined_signal_1's self-kill took the whole test host down with it, exit
    // 138, leaving every later test unmeasured). The residual is now the set the raw cast cannot
    // honestly serve: the synchronous faults the CLR owns (SIGILL/SIGABRT/SIGBUS/SIGFPE/SIGSEGV —
    // registering those would sit under the CLR's own fault handling), SIGPIPE (registers but does
    // not deliver — .NET handles EPIPE internally and the same probe measured the timeout),
    // SIGPROF (sigenable's own guard), the real-time range, and SIGKILL/SIGSTOP (uncatchable
    // everywhere).
    private static PosixSignal? MapPosixSignal(uint32 sig)
    {
        switch ((int)sig)
        {
            case 1:  return PosixSignal.SIGHUP;
            case 2:  return PosixSignal.SIGINT;
            case 3:  return PosixSignal.SIGQUIT;
            case 10: return (PosixSignal)10;  // SIGUSR1, raw platform number
            case 12: return (PosixSignal)12;  // SIGUSR2, raw platform number
            case 15: return PosixSignal.SIGTERM;
            case 17: return PosixSignal.SIGCHLD;
            case 18: return PosixSignal.SIGCONT;
            case 28: return PosixSignal.SIGWINCH;
            default: return null;
        }
    }

    // The libc sigaction READ side (act = NULL), used only to observe dispositions this process
    // INHERITED — a pure read conflicts with nothing the CLR owns, unlike the install side the
    // bridge exists to avoid. glibc's struct sigaction leads with the sa_handler union on
    // linux-x64; 160 bytes generously covers the 152-byte struct. SIG_IGN is (void*)1.
    [DllImport("libc", EntryPoint = "sigaction", SetLastError = false)]
    private static extern int sys_sigaction_read(int signum, IntPtr act, IntPtr oldact);

    private static readonly IntPtr SIG_IGN_HANDLER = (IntPtr)1;

    // Seeds runtime.sig.ignored with the dispositions this process INHERITED, the way Go's initsig
    // does via getsig(i) == _SIG_IGN -> sigInitIgnored(i). Runs once when the runtime assembly
    // loads — before any test or user code can ask os/signal.Ignored, and before the bridge's own
    // installs (which deliberately clear an inherited SIG_IGN) could repaint the picture. This is
    // what makes a child under nohup answer Ignored(SIGHUP) == true (TestDetectNohup's second
    // half, TestNohup's whole nohup family).
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void InitInheritedIgnoredSignals()
    {
        if (!OperatingSystem.IsLinux())
            return;

        IntPtr old = Marshal.AllocHGlobal(160);
        try
        {
            // The classic asynchronous range. 9/19 are SIGKILL/SIGSTOP (their dispositions cannot
            // differ from default); 32/33 are NPTL-reserved and not observable signals.
            for (int signum = 1; signum <= 31; signum++)
            {
                if (signum == 9 || signum == 19)
                    continue;

                if (sys_sigaction_read(signum, IntPtr.Zero, old) != 0)
                    continue;

                if (Marshal.ReadIntPtr(old) == SIG_IGN_HANDLER)
                    sigInitIgnored((uint32)signum);
            }
        }
        catch
        {
            // Defensive: an unreadable disposition just leaves that signal reported non-ignored,
            // which is where the mask already was.
        }
        finally
        {
            Marshal.FreeHGlobal(old);
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
        // Go's signal.Notify OVERRIDES an inherited SIG_IGN (setsig installs unconditionally); .NET's
        // PosixSignalRegistration RESPECTS it and won't install a handler for a signal it saw ignored.
        // Clear the ignore to SIG_DFL so .NET installs its handler — the faithful analog of Go's
        // override, applied only to a signal actually being enabled/ignored through os/signal, and
        // done before Create because .NET decides installation from the disposition it sees then.
        sys_signal((int)sig, SIG_DFL);
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
