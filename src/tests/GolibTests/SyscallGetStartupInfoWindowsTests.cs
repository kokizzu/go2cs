using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// syscall.GetStartupInfo fills Go's StartupInfo from kernel32's GetStartupInfoW. The converted record
// is reference-bearing -- Desktop, Title and two reserved slots are pointer fields, each a `ж<T>` -- so
// the generated wrapper's `(uintptr)Ꮡsi` is a managed pointer token, and the syscall boundary refuses
// it by name ("argument 0 is a managed pointer token, not an address"). That refusal kept syscall's
// TestGetStartupInfo, and the banked row, red at 1.24.13 (measured 2026-09-22 at 3469154a95).
// zsyscall_windows_startupinfo_impl.cs answers it through a blittable STARTUPINFOW mirror.
//
// The ORACLE is the same kernel call made directly here: every field the wrapper reports must be the
// field kernel32 wrote, the pointer fields as native-backed boxes over the same addresses.
[TestClass]
public class SyscallGetStartupInfoWindowsTests
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFOW
    {
        public uint cb;
        public nint lpReserved;
        public nint lpDesktop;
        public nint lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public nint lpReserved2;
        public nint hStdInput, hStdOutput, hStdError;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern void GetStartupInfoW(out STARTUPINFOW info);

    private static nuint AddressOf<T>(ж<T> pointer) => pointer == nil ? 0 : ((uintptr)pointer).Value;

    [TestMethod]
    public void GetStartupInfoFillsEveryFieldKernel32Writes()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("GetStartupInfoW is a Windows API.");

        var si = new syscall_package.StartupInfo();
        var Ꮡsi = new StandardBox<syscall_package.StartupInfo>(si);

        error err = syscall_package.GetStartupInfo(Ꮡsi);
        Assert.IsTrue(err == nil, "Go's GetStartupInfo always returns nil (go.dev/issue/31316)");

        GetStartupInfoW(out STARTUPINFOW want);
        ref syscall_package.StartupInfo got = ref Ꮡsi.Value;

        Assert.AreEqual((uint)Marshal.SizeOf<STARTUPINFOW>(), want.cb, "the oracle's own layout is the kernel's");
        Assert.AreEqual(want.cb, (uint)got.Cb, "cb: kernel32 writes the record's own size");
        Assert.AreEqual(want.dwX, (uint)got.X);
        Assert.AreEqual(want.dwY, (uint)got.Y);
        Assert.AreEqual(want.dwXSize, (uint)got.XSize);
        Assert.AreEqual(want.dwYSize, (uint)got.YSize);
        Assert.AreEqual(want.dwXCountChars, (uint)got.XCountChars);
        Assert.AreEqual(want.dwYCountChars, (uint)got.YCountChars);
        Assert.AreEqual(want.dwFillAttribute, (uint)got.FillAttribute);
        Assert.AreEqual(want.dwFlags, (uint)got.Flags);
        Assert.AreEqual(want.wShowWindow, (ushort)got.ShowWindow);
        Assert.AreEqual((nuint)want.hStdInput, ((uintptr)got.StdInput).Value, "hStdInput");
        Assert.AreEqual((nuint)want.hStdOutput, ((uintptr)got.StdOutput).Value, "hStdOutput");
        Assert.AreEqual((nuint)want.hStdError, ((uintptr)got.StdErr).Value, "hStdError");

        // The pointer fields: the SAME native addresses, nil exactly where kernel32 wrote NULL.
        Assert.AreEqual((nuint)want.lpDesktop, AddressOf(got.Desktop), "lpDesktop");
        Assert.AreEqual((nuint)want.lpTitle, AddressOf(got.Title), "lpTitle");

        // Stated, so a run where both were NULL (and the read-through arms below never executed) is
        // visible in the test output rather than passing silently.
        Console.WriteLine($"lpDesktop=0x{want.lpDesktop:x} lpTitle=0x{want.lpTitle:x}");

        // And they READ as the strings they point at -- the first UTF-16 unit through the Go pointer
        // is the first character of the kernel's string.
        if (want.lpDesktop != 0)
            Assert.AreEqual((ushort)Marshal.ReadInt16(want.lpDesktop), (ushort)got.Desktop.Value, "Desktop reads through");

        if (want.lpTitle != 0)
            Assert.AreEqual((ushort)Marshal.ReadInt16(want.lpTitle), (ushort)got.Title.Value, "Title reads through");
    }
}
