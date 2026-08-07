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
# PASSING, which reads as a failure and costs an investigation every time. index/suffixarray is
# the same shape and worse: `TestNew{32,64}/exhaustive3` brute-forces every string up to length 8
# over a 3-letter alphabet, 12.4 s in Go and ~35 min in C#; under 10m it reports exactly the
# TestNew64/exhaustive3 tail as empty verdicts.
$longTimeouts = @{ 'hash/maphash' = '30m'; 'index/suffixarray' = '60m' }

foreach ($row in $rows) {
    $pkg = $row.Package
    $outDir = Join-Path $src ('core\' + ($pkg -replace '/', '\'))
    $goDir = Join-Path $goroot ('src\' + ($pkg -replace '/', '\'))
    $label = '{0,-34}' -f $pkg
    $pkgTimeout = if ($longTimeouts.ContainsKey($pkg)) { $longTimeouts[$pkg] } else { $TestTimeout }

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
$drift = & git -C $repo -c core.safecrlf=false diff --numstat --ignore-cr-at-eol -- src/core

if ($drift) {
    # Twenty production files drift on EVERY sweep, and none of them is a problem. A `-tests` run
    # converts the package in its TEST closure, which imports more than the production closure, and
    # a wider closure legitimately changes three things in the production emission:
    #
    #   Delta-io alias      `using io = io_package;` becomes a shadow-renamed alias, because the
    #                       test closure pulls in a `go.io` CHILD namespace that `io` would collide
    #                       with (bufio, bytes, strings, regexp, crypto, hash, image).
    #   root qualification  `@internal.x` / `go.math` become root-qualified, because the test
    #                       closure imports a package whose own namespace shadows the root
    #                       (crypto/md5's byteorder; math/rand/v2's `go/format` via regress_test.go).
    #   init-tests hook     production `package_init.cs` gains the partial-method hook the test
    #                       variant's relocated initializers implement (unicode, internal/zstd,
    #                       time, and internal/buildcfg -- whose test half implements nothing, so
    #                       the hook is erased again and the committed file stays hookless).
    #
    # Both emissions are correct for their own closure -- only the pipeline pairs them -- so this is
    # owed to whoever owns the next whole-corpus rebank, not to the person running a sweep today.
    # See the charter's `math/rand/v2` worked example and DESIGN-named-interface-wrappers.md section 7.
    #
    # Listing them under the same warning as real drift trains the reader to skip the warning, which
    # is how a genuine regression gets waved through. They get their own section instead -- and only
    # if their content still MATCHES the class, so a stale entry cannot hide a real change.
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
        'src/core/internal/buildcfg/package_init.cs'
        'src/core/internal/zstd/package_init.cs'
        'src/core/math/rand/v2/pcg.cs'
        'src/core/math/rand/v2/rand.cs'
        'src/core/regexp/backtrack.cs'
        'src/core/regexp/exec.cs'
        'src/core/regexp/regexp.cs'
        'src/core/strings/reader.cs'
        'src/core/strings/replace.cs'
        'src/core/time/package_init.cs'
        'src/core/unicode/package_init.cs'
    )

    # Marker glyphs come from the canonical symbol table, never spelled here -- the standing rule
    # for every consumer of the converter's naming constants.
    $symbols = (Get-Content (Join-Path $src 'core\go2cs\symbols.json') -Raw | ConvertFrom-Json).symbols
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
        [regex]::Escape('init' + $temp + $temp + 'tests')     # the -tests init hook
        '^\s*//'                                              # the hook's explanatory comment
        '^\s*$'                                               # blank separator
    )

    function Test-ClosureClassDrift([string] $path) {
        $added = & git -C $repo -c core.safecrlf=false diff --ignore-cr-at-eol -U0 -- $path |
            Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' } |
            ForEach-Object { $_.Substring(1) }

        foreach ($line in $added) {
            if (-not ($closureShapes | Where-Object { $line -match $_ })) { return $false }
        }

        return $true
    }

    $known = @()
    $real = @()

    foreach ($entry in $drift) {
        # numstat row: added <tab> removed <tab> path
        $path = ($entry -split "`t")[-1]

        if ($closureFiles -contains $path -and (Test-ClosureClassDrift $path)) { $known += $entry } else { $real += $entry }
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
