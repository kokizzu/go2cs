using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using Δruntime = go.runtime_package;

namespace GolibTests;

/// <summary>
/// The acceptance row for <c>runtime.AddCleanup</c> — the Go 1.24 API whose auto conversion was a
/// SILENT NO-OP (COORD ruling <c>c58b4c01d</c>, finding C1 <c>f9f41e8d8</c> §3).
/// </summary>
/// <remarks>
/// <para>
/// WHAT WENT WRONG IN THE AUTO, and why a row exists for an API nothing calls yet.
/// <c>mcleanup.go</c>'s <c>AddCleanup</c> calls <c>createfing()</c>, which in the converted corpus
/// started the CONVERTED <c>runfinq</c> — the body <c>mfinal.cs</c>'s own header declares dead. Taken
/// as a plain auto, <c>runtime.AddCleanup</c> would compile, return a <c>Cleanup</c>, and never run
/// it: no throw, no diagnostic, nothing in the system able to say why. "Compiling is not
/// correctness" has no sharper instance, so the hand-own that replaces it is not believed without a
/// row that would have gone red on the auto.
/// </para>
/// <para>
/// ⚠ WHAT THESE ARMS CANNOT SEE, STATED HERE RATHER THAN IMPLIED BY THEIR PASSING. Arm 1 proves a
/// cleanup body reaches the live runner, but it CANNOT prove that <c>AddCleanup</c> is what started
/// that runner: MSTest runs one process, and any <c>SetFinalizer</c> anywhere in this assembly —
/// <c>FinalizerDispatchTests</c> runs several — has already called
/// <c>GoFinalizerQueue.EnsureRunner()</c> by then. So on the OLD <c>createfing</c> these arms would
/// still pass whenever a finalizer test ran first, and go red only in a process that uses cleanups
/// alone. The rewire's guard is therefore arm 1 PLUS the fact that <c>createfing</c> has exactly one
/// body; closing the gap properly needs a single-test process or a source-level guard, and is
/// offered as one rather than quietly assumed away.
/// </para>
/// <para>
/// ISOLATION follows <c>FinalizerDispatchTests</c>: every arm mints and drops its referent on a
/// DEDICATED THREAD that is joined before anything is measured, so no caller frame can root the box
/// and no two arms can contaminate one another through a shared stack.
/// </para>
/// </remarks>
[TestClass]
public sealed class CleanupDispatchTests
{
    private const int JoinWaitMs = 60_000;

    // How long an arm waits for a queued cleanup body to run. Cleanups are handed to the fing
    // analogue rather than invoked inline, so "collected" and "cleanup ran" are two events and the
    // second is asynchronous — polling for it is the difference between a real reading and a race
    // that passes on a fast host.
    private const int RanWaitMs = 30_000;

    // ------------------------------------------------------------------------------------------
    // Shared mint/drop machinery. Nothing here may leak the referent into a caller's frame.
    // ------------------------------------------------------------------------------------------

    // Built in its own method so its display class captures ONLY the observation cell — never the
    // box. Go's own AddCleanup contract makes the same demand of a caller: a cleanup that closes
    // over ptr keeps ptr alive and can never run.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<string> RecordingCleanup(List<string> seen) =>
        arg => { lock (seen) seen.Add(arg); };

    // Mints `new(*int)` in the converted shape, attaches `count` cleanups each carrying its own
    // argument, and returns only a WeakReference plus the handles. The box is never returned.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MintAttachDrop(Action<string> cleanup, int count, bool stopThem, List<Δruntime.Cleanup> handles)
    {
        ж<ж<nint>> garbage = @new<ж<nint>>();

        for (int i = 0; i < count; i++)
            handles.Add(Δruntime.AddCleanup(garbage, cleanup, $"arg{i}"));

        if (stopThem)
        {
            // Go: "To guarantee that Stop removes the cleanup function, the caller must ensure that
            // the pointer that was passed to AddCleanup is reachable across the call to Stop." It is
            // — `garbage` is live on this frame until the line below.
            foreach (Δruntime.Cleanup handle in handles)
                handle.Stop();
        }

        WeakReference weak = new(garbage, trackResurrection: false);
        garbage = default!;
        return weak;
    }

    private static WeakReference MintOnDedicatedThread(Action<string> cleanup, int count, bool stopThem, List<Δruntime.Cleanup> handles)
    {
        WeakReference? weak = null;
        Thread minter = new(() => weak = MintAttachDrop(cleanup, count, stopThem, handles))
        {
            IsBackground = true,
            Name = "cleanup-minter"
        };
        minter.Start();
        Assert.IsTrue(minter.Join(JoinWaitMs), "the minting thread did not finish");
        Assert.IsNotNull(weak, "the minting thread produced no WeakReference — the arm measured nothing");
        return weak!;
    }

    // Collects, then polls for the queued bodies. Returns what actually ran.
    private static List<string> CollectAndDrain(List<string> seen, int expected)
    {
        Δruntime.GC();
        Δruntime.GC();

        // Poll rather than sleep-once: a fixed sleep either wastes the budget or reads a race.
        for (int waited = 0; waited < RanWaitMs; waited += 25)
        {
            lock (seen)
            {
                if (seen.Count >= expected)
                    break;
            }

            Thread.Sleep(25);
        }

        lock (seen)
            return new List<string>(seen);
    }

    // ------------------------------------------------------------------------------------------
    // ARM 1 — the row that would have gone RED on the auto conversion.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm1_CleanupRunsWithItsArgumentAfterTheObjectDies()
    {
        List<string> seen = new();
        List<Δruntime.Cleanup> handles = new();
        WeakReference weak = MintOnDedicatedThread(RecordingCleanup(seen), count: 1, stopThem: false, handles);

        List<string> ran = CollectAndDrain(seen, expected: 1);

        Console.WriteLine($"[cleanup:arm1] IsAlive={weak.IsAlive} ran=[{string.Join(",", ran)}]");

        Assert.IsFalse(weak.IsAlive,
            "ARM 1: the object is STILL ROOTED after two runtime.GC() calls — an AddCleanup registration " +
            "is holding the very object whose death is supposed to trigger it, so the cleanup can never run.");
        CollectionAssert.AreEqual(new[] { "arg0" }, ran,
            "ARM 1: the object was collected but its cleanup never ran with its argument — this is the " +
            "silent no-op the auto conversion would have shipped (AddCleanup -> createfing -> the dead runfinq).");
    }

    // ------------------------------------------------------------------------------------------
    // ARM 2 — the differential control WITHOUT which arm 1 proves nothing.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm2_StopPreventsTheCleanupFromRunning()
    {
        List<string> seen = new();
        List<Δruntime.Cleanup> handles = new();
        WeakReference weak = MintOnDedicatedThread(RecordingCleanup(seen), count: 1, stopThem: true, handles);

        // Give a cancelled cleanup every chance to run before concluding it did not.
        Δruntime.GC();
        Δruntime.GC();
        Thread.Sleep(250);
        List<string> ran = CollectAndDrain(seen, expected: 1);

        Console.WriteLine($"[cleanup:arm2] IsAlive={weak.IsAlive} ran=[{string.Join(",", ran)}]");

        Assert.IsFalse(weak.IsAlive, "ARM 2: a STOPPED cleanup is still rooting the object.");
        Assert.AreEqual(0, ran.Count,
            "ARM 2: Cleanup.Stop() did not cancel the cleanup — so arm 1's green says only that SOMETHING " +
            "runs cleanups, not that this mechanism is under control.");
    }

    // ------------------------------------------------------------------------------------------
    // ARM 3 — Go's "multiple cleanups may be attached to the same pointer".
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm3_EveryCleanupOnOneObjectRuns()
    {
        List<string> seen = new();
        List<Δruntime.Cleanup> handles = new();
        WeakReference weak = MintOnDedicatedThread(RecordingCleanup(seen), count: 3, stopThem: false, handles);

        List<string> ran = CollectAndDrain(seen, expected: 3);
        ran.Sort(StringComparer.Ordinal);

        Console.WriteLine($"[cleanup:arm3] IsAlive={weak.IsAlive} ran=[{string.Join(",", ran)}]");

        Assert.IsFalse(weak.IsAlive, "ARM 3: three cleanups on one object left it rooted.");
        CollectionAssert.AreEqual(new[] { "arg0", "arg1", "arg2" }, ran,
            "ARM 3: not every cleanup attached to the object ran. Go specifies no ORDER among them — this " +
            "arm sorts for exactly that reason — but it does specify that multiple may be attached, and a " +
            "one-value-per-key registry silently keeps only the last.");
    }

    // ------------------------------------------------------------------------------------------
    // ARM 4 — the guard Go itself provides, and the one place we are deliberately WIDER than Go.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm4_AddCleanupPanicsWhenArgIsThePointer()
    {
        ж<ж<nint>> garbage = @new<ж<nint>>();

        PanicException panic = Assert.ThrowsException<PanicException>(
            () => Δruntime.AddCleanup(garbage, (ж<ж<nint>> _) => { }, garbage),
            "ARM 4: AddCleanup accepted arg == ptr. Go panics here because such a cleanup can NEVER run — " +
            "arg keeps the object alive — and accepting it is how a caller gets a cleanup that silently " +
            "never fires, which is the same class of defect as the auto conversion this row exists for.");

        StringAssert.Contains(panic.ToString(), "ptr is equal to arg",
            "ARM 4: it panicked, but not with Go's message — a reader hitting this must be able to find " +
            "Go's own documentation of the rule from the text they see.");

        Δruntime.KeepAlive(garbage);
    }

    // ------------------------------------------------------------------------------------------
    // ARM 5 — a nil pointer is refused, as Go refuses it.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm5_AddCleanupPanicsOnANilPointer()
    {
        PanicException panic = Assert.ThrowsException<PanicException>(
            () => Δruntime.AddCleanup(default(ж<ж<nint>>)!, (string _) => { }, "unused"),
            "ARM 5: AddCleanup accepted a nil ptr.");

        StringAssert.Contains(panic.ToString(), "ptr is nil",
            "ARM 5: refused, but not with Go's message.");
    }

    // ------------------------------------------------------------------------------------------
    // ARMS 6-9 -- ORDER AGAINST A FINALIZER (A11). Go: "If ptr has both a cleanup and a finalizer,
    // the cleanup will only run once it has been finalized and becomes unreachable without an
    // associated finalizer." runtime's TestCleanupAfterFinalizer reads it as: the first GC runs the
    // finalizer only, the second runs the cleanup.
    //
    // The finalizer RESURRECTS the object into a holder the arm controls, so "unreachable again" is
    // the arm's decision rather than whichever collection happens next. That makes the reading
    // deterministic in both directions: while the holder is set, no collection may run the cleanup.
    // ------------------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<ж<ж<nint>>> ResurrectingFinalizer(List<string> seen, StrongBox<object?> holder) =>
        x =>
        {
            lock (seen) seen.Add("finalizer");
            holder.Value = x;
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MintCleanupAndFinalizer(List<string> seen, StrongBox<object?> holder, bool finalizerFirst, bool clearFinalizer, List<Δruntime.Cleanup> handles)
    {
        ж<ж<nint>> garbage = @new<ж<nint>>();
        Action<ж<ж<nint>>> finalizer = ResurrectingFinalizer(seen, holder);

        if (finalizerFirst)
            Δruntime.SetFinalizer(garbage.OrTypedNil(), finalizer);

        handles.Add(Δruntime.AddCleanup(garbage, RecordingCleanup(seen), "cleanup"));

        if (!finalizerFirst)
            Δruntime.SetFinalizer(garbage.OrTypedNil(), finalizer);

        if (clearFinalizer)
            Δruntime.SetFinalizer(garbage.OrTypedNil(), default(object)!);

        garbage = default!;
    }

    private static void MintBothOnDedicatedThread(List<string> seen, StrongBox<object?> holder, bool finalizerFirst, bool clearFinalizer, List<Δruntime.Cleanup> handles)
    {
        Thread minter = new(() => MintCleanupAndFinalizer(seen, holder, finalizerFirst, clearFinalizer, handles))
        {
            IsBackground = true,
            Name = "cleanup-finalizer-minter"
        };
        minter.Start();
        Assert.IsTrue(minter.Join(JoinWaitMs), "the minting thread did not finish");
    }

    private static List<string> Snapshot(List<string> seen)
    {
        lock (seen)
            return new List<string>(seen);
    }

    // The first collection: the finalizer runs and resurrects the object; the cleanup must not run.
    private static void FirstDeathRunsOnlyTheFinalizer(List<string> seen, StrongBox<object?> holder, string arm)
    {
        List<string> ran = CollectAndDrain(seen, expected: 1);

        // Give a cleanup queued at the same death every chance to show itself before concluding.
        Thread.Sleep(250);
        ran = Snapshot(seen);

        Console.WriteLine($"[cleanup:{arm}] first death: resurrected={holder.Value is not null} ran=[{string.Join(",", ran)}]");

        Assert.IsNotNull(holder.Value, $"{arm}: the finalizer never ran, so the arm measured nothing");
        CollectionAssert.AreEqual(new[] { "finalizer" }, ran,
            $"{arm}: the cleanup ran at the SAME death as the finalizer. Go runs it only once the object " +
            "has been finalized AND becomes unreachable again; here the object is still reachable (the " +
            "finalizer resurrected it), so the cleanup ran on a live object.");
    }

    [TestMethod]
    public void Arm6_ACleanupWaitsForTheFinalizerAndTheNextDeath()
    {
        List<string> seen = new();
        StrongBox<object?> holder = new();
        List<Δruntime.Cleanup> handles = new();

        // runtime's TestCleanupAfterFinalizer order: AddCleanup, then SetFinalizer.
        MintBothOnDedicatedThread(seen, holder, finalizerFirst: false, clearFinalizer: false, handles);
        FirstDeathRunsOnlyTheFinalizer(seen, holder, "arm6");

        holder.Value = null;
        List<string> ran = CollectAndDrain(seen, expected: 2);

        Console.WriteLine($"[cleanup:arm6] second death: ran=[{string.Join(",", ran)}]");

        CollectionAssert.AreEqual(new[] { "finalizer", "cleanup" }, ran,
            "ARM 6: once the resurrected object became unreachable again, its cleanup did not run -- " +
            "holding it for the finalizer must defer it, never drop it.");
    }

    [TestMethod]
    public void Arm7_TheOrderHoldsWhenTheFinalizerIsRegisteredFirst()
    {
        List<string> seen = new();
        StrongBox<object?> holder = new();
        List<Δruntime.Cleanup> handles = new();

        MintBothOnDedicatedThread(seen, holder, finalizerFirst: true, clearFinalizer: false, handles);
        FirstDeathRunsOnlyTheFinalizer(seen, holder, "arm7");

        holder.Value = null;
        List<string> ran = CollectAndDrain(seen, expected: 2);

        Console.WriteLine($"[cleanup:arm7] second death: ran=[{string.Join(",", ran)}]");

        CollectionAssert.AreEqual(new[] { "finalizer", "cleanup" }, ran,
            "ARM 7: with SetFinalizer before AddCleanup the cleanup did not wait for the finalizer, or was lost.");
    }

    [TestMethod]
    public void Arm8_StopBetweenTheFinalizerAndTheNextDeathCancelsTheCleanup()
    {
        List<string> seen = new();
        StrongBox<object?> holder = new();
        List<Δruntime.Cleanup> handles = new();

        MintBothOnDedicatedThread(seen, holder, finalizerFirst: false, clearFinalizer: false, handles);
        FirstDeathRunsOnlyTheFinalizer(seen, holder, "arm8");

        // The cleanup is not queued yet -- the object is reachable again -- so Go's Stop removes it,
        // and the pointer IS reachable across the call, which is the condition Go's doc sets.
        handles[0].Stop();
        holder.Value = null;

        Δruntime.GC();
        Δruntime.GC();
        Thread.Sleep(250);
        List<string> ran = CollectAndDrain(seen, expected: 2);

        Console.WriteLine($"[cleanup:arm8] after Stop and the second death: ran=[{string.Join(",", ran)}]");

        CollectionAssert.AreEqual(new[] { "finalizer" }, ran,
            "ARM 8: Stop on a cleanup held across its object's finalizer did not cancel it -- the deferred " +
            "cleanup lost its handle when it was re-attached.");
    }

    [TestMethod]
    public void Arm9_AClearedFinalizerReleasesTheCleanupAtTheFirstDeath()
    {
        List<string> seen = new();
        StrongBox<object?> holder = new();
        List<Δruntime.Cleanup> handles = new();

        // A GUARD, green before and after: SetFinalizer(obj, nil) leaves no finalizer to wait for.
        MintBothOnDedicatedThread(seen, holder, finalizerFirst: false, clearFinalizer: true, handles);
        List<string> ran = CollectAndDrain(seen, expected: 1);

        Console.WriteLine($"[cleanup:arm9] ran=[{string.Join(",", ran)}]");

        Assert.IsNull(holder.Value, "ARM 9: a cleared finalizer ran.");
        CollectionAssert.AreEqual(new[] { "cleanup" }, ran,
            "ARM 9: with the finalizer cleared, the cleanup must run at the object's first death.");
    }

    // ------------------------------------------------------------------------------------------
    // ARM 10 -- the object's OWN second death, which no earlier arm's leftover can mask.
    //
    // Arms 6 and 7 read only whether the cleanup ran, and that depends on the finalizer runner
    // releasing the resurrected object. On windows the runner's frame kept the LAST item it
    // dispatched (the finalizer's, whose target is this object) reachable until another item
    // passed through: arm 6 read RED, and arm 7 passed only because arm 6's stranded cleanup came
    // due during arm 7's collections and flushed the runner. This arm is arm 7's order read on the
    // object itself: after the holder drops it, the next collections must see it dead, with no
    // other item through the runner in between.
    // ------------------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TakeWeakAndDrop(StrongBox<object?> holder)
    {
        WeakReference weak = new(holder.Value, trackResurrection: false);
        holder.Value = null;
        return weak;
    }

    [TestMethod]
    public void Arm10_TheResurrectedObjectDiesAtItsNextCollection()
    {
        List<string> seen = new();
        StrongBox<object?> holder = new();
        List<Δruntime.Cleanup> handles = new();

        MintBothOnDedicatedThread(seen, holder, finalizerFirst: true, clearFinalizer: false, handles);
        FirstDeathRunsOnlyTheFinalizer(seen, holder, "arm10");

        WeakReference weak = TakeWeakAndDrop(holder);
        List<string> ran = CollectAndDrain(seen, expected: 2);

        Console.WriteLine($"[cleanup:arm10] second death: objectAlive={weak.IsAlive} ran=[{string.Join(",", ran)}]");

        Assert.IsFalse(weak.IsAlive,
            "ARM 10: the resurrected object is still alive after the holder dropped it and two collections " +
            "ran. Something the finalizer runner kept from the last item it dispatched is rooting it.");
        CollectionAssert.AreEqual(new[] { "finalizer", "cleanup" }, ran,
            "ARM 10: the object died, but its cleanup did not run.");
    }
}
