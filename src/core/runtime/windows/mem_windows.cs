// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("runtime/mem_windows.go", "mem_windows.cs", "ABQ4wtaCgoIAChiCgoKClIKClILogoKCzIKCgoKClIKClIKkgraCuKam3sKCgoK4lKa4goKopg==")]

namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

internal static UntypedInt _MEM_COMMIT => 0x1000;
internal static UntypedInt _MEM_RESERVE => 0x2000;
internal static UntypedInt _MEM_DECOMMIT => 0x4000;
internal static UntypedInt _MEM_RELEASE => 0x8000;
internal static UntypedInt _PAGE_READWRITE => 0x0004;
internal static UntypedInt _PAGE_NOACCESS => 0x0001;
internal static UntypedInt _ERROR_NOT_ENOUGH_MEMORY => 8;
internal static UntypedInt _ERROR_COMMITMENT_LIMIT => 1455;

// Don't split the stack as this function may be invoked without a valid G,
// which prevents us from allocating more stack.
//
//go:nosplit
internal static @unsafe.Pointer sysAllocOS(uintptr n) {
    return (@unsafe.Pointer)stdcall4(_VirtualAlloc, 0, n, (uintptr)((uintptr)_MEM_COMMIT | (uintptr)_MEM_RESERVE), _PAGE_READWRITE);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string runtimeFailedToDecommitˢ = "runtime: failed to decommit pages"u8;

internal static void sysUnusedOS(@unsafe.Pointer v, uintptr n) {
    var r = stdcall3(_VirtualFree, (uintptr)v, n, _MEM_DECOMMIT);
    if (r != 0) {
        return;
    }
    // Decommit failed. Usual reason is that we've merged memory from two different
    // VirtualAlloc calls, and Windows will only let each VirtualFree handle pages from
    // a single VirtualAlloc. It is okay to specify a subset of the pages from a single alloc,
    // just not pages from multiple allocs. This is a rare case, arising only when we're
    // trying to give memory back to the operating system, which happens on a time
    // scale of minutes. It doesn't have to be terribly fast. Instead of extra bookkeeping
    // on all our VirtualAlloc calls, try freeing successively smaller pieces until
    // we manage to free something, and then repeat. This ends up being O(n log n)
    // in the worst case, but that's fast enough.
    while (n > 0) {
        var small = n;
        while (small >= 4096 && stdcall3(_VirtualFree, (uintptr)v, small, _MEM_DECOMMIT) == 0) {
            small /= 2;
            small &= unchecked((uintptr)~(uintptr)(4096 - 1));
        }
        if (small < 4096) {
            print((@string)"runtime: VirtualFree of "u8, small, (@string)" bytes failed with errno="u8, getlasterror(), (@string)"\n"u8);
            @throw(runtimeFailedToDecommitˢ);
        }
        v.Value = (uintptr)add(v, small);
        n -= small;
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string runtimeFailedToCommitˢ = "runtime: failed to commit pages"u8;

internal static void sysUsedOS(@unsafe.Pointer v, uintptr n) {
    var Δp = stdcall4(_VirtualAlloc, (uintptr)v, n, _MEM_COMMIT, _PAGE_READWRITE);
    if (Δp == (uintptr)v) {
        return;
    }
    // Commit failed. See SysUnused.
    // Hold on to n here so we can give back a better error message
    // for certain cases.
    var k = n;
    while (k > 0) {
        var small = k;
        while (small >= 4096 && stdcall4(_VirtualAlloc, (uintptr)v, small, _MEM_COMMIT, _PAGE_READWRITE) == 0) {
            small /= 2;
            small &= unchecked((uintptr)~(uintptr)(4096 - 1));
        }
        if (small < 4096) {
            var errno = getlasterror();
            var exprᴛ1 = errno;
            if (exprᴛ1 == _ERROR_NOT_ENOUGH_MEMORY || exprᴛ1 == _ERROR_COMMITMENT_LIMIT) {
                print((@string)"runtime: VirtualAlloc of "u8, n, (@string)" bytes failed with errno="u8, errno, (@string)"\n"u8);
                @throw(outOfMemoryˢ);
            }
            else { /* default: */
                print((@string)"runtime: VirtualAlloc of "u8, small, (@string)" bytes failed with errno="u8, errno, (@string)"\n"u8);
                @throw(runtimeFailedToCommitˢ);
            }

        }
        v.Value = (uintptr)add(v, small);
        k -= small;
    }
}

internal static void sysHugePageOS(@unsafe.Pointer v, uintptr n) {
}

internal static void sysNoHugePageOS(@unsafe.Pointer v, uintptr n) {
}

internal static void sysHugePageCollapseOS(@unsafe.Pointer v, uintptr n) {
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string runtimeFailedToReleaseˢ = "runtime: failed to release pages"u8;

// Don't split the stack as this function may be invoked without a valid G,
// which prevents us from allocating more stack.
//
//go:nosplit
internal static void sysFreeOS(@unsafe.Pointer v, uintptr n) {
    var r = stdcall3(_VirtualFree, (uintptr)v, 0, _MEM_RELEASE);
    if (r == 0) {
        print((@string)"runtime: VirtualFree of "u8, n, (@string)" bytes failed with errno="u8, getlasterror(), (@string)"\n"u8);
        @throw(runtimeFailedToReleaseˢ);
    }
}

internal static void sysFaultOS(@unsafe.Pointer v, uintptr n) {
    // SysUnused makes the memory inaccessible and prevents its reuse
    sysUnusedOS(v, n);
}

internal static @unsafe.Pointer sysReserveOS(@unsafe.Pointer v, uintptr n) {
    // v is just a hint.
    // First try at v.
    // This will fail if any of [v, v+n) is already reserved.
    v.Value = (@unsafe.Pointer)stdcall4(_VirtualAlloc, (uintptr)v, n, _MEM_RESERVE, _PAGE_READWRITE);
    if (v != nil) {
        return v;
    }
    // Next let the kernel choose the address.
    return (@unsafe.Pointer)stdcall4(_VirtualAlloc, 0, n, _MEM_RESERVE, _PAGE_READWRITE);
}

internal static void sysMapOS(@unsafe.Pointer v, uintptr n) {
}

} // end runtime_package
