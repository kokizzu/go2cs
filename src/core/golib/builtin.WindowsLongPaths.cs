// builtin.WindowsLongPaths.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;
using System.Runtime.InteropServices;

namespace go;

// ---------------------------------------------------------------------------------------------
// WINDOWS LONG-PATH AWARENESS - the one piece of process setup that makes a converted program
// answer a path longer than MAX_PATH the way the Go binary does.
//
// WHY THIS EXISTS
//   Every Go Windows binary opts its own process into long-path handling at startup: osinit calls
//   initLongPathSupport (runtime/os_windows.go), which checks for Windows 10.0.15063 or later and
//   then sets the undocumented IsLongPathAwareProcess flag in the PEB's bit field. ntdll's path
//   canonicalizer consults that flag, so with it set a plain (un-prefixed) path longer than
//   MAX_PATH reaches the kernel intact.
//
//   A converted program is an ordinary .NET process, and .NET does not do this. The divergence is
//   measured, not theoretical: at a 434-character path, Go's os.Chdir succeeds where .NET's
//   Directory.SetCurrentDirectory fails with ERROR_FILENAME_EXCED_RANGE (206, 0x800700CE). That is
//   the failure that skipped syscall's TestGetwd_DoesNotPanicWhenPathIsLong, and it is a whole
//   class of Windows behavior rather than one test row.
//
// WHY NOT AN APPLICATION MANIFEST
//   longPathAware in an app manifest reaches the same PEB flag, but Windows honors it only when
//   the machine-wide policy HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled is
//   also 1 - so a manifested converted binary would still diverge from the Go binary on a default
//   install, where that value is 0. Go asks for neither the manifest nor the policy. Doing what Go
//   does is both the smaller change (no per-project artifact, nothing in the emitted .csproj, no
//   churn through the banked test projects) and the only one that is unconditional the way Go's is.
//
// WHY IT IS DEFENSIVE
//   This is a parity measure, not a prerequisite: everything that worked before still works with
//   the flag clear, because Go's own os.fixLongPath fallback prefixes \\?\ explicitly and the
//   converted corpus carries that fallback. So a missing export, a refused write or an unusual
//   host is swallowed rather than allowed to take down module initialization.
//
// WHAT IS DELIBERATELY NOT DONE
//   Go's initLongPathSupport also sets internal/syscall/windows.CanUseLongPaths, which makes
//   os.fixLongPath stop adding the \\?\ prefix. That flag lives in a converted package golib
//   cannot reference - golib is the root of the dependency graph - and leaving it false is the
//   conservative side: the extended-prefix form still works with the PEB flag set, so the only
//   difference is which spelling reaches the kernel. Wiring it would need a hand-owned companion
//   in internal/syscall/windows and has no measured need today.
// ---------------------------------------------------------------------------------------------
public static partial class builtin
{
    // Offset of the PEB's BitField byte and the IsLongPathAwareProcess bit inside it - the same
    // two constants runtime/os_windows.go uses. The offset is identical for 32- and 64-bit PEBs:
    // InheritedAddressSpace, ReadImageFileExecOptions and BeingDebugged precede it, one byte each.
    private const int PebBitFieldOffset = 3;
    private const byte IsLongPathAwareProcess = 0x80;

    [LibraryImport("ntdll.dll", EntryPoint = "RtlGetCurrentPeb")]
    private static partial IntPtr RtlGetCurrentPeb();

    // Called from InitializeGoLib, which is golib's analogue of Go's runtime.osinit.
    private static void InitializeWindowsLongPaths()
    {
        // Go returns early below 10.0.15063 because the flag does nothing there; OperatingSystem
        // answers the same question without an RtlGetVersion call of our own.
        if (!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 15063))
            return;

        try
        {
            IntPtr peb = RtlGetCurrentPeb();
            byte bitField = Marshal.ReadByte(peb, PebBitFieldOffset);
            Marshal.WriteByte(peb, PebBitFieldOffset, (byte)(bitField | IsLongPathAwareProcess));
        }
        catch
        {
            // See "WHY IT IS DEFENSIVE" above - long paths simply keep needing the \\?\ prefix.
        }
    }
}