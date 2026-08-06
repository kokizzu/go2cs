// BehavioralTestBase.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Comment out the following line to standard dotnet builds instead of publish profiles for C# projects.
// Using publish profiles can increase run-time startup performance, after first run initialization, but
// increases build times due to extra compile time processing, e.g. trimming analysis, etc.
//#define USE_PUBLISH_PROFILES

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Don't enable for timing tests:
//[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace BehavioralTests;

[TestClass]
public abstract class BehavioralTestBase
{
    // Move up from here: "...\go2cs\src\tests\Behavioral\BehavioralTests\bin\Debug\net9.0"
    private const string RootPath = @"..\..\..\..\..\..\..\"; // At "src" folder

    protected const string TestRootPath = @"..\..\..\..\";

    protected static string BinOutput { get; private set; } = null!;

    protected static string NetVersion { get; private set; } = null!;

    protected static string go2cs { get; private set; } = null!;

    // The converter's -go2cspath: the root it reads an imported package's package_info.cs from when it
    // mints the emitted <ImportedTypeAliases> block. NOT the MSBuild $(go2csPath) property of the same
    // name (that one is set in CompileCSProject). Its default is ~/go2cs, so leaving it ambient made the
    // transpiled output -- and with it the golden comparison's verdict -- depend on whatever GO2CSPATH the
    // shell carried (BOARD-next-validation-candidates.md, 2026-08-06). Pinned here to the src root derived
    // from this assembly's own location: the behavioral .csproj files bind $(go2csPath)core\<pkg> with
    // MSBuild $(go2csPath) -> $(SolutionDir) -> src\, so src\core is what they compile against and src\ is
    // the only root whose metadata describes those assemblies.
    protected static string Go2csRoot { get; private set; } = null!;

    protected static string PublishProfile { get; private set; } = "win-x64";

    protected static string TargetConfig { get; private set; } = "Release";

    // Default timeout for build/transpile child processes (ms). Matches the ~3 min suite cap from
    // CLAUDE.md; a child that exceeds this is treated as hung and killed (with its whole process tree)
    // rather than blocking the suite forever via an unbounded WaitForExit.
    protected const int DefaultExecTimeoutMs = 180_000;

    // Tighter timeout for *running* a transpiled program. A deadlocked converted program (channel/
    // goroutine hang, a process blocked on stdin) is the worst offender: it must fail one test fast,
    // not wedge the run and leave an orphaned testhost holding a lock on BehavioralTests.dll.
    protected const int RunExecTimeoutMs = 30_000;

    protected static string GetCSExecPath(string projPath, string targetProject)
    {
    #if USE_PUBLISH_PROFILES
        return Path.Combine(projPath, $@"bin\{TargetConfig}\{NetVersion}\publish\{PublishProfile}");
    #else
        return Path.Combine(projPath, $@"bin\{TargetConfig}\{NetVersion}\");
    #endif
    }

    protected static string GetGoExePath(string projPath, string targetProject)
    {
        return Path.Combine(projPath, $@"bin\{TargetConfig}\Go");
    }

    private static readonly ConcurrentDictionary<string, object> s_projectLocks = new(StringComparer.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected static void Init(TestContext context)
    {
        string execPath = Directory.GetCurrentDirectory();
        string go2csSrc = Path.GetFullPath($@"{execPath}{RootPath}go2cs\");
        string go2csBin = Path.Combine(go2csSrc, @"bin\");

        // Resolved before the up-to-date early return below, so every path through Init sets it. The
        // trailing separator must go: a Windows command line ends the quoted argument on the closing
        // quote, and a backslash immediately before it escapes that quote instead.
        Go2csRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath($@"{execPath}{RootPath}"));

        int projectNameIndex = execPath.IndexOf(nameof(BehavioralTests), StringComparison.OrdinalIgnoreCase);

        BinOutput = execPath[(projectNameIndex + nameof(BehavioralTests).Length)..];
        NetVersion = BinOutput.Split(@"\", StringSplitOptions.RemoveEmptyEntries)[^1];

        if (!Directory.Exists(go2csBin))
            Directory.CreateDirectory(go2csBin);

        go2cs = Path.Combine(go2csBin, "go2cs.exe");

        if (File.Exists(go2cs))
        {
            FileInfo go2csExeInfo = new(go2cs);

            // If exe is newer than all .go source files, can skip build
            if (Directory.GetFiles(go2csSrc, "*.go").Select(fileName => new FileInfo(fileName)).All(info => go2csExeInfo.LastWriteTimeUtc > info.LastWriteTimeUtc))
                return;
        }

        int exitCode = Exec(context, "go", $"build -o \"{go2cs}\"", go2csSrc);

        if (exitCode != 0)
            throw new InvalidOperationException($"\"go build\" failed with exit code {exitCode:N0}");

        if (!File.Exists(go2cs))
            throw new InvalidOperationException($"Failed to find \"go2cs.exe\" build for testing, check path: {go2cs}");
    }

    public TestContext? TestContext { get; set; }

    protected int Exec(string application, string? arguments, string? workingDir = null, DataReceivedEventHandler? outputHandler = null, int timeoutMs = DefaultExecTimeoutMs, DataReceivedEventHandler? errorHandler = null)
    {
        return Exec(TestContext, application, arguments, workingDir, outputHandler, timeoutMs, errorHandler);
    }

    protected static int Exec(TestContext? context, string application, string? arguments, string? workingDir = null, DataReceivedEventHandler? outputHandler = null, int timeoutMs = DefaultExecTimeoutMs, DataReceivedEventHandler? errorHandler = null)
    {
        ProcessStartInfo startInfo = new()
        {
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = application,
            Arguments = arguments ?? "",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Disable MSBuild node reuse for every child we spawn. Persistent MSBuild worker nodes left
        // alive by in-test "dotnet build" calls are the root cause of the testhost/MSB3027 lock
        // contention: they keep handles on target bin+obj (and on BehavioralTests.dll) across runs.
        // Tearing them down on each child's exit removes the lingering lock-holders. Harmless for
        // the go/go2cs/transpiled-exe children, which ignore these variables.
        startInfo.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";

        if (!string.IsNullOrWhiteSpace(workingDir))
            startInfo.WorkingDirectory = workingDir;

        using Process process = new();
        
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;

        if (outputHandler is null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    context?.WriteLine(e.Data);
            };
        }
        else
        {
            process.OutputDataReceived += outputHandler;
        }

        // stderr routes to the dedicated errorHandler when one is provided (so callers can compare
        // the streams separately); otherwise it falls back to the merged/legacy behavior.
        if (errorHandler is not null)
        {
            process.ErrorDataReceived += errorHandler;
        }
        else if (outputHandler is null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    context?.WriteLine($"[ErrOut]: {e.Data}");
            };
        }
        else
        {
            process.ErrorDataReceived += outputHandler;
        }

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeoutMs))
        {
            // Hung child: kill it and its entire descendant tree so nothing it spawned (MSBuild
            // nodes, a deadlocked transpiled program) survives to lock files or keep testhost alive.
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have exited in the race between the timeout and the kill; ignore.
            }

            // Allow the killed tree and async output handlers a brief window to drain.
            process.WaitForExit(5000);

            throw new TimeoutException($"Process \"{application} {arguments}\" in \"{workingDir ?? Directory.GetCurrentDirectory()}\" exceeded its {timeoutMs:N0} ms timeout and was terminated along with its child process tree.");
        }

        // Per Process docs: after a timed WaitForExit(int) returns true, call the parameterless
        // overload so the asynchronous output/error handlers are guaranteed to finish before ExitCode.
        process.WaitForExit();

        return process.ExitCode;
    }

    protected void TranspileProject(string targetProject, bool forceBuild = false)
    {
        string targetPath = $"{TestRootPath}{targetProject}";
        string csproj = Path.GetFullPath($@"{targetPath}\{targetProject}.csproj");
        string projPath = Path.GetFullPath($"{targetPath}");

        object projectLock = s_projectLocks.GetOrAdd(targetProject, _ => new object());

        lock (projectLock)
        {
            if (!forceBuild && File.Exists(csproj))
            {
                // If all .cs files are newer than associated .go files AND newer than the converter that
                // produced them, skip build. The converter must be part of this test: converter work is
                // the normal case where the .go files DON'T change, so a .go-only check would leave every
                // project "up to date" and let the Target/Output phases validate the PREVIOUS converter's
                // output against goldens that same converter generated -- a false green.
                // PRODUCTION sources only: a production transpile excludes `_test.go`, so an in-package
                // test file has no matching .cs and would pin this check permanently out of date.
                DateTime go2csTime = File.GetLastWriteTimeUtc(go2cs);
                FileInfo[] goFiles = GoPackageDirs(projPath)
                    .SelectMany(ProductionGoFiles)
                    .Select(fileName => new FileInfo(fileName)).ToArray();
                bool allUpToDate = true;

                foreach (FileInfo goFile in goFiles)
                {
                    FileInfo csFile = new(Path.ChangeExtension(goFile.FullName, ".cs"));

                    if (csFile.Exists && csFile.LastWriteTimeUtc > goFile.LastWriteTimeUtc && csFile.LastWriteTimeUtc > go2csTime)
                        continue;

                    allUpToDate = false;
                    break;
                }

                if (allUpToDate)
                    return;
            }

            int exitCode;

            foreach (string pkgPath in GoPackageDirs(projPath))
                Assert.AreEqual(0, exitCode = Exec(go2cs, $"-go2cspath \"{Go2csRoot}\" \"{pkgPath}\""), $"go2cs transpile for \"{targetProject}\" package \"{Path.GetFileName(pkgPath)}\" failed with exit code {exitCode:N0}");
        }
    }

    // A production transpile converts the package's PRODUCTION sources only -- go/packages excludes
    // `_test.go` -- so an in-package test file has no `.cs`, by design (it is still a real input: the
    // converter scans the sibling test half for declarator names and for globals whose address it takes).
    private static string[] ProductionGoFiles(string pkgPath) =>
        Directory.GetFiles(pkgPath, "*.go")
            .Where(fileName => !fileName.EndsWith("_test.go", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    // Every Go package directory a project owns, DEEPEST-FIRST. Most projects are a single package, but
    // 22 carry nested sub-libraries (IoLike\FsLike, VersionedImport\vlib, …) that the converter must be
    // invoked on separately -- and BEFORE their parent, because a sub-library's generated
    // package_info.cs is an input to the parent's transpile (the parent reads its sibling's
    // [assembly: GoImplement] records when deciding whether to mint a local value adapter). Walking
    // top-level only left those sub-libraries permanently un-regenerated, which both froze them at an
    // old converter and made the parent's golden unable to fail on a regression in that area.
    private static string[] GoPackageDirs(string projPath) =>
        new[] { projPath }
            .Concat(Directory.GetDirectories(projPath, "*", SearchOption.AllDirectories)
                .Where(dirName => !dirName.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase) &&
                                  !dirName.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase))
                .Where(dirName => ProductionGoFiles(dirName).Length > 0))
            .OrderByDescending(dirName => dirName.Count(c => c == Path.DirectorySeparatorChar))
            .ThenBy(dirName => dirName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    protected void CompileCSProject(string targetProject, bool forceBuild = false)
    {
        string targetPath = $"{TestRootPath}{targetProject}";
        string projPath = Path.GetFullPath($"{targetPath}");
        string csExePath = GetCSExecPath(projPath, targetProject);
        string projExe = Path.Combine(csExePath, $"{targetProject}.exe");
        object projectLock = s_projectLocks.GetOrAdd(targetProject, _ => new object());

        lock (projectLock)
        {
            // Set 'go2csPath' environment variable
            Environment.SetEnvironmentVariable("go2csPath", Path.GetFullPath($@"{TestRootPath}..\..\"));

            if (!forceBuild && File.Exists(projExe))
            {
                FileInfo projExeInfo = new(projExe);

                // If exe is newer than all .cs source files, can skip build
                if (Directory.GetFiles(projPath, "*.cs").Select(fileName => new FileInfo(fileName)).All(info => projExeInfo.LastWriteTimeUtc > info.LastWriteTimeUtc))
                    return;
            }

            int exitCode;

            // Compile C# project
        #if USE_PUBLISH_PROFILES
            Assert.AreEqual(0, exitCode = Exec("dotnet", $"publish \"{Path.Combine(projPath, $"{targetProject}.csproj")}\" -f {NetVersion} -r {PublishProfile} -c {TargetConfig} --sc true -o \"{csExePath}\""), $"dotnet publish for \"{targetProject}\" failed with exit code {exitCode:N0}");
        #else
            Assert.AreEqual(0, exitCode = Exec("dotnet", $"build --configuration {TargetConfig} \"{Path.Combine(projPath, $"{targetProject}.csproj")}\""), $"dotnet build for \"{targetProject}\" failed with exit code {exitCode:N0}");
        #endif

            // If matching console output, run once after compile to ensure any first run initialization
            // steps are completed. The exit code is intentionally not asserted: a program may
            // legitimately exit nonzero (e.g. an unrecovered panic exits 2, like Go) — the output
            // comparison phase validates the exit code differentially against the Go binary.
            if (MatchConsoleOutput(targetProject))
                Exec(projExe, null, csExePath, timeoutMs: RunExecTimeoutMs);
        }
    }

    protected void CompileGoProject(string targetProject)
    {
        string targetPath = $"{TestRootPath}{targetProject}";
        string projPath = Path.GetFullPath($"{targetPath}");
        string goExePath = GetGoExePath(projPath, targetProject);

        if (!Directory.Exists(goExePath))
            Directory.CreateDirectory(goExePath);
        
        object projectLock = s_projectLocks.GetOrAdd(targetProject, _ => new object());

        lock (projectLock)
        {
            int exitCode;

            // Make sure Go module is initialized
            if (!File.Exists(Path.Combine(projPath, "go.mod")))
                Assert.AreEqual(0, exitCode = Exec("go", $"mod init go2cs/{targetProject}", projPath), $"go build for \"{targetProject}\" failed with exit code {exitCode:N0}");

            // Compile Go project
            Assert.AreEqual(0, exitCode = Exec("go", $"build -o \"{goExePath}\"", projPath), $"go build for \"{targetProject}\" failed with exit code {exitCode:N0}");
        }
    }

    private static bool MatchConsoleOutput(string targetProject)
    {
        // Access "package_info.cs" file for the target test project
        string packageInfoFile = Path.GetFullPath($@"{TestRootPath}{targetProject}\package_info.cs");

        if (!File.Exists(packageInfoFile))
            return false;

        string[] packageInfoLines = File.ReadAllLines(packageInfoFile);

        // Check for "GoTestMatchingConsoleOutput" attribute -- for now, just check for its presence
        // by looking for the attribute name in the file on its own line. Future implementations could
        // load assembly and verify attribute presence via reflection -- this is a simpler approach:
        return packageInfoLines.Any(line => line.Trim().Equals("[GoTestMatchingConsoleOutput]"));
    }
}
