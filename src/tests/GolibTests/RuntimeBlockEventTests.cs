using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// Guards the refusal-by-name <c>runtime.saveblockevent</c> (runtime/mprof_impl.cs, COORD ruling (A)): the
/// block/mutex profile bucket store is Go-layout memory, so a recorded block event must fail by NAMING that
/// cause, not by the stub's "assembly, cgo, or a linkname" throw. With the rate at Go's default of 0,
/// <c>blockevent</c> must return without reaching the recorder, so the common path does not move.
/// Red against the converted body: with the rate above 0 it returned silently (profstackdepth reads 0 on
/// this host), recording nothing and naming nothing.
/// </summary>
[TestClass]
public class RuntimeBlockEventTests
{
    [TestMethod]
    public void BlockEventAboveRateZeroRefusesByName()
    {
        SetBlockProfileRate(1);

        try
        {
            PanicException refusal = Assert.ThrowsException<PanicException>(() => GoBlockEventProbe(1_000_000, 1));
            StringAssert.Contains(refusal.Message, "saveblockevent");
            StringAssert.Contains(refusal.Message, "bucket store is Go-layout memory");
        }
        finally
        {
            SetBlockProfileRate(0);
        }
    }

    [TestMethod]
    public void BlockEventAtTheDefaultRateNeverReachesTheRecorder()
    {
        SetBlockProfileRate(0);

        // blocksampled(cycles, 0) is false, so blockevent returns before saveblockevent; the refusal above
        // is what would surface if it were entered.
        GoBlockEventProbe(1_000_000, 7);
    }
}
