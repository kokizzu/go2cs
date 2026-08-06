using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

[TestClass]
public class PanicTracebackTests
{
    // A panic does not reach its reporter on the stack it was raised on. GoFunc's finally-based
    // defer machinery re-raises it (`throw CapturedPanic.Value`) once the deferred sequence has
    // declined to recover it, and re-raising a stored instance resets Exception.StackTrace to the
    // re-raise point; a runtime-error panic is synthesized from the .NET exception and was never
    // thrown at the fault site at all. Left alone, BOTH print `GoFunc.HandleFinally` as the deepest
    // frame — so every panic in the corpus reads as a defect in the defer machinery, and the fault
    // site is unrecoverable. PanicException.StackTrace prefers the origin snapshot for exactly this
    // reason; these tests pin that it survives each way a panic travels.
    //
    // This is guarded here rather than behaviorally because it is not Go-observable: no converted
    // program reads a CLR stack trace. The Go-observable half — runtime.Stack / debug.Stack showing
    // the panic site from inside a deferred call — is guarded by sync's TestOnceFuncPanicTraceback.

    // Named so the assertions can look for them by name, and kept out of the inliner's reach: the
    // frame being asserted on has to exist to be found.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void dereferenceNilPointer()
    {
        ж<nint> p = nil;
        _ = p.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void raiseExplicitPanic()
    {
        throw panic("boom");
    }

    // Runs `body` and returns the panic that escaped it, failing the test if none did.
    private static PanicException escapingPanic(Action body)
    {
        try
        {
            body();
        }
        catch (PanicException ex)
        {
            return ex;
        }

        Assert.Fail("expected a panic to escape");
        return null;
    }

    [TestMethod]
    public void SynthesizedRuntimeErrorPanicReportsTheFaultSite()
    {
        // A nil dereference inside a deferring function: NullReferenceException is mapped to a Go
        // panic by RuntimeErrorPanic, so the PanicException that escapes was never thrown at the
        // fault — only the mapped-from exception carries those frames.
        PanicException escaped = escapingPanic(static () => func((defer, recover) => dereferenceNilPointer()));

        StringAssert.Contains(escaped.Message, "nil pointer dereference");

        string trace = escaped.StackTrace;

        Assert.IsNotNull(trace, "a panic that escaped a GoFunc must still carry a traceback");
        StringAssert.Contains(trace, nameof(dereferenceNilPointer),
            "the traceback must name the FAULT site, not just the defer machinery that re-raised it");
    }

    [TestMethod]
    public void ExplicitPanicReportsTheThrowSiteAfterReRaise()
    {
        // An explicit panic() IS thrown at its site, so its base trace starts out correct — and is
        // then destroyed by the re-raise in HandleFinally. The origin snapshot is what survives.
        PanicException escaped = escapingPanic(static () => func((defer, recover) => raiseExplicitPanic()));

        StringAssert.Contains(escaped.StackTrace, nameof(raiseExplicitPanic),
            "re-raising the stored instance reset the base trace; the origin snapshot must replace it");
    }

    [TestMethod]
    public void RuntimeErrorPanicOutsideAnyGoFuncStillReportsTheFaultSite()
    {
        // A converted function that never defers is not wrapped in a GoFunc, so NOTHING between the
        // fault and the reporter is part of the panic machinery: the NullReferenceException travels
        // raw and is adopted at the reporter's own catch. The synthesized panic is brand new there,
        // so unless the adoption point itself snapshots the origin the report carries no traceback at
        // all — which is what five of time's failures looked like (`panic: …` and nothing else).
        PanicException adopted = null;

        try
        {
            dereferenceNilPointer();
        }
        catch (Exception ex) when (go.golib.RuntimeErrorPanic.TryAsPanic(ex, out PanicException mapped))
        {
            adopted = mapped;
        }

        Assert.IsNotNull(adopted, "a nil dereference must map to a Go panic");
        Assert.IsNull(adopted.InnerException, "the mapped panic is synthesized, not wrapped");
        StringAssert.Contains(adopted.StackTrace, nameof(dereferenceNilPointer),
            "adoption must snapshot the origin: the synthesized panic was never thrown at the fault");
    }

    [TestMethod]
    public void RecoveredPanicIsNotReported()
    {
        // The whole mechanism is scoped to a panic that ESCAPES. A recovered one never reaches a
        // reporter, and must not leave the function looking like it faulted.
        object recovered = null;

        func((defer, recover) =>
        {
            defer(() => recovered = recover());
            dereferenceNilPointer();
        });

        Assert.IsNotNull(recovered, "recover() must observe the mapped runtime-error panic");
        StringAssert.Contains(recovered.ToString(), "nil pointer dereference");
    }

    [TestMethod]
    public void PanicWithNoOriginSnapshotFallsBackToTheBaseTrace()
    {
        // A panic raised outside any GoFunc is never caught by the defer machinery, so nothing
        // snapshots an origin. The base trace is intact in that case and must be what is reported —
        // the override adds the origin, it does not replace a good trace with nothing.
        PanicException escaped = escapingPanic(static () => raiseExplicitPanic());

        Assert.IsNull(escaped.PanicTrace, "nothing should have snapshotted an origin for this panic");
        StringAssert.Contains(escaped.StackTrace, nameof(raiseExplicitPanic));
    }
}
