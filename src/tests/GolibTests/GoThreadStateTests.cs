// GoThreadStateTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using static go.builtin;

namespace GolibTests;

/// <summary>
/// The one per-thread panic-and-defer holder (<see cref="GoThreadState"/>) keeps the per-THREAD semantics the
/// five <c>ThreadLocal</c> panic slots had: a coroutine's body runs on its own thread with its OWN holder, and
/// hand-offs in either direction leave the other side's state exactly as it was.
/// </summary>
[TestClass]
public class GoThreadStateTests
{
    // A deferring function that panics and recovers, as the converter emits one: it drives Run's panic path
    // (the claim, the handled and recoverable slots, a written sequence entry) through the holder.
    private static void panicsAndRecovers()
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); }, ref ᒐ);
            throw panic("coro");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    [TestMethod]
    public void ACoroutineAndItsCallerKeepTheirOwnHolders()
    {
        PanicException callerPanic = new("the caller's");
        PanicException coroPanic = new("the coroutine's");

        GoThreadState? callerBefore = null, callerAfter = null, coroHolder = null;
        int callerThread = 0, coroThread = 0;
        object? coroSawHandled = "unread";
        int coroSawDepth = -1, coroDepthAfterRun = -1;
        PanicException? callerHandledAfter = null;
        int callerDepthAfter = -1;
        Exception? coroFailure = null;

        OnGoroutine(() =>
        {
            GoThreadState caller = GoThreadState.Current;
            callerBefore = caller;
            callerThread = Environment.CurrentManagedThreadId;

            // State the caller holds across the hand-off, as a goroutine mid-sequence would.
            caller.HandledPanic = callerPanic;
            caller.SequenceDepth += 3;

            try
            {
                Coro.Start(() =>
                {
                    try
                    {
                        GoThreadState coro = GoThreadState.Current;
                        coroHolder = coro;
                        coroThread = Environment.CurrentManagedThreadId;
                        coroSawHandled = coro.HandledPanic;
                        coroSawDepth = coro.SequenceDepth;

                        // The coroutine's own state, and a real panic sequence run through its holder.
                        coro.HandledPanic = coroPanic;
                        panicsAndRecovers();
                        coroDepthAfterRun = coro.SequenceDepth;
                        coro.HandledPanic = null;
                    }
                    catch (Exception ex)
                    {
                        coroFailure = ex;
                    }
                }).Switch();

                callerAfter = GoThreadState.Current;
                callerHandledAfter = callerAfter.HandledPanic;
                callerDepthAfter = callerAfter.SequenceDepth;
            }
            finally
            {
                caller.HandledPanic = null;
                caller.SequenceDepth -= 3;
            }
        });

        if (coroFailure is not null)
            throw new AssertFailedException("the coroutine body failed", coroFailure);

        Assert.AreNotEqual(callerThread, coroThread, "a coroutine's body runs on its own thread");
        Assert.IsNotNull(coroHolder);
        Assert.AreNotSame(callerBefore, coroHolder, "the coroutine's thread has its OWN holder");
        Assert.IsNull(coroSawHandled, "the coroutine does not see the caller's handled panic");
        Assert.AreEqual(0, coroSawDepth, "the coroutine does not see the caller's sequence depth");
        Assert.AreEqual(0, coroDepthAfterRun, "the coroutine's own Run popped its sequence");
        Assert.AreSame(callerBefore, callerAfter, "the caller keeps its holder across the hand-off");
        Assert.AreSame(callerPanic, callerHandledAfter, "the coroutine's writes never reach the caller's handled panic");
        Assert.AreEqual(3, callerDepthAfter, "the coroutine's Run never moved the caller's sequence depth");
    }

    [TestMethod]
    public void AThreadHasNoHolderUntilFirstUseAndThenKeepsOne()
    {
        FieldInfo current = typeof(GoThreadState).GetField("t_current", BindingFlags.Static | BindingFlags.NonPublic)!;
        object? before = "unread";
        GoThreadState? first = null, second = null;

        Thread thread = new(() =>
        {
            before = current.GetValue(null);
            first = GoThreadState.Current;
            second = GoThreadState.Current;
        });

        thread.Start();
        Assert.IsTrue(thread.Join(10_000), "the probe thread never finished");

        Assert.IsNull(before, "a fresh thread has no holder before its first use");
        Assert.IsNotNull(first);
        Assert.AreSame(first, second, "the first use creates the holder once; later uses read the same one");
        Assert.AreNotSame(first, GoThreadState.Current, "another thread's holder is not this thread's");
    }

    // Drives a coro from a FRESH goroutine, never from the test thread (see ThreadStateCensusTests.OnGoroutine).
    private static void OnGoroutine(Action body)
    {
        Exception? error = null;
        using ManualResetEventSlim done = new(false);

        Goroutine.Start(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        Assert.IsTrue(done.Wait(30_000), "the driving goroutine never finished");

        if (error is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
