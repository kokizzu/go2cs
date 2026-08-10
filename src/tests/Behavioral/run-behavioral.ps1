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

.EXAMPLE
    ./run-behavioral.ps1
    ./run-behavioral.ps1 --filter Atomic
    ./run-behavioral.ps1 --phase transpile,target
    ./run-behavioral.ps1 --build-timeout 300 --build-one-timeout 180   # fast lane, fail-fast budgets
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $RunnerArgs
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot '_paths.ps1')

$runnerProj = Join-Path $PSScriptRoot 'BehavioralRunner/BehavioralRunner.csproj'
$runnerExe  = Join-Path $PSScriptRoot "BehavioralRunner/bin/Debug/net9.0/BehavioralRunner$ExeSuffix"

Write-Host "==> building BehavioralRunner..." -ForegroundColor Cyan
& dotnet build $runnerProj -c Debug -clp:ErrorsOnly --nologo | Out-Null
if ($LASTEXITCODE -ne 0) { throw "BehavioralRunner build failed ($LASTEXITCODE)" }

& $runnerExe @RunnerArgs
exit $LASTEXITCODE
