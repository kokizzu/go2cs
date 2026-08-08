// AssemblySetup.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BehavioralTests;

[TestClass]
public sealed class AssemblySetup
{
    // Opt-in gate for the machine-global teardown in Cleanup(). Set by run-behavioral-tests.ps1 from
    // its -ShutdownBuildServers switch, so ONE decision covers the whole run instead of the script
    // gating its own two calls while this assembly quietly makes a third. Anything other than "1" or
    // "true" — including unset, which is what a bare `dotnet test` and VS Test Explorer see — leaves
    // the teardown off, so the safe behavior is the one you get by default.
    private const string ShutdownBuildServersVariable = "GO2CS_SHUTDOWN_BUILD_SERVERS";

    // NOTE: We deliberately do NOT kill stray "testhost" processes here. This code runs *inside* the
    // current testhost, so a blanket kill would terminate the run itself. Worse, the prior-run lock a
    // stale testhost holds on BehavioralTests.dll manifests at *build* time (MSB3027) — before this
    // assembly can even load — so the only effective place to clear stale hosts is a pre-build step
    // outside the test process. That lives in run-behavioral-tests.ps1 (the recommended entry point).
    [AssemblyCleanup]
    public static void Cleanup()
    {
        // Release any MSBuild build-server nodes spawned by the in-test "dotnet build" calls so they
        // don't linger holding file locks on this assembly / target bin+obj for the next run.
        //
        // OPT-IN, because "dotnet build-server shutdown" is MACHINE-GLOBAL: it evicts every MSBuild /
        // Roslyn / Razor server on the box, not just the ones this run spawned, so on a shared
        // checkout it is indistinguishable from killing a sibling worktree's in-flight compile (the
        // 2026-08-03 incident in CLAUDE.md's concurrent-session section — one lane's shutdown yanked
        // another's build servers, giving a truncated log and exit -1 with nothing to explain it).
        // Unlike the process kill in run-behavioral-tests.ps1 this cannot be path-scoped, so the only
        // safe default is not to do it. That costs nothing: node reuse is ALREADY disabled per child
        // (MSBUILDDISABLENODEREUSE) in BehavioralTestBase.Exec, so this run's nodes exit with their
        // child and there is nothing of ours left to evict — this was always belt-and-suspenders.
        if (!ShutdownBuildServersRequested())
            return;

        // Best-effort only — never fail the run on cleanup.
        try
        {
            using Process process = new();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build-server shutdown",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.Start();

            if (!process.WaitForExit(30_000))
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort teardown; ignore any failure.
        }
    }

    // Deliberately allow-list the two enabling spellings rather than reject a deny-list: an
    // unrecognized value must fall to the safe side, not to the one that reaches off this tree.
    private static bool ShutdownBuildServersRequested()
    {
        string? setting = Environment.GetEnvironmentVariable(ShutdownBuildServersVariable);

        return setting is not null &&
               (setting.Equals("1", StringComparison.Ordinal) ||
                setting.Equals("true", StringComparison.OrdinalIgnoreCase));
    }
}
