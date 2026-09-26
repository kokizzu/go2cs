// cpuprof_linux_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// The guard's view of the CPU-profiler setters on LINUX (RuntimeCPUProfilerTests). The setters
// themselves are still the converter's emission in os_linux.cs in this commit.
//
// Hand-owned (no cpuprof_linux_impl.go exists, so a reconvert never regenerates this file).
[module: go.GoManualConversion]

namespace go;

partial class runtime_package {

// ---- the guard's view (RuntimeCPUProfilerTests) ----

/// <summary>What the calling goroutine's m records as its profiling rate.</summary>
public static int GoThreadProfileHz => (int)(~(~getg()).m).profilehz;

} // end runtime_package
