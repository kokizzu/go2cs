using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Descriptor cargo, increment C: the element array LENGTH of a slice-of-array is recorded at the
/// site that still knows it statically, because a slice with no elements cannot be asked for it.
/// </summary>
/// <remarks>
/// A Go <c>[][3]uint8</c> and a <c>[][4]uint8</c> are one managed type, <c>slice&lt;array&lt;byte&gt;&gt;</c>,
/// so the length lives nowhere in the value once the slice is empty. These rows guard the three
/// facts the side table rests on, and one boundary it deliberately does not cover.
/// </remarks>
[TestClass]
public class SliceElemDimsTests
{
    private static slice<array<byte>> EmptySliceOfArray() => new array<byte>[] { }.slice();

    /// <summary>
    /// The fix itself: an EMPTY slice-of-array answers the length recorded at its creation site,
    /// where observation has nothing to look at.
    /// </summary>
    [TestMethod]
    public void AnEmptySliceOfArrayAnswersTheLengthRecordedAtItsCreationSite()
    {
        slice<array<byte>> empty = GoReflect.WithElemDims(EmptySliceOfArray(), 3);

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.SliceElemArrayDims(empty),
            "an empty slice-of-array must answer the element length recorded when it was created");
    }

    /// <summary>
    /// The two-lengths case, which is what makes the key a per-backing identity rather than a
    /// per-managed-type one: <c>[][3]uint8</c> and <c>[][4]uint8</c> share a managed type and must
    /// still answer their own lengths.
    /// </summary>
    [TestMethod]
    public void TwoEmptySlicesOfDifferentElementLengthsDoNotShareARecord()
    {
        slice<array<byte>> three = GoReflect.WithElemDims(EmptySliceOfArray(), 3);
        slice<array<byte>> four = GoReflect.WithElemDims(EmptySliceOfArray(), 4);

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.SliceElemArrayDims(three));
        CollectionAssert.AreEqual(new nint[] { 4 }, GoReflect.SliceElemArrayDims(four));
    }

    /// <summary>
    /// <see cref="Array.Empty{T}"/> is a SINGLETON shared by every length, so recording against it
    /// would make the two lengths above collide on one key. The write path substitutes a fresh
    /// zero-length backing rather than refusing, because <c>make([][3]uint8, 0)</c> is a legal Go
    /// program and must not throw. This row feeds the singleton in deliberately.
    /// </summary>
    [TestMethod]
    public void ASharedEmptyBackingIsSubstitutedRatherThanRecordedAgainst()
    {
        slice<array<byte>> fromSingleton = GoReflect.WithElemDims(new slice<array<byte>>(Array.Empty<array<byte>>()), 3);
        slice<array<byte>> otherFromSingleton = GoReflect.WithElemDims(new slice<array<byte>>(Array.Empty<array<byte>>()), 4);

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.SliceElemArrayDims(fromSingleton),
            "the shared empty backing must have been substituted, or the two lengths collide");
        CollectionAssert.AreEqual(new nint[] { 4 }, GoReflect.SliceElemArrayDims(otherFromSingleton));
        Assert.AreEqual(0, (int)fromSingleton.Length, "substitution must not change the slice's length");
    }

    /// <summary>
    /// A slice that HAS an element is still answered by observation, so nothing that was already
    /// right becomes wrong when the table has no entry for it.
    /// </summary>
    [TestMethod]
    public void APopulatedSliceOfArrayIsStillAnsweredByObservation()
    {
        slice<array<byte>> populated = new array<byte>[] { new array<byte>(6) }.slice();

        CollectionAssert.AreEqual(new nint[] { 6 }, GoReflect.SliceElemArrayDims(populated),
            "observation remains the fallback for a slice with an element to measure");
    }

    /// <summary>
    /// Slicing an ARRAY is the third creation kind, and <c>arr[:0]</c> is its shape with nothing to
    /// observe. The record goes on the ARRAY'S OWN backing store — <c>array&lt;T&gt;.slice</c> hands
    /// out a window over <c>m_array</c> rather than a copy — so every later window over the same
    /// array inherits it without being wrapped itself.
    /// </summary>
    [TestMethod]
    public void SlicingAnArrayRecordsAgainstTheArraysOwnBackingSoEveryWindowInheritsIt()
    {
        array<array<byte>> source = new array<array<byte>>(5);

        slice<array<byte>> emptyWindow = GoReflect.WithElemDims(source.slice(0, 0), 3);

        Assert.AreEqual(0, (int)emptyWindow.Length, "arr[:0] is the window with nothing to observe");
        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.SliceElemArrayDims(emptyWindow));

        slice<array<byte>> unwrappedWindow = source.slice(2, 2);

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.SliceElemArrayDims(unwrappedWindow),
            "a window over the same array shares its backing store and must inherit the record");
    }

    /// <summary>
    /// RECORDED BOUNDARY, asserted at TODAY's answer so the remedy cannot land silently: a NIL slice
    /// has no backing object to key on, so its element length stays unrecoverable and
    /// <c>reflect.TypeOf(x)</c> answers a dimensionless descriptor where Go answers
    /// <c>[][3]uint8</c>. The remedy is the +8 B element-dims field on the slice header, measured and
    /// declined for this cut (130 stdlib creation sites would need it; 27,143 would pay for it).
    /// When that field lands this row FAILS, which is the point: update it then, deliberately.
    /// </summary>
    [TestMethod]
    public void ANilSliceCannotCarryItsElementLength_RecordedBoundary()
    {
        slice<array<byte>> nilSlice = default;

        Assert.IsNull(GoReflect.WithElemDims(nilSlice, 3).m_array,
            "a nil slice must stay nil — substituting a backing here would change `x == nil`");
        Assert.IsNull(GoReflect.SliceElemArrayDims(GoReflect.WithElemDims(nilSlice, 3)),
            "TODAY's answer for a nil slice-of-array; see the remedy in this row's remarks");
    }
}
