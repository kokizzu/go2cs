using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using syscall = go.syscall_package;

namespace GolibTests;

// The value-level guard for syscall.NetUserGetInfo, the sixth member of the ptrout class
// (a Go `**T` OUT-PARAMETER the kernel writes a raw address into).
//
// WHY THIS EXISTS SEPARATELY FROM os/user. This member's consumer is os/user, and the class's
// standing bar is that a VALUE-LEVEL guard proves each member -- "it no longer returns nil" being
// exactly the evidence this class's history says not to trust. os/user cannot be that guard: it is
// roster-excluded E2 because Go's own TestGroupIds fails, and on a domain-joined host that cannot
// reach its DC that failure is structural, not a flap ("The specified domain either does not exist
// or could not be contacted"). A proof that needs a reachable domain controller is a proof that
// flaps with the network, so the guard is built to need neither.
//
// HOW IT AVOIDS THE DOMAIN. NetUserGetInfo with a NULL servername queries the LOCAL machine, which
// works offline and unprivileged, and the well-known built-in accounts carry values identical on
// every Windows host -- the same trick PointerOutParameter uses with well-known SIDs:
//
//     Administrator   Priv = USER_PRIV_ADMIN (2),  PrimaryGroupID = 513
//     Guest           Priv = USER_PRIV_GUEST (0),  PrimaryGroupID = 513
//
// and the account NAME read back out of the record must equal the name queried -- a ROUND TRIP, so
// a wrapper that hands back the wrong address is caught by the VALUE and not merely by a crash.
//
// WHAT WOULD FAIL WITHOUT THE FIX. Before the wrapper was taken, `Ꮡbuf`'s ж<ж<byte>> slot could
// lend the kernel no eight-byte cell, so the operator answered 0, Windows read that as "no output
// wanted", the call SUCCEEDED, and the caller read back the nil it started with. That is invisible
// to a liveness check and is precisely why every assertion here is a value.
//
// Windows-only by nature (netapi32), and gated on the HOST rather than on which declaration was
// compiled. That distinction is load-bearing: a linux-flavor guard run on a Windows host sails past
// a declaration-only check and then dies in a module initializer, which reads like a regression in
// whatever is under test. A non-Windows run must read NOT MEASURED instead.
[TestClass]
public class WindowsNetUserInfoTests
{
    // Field-for-field USER_INFO_4 / USER_INFO_10 from lmaccess.h, matching the mirrors in
    // os/user's lookup_windows_impl.cs. Measured against Go on this host: sizeof(USER_INFO_4) is
    // 192 bytes and PrimaryGroupID sits at offset 160.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeUserInfo4
    {
        public ushort* Name;
        public ushort* Password;
        public uint PasswordAge;
        public uint Priv;
        public ushort* HomeDir;
        public ushort* Comment;
        public uint Flags;
        public ushort* ScriptPath;
        public uint AuthFlags;
        public ushort* FullName;
        public ushort* UsrComment;
        public ushort* Parms;
        public ushort* Workstations;
        public uint LastLogon;
        public uint LastLogoff;
        public uint AcctExpires;
        public uint MaxStorage;
        public uint UnitsPerWeek;
        public byte* LogonHours;
        public uint BadPwCount;
        public uint NumLogons;
        public ushort* LogonServer;
        public uint CountryCode;
        public uint CodePage;
        public void* UserSid;
        public uint PrimaryGroupID;
        public ushort* Profile;
        public ushort* HomeDirDrive;
        public uint PasswordExpired;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeUserInfo10
    {
        public ushort* Name;
        public ushort* Comment;
        public ushort* UsrComment;
        public ushort* FullName;
    }

    // The INDEPENDENT oracle: netapi32 reached directly, owing nothing to the converted surface.
    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "NetUserGetInfo")]
    private static extern uint NetUserGetInfoDirect(string? servername, string username, uint level, out IntPtr buf);

    [DllImport("netapi32.dll", EntryPoint = "NetApiBufferFree")]
    private static extern uint NetApiBufferFreeDirect(IntPtr buf);

    private static unsafe string NativeString(ushort* p)
    {
        return p == null ? "" : new string((char*)p);
    }

    private static bool OnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // The mirrors are only as good as their LAYOUT, and a silently wrong offset would read a
    // neighbouring field rather than fault. These are the numbers Go reports for the same record.
    [TestMethod]
    public unsafe void NativeMirrorsMatchTheDocumentedRecordLayout()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("netapi32 is the windows flavor");
            return;
        }

        Assert.AreEqual(192, sizeof(NativeUserInfo4), "USER_INFO_4 is 192 bytes on x64");
        Assert.AreEqual(160, (int)Marshal.OffsetOf<NativeUserInfo4>(nameof(NativeUserInfo4.PrimaryGroupID)),
            "usri4_primary_group_id sits at offset 160");
        Assert.AreEqual(32, sizeof(NativeUserInfo10), "USER_INFO_10 is four pointers");
    }

    // The wrapper must publish the kernel's buffer into the caller's pointer -- the defect this
    // member was taken for is that it published nothing while reporting success.
    [TestMethod]
    public unsafe void WrapperPublishesTheKernelsBuffer()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("netapi32 is the windows flavor");
            return;
        }

        var (u, uerr) = syscall.UTF16PtrFromString("Administrator");
        Assert.IsNull(uerr, "UTF16PtrFromString must succeed");

        ref var p = ref heap<ж<byte>>(out var Ꮡp);
        var e = syscall.NetUserGetInfo(nil, u, 10, Ꮡp);
        Assert.IsNull(e, $"NetUserGetInfo(level 10) must succeed, got: {e?.Error().ToString()}");

        try
        {
            nuint published = (nuint)(uintptr)p;
            Assert.AreNotEqual((nuint)0, published,
                "the wrapper published NOTHING -- this is the exact defect: success reported, caller's pointer left nil");

            NativeUserInfo10* info = (NativeUserInfo10*)published;
            Assert.AreEqual("Administrator", NativeString(info->Name),
                "the account name read back through the published buffer must round-trip");
        }
        finally
        {
            syscall.NetApiBufferFree(p);
        }
    }

    // Level 4 is the record os/user's lookupUserPrimaryGroup reads, and PrimaryGroupID is the one
    // field it takes. Both are compared against the independent P/Invoke, so this fails if the
    // converted path and netapi32 ever disagree -- including on the field's OFFSET.
    [TestMethod]
    public unsafe void ConvertedPathAgreesWithNetapi32Directly()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("netapi32 is the windows flavor");
            return;
        }

        foreach (var (account, expectedPriv) in new[] { ("Administrator", 2u), ("Guest", 0u) })
        {
            // ---- the ORACLE: netapi32, reached directly ----
            uint rc = NetUserGetInfoDirect(null, account, 4, out IntPtr direct);
            Assert.AreEqual(0u, rc, $"the oracle itself must succeed for {account}");

            string oracleName;
            uint oraclePriv, oracleGid;

            try
            {
                NativeUserInfo4* o = (NativeUserInfo4*)direct;
                oracleName = NativeString(o->Name);
                oraclePriv = o->Priv;
                oracleGid = o->PrimaryGroupID;
            }
            finally
            {
                NetApiBufferFreeDirect(direct);
            }

            // Well-known and identical on every Windows host, so a wrong offset is caught by the
            // VALUE rather than only by disagreement between two equally-wrong reads.
            Assert.AreEqual(account, oracleName, $"oracle name round-trip for {account}");
            Assert.AreEqual(expectedPriv, oraclePriv, $"{account} carries the well-known privilege level");
            Assert.AreEqual(513u, oracleGid, $"{account} carries the well-known primary group RID");

            // ---- the CONVERTED path ----
            var (u, uerr) = syscall.UTF16PtrFromString(account);
            Assert.IsNull(uerr, "UTF16PtrFromString must succeed");

            ref var p = ref heap<ж<byte>>(out var Ꮡp);
            var e = syscall.NetUserGetInfo(nil, u, 4, Ꮡp);
            Assert.IsNull(e, $"NetUserGetInfo(level 4) must succeed for {account}, got: {e?.Error().ToString()}");

            try
            {
                nuint published = (nuint)(uintptr)p;
                Assert.AreNotEqual((nuint)0, published, $"the wrapper published nothing for {account}");

                NativeUserInfo4* c = (NativeUserInfo4*)published;

                Assert.AreEqual(oracleName, NativeString(c->Name), $"name must agree with netapi32 for {account}");
                Assert.AreEqual(oraclePriv, c->Priv, $"Priv must agree with netapi32 for {account}");
                Assert.AreEqual(oracleGid, c->PrimaryGroupID, $"PrimaryGroupID must agree with netapi32 for {account}");
            }
            finally
            {
                syscall.NetApiBufferFree(p);
            }
        }
    }
}
