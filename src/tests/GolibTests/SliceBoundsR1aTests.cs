using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using Δruntime = go.runtime_package;

namespace GolibTests;

// S-c R1-A, commit 1 (docs/phase4/DESIGN-slice-bounds-r1a.md): golib's slice-bound panics are Go's, in text AND
// in type, through the paths converted code reaches TODAY -- the Range indexers (2-index) and the three-argument
// `.slice(low, high, max)` (3-index). Every expectation is go1.24.13's own output for the same expression.
//
//   - A 2-index slice past its capacity, low past high, and low past the length recovered as a plain STRING;
//     Go panics with runtime.boundsError (boundsSliceAcap, boundsSliceB), a runtime.Error.
//   - An ARRAY past its length printed "with capacity 3"; Go prints "with length 3" (boundsSliceAlen), and the
//     3-index form "[::4] with length 3" (boundsSlice3Alen).
//   - A 3-index expression was formatted with the 2-index shapes: `s[0:11:10]` printed "[:11] with capacity 10"
//     where Go prints "[:11:10]" (boundsSlice3B), `s[4:2:5]` printed "[4:2]" for Go's "[4:2:]" (boundsSlice3C),
//     and a negative bound lost its colons (`[-2:]` for Go's "[-2::]").
//   - A stack string (sstring, the sstring-twin bodies) sliced past its end threw the span's CLR
//     ArgumentOutOfRangeException, which recover() never sees; Go panics "[:5] with length 3".
// The in-range control at the end must read the same before and after.
[TestClass]
public class SliceBoundsR1aTests
{
    [ClassInitialize]
    public static void RunRuntimeModuleInitializer(TestContext _)
    {
        // runtime registers the boundsError hook in its module initializer (RuntimeErrorPanicValueTests).
        RuntimeHelpers.RunModuleConstructor(typeof(Δruntime).Module.ModuleHandle);
    }

    private static object? PanicValue(Action fn)
    {
        object? recovered = null;
        GoFrame frame = default;

        try
        {
            frame.Push(() => recovered = recover());
            fn();
        }
        catch (Exception ex) when (GoFrame.IsPanic(ex, out PanicException? p))
        {
            GoFrame.Capture(p);
        }
        finally
        {
            frame.Run();
        }

        return recovered;
    }

    private static void AssertRuntimeError(Action fn, string want, string site)
    {
        object? recovered = PanicValue(fn);

        Assert.IsNotNull(recovered, $"{site}: did not panic");

        Assert.IsTrue(recovered._<Δruntime.ΔError>(out Δruntime.ΔError? error),
            $"{site}: recovered value {recovered} (type {recovered.GetType().FullName}) does not implement runtime.Error");

        Assert.AreEqual(want, error!.Error().ToString(), $"{site}: runtime.Error text");
    }

    private const string Prefix = "runtime error: slice bounds out of range ";

    // s := make([]int, 3, 10)
    private static slice<int> Slice3Cap10() => new(3, 10);

    [TestMethod]
    public void TwoIndexSliceBoundsRecoverAsRuntimeError()
    {
        slice<int> s = Slice3Cap10();

        AssertRuntimeError(() => _ = s[..11], Prefix + "[:11] with capacity 10", "s[:11]");
        AssertRuntimeError(() => _ = s[4..2], Prefix + "[4:2]", "s[4:2]");
        AssertRuntimeError(() => _ = s[4..], Prefix + "[4:3]", "s[4:]");
        AssertRuntimeError(() => _ = default(slice<int>)[..1], Prefix + "[:1] with capacity 0", "nil[:1]");
        AssertRuntimeError(() => _ = default(slice<int>)[1..], Prefix + "[1:0]", "nil[1:]");
    }

    [TestMethod]
    public void TwoIndexArrayBoundsUseTheLength()
    {
        array<int> a = new(3);

        AssertRuntimeError(() => _ = a[..5], Prefix + "[:5] with length 3", "a[:5]");
        AssertRuntimeError(() => _ = a[2..1], Prefix + "[2:1]", "a[2:1]");
        AssertRuntimeError(() => _ = a[4..], Prefix + "[4:3]", "a[4:]");
    }

    [TestMethod]
    public void ThreeIndexSliceBoundsUseTheThreeIndexShapes()
    {
        slice<int> s = Slice3Cap10();

        AssertRuntimeError(() => _ = s.slice(0, 2, 11), Prefix + "[::11] with capacity 10", "s[0:2:11]");
        AssertRuntimeError(() => _ = s.slice(0, 11, 10), Prefix + "[:11:10]", "s[0:11:10]");
        AssertRuntimeError(() => _ = s.slice(0, 5, 4), Prefix + "[:5:4]", "s[0:5:4]");
        AssertRuntimeError(() => _ = s.slice(4, 2, 5), Prefix + "[4:2:]", "s[4:2:5]");
        AssertRuntimeError(() => _ = s.slice(-2, 2, 5), Prefix + "[-2::]", "s[-2:2:5]");
        AssertRuntimeError(() => _ = s.slice(0, -2, 5), Prefix + "[:-2:]", "s[0:-2:5]");
        AssertRuntimeError(() => _ = s.slice(0, 2, -2), Prefix + "[::-2]", "s[0:2:-2]");
    }

    [TestMethod]
    public void ThreeIndexArrayBoundsUseTheLength()
    {
        array<int> a = new(3);

        AssertRuntimeError(() => _ = a.slice(0, 2, 4), Prefix + "[::4] with length 3", "a[0:2:4]");
        AssertRuntimeError(() => _ = a.slice(2, 1, 3), Prefix + "[2:1:]", "a[2:1:3]");
    }

    [TestMethod]
    public void AStackStringSlicedPastItsEndRecoversAsRuntimeError()
    {
        AssertRuntimeError(() => { sstring ss = new("abc"u8); _ = ss[0..5]; }, Prefix + "[:5] with length 3", "ss[:5]");
        AssertRuntimeError(() => { sstring ss = new("abc"u8); _ = ss[2..1]; }, Prefix + "[2:1]", "ss[2:1]");
    }

    // Past int32: Go's `s[:big]` with big = 1<<32 + 5, which a C# Range truncates to 5.
    private const long Big = (1L << 32) + 5;

    // The sentinel-free 2-index API S-c R1-A's emission will call when a bound is not a constant that fits int32:
    // every nint value is a bound, so -1 panics as Go's -1 does and Big is checked at its full value.
    [TestMethod]
    public void SentinelFreeSliceBoundsPanicAsGo()
    {
        slice<int> s = Slice3Cap10();

        AssertRuntimeError(() => _ = s.slice(0, -1), Prefix + "[:-1]", "s[:-1]");
        AssertRuntimeError(() => _ = s.slice(-1), Prefix + "[-1:]", "s[-1:]");
        AssertRuntimeError(() => _ = s.slice(-1, 2), Prefix + "[-1:]", "s[-1:2]");
        AssertRuntimeError(() => _ = s.slice(4), Prefix + "[4:3]", "s[4:]");
        AssertRuntimeError(() => _ = s.slice(4, 2), Prefix + "[4:2]", "s[4:2]");
        AssertRuntimeError(() => _ = s.slice(0, (nint)Big), Prefix + "[:4294967301] with capacity 10", "s[:big]");
        AssertRuntimeError(() => _ = s.slice((nint)Big), Prefix + "[4294967301:3]", "s[big:]");

        slice<int> t = s.slice(1, 5);
        Assert.AreEqual((nint)4, len(t), "len(s[1:5]) reaches past the length into the capacity");
        Assert.AreEqual((nint)9, cap(t), "cap(s[1:5])");
        Assert.AreEqual((nint)0, len(s.slice(3)), "len(s[3:])");
        Assert.AreEqual((nint)8, cap(new slice<int>(new int[8]).slice(0, 2)), "an existing two-argument call keeps its capacity");
    }

    [TestMethod]
    public void SentinelFreeArrayBoundsUseTheLength()
    {
        array<int> a = new(3);

        AssertRuntimeError(() => _ = a.slice(0, -1), Prefix + "[:-1]", "a[:-1]");
        AssertRuntimeError(() => _ = a.slice(-1), Prefix + "[-1:]", "a[-1:]");
        AssertRuntimeError(() => _ = a.slice(0, 5), Prefix + "[:5] with length 3", "a[:5]");
        AssertRuntimeError(() => _ = a.slice(0, (nint)Big), Prefix + "[:4294967301] with length 3", "a[:big]");

        Assert.AreEqual((nint)2, cap(a.slice(1)), "cap(a[1:])");
    }

    [TestMethod]
    public void SentinelFreeStringBoundsUseTheLength()
    {
        @string str = "abc";

        AssertRuntimeError(() => _ = str.slice(0, -1), Prefix + "[:-1]", "str[:-1]");
        AssertRuntimeError(() => _ = str.slice(-1), Prefix + "[-1:]", "str[-1:]");
        AssertRuntimeError(() => _ = str.slice(0, (nint)Big), Prefix + "[:4294967301] with length 3", "str[:big]");
        AssertRuntimeError(() => _ = str.slice(4), Prefix + "[4:3]", "str[4:]");
        Assert.AreEqual("bc", str.slice(1).ToString(), "str[1:]");
        Assert.AreEqual("b", str.slice(1, 2).ToString(), "str[1:2]");

        AssertRuntimeError(() => { sstring ss = new("abc"u8); _ = ss.slice(-1); }, Prefix + "[-1:]", "ss[-1:]");
        AssertRuntimeError(() => { sstring ss = new("abc"u8); _ = ss.slice(0, 5); }, Prefix + "[:5] with length 3", "ss[:5]");

        // A string LITERAL is a `"..."u8` span in converted code.
        AssertRuntimeError(() => _ = "abc"u8.slice(0, 5), Prefix + "[:5] with length 3", "\"abc\"[:5]");
        AssertRuntimeError(() => _ = "abc"u8.slice(-1), Prefix + "[-1:]", "\"abc\"[-1:]");
        Assert.AreEqual((byte)'b', "abc"u8.slice(1, 2)[0], "\"abc\"[1:2]");
    }

    // A `string | []byte` body sub-slices through its constraint (IByteSeq<TSelf, T>), for both members.
    private static TSeq Sub<TSeq>(TSeq seq, nint low, nint high) where TSeq : IByteSeq<TSeq, byte> => seq.slice(low, high);

    private static TSeq From<TSeq>(TSeq seq, nint low) where TSeq : IByteSeq<TSeq, byte> => seq.slice(low);

    [TestMethod]
    public void SentinelFreeByteSeqBoundsPanicAsGo()
    {
        @string str = "abc";
        slice<byte> bytes = new(new byte[] { 1, 2, 3 });

        AssertRuntimeError(() => _ = Sub(str, 0, -1), Prefix + "[:-1]", "string [:-1]");
        AssertRuntimeError(() => _ = Sub(bytes, 0, 4), Prefix + "[:4] with capacity 3", "[]byte [:4]");
        AssertRuntimeError(() => _ = From(bytes, -1), Prefix + "[-1:]", "[]byte [-1:]");

        Assert.AreEqual("bc", Sub(str, 1, 3).ToString(), "string [1:3]");
        Assert.AreEqual((nint)2, len(From(bytes, 1)), "[]byte [1:]");
    }

    // The go2cs-gen wrappers forward the pair and keep the NAMED type: a named slice (sort.IntSlice) and a named
    // string (reflect.StructTag), both real converted types.
    [TestMethod]
    public void SentinelFreeNamedTypesKeepTheirType()
    {
        sort_package.IntSlice n = new slice<nint>(3, 10);

        AssertRuntimeError(() => _ = n.slice(0, 11), Prefix + "[:11] with capacity 10", "IntSlice[:11]");
        AssertRuntimeError(() => _ = n.slice(-1), Prefix + "[-1:]", "IntSlice[-1:]");
        Assert.AreEqual(typeof(sort_package.IntSlice), ((object)n.slice(1, 5)).GetType(), "IntSlice[1:5] is an IntSlice");
        Assert.AreEqual((nint)4, len(n.slice(1, 5)), "len(IntSlice[1:5])");

        reflect_package.StructTag tag = "abc"u8;

        AssertRuntimeError(() => _ = tag.slice(0, 5), Prefix + "[:5] with length 3", "StructTag[:5]");
        AssertRuntimeError(() => _ = tag.slice(-1), Prefix + "[-1:]", "StructTag[-1:]");
        Assert.AreEqual(typeof(reflect_package.StructTag), ((object)tag.slice(1)).GetType(), "StructTag[1:] is a StructTag");
        Assert.AreEqual((byte)'c', tag.slice(1, 3)[1], "StructTag[1:3][1]");
    }

    [TestMethod]
    public void InRangeSlicingIsUnchanged()
    {
        slice<int> s = Slice3Cap10();
        slice<int> t = s[1..3];
        Assert.AreEqual((nint)2, len(t), "len(s[1:3])");
        Assert.AreEqual((nint)9, cap(t), "cap(s[1:3])");

        slice<int> u = s[..10];
        Assert.AreEqual((nint)10, len(u), "len(s[:10]) reaches the capacity");

        slice<int> v = s.slice(1, 2, 5);
        Assert.AreEqual((nint)1, len(v), "len(s[1:2:5])");
        Assert.AreEqual((nint)4, cap(v), "cap(s[1:2:5])");

        array<int> a = new(3);
        Assert.AreEqual((nint)2, len(a[1..]), "len(a[1:])");
        Assert.AreEqual((nint)2, cap(a[1..]), "cap(a[1:])");
        Assert.AreEqual((nint)1, cap(a.slice(0, 1, 1)), "cap(a[0:1:1])");

        @string str = "abc";
        Assert.AreEqual("bc", str[1..].ToString(), "str[1:]");

        sstring ss = new("abc"u8);
        Assert.AreEqual(2, ss[1..3].Length, "len(ss[1:3])");
    }
}
