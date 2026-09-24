// SstringAllocationCountTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// V-fix 11: <c>sstring</c>'s own allocations are charged through <c>AllocationCounter</c>, and each empty
/// case, which allocates nothing, is charged nothing.
/// </summary>
/// <remarks>
/// Before, sstring encoded C# strings with <c>Encoding.UTF8.GetBytes</c>, copied with <c>ToArray()</c> and
/// concatenated into a raw <c>new byte[]</c>, so every one of those objects was invisible to
/// <c>testing.AllocsPerRun</c>'s count: encoding/json's <c>"..."u8 + (sstring)buf + "..."u8</c> made two
/// objects and counted one. The count is of CLR objects, so a shape that allocates more objects than Go's
/// (a C# string operand is encoded before it is concatenated) now says so rather than reading low.
/// </remarks>
[TestClass]
public class SstringAllocationCountTests
{
    [ClassInitialize]
    public static void EnableCounting(TestContext _) => AllocationCounter.Enable();

    [TestMethod]
    public void ConstructionFromACSharpStringCountsItsEncoding()
    {
        long before = AllocationCounter.CurrentThreadCount;
        sstring s = new("hello");
        Assert.AreEqual(1L, AllocationCounter.CurrentThreadCount - before, "new sstring(\"hello\") encodes one backing");
        Assert.AreEqual(5, s.Length);

        before = AllocationCounter.CurrentThreadCount;
        sstring empty = new("");
        sstring nil = new((string?)null);
        Assert.AreEqual(0L, AllocationCounter.CurrentThreadCount - before, "an empty or null C# string allocates nothing");
        Assert.AreEqual(0, empty.Length + nil.Length);
    }

    [TestMethod]
    public void ByteConversionsCountTheirCopy()
    {
        sstring s = (sstring)"hello"u8;
        sstring empty = (sstring)""u8;

        long before = AllocationCounter.CurrentThreadCount;
        slice<byte> window = s.Slice(1, 3);
        Assert.AreEqual(1L, AllocationCounter.CurrentThreadCount - before, "[]byte(s[a:b]) copies once");
        Assert.AreEqual(3, window.Length);

        before = AllocationCounter.CurrentThreadCount;
        slice<byte> whole = s;
        byte[] raw = (byte[])s;
        Assert.AreEqual(2L, AllocationCounter.CurrentThreadCount - before, "[]byte(s) and the byte[] conversion copy once each");
        Assert.AreEqual(10, whole.Length + raw.Length);

        before = AllocationCounter.CurrentThreadCount;
        slice<byte> emptyWindow = s.Slice(1, 0);
        slice<byte> emptyWhole = empty;
        byte[] emptyRaw = (byte[])empty;
        Assert.AreEqual(0L, AllocationCounter.CurrentThreadCount - before, "an empty copy allocates nothing");
        Assert.AreEqual(0, emptyWindow.Length + emptyWhole.Length + emptyRaw.Length);
    }

    [TestMethod]
    public void ConcatenationCountsEveryObjectItMakes()
    {
        sstring a = (sstring)"ab"u8;
        sstring b = (sstring)"cd"u8;
        sstring empty = (sstring)""u8;

        long before = AllocationCounter.CurrentThreadCount;
        @string ab = a + b;
        Assert.AreEqual(1L, AllocationCounter.CurrentThreadCount - before, "sstring + sstring is one backing");
        Assert.AreEqual("abcd", ab.ToString());

        // A C# string operand is UTF-8 encoded first (1) and then concatenated (1).
        before = AllocationCounter.CurrentThreadCount;
        @string left = "xy" + b;
        @string right = a + "xy";
        Assert.AreEqual(4L, AllocationCounter.CurrentThreadCount - before, "string + sstring encodes, then concatenates");
        Assert.AreEqual("xycdabxy", left.ToString() + right.ToString());

        // Go's "" + "" allocates nothing, and neither does the zero-length backing (REC-F).
        before = AllocationCounter.CurrentThreadCount;
        @string none = empty + empty;
        Assert.AreEqual(0L, AllocationCounter.CurrentThreadCount - before, "\"\" + \"\" allocates nothing");
        Assert.AreEqual(0, none.Length);
    }
}
