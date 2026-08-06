using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

[TestClass]
public class ChannelWakeupStrainTests
{
    // The channels redesign's close/receive protocol is a LOST-WAKEUP surface, and the behavioral
    // guards that cover it (ChannelReceiveFromClosed, CloseWakesBlockedReceivers, SelectSingleFire,
    // …) are single-shot and deterministic: they prove the protocol is right on the interleaving
    // they happen to take, never that it survives a racing one. A missed close-check window or an
    // unclaimed waiter would leave every one of them green.
    //
    // These are the racing counterparts — thousands of instances, run concurrently, each with a
    // watchdog, of the three shapes where a receiver's park can race a close:
    //
    //   * a `for range` receiver woken by close (the shape os.testPipeEOF uses),
    //   * a direct hand-off racing a close on the same channel,
    //   * a blocked SELECT woken by a close on one of its cases — the twin window, since
    //     SelectRuntime.Run parks through the same queues.
    //
    // They exist because a channel-defect SIGHTING (2026-08-02, os's TestPipeEOF hanging with a
    // ranging receiver parked) had to be adjudicated, and "the protocol is fine" needed evidence
    // stronger than reading the code. The verdict was that the channel was never CLOSED — not a
    // lost wakeup — and these tests are the standing form of that evidence. A regression that
    // reintroduces a lost wakeup fails them by TIMING OUT, never by asserting the wrong value, so
    // the watchdog (not the assertion) is what makes them a guard.

    // Enough instances to make a narrow window overwhelmingly likely, small enough to keep the
    // class under a few seconds. Concurrency exceeds the shapes' own goroutine count so parks and
    // closes land on genuinely contended pool threads.
    private const int Instances = 3000;
    private const int Concurrency = 8;

    // A park that outlives this is not slow, it is lost: every shape completes in microseconds.
    private const int InstanceTimeoutMs = 15000;

    [TestMethod]
    public void CloseWakesRangingReceiver_UnderStrain()
    {
        RunStrain(RangeCloseShape);
    }

    [TestMethod]
    public void HandoffRacingClose_UnderStrain()
    {
        RunStrain(HandoffCloseShape);
    }

    [TestMethod]
    public void CloseWakesBlockedSelect_UnderStrain()
    {
        RunStrain(SelectCloseShape);
    }

    /// <summary>
    /// A ranging receiver parked on an EMPTY channel that is then closed must leave its loop.
    /// The seed alternates unbuffered/buffered and the number of sends before the close, so the
    /// close lands at every point relative to the receiver's park.
    /// </summary>
    private static void RangeCloseShape(int seed, Instance instance)
    {
        channel<nint> ch = new(seed % 2);
        channel<EmptyStruct> done = new(0);

        Goroutine.Start(() => instance.Guard(() =>
        {
            foreach (nint _ in ch)
            {
            }
        },
        finish: () => done.Close()));

        Goroutine.Start(() => instance.Complete(() =>
        {
            for (int i = 0; i < seed % 4; i++)
                ch.Send(i);

            Jitter(seed);
            ch.Close();

            // Blocks until the ranging goroutine's loop has actually terminated — the property
            // under test. A lost wakeup parks HERE forever and the watchdog reports it.
            done.Receive();
        }));
    }

    /// <summary>
    /// A direct hand-off to a parked receiver releases the channel lock BEFORE publishing the
    /// value and signalling, so a close can land inside that window. The receiver must still get
    /// its value, and the close must not double-wake the waiter the sender already unlinked.
    /// </summary>
    private static void HandoffCloseShape(int seed, Instance instance)
    {
        channel<nint> ch = new(1);
        channel<EmptyStruct> done = new(0);
        int received = 0;

        Goroutine.Start(() => instance.Guard(() =>
        {
            foreach (nint _ in ch)
                Interlocked.Increment(ref received);
        },
        finish: () => done.Close()));

        Goroutine.Start(() => instance.Complete(() =>
        {
            ch.Send(1);
            Jitter(seed);
            ch.Close();
            done.Receive();

            // The value sent before the close is delivered, never dropped: Go's close does not
            // discard buffered or in-flight values.
            Assert.AreEqual(1, Volatile.Read(ref received), "hand-off value lost across a racing close");
        }));
    }

    /// <summary>
    /// A blocked select parks one waiter per case through the same queues a plain receive uses.
    /// A close on one case (or a send to the other) must claim exactly one waiter and return that
    /// case's ordinal — never park past the wake, never return the sentinel.
    /// </summary>
    private static void SelectCloseShape(int seed, Instance instance)
    {
        channel<nint> a = new(seed % 2);
        channel<nint> b = new(seed / 2 % 2);
        channel<EmptyStruct> done = new(0);
        int fired = -2;

        Goroutine.Start(() => instance.Guard(() =>
        {
            fired = select(a.Receiving, b.Receiving);
        },
        finish: () => done.Close()));

        Goroutine.Start(() => instance.Complete(() =>
        {
            Jitter(seed);

            // Both wakers route through the same claim discipline: close drains the queues,
            // a send takes the head of one.
            if (seed % 2 == 0)
                a.Close();
            else
                b.Send(7);

            done.Receive();

            int winner = Volatile.Read(ref fired);
            Assert.IsTrue(winner is 0 or 1, $"blocked select returned {winner}, want a case ordinal");
        }));
    }

    // Spreads the close across the receiver's whole park sequence — sometimes before it has even
    // reached the channel, sometimes while it is committing, sometimes long after it parked.
    private static void Jitter(int seed)
    {
        switch (seed % 5)
        {
            case 0: Thread.SpinWait(50 + seed % 500); break;
            case 1: Thread.Yield(); break;
            case 2: Thread.Sleep(0); break;
            case 3: Thread.Sleep(1); break;
            default: Thread.SpinWait(5); break;
        }
    }

    private static void RunStrain(Action<int, Instance> shape)
    {
        int next = 0;
        List<string> failures = [];
        object gate = new();
        Thread[] drivers = new Thread[Concurrency];

        for (int d = 0; d < Concurrency; d++)
        {
            drivers[d] = new Thread(() =>
            {
                while (true)
                {
                    int seed = Interlocked.Increment(ref next) - 1;

                    if (seed >= Instances)
                        return;

                    Instance instance = new();
                    shape(seed, instance);

                    string? failure = instance.Await(InstanceTimeoutMs) is { } error
                        ? $"instance {seed}: {error}"
                        : null;

                    if (failure is null)
                        continue;

                    lock (gate)
                        failures.Add(failure);

                    // STOP on the first failure. A lost wakeup makes EVERY instance time out, so
                    // a run that kept going would take Instances/Concurrency × the timeout — hours
                    // — to report what the first instance already proved. The stuck goroutines are
                    // abandoned rather than joined, for the same reason.
                    Interlocked.Exchange(ref next, Instances);
                    return;
                }
            })
            { IsBackground = true, Name = $"strain{d}" };
        }

        foreach (Thread driver in drivers)
            driver.Start();

        foreach (Thread driver in drivers)
            driver.Join();

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// One run of a shape: the driver waits on <see cref="Await"/>, which reports a timeout (a
    /// lost wakeup) or the exception a goroutine failed with.
    /// </summary>
    private sealed class Instance
    {
        private readonly ManualResetEventSlim m_done = new(false);
        private Exception? m_error;

        /// <summary>Runs a goroutine body that is NOT the one the driver waits on.</summary>
        internal void Guard(Action body, Action finish)
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref m_error, ex, null);
            }
            finally
            {
                finish();
            }
        }

        /// <summary>Runs the goroutine body whose completion ends the instance.</summary>
        internal void Complete(Action body)
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref m_error, ex, null);
            }
            finally
            {
                m_done.Set();
            }
        }

        internal string? Await(int timeoutMs)
        {
            if (!m_done.Wait(timeoutMs))
                return $"timed out after {timeoutMs} ms — a parked channel operation was never woken";

            return Volatile.Read(ref m_error)?.Message;
        }
    }
}
