<#
.SYNOPSIS
    Fast converter no-regression check: re-transpile every behavioral project and report any change
    to the converter's output (.cs and .csproj). No compile, no run, no testhost.

.DESCRIPTION
    Per CLAUDE.md: byte-identical generated .cs  =>  identical compile+run  =>  identical results.
    So after a converter change, the cheapest regression signal is simply to re-transpile every
    tests\Behavioral\* project and `git status` the converter-emitted files -- the generated .cs AND
    the generated .csproj (the transpile rewrites both). If nothing changed, the converter change is
    provably output-neutral for the behavioral corpus with zero build/run cost. If files changed,
    this prints them so you can inspect (intended new goldens) vs. revert (regression).

    This regenerates the output in-place. That IS the check: identical output leaves the tree clean.

    The byte-identical verdict is only meaningful for a package whose output this run actually
    regenerated, so a package the converter could not fully convert (best-effort/untyped conversion,
    a recovered visitor panic, or a non-zero exit) fails the gate as NOT MEASURED rather than
    counting toward the green verdict (coordinator ruling 2026-08-08, docs/PLAN-linux-operation.md:
    on Linux, FindFirstFileData does not type-check, its main.cs is never rewritten, and this gate
    reported "byte-identical" against a file it never regenerated).

.PARAMETER Revert
    After reporting, `git checkout` any changed .cs back to HEAD (use when you only wanted the check,
    not to keep regenerated output).

.EXAMPLE
    ./check-no-regression.ps1
    ./check-no-regression.ps1 -Revert
#>
[CmdletBinding()]
param(
    [switch] $Revert,

    # POSITIVE CONTROL for the platform-alias-drift check below, as a parameter so exercising it
    # needs no tracked-file edit (the same idiom check-solution-integrity.ps1's -InjectReference
    # uses). Naming a documented member here removes it from the list, and the check must then
    # report that package as UNDOCUMENTED and fail. A check that has never been made to fail is
    # not a measurement.
    [string[]] $OmitAliasDriftMembers = @(),

    # Run ONLY the platform-alias-drift classification over the CURRENT working tree and exit --
    # no transpile, no git status of anything else. This exists so the check is CONTROLLABLE: a
    # guard whose only exercise costs a 25-minute full run is a guard nobody will positive-control,
    # and one that has never been made to fail is not a measurement. Intended use is immediately
    # after a real run has left the tree dirty, once plain and once with -OmitAliasDriftMembers.
    [switch] $AliasDriftCheckOnly
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
if (-not $AliasDriftCheckOnly) {

Write-Host "==> go build -o $go2csExe" -ForegroundColor Cyan
Push-Location $converterSrc
try {
    & go build -o $go2csExe
    if ($LASTEXITCODE -ne 0) { throw "go build failed ($LASTEXITCODE)" }
}
finally { Pop-Location }

}

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
# go2cs writes WARNINGs to stderr. Under $ErrorActionPreference='Stop' native-command stderr surfaces
# as a terminating NativeCommandError and aborts the loop, so relax it here and CAPTURE the merged
# output instead of discarding it (it was `| Out-Null` until 2026-08-08, which is how a loud converter
# warning could coexist with a green verdict). Two warning classes mean this run did NOT fully
# regenerate the package's output, so the byte-identical verdict below would be vacuous for it:
#   - "did not fully type-check" (conversionDriver.go) -- best-effort/untyped conversion;
#   - "visit file error" (a recovered visitor panic) -- a source file's emission was skipped.
# Those, and a non-zero converter exit (the same hole's previously-unhandled sibling: the loop printed
# the failure but the verdict stayed green), fail the gate by name as NOT MEASURED. Every other
# WARNING is advisory (e.g. unsafe.Sizeof usage) -- present on a healthy run, counted, never fatal.
# F8 -- PLATFORM-EXCLUSIVE packages. A package whose Go source only type-checks on some platforms
# carries [GoPlatformExclusive("<goos>", ...)] in its package_info.cs (hand-added, preserved across
# a re-transpile -- measured). On a NON-native host the converter cannot type-check it, so this gate
# reported it NOT MEASURED: correct, but indistinguishable BY NAME from a real conversion failure,
# which is how a genuine regression could hide among expected lines. The class bites both ways --
# ScmRightsSeam (unix-only syscall API) on Windows is the exact mirror of FindFirstFileData on Linux.
#
# Such a package is SKIPPED BY NAME on a non-native host: printed with its platform, counted in its
# own summary line, and excluded from the transpile loop and the byte-identical denominator -- never
# silently dropped from the enumeration, which would trade one invisible problem for another.
function Get-PlatformExclusivePlatforms {
    param([string] $PackageDir)

    $infoFile = Join-Path $PackageDir 'package_info.cs'

    if (-not (Test-Path $infoFile)) { return @() }

    # Line-anchored, like the corpus's own GoManualConversion census: the attribute NAME can appear
    # in a comment, and an unanchored match would gate a package on prose.
    $line = @(Get-Content -LiteralPath $infoFile | Where-Object { $_ -match '^\s*\[GoPlatformExclusive\(' }) |
        Select-Object -First 1

    if (-not $line) { return @() }

    return @([regex]::Matches($line, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
}

$hostGoos = if ($env:GoTargetOS) { $env:GoTargetOS } elseif ($IsWindowsHost) { 'windows' } elseif ($IsMacOS) { 'darwin' } else { 'linux' }
$skippedExclusive = @()
$measurable = @()

foreach ($proj in $projects) {
    $platforms = Get-PlatformExclusivePlatforms $proj.FullName

    if ($platforms.Count -gt 0 -and $platforms -notcontains $hostGoos) {
        $skippedExclusive += [pscustomobject]@{
            Name      = Get-RelativeDisplayPath $proj.FullName $behavioral
            Platforms = ($platforms -join ', ')
        }
    }
    else {
        $measurable += $proj
    }
}

if ($skippedExclusive.Count -gt 0) {
    Write-Host "==> SKIPPED (platform-exclusive, $($skippedExclusive.Count)): native to another platform, so this $hostGoos host cannot type-check them:" -ForegroundColor DarkCyan
    $skippedExclusive | ForEach-Object { Write-Host "    $($_.Name) [$($_.Platforms)]" -ForegroundColor DarkCyan }
}

$savedEAP = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$unmeasured = @()
$advisoryWarnings = 0
try {
    # -AliasDriftCheckOnly skips the whole transpile: the classification below reads the
    # working tree a REAL run has already dirtied, which is what makes controlling it cheap.
    if ($AliasDriftCheckOnly) { $measurable = @() }
    foreach ($proj in $measurable) {
        $lines = @(& $go2csExe -go2cspath $go2csRoot $proj.FullName 2>&1 | ForEach-Object { "$_" })
        # Report the path relative to the behavioral root: a bare .Name is ambiguous for nested
        # sub-libraries (three of them are called "inner", two "latelib").
        $rel = Get-RelativeDisplayPath $proj.FullName $behavioral
        $vacuous = @($lines | Where-Object { $_ -match 'did not fully type-check|visit file error' })
        if ($LASTEXITCODE -ne 0) {
            Write-Host "    [transpile FAILED] $rel" -ForegroundColor Red
            $unmeasured += $rel
        }
        elseif ($vacuous.Count -gt 0) {
            Write-Host "    [NOT MEASURED] $rel -- output not fully regenerated:" -ForegroundColor Red
            $vacuous | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkYellow }
            $unmeasured += $rel
        }
        $advisoryWarnings += @($lines | Where-Object { $_ -match 'WARNING' -and $_ -notmatch 'did not fully type-check|visit file error' }).Count
    }
}
finally { $ErrorActionPreference = $savedEAP }

# 3. Report any changed converter output under the behavioral tree: the generated .cs AND the
#    generated .csproj (the transpile rewrites both; the pathspec was .cs-only until 2026-08-08, so a
#    converter change to csproj emission -- e.g. a dropped <ProjectReference> block -- was invisible
#    to this gate on EVERY platform, not just the Linux host where it was found). Both C# tooling
#    dirs are excluded: their sources are HAND-WRITTEN, not converter output, so an edit to either is
#    a deliberate harness change and reporting it as converter drift is pure noise. (BehavioralRunner
#    was missing from this filter until 2026-08-02, so editing the runner made CNR accuse itself of a
#    regression.)
$changed = & git -C $repoRoot status --short -- "src/tests/Behavioral/*.cs" "src/tests/Behavioral/*.csproj" |
    Where-Object { $_ -notmatch "Behavioral(Tests|Runner)/" }

# ---------------------------------------------------------------------------------------------
# PLATFORM-ALIAS DRIFT -- the standing check (coordinator ruling 2026-09-02, option 1: named, not
# merely tolerated).
#
# A behavioral package whose committed emission was captured on Windows carries the Delta-prefixed
# syscall aliases (syscallHandle -> Delta-Handle, syscallSockaddr -> Delta-Sockaddr) that ONLY the
# Windows syscall flavor mints -- syscall/windows/package_info.cs declares them and the linux
# flavor has none. A Linux CNR therefore re-emits those files without the prefix and reports them
# CHANGED forever. For a package that runs meaningfully on ONE platform the remedy is F8's marker
# (SendtoSeam, SockaddrRoundTrip). For a package that must RUN on BOTH -- EnvironBlockWalk guards
# syscall.Environ, hand-owned on Windows and auto on Linux -- the marker would throw away a real
# run to silence a golden mismatch, and per-GOOS behavioral goldens are a cost no single row
# justifies. So the drift is ACCEPTED, and this check is what stops 'accepted' from decaying into
# 'unnoticed': the alias-drift set must be a SUBSET of the documented members below. A new member
# is loud, by name, the day it appears.
#
# The members surface one at a time as packages are added and only ever on a Linux run, which is
# exactly why a list nobody checks would go stale silently.
$documentedAliasDriftPackages = @(
    'EnvironBlockWalk'   # golden captured on Windows; runs on both platforms, so no F8 marker
) | Where-Object { $OmitAliasDriftMembers -notcontains $_ }

function Test-IsPlatformAliasDrift {
    <#
    .SYNOPSIS
        True when a changed file's ENTIRE diff is the Windows-vs-Linux syscall alias difference.
    .DESCRIPTION
        The drift IS the prefix, so the predicate is: delete the whole-line alias `global using`
        declarations (they exist on one side only), strip the prefix character from what remains,
        and require the two sides to be identical. Any other hunk -- a real regression in the same
        file -- leaves a difference and the file is NOT classified, which is the property that
        makes accepting the drift safe.
    #>
    param([string] $RelPath)

    # Built from CODEPOINTS, never from literals. A BOM-less .ps1 is parsed by Windows PowerShell
    # 5.1 under the system codepage, so an embedded non-ASCII glyph mojibakes at PARSE time and a
    # regex that can never match reports a confident zero (measured 2026-08-30 on another
    # instrument). This file is BOM-less and carries non-ASCII only in comments, where it is
    # harmless; keeping the load-bearing characters as codepoints means that stays true.
    # [string], not [char]: String.Replace picks its (char, char) overload for a [char] first
    # argument and then cannot bind '' to the second, so a [char] here throws at the first line
    # that actually needs stripping. Measured: the two-arm control passed WITHOUT reaching this
    # code at all, because every line in those diffs was a whole alias-using line and was
    # `continue`d above -- it took the negative control (a real hunk alongside the drift) to
    # execute the line and expose it. A control only tests the axis it varies.
    $delta = [string][char]0x0394    # the alias prefix the Windows syscall flavor mints
    $dot   = [string][char]0xA4F8    # the converter's qualified-name separator (regex only, but kept symmetric)
    $aliasUsingPattern = "^[-+]\s*global using syscall$dot\w+ = go\.syscall_package\.$delta\w+;\s*$"

    $diff = & git -C $repoRoot diff --unified=0 -- $RelPath
    if (-not $diff) { return $false }   # no textual diff at all is not this class

    $minus = @()
    $plus  = @()
    foreach ($line in $diff) {
        if ($line -match '^(diff |index |--- |\+\+\+ |@@|new file|deleted file|similarity|rename )') { continue }
        if ($line -match $aliasUsingPattern) { continue }
        if ($line.StartsWith('-'))     { $minus += $line.Substring(1).Replace($delta, '') }
        elseif ($line.StartsWith('+')) { $plus  += $line.Substring(1).Replace($delta, '') }
    }

    if ($minus.Count -ne $plus.Count) { return $false }
    for ($i = 0; $i -lt $minus.Count; $i++) {
        if ($minus[$i] -ne $plus[$i]) { return $false }
    }
    return $true
}

$aliasDriftDocumented   = @()
$aliasDriftUndocumented = @()
if ($hostGoos -ne 'windows' -and $changed) {
    foreach ($entry in $changed) {
        $rel = ($entry -replace '^\s*\S+\s+', '')
        if (-not (Test-IsPlatformAliasDrift $rel)) { continue }
        $pkg = Split-Path (Split-Path $rel -Parent) -Leaf
        if ($documentedAliasDriftPackages -contains $pkg) { $aliasDriftDocumented += $rel }
        else { $aliasDriftUndocumented += "$rel  [package: $pkg]" }
    }
}

if ($aliasDriftDocumented.Count -gt 0) {
    Write-Host "==> PLATFORM-ALIAS DRIFT (documented, $($aliasDriftDocumented.Count)): the whole diff is the Windows-vs-Linux syscall alias, accepted per the 2026-09-02 ruling:" -ForegroundColor DarkCyan
    $aliasDriftDocumented | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkCyan }
}

if ($aliasDriftUndocumented.Count -gt 0) {
    Write-Host "==> UNDOCUMENTED PLATFORM-ALIAS DRIFT ($($aliasDriftUndocumented.Count)): a NEW member of a class that is accepted only while its membership is known." -ForegroundColor Red
    $aliasDriftUndocumented | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    Write-Host "    Either mark the package [GoPlatformExclusive] if it runs on one platform only, or add it to" -ForegroundColor Red
    Write-Host "    \$documentedAliasDriftPackages in this script with a note saying which platform captured its golden." -ForegroundColor Red
}

if ($AliasDriftCheckOnly) {
    if ($aliasDriftUndocumented.Count -gt 0) {
        Write-Host "==> ALIAS-DRIFT CHECK: FAILED -- $($aliasDriftUndocumented.Count) undocumented member(s)." -ForegroundColor Red
        exit 1
    }
    Write-Host "==> ALIAS-DRIFT CHECK: OK -- $($aliasDriftDocumented.Count) documented, 0 undocumented (of $(@($changed).Count) changed file(s) inspected)." -ForegroundColor Green
    exit 0
}

if (-not $changed -and $unmeasured.Count -eq 0) {
    # N is ENUMERATED-MINUS-SKIPPED: a platform-exclusive package this host cannot type-check was
    # never transpiled, so counting it toward "byte-identical across all N" is the vacuous claim F8
    # exists to stop. The skipped names are restated here so the verdict line and the skip line
    # cannot drift apart in a log someone reads later.
    $skipNote = if ($skippedExclusive.Count -gt 0) { " ($($skippedExclusive.Count) platform-exclusive skipped: $(($skippedExclusive | ForEach-Object { $_.Name }) -join ", "))" } else { "" }
    Write-Host "==> NO REGRESSION: generated C# and .csproj are byte-identical across all $($measurable.Count) behavioral packages ($advisoryWarnings advisory converter warnings)$skipNote." -ForegroundColor Green
    exit 0
}

if ($changed) {
    Write-Host "==> CHANGED converter output (inspect: intended new golden vs. regression):" -ForegroundColor Yellow
    $changed | ForEach-Object { Write-Host "    $_" }
}

if ($unmeasured.Count -gt 0) {
    Write-Host "==> NOT MEASURED ($($unmeasured.Count)): output was not fully regenerated for these packages, so the byte-identical check is vacuous for them:" -ForegroundColor Red
    $unmeasured | ForEach-Object { Write-Host "    $_" }
}

if ($Revert -and $changed) {
    # Same two exclusions as the report above, and for a sharper reason: without them this checkout
    # DESTROYS uncommitted hand-edits to the harness sources themselves (they are .cs/.csproj under
    # tests\Behavioral, so the bare pathspec swept them up).
    Write-Host "==> -Revert: restoring changed .cs/.csproj to HEAD" -ForegroundColor Cyan
    & git -C $repoRoot checkout -- "src/tests/Behavioral/*.cs" "src/tests/Behavioral/*.csproj" `
        ":(exclude)src/tests/Behavioral/BehavioralTests/*" `
        ":(exclude)src/tests/Behavioral/BehavioralRunner/*"
}

exit 1
