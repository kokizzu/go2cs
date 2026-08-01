// zsyscall_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementation of the one generated syscall wrapper whose STRUCT cannot cross the
// managed/native boundary by address.
//
// Go's GetTimeZoneInformation is a plain mksyscall wrapper:
//
//     r0, _, e1 := Syscall(procGetTimeZoneInformation.Addr(), 1, uintptr(unsafe.Pointer(tzi)), 0, 0)
//
// which works in Go because TIME_ZONE_INFORMATION is 172 bytes laid out exactly as Windows wants,
// with the two WCHAR[32] name buffers INLINE. The converted Timezoneinformation cannot be: its
// name fields are golib `array<uint16>` MANAGED REFERENCES, so the struct is roughly 64 bytes and
// its field offsets bear no relation to the native ones. Handing the kernel that address makes it
// write 172 bytes of native data over a smaller managed object — corrupting the heap past its end
// and leaving fabricated object references where the name arrays belong. The very next statement
// in Go's own zoneinfo_windows.go, `syscall.UTF16ToString(z.StandardName[:])`, then dies with an
// ACCESS_VIOLATION inside slice<ushort>..ctor. Because time.initLocal is what reaches this, EVERY
// converted program that calls Weekday / Location / Local on Windows crashed.
//
// This is the same seam as exec_windows.go's StartProcess (_STARTUPINFOEXW) and takes the same
// remedy, described in docs/Baseline-vs-FullConversion.md "Child-process creation": a blittable
// mirror of the native layout, a direct P/Invoke, and an explicit field-for-field copy back into
// the converted struct. Note what does NOT need this: every other declaration in
// zsyscall_windows.go passes scalars and handles, which convert faithfully — only a struct
// carrying `ж<T>` or `array<T>` fields breaks.

using System;
using System.Runtime.InteropServices;

// Hand-owned (no zsyscall_windows_impl.go exists, so a reconvert never regenerates this file);
// marked for consistency with the other hand-owned operational files in this package.
[module: go.GoManualConversion]

namespace go;

partial class syscall_package
{
    // TIME_ZONE_INFORMATION exactly as Windows lays it out: 172 bytes, the two name buffers inline
    // as WCHAR[32]. `fixed` keeps them inline (a C# array field would be another reference, which
    // is the whole bug); the struct is therefore blittable and needs no marshalling layer.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeTimeZoneInformation
    {
        public int32 Bias;
        public fixed uint16 StandardName[32];
        public NativeSystemTime StandardDate;
        public int32 StandardBias;
        public fixed uint16 DaylightName[32];
        public NativeSystemTime DaylightDate;
        public int32 DaylightBias;
    }

    // SYSTEMTIME. Blittable already — the converted Systemtime has the same eight uint16 fields in
    // the same order — but mirrored here so the enclosing layout is stated in one place.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSystemTime
    {
        public uint16 Year;
        public uint16 Month;
        public uint16 DayOfWeek;
        public uint16 Day;
        public uint16 Hour;
        public uint16 Minute;
        public uint16 Second;
        public uint16 Milliseconds;
    }

    [DllImport("kernel32.dll", EntryPoint = "GetTimeZoneInformation", SetLastError = true)]
    private static extern unsafe uint32 win32GetTimeZoneInformation(NativeTimeZoneInformation* tzi);

    // GetTimeZoneInformation is the native transcription of the generated wrapper — see the file
    // header for why it cannot be a literal conversion. Return values follow the Go original
    // exactly: rc is the raw TIME_ZONE_ID_* result, and only TIME_ZONE_ID_INVALID (0xffffffff)
    // produces an error.
    public static unsafe (uint32 rc, error err) GetTimeZoneInformation(ж<Timezoneinformation> Ꮡtzi) {
        NativeTimeZoneInformation native;
        uint32 rc = win32GetTimeZoneInformation(&native);

        if (rc == 0xffffffffU) {
            return (rc, errnoErr((Errno)(uint32)Marshal.GetLastSystemError()));
        }

        ref var tzi = ref Ꮡtzi.Value;

        tzi.Bias = native.Bias;
        tzi.StandardDate = toSystemtime(native.StandardDate);
        tzi.StandardBias = native.StandardBias;
        tzi.DaylightDate = toSystemtime(native.DaylightDate);
        tzi.DaylightBias = native.DaylightBias;

        // The name buffers are copied whole, NULs included: Go reads them as
        // `UTF16ToString(z.StandardName[:])`, which stops at the first NUL, and Windows pads the
        // remainder with NULs. Copying only up to the terminator would leave stale runes behind it
        // when a Timezoneinformation is reused.
        copyNativeName(native.StandardName, ref tzi.StandardName);
        copyNativeName(native.DaylightName, ref tzi.DaylightName);

        return (rc, default!);
    }

    private static Systemtime toSystemtime(NativeSystemTime value) {
        return new Systemtime{
            Year = value.Year,
            Month = value.Month,
            DayOfWeek = value.DayOfWeek,
            Day = value.Day,
            Hour = value.Hour,
            Minute = value.Minute,
            Second = value.Second,
            Milliseconds = value.Milliseconds
        };
    }

    // Copies a native WCHAR[32] buffer into the converted struct's `array<uint16>` field. The
    // destination is (re)allocated when it is not already 32 elements, so a Timezoneinformation
    // that reached here as `default` — its field initializer never having run — is filled rather
    // than dereferenced through a null backing.
    private static unsafe void copyNativeName(uint16* source, ref array<uint16> destination) {
        const nint nameLength = 32;

        if (destination.Length != nameLength) {
            destination = new array<uint16>(nameLength);
        }

        for (nint i = 0; i < nameLength; i++) {
            destination[i] = source[i];
        }
    }
}
