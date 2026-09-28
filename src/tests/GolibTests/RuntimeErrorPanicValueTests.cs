using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using static go.builtin;
using Δruntime = go.runtime_package;

namespace GolibTests;

// The panic VALUE golib raises for a Go runtime error, read through recover() exactly as runtime's
// TestRuntimePanicWithRuntimeError (crash_test.go:433) reads it: `_, ok := got.(runtime.Error)`.
//
// Go raises these as runtime.plainError (nil-map write, close of a closed or nil channel, send on a
// closed channel, makechan with a negative size) and runtime.boundsError (an index out of range). golib
// sits UNDER the runtime package and cannot name either, so the runtime registers them through
// RuntimeErrorPanic's hooks (runtime/panicvalues_impl.cs), the inverted dependency the divide-by-zero
// value already uses. Sizing: docs/phase4/CENSUS-runtime-error-factories-go1.24.13.md.
//
// One arm per THROW SITE, not per case: a nil-map write has three doors (indexer, Add, Set), a send on a
// closed channel has four (the immediate and the woken send, the select poll and the woken select), and
// makechan has four (three constructors and ISupportMake). A site left raising a string reads green in
// every arm but its own.
[TestClass]
public class RuntimeErrorPanicValueTests
{
    private const int TimeoutMs = 10_000;

    // The hooks are registered by the runtime module's initializer, which a converted program runs long
    // before it can recover anything (every converted package's init touches runtime). A unit test holds
    // no such guarantee, so it is run here, once, explicitly.
    [ClassInitialize]
    public static void RunRuntimeModuleInitializer(TestContext _)
    {
        RuntimeHelpers.RunModuleConstructor(typeof(Δruntime).Module.ModuleHandle);
    }

    // ---- helpers ------------------------------------------------------------------------------

    // Runs fn inside a frame shaped as the converter emits `defer func() { recovered = recover() }()`,
    // and returns what recover() saw. A .NET exception that is not a Go panic escapes, which is itself
    // the failure (it is what makechan's ArgumentOutOfRangeException did).
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

    private static void AssertRuntimeError(object? recovered, string want, string site)
    {
        Assert.IsNotNull(recovered, $"{site}: did not panic");

        Assert.IsTrue(recovered._<Δruntime.ΔError>(out Δruntime.ΔError? error),
            $"{site}: recovered value {recovered} (type {recovered.GetType().FullName}) does not implement runtime.Error");

        Assert.AreEqual(want, error!.Error().ToString(), $"{site}: runtime.Error text");
    }

    // Runs fn on a goroutine, waits until it PARKS, runs wake, and returns what recover() saw there:
    // the woken-by-close door of a blocked send, which the immediate door never reaches.
    private static object? PanicValueAfterPark(Action fn, Action wake)
    {
        object? recovered = null;
        Exception? failure = null;
        Goroutine? g = null;
        using ManualResetEventSlim done = new(false);

        Goroutine.Start(() =>
        {
            try
            {
                g = Goroutine.Current;
                recovered = PanicValue(fn);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                done.Set();
            }
        });

        long deadline = Environment.TickCount64 + TimeoutMs;

        while (Volatile.Read(ref g) is not { IsParked: true })
        {
            if (done.IsSet)
                Assert.Fail($"the sender finished before parking: {failure?.ToString() ?? recovered?.ToString() ?? "no panic"}");

            if (Environment.TickCount64 > deadline)
                Assert.Fail("the sender never parked");

            Thread.Sleep(1);
        }

        wake();

        Assert.IsTrue(done.Wait(TimeoutMs), "the woken sender never finished");

        if (failure is not null)
            Assert.Fail($"the woken sender escaped with a non-panic exception: {failure}");

        return recovered;
    }

    // ---- runtime.plainError: assignment to entry in nil map -----------------------------------

    [TestMethod]
    public void NilMapIndexerWriteIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            map<uint64, bool> m = default!;
            m[1234] = true;
        }), "assignment to entry in nil map", "map indexer set");
    }

    [TestMethod]
    public void NilMapAddIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            map<uint64, bool> m = default!;
            m.Add(1234, true);
        }), "assignment to entry in nil map", "map.Add");
    }

    [TestMethod]
    public void NilMapSetIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            map<uint64, bool> m = default!;
            m.Set(1234, true);
        }), "assignment to entry in nil map", "map.Set");
    }

    // ---- runtime.plainError: channels ---------------------------------------------------------

    [TestMethod]
    public void CloseOfClosedChannelIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            channel<EmptyStruct> ch = new(0);
            close(ch);
            close(ch);
        }), "close of closed channel", "close of a closed channel");
    }

    [TestMethod]
    public void CloseOfNilChannelIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            close((channel<bool>)default!);
        }), "close of nil channel", "close of a nil channel");
    }

    [TestMethod]
    public void SendOnClosedChannelIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            channel<EmptyStruct> ch = new(0);
            close(ch);
            ch.ᐸꟷ(new EmptyStruct());
        }), "send on closed channel", "send on a closed channel");
    }

    [TestMethod]
    public void BlockedSendWokenByCloseIsARuntimeError()
    {
        channel<EmptyStruct> ch = new(0);

        AssertRuntimeError(PanicValueAfterPark(() => ch.ᐸꟷ(new EmptyStruct()), () => close(ch)),
            "send on closed channel", "a parked send woken by close");
    }

    [TestMethod]
    public void SelectSendOnClosedChannelIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            channel<EmptyStruct> ch = new(0);
            close(ch);
            select(ch.Sending(new EmptyStruct()));
        }), "send on closed channel", "select send on a closed channel");
    }

    [TestMethod]
    public void BlockedSelectSendWokenByCloseIsARuntimeError()
    {
        channel<EmptyStruct> ch = new(0);

        AssertRuntimeError(PanicValueAfterPark(() => select(ch.Sending(new EmptyStruct())), () => close(ch)),
            "send on closed channel", "a parked select send woken by close");
    }

    // ---- runtime.plainError: makechan: size out of range --------------------------------------

    [TestMethod]
    public void MakeChanNegativeSizeIsARuntimeError()
    {
        nint n = -1;

        AssertRuntimeError(PanicValue(() => _ = new channel<bool>(n)),
            "makechan: size out of range", "channel(size)");
    }

    [TestMethod]
    public void MakeDirectionalChanNegativeSizeIsARuntimeError()
    {
        nint n = -1;

        AssertRuntimeError(PanicValue(() => _ = new channel<bool>(n, GoChanDir.Recv)),
            "makechan: size out of range", "channel(size, direction)");
    }

    [TestMethod]
    public void MakeCargoChanNegativeSizeIsARuntimeError()
    {
        nint n = -1;

        AssertRuntimeError(PanicValue(() => _ = new channel<bool>(n, (ChanCargo?)null)),
            "makechan: size out of range", "channel(size, cargo)");
    }

    [TestMethod]
    public void MakeChanThroughISupportMakeNegativeSizeIsARuntimeError()
    {
        nint n = -1;

        AssertRuntimeError(PanicValue(() => _ = channel<bool>.Make(n)),
            "makechan: size out of range", "channel<T>.Make");
    }

    // ---- runtime.boundsError: index out of range ----------------------------------------------

    [TestMethod]
    public void SliceIndexOutOfRangeIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            slice<nint> s = new(2);
            _ = s[2];
        }), "runtime error: index out of range [2] with length 2", "slice index");
    }

    [TestMethod]
    public void ArrayIndexOutOfRangeIsARuntimeError()
    {
        nint i = 3;

        AssertRuntimeError(PanicValue(() =>
        {
            array<int> a = new(3);
            _ = a[i];
        }), "runtime error: index out of range [3] with length 3", "array index");
    }

    [TestMethod]
    public void StringIndexOutOfRangeIsARuntimeError()
    {
        AssertRuntimeError(PanicValue(() =>
        {
            @string s = "ab";
            _ = s[2];
        }), "runtime error: index out of range [2] with length 2", "string index");
    }

    [TestMethod]
    public void NegativeSliceIndexIsARuntimeError()
    {
        nint i = -1;

        AssertRuntimeError(PanicValue(() =>
        {
            slice<nint> s = new(2);
            _ = s[i];
        }), "runtime error: index out of range [-1]", "negative slice index");
    }

    // ---- the fallback: with no runtime registered, the message stands as a string --------------

    // A converted program that never loads the runtime package has nobody to register the values, and
    // golib must still raise a readable panic. Also the control that every arm above reads the HOOK:
    // unregistered, the same sites recover strings again. MSTest runs an assembly's tests serially,
    // so the process-global hooks can be swapped and restored here.
    [TestMethod]
    public void UnregisteredHooksFallBackToThePlainMessage()
    {
        Func<string, object>? plain = RuntimeErrorPanic.PlainErrorValue;
        Func<long, long, bool, byte, object>? bounds = RuntimeErrorPanic.BoundsErrorValue;

        Assert.IsNotNull(plain, "the runtime module did not register PlainErrorValue");
        Assert.IsNotNull(bounds, "the runtime module did not register BoundsErrorValue");

        try
        {
            RuntimeErrorPanic.PlainErrorValue = null;
            RuntimeErrorPanic.BoundsErrorValue = null;

            object? closed = PanicValue(() => close((channel<bool>)default!));
            object? index = PanicValue(() => { slice<nint> s = new(2); _ = s[2]; });

            Assert.IsTrue(closed is string or @string, $"unregistered plain error recovered {closed?.GetType().FullName}");
            Assert.IsTrue(index is string or @string, $"unregistered bounds error recovered {index?.GetType().FullName}");
            Assert.AreEqual("close of nil channel", closed.ToString());
            Assert.AreEqual("runtime error: index out of range [2] with length 2", index.ToString());
        }
        finally
        {
            RuntimeErrorPanic.PlainErrorValue = plain;
            RuntimeErrorPanic.BoundsErrorValue = bounds;
        }
    }

    // ---- the TEXT is not this change's business: it must read exactly as it did ----------------

    [TestMethod]
    public void PanicTextIsGosText()
    {
        (Action fn, string want)[] cases =
        [
            (() => { map<uint64, bool> m = default!; m[1] = true; }, "assignment to entry in nil map"),
            (() => { channel<bool> ch = new(0); close(ch); close(ch); }, "close of closed channel"),
            (() => { slice<nint> s = new(2); _ = s[2]; }, "runtime error: index out of range [2] with length 2"),
        ];

        foreach ((Action fn, string want) in cases)
        {
            PanicException ex = Assert.ThrowsException<PanicException>(fn);
            Assert.AreEqual(want, ex.Message);
        }
    }
}
