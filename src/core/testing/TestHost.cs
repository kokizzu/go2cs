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
        string runRoot = Path.Combine(Path.GetTempPath(), "go2cs-tests", SanitizePath(registry.Package), Guid.NewGuid().ToString("N"));
        string workingDirectory = Path.Combine(runRoot, PackageDirectoryPath(registry.Package));
        options.ResolveOutputPaths(previousDirectory);

        try
        {
            Directory.CreateDirectory(workingDirectory);
            CopyFixtures(registry.Fixtures, workingDirectory, runRoot);
            Environment.CurrentDirectory = workingDirectory;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Environment.SetEnvironmentVariable("TZ", "UTC");

            TestReporter reporter = new(registry.Package, options.Json, options.Verbose);
            TestRunner runner = new(registry, options, reporter, workingDirectory);

            // TEST-HOST-ONLY: contain an unhandled exception escaping a goroutine so it fails ONE
            // test instead of the whole run. See TestRunner.ContainGoroutineException — a converted
            // program keeps Go's process-death fidelity, which is golib's default.
            Goroutine.ContainUnhandledExceptions(runner.ContainGoroutineException);

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
                Directory.Delete(runRoot, true);
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

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
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

    // The package's directory path under the run root, mirroring its whole Go import path
    // ("compress/flate" -> "compress\flate"). The last element is what `go test` makes the working
    // directory's base name; the ANCESTORS matter too, because a package that reads a shared
    // fixture ("../testdata/e.txt", "../../testdata/Isaac.Newton-Opticks.txt") needs those levels to
    // exist INSIDE the sandbox for the relative open() to resolve. Mirroring the import path gives
    // exactly Go's own layout, so the depth is always sufficient and never guessed. It also
    // preserves the property the flat form was chosen for — the parent of the working directory
    // holds nothing but the package (io/fs's TestGlob globs `*/glob.go` against os.DirFS("..") and
    // expects `fs/glob.go`) — since each mirrored level holds only the next.
    private static string PackageDirectoryPath(string importPath)
    {
        string[] segments = importPath.TrimEnd('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePath)
            .Where(segment => !string.IsNullOrEmpty(segment))
            .ToArray();

        return segments.Length == 0 ? "pkg" : Path.Combine(segments);
    }

    private static string SanitizePath(string value) =>
        string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch is '/' or '\\' ? '_' : ch));
}
