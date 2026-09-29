// netpoll_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// The windows runtime poller's break, under a managed host that never creates the poller.
//
// netpollBreak posts one completion packet to iocphandle, the I/O completion port netpollinit creates.
// netpollGenericInit is a no-op here (netpoll_impl.cs: there is no runtime-side poller and nothing to
// drain it), so the port never exists. The converted body's PostQueuedCompletionStatus reached
// stdcall4 -> asmcgocall, whose stub throws NotImplementedException: proc_test.go's TestNetpollBreak
// read INFRASTRUCTURE-ERROR on the windows runtime row, which no disclosure can absorb (the manifest's
// fail arm admits Go pass / C# fail only). Had asmcgocall a body, the post to the invalid handle would
// fail and the converted body would take Go's throw, a fatal that ends the test host, which is the
// path linux met once write1 had a body (linux/netpoll_epoll_impl.cs).
//
// netpollBreak therefore refuses by name, before any post, exactly as linux's does, and with the same
// leading words, since the cause is the same: the runtime poller the break would interrupt does not
// exist. Only the parenthetical names each flavour's missing object. TestNetpollBreak keeps its one loud,
// locatable failure, now a Go panic, and the host lives.
//
// Hand-owned (no netpoll_windows_impl.go exists, so a reconvert never regenerates this file). The
// converter drops the auto form (manualConversionFuncs["runtime"], "netpollBreak" scoped to linux and
// windows in go2cs/manualTypeOperations.go).

using go.golib;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    internal static void netpollBreak() =>
        throw new PanicException("runtime: netpollBreak: the managed host has no runtime poller to break (netpollGenericInit creates no I/O completion port; see netpoll_impl.cs)");
}
