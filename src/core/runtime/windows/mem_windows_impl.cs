// mem_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// runtime.sysAllocOS / sysFreeOS on Windows, hand-owned (COORD ruling 2026-09-22;
// manualConversionFuncs["runtime"], goosWindows).
//
// WHY. Go's bodies call VirtualAlloc / VirtualFree through stdcall4 / stdcall3 -> asmcgocall, which has
// no managed body, so every sysAlloc threw NotImplementedException. On the runtime row that one root
// (first reached by TestAddrRangesAdd: NewAddrRanges -> addrRanges.init -> persistentalloc ->
// persistentalloc1 -> sysAlloc) died holding globalAlloc's lock, and 70 later lockers died on the
// abandoned-lock panic that quotes it.
//
// WHAT. The SAME kernel calls, made directly -- the linux flavour's precedent (mem_linux_impl.cs calls
// libc mmap/munmap rather than a managed allocator). VirtualAlloc(nil, n, MEM_COMMIT|MEM_RESERVE,
// PAGE_READWRITE) is Go's own request: committed, ZEROED, read-write pages aligned to the 64 KB
// allocation granularity (what persistentalloc1's page-aligned chunk arithmetic and sysAlloc's callers
// assume), or 0 on failure, which Go reports as nil. sysFreeOS is VirtualFree(v, 0, MEM_RELEASE),
// releasing the whole allocation exactly as Go does, and throws Go's own message on failure. A managed
// allocator (NativeMemory) would satisfy the alignment only by over-allocating and could not release a
// region some other VirtualAlloc produced; the kernel call pairs with every Windows region by
// construction. The pages are NATIVE and outside the CLR heap.
//
// Hand-owned: there is no mem_windows_impl.go, so a -stdlib reconvert never regenerates this file.

using System.Runtime.InteropServices;
using @unsafe = go.unsafe_package;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern nint VirtualAlloc(nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(nint lpAddress, nuint dwSize, uint dwFreeType);

    internal static @unsafe.Pointer sysAllocOS(uintptr n)
    {
        nint p = VirtualAlloc(0, (nuint)n, (uint)(_MEM_COMMIT | _MEM_RESERVE), (uint)_PAGE_READWRITE);

        return p == 0 ? nil : new @unsafe.Pointer((nuint)p);
    }

    internal static void sysFreeOS(@unsafe.Pointer v, uintptr n)
    {
        if (!VirtualFree((nint)(nuint)(uintptr)v, 0, (uint)_MEM_RELEASE))
        {
            print((@string)"runtime: VirtualFree of "u8, n, (@string)" bytes failed with errno="u8, (uint32)Marshal.GetLastPInvokeError(), (@string)"\n"u8);
            @throw((@string)"runtime: failed to release pages"u8);
        }
    }

    // TEST SEAMS (pinner_impl.cs's pattern; GolibTests is not in the InternalsVisibleTo grant).
    public static nuint GoSysAllocOS(nuint n) => ((uintptr)sysAllocOS(n)).Value;

    public static void GoSysFreeOS(nuint v, nuint n) => sysFreeOS(new @unsafe.Pointer(v), n);
}
