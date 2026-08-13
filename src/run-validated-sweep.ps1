# run-validated-sweep.ps1 - re-validate every banked Phase-4 package against `go test`.
#
# The charter's operational gate: after any change to golib, go2cs-gen, the converter or the
# corpus, every already-validated package must still validate -- verdict for verdict, at its
# exact banked count. This script is that gate. (One narrow, named exception: a row that declares
# HOST-CONDITIONAL verdicts may exceed its banked floor by exactly those named rows -- see
# Test-HostConditionalDelta below. Any other count movement is still a failure.)
#
# The roster is READ FROM docs/ValidatedTestPackages.md rather than hardcoded, so it can never
# drift from the table a banking commit just updated: the table is the single source of truth for
# which packages are banked and what counts they carry.
#
#   ./run-validated-sweep.ps1                       # every banked package
#   ./run-validated-sweep.ps1 -Filter compress      # just the ones whose path contains "compress"
#   ./run-validated-sweep.ps1 -TestTimeout 15m      # slower machine / contended box (a value LARGER
#                                                   #   than a $longTimeouts floor raises that too)
#   ./run-validated-sweep.ps1 -SkipBuild            # reuse the current go2cs.exe as-is
#   ./run-validated-sweep.ps1 -IgnoreDiskPreflight  # proceed on a nearly-full drive anyway
[CmdletBinding()]
param(
    [string] $Filter,
    # WELL above the pipeline's 2-minute default ON PURPOSE. regexp and strings legitimately run
    # for minutes (regexp exercises the whole RE2 corpus; strings' TestCompareStrings alone is
    # ~110 s), and at the default they report as failures when they are merely slow -- a false
    # red that costs an investigation every time someone hits it.
    [string] $TestTimeout = '10m',
    [switch] $SkipBuild,
    [switch] $IgnoreDiskPreflight
)

$ErrorActionPreference = 'Stop'

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

# Roots, the converter path and the executable suffix come from one shared definition (src\_paths.ps1)
# so this gate cannot disagree with the behavioral instruments about where anything is -- and so it
# carries no backslash literal, which off Windows fails SILENTLY rather than loudly (F4,
# docs/PLAN-linux-operation.md). Every path below is joined with a single forward-slash child
# argument: PowerShell 5.1 and 7+ both normalize that to the host separator, so the strings handed to
# the converter are byte-identical to the ones the backslash literals produced on Windows.
. (Join-Path $PSScriptRoot '_paths.ps1')

$src = $SrcRoot
$repo = $RepoRoot
$table = Join-Path $repo 'docs/ValidatedTestPackages.md'
$exe = $Go2csExe
$goroot = (& go env GOROOT).Trim()

if (-not (Test-Path $table)) { throw "Cannot find the validated-package table at $table" }
if (-not $goroot) { throw 'Could not resolve GOROOT -- is the Go toolchain on PATH?' }

# ---- toolchain pin ------------------------------------------------------------------------------
# The sweep re-runs each package's Go tests from GOROOT's SOURCES and compares the verdicts against
# counts banked from one specific Go release, so the toolchain silently decides what is being
# measured. On the wrong release this compares that toolchain's tests against the pinned release's
# banked numbers, and nothing reports it, because each side stays internally consistent -- which is
# how a lane's gates once passed at banked counts on a toolchain the corpus was never pinned to.
# In the sweep's own terms such a run is NOT MEASURED, never a verdict, so it stops here rather than
# printing counts nobody can bank. Same comparison and same failure shape as the converter's own
# -stdlib/-tests guard (checkCorpusToolchainPin, src/go2cs/toolchainResolution.go).

# GOROOT's own VERSION file is preferred over `go env GOVERSION`, because the two can disagree and
# when they do it is the reported version that misdescribes what is being read: a GOROOT environment
# variable overrides the selected toolchain's own root, so a 1.23.1 go binary can report go1.23.1
# while GOROOT holds a 1.23.2 tree. The SOURCES are what the banked counts came from, so they win.
# Same precedence as the converter's convertingRelease.
$goversion = (& go env GOVERSION).Trim()
$gorootVersionFile = Join-Path $goroot 'VERSION'

if (Test-Path $gorootVersionFile) {
    # Go 1.21 added a `time <stamp>` line beneath the release; the release is the first line.
    $firstLine = Get-Content $gorootVersionFile -TotalCount 1

    if ($firstLine) { $goversion = $firstLine.Trim() }
}

$versionProps = Join-Path $src 'version.props'
$pinnedRelease = ''

if (Test-Path $versionProps) {
    $propsText = [System.IO.File]::ReadAllText($versionProps)

    if ($propsText -match '<GoStdLibVersion>([^<]+)</GoStdLibVersion>') {
        $pinnedRelease = $Matches[1].Trim()
    }
}

# An unreadable version on EITHER side is not a mismatch -- there is nothing to assert against, and
# refusing there would stop legitimate runs. Only a known disagreement fails.
if ($pinnedRelease -and $goversion) {
    $runningRelease = $goversion -replace '^go', ''

    if ($runningRelease -ne $pinnedRelease) {
        throw ("Toolchain pin: version.props pins the corpus to Go $pinnedRelease " +
            "(<GoStdLibVersion>$pinnedRelease</GoStdLibVersion>), but the Go tree this run would " +
            "read is $goversion, at GOROOT $goroot. The sweep re-runs each package's Go tests from " +
            "those sources, so it would measure go$runningRelease's tests against counts banked " +
            "from $pinnedRelease -- NOT MEASURED, never a verdict. Either run on go$pinnedRelease " +
            "with GOROOT pointing at that same tree, or, if the corpus is deliberately moving to " +
            "$runningRelease, bump <GoStdLibVersion> in version.props first. (If GOVERSION already " +
            "says go$pinnedRelease, check for a GOROOT environment variable overriding the selected " +
            "toolchain -- that mismatch is what this reads.)")
    }
}

# ---- roster: parse the table's rows -------------------------------------------------------------
# Row shape:  | [`net/http/internal/ascii`](https://...) | 13 | 1 | What it exercises. |
# Column 2 is the matching-verdict count, column 3 the disclosed count (blank when none).
# A row may additionally declare HOST-CONDITIONAL verdicts inside its What-it-exercises cell:
#
#   host-conditional (<why, colon-free>): `Name`, `Name`, ...
#
# naming verdict rows that exist only on a host with some capability (path/filepath's six
# TestWalkSymlinkRoot subtests exist only where symlink creation is permitted -- the parent test
# skips before spawning them otherwise). Column 2 stays the FLOOR every host produces; the names
# bound what a more-capable host may legitimately add. The phrase "host-conditional" is reserved
# for this annotation. Acceptance rules live on Test-HostConditionalDelta below.
$rows = foreach ($line in Get-Content $table) {
    if ($line -match '^\|\s*\[`([^`]+)`\]\([^)]*\)\s*\|\s*(\d+)\s*\|\s*(\d*)\s*\|') {
        # Row fields FIRST: the annotation -match below overwrites $Matches.
        $rowPackage = $Matches[1]
        $rowExpected = [int]$Matches[2]
        $rowDisclosed = if ($Matches[3]) { [int]$Matches[3] } else { 0 }

        # The capture takes the run of backticked, comma-separated names right after the colon and
        # stops at the first text that is not one -- the cell's next " * [proof]" segment ends it.
        $rowConditional = @()
        if ($line -match 'host-conditional\s*(?:\([^)]*\))?\s*:\s*((?:`[^`]+`\s*,\s*)*`[^`]+`)') {
            $rowConditional = @([regex]::Matches($Matches[1], '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
        }

        [PSCustomObject]@{
            Package     = $rowPackage
            Expected    = $rowExpected
            Disclosed   = $rowDisclosed
            Conditional = $rowConditional
        }
    }
}

if ($Filter) { $rows = $rows | Where-Object { $_.Package -like "*$Filter*" } }
# @() so a single match stays an array -- PowerShell unwraps a one-element pipeline to a scalar,
# which has no .Count and would print a blank package count.
$rows = @($rows)
if (-not $rows) { throw "No banked packages matched$(if ($Filter) { " filter '$Filter'" })." }

$expectedTotal = ($rows | Measure-Object -Property Expected -Sum).Sum
Write-Host "validated sweep: $($rows.Count) package(s), $expectedTotal expected verdicts, timeout $TestTimeout" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host '==> building the converter' -ForegroundColor Cyan
    Push-Location $ConverterSrc
    try {
        # Absolute output path rather than the relative 'bin\go2cs.exe' this replaced: same file on
        # Windows, and it carries the host's own executable suffix instead of a hard-coded one.
        & go build -o $exe .
        if ($LASTEXITCODE -ne 0) { throw 'converter build failed' }
    }
    finally { Pop-Location }
}
if (-not (Test-Path $exe)) { throw "Converter not built: $exe" }

# ---- sweep --------------------------------------------------------------------------------------
# SERIAL by design: concurrent -tests runs share freshly-built dependency DLLs and collide on them
# (CS2012, "the process cannot access the file"), which reads as a package failure but is not one.
$env:MSBUILDDISABLENODEREUSE = '1'
$pass = 0; $fail = 0; $failed = @(); $started = Get-Date

# 'Continue' for the sweep itself: merging a native command's stderr with 2>&1 wraps each line in
# an ErrorRecord in PS 5.1, so under 'Stop' one benign converter warning (e.g. the unsafe.Sizeof
# notice from crypto/subtle) aborts the whole run. The verdict is judged from the output below,
# not from $?, so a non-terminating preference is the correct setting here.
$ErrorActionPreference = 'Continue'

# Go duration text -> TimeSpan, so two -test-timeout values can be COMPARED rather than merely
# swapped for one another. Returns $null for anything it cannot read -- the empty string, a bare
# number with no unit, an unknown unit -- all of which the converter would reject too; every caller
# treats $null as "the comparison cannot be made, keep the safer value".
function ConvertTo-GoDuration([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $null }

    $text = $value.Trim()
    $sign = 1
    if ($text.StartsWith('-')) { $sign = -1; $text = $text.Substring(1) }
    elseif ($text.StartsWith('+')) { $text = $text.Substring(1) }
    if ($text -eq '0') { return [TimeSpan]::Zero }

    # TICKS per unit, not milliseconds: [TimeSpan]::FromMilliseconds rounds to the nearest whole
    # millisecond on .NET Framework (PS 5.1), which silently flattens every sub-second value to zero.
    # Those units are here for completeness -- Go accepts them, so a value naming one must not be
    # misread as unparseable and demoted to the floor. The two micro signs Go accepts are spelled by
    # code point so this file needs no non-ASCII literal.
    $perUnit = @{
        'ns' = 0.01; 'us' = 10.0; "$([char]0xB5)s" = 10.0; "$([char]0x3BC)s" = 10.0
        'ms' = 10000.0; 's' = 1e7; 'm' = 6e8; 'h' = 3.6e10
    }

    $ticks = 0.0
    $consumed = 0

    foreach ($part in [regex]::Matches($text, '(\d+(?:\.\d*)?|\.\d+)([^\d.]+)')) {
        # Parts must tile the string end to end: a gap means it holds something that is not a
        # <number><unit> pair, which is a value this script must not pretend to understand.
        if ($part.Index -ne $consumed) { return $null }

        $unit = $part.Groups[2].Value
        if (-not $perUnit.ContainsKey($unit)) { return $null }

        # InvariantCulture explicitly: a fractional duration ('1.5h') must not read as 15 under a
        # comma-decimal culture.
        $ticks += [double]::Parse($part.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture) * $perUnit[$unit]
        $consumed = $part.Index + $part.Length
    }

    # Also catches "no parts matched at all", since $text is non-empty by here.
    if ($consumed -ne $text.Length) { return $null }
    # A duration too large for TimeSpan is unreadable rather than fatal -- Go rejects one too, and
    # $null routes it to the caller's safe branch instead of throwing mid-sweep.
    if ($ticks -gt [double][long]::MaxValue) { return $null }

    return [TimeSpan]::FromTicks([long][math]::Round($sign * $ticks))
}

# ---- host-conditional verdicts ------------------------------------------------------------------
# A banked count is normally exact, but a verdict COUNT can be legitimately host-dependent:
# path/filepath banks 61 on a host without symlink-creation privilege, where Go itself skips
# TestWalkSymlinkRoot -- and on a host WITH the privilege the test runs and spawns its six
# table-driven subtests, six verdict rows that simply do not exist on the banking host (the
# skip->pass flips of the other privilege-gated tests are count-NEUTRAL; only rows that appear or
# vanish move the count). Banking the larger number would false-red every unprivileged host, the
# larger population -- so the roster banks the FLOOR and names the conditional verdicts, and the
# sweep accepts floor+k ONLY when the k extra rows are exactly k of the named tests, agreeing on
# both runtimes, with no banked verdict missing. Anything outside the named set still fails
# loudly, exactly as before.
#
# The check needs the banked verdict NAME SET, not just the count -- count arithmetic alone would
# wave through a lost banked row canceling against a rogue new one. That set is the package's
# committed proof page (docs/validation/current/<pkg-dots>.md), read from HEAD deliberately: the
# sweep run that just validated at the larger count has already REWRITTEN the working-tree page to
# match itself, so only the committed copy still records what was banked.
function Test-HostConditionalDelta {
    param(
        [int] $Expected,           # banked matching-verdict count (roster column 2, the floor)
        [int] $Disclosed,          # banked disclosed count (roster column 3)
        [string[]] $Conditional,   # the named host-conditional verdicts (roster annotation)
        [int] $Got,                # the live run's validated count
        $Comparison,               # the run's go2cs_test_comparison.json, ConvertFrom-Json form
        [string[]] $BankedNames    # verdict names from the committed proof page's Verdicts table
    )

    # Local to this function -- nested definitions do not leak into the script scope.
    function New-HostConditionalVerdictResult([bool] $accepted, [string[]] $extras, [string] $reason) {
        return [PSCustomObject]@{ Accepted = $accepted; Extras = $extras; Reason = $reason }
    }

    $k = $Got - $Expected
    if ($k -lt 1) {
        return New-HostConditionalVerdictResult $false @() "count $Got is below the banked floor $Expected -- a lost verdict is never host-conditional"
    }
    if ($k -gt $Conditional.Count) {
        return New-HostConditionalVerdictResult $false @() "count $Got exceeds the floor by $k, more than the $($Conditional.Count) named host-conditional verdicts"
    }
    if ($null -eq $Comparison -or $null -eq $Comparison.go -or $null -eq $Comparison.csharp) {
        return New-HostConditionalVerdictResult $false @() 'comparison record carries no per-test verdict maps'
    }

    # A moved disclosed count shifts the same arithmetic this check relies on, and a disclosure
    # appearing or retiring is roster maintenance, never host capability.
    $liveDisclosed = if ($null -eq $Comparison.disclosed) { 0 } else { @($Comparison.disclosed).Count }
    if ($liveDisclosed -ne $Disclosed) {
        return New-HostConditionalVerdictResult $false @() "disclosed count moved ($liveDisclosed live vs $Disclosed banked) -- not a host-conditional shape"
    }

    # The proof page lists every compared verdict: the matched rows plus the disclosed-divergent
    # ones. If page and roster disagree the banked evidence is inconsistent -- absorb nothing.
    if ($BankedNames.Count -ne ($Expected + $Disclosed)) {
        return New-HostConditionalVerdictResult $false @() "committed proof page lists $($BankedNames.Count) verdicts where the roster banks $Expected matched + $Disclosed disclosed -- page and table disagree"
    }

    $goProperties = $Comparison.go.PSObject.Properties
    $csProperties = $Comparison.csharp.PSObject.Properties
    $liveNames = @($goProperties | ForEach-Object { $_.Name })

    $missing = @($BankedNames | Where-Object { $liveNames -notcontains $_ })
    if ($missing.Count -gt 0) {
        return New-HostConditionalVerdictResult $false @() "banked verdicts missing from this run: $($missing -join ', ')"
    }

    $extras = @($liveNames | Where-Object { $BankedNames -notcontains $_ })
    $outside = @($extras | Where-Object { $Conditional -notcontains $_ })
    if ($outside.Count -gt 0) {
        return New-HostConditionalVerdictResult $false @() "extra verdicts outside the named host-conditional set: $($outside -join ', ')"
    }

    # With nothing missing this equals $k by construction; a mismatch means the count and the
    # verdict maps have come apart (a converter-side accounting change) -- refuse to absorb.
    if ($extras.Count -ne $k) {
        return New-HostConditionalVerdictResult $false @() "the $($extras.Count) extra verdict rows do not account for the count delta of $k"
    }

    foreach ($name in $extras) {
        $goVerdict = $goProperties[$name].Value
        $csProperty = $csProperties[$name]
        if ($null -eq $csProperty -or $csProperty.Value -ne $goVerdict) {
            $csVerdict = if ($null -eq $csProperty) { 'absent' } else { $csProperty.Value }
            return New-HostConditionalVerdictResult $false @() "conditional verdict ${name}: go '$goVerdict' vs C# '$csVerdict' -- the sides do not agree"
        }
    }

    return New-HostConditionalVerdictResult $true $extras $null
}

# Reads the two evidence artifacts the delta check needs -- the run's own comparison record and
# the committed proof page -- and applies Test-HostConditionalDelta. Every unreadable input is a
# rejection with its reason, never an acceptance: the mechanism only absorbs what it can prove.
function Get-HostConditionalVerdict {
    param([PSCustomObject] $Row, [int] $Got, [string] $OutDir)

    $comparisonPath = Join-Path $OutDir 'go2cs_test_comparison.json'
    if (-not (Test-Path $comparisonPath)) {
        return [PSCustomObject]@{ Accepted = $false; Extras = @(); Reason = "no comparison record at $comparisonPath" }
    }

    $comparison = $null
    try { $comparison = [System.IO.File]::ReadAllText($comparisonPath) | ConvertFrom-Json } catch {}
    if ($null -eq $comparison) {
        return [PSCustomObject]@{ Accepted = $false; Extras = @(); Reason = "unreadable comparison record at $comparisonPath" }
    }

    # 2>$null is safe here: the sweep loop runs under 'Continue', so a missing page's stderr lines
    # become discarded ErrorRecords rather than a terminating abort (the 'Stop' hazard the drift
    # section documents), and the rejection below already names the path loudly.
    $pageRel = 'docs/validation/current/' + ($Row.Package -replace '/', '.') + '.md'
    $pageLines = & git -C $repo show "HEAD:$pageRel" 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $pageLines) {
        return [PSCustomObject]@{ Accepted = $false; Extras = @(); Reason = "no committed proof page at HEAD:$pageRel" }
    }

    # Verdict names come from the page's "## Verdicts" table alone -- a disclosed package repeats
    # its divergent tests' names in a later Disclosed-divergences table, which must not be counted.
    $bankedNames = New-Object System.Collections.Generic.List[string]
    $inVerdicts = $false
    foreach ($pageLine in @($pageLines)) {
        if ($pageLine -match '^##\s') { $inVerdicts = [bool]($pageLine -match '^##\s+Verdicts\b'); continue }
        if ($inVerdicts -and $pageLine -match '^\|\s*`([^`]+)`\s*\|') { [void]$bankedNames.Add($Matches[1]) }
    }

    return Test-HostConditionalDelta -Expected $Row.Expected -Disclosed $Row.Disclosed -Conditional $Row.Conditional `
        -Got $Got -Comparison $comparison -BankedNames $bankedNames.ToArray()
}

# Packages whose C# suite legitimately exceeds the default package deadline. hash/maphash's
# SMHasher matrix runs ~15 minutes in C# (7.6 s in Go — a performance gap, not a correctness one)
# and was BANKED under 30m; at the default it reports a timeout with every test up to the cut
# PASSING, which reads as a failure and costs an investigation every time. index/suffixarray is
# the same shape and worse: `TestNew{32,64}/exhaustive3` brute-forces every string up to length 8
# over a 3-letter alphabet, 12.4 s in Go and ~35 min in C#; under 10m it reports exactly the
# TestNew64/exhaustive3 tail as empty verdicts.
# crypto/dsa is the third member and the one that cost the most to misread: TestParameterGeneration
# is a probabilistic prime search over the converted math/big and takes 1,156.8 s (19.3 min) in C#
# against seconds in Go. The board recorded it as "no -test-timeout is enough" after measuring at
# 6m and again at 20m -- but 20m is just UNDER what the package needs end to end, so the deadline
# cut a run that was converging. It validates 4/4 at 30m.
#
# archive/zip is the mildest of the four and the one whose number MOVED. TestZip64LargeDirectory
# builds a 4 GiB central directory out of ~128 KB records; before r57c it never completed at all
# (>45 m) because @string's range indexer copied, making the rune walk over each 65,535-byte name
# quadratic. With @string carrying a real window the whole suite runs 20 s in Release against Go's
# 11.3 s -- honest. The deadline is here for the harness's DEBUG build, which the pipeline uses and
# which pays ~22x for non-inlined golib accessors: 391 s measured solo on the reference desktop,
# 774 s on an i7-5820K -- which leaves r57c's original 20m only ~35% headroom on slow hardware, so
# the entry is 30m: a deadline is a safety net against a hung run, never a performance assumption.
#
# PRECEDENCE -- the table is a FLOOR, not an override (2026-08-10). The effective budget is the
# LONGER of the entry and an explicitly-passed -TestTimeout, so a larger -TestTimeout raises these
# four like it raises everything else. It used to win unconditionally, which meant -TestTimeout was
# silently ignored for exactly the packages that need a long budget: an i7-5820K desktop reported
# hash/maphash and crypto/dsa as FAIL "package timeout after 00:30:00", and `-TestTimeout 60m` died
# at 30:00 again -- while driving the same package's pipeline by hand at 60m validated its banked
# 22/22. A SMALLER -TestTimeout still loses to the table: under-budgeting these four is the exact
# false red the table exists to prevent, and the usage line above advertises the flag for slow boxes.
# The values are SLOW-HOST-CALIBRATED per the safety-net doctrine (a deadline is a net against a
# hung run, never a performance assumption): a floor sized to the fastest box false-reds every bare
# sweep on a slower one, while a raised floor costs a fast box only how long a RARE genuine hang
# takes to be declared. Calibration evidence, i7-5820K 2026-08-10: maphash validated 22/22 in
# **2,406 s (40.1 min)** -- past the old i9-sized 30m floor, hence 60m; crypto/dsa's banked
# TestParameterGeneration was 1,156.8 s (19.3 min) on the i9, extrapolating to ~40-60 min here,
# hence 60m; index/suffixarray's ~35 min on the i9 extrapolates to 70-105 min, hence 120m;
# archive/zip measured 774 s here, so its 30m stands. -TestTimeout raises any of these further on
# a still-slower box; re-measure and move a floor when a slower legitimate host proves one short.
#
# crypto/dsa's floor is sized to its VARIANCE, not its typical run: TestParameterGeneration is a
# randomized prime search, and two same-machine samples an hour apart measured 1,496 s (25 min)
# and >3,600 s (blew the then-60m floor mid-run) -- the heavy tail, not load, is the variable, so
# the deadline must clear the tail. 120m holds both observed extremes with 2x headroom on the
# fast sample (2026-08-11, i7-5820K).
$longTimeouts = @{ 'hash/maphash' = '60m'; 'index/suffixarray' = '120m'; 'crypto/dsa' = '120m'; 'archive/zip' = '30m' }

foreach ($row in $rows) {
    $pkg = $row.Package
    # The import path is already forward-slash-separated, which is exactly the form Join-Path
    # normalizes for us -- so the -replace that hand-built a Windows path is not just unnecessary,
    # it was the thing that made this mapping wrong off Windows.
    $outDir = Join-Path $src "core/$pkg"
    $goDir = Join-Path $goroot "src/$pkg"
    $label = '{0,-34}' -f $pkg
    # The longer of the two, passed VERBATIM -- whichever string wins reaches the converter exactly
    # as it was written, never re-formatted through the TimeSpan the comparison went by.
    $pkgTimeout = $TestTimeout

    if ($longTimeouts.ContainsKey($pkg)) {
        $floor = ConvertTo-GoDuration $longTimeouts[$pkg]
        $asked = ConvertTo-GoDuration $TestTimeout
        $raisesTheFloor = ($null -ne $asked) -and ($null -ne $floor) -and ($asked -gt $floor)
        if (-not $raisesTheFloor) { $pkgTimeout = $longTimeouts[$pkg] }
    }

    # -go2cspath is pinned to $src (this script's own directory) rather than inherited from the
    # ambient GO2CSPATH. A -tests run already self-locates the root from its output path, which lands
    # under src\core, so on a healthy box this is the same value -- but only when the ambient root is
    # INVALID does that recovery run: a GO2CSPATH pointing at some other real go2cs tree (a
    # deploy-core staging root, say) would be honored instead, and the suite would be built against
    # one tree's metadata while compiling the other's sources.
    $out = & $exe -tests -test-action all -test-timeout $pkgTimeout -go2cspath $src $goDir $outDir 2>&1
    $verdict = ($out | Select-String 'Validated (\d+) tests against go test' | Select-Object -First 1)

    if ($verdict) {
        $got = [int]$verdict.Matches[0].Groups[1].Value
        if ($got -eq $row.Expected) {
            $pass++
            Write-Host "  PASS  $label $got" -ForegroundColor Green
        }
        else {
            # Validated, but NOT at its banked count -- normally a silent change in what the suite
            # asserts, and a failure: the table and reality must agree, one of them is now wrong.
            # A row that declares host-conditional verdicts gets one chance to PROVE the surplus is
            # exactly those named rows materializing on a more-capable host; anything unprovable
            # falls through to the same failure as before, with the rejection reason attached.
            $hostConditional = $null
            if ($row.Conditional.Count -gt 0) {
                $hostConditional = Get-HostConditionalVerdict -Row $row -Got $got -OutDir $outDir
            }

            if ($null -ne $hostConditional -and $hostConditional.Accepted) {
                $pass++
                Write-Host "  PASS  $label $got = $($row.Expected) banked + $($hostConditional.Extras.Count) host-conditional" -ForegroundColor Green
            }
            else {
                $fail++; $failed += "$pkg (count $got, banked $($row.Expected))"
                Write-Host "  COUNT $label $got, banked $($row.Expected)" -ForegroundColor Yellow
                if ($null -ne $hostConditional -and $hostConditional.Reason) {
                    Write-Host "        host-conditional check: $($hostConditional.Reason)" -ForegroundColor Yellow
                }
            }
        }
    }
    else {
        $fail++; $failed += $pkg
        Write-Host "  FAIL  $label" -ForegroundColor Red
        $out | Select-Object -Last 3 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkGray }
    }
}

$elapsed = [int]((Get-Date) - $started).TotalSeconds
Write-Host ''
Write-Host "sweep: $pass pass / $fail fail  (${elapsed}s)" -ForegroundColor $(if ($fail) { 'Red' } else { 'Green' })

# The pipeline regenerates each package's committed test artifacts in place. Content drift is a
# real signal; CRLF-only churn is not (autocrlf smudges LF fixtures on checkout) -- so report by
# CONTENT, using the same --ignore-cr-at-eol discriminator the rebank doctrine uses.
# Do NOT redirect git's stderr: in PS 5.1 redirecting a native command's stderr wraps each line
# in an ErrorRecord, which under $ErrorActionPreference='Stop' aborts the script. Silence the
# autocrlf notice at its source instead, and relax the preference across the call.
$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$drift = & git -C $repo -c core.safecrlf=false diff --numstat --ignore-cr-at-eol -- src/core

if ($drift) {
    # A couple of dozen production files drift on EVERY sweep, and none of them is a problem. A
    # `-tests` run converts the package in its TEST closure, which imports more than the production
    # closure, and a wider closure legitimately changes three things in the production emission:
    #
    #   Delta-io alias      `using io = io_package;` becomes a shadow-renamed alias, because the
    #                       test closure pulls in a `go.io` CHILD namespace that `io` would collide
    #                       with (bufio, bytes, strings, regexp, crypto, hash, image).
    #   root qualification  `@internal.x` / `go.math` become root-qualified, because the test
    #                       closure imports a package whose own namespace shadows the root
    #                       (crypto/md5's byteorder; math/rand/v2's `go/format` via regress_test.go).
    #   init-tests hook     production `package_init.cs` gains the partial-method hook the test
    #                       variant's relocated initializers implement -- and the compiler erases
    #                       it again when that half implements nothing, which is why NO committed
    #                       package_init.cs carries the hook and the drift is always the hook
    #                       APPEARING, never changing or vanishing.
    #                       ⚠ THIS CLASS IS DERIVED, AND A BANK OWES IT NOTHING. It used to be six
    #                       hand-maintained rows in the list below under the standing warning "THIS
    #                       LIST IS OWED BY EVERY BANK" -- a debt that went unpaid twice, and each
    #                       time false-redded EVERY full sweep against a healthy package until
    #                       someone re-derived the missing row from the failure (internal/profile,
    #                       2026-08-09; syscall, 2026-08-10). Nothing is listed now. The candidates
    #                       are every package_init.cs in the corpus, recognized by their PATH shape
    #                       ($initHookPathShape), and membership is decided entirely by CONTENT in
    #                       Test-InitTestsHookDrift -- which is also STRICTLY TIGHTER than the list
    #                       it replaces: it requires the hook to be the thing that appeared and
    #                       rejects any removed line, where a listed file was judged on its added
    #                       lines alone.
    #                       ⚠ THE PATH SHAPE COUNTS NO DIRECTORY SEGMENTS, deliberately. Under
    #                       layout L3 a platform-varying package keeps its copy at
    #                       `<pkg>/<goos>/package_init.cs` while every other package keeps it flat
    #                       at `<pkg>/package_init.cs`; one depth-agnostic pattern matches both, so
    #                       the flat-shape assumption that hid syscall's row is not even available
    #                       to make. Spelling the GOOS names instead would have re-created exactly
    #                       the maintenance this retires, at the first new port.
    #
    # Both emissions are correct for their own closure -- only the pipeline pairs them -- so this is
    # owed to whoever owns the next whole-corpus rebank, not to the person running a sweep today.
    # See the charter's `math/rand/v2` worked example and DESIGN-named-interface-wrappers.md section 7.
    #
    # Listing them under the same warning as real drift trains the reader to skip the warning, which
    # is how a genuine regression gets waved through. They get their own section instead -- and only
    # if their content still MATCHES the class, so a stale entry cannot hide a real change.
    #
    # What stays a NAME LIST is the alias/qualification class alone. Those are ordinary production
    # sources with no structural signature to enumerate by, and -- unlike the hook class -- they do
    # not gain a member every time a package banks: the set moves only when the converter's aliasing
    # or qualification changes, which is a converter arc with its own review, not a bank's paperwork.
    $closureFiles = @(
        'src/core/bufio/bufio.cs'
        'src/core/bufio/scan.cs'
        'src/core/bytes/buffer.cs'
        'src/core/bytes/reader.cs'
        'src/core/crypto/crypto.cs'
        'src/core/crypto/md5/md5.cs'
        'src/core/crypto/md5/md5block.cs'
        'src/core/hash/hash.cs'
        'src/core/image/format.cs'
        'src/core/math/rand/v2/pcg.cs'
        'src/core/math/rand/v2/rand.cs'
        'src/core/regexp/backtrack.cs'
        'src/core/regexp/exec.cs'
        'src/core/regexp/regexp.cs'
        'src/core/strings/reader.cs'
        'src/core/strings/replace.cs'
    )

    # The DERIVED class's candidate set: any package_init.cs anywhere under the corpus. `.+` spans
    # any number of directory segments, so `syscall/windows/package_init.cs` (layout L3) and
    # `unicode/package_init.cs` (flat) are the same pattern and no GOOS name appears anywhere.
    # Being a candidate decides nothing on its own -- Test-InitTestsHookDrift below still has to
    # recognize the content.
    $initHookPathShape = '^src/core/.+/package_init\.cs$'

    # Marker glyphs come from the canonical symbol table, never spelled here -- the standing rule
    # for every consumer of the converter's naming constants.
    $symbols = (Get-Content (Join-Path $src 'core/go2cs/symbols.json') -Raw | ConvertFrom-Json).symbols
    $symbolValue = { param($name) ($symbols | Where-Object { $_.name -eq $name }).value }
    $shadow = & $symbolValue 'ShadowVarMarker'
    $temp = & $symbolValue 'TempVarMarker'
    $root = & $symbolValue 'RootNamespace'

    # What an ADDED line in a closure-class diff may look like. Anything else means the file changed
    # for some OTHER reason as well, and it goes back to the warning block where it belongs.
    $closureShapes = @(
        [regex]::Escape("$shadow" + 'io')                     # the shadow-renamed io alias
        [regex]::Escape('global::' + $root + '.')             # root-qualified reference
        [regex]::Escape($root + '.@internal')                 # root-qualified internal package
        [regex]::Escape('init' + $temp + $temp + 'tests')     # the -tests init hook (see below)
        '^\s*//'                                              # a comment carried by the reorder
        '^\s*$'                                               # blank separator
    )

    # Judges the NAME-LISTED alias/qualification class only. (The hook shape stays in the list
    # above because this predicate is generic, but no listed file can gain one: the hook is emitted
    # into package_init.cs alone, and those are routed to Test-InitTestsHookDrift instead.)
    function Test-ClosureClassDrift([string] $path) {
        $added = & git -C $repo -c core.safecrlf=false diff --ignore-cr-at-eol -U0 -- $path |
            Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' } |
            ForEach-Object { $_.Substring(1) }

        foreach ($line in $added) {
            if (-not ($closureShapes | Where-Object { $line -match $_ })) { return $false }
        }

        return $true
    }

    # Judges the DERIVED init-tests hook class, and it is the whole safety argument for having no
    # name list: a candidate is absorbed only when its diff IS the hook, exactly as
    # writePackageInitFile emits it (initOrderOperations.go) -- the call inside the static ctor, a
    # blank line, the four-line explanation, and the erasable declaration. Seven added lines, none
    # removed. Three conditions, each closing a way a real change could pass for the class:
    #
    #   nothing removed   The committed corpus holds the production emission and the -tests
    #                     emission is that PLUS the hook, so this class only ever adds. A
    #                     package_init.cs that LOSES a line has lost a relocated initializer --
    #                     a real regression, and the one the name list waved through, since it
    #                     inspected added lines only.
    #   the hook appears  Without this anchor the comment and blank shapes below would absorb any
    #                     comment-only edit to any package_init.cs in the corpus.
    #   nothing else      Every added line must be the hook, a comment, or blank. One added line
    #                     of real code -- an extra relocated init call, say -- and the file goes
    #                     back to the warning block, hook or no hook.
    function Test-InitTestsHookDrift([string] $path) {
        $hook = [regex]::Escape('init' + $temp + $temp + 'tests')
        $changed = & git -C $repo -c core.safecrlf=false diff --ignore-cr-at-eol -U0 -- $path

        $added = @($changed | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' } | ForEach-Object { $_.Substring(1) })
        $removed = @($changed | Where-Object { $_ -match '^-' -and $_ -notmatch '^---' })

        if ($removed.Count -gt 0) { return $false }
        if (-not ($added | Where-Object { $_ -match $hook })) { return $false }

        foreach ($line in $added) {
            if ($line -notmatch $hook -and $line -notmatch '^\s*//' -and $line -notmatch '^\s*$') { return $false }
        }

        return $true
    }

    $known = @()
    $real = @()

    foreach ($entry in $drift) {
        # numstat row: added <tab> removed <tab> path
        $path = ($entry -split "`t")[-1]

        # A package_init.cs is judged by the derived check ALONE -- the name list has no say over
        # it, in either direction, which is what makes a bank owe this section nothing.
        $isKnown = if ($path -match $initHookPathShape) {
            Test-InitTestsHookDrift $path
        }
        else {
            ($closureFiles -contains $path) -and (Test-ClosureClassDrift $path)
        }

        if ($isKnown) { $known += $entry } else { $real += $entry }
    }

    if ($known) {
        Write-Host ''
        Write-Host "known -tests-closure emission class ($($known.Count) files, documented, not drift):" -ForegroundColor DarkGray
        $known | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    }

    if ($real) {
        Write-Host ''
        Write-Host 'CONTENT drift in the corpus after the sweep -- inspect before banking or restoring:' -ForegroundColor Yellow
        $real | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    }
}

$ErrorActionPreference = $prevEap

if ($fail) {
    Write-Host ''
    Write-Host 'failed:' -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

exit 0
