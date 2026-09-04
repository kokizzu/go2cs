using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using Δruntime = go.runtime_package;

namespace GolibTests;

/// <summary>
/// Q23 — why <c>runtime/pprof</c>'s <c>TestGoroutineCounts</c> never sees its finalizer run.
/// </summary>
/// <remarks>
/// <para>
/// WHAT THIS MEASURES. Go's <c>runtime/pprof/pprof_test.go</c> (1451-1460 in go1.23.12) blocks the
/// test goroutine on a finalizer BEFORE it ever calls <c>Lookup("goroutine")</c>:
/// </para>
/// <code>
///   garbage := new(*int)
///   fingReady := make(chan struct{})
///   runtime.SetFinalizer(garbage, func(v **int) {
///       Do(context.Background(), Labels("fing-label", "fing-value"), func(ctx context.Context) {
///           close(fingReady)
///           &lt;-c                     // c is closed only at the END of the test
///       })
///   })
///   garbage = nil
///   for i := 0; i &lt; 2; i++ { runtime.GC() }
///   &lt;-fingReady
/// </code>
/// <para>
/// Under the converted runtime it never completes: C1 measured the 25-minute package deadline at
/// Release + <c>DOTNET_TieredCompilation=0</c> AND at Debug, a one-axis A/B with identical streams
/// (mailbox <c>0641cfe4e</c>, <c>e8fe835df</c>). Equal behaviour on both sides of the JIT-tier axis
/// rules out the codegen-liveness class, so the next step is an instrument rather than a fifth
/// hypothesis. Three mechanisms remain, and the arms below separate them:
/// </para>
/// <list type="number">
///   <item>REGISTRATION — <c>runtime.SetFinalizer</c> roots the target, so it never becomes
///   collectible and no finalizer is ever due (arms 1/2/3).</item>
///   <item>DISPATCH — the target IS collected and the Go finalizer DOES start, but the converted
///   <c>runtime.GC()</c> cannot return while it is running (arm 4).</item>
///   <item>The converted test's own emission holds <c>garbage</c> in a display class — a converter
///   finding rather than a golib one, which arms 1/2 exonerate or implicate by elimination.</item>
/// </list>
///
/// <para>
/// PREDICTION, ON RECORD BEFORE THE FIRST RUN (committed with this file; results land as a
/// follow-up commit, never by editing this block). Reasoning stated so a wrong prediction is
/// informative:
/// </para>
/// <list type="bullet">
///   <item><b>Arm 1 — DEAD, finalizer RAN.</b> <c>SetFinalizer</c> keys a
///   <c>ConditionalWeakTable</c> on <c>ReferentOf(obj)</c>, which for a <c>StandardBox</c> is the box
///   itself, with a sentinel value that strong-references that same box. A dependent handle keeps the
///   VALUE alive while the KEY is alive and not the reverse, so the value→key edge is exactly the
///   cycle dependent handles exist to tolerate. Nothing else should root the box once the minting
///   thread's stack is gone. So: registration is NOT the mechanism.</item>
///   <item><b>Arm 2 — DEAD, finalizer NEVER RAN.</b> <c>SetFinalizer(obj, nil)</c> cancels and
///   removes; the arm is the differential control that makes arm 1's reading mean something.</item>
///   <item><b>Arm 3 — value collected, key collectible.</b> The same value→key shape modelled with a
///   local <c>ConditionalWeakTable</c> and no corpus code at all: a second derivation of arm 1's
///   premise that does not run through the thing under test.</item>
///   <item><b>Arm 4 — the finalizer ENTERS, and <c>runtime.GC()</c> DOES NOT RETURN while it is
///   still running.</b> This is the predicted mechanism. The converted <c>runtime.GC()</c>
///   (<c>runtime/managed_impl.cs</c>) calls <c>System.GC.WaitForPendingFinalizers()</c>, and the
///   Go finalizer body runs INLINE on the CLR finalizer thread inside
///   <c>~GoFinalizerSentinel</c>. Go's <c>runtime.GC()</c> makes no such promise — it completes a GC
///   cycle and returns, while finalizers run concurrently on the <c>fing</c> goroutine, which is
///   precisely what lets Go's test reach <c>&lt;-fingReady</c> while its finalizer sits blocked on
///   <c>&lt;-c</c>. Here the first <c>runtime.GC()</c> can never return, so the test never reaches
///   the channel receive at all: a DEADLOCK between the caller and its own finalizer.</item>
///   <item><b>Arm 5 — the literal transcription hangs, and the watchdog fires.</b> Same shape as
///   arm 4 through the converted <c>channel</c> primitives, i.e. the row itself in miniature.</item>
/// </list>
///
/// <para>
/// ISOLATION. GC/liveness probes contaminate one another when they share a frame, so every arm mints
/// and drops its referent on a DEDICATED THREAD that is joined before anything is measured — the
/// stack that held the box is gone, which is stronger than a non-inlined helper. The finalizer
/// delegate is built in its own method so the box cannot be hoisted into the same display class as
/// the captured counter (C# captures a method's variables into ONE display class; the converted
/// emission has the same property, which is why arm 1 must not accidentally model a capture the row
/// does not have).
/// </para>
/// </remarks>
[TestClass]
public class FinalizerDispatchTests
{
    // Bounded so a regression is a FAILURE rather than a hung suite. Generous against a loaded
    // host: every arm's expected wall is milliseconds.
    // Comfortably above the converted runtime.GC()'s own bounded finalizer-drain budget
    // (GoFinalizerQueue.DrainBudgetMs, 10 s) and infinitely below "never", which is what an
    // unbounded wait costs — the separation the arm-4 guard is measuring.
    private const int GcWatchdogMs = 60_000;
    private const int EnterWaitMs = 60_000;

    // ----------------------------------------------------------------------------------------
    // Shared mint/drop machinery. Nothing here may leak the referent into a caller's frame.
    // ----------------------------------------------------------------------------------------

    // Built in its own method so its display class captures ONLY `ran` — never the box. Go's test
    // has the same property (its closure captures fingReady and c, not garbage).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<ж<ж<nint>>> NonBlockingFinalizer(StrongBox<int> ran) =>
        _ => Interlocked.Increment(ref ran.Value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<ж<ж<nint>>> GatedFinalizer(ManualResetEventSlim entered, ManualResetEventSlim release) =>
        _ =>
        {
            entered.Set();
            release.Wait();
        };

    // Mints the converted shape of Go's `garbage := new(*int)` — @new<ж<nint>>() is a
    // StandardBox<ж<nint>>, the `**int` a corpus emission produces — registers `finalizer` on it
    // through the CONVERTED runtime API, and returns only a WeakReference. The box is never
    // returned, never stored, and the frame that held it is destroyed when the thread exits.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MintRegisterDrop(object? finalizer, bool clearAfterRegister)
    {
        ж<ж<nint>> garbage = @new<ж<nint>>();
        if (finalizer is not null)
            Δruntime.SetFinalizer(garbage.OrTypedNil(), finalizer);

        if (clearAfterRegister)
            Δruntime.SetFinalizer(garbage.OrTypedNil(), default(object)!);

        WeakReference weak = new(garbage, trackResurrection: false);
        garbage = default!;
        return weak;
    }

    // Runs MintRegisterDrop on a thread that then EXITS, so no stack anywhere still holds the box.
    private static WeakReference MintOnDedicatedThread(object? finalizer, bool clearAfterRegister)
    {
        WeakReference? weak = null;
        Thread minter = new(() => weak = MintRegisterDrop(finalizer, clearAfterRegister))
        {
            IsBackground = true,
            Name = "q23-minter"
        };
        minter.Start();
        Assert.IsTrue(minter.Join(EnterWaitMs), "the minting thread did not finish");
        Assert.IsNotNull(weak, "the minting thread produced no WeakReference — the arm measured nothing");
        return weak!;
    }

    // ----------------------------------------------------------------------------------------
    // ARM 1 / ARM 2 — is the ROOT the registration?
    // ----------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm1_RegisteredReferentIsCollectedAndItsFinalizerRuns()
    {
        StrongBox<int> ran = new(0);
        WeakReference weak = MintOnDedicatedThread(NonBlockingFinalizer(ran), clearAfterRegister: false);

        // The converted runtime.GC(), exactly as the row calls it.
        Δruntime.GC();
        Δruntime.GC();

        Console.WriteLine($"[q23:arm1] IsAlive={weak.IsAlive} finalizerRuns={Volatile.Read(ref ran.Value)}");

        Assert.IsFalse(weak.IsAlive,
            "ARM 1: the registered referent is STILL ROOTED after two runtime.GC() calls — the root is the " +
            "registration (SetFinalizer's ConditionalWeakTable entry), not the caller's frame.");
        Assert.AreEqual(1, Volatile.Read(ref ran.Value),
            "ARM 1: the referent was collected but its Go finalizer never ran — the defect is in the " +
            "sentinel's dispatch, not in retention.");
    }

    [TestMethod]
    public void Arm2_ClearedRegistrationCollectsAndNeverRunsTheFinalizer()
    {
        StrongBox<int> ran = new(0);
        WeakReference weak = MintOnDedicatedThread(NonBlockingFinalizer(ran), clearAfterRegister: true);

        Δruntime.GC();
        Δruntime.GC();

        Console.WriteLine($"[q23:arm2] IsAlive={weak.IsAlive} finalizerRuns={Volatile.Read(ref ran.Value)}");

        Assert.IsFalse(weak.IsAlive,
            "ARM 2: a CLEARED registration still roots the referent — SetFinalizer(obj, nil) is not releasing it.");
        Assert.AreEqual(0, Volatile.Read(ref ran.Value),
            "ARM 2: SetFinalizer(obj, nil) did not cancel the finalizer.");
    }

    // ----------------------------------------------------------------------------------------
    // ARM 3 — the value→key tolerance, derived WITHOUT the code under test.
    // ----------------------------------------------------------------------------------------

    private sealed class KeyHoldingSentinel
    {
        // The shape mfinal.cs uses: the CWT value strong-references its own key.
        private readonly object m_key;

        public KeyHoldingSentinel(object key) => m_key = key;

        public object Key => m_key;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MintCwtCycle(ConditionalWeakTable<object, KeyHoldingSentinel> table)
    {
        object key = new();
        table.Add(key, new KeyHoldingSentinel(key));
        WeakReference weak = new(key, trackResurrection: false);
        key = null!;
        return weak;
    }

    [TestMethod]
    public void Arm3_ConditionalWeakTableToleratesTheValueToKeyCycle()
    {
        ConditionalWeakTable<object, KeyHoldingSentinel> table = new();

        WeakReference? weak = null;
        Thread minter = new(() => weak = MintCwtCycle(table)) { IsBackground = true, Name = "q23-cwt-minter" };
        minter.Start();
        Assert.IsTrue(minter.Join(EnterWaitMs), "the CWT minting thread did not finish");

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        Console.WriteLine($"[q23:arm3] IsAlive={weak!.IsAlive}");

        Assert.IsFalse(weak.IsAlive,
            "ARM 3: a ConditionalWeakTable value that strong-references its own key kept the key alive — " +
            "the dependent-handle tolerance mfinal.cs relies on does not hold on this runtime, and the " +
            "registration shape itself is the defect.");
    }

    // ----------------------------------------------------------------------------------------
    // ARM 4 — the DISPATCH axis, isolated from converted channels.
    // ----------------------------------------------------------------------------------------

    [TestMethod]
    public void Arm4_RuntimeGcDoesNotReturnWhileAGoFinalizerIsStillRunning()
    {
        using ManualResetEventSlim entered = new(false);
        using ManualResetEventSlim release = new(false);

        try
        {
            WeakReference weak = MintOnDedicatedThread(GatedFinalizer(entered, release), clearAfterRegister: false);
            Assert.IsNotNull(weak);

            ManualResetEventSlim gcReturned = new(false);
            Stopwatch sw = Stopwatch.StartNew();
            Thread collector = new(() =>
            {
                Δruntime.GC();
                gcReturned.Set();
            })
            { IsBackground = true, Name = "q23-collector" };
            collector.Start();

            bool ran = entered.Wait(EnterWaitMs);
            bool returnedWhileBlocked = gcReturned.Wait(GcWatchdogMs);
            long blockedMs = sw.ElapsedMilliseconds;

            Console.WriteLine($"[q23:arm4] finalizerEntered={ran} gcReturnedWhileFinalizerBlocked={returnedWhileBlocked} waitedMs={blockedMs}");

            Assert.IsTrue(ran,
                "ARM 4: the Go finalizer never started, so this arm cannot speak to dispatch — re-read arms 1-3.");

            // THE GUARD. Go's runtime.GC() completes a GC cycle and returns; finalizers run
            // concurrently on the fing goroutine. A converted runtime.GC() that cannot return
            // while a Go finalizer is running deadlocks against any finalizer that waits on its
            // caller — which is exactly what runtime/pprof's TestGoroutineCounts does.
            Assert.IsTrue(returnedWhileBlocked,
                $"ARM 4: runtime.GC() did not return within {GcWatchdogMs} ms while a Go finalizer was still " +
                "running. Go's runtime.GC() does not wait for finalizers to COMPLETE, so a finalizer that " +
                "blocks on its caller (runtime/pprof TestGoroutineCounts: close(fingReady) then <-c) can " +
                "never be reached: the caller is stuck inside GC() and never gets to the channel receive.");
        }
        finally
        {
            // Always release, then hand a QUIESCED finalizer queue to the next arm: without the
            // release a parked body would outlive this arm (before the fix it wedged the CLR's own
            // finalizer thread for the whole process), and without the trailing collection the next
            // arm's reading would start against a still-draining queue.
            release.Set();
            GC.WaitForPendingFinalizers();
            Δruntime.GC();
        }
    }

    // ----------------------------------------------------------------------------------------
    // ARM 5 — the row in miniature, through the converted channel primitives.
    // ----------------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Action<ж<ж<nint>>> ChannelFinalizer(channel<bool> fingReady, channel<bool> c) =>
        _ =>
        {
            fingReady.Close();
            c.Receive();
        };

    [TestMethod]
    public void Arm5_TheRowInMiniatureCompletes()
    {
        // Unbuffered, exactly as Go's `make(chan struct{})`.
        channel<bool> fingReady = new(0);
        channel<bool> c = new(0);

        try
        {
            WeakReference weak = MintOnDedicatedThread(ChannelFinalizer(fingReady, c), clearAfterRegister: false);
            Assert.IsNotNull(weak);

            ManualResetEventSlim reached = new(false);
            Exception? failure = null;
            Thread row = new(() =>
            {
                try
                {
                    // The row's own two lines.
                    Δruntime.GC();
                    Δruntime.GC();
                    fingReady.Receive();
                    reached.Set();
                }
                catch (Exception ex)
                {
                    failure = ex;
                    reached.Set();
                }
            })
            { IsBackground = true, Name = "q23-row" };
            row.Start();

            bool ok = reached.Wait(GcWatchdogMs);
            Console.WriteLine($"[q23:arm5] reachedTheChannelReceive={ok} failure={failure?.GetType().Name ?? "none"}");

            Assert.IsNull(failure, $"ARM 5: the transcription threw: {failure}");
            Assert.IsTrue(ok,
                $"ARM 5: the runtime/pprof TestGoroutineCounts shape did not get past `<-fingReady` within " +
                $"{GcWatchdogMs} ms — the row's hang, reproduced.");
        }
        finally
        {
            // Releases the finalizer body's `<-c`, exactly as the row's own `close(c)` does at the
            // end of TestGoroutineCounts; the collection then quiesces the queue for the next arm.
            c.Close();
            GC.WaitForPendingFinalizers();
            Δruntime.GC();
        }
    }
}
