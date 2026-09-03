using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.golib;

namespace GolibTests;

[TestClass]
public class GoroutineExecutionContextFlowTests
{
    // The load-bearing measurement under the pprof label cut (DESIGN bucket-B root 1).
    //
    // Go's profile labels are INHERITED at goroutine creation and then independent:
    //
    //     proc.go:5097   // Only user goroutines inherit pprof labels.
    //     proc.go:5099   newg.labels = mp.curg.labels
    //
    // A [ThreadStatic] slot cannot express that -- a spawned goroutine would start with no labels
    // where Go gives it the parent's, which is a silent wrong answer that passes every test that
    // does not spawn under a label. An AsyncLocal<T> under a flowing ExecutionContext expresses it
    // exactly: the value is CAPTURED at thread start and later writes on either side do not cross.
    //
    // Goroutine.Start's own comment asserts the flow ("Thread.Start captures it the same way"), and
    // reading a comment is not measuring the behaviour. These tests are the measurement, and the
    // suppressed-flow case is what makes the flowing one mean anything: without it, "the child saw
    // the value" is equally consistent with a probe that cannot tell the two mechanisms apart.
    //
    // If golib's spawn path ever moves to Thread.UnsafeStart, ThreadPool.UnsafeQueueUserWorkItem,
    // or wraps spawning in ExecutionContext.SuppressFlow, the first test here goes red and the
    // pprof labels silently stop inheriting. That is the regression this file exists to catch.

    private static readonly AsyncLocal<string?> s_label = new();

    private static string? SpawnAndRead(Action<Action> spawn)
    {
        string? seen = null;
        using ManualResetEventSlim done = new(false);

        spawn(() =>
        {
            seen = s_label.Value;
            done.Set();
        });

        Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(30)), "the spawned body did not run within 30s");
        return seen;
    }

    [TestMethod]
    public void GoroutineInheritsAsyncLocalFromItsCreator()
    {
        s_label.Value = "parent-label";

        try
        {
            string? seen = SpawnAndRead(Goroutine.Start);
            Assert.AreEqual("parent-label", seen,
                "a goroutine must observe the creator's AsyncLocal value at its first instruction -- " +
                "this is Go's newg.labels = mp.curg.labels. If this fails, golib's spawn path has " +
                "stopped flowing the ExecutionContext and pprof labels no longer inherit.");
        }
        finally
        {
            s_label.Value = null;
        }
    }

    [TestMethod]
    public void SuppressedFlowDoesNotInherit_TheControlThatMakesTheAboveMeanSomething()
    {
        s_label.Value = "parent-label";

        try
        {
            string? seen = SpawnAndRead(body =>
            {
                // Same thread mechanism, one axis varied: the ExecutionContext is not captured.
                using (ExecutionContext.SuppressFlow())
                {
                    Thread t = new(() => body()) { IsBackground = true };
                    t.UnsafeStart();
                }
            });

            Assert.IsNull(seen,
                "with flow suppressed the child must NOT see the parent's value -- if it does, this " +
                "probe cannot distinguish inheritance from ambient visibility and the test above " +
                "proves nothing");
        }
        finally
        {
            s_label.Value = null;
        }
    }

    [TestMethod]
    public void WritesInTheChildDoNotReachTheParent()
    {
        // The other half of Go's semantics: copy at creation, then INDEPENDENT. A shared slot would
        // pass the inheritance test and still be wrong.
        s_label.Value = "parent-label";

        try
        {
            SpawnAndRead(body => Goroutine.Start(() => { s_label.Value = "child-label"; body(); }));
            Assert.AreEqual("parent-label", s_label.Value,
                "a goroutine's write to its own labels must not be visible to its creator");
        }
        finally
        {
            s_label.Value = null;
        }
    }
}
