using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using user = go.os.user_package;

namespace GolibTests;

// The owed observer for row 46 --- os/user's local-group decision, in os/user's
// windows/lookup_windows_impl.cs.
//
// WHY IT IS OWED. Row 46's build half is met (os/user compiles, i9 at 092c0213e2), but its RUNTIME
// half is unobservable by Go's own suite here: TestGroupIds needs an account, at 1.24 the path runs
// off the CURRENT process token so what it proves depends on which account ran it, and COORD ruled
// (39ddead63e) that no box which cannot create accounts can reach the branches the row's own commit
// created. An unreachable branch with no guard is a hand-own nobody can falsify.
//
// WHAT IS UNDER TEST, and what is not. The hand-own's decision is a pure function of
// (entriesRead, entries, domain, username) taken AFTER NetUserGetLocalGroups returns, so the
// package exposes it as `readLocalGroupNames` and this class drives it with argument tuples it
// builds itself --- no account, no domain, no privilege, no network. The API call stays on the
// other side of the seam, and so does the SID lookup: lookupGroupName is syscall.LookupSID, and a
// seam that answered SIDs could only be driven against names that resolve on the running host,
// which is the account dependence this extraction exists to remove.
//
// NOT A MOCK OF NetUserGetLocalGroups, deliberately. A mock would assert what the mock does. What
// needs asserting is what the hand-own DECIDES about a result --- which is the half Go's suite
// cannot reach, and the half the 1.24 hop changed: until 1.24 an empty membership and a published
// nil shared one error, and 1.24 made the empty case `nil, nil` while the nil buffer stayed an
// error. Sharing one branch now would answer "no groups" to a nil buffer, which is the silent wrong
// answer. The first two arms below are that order, asserted in both directions.
//
// THE BUFFER IS SHAPED THE WAY NETAPI32 SHAPES ONE: an array of LOCALGROUP_USERS_INFO_0 (one LPWSTR
// each) followed by the name text THOSE ENTRIES POINT AT, all inside a single pinned allocation, so
// the entries carry absolute addresses of their own array's interior. Pointing them at text
// allocated somewhere else would exercise the stride walk and miss the shape it walks.
//
// Windows-only by nature (os/user's windows flavour declares this member at all), and gated on the
// HOST rather than on which declaration was compiled --- the distinction WindowsNetUserInfoTests
// states: a guard compiled for one flavour and run on another host sails past a declaration-only
// check and then dies in a module initializer, which reads like a regression in whatever is under
// test. A non-Windows run must read NOT MEASURED instead.
[TestClass]
public class WindowsLocalGroupDecisionTests
{
    private static bool OnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // The domain and username the error text must name back. Neither exists anywhere, which is the
    // point: they are carried through the decision, not looked up by it.
    private const string Domain = "TESTDOM";
    private const string Username = "testuser";

    // Three names, because three is the count the error arm claims and the count the walk must
    // lift. One carries a non-ASCII rune so a decoder that read bytes rather than UTF-16 code units
    // fails on a VALUE here rather than passing by accident on ASCII.
    private static readonly string[] GroupNames = { "go2cs Local Admins", "Sécurité", "REMOTE DESKTOP USERS" };

    // The error the nil-buffer branch owes, spelled out rather than composed from the same format
    // string the code under test uses --- a guard that built its expectation the way the subject
    // builds its answer would agree with any change to either.
    private const string NilBufferError =
        "listGroupsForUsernameAndDomain: NetUserGetLocalGroups() published a nil buffer for 3 entries " +
        "for domain: TESTDOM, username: testuser";

    // Builds a LOCALGROUP_USERS_INFO_0 array the way netapi32 leaves one: `names.Length` LPWSTR
    // slots, then the NUL-terminated UTF-16 text each slot points at, in one pinned array.
    //
    // Pinned because the slots store absolute addresses of their own array's interior. The buffer is
    // returned alongside the address so the caller holds it: an unrooted pinned array is still
    // collectable, and a guard that lost it would be measuring freed memory.
    private static unsafe (byte[] buffer, nuint address) buildLocalGroupImage(string[] names)
    {
        int entryLength = IntPtr.Size;
        int[] offsets = new int[names.Length];
        int total = names.Length * entryLength;

        for (int i = 0; i < names.Length; i++)
        {
            offsets[i] = total;
            total += (names[i].Length + 1) * sizeof(char);
        }

        byte[] buffer = GC.AllocateArray<byte>(total, pinned: true);

        fixed (byte* basePtr = buffer)
        {
            for (int i = 0; i < names.Length; i++)
            {
                char* text = (char*)(basePtr + offsets[i]);

                for (int j = 0; j < names[i].Length; j++)
                {
                    text[j] = names[i][j];
                }

                text[names[i].Length] = '\0';

                ((nuint*)basePtr)[i] = (nuint)(basePtr + offsets[i]);
            }

            return (buffer, (nuint)basePtr);
        }
    }

    // ARM 1 --- (0, null). An empty membership is not an error, and 1.24 is where that became true.
    // Before the row-46 commit this tuple produced an fmt.Errorf, so a regression to the shared
    // branch fails here on a VALUE.
    [TestMethod]
    public void EmptyMembershipOverANilBufferIsNotAnError()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("os/user's local-group decision is the windows flavor");
        }

        var (names, err) = user.readLocalGroupNames(0u, (nuint)0, Domain, Username);

        Assert.IsNull(err, $"an empty membership carries no error, got: {err?.Error().ToString()}");
        Assert.IsTrue(names == nil, "an empty membership answers a nil slice");
        Assert.AreEqual(0, (int)len(names), "and the caller ranges it zero times");
    }

    // ARM 2 --- (0, non-null). The SAME answer over a buffer that is emphatically there. What this
    // adds to arm 1 is that entriesRead ALONE decides the empty case: a walk that trusted the
    // pointer instead would lift entry 0 out of a buffer netapi32 claimed held nothing. Arm 1 is
    // what catches the two tests being swapped (a nil buffer would become an error there); this one
    // is what catches the empty case being read off the pointer.
    [TestMethod]
    public void EmptyMembershipOverANonNilBufferIsAlsoNotAnError()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("os/user's local-group decision is the windows flavor");
        }

        var (buffer, address) = buildLocalGroupImage(GroupNames);

        var (names, err) = user.readLocalGroupNames(0u, address, Domain, Username);

        Assert.IsNull(err, $"an empty membership carries no error, got: {err?.Error().ToString()}");
        Assert.IsTrue(names == nil, "an empty membership answers a nil slice whatever the buffer holds");
        Assert.AreEqual(0, (int)len(names), "and nothing is lifted from a buffer with no entries claimed");

        GC.KeepAlive(buffer);
    }

    // ARM 3 --- (3, null). A published nil with entries CLAIMED is the branch the hand-own was taken
    // for: upstream slices the buffer here unconditionally and faults. It must be an error and it
    // must NOT be arm 1's answer --- answering "no groups" to a nil buffer is the silent wrong
    // answer this split exists to prevent.
    //
    // The TEXT is asserted, not merely the presence of an error. It names entriesRead, the domain
    // and the username, and it is the only record a reader of a failed lookup has that the BUFFER,
    // not the caller, was what was wrong.
    [TestMethod]
    public void EntriesClaimedOverANilBufferIsTheNamedError()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("os/user's local-group decision is the windows flavor");
        }

        var (names, err) = user.readLocalGroupNames(3u, (nuint)0, Domain, Username);

        Assert.IsNotNull(err, "a nil buffer with entries claimed is an error, not an empty membership");
        Assert.AreEqual(NilBufferError, err.Error().ToString(),
            "the error names entriesRead, the domain and the username");
        Assert.IsTrue(names == nil, "and it answers no names");
    }

    // ARM 4 --- (3, non-null over a hand-built buffer). The stride walk: every name the buffer holds
    // is lifted, in order, decoded as UTF-16, and COPIED --- the copy is what lets the caller free
    // the netapi32 buffer before the SID lookups run.
    //
    // A regression that walked the CONVERTED LocalGroupUserInfo0 (whose single field is a `ж<uint16>`,
    // i.e. a managed reference) would fabricate one object reference per element out of raw kernel
    // bytes; this arm reads the VALUES back, so that failure is a wrong name here and not a crash
    // somewhere later.
    [TestMethod]
    public void EveryNameTheBufferHoldsIsLifted()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("os/user's local-group decision is the windows flavor");
        }

        var (buffer, address) = buildLocalGroupImage(GroupNames);

        var (names, err) = user.readLocalGroupNames((uint)GroupNames.Length, address, Domain, Username);

        Assert.IsNull(err, $"a populated buffer carries no error, got: {err?.Error().ToString()}");
        Assert.AreEqual(GroupNames.Length, (int)len(names), "every entry claimed is lifted");

        for (int i = 0; i < GroupNames.Length; i++)
        {
            Assert.AreEqual(GroupNames[i], names[i].ToString(), $"group {i} round-trips through the stride walk");
        }

        GC.KeepAlive(buffer);
    }

    // WHAT THIS CLASS DOES NOT ASSERT, stated rather than left to be discovered:
    //
    //   * the `name == null` CONTINUE inside the walk --- an entry whose LPWSTR is nil is skipped.
    //     COORD's design named four arms and these are those four; the branch is one line and is
    //     named here so the gap is on the record rather than in nobody's head.
    //   * anything about NetUserGetLocalGroups itself. That call is on the other side of the seam,
    //     and WindowsNetUserInfoTests already drives it against netapi32's own answer.
    //   * the SID lookup. lookupGroupName is an API call and stays outside, for the reason the
    //     header gives.
}
