using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// runtime.sysAllocOS / sysFreeOS on Windows (src/core/runtime/windows/mem_windows_impl.cs). The converted
// bodies reached VirtualAlloc/VirtualFree through stdcall -> asmcgocall and threw; one throw under
// persistentalloc1 held globalAlloc's lock and echoed as 70 abandoned-lock panics on the runtime row.
// Windows-only by construction: the bodies and seams live in runtime's windows/ folder.
[TestClass]
public class RuntimeSysAllocWindowsTests
{
    [TestMethod]
    public void SysAllocOSReturnsZeroedGranularityAlignedWritablePagesAndSysFreeOSReleasesThem()
    {
        const nuint n = 64 * 1024;
        nuint p = runtime_package.GoSysAllocOS(n);

        Assert.AreNotEqual((nuint)0, p, "sysAllocOS returned nil for 64 KiB");

        try
        {
            Assert.AreEqual((nuint)0, p % (64 * 1024), "VirtualAlloc's allocation granularity: a 64 KiB-aligned base");

            byte[] probe = new byte[(int)n];
            Marshal.Copy((nint)p, probe, 0, probe.Length);
            Assert.IsTrue(Array.TrueForAll(probe, b => b == 0), "sysAllocOS pages are zeroed (Go's contract for fresh OS memory)");

            Marshal.WriteByte((nint)p, 0x5a);
            Marshal.WriteByte((nint)p + (nint)(n - 1), 0xa5);
            Assert.AreEqual((byte)0x5a, Marshal.ReadByte((nint)p));
            Assert.AreEqual((byte)0xa5, Marshal.ReadByte((nint)p + (nint)(n - 1)));
        }
        finally
        {
            runtime_package.GoSysFreeOS(p, n);   // throws by Go's own message if VirtualFree refused
        }
    }
}
