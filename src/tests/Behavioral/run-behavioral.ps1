<#
.SYNOPSIS
    Build and run the standalone go2cs behavioral runner (Tier 3) -- no MSTest, no testhost.

.DESCRIPTION
    The runner (BehavioralRunner) executes the same four phases as the MSTest suite
    (Transpile -> Compile -> TargetComparison -> OutputComparison) as a plain console app that is not
    hosted in testhost.exe, so it cannot wedge on the testhost/MSB3027 self-lock. It also collapses the
    180 per-project "dotnet build" calls into one parallel MSBuild invocation. This wrapper just builds
    the runner (fast, dependency-free) and forwards all arguments to it.

.PARAMETER RunnerArgs
    Forwarded verbatim to BehavioralRunner. Examples:
      --filter <substr>     only matching projects
      --phase <list>        transpile,compile,target,output,all
      --update-targets      regenerate .cs.target goldens, then stop
      --list                list matched projects

    Timeout budgets, in SECONDS (flag > environment variable > default). Build defaults are sized
    for the slowest legitimate host (a timeout is a safety net against a hung child, never a
    performance assumption); a fast lane that wants fail-fast opts DOWN explicitly:
      --build-timeout       one-shot parallel build of every C# target   (env GO2CS_BUILD_TIMEOUT,     default 2400)
      --build-one-timeout   per-project build / shared-dep pre-build     (env GO2CS_BUILD_ONE_TIMEOUT, default 300)
      --transpile-timeout   one converter invocation                     (env GO2CS_TRANSPILE_TIMEOUT, default 60)
      --run-timeout         one program run in the Output phase          (env GO2CS_RUN_TIMEOUT,       default 30)

    Exceeding a budget is reported as NOT MEASURED, never as a compile or behavioral failure -- but it
    still fails the run, because an unmeasured project must not read as a pass. Measured 2026-08-10 on
    an i7-5820K (~3x slower than the retired baseline desktop) at 555 packages, a 300 s batch budget
    could not be met cold OR warm -- the measurement that sized the current defaults.

.PARAMETER IgnoreDiskPreflight
    Proceed even when the repo drive is below the 25 GB free-space floor. Consumed here, never
    forwarded to the runner.

.EXAMPLE
    ./run-behavioral.ps1
    ./run-behavioral.ps1 --filter Atomic
    ./run-behavioral.ps1 --phase transpile,target
    ./run-behavioral.ps1 --build-timeout 300 --build-one-timeout 180   # fast lane, fail-fast budgets
#>
[CmdletBinding()]
param(
    [switch] $IgnoreDiskPreflight,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $RunnerArgs
)

$ErrorActionPreference = "Stop"

# ---- disk preflight -----------------------------------------------------------------------------
# Three separate 2026-08-13 incidents traced back to a full repo drive, and NOT ONE of them named the
# disk in its own output: writes failed MID-RUN and surfaced as corpus FAILURES (false reds nobody
# could reproduce), and a failed write left a TRACKED file TRUNCATED, which then reads as real drift.
# Both shapes cost an investigation before anyone thought to look at free space, so this names the
# number first. GetPathRoot + DriveInfo is the portable pair: 'D:\' on Windows, '/' elsewhere.
$freeGB = [math]::Round(([System.IO.DriveInfo]::new([System.IO.Path]::GetPathRoot($PSScriptRoot))).AvailableFreeSpace / 1GB, 1)

if ($freeGB -lt 25) {
    Write-Host "*** DISK PREFLIGHT: $freeGB GB free on the repo drive -- below the 25 GB floor ***" -ForegroundColor Red
    Write-Host '    Below this, writes fail mid-run: builds and conversions report FALSE REDS, and a' -ForegroundColor Red
    Write-Host '    partial write leaves a TRACKED FILE TRUNCATED (three such incidents, 2026-08-13).' -ForegroundColor Red
    Write-Host '    Free space, or pass -IgnoreDiskPreflight to proceed with unmeasurable results.' -ForegroundColor Red

    if (-not $IgnoreDiskPreflight) { exit 1 }
}

. (Join-Path $PSScriptRoot '_paths.ps1')

$runnerProj = Join-Path $PSScriptRoot 'BehavioralRunner/BehavioralRunner.csproj'
$runnerExe  = Join-Path $PSScriptRoot "BehavioralRunner/bin/Debug/$NetVersion/BehavioralRunner$ExeSuffix"

Write-Host "==> building BehavioralRunner..." -ForegroundColor Cyan
& dotnet build $runnerProj -c Debug -clp:ErrorsOnly --nologo | Out-Null
if ($LASTEXITCODE -ne 0) { throw "BehavioralRunner build failed ($LASTEXITCODE)" }

& $runnerExe @RunnerArgs
exit $LASTEXITCODE
