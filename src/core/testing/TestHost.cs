// TestHost.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using go.golib;

namespace go.testing_runtime;
/// <summary>
/// Entry point for a converted package's test binary: parses the command line, builds the isolated
/// environment one <c>go test</c> invocation gets, runs the suite, and writes the results out.
/// </summary>
/// <remarks>
/// <para>
/// This type owns the RUN, not the tests. <see cref="TestRunner"/> decides what executes and in what
/// order; <see cref="TestExecution"/> is one running test. What lives here is everything that has to
/// happen exactly once around them: the isolated working directory and its fixture staging, the
/// process-wide state <c>testing.Short()</c>/<c>Verbose()</c>/<c>T.Deadline()</c> report, the
/// package deadline, and the result and JUnit files.
/// </para>
/// <para>
/// The process-wide statics are correct rather than convenient: one host run executes per test
/// process, which is exactly Go's model — <c>go test</c> builds and runs one binary per package.
/// </para>
/// <para>
/// The isolated run directory reproduces the SHAPE <c>go test</c> gives a package and not merely a
/// scratch space, because tests observe it. Its last segment is the package's own directory name and
/// its parent holds nothing else, so a test that walks out and back in by name (io/fs's TestGlob
/// globs <c>*/glob.go</c> against <c>os.DirFS("..")</c> and expects <c>fs/glob.go</c>) sees what Go
/// shows it. Culture and TZ are pinned to invariant/UTC for the same reason: Go's own runs are not
/// locale-dependent, and a differential comparison against them cannot be either. All of it is
/// restored in the <c>finally</c>, whatever the run did.
/// </para>
/// </remarks>
public static class TestHost
{
    /// <summary>
    /// Gets whether the current run was started with -short — the value testing.Short() reports.
    /// One host run executes per test process, so process-wide state matches Go's model.
    /// </summary>
    public static bool ShortMode { get; private set; }

    /// <summary>
    /// Gets whether the current run was started with -v — the value testing.Verbose() reports.
    /// </summary>
    public static bool VerboseMode { get; private set; }

    /// <summary>
    /// Gets the UTC instant at which the package deadline (-timeout) expires, or null when no
    /// deadline is in effect. This is what testing.T.Deadline() reports, so it is measured from the
    /// same moment the host starts counting against options.Timeout — not from process start.
    /// </summary>
    public static DateTime? PackageDeadlineUtc { get; private set; }

    public static int Run(TestRegistry registry, string[] args)
    {
        // The go2cs runtime allocation counter is off by default, and this is the ONLY thing that
        // turns it on: testing.AllocsPerRun is its only reader, so a converted application that
        // never runs a test must not pay for it. Enabled at the very top of the run rather than
        // lazily, so that everything a test can observe is inside the counting window.
        AllocationCounter.Enable();

        TestOptions options;

        try
        {
            options = TestOptions.Parse(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }

        ShortMode = options.Short;
        VerboseMode = options.Verbose;

        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUICulture = CultureInfo.CurrentUICulture;
        string previousDirectory = Environment.CurrentDirectory;
        string? previousTimezone = Environment.GetEnvironmentVariable("TZ");
        // The isolated run directory reproduces the SHAPE `go test` gives a package, not just a
        // scratch space: its own last segment is the package's directory name, and its parent holds
        // nothing else. A test may walk out of the working directory and back in by name — io/fs's
        // TestGlob globs `*/glob.go` against os.DirFS("..") and expects `fs/glob.go` — which a bare
        // GUID directory answers with the GUID (and, worse, with every SIBLING run still on disk).
        (string runRoot, string workingDirectory) = CreateRunDirectory(registry.Package);
        options.ResolveOutputPaths(previousDirectory);

        try
        {
            // The ancestry goes in FIRST, so the fixture staging that follows can replace any linked
            // component it needs to write into with a real one. GOROOT is what the pipeline exports to
            // both sides; when there is none, staging is skipped and the sandbox is what it always was.
            PackageAncestry.TryStage(Environment.GetEnvironmentVariable("GOROOT"), registry.Package, runRoot, workingDirectory);

            CreateFixtureDirectories(registry.FixtureDirectories, workingDirectory, runRoot);
            CopyFixtures(registry.Fixtures, workingDirectory, runRoot);
            Environment.CurrentDirectory = workingDirectory;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Environment.SetEnvironmentVariable("TZ", "UTC");

            // Declare this run's command line on the converted flag package, the way testing.Init()
            // declares -test.* before a Go test binary's TestMain runs — otherwise a converted
            // TestMain calling flag.Parse() rejects the host's own arguments ("flag provided but not
            // defined: -json") before a single test executes. Placed HERE, after the isolated
            // environment is in place and before anything from the package under test is touched,
            // because that is the same moment Go's testing.Init() occupies: the test binary is
            // already running where `go test` put it, and no test code has run yet.
            TestFlagBridge.Register(options);

            TestReporter reporter = new(registry.Package, options.Json, options.Verbose);
            TestRunner runner = new(registry, options, reporter, workingDirectory, runRoot);

            // TEST-HOST-ONLY: contain an unhandled exception escaping a goroutine so it fails ONE
            // test instead of the whole run. See TestRunner.ContainGoroutineException — a converted
            // program keeps Go's process-death fidelity, which is golib's default.
            Goroutine.ContainUnhandledExceptions(runner.ContainGoroutineException);

            // TEST-HOST-ONLY: a PANIC escaping a goroutine still kills the process — that is Go's
            // behavior and the oracle must keep observing it — but it no longer takes the run's
            // evidence with it. The panic is attributed to the test that started the goroutine and
            // reported WITH its traceback, and every verdict gathered so far is flushed to the result
            // files, which the fatal path would otherwise discard whole (a package that had already
            // passed six tests recorded zero).
            Goroutine.ObserveUnhandledPanic(panic => ReportFatalGoroutinePanic(runner, reporter, registry, options, panic));

            // Set immediately before the clock starts, so testing.T.Deadline() and the Wait below
            // are answering about the same instant.
            PackageDeadlineUtc = options.Timeout > TimeSpan.Zero ? DateTime.UtcNow + options.Timeout : null;

            Task<nint> run = Task.Run(() => RunTests(registry, runner));

            if (!run.Wait(options.Timeout))
            {
                reporter.ReportPackage("timeout", options.Timeout.TotalSeconds, $"package timeout after {options.Timeout}");
                WriteResults(options.ResultFile, registry.Package, options, reporter.Events);
                WriteJUnit(options.JUnitFile, registry.Package, reporter.Events);
                return 1;
            }

            int exitCode = checked((int)run.Result);
            WriteResults(options.ResultFile, registry.Package, options, reporter.Events);
            WriteJUnit(options.JUnitFile, registry.Package, reporter.Events);
            return exitCode;
        }
        catch (Exception ex)
        {
            TestEvent infrastructureError = new(registry.Package, "", "infrastructure-error", Output: ex.ToString());
            if (options.Json)
                Console.WriteLine(JsonSerializer.Serialize(infrastructureError, TestReporter.JsonOptions));
            else
                Console.Error.WriteLine(ex);
            return 2;
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
            Environment.SetEnvironmentVariable("TZ", previousTimezone);

            try
            {
                // The whole run root, so the package-named directory's private parent goes with it.
                // Junction-aware: a recursive delete does not FOLLOW a link (which is what keeps the
                // real GOROOT safe) but it does not remove one either, so the ancestry's links are
                // unlinked first.
                PackageAncestry.Delete(runRoot);
            }
            catch
            {
                // Per-test cleanup failures are reported; final process cleanup is best effort.
            }
        }
    }

    private static nint RunTests(TestRegistry registry, TestRunner runner)
    {
        if (registry.TestMain is null)
            return runner.RunAll();

        testing_package.M m = new() { Runner = runner };
        registry.TestMain(new ж<testing_package.M>(m));
        return runner.HasRun ? runner.ExitCode : 0;
    }

    // Output-directory folder holding fixtures that reach ABOVE the package. MUST match the
    // converter's SharedFixtureStagingRoot, which emits the matching csproj <Link>.
    private const string SharedFixtureStagingRoot = "go2cs_shared_fixtures";

    /// <summary>
    /// Creates this run's isolated directory pair — the run root and the package working directory
    /// inside it — under the first base that will actually accept one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The temp directory is the right FIRST choice and the wrong ONLY choice, because it is under
    /// test control: a Go test may repoint TMP/TEMP at anything, including somewhere that does not
    /// exist. os's TestRootDirAsTemp re-execs the test binary with TMP and TEMP set to an
    /// intentionally UNMOUNTED drive root — <c>findUnusedDriveLetter</c> picks a letter precisely
    /// because <c>os.Stat</c> says it is not there — to check what <c>os.TempDir</c> reports. Go's
    /// test binary needs no scratch space of its own and does not care; this host does, and it died
    /// in startup with <c>DirectoryNotFoundException</c> before running a single test, which the
    /// parent then read as a child that produced no output.
    /// </para>
    /// <para>
    /// So the host's own isolation must not depend on an environment variable the suite it is
    /// running is free to rewrite. The executable's own directory is the fallback: it exists by
    /// construction (the host is running out of it) and is writable wherever the build put it.
    /// </para>
    /// </remarks>
    private static (string runRoot, string workingDirectory) CreateRunDirectory(string package)
    {
        Exception? firstFailure = null;

        foreach (string root in new[] { Path.GetTempPath(), AppContext.BaseDirectory })
        {
            // The isolated run directory reproduces the SHAPE `go test` gives a package, not just a
            // scratch space: its own last segment is the package's directory name, and its parent
            // holds nothing else. A test may walk out of the working directory and back in by name —
            // io/fs's TestGlob globs `*/glob.go` against os.DirFS("..") and expects `fs/glob.go` —
            // which a bare GUID directory answers with the GUID (and, worse, with every SIBLING run
            // still on disk).
            string runRoot = Path.Combine(root, "go2cs-tests", SanitizePath(package), Guid.NewGuid().ToString("N"));
            string workingDirectory = Path.Combine(runRoot, PackageDirectoryPath(package));

            try
            {
                Directory.CreateDirectory(workingDirectory);
                return (runRoot, workingDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                firstFailure ??= ex;
            }
        }

        throw new InvalidOperationException(
            "testing: could not create an isolated run directory under the temp path or the test binary's own directory",
            firstFailure);
    }

    // Reproduces the package directory's own SHAPE: `go test` runs a package where the sibling
    // packages nested under it are present as subdirectories, so a test that asks what its working
    // directory contains — os's TestReadDir looks for the `exec` directory beside `read_test.go` —
    // must find them here too. Names only, created empty: the name is what such a test observes, and
    // a sibling package's files are staged by that package's own run. (testdata, when the suite has
    // one, is created here too and then filled in by CopyFixtures — CreateDirectory is idempotent.)
    private static void CreateFixtureDirectories(IReadOnlyList<string> directories, string workingDirectory, string runRoot)
    {
        foreach (string name in directories)
        {
            string target = Path.GetFullPath(Path.Combine(workingDirectory, name));

            // A directory NAME never escapes the package directory; anything that resolves outside
            // it did not come from the converter's enumeration and is not created.
            if (!target.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"fixture directory escapes run root: {name}");

            PackageAncestry.EnsureWritable(target, runRoot);
        }
    }

    private static void CopyFixtures(IReadOnlyList<string> fixtures, string workingDirectory, string runRoot)
    {
        foreach (string relativePath in fixtures)
        {
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);

            // Go shares large fixtures between sibling packages rather than duplicating them:
            // compress/{flate,zlib,lzw} all read ../testdata/{e,pi,gettysburg}.txt, and flate also
            // reads ../../testdata/Isaac.Newton-Opticks.txt. Such a path cannot keep its shape under
            // the build output, so the csproj links it to <root>/up<N>/<tail>; restore the true
            // relative path here. The TARGET lands inside runRoot rather than the working directory
            // — which is exactly why the working directory mirrors the whole import path.
            string source = SharedFixtureStagingParts(relativePath) is var (up, tail) && up > 0
                ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SharedFixtureStagingRoot, $"up{up}", tail.Replace('/', Path.DirectorySeparatorChar)))
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, normalized));

            string target = Path.GetFullPath(Path.Combine(workingDirectory, normalized));

            // The run root is the containment boundary, not the working directory: a shared fixture
            // legitimately resolves to a SIBLING of the package directory inside the sandbox.
            if (!source.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase) ||
                !target.StartsWith(runRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"fixture escapes run root: {relativePath}");

            // The ancestry view may hold a LINK at the fixture's parent — compress/{flate,zlib,lzw}
            // all stage into `../testdata` — and writing through one would put staged fixtures inside
            // the real Go installation. EnsureWritable makes every component below the run root a
            // real directory first.
            PackageAncestry.EnsureWritable(Path.GetDirectoryName(target)!, runRoot);
            File.Copy(source, target, true);
        }
    }

    // Splits a fixture path that reaches above the package into the levels it ascends and the
    // remainder ("../testdata/e.txt" -> (1, "testdata/e.txt")); (0, path) for anything at or below
    // the package. The level count is part of the staging key because two different ancestors can
    // hold a same-named file. Mirrors the converter's sharedFixtureStagingParts.
    private static (int Up, string Tail) SharedFixtureStagingParts(string fixture)
    {
        int up = 0;
        string tail = fixture;

        while (tail.StartsWith("../", StringComparison.Ordinal))
        {
            up++;
            tail = tail["../".Length..];
        }

        return (up, tail);
    }

    /// <summary>
    /// Says what died and writes the run's evidence out, in the moment between a panic escaping a
    /// goroutine root and the process ending on it.
    /// </summary>
    /// <remarks>
    /// Ordering is the whole point: the attribution has to be recorded BEFORE the files are written,
    /// or the panic itself is the one verdict the files do not carry. Everything here is best-effort
    /// — the process is already dying, and a failure to write must not replace the panic's own report
    /// with a report about the writer.
    /// </remarks>
    private static void ReportFatalGoroutinePanic(TestRunner runner, TestReporter reporter, TestRegistry registry, TestOptions options, PanicException panic)
    {
        try
        {
            runner.ReportGoroutinePanic(panic);
            WriteResults(options.ResultFile, registry.Package, options, reporter.Events);
            WriteJUnit(options.JUnitFile, registry.Package, reporter.Events);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"go2cs test host: could not record the goroutine panic: {ex}");
        }
    }

    private static void WriteResults(string? path, string package, TestOptions options, IReadOnlyList<TestEvent> events)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            object result = new
            {
                schemaVersion = 1,
                package,
                environment = new
                {
                    dotnetRuntime = RuntimeInformation.FrameworkDescription,
                    culture = CultureInfo.CurrentCulture.Name,
                    timezone = Environment.GetEnvironmentVariable("TZ"),
                    shuffleSeed = options.ShuffleSeed
                },
                events
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(result, TestReporter.JsonOptions));
        }
    }

    private static void WriteJUnit(string? path, string package, IReadOnlyList<TestEvent> events)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        TestEvent[] terminal = events
            .Where(testEvent => testEvent.Test.Length > 0 && testEvent.Action is "pass" or "fail" or "skip" or "timeout" or "infrastructure-error")
            .ToArray();
        int failures = terminal.Count(testEvent => testEvent.Action is "fail" or "timeout");
        int errors = terminal.Count(testEvent => testEvent.Action == "infrastructure-error");
        int skipped = terminal.Count(testEvent => testEvent.Action == "skip");

        XElement suite = new("testsuite",
            new XAttribute("name", package),
            new XAttribute("tests", terminal.Length),
            new XAttribute("failures", failures),
            new XAttribute("errors", errors),
            new XAttribute("skipped", skipped),
            new XAttribute("time", terminal.Sum(testEvent => testEvent.Elapsed).ToString("0.######", CultureInfo.InvariantCulture)));

        foreach (TestEvent testEvent in terminal)
        {
            XElement testCase = new("testcase",
                new XAttribute("classname", package),
                new XAttribute("name", XmlSanitize(testEvent.Test)),
                new XAttribute("time", testEvent.Elapsed.ToString("0.######", CultureInfo.InvariantCulture)));

            if (testEvent.Action == "skip")
                testCase.Add(new XElement("skipped", new XAttribute("message", XmlSanitize(testEvent.Output ?? "skipped"))));
            else if (testEvent.Action is "fail" or "timeout")
                testCase.Add(new XElement("failure", new XAttribute("message", testEvent.Action), XmlSanitize(testEvent.Output ?? "")));
            else if (testEvent.Action == "infrastructure-error")
                testCase.Add(new XElement("error", new XAttribute("message", "infrastructure-error"), XmlSanitize(testEvent.Output ?? "")));

            suite.Add(testCase);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        new XDocument(new XElement("testsuites", suite)).Save(path);
    }

    /// <summary>
    /// Replaces characters XML 1.0 cannot carry with visible \uXXXX escape text. Real Go test
    /// logs legitimately contain them — unicode/utf8's own tests log U+FFFE/U+FFFF data — and an
    /// unsanitized XDocument.Save throws AFTER the run completed, downgrading a finished suite to
    /// an infrastructure error with no JUnit file at all.
    /// </summary>
    private static string XmlSanitize(string value)
    {
        StringBuilder? sanitized = null;

        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            bool valid;

            if (char.IsHighSurrogate(ch))
                valid = i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]);
            else if (char.IsLowSurrogate(ch))
                valid = i > 0 && char.IsHighSurrogate(value[i - 1]);
            else
                valid = ch is '\t' or '\n' or '\r' || (ch >= 0x20 && ch <= 0xD7FF) || (ch >= 0xE000 && ch <= 0xFFFD);

            if (valid)
            {
                sanitized?.Append(ch);
                continue;
            }

            sanitized ??= new StringBuilder(value[..i], value.Length + 8);
            sanitized.Append(CultureInfo.InvariantCulture, $"\\u{(int)ch:x4}");
        }

        return sanitized?.ToString() ?? value;
    }

    // The package's directory path under the run root, mirroring its whole Go import path BENEATH a
    // `src` level ("compress/flate" -> "src\compress\flate"). The last element is what `go test`
    // makes the working directory's base name; the ANCESTORS matter too, because a package that reads
    // a shared fixture ("../testdata/e.txt", "../../testdata/Isaac.Newton-Opticks.txt") needs those
    // levels to exist INSIDE the sandbox for the relative open() to resolve. Mirroring the import
    // path gives exactly Go's own layout, so the depth is always sufficient and never guessed.
    //
    // `src` is a level of GOROOT, so it is a level here: without it, a package's climb out of its own
    // tree lands one short. internal/godebugs reads ../../../doc/godebug.md, which is GOROOT's `doc`
    // beside `src` and not a fourth level above the package; internal/testenv stats ../../../bin/go
    // the same way. It is also where the Go toolchain's module walk finds `module std`, which is what
    // internal/coverage/cfile needs for `go test` inside a testdata directory to resolve at all.
    // PackageAncestry fills these levels with the real GOROOT's content.
    private static string PackageDirectoryPath(string importPath)
    {
        string[] segments = importPath.TrimEnd('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePath)
            .Where(segment => !string.IsNullOrEmpty(segment))
            .ToArray();

        return Path.Combine("src", segments.Length == 0 ? "pkg" : Path.Combine(segments));
    }

    private static string SanitizePath(string value) =>
        string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch is '/' or '\\' ? '_' : ch));
}
