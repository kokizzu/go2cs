// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

internal static readonly UntypedInt _MEM_COMMIT = /* 0x1000 */ 4096;
internal static readonly UntypedInt _MEM_RESERVE = /* 0x2000 */ 8192;
internal static readonly UntypedInt _MEM_DECOMMIT = /* 0x4000 */ 16384;
internal static readonly UntypedInt _MEM_RELEASE = /* 0x8000 */ 32768;
internal static readonly UntypedInt _PAGE_READWRITE = /* 0x0004 */ 4;
internal static readonly UntypedInt _PAGE_NOACCESS = /* 0x0001 */ 1;
internal static readonly UntypedInt _ERROR_NOT_ENOUGH_MEMORY = 8;
internal static readonly UntypedInt _ERROR_COMMITMENT_LIMIT = 1455;

// Don't split the stack as this function may be invoked without a valid G,
// which prevents us from allocating more stack.
//
//go:nosplit
internal static @unsafe.Pointer sysAllocOS(uintptr n) {
    return ((@unsafe.Pointer)stdcall4(_VirtualAlloc, 0, n, (uintptr)(_MEM_COMMIT | _MEM_RESERVE), _PAGE_READWRITE));
}

internal static void sysUnusedOS(@unsafe.Pointer v, uintptr n) {
    var r = stdcall3(_VirtualFree, ((uintptr)v), n, _MEM_DECOMMIT);
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
        while (small >= 4096 && stdcall3(_VirtualFree, ((uintptr)v), small, _MEM_DECOMMIT) == 0) {
            small /= 2;
            small &= ~(uintptr)(4096 - 1);
        }
        if (small < 4096) {
            print("runtime: VirtualFree of ", small, " bytes failed with errno=", getlasterror(), "\n");
            @throw("runtime: failed to decommit pages"u8);
        }
        v = (uintptr)add(v.val, small);
        n -= small;
    }
}

internal static void sysUsedOS(@unsafe.Pointer v, uintptr n) {
    var Δp = stdcall4(_VirtualAlloc, ((uintptr)v), n, _MEM_COMMIT, _PAGE_READWRITE);
    if (Δp == ((uintptr)v)) {
        return;
    }
    // Commit failed. See SysUnused.
    // Hold on to n here so we can give back a better error message
    // for certain cases.
    var k = n;
    while (k > 0) {
        var small = k;
        while (small >= 4096 && stdcall4(_VirtualAlloc, ((uintptr)v), small, _MEM_COMMIT, _PAGE_READWRITE) == 0) {
            small /= 2;
            small &= ~(uintptr)(4096 - 1);
        }
        if (small < 4096) {
            var errno = getlasterror();
            var exprᴛ1 = errno;
            if (exprᴛ1 == _ERROR_NOT_ENOUGH_MEMORY || exprᴛ1 == _ERROR_COMMITMENT_LIMIT) {
                print("runtime: VirtualAlloc of ", n, " bytes failed with errno=", errno, "\n");
                @throw("out of memory"u8);
            }
            else { /* default: */
                print("runtime: VirtualAlloc of ", small, " bytes failed with errno=", errno, "\n");
                @throw("runtime: failed to commit pages"u8);
            }

        }
        v = (uintptr)add(v.val, small);
        k -= small;
    }
}

internal static void sysHugePageOS(@unsafe.Pointer v, uintptr n) {
}

internal static void sysNoHugePageOS(@unsafe.Pointer v, uintptr n) {
}

internal static void sysHugePageCollapseOS(@unsafe.Pointer v, uintptr n) {
}

// Don't split the stack as this function may be invoked without a valid G,
// which prevents us from allocating more stack.
//
//go:nosplit
internal static void sysFreeOS(@unsafe.Pointer v, uintptr n) {
    var r = stdcall3(_VirtualFree, ((uintptr)v), 0, _MEM_RELEASE);
    if (r == 0) {
        print("runtime: VirtualFree of ", n, " bytes failed with errno=", getlasterror(), "\n");
        @throw("runtime: failed to release pages"u8);
    }
}

internal static void sysFaultOS(@unsafe.Pointer v, uintptr n) {
    // SysUnused makes the memory inaccessible and prevents its reuse
    sysUnusedOS(v.val, n);
}

internal static @unsafe.Pointer sysReserveOS(@unsafe.Pointer v, uintptr n) {
    // v is just a hint.
    // First try at v.
    // This will fail if any of [v, v+n) is already reserved.
    v = ((@unsafe.Pointer)stdcall4(_VirtualAlloc, ((uintptr)v), n, _MEM_RESERVE, _PAGE_READWRITE));
    if (v != nil) {
        return Ꮡv;
    }
    // Next let the kernel choose the address.
    return ((@unsafe.Pointer)stdcall4(_VirtualAlloc, 0, n, _MEM_RESERVE, _PAGE_READWRITE));
}

internal static void sysMapOS(@unsafe.Pointer v, uintptr n) {
}

} // end runtime_package
