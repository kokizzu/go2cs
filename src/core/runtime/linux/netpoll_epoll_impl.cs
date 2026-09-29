// netpoll_epoll_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// The linux runtime poller's break, under a managed host that never creates the poller.
//
// netpollBreak writes one word to netpollEventFd, the eventfd netpollinit creates. netpollGenericInit
// is a no-op here (netpoll_impl.cs: there is no runtime-side poller and nothing to drain it), so the
// eventfd never exists. While write1 was a throwing stub, netpollBreak failed as one loud row with a
// NotImplementedException, which is what netpoll_impl.cs intends. Once write1 had its libc body
// (stubs2_impl.cs), the write answered -EBADF and netpollBreak took Go's throw: a fatal error that
// ended the linux runtime row's test host at TestNetpollBreak (COORD ruling, ledger 80fc879c1c).
//
// netpollBreak therefore refuses by name, before the write.
// The row keeps its one loud, locatable failure and the host lives. Linux only: windows reaches
// stdcall4 -> getg, which still throws, and darwin's netpollBreak goes through wakeNetpoll(kq), not
// write1 (darwin has no run layer, so that path is unmeasured).
//
// Hand-owned (no netpoll_epoll_impl.go exists, so a reconvert never regenerates this file).

using System;
using System.Threading;
using go.golib;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    internal static void netpollBreak() =>
        throw new PanicException("runtime: netpollBreak: the managed host has no runtime poller to break (netpollGenericInit creates no eventfd; see netpoll_impl.cs)");

    // ---- the guard's view (GolibTests RuntimeFdSyscallTests) ----

    /// <summary>
    /// TestNetpollBreak's call: netpollBreak on a goroutine, after netpollGenericInit. Returns the
    /// call's failure as type and message, or null when it returned. A Go throw here ends the process,
    /// so a red reading of this probe is the test host exiting with "fatal error: runtime:
    /// netpollBreak write failed".
    /// </summary>
    public static string? GoNetpollBreakProbe(int timeoutMs)
    {
        string? failure = "netpollBreak never returned";

        using ManualResetEventSlim returned = new(false);

        Goroutine.Start(() =>
        {
            try
            {
                netpollGenericInit();
                netpollBreak();
                failure = null;
            }
            catch (Exception ex)
            {
                failure = $"{ex.GetType().Name}: {ex.Message}";
            }

            returned.Set();
        });

        returned.Wait(timeoutMs);
        return failure;
    }
}
