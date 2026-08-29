using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class FixtureLinkStagingTests
{
    // THE INVARIANT SET for link-staged fixture trees.
    //
    // A runnable-program fixture tree is staged as a LINK to the real Go source directory rather
    // than as file copies, because `cmd/go/internal/load.disallowInternal` decides an `internal/…`
    // import on ONE directory-prefix test: the file being compiled must live under $GOROOT/src.
    // Copies live in the sandbox and are refused; a link is resolved into GOROOT and permitted.
    //
    // That buys the verdicts at the cost of a NEW hazard, and these tests are about the hazard.
    // The link points INTO the Go installation, so anything that writes through it writes into
    // GOROOT — and the harness already holds a routine that does exactly that:
    // PackageAncestry.EnsureWritable replaces a link component with an EMPTY real directory so the
    // shared-fixture staging can write into ancestor-relative paths. Applied to a FIXTURE link that
    // is not safety, it is silent content LOSS: the tree the tests read simply ceases to exist and
    // every reader fails on a bare file-not-found with nothing attributing it — the same shape as
    // the vanished-fixture regression FixtureStagingLoudSkipTests exists for.
    //
    // EVERY TEST HERE USES A THROWAWAY DIRECTORY AS THE LINK TARGET, NEVER GOROOT. What is under
    // test is the refusal, and a guard that has to point at the real Go installation to prove
    // itself would be a guard nobody could run twice.

    private static string NewDirectory(string tag)
    {
        string path = Path.Combine(Path.GetTempPath(), $"g2cs-linkstage-{tag}-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    // Removes a tree the way the host does: links are UNLINKED, never traversed, so a sacrificial
    // target's content can never be reached through one during cleanup.
    private static void Discard(string root)
    {
        try
        {
            PackageAncestry.Delete(root);
        }
        catch (Exception)
        {
        }
    }

    [TestMethod]
    public void AWriteThroughALinkStagedFixtureTreeIsRefusedByPath()
    {
        // Failing-first record: before the refusal, EnsureWritable deleted the link and created an
        // empty real directory in its place — this test observed `sentinel.txt` GONE from the
        // sacrificial target's mirror and the run continuing happily. That is the loss this guards.
        string runRoot = NewDirectory("run");
        string real = NewDirectory("real");

        try
        {
            File.WriteAllText(Path.Combine(real, "sentinel.txt"), "the fixture tree's content");

            string workingDirectory = Path.Combine(runRoot, "src", "example", "pkg");
            Directory.CreateDirectory(Path.Combine(workingDirectory, "testdata"));

            PackageAncestry.StageFixtureLinks(["testdata/prog"], LinkStagingGoRoot(real, "example/pkg", "testdata/prog"),
                "example/pkg", workingDirectory, runRoot);

            string link = Path.Combine(workingDirectory, "testdata", "prog");

            Assert.IsTrue(File.Exists(Path.Combine(link, "sentinel.txt")),
                "the link must present the real tree's content — without that there is nothing to protect");

            // The exact call the shared-fixture staging makes on its way to writing a file.
            InvalidOperationException refusal = Assert.ThrowsException<InvalidOperationException>(
                () => PackageAncestry.EnsureWritable(Path.Combine(link, "nested"), runRoot),
                "a write into a link-staged fixture tree was absorbed silently — the link would have been replaced by an EMPTY directory and the whole tree lost");

            StringAssert.Contains(refusal.Message, link,
                "the refusal must NAME the path, or it cannot be acted on");

            // …and the content is still there. The refusal is only worth having if it actually
            // prevented the delete rather than reporting it afterwards.
            Assert.IsTrue(File.Exists(Path.Combine(real, "sentinel.txt")),
                "the real tree lost its content despite the refusal");
            Assert.IsTrue(File.Exists(Path.Combine(link, "sentinel.txt")),
                "the link was unlinked despite the refusal");
        }
        finally
        {
            PackageAncestry.ReleaseFixtureLinks();
            Discard(runRoot);
            Directory.Delete(real, true);
        }
    }

    [TestMethod]
    public void ADirectoryOUTSIDEALinkStagedTreeStaysWritable()
    {
        // The control the refusal needs: it must key on being INSIDE a fixture link, never on
        // "some fixture link exists". The ancestry view's own links are still replaced on demand,
        // which is what makes the sandbox a sandbox, and the shared-fixture staging depends on it.
        string runRoot = NewDirectory("run");
        string real = NewDirectory("real");

        try
        {
            File.WriteAllText(Path.Combine(real, "sentinel.txt"), "content");

            string workingDirectory = Path.Combine(runRoot, "src", "example", "pkg");
            Directory.CreateDirectory(Path.Combine(workingDirectory, "testdata"));

            PackageAncestry.StageFixtureLinks(["testdata/prog"], LinkStagingGoRoot(real, "example/pkg", "testdata/prog"),
                "example/pkg", workingDirectory, runRoot);

            // A sibling of the link, and the link's own PARENT: neither is inside it.
            string sibling = Path.Combine(workingDirectory, "testdata", "other");
            PackageAncestry.EnsureWritable(sibling, runRoot);
            Assert.IsTrue(Directory.Exists(sibling), "a directory beside a fixture link must still be created");

            PackageAncestry.EnsureWritable(Path.Combine(workingDirectory, "testdata"), runRoot);
            Assert.IsTrue(Directory.Exists(Path.Combine(workingDirectory, "testdata", "prog")),
                "making the link's PARENT writable must not disturb the link");
            Assert.IsTrue(File.Exists(Path.Combine(workingDirectory, "testdata", "prog", "sentinel.txt")),
                "the link's content must survive its parent being made writable");
        }
        finally
        {
            PackageAncestry.ReleaseFixtureLinks();
            Discard(runRoot);
            Directory.Delete(real, true);
        }
    }

    [TestMethod]
    public void TheLinkPresentsTheRealTreeRatherThanACopy()
    {
        // The property the whole remedy rests on, stated as something observable: a file added to
        // the real tree AFTER staging is visible through the link. A copy could not do that, and a
        // copy is exactly what the toolchain refuses.
        string runRoot = NewDirectory("run");
        string real = NewDirectory("real");

        try
        {
            File.WriteAllText(Path.Combine(real, "a.go"), "package main\n");

            string workingDirectory = Path.Combine(runRoot, "src", "example", "pkg");
            Directory.CreateDirectory(Path.Combine(workingDirectory, "testdata"));

            PackageAncestry.StageFixtureLinks(["testdata/prog"], LinkStagingGoRoot(real, "example/pkg", "testdata/prog"),
                "example/pkg", workingDirectory, runRoot);

            File.WriteAllText(Path.Combine(real, "b.go"), "package main\n");

            string link = Path.Combine(workingDirectory, "testdata", "prog");

            Assert.IsTrue(File.Exists(Path.Combine(link, "b.go")),
                "the staged tree is a COPY, not a link — the toolchain would refuse its internal/… imports exactly as before");

            FileSystemInfo? resolved = Directory.ResolveLinkTarget(link, returnFinalTarget: true);

            Assert.IsNotNull(resolved, "the staged path is not a link at all");
            Assert.IsTrue(
                string.Equals(
                    Path.GetFullPath(real).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(resolved!.FullName).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase),
                $"the link resolves to '{resolved.FullName}' rather than the real fixture tree '{real}'");
        }
        finally
        {
            PackageAncestry.ReleaseFixtureLinks();
            Discard(runRoot);
            Directory.Delete(real, true);
        }
    }

    [TestMethod]
    public void StagingWithNoGoRootFailsRatherThanLeavingTheTreeMissing()
    {
        // The trees are in NEITHER the project's copied fixtures nor the build output, so there is
        // no silent-degradation option: without a GOROOT to link into, the sandbox would hold an
        // empty directory and every reader would fail on file-not-found. Loud, at the moment the
        // fact is known.
        string runRoot = NewDirectory("run");

        try
        {
            string workingDirectory = Path.Combine(runRoot, "src", "example", "pkg");
            Directory.CreateDirectory(workingDirectory);

            Assert.ThrowsException<InvalidOperationException>(
                () => PackageAncestry.StageFixtureLinks(["testdata/prog"], null, "example/pkg", workingDirectory, runRoot),
                "link staging with no GOROOT must fail the run, not proceed half-staged");

            Assert.ThrowsException<InvalidOperationException>(
                () => PackageAncestry.StageFixtureLinks(["testdata/prog"], Path.Combine(runRoot, "no-such-goroot"), "example/pkg", workingDirectory, runRoot),
                "link staging against a GOROOT that does not hold the package must fail the run");
        }
        finally
        {
            PackageAncestry.ReleaseFixtureLinks();
            Discard(runRoot);
        }
    }

    [TestMethod]
    public void NoLinksDeclaredLeavesTheSandboxUntouched()
    {
        // The control for the whole capability: the overwhelming majority of packages declare no
        // link-staged trees and must not acquire a GOROOT requirement, a probe, or a refusal.
        string runRoot = NewDirectory("run");

        try
        {
            string workingDirectory = Path.Combine(runRoot, "src", "example", "pkg");
            Directory.CreateDirectory(workingDirectory);

            PackageAncestry.StageFixtureLinks([], goRoot: null, "example/pkg", workingDirectory, runRoot);

            string ordinary = Path.Combine(workingDirectory, "testdata");
            PackageAncestry.EnsureWritable(ordinary, runRoot);

            Assert.IsTrue(Directory.Exists(ordinary),
                "a package with no link-staged trees must keep the staging behavior it always had");
        }
        finally
        {
            Discard(runRoot);
        }
    }

    [TestMethod]
    public void TheToolchainProbeAcceptsARealLinkStagedTreeInAModuleRootedSandbox()
    {
        // THE REGRESSION THIS EXISTS FOR, and it is a measured one rather than an imagined one.
        //
        // The startup assertion's first implementation asked `go list -e -f {{.Dir}}` on the linked
        // DIRECTORY and required a path under $GOROOT/src. That is correct in a bare scratch
        // directory and WRONG here, because the sandbox has a module — the ancestry view stages
        // GOROOT's `src/go.mod` (module std) above the package — and inside a module cmd/go resolves
        // the directory against THAT module and reports the sandbox path for a copy, a symlink and a
        // junction alike. The assertion consequently failed a run whose links were working, with
        // "the Go toolchain does not resolve the fixture link ... into ...\src". The FILE form is the
        // discriminator, because it is the form the tests use and the one that reaches
        // disallowInternal.
        //
        // So this test reproduces the module-rooted sandbox exactly and requires the staging to
        // ACCEPT. It reads GOROOT and never writes to it: the link is read-only by construction and
        // the sandbox is a temp tree that goes away.
        string? goRoot = Environment.GetEnvironmentVariable("GOROOT");
        string relative = Path.Combine("testdata", "testprog");

        if (string.IsNullOrWhiteSpace(goRoot) ||
            !Directory.Exists(Path.Combine(goRoot, "src", "internal", "trace", relative)) ||
            !File.Exists(Path.Combine(goRoot, "bin", OperatingSystem.IsWindows() ? "go.exe" : "go")))
        {
            Assert.Inconclusive("needs a GOROOT holding internal/trace/testdata/testprog and a go binary — the probe has nothing to ask otherwise");
            return;
        }

        string runRoot = NewDirectory("run");

        try
        {
            // The module context is the whole point: without `src/go.mod` above the package this
            // passes for the wrong reason, because cmd/go then falls back to GOROOT resolution.
            string sandboxSrc = Path.Combine(runRoot, "src");
            Directory.CreateDirectory(sandboxSrc);
            File.Copy(Path.Combine(goRoot, "src", "go.mod"), Path.Combine(sandboxSrc, "go.mod"));

            string workingDirectory = Path.Combine(sandboxSrc, "internal", "trace");
            Directory.CreateDirectory(Path.Combine(workingDirectory, "testdata"));

            PackageAncestry.StageFixtureLinks(["testdata/testprog"], goRoot, "internal/trace", workingDirectory, runRoot);

            string link = Path.Combine(workingDirectory, relative);

            Assert.IsTrue(File.Exists(Path.Combine(link, "cpu-profile.go")),
                "the link does not present Go's own testprog sources");

            // Nothing was written into GOROOT: the staged tree is a link, and the one file the probe
            // named is still exactly where Go put it.
            Assert.IsTrue(File.Exists(Path.Combine(goRoot, "src", "internal", "trace", relative, "cpu-profile.go")),
                "the real Go source tree lost a file — staging must never write through the link");
        }
        finally
        {
            PackageAncestry.ReleaseFixtureLinks();
            Discard(runRoot);
        }
    }

    // Builds the GOROOT-shaped root the staging expects — <goRoot>/src/<importPath>/<relative> — as a
    // link to the SACRIFICIAL directory, so nothing here ever addresses the real Go installation.
    // The staging resolves its target through this root exactly as it would through a real GOROOT.
    private static string LinkStagingGoRoot(string real, string importPath, string relative)
    {
        string goRoot = Path.Combine(Path.GetTempPath(), "g2cs-linkstage-goroot-" + Guid.NewGuid().ToString("N"));
        string leaf = Path.Combine(goRoot, "src", importPath.Replace('/', Path.DirectorySeparatorChar),
            relative.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(leaf)!);
        Directory.CreateSymbolicLink(leaf, real);

        return goRoot;
    }
}
