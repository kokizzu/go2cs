// BoringCacheRegistryTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.golib;
using Δruntime = go.runtime_package;

namespace GolibTests;

/// <summary>
/// The standing guard over <see cref="BoringCaches"/> — the managed form of Go's
/// <c>runtime.registerCache</c> / <c>clearpools</c> boringcrypto arm, behind
/// <c>crypto/internal/boring/bcache</c>'s hand-owned <c>Register</c>.
/// </summary>
[TestClass]
public class BoringCacheRegistryTests
{
    // WHY THIS FILE EXISTS.
    //
    // golib.BoringCaches has exactly ONE consumer in the tree — bcache's hand-owned Register — and
    // that package's whole suite is a SINGLE verdict (TestCache), reachable only through the -tests
    // pipeline. So until this file, the entire mechanism was guarded by one roster row that nothing
    // at master runs: CNR is transpile-only, the corpus still compiles with the registry inert, and
    // no behavioral test reaches it. That is the same argument this project's csproj already makes
    // for sha3's xor.cs ("the ONLY guard the hand-owned sha3 xor.cs has") and for
    // crypto/internal/alias ("its banked row is a single verdict, so the semantic contract is
    // guarded nowhere else") — BoringCaches is that shape and did not have the guard.
    //
    // It is worse placed than either of those, because the load-bearing half is a WIRING line in
    // another file: runtime/managed_impl.cs's `golib.BoringCaches.ClearAll();` inside runtime.GC().
    // Delete that one line and every standing gate stays green. RuntimeGCDrainsTheRegistrySynchronously
    // is the arm that catches it, and it is the reason this file binds the converted runtime rather
    // than testing golib in isolation.
    //
    // EVERY ARM BELOW HAS BEEN MADE TO FAIL, one control each, before any of it was believed:
    // ClearAll emptied reddens 1/2/3/4/6 and leaves 5; Register overwriting instead of appending
    // reddens 4 alone; the sentinel never armed reddens 3 alone; the null refusal removed reddens 5
    // alone; the clearpools line deleted from managed_impl.cs reddens 6 alone; and the negative arm's
    // own subject deliberately over-enrolled reddens 2 alone. The fifth of those earned its keep —
    // it caught this file asserting the wrong property; see that arm's remarks.
    //
    // WHAT IS AND IS NOT ASSERTED. The contract is Go's: a registered cache is cleared at each
    // collection, and the cache is LOSSY by construction ("clients need to be able to cope with
    // cache entries disappearing"). So there is no assertion anywhere below that a REGISTERED cache
    // still holds anything — that would be flaky by construction, since any collection in the
    // process may legitimately empty it. The negative arm therefore uses an UNREGISTERED cache,
    // which nothing is entitled to touch; that is what makes it a stable negative rather than a
    // race. Registration is also permanent (Go's is too — boringCaches never shrinks), so every
    // arm below allocates its OWN holder and reads only that one.

    /// <summary>
    /// A stand-in for the <c>ptable</c> word a real <see cref="BoringCaches"/> registration clears.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT a <c>bcache.Cache</c>: the registry's contract is with the DELEGATE it is
    /// handed, so driving it with a delegate over a holder tests the registry rather than bcache's
    /// generics. The bcache side of the seam is what the banked row measures.
    /// </remarks>
    private sealed class Holder
    {
        private int m_entries;

        public void Fill(int entries) => Volatile.Write(ref m_entries, entries);

        public int Entries => Volatile.Read(ref m_entries);

        public bool IsEmpty => Entries == 0;

        // The shape of bcache's own Clear: drop everything, in one store.
        public void Clear() => Volatile.Write(ref m_entries, 0);
    }

    // Bound for the asynchronous arm. A finalizer is not scheduled by the caller, so the sentinel
    // may need more than one condemn/drain round to come around; the bound is a FLAKE guard, not a
    // weakening of the assertion, because a sentinel that never fires never clears at any number of
    // rounds. If this ever needs raising, the mechanism has changed and the arm should be re-read
    // rather than the number bumped.
    private const int SentinelRounds = 8;

    private static void ForceGen2Cycle()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// <c>ClearAll</c> runs a registered cache's clear — <c>clearpools</c>' boringcrypto arm, taken
    /// directly.
    /// </summary>
    [TestMethod]
    public void ClearAllClearsARegisteredCache()
    {
        Holder cache = new();

        BoringCaches.Register(cache.Clear);
        cache.Fill(1021);

        Assert.AreEqual(1021, cache.Entries, "the holder did not fill — the arm never reached its own subject.");

        BoringCaches.ClearAll();

        Assert.IsTrue(cache.IsEmpty,
            $"a REGISTERED cache still held {cache.Entries} entries after ClearAll(). The registry is not " +
            "invoking the delegate it was handed, which is the whole of the mechanism.");
    }

    /// <summary>
    /// The negative arm: a cache that never registered is not the registry's to clear.
    /// </summary>
    /// <remarks>
    /// This is what makes the positive arms mean something. A <c>ClearAll</c> that cleared some
    /// process-wide state — or a <c>Register</c> that quietly enrolled everything — would satisfy
    /// every other arm in this file and fail only here.
    /// </remarks>
    [TestMethod]
    public void AnUnregisteredCacheSurvivesClearAll()
    {
        Holder registered = new();
        Holder unregistered = new();

        // Register ONE of the two, so the registry is armed and demonstrably working in the same
        // breath — otherwise a no-op ClearAll would pass this arm for the wrong reason.
        BoringCaches.Register(registered.Clear);

        registered.Fill(7);
        unregistered.Fill(7);

        BoringCaches.ClearAll();

        Assert.IsTrue(registered.IsEmpty, "the registered control was not cleared — ClearAll did nothing at all.");

        Assert.AreEqual(7, unregistered.Entries,
            "an UNREGISTERED cache lost its entries to ClearAll. The registry is reaching past what was " +
            "handed to it; Go's clearpools walks boringCaches and nothing else.");
    }

    /// <summary>
    /// Every registered cache clears, not just the most recent one — the copy-on-write array
    /// APPENDS.
    /// </summary>
    [TestMethod]
    public void EveryRegisteredCacheClears()
    {
        Holder first = new();
        Holder second = new();
        Holder third = new();

        BoringCaches.Register(first.Clear);
        BoringCaches.Register(second.Clear);
        BoringCaches.Register(third.Clear);

        first.Fill(1);
        second.Fill(2);
        third.Fill(3);

        BoringCaches.ClearAll();

        // Named individually: a Register that OVERWRITES instead of appending leaves the earlier
        // ones full, and the message should say which rather than "something was not cleared".
        Assert.IsTrue(first.IsEmpty, $"the FIRST registration was not cleared ({first.Entries} entries) — " +
                                     "a later Register displaced it instead of appending.");
        Assert.IsTrue(second.IsEmpty, $"the SECOND registration was not cleared ({second.Entries} entries).");
        Assert.IsTrue(third.IsEmpty, $"the THIRD registration was not cleared ({third.Entries} entries).");
    }

    /// <summary>
    /// <c>Register</c> refuses a null clear rather than storing a hole that faults on the finalizer
    /// thread.
    /// </summary>
    /// <remarks>
    /// The failure this forbids is not the throw — it is a null landing in the array and being
    /// invoked from <c>Sentinel</c>'s finalizer, where <see cref="BoringCaches"/> swallows
    /// everything by design. A cache registered after the hole would then silently stop clearing,
    /// with nothing anywhere reporting it.
    /// </remarks>
    [TestMethod]
    public void RegisterRejectsANullClear()
    {
        Assert.ThrowsException<ArgumentNullException>(() => BoringCaches.Register(null!),
            "Register accepted a null clear. It would be stored, invoked from the finalizer, and the throw " +
            "swallowed — every registration behind it stops clearing, silently.");
    }

    /// <summary>
    /// The CADENCE arm: the resurrecting sentinel clears on a real collection, with no
    /// <c>ClearAll</c> call anywhere.
    /// </summary>
    /// <remarks>
    /// This is the half that makes the registry Go-faithful away from <c>runtime.GC()</c> — Go
    /// clears at the head of every cycle whether or not anyone asked for one. It is the only
    /// asynchronous arm here, and the only one that can distinguish a live sentinel from a
    /// registry that merely works when called.
    /// </remarks>
    [TestMethod]
    public void TheSentinelClearsOnACollection()
    {
        Holder cache = new();

        BoringCaches.Register(cache.Clear);

        // Fill AFTER registering, and never call ClearAll below — the collection has to be what
        // empties this, or the arm proves nothing.
        cache.Fill(512);
        Assert.AreEqual(512, cache.Entries, "the holder did not fill — the arm never reached its own subject.");

        int rounds = 0;

        while (rounds < SentinelRounds && !cache.IsEmpty)
        {
            rounds++;
            ForceGen2Cycle();
        }

        Console.WriteLine($"[BoringCaches] sentinel cleared after {rounds} gen2 round(s); " +
                          $"gen2 count now {GC.CollectionCount(GC.MaxGeneration)}.");

        Assert.IsTrue(cache.IsEmpty,
            $"a registered cache still held {cache.Entries} entries after {SentinelRounds} forced gen2 " +
            "collections with finalizers drained. The resurrecting sentinel is not firing — either it is " +
            "no longer armed by the first Register, or it stopped calling GC.ReRegisterForFinalize and so " +
            "fired exactly once, early, and never again.");
    }

    /// <summary>
    /// The WIRING arm: the converted <c>runtime.GC()</c> drains the registry before it returns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>runtime/managed_impl.cs</c> calls <see cref="BoringCaches.ClearAll"/> directly as
    /// <c>clearpools</c>' third arm, beside <c>poolcleanup</c> and unique's map cleanup. It has to:
    /// Go's <c>GC()</c> is documented to complete a full cycle, and bcache's suite reads the
    /// registered cache on the statement after <c>runtime.GC()</c> returns — a clear left to the
    /// sentinel's own cadence would not be there yet.
    /// </para>
    /// <para>
    /// Nothing else at master exercises that line. Removing it leaves the corpus compiling, CNR
    /// byte-identical and the behavioral suite green — this arm is that line's only guard.
    /// </para>
    /// <para>
    /// <b>What is asserted, and why it is not simply "the cache is empty".</b> Emptiness does NOT
    /// discriminate, and the control proved it: with the <c>ClearAll</c> line deleted this arm still
    /// passed, because <c>runtime.GC()</c> ends in
    /// <c>Collect(gen2) / WaitForPendingFinalizers / Collect(gen2)</c> and the registry's own
    /// sentinel is finalized inside that drain — so the cache comes back empty by the ASYNCHRONOUS
    /// path even with the synchronous one gone. The line is therefore a GUARANTEE rather than the
    /// only route, and the guarantee is what has to be asserted: Go clears at <c>gcStart</c>, before
    /// the cycle, so the clear must be synchronous with the call and ordered ahead of the
    /// collections — not something a finalizer gets around to.
    /// </para>
    /// <para>
    /// The discriminator is the THREAD. <c>ClearAll</c> at the head of <c>GC()</c> runs inline on
    /// the caller's thread; <c>Sentinel</c>'s clear runs on the finalizer thread, always. Comparing
    /// the thread the first clear observed against the caller's separates the two paths exactly,
    /// with no timing window and no dependence on collection counts.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void RuntimeGCDrainsTheRegistrySynchronously()
    {
        Holder cache = new();

        int callerThread = Environment.CurrentManagedThreadId;
        int clearedOnThread = 0;
        long clearedAtGen2 = -1;
        long gen2BeforeCall;

        // Record WHERE the first clear ran, then do the cache's own clear. Only the first is taken:
        // GC() may legitimately clear more than once (the head call, then the sentinel during the
        // drain), and it is the FIRST that says which path got there.
        BoringCaches.Register(() =>
        {
            if (Interlocked.CompareExchange(ref clearedOnThread, Environment.CurrentManagedThreadId, 0) == 0)
                Interlocked.Exchange(ref clearedAtGen2, GC.CollectionCount(GC.MaxGeneration));

            cache.Clear();
        });

        cache.Fill(4096);
        Assert.AreEqual(4096, cache.Entries, "the holder did not fill — the arm never reached its own subject.");

        gen2BeforeCall = GC.CollectionCount(GC.MaxGeneration);

        // The converted runtime.GC(), not GC.Collect: the clearpools wiring is reached only here.
        Δruntime.GC();

        Console.WriteLine($"[BoringCaches] runtime.GC(): caller thread {callerThread}, first clear on thread " +
                          $"{Volatile.Read(ref clearedOnThread)}; gen2 {gen2BeforeCall} at call, " +
                          $"{Interlocked.Read(ref clearedAtGen2)} at clear, " +
                          $"{GC.CollectionCount(GC.MaxGeneration)} on return.");

        Assert.AreNotEqual(0, Volatile.Read(ref clearedOnThread),
            "runtime.GC() returned without the registered clear running at all — neither clearpools' " +
            "boringcrypto arm nor the registry's sentinel reached it.");

        Assert.IsTrue(cache.IsEmpty,
            $"a registered cache still held {cache.Entries} entries the statement after runtime.GC() returned.");

        // THE discriminating assertion. See the remarks: emptiness alone passes with the wiring gone.
        Assert.AreEqual(callerThread, Volatile.Read(ref clearedOnThread),
            "the registered clear first ran on the FINALIZER thread, not on the caller's — so it was the " +
            "registry's sentinel that got there during runtime.GC()'s own drain, and the synchronous " +
            "`golib.BoringCaches.ClearAll();` arm of clearpools is missing from runtime/managed_impl.cs. " +
            "The cache ends up empty either way, which is why this arm asserts the thread rather than the " +
            "emptiness: Go clears at gcStart, ahead of the cycle, not whenever a finalizer is scheduled.");

        Assert.AreEqual(gen2BeforeCall, Interlocked.Read(ref clearedAtGen2),
            "the clear ran on the caller's thread but only AFTER runtime.GC() had already forced a collection. " +
            "clearpools runs at the HEAD of the cycle; a clear ordered behind the collections is not that.");
    }
}
