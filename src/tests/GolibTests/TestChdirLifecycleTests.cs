using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class TestChdirLifecycleTests
{
    // WHY THIS EXISTS.
    //
    // Go 1.24's TB.Chdir was UNEXERCISED, and I announced it as uncoverable here on the reasoning
    // that a working-directory change is process-global and so cannot be asserted by a test running
    // beside others. THAT REASONING WAS AN ASSUMPTION AND IT WAS WRONG: this project declares no
    // [Parallelize] attribute and ships no .runsettings, so MSTest runs its tests SERIALLY within
    // the assembly -- and HostEnvironmentVisibilityTests already mutates process-global environment
    // state here on exactly that basis. Checking it cost one command; asserting it cost a member of
    // the Go 1.24 bill its only coverage.
    //
    // Two members of this bill had already turned out to be invented rather than read (b.Loop's
    // cursor reset, Context's owner check). This is the third assumption in the same sitting that
    // did not survive being checked, which is the argument for checking rather than for confidence.
    //
    // THE ASSERTIONS ARE SEMANTIC, NOT STRING COMPARISONS. Comparing Directory.GetCurrentDirectory()
    // against the target path is fragile on Windows, where GetTempPath can hand back an 8.3 short
    // form while GetCurrentDirectory returns the long one -- a guard that fails for that reason
    // teaches nothing. Instead a sentinel file is dropped in the target and the test asks whether a
    // RELATIVE path resolves to it, which is what a working directory actually IS.

    private const string Sentinel = "chdir-guard-probe.txt";

    private static TestExecution NewExecution(string name)
    {
        TestReporter reporter = new("guard", json: false, verbose: false);
        TestRunner runner = new(new TestRegistry("guard", []), new TestOptions(), reporter, ".", ".");

        return new TestExecution(runner, name, null, "guard.go", 1);
    }

    private static string NewTargetDirectory()
    {
        string target = Path.Combine(Path.GetTempPath(), "r-chdir-guard-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, Sentinel), "probe");

        return target;
    }

    // Both halves of Go's contract in one arm, because they are one contract: Chdir moves the
    // process into the directory FOR THE DURATION OF THE TEST, and Cleanup puts it back.
    [TestMethod]
    public void ChdirEntersTheDirectoryAndTheCleanupRestoresIt()
    {
        string original = Directory.GetCurrentDirectory();
        string target = NewTargetDirectory();

        try
        {
            // Chdir refuses off the test goroutine, so it must be driven from INSIDE a running test
            // rather than on a freshly constructed execution whose owner thread is not yet set.
            TestExecution parent = NewExecution("TestChdirGuard");
            bool sentinelVisibleInBody = false;
            bool bodyRan = false;

            parent.Run("child", t =>
            {
                bodyRan = true;
                t.Value.Execution.Chdir(target);
                sentinelVisibleInBody = File.Exists(Sentinel);
            });

            Assert.IsTrue(bodyRan, "the test body must have run, or neither assertion below means anything");
            Assert.IsTrue(sentinelVisibleInBody,
                "a RELATIVE path must resolve inside the target directory while the test runs -- that is what Chdir does");
            Assert.IsFalse(File.Exists(Sentinel),
                "the cleanup must have restored the working directory -- the sentinel must NOT resolve relatively any more");
            Assert.AreEqual(original, Directory.GetCurrentDirectory(),
                "and the restored directory must be the one the test started in");
        }
        finally
        {
            // Restore unconditionally: a working directory left moved by a FAILING assertion would
            // poison every test that runs after this one in the same process.
            Directory.SetCurrentDirectory(original);
            TryDelete(target);
        }
    }

    // Go's Chdir calls c.Fatal on a directory it cannot enter. The host's Fatal is Log + FailNow,
    // which throws and ends the test -- so the observable is a FAILED test whose working directory
    // never moved.
    [TestMethod]
    public void ChdirToAMissingDirectoryFailsTheTestAndDoesNotMove()
    {
        string original = Directory.GetCurrentDirectory();
        string missing = Path.Combine(Path.GetTempPath(), "r-chdir-absent-" + Guid.NewGuid().ToString("N"));

        Assert.IsFalse(Directory.Exists(missing), "PRECONDITION: the target must not exist");

        try
        {
            TestExecution parent = NewExecution("TestChdirMissing");
            TestExecution? child = null;

            parent.Run("child", t =>
            {
                child = t.Value.Execution;
                t.Value.Execution.Chdir(missing);
            });

            Assert.IsNotNull(child, "the subtest body must have run and exposed its execution");
            Assert.IsTrue(child!.Failed, "Chdir into a directory that cannot be entered must FAIL the test, as Go's Fatal does");
            Assert.AreEqual(original, Directory.GetCurrentDirectory(),
                "a failed Chdir must leave the working directory exactly where it was");
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
