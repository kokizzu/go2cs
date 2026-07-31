# run-validated-sweep.ps1 - re-validate every banked Phase-4 package against `go test`.
#
# The charter's operational gate: after any change to golib, go2cs-gen, the converter or the
# corpus, every already-validated package must still validate -- verdict for verdict, at its
# exact banked count. This script is that gate.
#
# The roster is READ FROM docs/ValidatedTestPackages.md rather than hardcoded, so it can never
# drift from the table a banking commit just updated: the table is the single source of truth for
# which packages are banked and what counts they carry.
#
#   ./run-validated-sweep.ps1                       # every banked package
#   ./run-validated-sweep.ps1 -Filter compress      # just the ones whose path contains "compress"
#   ./run-validated-sweep.ps1 -TestTimeout 15m      # slower machine / contended box
#   ./run-validated-sweep.ps1 -SkipBuild            # reuse the current go2cs.exe as-is
[CmdletBinding()]
param(
    [string] $Filter,
    # WELL above the pipeline's 2-minute default ON PURPOSE. regexp and strings legitimately run
    # for minutes (regexp exercises the whole RE2 corpus; strings' TestCompareStrings alone is
    # ~110 s), and at the default they report as failures when they are merely slow -- a false
    # red that costs an investigation every time someone hits it.
    [string] $TestTimeout = '10m',
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$src = $PSScriptRoot
$repo = Split-Path $src -Parent
$table = Join-Path $repo 'docs\ValidatedTestPackages.md'
$exe = Join-Path $src 'go2cs\bin\go2cs.exe'
$goroot = (& go env GOROOT).Trim()

if (-not (Test-Path $table)) { throw "Cannot find the validated-package table at $table" }
if (-not $goroot) { throw 'Could not resolve GOROOT -- is the Go toolchain on PATH?' }

# ---- roster: parse the table's rows -------------------------------------------------------------
# Row shape:  | [`net/http/internal/ascii`](https://...) | 13 | 1 | What it exercises. |
# Column 2 is the matching-verdict count, column 3 the disclosed count (blank when none).
$rows = foreach ($line in Get-Content $table) {
    if ($line -match '^\|\s*\[`([^`]+)`\]\([^)]*\)\s*\|\s*(\d+)\s*\|\s*(\d*)\s*\|') {
        [PSCustomObject]@{
            Package   = $Matches[1]
            Expected  = [int]$Matches[2]
            Disclosed = if ($Matches[3]) { [int]$Matches[3] } else { 0 }
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
    Push-Location (Join-Path $src 'go2cs')
    try {
        & go build -o bin\go2cs.exe .
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

# Packages whose C# suite legitimately exceeds the default package deadline. hash/maphash's
# SMHasher matrix runs ~15 minutes in C# (7.6 s in Go — a performance gap, not a correctness one)
# and was BANKED under 30m; at the default it reports a timeout with every test up to the cut
# PASSING, which reads as a failure and costs an investigation every time.
$longTimeouts = @{ 'hash/maphash' = '30m' }

foreach ($row in $rows) {
    $pkg = $row.Package
    $outDir = Join-Path $src ('go-src-converted\' + ($pkg -replace '/', '\'))
    $goDir = Join-Path $goroot ('src\' + ($pkg -replace '/', '\'))
    $label = '{0,-34}' -f $pkg
    $pkgTimeout = if ($longTimeouts.ContainsKey($pkg)) { $longTimeouts[$pkg] } else { $TestTimeout }

    $out = & $exe -tests -test-action all -test-timeout $pkgTimeout $goDir $outDir 2>&1
    $verdict = ($out | Select-String 'Validated (\d+) tests against go test' | Select-Object -First 1)

    if ($verdict) {
        $got = [int]$verdict.Matches[0].Groups[1].Value
        if ($got -eq $row.Expected) {
            $pass++
            Write-Host "  PASS  $label $got" -ForegroundColor Green
        }
        else {
            # Validated, but NOT at its banked count -- a silent change in what the suite asserts.
            # Treat as a failure: the table and reality must agree, one of them is now wrong.
            $fail++; $failed += "$pkg (count $got, banked $($row.Expected))"
            Write-Host "  COUNT $label $got, banked $($row.Expected)" -ForegroundColor Yellow
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
$drift = & git -C $repo -c core.safecrlf=false diff --numstat --ignore-cr-at-eol -- src/go-src-converted
$ErrorActionPreference = $prevEap
if ($drift) {
    Write-Host ''
    Write-Host 'CONTENT drift in the corpus after the sweep -- inspect before banking or restoring:' -ForegroundColor Yellow
    $drift | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($fail) {
    Write-Host ''
    Write-Host 'failed:' -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

exit 0
