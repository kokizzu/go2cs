using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using sync = go.sync_package;

namespace GolibTests;

[TestClass]
public class OnceForeignExceptionTests
{
    // The defect these guards pin closed (exec-wall design OQ-6, ratified 2026-08-22): when the
    // function inside sync.OnceFunc/OnceValue throws a FOREIGN (.NET) exception rather than a Go
    // panic, the converted guard's deferred `p = recover(); if !valid { panic(p) }` sees recover()
    // return nil — there is no Go panic in flight — and its `panic(nil)` both masks the original
    // exception's identity and parks a nil-valued PanicException in the thread's captured-panic
    // slot with the oncefunc stack attached. Every R2 exec-wall residual behind a OnceValue-guarded
    // probe (testenv, os/user) reported as `panic: nil` instead of naming the
    // NotImplementedException underneath — a small honesty defect that poisoned a whole census
    // column's readability.
    //
    // The ratified semantics: a re-panic of the ORIGINAL is Go's own OnceValue contract. During a
    // foreign unwind, the deferred nil re-panic must surface the preserved original instead.

    [TestMethod]
    public void OnceValueForeignExceptionSurvivesWithItsIdentity()
    {
        var probe = sync.OnceValue<int>(() => throw new NotImplementedException("exec-wall-marker"));

        Exception? first = Capture(() => probe());

        Assert.IsNotNull(first, "the foreign exception vanished entirely on first call");
        Assert.IsTrue(
            first is NotImplementedException || first.ToString().Contains("exec-wall-marker"),
            $"first call surfaced '{first.GetType().Name}: {first.Message}' — the original " +
            "NotImplementedException identity was masked (the panic-nil shape this guard pins closed).");
    }

    [TestMethod]
    public void OnceValueForeignExceptionDoesNotPoisonLaterPanics()
    {
        var probe = sync.OnceValue<int>(() => throw new NotImplementedException("poison-marker"));
        Capture(() => probe());

        // A later, UNRELATED Go panic on the same thread must report its own value — not the
        // stale nil parked by the once guard, and not the once guard's foreign exception.
        Exception? later = Capture(() => go.builtin.panic("unrelated-panic"));

        Assert.IsNotNull(later);
        Assert.IsTrue(later.Message.Contains("unrelated-panic"),
            $"a later panic reported '{later.Message}' — state leaked from the once guard's unwind.");
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
