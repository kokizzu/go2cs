using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using abi = go.@internal.abi_package;

namespace GolibTests;

/// <summary>
/// Descriptor cargo, increment 2b: a channel's direction is carried as a PER-LEVEL CHAIN rather
/// than one scalar, because a scalar can only describe the outermost channel.
/// </summary>
/// <remarks>
/// These rows assert the interning KEY rather than the rendered string, because the string is the
/// symptom and the key is the mechanism: two spellings of one Go type that key differently intern
/// as two descriptors and split type identity, which is a defect no amount of correct rendering
/// repairs. The rendered five live in the ChanDirectionChain behavioral guard, compared against
/// `go run`; these are the half that guard cannot see.
/// </remarks>
[TestClass]
public class ChanDirChainTests
{
    private static string Key(params GoChanDir[]? chain) => abi.descriptorDimsKey(null, null, chain, null);

    /// <summary>
    /// `chan chan T` is all-Both, so it must key exactly as an unstamped descriptor does — the
    /// clause that keeps a constructed nested channel and a value-derived one on ONE descriptor.
    /// </summary>
    [TestMethod]
    public void AnAllBothChainKeysExactlyAsAnUnstampedDescriptor()
    {
        Assert.AreEqual(Key(null), Key(GoChanDir.Both, GoChanDir.Both),
            "`chan chan T` must key as the unstamped descriptor: an all-Both chain normalizes to absent.");
    }

    /// <summary>
    /// `chan&lt;- chan T` is [Send, Both]; the trailing Both says nothing its absence would not say,
    /// so it trims and the row keys exactly as the SCALAR era's [Send] did. This is the whole of
    /// the backward-compatibility claim, stated as an equality rather than as prose.
    /// </summary>
    [TestMethod]
    public void ATrailingBothTrimsSoTheKeyMatchesTheScalarEra()
    {
        Assert.AreEqual(Key(GoChanDir.Send), Key(GoChanDir.Send, GoChanDir.Both),
            "`chan<- chan T` must key as the scalar era's Send — otherwise every existing directional descriptor re-interns.");
    }

    /// <summary>
    /// `chan (&lt;-chan T)` is [Both, Recv]: the interior Both is LOAD-BEARING, because dropping it
    /// would spell `&lt;-chan chan T`, a different Go type. Both entries must survive.
    /// </summary>
    [TestMethod]
    public void AnInteriorBothIsKeptBecauseItIsPositional()
    {
        string nested = Key(GoChanDir.Both, GoChanDir.Recv);

        Assert.AreNotEqual(Key(GoChanDir.Recv), nested,
            "`chan (<-chan T)` must not key as `<-chan T` — dropping the interior Both changes the Go type.");
        Assert.AreNotEqual(Key(GoChanDir.Both), nested,
            "`chan (<-chan T)` must not key as a bare `chan T` — the element's direction is part of the type.");
    }

    /// <summary>
    /// THE NEGATIVE ARM. The trailing trim is not cosmetic: without it the two spellings of one Go
    /// type key DIFFERENTLY, which is the descriptor split TestChanOf's first assertion measured.
    /// This row proves the split is real at the key level, so the row above is testing something.
    /// </summary>
    [TestMethod]
    public void WithoutTheTrailingTrimTheTwoSpellingsOfOneTypeWouldSplit()
    {
        // Rendered WITHOUT normalization — what the key would be if the trim did not exist.
        string unnormalized = "@" + (byte)GoChanDir.Both + "," + (byte)GoChanDir.Both;

        Assert.AreNotEqual(Key(null), unnormalized,
            "the un-trimmed spelling really is a different key — which is why normalization has to happen at one authority.");
        Assert.AreEqual(Key(null), Key(GoChanDir.Both, GoChanDir.Both),
            "and normalization is what closes it.");
    }

    /// <summary>
    /// The COST row. A chain is a reference where the scalar was a byte, but <c>channel&lt;T&gt;</c>
    /// already carries a reference and pads to the word, so the chain is expected to be free. The
    /// number is asserted rather than argued: if a future field makes it grow, this row says so.
    /// </summary>
    [TestMethod]
    public void TheChannelValueDoesNotGrow()
    {
        int size = System.Runtime.CompilerServices.Unsafe.SizeOf<channel<int>>();

        Assert.AreEqual(16, size,
            $"channel<int> measured {size} B on this runtime; the direction cargo is expected to ride in the padding beside the core reference.");
    }
}
