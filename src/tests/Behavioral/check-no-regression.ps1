<#
.SYNOPSIS
    Fast converter no-regression check: re-transpile every behavioral project and report any change
    to the generated C#. No compile, no run, no testhost.

.DESCRIPTION
    Per CLAUDE.md: byte-identical generated .cs  =>  identical compile+run  =>  identical results.
    So after a converter change, the cheapest regression signal is simply to re-transpile every
    tests\Behavioral\* project and `git status` the generated .cs files. If nothing changed, the
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

# Roots, the executable suffix and the separator-agnostic path helpers come from one shared
# definition so this script, run-behavioral.ps1 and check-solution-integrity.ps1 cannot disagree --
# and so none of them carries a backslash literal, which off Windows fails SILENTLY rather than
# loudly (F4, docs/PLAN-linux-operation.md).
. (Join-Path $PSScriptRoot '_paths.ps1')

$repoRoot     = $RepoRoot
$converterSrc = $ConverterSrc
$go2csExe     = $Go2csExe
$behavioral   = $BehavioralRoot

# The converter's -go2cspath (env GO2CSPATH, default ~/go2cs) is the root it reads an imported
# package's package_info.cs from to mint the emitted <ImportedTypeAliases> block -- NOT the MSBuild
# $(go2csPath) property of the same name. Left ambient, this gate's VERDICT moved with whatever the
# shell happened to carry: a stale ~/go2cs stub deploy drops the reflect aliases from four projects
# while the repository tree adds time/syscall/encoding-json/io aliases to twelve others, and the two
# sets are disjoint (BOARD-next-validation-candidates.md, 2026-08-06). Pin it, from THIS script's own
# location, to the tree the behavioral .csproj files actually compile against: MSBuild $(go2csPath)
# resolves to $(SolutionDir) = src\, so src\core is what the tests link and src\ is the only root
# whose metadata describes those assemblies.
$go2csRoot    = Join-Path $repoRoot "src"

# 0. Solution-integrity preflight (fast, static, <1s): every behavioral test project on disk must be
#    registered in go2cs.slnx. A missing registration builds fine here (the harness builds each .csproj
#    by path) but breaks the go2cs.slnx build in Visual Studio, so the transpile no-regression loop
#    below would never catch it. Fail fast before the expensive re-transpile if the tree is inconsistent.
Write-Host "==> solution-integrity preflight" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "check-solution-integrity.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 1. Build a current converter binary (cheap; only relinks if the Go sources changed).
Write-Host "==> go build -o $go2csExe" -ForegroundColor Cyan
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
    Where-Object { $_.FullName -notmatch "$SepPattern(bin|obj)($SepPattern|`$)" } |
    Where-Object { Get-ChildItem $_.FullName -Filter *.go -File } |
    Sort-Object -Property @{ Expression = { Get-PathDepth $_.FullName }; Descending = $true },
                          @{ Expression = { $_.FullName }; Descending = $false }

# Both filters above used to be anchored on a literal '\'. Off Windows neither ERRORS -- the bin/obj
# exclusion matches nothing (so build output gets transpiled) and the depth split returns 1 for every
# path (so the deepest-first order collapses to alphabetical, silently reverting the fix that closed
# FALSE-GREEN route #3: a nested sub-library must be regenerated BEFORE the parent that reads its
# package_info.cs). Neither would fail this gate; it would just quietly stop proving what it claims.
# These two assertions make that impossible. Both are shape assertions rather than pinned counts --
# the corpus grows continuously and CLAUDE.md's standing instruction is to measure, not to decrement.
$depths = $projects | ForEach-Object { Get-PathDepth $_.FullName }

if ($projects.Count -lt 400) {
    Write-Host "==> ENUMERATION BROKEN: found only $($projects.Count) behavioral packages under $behavioral." -ForegroundColor Red
    Write-Host "    The corpus has been in the 500s since 2026-08; a collapse this large means the walk" -ForegroundColor Red
    Write-Host "    or its bin/obj filter is matching on the wrong path separator, not that tests vanished." -ForegroundColor Red
    exit 1
}

if (($depths | Sort-Object -Unique).Count -lt 2) {
    Write-Host "==> DEPTH SORT BROKEN: every one of the $($projects.Count) packages measured the same depth." -ForegroundColor Red
    Write-Host "    The tree has nested sub-library packages (IoLike/FsLike, VersionedImport/vlib, ...), so" -ForegroundColor Red
    Write-Host "    a uniform depth means Get-PathDepth is not splitting on this platform's separator and" -ForegroundColor Red
    Write-Host "    the deepest-first invariant is not being applied." -ForegroundColor Red
    exit 1
}

Write-Host "==> transpiling $($projects.Count) behavioral packages (deepest-first, depths $(($depths | Measure-Object -Minimum).Minimum)-$(($depths | Measure-Object -Maximum).Maximum))..." -ForegroundColor Cyan
# go2cs writes advisory WARNINGs to stderr (e.g. unsafe.Sizeof usage). Under $ErrorActionPreference='Stop'
# native-command stderr surfaces as a terminating NativeCommandError and aborts the loop, so relax it here
# and gate purely on the exit code; merge stderr into the pipeline so warnings are swallowed by Out-Null.
$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
try {
    foreach ($proj in $projects) {
        & $go2csExe -go2cspath $go2csRoot $proj.FullName 2>&1 | Out-Null
        # Report the path relative to the behavioral root: a bare .Name is ambiguous for nested
        # sub-libraries (three of them are called "inner", two "latelib").
        if ($LASTEXITCODE -ne 0) {
            Write-Host "    [transpile FAILED] $(Get-RelativeDisplayPath $proj.FullName $behavioral)" -ForegroundColor Red
        }
    }
}
finally { $ErrorActionPreference = $savedEAP }

# 3. Report any changed generated .cs under the behavioral tree. Both C# tooling dirs are excluded:
#    their .cs is HAND-WRITTEN source, not converter output, so an edit to either is a deliberate
#    harness change and reporting it as converter drift is pure noise. (BehavioralRunner was missing
#    from this filter until 2026-08-02, so editing the runner made CNR accuse itself of a regression.)
$changed = & git -C $repoRoot status --short -- "src/tests/Behavioral/*.cs" |
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
    # tests\Behavioral, so the bare pathspec swept them up).
    Write-Host "==> -Revert: restoring changed .cs to HEAD" -ForegroundColor Cyan
    & git -C $repoRoot checkout -- "src/tests/Behavioral/*.cs" `
        ":(exclude)src/tests/Behavioral/BehavioralTests/*" `
        ":(exclude)src/tests/Behavioral/BehavioralRunner/*"
}

exit 1
