// netpoll_epoll_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// The linux runtime poller's break, under a managed host that never creates the poller.
//
// Hand-owned (no netpoll_epoll_impl.go exists, so a reconvert never regenerates this file).

using System;
using System.Threading;
using go.golib;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
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
