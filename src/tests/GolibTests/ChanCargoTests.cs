using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Descriptor cargo, increment D: the unified channel-VALUE cargo. One reference field on
/// <c>channel&lt;T&gt;</c> carries both the per-level direction chain and the element array dims,
/// because both are value-position cargo on a struct whose nil and zero forms have no core to key a
/// side table on. These rows guard the golib facts the bridge stands on; the printed spellings live in
/// the ChanDirectionChain and ChanElemDims behavioral guards, compared against <c>go run</c>.
/// </summary>
[TestClass]
public class ChanCargoTests
{
    /// <summary>
    /// The two scalar directions every pre-D stamp site produces are INTERNED: a directional nil
    /// channel allocates nothing per instance, which is what keeps the cost of the cargo confined to
    /// the sites that actually stamp a chain or dims.
    /// </summary>
    [TestMethod]
    public void AScalarDirectionIsInternedAndAllocatesNothingPerChannel()
    {
        Assert.AreSame(ChanCargo.Of(GoChanDir.Send), ChanCargo.Of(GoChanDir.Send), "Send must be one shared instance");
        Assert.AreSame(ChanCargo.Of(GoChanDir.Recv), ChanCargo.Of(GoChanDir.Recv), "Recv must be one shared instance");
        Assert.IsNull(ChanCargo.Of(GoChanDir.Both), "Both is the unstamped channel's canonical spelling: null");
        Assert.IsNull(ChanCargo.Of(GoChanDir.Unstamped), "Unstamped is null");
    }

    /// <summary>
    /// The bridge reads a value's direction as the chain's HEAD, so a nested chain answers this
    /// channel's own direction and keeps the element's for Elem().
    /// </summary>
    [TestMethod]
    public void ANilChannelCarriesAChainAndReportsItsHeadAsItsDirection()
    {
        channel<int> c = channel<int>.Nil(ChanCargo.Of([GoChanDir.Both, GoChanDir.Recv], null));

        Assert.AreEqual(GoChanDir.Both, c.Direction, "the head of [Both, Recv] is this channel's own direction");
        CollectionAssert.AreEqual(new[] { GoChanDir.Both, GoChanDir.Recv }, c.Cargo!.DirChain, "the whole chain survives on the value");
        Assert.IsTrue(c == nil, "Nil(cargo) is still the nil channel");
    }

    /// <summary>
    /// The element dims ride the SAME cargo — the ChanElemDims boundary, closed at the value.
    /// </summary>
    [TestMethod]
    public void TheElementArrayDimsRideTheSameCargo()
    {
        channel<array<nint>> c = new(0, ChanCargo.Of(null, [3]));

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.ChanCargoOfValue(c)!.ElemDims, "`chan [3]int`'s length is on the value");
        Assert.IsNull(c.Cargo!.DirChain, "no direction was stamped, so none is claimed");
        Assert.AreEqual(GoChanDir.Unstamped, c.Direction);
    }

    /// <summary>
    /// The type-erased route the bridge actually takes: a BOXED channel answers its cargo, and an
    /// unstamped one answers null rather than an empty cargo.
    /// </summary>
    [TestMethod]
    public void TheBridgeReadsTheCargoOffABoxedValueAndNullOffAnUnstampedOne()
    {
        object stamped = channel<int>.Nil(ChanCargo.Of([GoChanDir.Send, GoChanDir.Recv], null));
        object plain = new channel<int>(0);

        Assert.IsNotNull(GoReflect.ChanCargoOfValue(stamped));
        Assert.AreEqual(2, GoReflect.ChanCargoOfValue(stamped)!.DirChain!.Length);
        Assert.IsNull(GoReflect.ChanCargoOfValue(plain), "an unstamped channel carries no cargo object at all");
        Assert.AreEqual(GoChanDir.Send, GoReflect.ChanDirOfValue(stamped), "the scalar reader still answers the head");
    }

    /// <summary>
    /// The cargo belongs to the Go TYPE a value was born with, never to the channel object: two
    /// values over one core may carry different cargo, and equality reads the core alone.
    /// </summary>
    [TestMethod]
    public void WithCargoKeepsTheCoreAndEqualityIgnoresTheCargo()
    {
        channel<int> a = new(1);
        channel<int> b = a.WithCargo(ChanCargo.Of([GoChanDir.Send, GoChanDir.Both, GoChanDir.Recv], null));

        Assert.IsTrue(a == b, "same core, so the same Go channel");
        Assert.IsNull(a.Cargo);
        Assert.AreEqual(3, b.Cargo!.DirChain!.Length, "the cargo is stored as handed in; the descriptor authority normalizes it");
    }
}
