using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class FixtureStagingLoudSkipTests
{
    // THE INVARIANT: a suite that DECLARES fixtures and stages NONE of them fails the run, loudly.
    //
    // CopyFixtures resolves each fixture against AppContext.BaseDirectory and skips what is absent.
    // I added that skip during the single-file host arc for a real reason — os/exec-style tests copy
    // the lone executable to a temp directory and re-exec it, exactly as they do Go's statically
    // linked test binary, and that copy has no fixtures beside it; throwing at startup killed the
    // helper re-entry before TestMain ran. But the same skip turns "the publish step dropped every
    // fixture" into a per-test ENOENT, which is how a banked row (time's
    // TestLoadLocationFromTZDataSlim, pass/pass) reached master failing on the published path
    // without any gate saying so. R found it by A/B; nothing in the harness reported it.
    //
    // The two situations need separating rather than trading against each other, because both are
    // real. The discriminator is the host's own directory: a PUBLISHED host sits among its
    // dependencies and staged sources — many files — while a RELOCATED copy is a lone executable
    // someone copied out on its own. So: fixtures declared, none resolved, and the host is NOT
    // alone ⇒ the staging is broken and the run fails. Fixtures declared, none resolved, host IS
    // alone ⇒ Go's own shape, keep going and let each test meet its own ENOENT.

    private static string NewEmptyDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "g2cs-fixture-guard-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [TestMethod]
    public void DeclaredFixturesThatAllVanishFailTheRun()
    {
        // Failing-first record (2026-08-29): this returned 0 — the run reported success with an
        // empty testdata/ — because every missing fixture was skipped silently and nothing counted
        // the skips. That is the exact shape that let time's regression reach master.
        //
        // The names cannot resolve beside the test host (GolibTests' own directory holds no
        // testdata/), so this is the "declared N, staged 0" case, and this host is emphatically not
        // a lone relocated executable.
        TestRegistry registry = new("guard", ["testdata/does-not-exist-a", "testdata/does-not-exist-b"]);

        int exitCode = TestHost.Run(registry, []);

        Assert.AreNotEqual(0, exitCode,
            "a suite declared fixtures, none were staged, and the run still reported success — the silent skip is hiding a broken publish/staging path exactly as it did for time's banked row");
    }

    [TestMethod]
    public void NoDeclaredFixturesStillRunsClean()
    {
        // The control: the loud check must key on DECLARED-but-unstaged, never on "no fixtures".
        // Most suites declare none at all and must be unaffected.
        TestRegistry registry = new("guard", []);

        Assert.AreEqual(0, TestHost.Run(registry, []),
            "a suite that declares no fixtures must be untouched by the fixture-staging check");
    }
}
