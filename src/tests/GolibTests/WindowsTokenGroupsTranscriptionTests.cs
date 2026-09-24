using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using syscall = go.syscall_package;
using winint = go.@internal.syscall.windows_package;

// AllGroups is an EXTENSION method: the hand-own declares `AllGroups(this ref TOKEN_GROUPS)` and the
// generator emits the pointer-receiver overload `AllGroups(this ж<TOKEN_GROUPS>)` beside it, both in
// the package's static class. A TYPE ALIAS names the type and binds its static calls
// (winint.transcribeTokenGroups does), but it does NOT bring that class's extension methods into
// scope -- so `Ꮡgroups.AllGroups()` below finds nothing without this line. Converted os/user, which
// calls `groups.AllGroups()` and compiles, imports the namespace exactly this way
// (os/user/windows/lookup_windows.cs:13). Importing it rather than spelling the call
// `winint.AllGroups(Ꮡgroups)` keeps the guard driving the member the way a converted consumer
// drives it, which is the shape worth guarding.
using go.@internal.syscall;

namespace GolibTests;

// The value-level guard for internal/syscall/windows' TOKEN_GROUPS transcription --- the
// token-information members of the native-boundary class, cured in that package's
// security_windows_impl.cs.
//
// WHAT WENT WRONG, and why a VALUE is the only evidence worth having. The converted
// `GetTokenGroups` cast a kernel-filled byte buffer to a managed TOKEN_GROUPS whose Groups field is
// an `array<SID_AND_ATTRIBUTES>` MANAGED REFERENCE, so the first read of that field fabricated an
// object reference out of raw kernel bytes. AllGroups then reported the fabricated array's length:
// measured `slice bounds out of range [::20] with capacity 14` against a token holding 20 groups
// (i9, ad7795475a). A liveness check cannot see that --- the call returns, the record exists, and
// the COUNT is simply wrong --- which is why every assertion here is a value.
//
// HOW THIS AVOIDS NEEDING A TOKEN, AN ACCOUNT OR A DOMAIN. os/user's own suite cannot be the guard:
// it is roster-excluded, its TestGroupIds needs a reachable domain controller, and at Go 1.24 the
// path runs off the CURRENT process token, so what it proves depends on which account ran it. The
// repair's whole surface is the TRANSCRIPTION, and the package exposes it as
// `transcribeTokenGroups(byte[])` for exactly this reason: handed a buffer the test builds itself,
// every repaired line runs on any Windows host, unprivileged, offline.
//
// THE BUFFER IS SHAPED THE WAY WINDOWS SHAPES IT, which is the part that matters. Windows appends
// the SID bytes INSIDE the buffer it fills and points each entry at them, so the payload is
// SELF-REFERENTIAL. A test that pointed the entries at SIDs allocated somewhere else would exercise
// the transcription and MISS the lifetime half entirely. These SIDs live in the same array, at
// offsets computed from its own pinned base, exactly as the kernel would leave them.
//
// Windows-only by nature (the SIDs are formatted by advapi32), and gated on the HOST rather than on
// which declaration was compiled --- the distinction WindowsNetUserInfoTests states: a linux-flavor
// guard on a Windows host sails past a declaration-only check and then dies in a module
// initializer, which reads like a regression in whatever is under test. A non-Windows run must read
// NOT MEASURED instead.
[TestClass]
public class WindowsTokenGroupsTranscriptionTests
{
    private static bool OnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // SID_AND_ATTRIBUTES as Windows lays it out, and the same mirror the companion declares. Kept
    // here rather than shared so the guard measures the LAYOUT independently of the code under
    // test: a mirror that drifted in both places at once would still be caught by the string
    // round-trip below, but a guard that borrows the subject's own definition of the record cannot
    // notice a change to it.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSIDAndAttributes
    {
        public nuint Sid;
        public uint Attributes;
    }

    // The group count this guard builds. Twenty is not arbitrary: it is the count the failing
    // measurement reported, against a fabricated managed length of 14 --- so a regression to the
    // old behaviour reads back a WRONG COUNT here rather than merely a different one.
    private const int GroupCount = 20;

    // A SID with one sub-authority: Revision, SubAuthorityCount, the six-byte NT authority
    // {0,0,0,0,0,5}, then the sub-authority DWORD. `S-1-5-<rid>` when advapi32 formats it.
    private const int SidImageLength = 8 + 4;

    // Offsets inside the buffer this guard builds, in the order Windows writes them: the count, the
    // inline entry array, then the SID images the entries point at.
    private static int EntriesOffset => Marshal.SizeOf<NativeSIDAndAttributes>() <= 8 ? 4 : 8;

    // Builds a TOKEN_GROUPS image the way the kernel leaves one: GroupCount, then GroupCount inline
    // SID_AND_ATTRIBUTES, then the SID bytes THOSE ENTRIES POINT AT, all in one pinned array.
    //
    // Pinned because the entries store absolute addresses of their own array's interior --- the
    // self-referential shape the companion's lifetime half exists for. `GC.AllocateArray(pinned:
    // true)` is what the production feeder uses, and using anything else here would make the guard
    // easier and the thing it guards untested.
    private static unsafe (byte[] buffer, uint[] rids) buildTokenGroupsImage()
    {
        int entryLength = Marshal.SizeOf<NativeSIDAndAttributes>();
        int entriesOffset = EntriesOffset;
        int sidsOffset = entriesOffset + GroupCount * entryLength;
        int total = sidsOffset + GroupCount * SidImageLength;

        byte[] buffer = GC.AllocateArray<byte>(total, pinned: true);
        uint[] rids = new uint[GroupCount];

        fixed (byte* basePtr = buffer)
        {
            *(uint*)basePtr = GroupCount;

            for (int i = 0; i < GroupCount; i++)
            {
                byte* sid = basePtr + sidsOffset + i * SidImageLength;

                // A distinct, well-formed S-1-5-<rid>. The RIDs start high enough not to collide
                // with the well-known ones a reader might mistake for a fixture.
                uint rid = (uint)(4000 + i);
                rids[i] = rid;

                sid[0] = 1;                                   // Revision
                sid[1] = 1;                                   // SubAuthorityCount
                sid[2] = 0; sid[3] = 0; sid[4] = 0;           // IdentifierAuthority {0,0,0,0,0,5}
                sid[5] = 0; sid[6] = 0; sid[7] = 5;
                *(uint*)(sid + 8) = rid;                      // SubAuthority[0]

                NativeSIDAndAttributes* entry =
                    (NativeSIDAndAttributes*)(basePtr + entriesOffset + i * entryLength);

                entry->Sid = (nuint)sid;                      // INTO this same buffer
                entry->Attributes = (uint)(i + 1);            // distinct, and never zero
            }
        }

        return (buffer, rids);
    }

    // The count, and every entry's VALUE. This is the assertion the old behaviour fails: it read a
    // fabricated managed array's length, not the buffer's count.
    [TestMethod]
    public void TranscriptionReadsEveryGroupTheBufferHolds()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("the SIDs are formatted by advapi32; NOT MEASURED off Windows");
            return;
        }

        var (buffer, rids) = buildTokenGroupsImage();

        var Ꮡgroups = winint.transcribeTokenGroups(buffer);

        Assert.AreEqual((uint)GroupCount, (uint)Ꮡgroups.Value.GroupCount,
            "the transcription must report the count the BUFFER holds. Reading a managed array's " +
            "length instead is the defect this guard exists for: the failing measurement saw 14 " +
            "where the token said 20");

        slice<winint.SID_AND_ATTRIBUTES> all = Ꮡgroups.AllGroups();

        Assert.AreEqual(GroupCount, len(all),
            "AllGroups must hand back every transcribed entry — len and cap both GroupCount, as " +
            "Go's [:GroupCount:GroupCount] gives");

        for (int i = 0; i < GroupCount; i++)
        {
            Assert.AreEqual((uint)(i + 1), (uint)all[i].Attributes,
                $"entry {i}'s Attributes came from the wrong offset: a stride or a mirror that " +
                "drifted reads a neighbouring field rather than faulting");

            var (text, err) = all[i].Sid.String();

            Assert.IsNull(err, $"entry {i}'s Sid did not format: {err?.Error()}");

            // `text` is a golib @string and the expected value is a C# interpolated
            // System.String. There is no implicit conversion between them, so a bare
            // Assert.AreEqual binds MSTest's AreEqual(object, object) overload, whose equality is
            // TYPE-sensitive: the two never compare equal however identical their text, and the
            // failure prints both values looking the same. Both operands are brought into the C#
            // domain, and the type argument is written out so that a future operand which is NOT
            // a string is a COMPILE error here rather than a silent rebind to object. Same at the
            // second arm's compare below.
            Assert.AreEqual<string>($"S-1-5-{rids[i]}", text.ToString(),
                $"entry {i}'s Sid must name the SID this buffer holds AT THAT ENTRY. A wrong " +
                "address formats as a different SID or fails outright, which is what makes this a " +
                "ROUND TRIP rather than a liveness check");
        }
    }

    // ⚠ THE LIFETIME HALF, and the arm that would still pass if only the type half were fixed.
    //
    // The SIDs live inside the buffer, so the addresses are meaningful only while it stays put. The
    // companion anchors the buffer to every SID minted from it, which means dropping every OTHER
    // reference must not invalidate them. Here the guard drops its own and collects, hard, before
    // reading the SIDs back.
    //
    // It cannot PROVE the anchor — a pinned array that happened to survive would read the same —
    // so what it does is remove the easy way to be wrong: a transcription that returned addresses
    // into a buffer nothing retains fails here whenever a collection reclaims it, and passes only
    // by luck otherwise. Stated rather than claimed as proof.
    [TestMethod]
    public void TranscribedSidsSurviveTheBufferGoingOutOfScope()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("the SIDs are formatted by advapi32; NOT MEASURED off Windows");
            return;
        }

        uint[] rids;
        slice<winint.SID_AND_ATTRIBUTES> all;

        {
            var (buffer, built) = buildTokenGroupsImage();
            rids = built;
            all = winint.transcribeTokenGroups(buffer).AllGroups();
            // `buffer` goes out of scope here and is not kept alive on purpose.
        }

        for (int round = 0; round < 3; round++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }

        for (int i = 0; i < GroupCount; i++)
        {
            var (text, err) = all[i].Sid.String();

            Assert.IsNull(err, $"entry {i}'s Sid did not format after collection: {err?.Error()}");

            // The typed compare, for the reason the first arm states.
            Assert.AreEqual<string>($"S-1-5-{rids[i]}", text.ToString(),
                $"entry {i}'s Sid stopped naming its SID once the caller's own reference to the " +
                "buffer was gone — the anchor that ties the buffer's lifetime to the SIDs is what " +
                "this asserts, and its absence is a use-after-free with a plausible-looking value");
        }
    }
}
