using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class HostUnknownFlagPassThroughTests
{
    // THE INVARIANT: the HOST does not decide what an unrecognized flag means. Go's test binary
    // reaches exactly one flag.Parse(), and the package under test is entitled to have installed
    // its own flag.Usage by then — so an unknown name must reach the converted flag package and be
    // answered by GO'S contract, not short-circuited here.
    //
    // Why it matters beyond tidiness, measured 2026-08-28 on crypto/tls's BoGo shim: Go's
    // crypto/tls TestMain overrides flag.Usage to `if *bogoMode { os.Exit(89) }`, and the BoGo
    // runner reads exit 89 as errUnimplemented -> SKIP (runner.go:1685/20380). That is the shim's
    // DESIGNED grace path: Go's shim defines ~45 flags, the runner asks for ~100, and the ~55 it
    // has never heard of are meant to exit 89 and skip. The host used to answer first —
    // "flag provided but not defined: -<name>", return 2 — which the runner reads as a hard
    // failure. i9's run of the instrument turned 1,902 cases into FAILs on exactly that line.
    // The class is wider than BoGo: ANY package that installs a flag.Usage doing something other
    // than flag's default diverges the same way.
    //
    // These guards bind the converted testing host directly, the route
    // TestExecutionOutputCapTests established for a host that no _test.go can reach. GolibTests
    // does not reference the converted `flag` assembly, so TestFlagBridge's Type.GetType resolves
    // null and its members no-op — which is exactly what makes the host's own decision observable
    // in isolation: anything returned here is the HOST's verdict, never flag's.

    private static TestRegistry EmptyRegistry() => new("guard", []);

    [TestMethod]
    public void UnknownFlagIsNotRejectedByTheHost()
    {
        // Failing-first record (2026-08-28): this returned 2 — the host's own verdict — before the
        // pass-through landed. Go's exit code for a bad command line is also 2, which is precisely
        // why the divergence hid: the CODE matched while the DECISION was taken in the wrong place
        // and too early, so the package's Usage never ran.
        int exitCode = TestHost.Run(EmptyRegistry(), ["-no-such-flag-anywhere"]);

        Assert.AreNotEqual(2, exitCode,
            "the host answered an unrecognized flag itself; it must defer to the converted flag package so the package's own flag.Usage (crypto/tls's os.Exit(89) among them) can run");
    }

    [TestMethod]
    public void UnknownFlagStillReachesTheRun()
    {
        // The other half, and the one that actually enables the 89: not merely "no early return"
        // but that execution REACHES the stage where the converted flag package parses. Observed
        // through the host's own output: a run that got that far reports a terminal verdict.
        // Failing-first this captured the EMPTY string — the host had already returned 2 above it.
        StringWriter captured = new();
        TextWriter previous = Console.Out;

        try
        {
            Console.SetOut(captured);
            TestHost.Run(EmptyRegistry(), ["-no-such-flag-anywhere"]);
        }
        finally
        {
            Console.SetOut(previous);
        }

        StringAssert.Contains(captured.ToString(), "PASS",
            "the run never reached the package stage with an unrecognized flag present, so a converted flag.Parse — and any flag.Usage the package installed — could not have run");
    }
}
