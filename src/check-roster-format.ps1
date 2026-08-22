<#
.SYNOPSIS
    Guards the validated-package roster's machine-parsed format and its arithmetic.

.DESCRIPTION
    Two things this checks, both cheap enough to run at any time (pure text, no build, no gate):

      1. THE PARSER'S CONTRACT, against fixture rows -- the columns, the host-conditional
         annotation, and the per-OS annotation ruled on 2026-08-22, including the shapes that must
         NOT parse as one. The parser lives in `_roster.ps1` and is what `run-validated-sweep.ps1`
         reads the roster with, so a defect here moves a gate's verdict; it is guarded where the
         parsing lives rather than where the sweep runs.

      2. THE ROSTER'S OWN ARITHMETIC, derived from the table every time -- the progress header's
         package count, verdict sum and disclosed sum against the columns, and the Linux progress
         line against the per-OS annotations. Nothing is hand-listed here: a hand-maintained roster
         mirror is the exact debt the sweep's own drift section records going unpaid twice, so
         every number this asserts is computed from the table it is asserting about.

        ./check-roster-format.ps1            # the guard
        ./check-roster-format.ps1 -List      # also print every per-OS annotation the roster carries

.NOTES
    Requires PowerShell 5.1 (Windows) or PowerShell 7+ (any platform). Exit 0 clean, 1 on any
    violation. No non-ASCII literal: the roster's separator glyphs are spelled by code point.
#>
[CmdletBinding()]
param(
    [switch] $List
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_roster.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$table = Join-Path $repo 'docs/ValidatedTestPackages.md'

$dot = [string][char]0x00B7      # U+00B7 MIDDLE DOT -- the cell-segment separator
$dash = [string][char]0x2014     # U+2014 EM DASH -- the header's clause separator

$failures = New-Object System.Collections.Generic.List[string]
$checks = 0

function Assert-Equal {
    param([string] $What, $Expected, $Actual)

    $script:checks++
    if ("$Expected" -ne "$Actual") {
        [void]$script:failures.Add("$What -- expected '$Expected', got '$Actual'")
    }
}

function Assert-Throws {
    param([string] $What, [scriptblock] $Body, [string] $Fragment)

    $script:checks++
    try {
        & $Body | Out-Null
        [void]$script:failures.Add("$What -- expected a throw naming '$Fragment', nothing was thrown")
    }
    catch {
        if ("$_" -notmatch [regex]::Escape($Fragment)) {
            [void]$script:failures.Add("$What -- expected a throw naming '$Fragment', got '$_'")
        }
    }
}

# Writes fixture rows to a uniquely-named temp roster and parses it. Unique per call so two
# concurrent runs (a lane and a sibling worktree) cannot collide on one another's fixture.
function Read-FixtureRoster {
    param([string[]] $Rows)

    $header = @(
        '| Package | Tests | Disclosed | What it exercises |'
        '|:--|:--:|:--:|:--|'
    )
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ('go2cs-roster-fixture-' + [guid]::NewGuid().ToString('n') + '.md')

    try {
        [System.IO.File]::WriteAllText($path, (($header + $Rows) -join "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
        return @(Get-ValidatedRosterRows -Path $path)
    }
    finally {
        if (Test-Path $path) { Remove-Item $path -Force }
    }
}

Write-Host 'roster format guard' -ForegroundColor Cyan

# ---- 1. the parser's contract, against fixtures --------------------------------------------------
$fixtureRows = @(
    "| [``plain/pkg``](https://x/plain) | 12 |  | Nothing special. $dot [proof](p.md) |"
    "| [``disc/pkg``](https://x/disc) | 12 | 3 | Has disclosures. $dot [proof](p.md) |"
    "| [``cond/pkg``](https://x/cond) | 61 |  | Path algebra $dot host-conditional (privilege $dash colon-free): ``TestA/one``, ``TestB/two`` $dot linux: 54 $dot [proof](p.md) |"
    "| [``ann/pkg``](https://x/ann) | 298 |  | Random ints. $dot linux: 302 $dot [proof](p.md) |"
    "| [``annd/pkg``](https://x/annd) | 17 | 1 | Mime tables. $dot linux: 18 + 1 $dot [proof](p.md) |"
    "| [``dar/pkg``](https://x/dar) | 5 |  | Mac things. $dot darwin: 7 $dot [proof](p.md) |"
    "| [``prose/pkg``](https://x/prose) | 9 |  | Behavior on linux: 5 subtests skip. $dot [proof](p.md) |"
    "| [``segment/pkg``](https://x/segment) | 9 |  | Counted here $dot linux: 5 subtests skip $dot [proof](p.md) |"
    "| [``tail/pkg``](https://x/tail) | 4 |  | Ends on the annotation $dot linux: 6 |"
)

$fixture = Read-FixtureRoster $fixtureRows
$byName = @{}
foreach ($row in $fixture) { $byName[$row.Package] = $row }

Assert-Equal 'fixture: every row parses' 9 $fixture.Count

Assert-Equal 'columns: matched count' 12 $byName['plain/pkg'].Expected
Assert-Equal 'columns: blank disclosed reads 0' 0 $byName['plain/pkg'].Disclosed
Assert-Equal 'columns: disclosed count' 3 $byName['disc/pkg'].Disclosed
Assert-Equal 'columns: a plain row carries no annotation' 0 $byName['plain/pkg'].OS.Count

# The host-conditional annotation must survive an OS annotation following it in the same cell --
# its capture stops at the last backticked name, and the per-OS segment starts after that.
Assert-Equal 'host-conditional: names still parse beside an OS annotation' 'TestA/one,TestB/two' ($byName['cond/pkg'].Conditional -join ',')
Assert-Equal 'host-conditional: the row also carries its OS annotation' 54 $byName['cond/pkg'].OS['linux'].Expected

Assert-Equal 'annotation: N alone' 302 $byName['ann/pkg'].OS['linux'].Expected
Assert-Equal 'annotation: N alone means zero disclosed' 0 $byName['ann/pkg'].OS['linux'].Disclosed
Assert-Equal 'annotation: N + D matched' 18 $byName['annd/pkg'].OS['linux'].Expected
Assert-Equal 'annotation: N + D disclosed' 1 $byName['annd/pkg'].OS['linux'].Disclosed
Assert-Equal 'annotation: darwin is a valid key' 7 $byName['dar/pkg'].OS['darwin'].Expected
Assert-Equal 'annotation: it is the last segment, terminating pipe included' 6 $byName['tail/pkg'].OS['linux'].Expected

# The two ways prose must NOT read as an annotation: no separator before it, and a segment that
# continues into words after the number.
Assert-Equal 'prose: unseparated "linux: 5" is not an annotation' 0 $byName['prose/pkg'].OS.Count
Assert-Equal 'prose: a segment continuing past the number is not an annotation' 0 $byName['segment/pkg'].OS.Count

# The columns ARE the Windows expectation, so a windows-keyed annotation is a contradiction, and an
# unknown key is a typo the sweep must not silently drop.
Assert-Throws 'annotation: a windows key is refused by name' {
    Read-FixtureRoster @("| [``w/pkg``](https://x/w) | 3 |  | Two Windows answers. $dot windows: 4 $dot [proof](p.md) |")
} "carries a 'windows:' per-OS annotation"
Assert-Throws 'annotation: an unknown key is refused by name' {
    Read-FixtureRoster @("| [``p/pkg``](https://x/p) | 3 |  | Not a corpus flavor. $dot plan9: 4 $dot [proof](p.md) |")
} 'unknown per-OS annotation key'
Assert-Throws 'annotation: a repeated key is refused' {
    Read-FixtureRoster @("| [``r/pkg``](https://x/r) | 3 |  | Twice. $dot linux: 4 $dot linux: 5 $dot [proof](p.md) |")
} 'more than one'

# Expectation resolution: the annotation answers on its own OS, the columns everywhere else --
# including on Windows, where an annotation must never displace the banked columns.
$annotated = $byName['annd/pkg']
$plain = $byName['plain/pkg']

Assert-Equal 'expectation: annotated row under its own OS' 18 (Get-RosterRowExpectation -Row $annotated -Goos 'linux').Expected
Assert-Equal 'expectation: annotated row under its own OS, disclosed' 1 (Get-RosterRowExpectation -Row $annotated -Goos 'linux').Disclosed
Assert-Equal 'expectation: annotated row names its source' 'linux' (Get-RosterRowExpectation -Row $annotated -Goos 'linux').Source
Assert-Equal 'expectation: annotated row on Windows still reads the columns' 17 (Get-RosterRowExpectation -Row $annotated -Goos 'windows').Expected
Assert-Equal 'expectation: annotated row on Windows names the columns' 'columns' (Get-RosterRowExpectation -Row $annotated -Goos 'windows').Source
Assert-Equal 'expectation: unannotated row falls back to the columns' 12 (Get-RosterRowExpectation -Row $plain -Goos 'linux').Expected
Assert-Equal 'expectation: unannotated row names the columns' 'columns' (Get-RosterRowExpectation -Row $plain -Goos 'linux').Source
Assert-Equal 'expectation: a different OS does not read another OS annotation' 'columns' (Get-RosterRowExpectation -Row $annotated -Goos 'darwin').Source

# ---- 1b. the sweep's classification rule ---------------------------------------------------------
# The rule the sweep reports from, exercised without running the gate. The WINDOWS rows come first
# and matter most: they are the proof that the reachable classes on Windows are exactly the three
# that existed before the OS dimension did.
$winPlain = Get-RosterRowExpectation -Row $plain -Goos 'windows'          # columns 12 / 0
$winAnnotated = Get-RosterRowExpectation -Row $annotated -Goos 'windows'  # columns 17 / 1
$linAnnotated = Get-RosterRowExpectation -Row $annotated -Goos 'linux'    # annotation 18 / 1
$linPlain = Get-RosterRowExpectation -Row $plain -Goos 'linux'            # columns 12 / 0

Assert-Equal 'windows: a row at its banked count passes' 'pass' `
    (Get-SweepRowClassification -Expectation $winPlain -Got 12 -GotDisclosed 0 -TargetGoos 'windows')
Assert-Equal 'windows: a row off its banked count is a count failure' 'count' `
    (Get-SweepRowClassification -Expectation $winPlain -Got 13 -GotDisclosed 0 -TargetGoos 'windows')
Assert-Equal 'windows: a proven host-conditional surplus passes' 'host-conditional' `
    (Get-SweepRowClassification -Expectation $winPlain -Got 18 -GotDisclosed 0 -TargetGoos 'windows' -HostConditionalAccepted)
Assert-Equal 'windows: the disclosed count is NOT re-enforced on the columns path' 'pass' `
    (Get-SweepRowClassification -Expectation $winAnnotated -Got 17 -GotDisclosed 4 -TargetGoos 'windows')
Assert-Equal 'windows: an annotated row is still judged by its columns' 'count' `
    (Get-SweepRowClassification -Expectation $winAnnotated -Got 18 -GotDisclosed 1 -TargetGoos 'windows')
Assert-Equal 'windows: comparison-validated-at-count is unreachable' 'count' `
    (Get-SweepRowClassification -Expectation $winPlain -Got 99 -GotDisclosed 0 -TargetGoos 'windows')

Assert-Equal 'linux: an annotated row passes at its linux count' 'pass' `
    (Get-SweepRowClassification -Expectation $linAnnotated -Got 18 -GotDisclosed 1 -TargetGoos 'linux')
Assert-Equal 'linux: an annotated row off its linux count is a count failure' 'count' `
    (Get-SweepRowClassification -Expectation $linAnnotated -Got 17 -GotDisclosed 1 -TargetGoos 'linux')
Assert-Equal 'linux: an annotated row whose disclosures moved is named as that' 'disclosed-moved' `
    (Get-SweepRowClassification -Expectation $linAnnotated -Got 18 -GotDisclosed 0 -TargetGoos 'linux')
Assert-Equal 'linux: a moved disclosure is never absorbed as host-conditional' 'disclosed-moved' `
    (Get-SweepRowClassification -Expectation $linAnnotated -Got 19 -GotDisclosed 0 -TargetGoos 'linux' -HostConditionalAccepted)
Assert-Equal 'linux: an unannotated row still passes at the windows count' 'pass' `
    (Get-SweepRowClassification -Expectation $linPlain -Got 12 -GotDisclosed 0 -TargetGoos 'linux')
Assert-Equal 'linux: an unannotated row off the windows count is comparison-validated-at-count' 'unbanked-count' `
    (Get-SweepRowClassification -Expectation $linPlain -Got 14 -GotDisclosed 0 -TargetGoos 'linux')
Assert-Equal 'linux: a lost verdict on an unannotated row is also unbanked, never a silent pass' 'unbanked-count' `
    (Get-SweepRowClassification -Expectation $linPlain -Got 1 -GotDisclosed 0 -TargetGoos 'linux')

# ---- 2. the roster's own arithmetic --------------------------------------------------------------
$rows = @(Get-ValidatedRosterRows -Path $table)
$lines = [System.IO.File]::ReadAllLines($table)

function Get-HeaderNumber {
    param([string[]] $Lines, [string] $Select, [string] $Pattern, [int] $Group = 1)

    foreach ($line in $Lines) {
        if ($line -match [regex]::Escape($Select) -and $line -match $Pattern) {
            return [int](($Matches[$Group]) -replace ',', '')
        }
    }

    return -1
}

$columnTotal = ($rows | Measure-Object -Property Expected -Sum).Sum
$columnDisclosed = ($rows | Measure-Object -Property Disclosed -Sum).Sum

Assert-Equal 'header: validated package count equals the table row count' $rows.Count `
    (Get-HeaderNumber $lines 'Phase 4 progress' '(\d+)\s*/\s*(\d+)\s+testable packages validated')
Assert-Equal 'header: matching verdicts equal the Tests column sum' $columnTotal `
    (Get-HeaderNumber $lines 'matching test verdicts' '([\d,]+)\s+matching test verdicts')
Assert-Equal 'header: disclosed equals the Disclosed column sum' $columnDisclosed `
    (Get-HeaderNumber $lines 'matching test verdicts' '([\d,]+)\s+disclosed')

$testable = Get-HeaderNumber $lines 'Phase 4 progress' '(\d+)\s*/\s*(\d+)\s+testable packages validated' 2
$percentText = ''
foreach ($line in $lines) {
    if ($line -match 'Phase 4 progress' -and $line -match '([\d.]+)%') { $percentText = $Matches[1]; break }
}
if ($testable -gt 0) {
    $expectedPercent = [math]::Round(($rows.Count / [double]$testable) * 100, 1, [MidpointRounding]::AwayFromZero)
    Assert-Equal 'header: the percentage follows from the two counts' ('{0:0.0}' -f $expectedPercent) $percentText
}

# The Linux progress line is summed from the annotations exactly as the header above it is summed
# from the columns -- derived on both sides, so neither can drift from the table it describes.
$linuxRows = @($rows | Where-Object { $_.OS.ContainsKey('linux') })
$linuxTotal = 0
$linuxDisclosed = 0
foreach ($row in $linuxRows) {
    $linuxTotal += $row.OS['linux'].Expected
    $linuxDisclosed += $row.OS['linux'].Disclosed
}

Assert-Equal 'linux header: annotated row count' $linuxRows.Count `
    (Get-HeaderNumber $lines 'Linux:' 'Linux:\s*\*{0,2}(\d+)\s+of\s+(\d+)\s+rows')
Assert-Equal 'linux header: denominator is the whole table' $rows.Count `
    (Get-HeaderNumber $lines 'Linux:' 'Linux:\s*\*{0,2}(\d+)\s+of\s+(\d+)\s+rows' 2)
Assert-Equal 'linux header: matching verdicts equal the annotation sum' $linuxTotal `
    (Get-HeaderNumber $lines 'Linux:' '([\d,]+)\s+matching verdicts')
Assert-Equal 'linux header: disclosed equals the annotation sum' $linuxDisclosed `
    (Get-HeaderNumber $lines 'Linux:' '([\d,]+)\s+disclosed')

# Every annotation must be a real expectation, not a placeholder: a zero-count row would read as
# "validated at nothing" in the header's numerator.
foreach ($row in $linuxRows) {
    Assert-Equal "annotation is a real count: $($row.Package)" $true ($row.OS['linux'].Expected -gt 0)
}

if ($List) {
    Write-Host ''
    Write-Host 'per-OS annotations in the roster:' -ForegroundColor Cyan
    foreach ($row in ($rows | Where-Object { $_.OS.Count -gt 0 } | Sort-Object Package)) {
        foreach ($key in ($row.OS.Keys | Sort-Object)) {
            $windows = "windows $($row.Expected)" + $(if ($row.Disclosed) { " + $($row.Disclosed)" } else { '' })
            $osText = "$key $($row.OS[$key].Expected)" + $(if ($row.OS[$key].Disclosed) { " + $($row.OS[$key].Disclosed)" } else { '' })
            Write-Host ('  {0,-34} {1,-16} {2}' -f $row.Package, $windows, $osText)
        }
    }
}

Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host "roster format guard: $($failures.Count) of $checks checks FAILED" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

Write-Host "roster format guard: $checks checks pass ($($rows.Count) rows, $($linuxRows.Count) with a linux annotation)" -ForegroundColor Green
exit 0
