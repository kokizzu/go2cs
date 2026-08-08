<#
.SYNOPSIS
    Clean-slate runner for the go2cs behavioral test suite.

.DESCRIPTION
    The behavioral suite shells out to dotnet build / go build / the transpiled .exe from inside an
    MSTest assembly hosted in testhost.exe. A stale testhost / vstest.console / MSBuild worker node
    left resident by a prior run (or by Visual Studio) can hold a file lock on BehavioralTests.dll
    or on a target's bin\obj, making the *next* build fail with MSB3027 or hang indefinitely.

    This script clears those stale lock-holders BEFORE the build (the only place that can be done,
    since the lock manifests at build time, before testhost loads), then runs the suite with
    --blame-hang so a wedged testhost is force-killed and reported instead of left resident.

    Every cleanup it performs is scoped to THIS tree. The process kill is path-scoped, and
    `dotnet build-server shutdown` -- which is machine-global and cannot be scoped -- sits behind
    -ShutdownBuildServers, default OFF. CLAUDE.md's concurrent-session section records why: on
    2026-08-03 one lane's startup shutdown yanked the shared MSBuild servers out from under a
    SIBLING worktree's in-flight compile, producing the same truncated-log / exit -1 signature as a
    name-wide Stop-Process and nothing in the log to explain it. Its ruling -- "while sibling
    sessions may be building, do NOT run it; isolate your own builds instead
    (MSBUILDDISABLENODEREUSE=1, -p:UseSharedCompilation=false)" -- is what the default path now
    does. Disabling node reuse is also what makes the old trailing shutdown redundant rather than
    merely skipped: this run's worker nodes exit with the build, so nothing of ours is left to evict.

    The switch governs the WHOLE run, not just this script. BehavioralTests' [AssemblyCleanup] ran
    the same machine-global shutdown from inside the test host, which no parameter here could reach,
    so it is now gated on the GO2CS_SHUTDOWN_BUILD_SERVERS environment variable that this script
    sets from the switch. A default run therefore performs NO machine-global action anywhere in it,
    and a bare `dotnet test` or a VS Test Explorer run -- which never sets the variable -- gets the
    same safe default.

.PARAMETER Filter
    Optional VSTest filter expression, e.g. "FullyQualifiedName~AtomicField". Omit to run all tests.

.PARAMETER NoBuild
    Pass --no-build to dotnet test (valid only when no *Tests.cs changed since the last build).

.PARAMETER HangTimeout
    Per-test hang timeout handed to --blame-hang-timeout. Default 180s (matches the ~3 min cap).

.PARAMETER ShutdownBuildServers
    Run `dotnet build-server shutdown` before and after the suite, and let the MSTest assembly's
    teardown run it too (via GO2CS_SHUTDOWN_BUILD_SERVERS). Machine-global: it evicts EVERY lane's
    MSBuild/Roslyn/Razor servers, not just this tree's, so it is opt-in and safe only in a solo
    context or at a coordinator-owned quiet point. Leave it off whenever a sibling worktree may be
    building.

.EXAMPLE
    ./run-behavioral-tests.ps1
    ./run-behavioral-tests.ps1 -Filter "FullyQualifiedName~AtomicField" -NoBuild
    ./run-behavioral-tests.ps1 -ShutdownBuildServers   # solo box / quiet point only
#>
[CmdletBinding()]
param(
    [string] $Filter,
    [switch] $NoBuild,
    [string] $HangTimeout = "180s",
    [switch] $ShutdownBuildServers
)

$ErrorActionPreference = "Stop"

# Roots and the platform primitives come from one shared definition, so this script cannot disagree
# with the other behavioral instruments about where anything is (F4, docs/PLAN-linux-operation.md).
. (Join-Path $PSScriptRoot '_paths.ps1')

$slnx = Join-Path $SrcRoot 'go2cs.slnx'

Write-Host "==> Clearing stale test/build processes (prior-run lock-holders)..." -ForegroundColor Cyan

# Stale testhost/vstest from a PRIOR run hold the lock that breaks THIS build. Safe to kill here:
# this script runs in a normal shell, not inside testhost, so we are not killing ourselves.
# PATH-SCOPED, never by name alone: worktree isolation does not isolate Stop-Process, and a
# name-wide kill takes down a SIBLING worktree's in-flight suite (exit -1, log truncated mid-line;
# see CLAUDE.md's concurrent-session kill section — this script was the last name-wide killer).
# Only processes whose executable lives under THIS tree (repo root, two levels up) are fair game.
#
# Off Windows this loop is a deliberate no-op rather than a port: the three names are Windows apphost
# names, and the muxer that hosts them on Linux (`dotnet`) lives outside the tree, so the path guard
# excludes it. That is the right outcome, not a gap -- the MSB3027 class this clears exists because
# Windows file locking is MANDATORY; on Linux an open handle does not block a rewrite, so there is no
# stale lock-holder to clear. The guard's string comparison follows the host's own filesystem rule.
# Unchanged scope: src\tests, two levels up from here. Deliberately NOT widened to $SrcRoot while
# porting -- a kill scope is the one thing that must never grow as a side effect of a path cleanup.
$killScopeRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$pathComparison = if ($IsWindowsHost) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }

foreach ($name in @("testhost", "vstest.console", "MSBuild")) {
    $procs = Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and $_.Path.StartsWith($killScopeRoot, $pathComparison)
    }
    if ($procs) {
        Write-Host "    killing $($procs.Count) x $name (scoped to $killScopeRoot)" -ForegroundColor DarkYellow
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

# Release MSBuild build-server nodes so they stop holding bin\obj handles. OPT-IN, unlike the kill
# loop above: this one cannot be path-scoped at all. `dotnet build-server shutdown` talks to every
# server on the MACHINE, so on a shared checkout it is indistinguishable from killing a sibling
# lane's build. The default path isolates the children THIS script spawns instead (.DESCRIPTION).
if ($ShutdownBuildServers) {
    Write-Host "==> dotnet build-server shutdown" -ForegroundColor Cyan
    & dotnet build-server shutdown | Out-Null
}

# Worker nodes this run spawns exit with it rather than lingering to hold bin\obj handles into the
# next invocation -- the half of the isolation that replaces the SHUTDOWN, as opposed to merely
# dropping it.
$env:MSBUILDDISABLENODEREUSE = '1'

# BehavioralTests' [AssemblyCleanup] runs the SAME machine-global shutdown from inside the test run,
# where the switch above cannot reach it -- so gating only this script's own two calls would leave a
# default run still evicting every lane's servers, just invisibly. Hand the decision across the
# process boundary so one switch governs the whole run. Written in BOTH directions on purpose: an
# ambient value left in the shell by an earlier -ShutdownBuildServers run must not silently re-enable
# the teardown here.
$env:GO2CS_SHUTDOWN_BUILD_SERVERS = if ($ShutdownBuildServers) { '1' } else { '0' }

# Assemble the dotnet test invocation. UseSharedCompilation=false keeps our compiles off the shared
# VBCSCompiler server, the other half: we neither borrow another lane's nor leave one behind.
$testArgs = @(
    "test", $slnx,
    "-c", "Debug",
    "-p:UseSharedCompilation=false",
    "--blame-hang",
    "--blame-hang-timeout", $HangTimeout,
    "--blame-hang-dump-type", "none"
)

if ($NoBuild)              { $testArgs += "--no-build" }
if ($Filter)              { $testArgs += @("--filter", $Filter) }

Write-Host "==> dotnet $($testArgs -join ' ')" -ForegroundColor Cyan
& dotnet @testArgs
$exit = $LASTEXITCODE

# Tear down any nodes our run spawned so the next invocation starts clean. Reachable only under the
# opt-in switch: with node reuse disabled there are none of ours left to tear down, and running it
# unconditionally would evict every sibling lane's servers at the moment this run happens to finish.
if ($ShutdownBuildServers) {
    & dotnet build-server shutdown | Out-Null
}

exit $exit
