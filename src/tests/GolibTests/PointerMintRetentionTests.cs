using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using @unsafe = go.unsafe_package;

namespace GolibTests;

// The RETENTION half of the address contract, and the sibling of PinLifetimeAtTheNativeBoundaryTests:
// that file measures what the pin does when the BOX is not held, and states the contract — "the pin's
// lifetime is the BOX's reachability, not the referent's and not the address's". This file measures
// whether the thing the converter hands a native callee holds that box at all.
//
// THE DEFECT, measured 2026-09-04. `new @unsafe.Pointer(box)` has no constructor taking a box, so it
// binds the implicit `ж<T> → uintptr` conversion and lands in `Pointer(uintptr)`. That conversion IS
// the pin moment (EnsureStableAddress stores a GCHandle in the box's own field, and the address is
// registered with ManagedPointerTokens) — but the resulting Pointer keeps only the NUMBER, so the box
// carrying that pin is unreachable garbage the instant the mint returns, and the provenance table is
// WeakReference by design ("this table must never be the reason a box stays alive"). A collection
// landing inside a native callee's window then frees the pin and relocates the buffer while the
// callee is still writing through the address. Every converted syscall `read` and `write` was minted
// this way: sixteen concurrent TLS connections over the converted stack died SIGSEGV in five seconds,
// 3/3, and stopped once the box was held across the call.
//
// The fix is one door: `@unsafe.Pointer.FromPinnedBox`, which takes the address from the SAME
// conversion (so the pin and the provenance record are unchanged) and RETAINS the box, and which the
// converter now emits wherever it used to construct a Pointer directly from a box.
//
// The remaining sibling door, NAMED rather than folded in: `FromBox` retains its box but takes the
// address inside a `fixed` statement, so its number is transient by construction. That is a
// different shape from the one measured here (a live box with a possibly-stale number, rather than a
// stale-able buffer with no live box) and it is left to its own increment.
[TestClass]
public class PointerMintRetentionTests
{
    private static bool JitOptimizerDisabled(Assembly assembly) =>
        assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true;

    // The two mints, in their own non-inlined frames so no slot of the calling frame can root the box
    // for either arm — the mistake PinLifetimeAtTheNativeBoundaryTests records from its own first cut.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (@unsafe.Pointer pointer, WeakReference box) MintRetaining()
    {
        ж<long> box = new StandardBox<long>(0x5eed_1234L);
        return (@unsafe.Pointer.FromPinnedBox(box), new WeakReference(box));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (@unsafe.Pointer pointer, WeakReference box) MintBare()
    {
        ж<long> box = new StandardBox<long>(0x5eed_1234L);
        return (new @unsafe.Pointer(box), new WeakReference(box));
    }

    [TestMethod]
    public void TheRetainingMintCarriesItsBoxAndTheBareMintDoesNot()
    {
        // The contract, deterministically: no GC involved, so this arm cannot flake. RetainedSource is
        // what the bare-Pointer primitives resolve through, and it is what keeps the pin reachable.
        ж<long> box = new StandardBox<long>(42L);

        @unsafe.Pointer retaining = @unsafe.Pointer.FromPinnedBox(box);
        @unsafe.Pointer bare = new @unsafe.Pointer(box);

        Assert.AreSame(box, retaining.RetainedSource,
            "a Pointer minted through the retaining door must carry the box whose field holds the pin");
        Assert.IsNull(bare.RetainedSource,
            "POSITIVE CONTROL: constructing a Pointer directly from a box retains nothing — if this ever " +
            "starts passing, the defect this file guards has been fixed somewhere else and the guard above " +
            "no longer discriminates");
        Assert.AreEqual((nuint)(uintptr)box, (nuint)retaining.Value,
            "the retaining door must report the SAME address as the conversion the bare mint used — the pin " +
            "moment and the provenance record are unchanged, only the reference is added");

        GC.KeepAlive(box);
    }

    [TestMethod]
    public void ARetainedBoxSurvivesCollectionWhereTheBareMintsBoxDoesNot()
    {
        // The consequence of the contract above, with the collector actually run. At Debug a
        // non-optimizing frame roots every local for the method's life, so the bare arm's box would
        // survive for a reason that has nothing to do with the mint — the same configuration caveat
        // AliasOverlapRaceTests records, and the reason this arm reports Inconclusive there.
        if (JitOptimizerDisabled(typeof(PointerMintRetentionTests).Assembly))
        {
            Assert.Inconclusive("GolibTests is a JIT-optimizer-disabled (Debug) build: a non-optimizing frame " +
                "roots both arms' boxes for the method's life, so this arm measures the frame rather than the " +
                "mint — run GolibTests -c Release");
            return;
        }

        (@unsafe.Pointer retaining, WeakReference retainedBox) = MintRetaining();
        (@unsafe.Pointer bare, WeakReference bareBox) = MintBare();

        for (int i = 0; i < 3; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
        }

        bool bareSurvived = bareBox.IsAlive;

        Assert.IsTrue(retainedBox.IsAlive,
            "the retaining mint's box was collected while its Pointer was still reachable — the pin in that " +
            "box's field has been finalized and the address the Pointer carries names storage the collector " +
            "is free to move");

        GC.KeepAlive(retaining);
        GC.KeepAlive(bare);

        if (bareSurvived)
        {
            // Not a silent pass: if the bare arm's box also survives, this run did not stage the
            // difference (a conservative root, a runtime that did not collect), and the assertion above
            // is true for a reason other than the retention.
            Assert.Inconclusive("POSITIVE CONTROL did not stage: the BARE mint's box survived three forced " +
                "collections too, so this run cannot distinguish retention from a frame that rooted both");
        }
    }
}
