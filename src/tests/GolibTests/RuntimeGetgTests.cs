using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using @unsafe = go.unsafe_package;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// Guards the managed <c>runtime.getg()</c> (runtime/stubs_impl.cs, Q47 over DESIGN-managed-getg.md):
/// on a goroutine the g carries the REGISTRY's identity (goid, parent goid) and an m whose curg is
/// itself; the labels follow golib's mirror on every call; a thread with no goroutine mints goid 0;
/// two calls on the same goroutine return the SAME g (the thread-static cache — the negative control
/// neuters the cache and this arm goes red); the goid agrees with the header <c>runtime.Stack</c>
/// prints on the same goroutine (the design's §8.5 guard); and the descriptor sizes and the first
/// call's allocation are MEASURED rather than carried from the design's provisional figures. The
/// generated stub throws NotImplementedException, so every arm is red against it.
/// </summary>
[TestClass]
public class RuntimeGetgTests
{
    private const uint Grunning = 2;

    // Run `body` on a fresh goroutine (a dedicated thread, golib's executor) and wait for it, surfacing
    // its assertion failure on the test thread.
    private static void OnGoroutine(Action body)
    {
        using ManualResetEventSlim done = new(false);
        Exception? failure = null;

        Goroutine.Start(() =>
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                failure = e;
            }
            finally
            {
                done.Set();
            }
        });

        Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(30)), "the goroutine did not finish");

        if (failure is not null)
            throw new AssertFailedException($"on the goroutine: {failure.Message}", failure);
    }

    [TestMethod]
    public void OnAGoroutineTheDescriptorCarriesTheRegistrysIdentity()
    {
        OnGoroutine(() =>
        {
            Goroutine current = Goroutine.Current!;
            GoGetgView view = GoGetgSnapshot();

            Assert.AreEqual(unchecked((ulong)current.Id), view.Goid, "goid is the registry's id");
            Assert.AreEqual(unchecked((ulong)current.ParentId), view.ParentGoid, "parentGoid is the registry's parent id");
            Assert.IsTrue(view.HasM, "the g carries an m — the thread that runs it");
            Assert.IsTrue(view.MCurgIsSelf, "m.curg is the g itself");
            Assert.AreEqual(Grunning, view.Status, "atomicstatus is _Grunning: true of the caller");
            Assert.IsTrue(view.SecondCallIsSameG, "two calls on one goroutine return the same g");
            Assert.AreNotEqual((nuint)0, view.Startpc, "startpc is the entry's synthetic PC");
        });
    }

    [TestMethod]
    public void TheLabelsFollowTheMirrorOnEveryCall()
    {
        OnGoroutine(() =>
        {
            Assert.IsNull(GoGetgSnapshot().Labels, "a goroutine never labelled reads nil");

            @unsafe.Pointer label = new((nuint)0x1234);
            Goroutine.SetProfileLabels(label);
            Assert.AreSame(label, GoGetgSnapshot().Labels, "the mirror's value is what getg().labels answers");

            Goroutine.SetProfileLabels(null);
            Assert.IsNull(GoGetgSnapshot().Labels, "…and it is read on every call, not cached at mint");
        });
    }

    [TestMethod]
    public void AThreadWithNoGoroutineMintsGoidZero()
    {
        GoGetgView view = default;
        Exception? failure = null;

        Thread thread = new(() =>
        {
            try
            {
                Assert.IsNull(Goroutine.Current, "the premise: a plain thread is not a goroutine");
                view = GoGetgSnapshot();
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw new AssertFailedException(failure.Message, failure);

        Assert.AreEqual(0UL, view.Goid, "goid 0 — the id runtime.Stack already prints for such a thread");
        Assert.IsTrue(view.HasM && view.MCurgIsSelf, "the pair is minted all the same");
        Assert.IsTrue(view.SecondCallIsSameG);
    }

    [TestMethod]
    public void TheGoidAgreesWithTheHeaderStackPrints()
    {
        OnGoroutine(() =>
        {
            GoGetgView view = GoGetgSnapshot();
            slice<byte> buf = new(new byte[8192]);
            nint n = Stack(buf, false);
            string text = Encoding.UTF8.GetString(buf.ToSpan().Slice(0, (int)n));

            Match header = Regex.Match(text, @"^goroutine (\d+) \[", RegexOptions.Multiline);
            Assert.IsTrue(header.Success, $"Stack's header names the goroutine: {text.Split('\n')[0]}");
            Assert.AreEqual(view.Goid, ulong.Parse(header.Groups[1].Value), "getg().goid is the id Stack prints for the same goroutine (the design's §8.5 guard)");
        });
    }

    [TestMethod]
    public void TheDescriptorSizesAndTheFirstCallsAllocationAreMeasured()
    {
        (int gBytes, int mBytes) = GoGetgDescriptorSizes();
        Assert.IsTrue(gBytes > 0 && mBytes > 0);

        long minimum = long.MaxValue;

        for (int i = 0; i < 5; i++)
        {
            long allocated = -1;

            OnGoroutine(() =>
            {
                Assert.IsFalse(GoGetgIsMinted(), "a fresh goroutine has no descriptor yet");
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                GoGetgSnapshot();
                allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            });

            Assert.IsTrue(allocated > 0, "the first call mints");
            minimum = Math.Min(minimum, allocated);
        }

        Console.WriteLine($"getg descriptor sizes: g {gBytes} B, m {mBytes} B (Unsafe.SizeOf); first-call allocation floor over 5 fresh goroutines: {minimum} B");
        Assert.IsTrue(minimum >= gBytes + mBytes, "the first call allocates at least the two descriptors");
    }
}
