using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// THE POSITIVE CONTROL FOR THE Q44 §10.5 REGISTRY CENSUS.
//
// The census counts four arms at ж.cs's `uintptr -> ж<T>` operator. A count of zero in any arm is
// only a READING if that counter can be made to move; otherwise the zero came from never being
// wired, which is this tree's most-paid lesson. So every arm is driven DELIBERATELY here and each
// assertion is that the counter INCREASED across the call -- not that a total matches a guess, which
// is precisely the shape that cannot distinguish a wired counter from an unwired one.
//
// Gated on the census being enabled (`GO2CS_Q44_CENSUS`), because the counters are off by default
// and a control that silently passes with the instrument off would be worse than no control at all.
// With it off this reports NOT MEASURED rather than green.
[TestClass]
public class Q44RegistryCensusControlTests
{
    private struct RefBearing { internal string name; internal nint scalar; }
    private struct OtherType  { internal string other; internal nint scalar; }

    [TestInitialize]
    public void RequireTheCensusEnabled()
    {
        if (!Q44RegistryCensus.Enabled)
        {
            Assert.Inconclusive("NOT MEASURED: the Q44 census is off. Set GO2CS_Q44_CENSUS to run this control; " +
                                "a green here with the instrument off would be a lie about a counter nothing drove.");
        }
    }

    [TestMethod]
    public void Arm1_SamePointeeType_IsCounted()
    {
        ж<RefBearing> box = new StandardBox<RefBearing>(new RefBearing { name = "tcp", scalar = 0x5A5A });
        nuint token = box.PointerOrderToken;
        ManagedPointerTokens.Register(token, box);

        long before = Q44RegistryCensus.Snapshot().arm1;
        var back = (ж<RefBearing>)(uintptr)token;

        Assert.IsTrue(Q44RegistryCensus.Snapshot().arm1 > before, "arm 1's counter must move");
        Assert.AreSame(box, back, "and arm 1 must alias the SAME box -- the count is worthless if the arm is wrong");
    }

    [TestMethod]
    public void Arm2_DifferentPointeeTypeAtOffsetZero_IsCounted()
    {
        // The write's case, and the one §10.3 calls new work: the token names a live box whose pointee
        // type is not T. It reaches NEITHER arm 1's alias nor arm 3's refusal today.
        ж<RefBearing> box = new StandardBox<RefBearing>(new RefBearing { name = "udp" });
        nuint token = box.PointerOrderToken;
        ManagedPointerTokens.Register(token, box);

        long before = Q44RegistryCensus.Snapshot().arm2a;

        try
        {
            _ = (ж<OtherType>)(uintptr)token;
        }
        catch (PanicException)
        {
            // Counted before any refusal, so the arm registers either way.
        }

        Assert.IsTrue(Q44RegistryCensus.Snapshot().arm2a > before,
            "arm 2a's counter must move -- offset 0, different pointee type");
    }

    [TestMethod]
    public void Arm3_InsideALiveBlockButNotTheToken_IsCounted()
    {
        ж<RefBearing> box = new StandardBox<RefBearing>(new RefBearing { name = "ip" });
        nuint token = box.PointerOrderToken;
        ManagedPointerTokens.Register(token, box);

        long before = Q44RegistryCensus.Snapshot().arm3;

        Assert.ThrowsException<PanicException>(() => { _ = (ж<RefBearing>)(uintptr)(token + 8); },
            "token + 8 is arithmetic on a live token and must still refuse");

        Assert.IsTrue(Q44RegistryCensus.Snapshot().arm3 > before, "arm 3's counter must move");
    }

    [TestMethod]
    public unsafe void Arm4_ARealAddress_IsCounted()
    {
        long local = 0;
        long before = Q44RegistryCensus.Snapshot().arm4;

        _ = (ж<long>)(uintptr)(nuint)(&local);

        Assert.IsTrue(Q44RegistryCensus.Snapshot().arm4 > before, "arm 4's counter must move for a real address");
    }

    [TestMethod]
    public void TheMintCounterMoves()
    {
        ж<RefBearing> box = new StandardBox<RefBearing>(new RefBearing { name = "unix" });
        long before = Q44RegistryCensus.Snapshot().mints;

        ManagedPointerTokens.Register(box.PointerOrderToken, box);

        Assert.IsTrue(Q44RegistryCensus.Snapshot().mints > before,
            "the projection mint must be counted (RegisterPinned is a DIFFERENT mint and deliberately is not)");
    }

    [TestMethod]
    public void TheCensusCanActuallyREPORT_TheFileAppears()
    {
        // ⚠ THE ARM THAT WAS MISSING, AND IT COST A MEASUREMENT. The first version of this control
        // proved all four arms fire and said nothing about whether the census could REPORT: the dump
        // went to stderr from a ProcessExit hook, and the MSTest host swallowed it -- every counter
        // wired, every arm green, and zero census lines in the log. A counter that moves into a
        // channel nobody reads is the same defect as a counter that never moves, and harder to see.
        // ⚠ ITS OWN FILE, and this is a defect this control already caused once. The first version
        // used the census's configured OutputPath and DELETED it before dumping -- so running the
        // control inside a census run wiped the census's own file mid-flight and re-wrote it with a
        // partial block. An instrument's control must not be able to damage the instrument's output.
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                             $"q44-census-control-{Environment.ProcessId}.txt");

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        Environment.SetEnvironmentVariable("GO2CS_Q44_CENSUS_FILE_OVERRIDE", path);

        try
        {
            Q44RegistryCensus.DumpTo(path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GO2CS_Q44_CENSUS_FILE_OVERRIDE", null);
        }

        Assert.IsTrue(System.IO.File.Exists(path), $"the census must write {path}; a dump nobody can read is not a report");

        string[] lines = System.IO.File.ReadAllLines(path);
        Assert.IsTrue(lines.Length >= 2, "the dump must carry at least the totals line and the reconciliation line");
        StringAssert.StartsWith(lines[0], "Q44CENSUS ", "the totals line must be first and greppable");
        Assert.IsTrue(Array.Exists(lines, l => l.StartsWith("Q44CENSUS-RECONCILES", StringComparison.Ordinal)),
            "the reconciliation must be RECORDED, not merely computed -- a census whose exhaustiveness is not in the artifact cannot be checked later");
    }

    [TestMethod]
    public void TheArmsAreExhaustive_TheSumReconcilesWithTheConversionCount()
    {
        // The property that makes every other count meaningful: each conversion lands in exactly one
        // arm. If the sum drifts from the conversion count the classification is not exhaustive, and
        // no per-arm number below it means anything.
        var s = Q44RegistryCensus.Snapshot();

        Assert.AreEqual(s.conversions, s.arm1 + s.arm2a + s.arm2b + s.arm3 + s.arm4,
            "the arms must sum to the conversions -- the classification is exhaustive or the census is broken");
    }
}
