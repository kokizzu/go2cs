<#
.SYNOPSIS
    Fast converter no-regression check: re-transpile every behavioral project and report any change
    to the generated C#. No compile, no run, no testhost.

.DESCRIPTION
    Per CLAUDE.md: byte-identical generated .cs  =>  identical compile+run  =>  identical results.
    So after a converter change, the cheapest regression signal is simply to re-transpile every
    Tests\Behavioral\* project and `git status` the generated .cs files. If nothing changed, the
    converter change is provably output-neutral for the behavioral corpus with zero build/run cost.
    If files changed, this prints them so you can inspect (intended new goldens) vs. revert (regression).

    This regenerates the .cs in-place. That IS the check: identical output leaves the tree clean.

.PARAMETER Revert
    After reporting, `git checkout` any changed .cs back to HEAD (use when you only wanted the check,
    not to keep regenerated output).

.EXAMPLE
    ./check-no-regression.ps1
    ./check-no-regression.ps1 -Revert
#>
[CmdletBinding()]
param(
    [switch] $Revert
)

$ErrorActionPreference = "Stop"

$repoRoot     = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$converterSrc = Join-Path $repoRoot "src\go2cs"
$go2csExe     = Join-Path $converterSrc "bin\go2cs.exe"
$behavioral   = $PSScriptRoot

# 0. Solution-integrity preflight (fast, static, <1s): every behavioral test project on disk must be
#    registered in go2cs.slnx. A missing registration builds fine here (the harness builds each .csproj
#    by path) but breaks the go2cs.slnx build in Visual Studio, so the transpile no-regression loop
#    below would never catch it. Fail fast before the expensive re-transpile if the tree is inconsistent.
Write-Host "==> solution-integrity preflight" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "check-solution-integrity.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 1. Build a current go2cs.exe (cheap; only relinks if the Go sources changed).
Write-Host "==> go build -o bin\go2cs.exe" -ForegroundColor Cyan
Push-Location $converterSrc
try {
    & go build -o $go2csExe
    if ($LASTEXITCODE -ne 0) { throw "go build failed ($LASTEXITCODE)" }
}
finally { Pop-Location }

# 2. Re-transpile every behavioral Go package. A test-target dir is defined by Go-source presence (a
#    *.go file), not by a .csproj (cf. commit 2cbe71947). This naturally excludes the C# tooling dirs
#    BehavioralTests (the MSTest runner) and BehavioralRunner (the standalone runner) — neither has Go
#    source, so transpiling them just fails with "go: cannot find main module".
#
#    The walk is RECURSIVE because a project's Go source can span nested sub-library packages
#    (IoLike\FsLike, VersionedImport\vlib, CrossPackageArrayZeroValue\bufpkg, …). Those are not
#    decoration: a sub-library's generated package_info.cs is an INPUT to its parent's transpile — the
#    parent reads the sibling's [assembly: GoImplement] records to decide whether to mint a local value
#    adapter. A top-level-only walk therefore froze all 22 of them at whatever converter last touched
#    them (17 files measurably stale by 2026-08-02) AND left the parent reading stale-but-plausible
#    records, so a converter regression in that area could not make the parent's golden fail — a false
#    green for the ForeignValueImplementSuppression / ValueAdapterDynamicType / SamePackageImplementNoWitness
#    guards specifically. Order is DEEPEST-FIRST so a sub-library is regenerated before its parent
#    consumes it.
$projects = Get-ChildItem -Path $behavioral -Directory -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)(\\|$)' } |
    Where-Object { Get-ChildItem $_.FullName -Filter *.go -File } |
    Sort-Object -Property @{ Expression = { ($_.FullName -split '\\').Count }; Descending = $true },
                          @{ Expression = { $_.FullName }; Descending = $false }

Write-Host "==> transpiling $($projects.Count) behavioral packages (deepest-first)..." -ForegroundColor Cyan
# go2cs writes advisory WARNINGs to stderr (e.g. unsafe.Sizeof usage). Under $ErrorActionPreference='Stop'
# native-command stderr surfaces as a terminating NativeCommandError and aborts the loop, so relax it here
# and gate purely on the exit code; merge stderr into the pipeline so warnings are swallowed by Out-Null.
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    foreach ($proj in $projects) {
        & $go2csExe $proj.FullName 2>&1 | Out-Null
        # Report the path relative to the behavioral root: a bare .Name is ambiguous for nested
        # sub-libraries (three of them are called "inner", two "latelib").
        if ($LASTEXITCODE -ne 0) {
            Write-Host "    [transpile FAILED] $($proj.FullName.Substring($behavioral.Length).TrimStart('\'))" -ForegroundColor Red
        }
    }
}
finally { $ErrorActionPreference = $savedEAP }

# 3. Report any changed generated .cs under the behavioral tree. Both C# tooling dirs are excluded:
#    their .cs is HAND-WRITTEN source, not converter output, so an edit to either is a deliberate
#    harness change and reporting it as converter drift is pure noise. (BehavioralRunner was missing
#    from this filter until 2026-08-02, so editing the runner made CNR accuse itself of a regression.)
$changed = & git -C $repoRoot status --short -- "src/Tests/Behavioral/*.cs" |
    Where-Object { $_ -notmatch "Behavioral(Tests|Runner)/" }

if (-not $changed) {
    Write-Host "==> NO REGRESSION: generated C# is byte-identical across all behavioral projects." -ForegroundColor Green
    exit 0
}

Write-Host "==> CHANGED generated C# (inspect: intended new golden vs. regression):" -ForegroundColor Yellow
$changed | ForEach-Object { Write-Host "    $_" }

if ($Revert) {
    # Same two exclusions as the report above, and for a sharper reason: without them this checkout
    # DESTROYS uncommitted hand-edits to the harness sources themselves (they are .cs under
    # Tests\Behavioral, so the bare pathspec swept them up).
    Write-Host "==> -Revert: restoring changed .cs to HEAD" -ForegroundColor Cyan
    & git -C $repoRoot checkout -- "src/Tests/Behavioral/*.cs" `
        ":(exclude)src/Tests/Behavioral/BehavioralTests/*" `
        ":(exclude)src/Tests/Behavioral/BehavioralRunner/*"
}

exit 1
