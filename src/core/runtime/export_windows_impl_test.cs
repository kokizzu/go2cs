// export_windows_impl_test.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// Hand-owned bodies for runtime's export_windows_test.go, the WINDOWS test-file companion: the platform
// in the name makes testConversion join this file to the windows test project alone
// (testImplCompanionAppliesTo), as Go's `_windows` suffix scopes the file it stands beside, since
// ContextStub exists only in that platform's conversion. The converter drops both auto forms
// (manualConversionFuncs["runtime"], goosWindows).
//
// NewContextStub REFUSES BY NAME. Go's body fills a machine CONTEXT with its own caller's PC, SP and
// frame pointer, and runtime-seh_windows_test.go hands that context to RtlLookupFunctionEntry and
// RtlVirtualUnwind to walk Go frames through the .pdata table the Go linker emits for its machine code.
// go2cs emits no Go machine code: no Go PC, SP or frame exists to put in a context, and no function
// table describes one. The converted body reached sys.GetCallerPC's throwing stub, an infrastructure
// error that no disclosure can absorb, on all five SEH rows (TestSehLookupFunctionEntry, TestSehUnwind,
// TestSehUnwindPanic, TestSehUnwindDoublePanic, TestSehUnwindNilPointerPanic). This is the rows'
// representational point, so the refusal sits here rather than in GetCallerPC, whose other callers are
// ruled separately.
//
// NumberOfProcessors answers GetSystemInfo's dwNumberOfProcessors, as Go's body does. The converted body
// asked through stdcall1 -> asmcgocall, whose stub throws, so TestNumCPU read infrastructure-error. The
// hand-own asks kernel32 directly, over a blittable mirror of SYSTEM_INFO (runtime's converted
// systeminfo carries pointer fields, which are managed boxes here).
//
// Hand-owned (no export_windows_impl_test.go exists, so a reconvert never regenerates this file).

using System.Runtime.InteropServices;

namespace go;

partial class runtime_internal_test_package
{
    public static ж<ContextStub> NewContextStub() =>
        throw new PanicException("runtime: NewContextStub: go2cs emits no Go machine code, so there is no Go PC, SP or frame to build a CONTEXT from and no function table for SEH to look it up in");

    public static int32 NumberOfProcessors()
    {
        GetSystemInfo(out SystemInfo info);
        return (int32)info.NumberOfProcessors;
    }

    // SYSTEM_INFO (sysinfoapi.h), x64 layout.
    [StructLayout(LayoutKind.Sequential)]
    private struct SystemInfo
    {
        public ushort ProcessorArchitecture;
        public ushort Reserved;
        public uint PageSize;
        public nint MinimumApplicationAddress;
        public nint MaximumApplicationAddress;
        public nuint ActiveProcessorMask;
        public uint NumberOfProcessors;
        public uint ProcessorType;
        public uint AllocationGranularity;
        public ushort ProcessorLevel;
        public ushort ProcessorRevision;
    }

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SystemInfo info);
}
