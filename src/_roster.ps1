<#
.SYNOPSIS
    The validated-package roster reader: docs\ValidatedTestPackages.md parsed into row objects,
    including the per-OS expectation annotations. Dot-source it.

.DESCRIPTION
    The roster table is the single source of truth for which packages are banked and what counts
    they carry, and `run-validated-sweep.ps1` has always read it rather than hardcoding a list. The
    parsing lives HERE rather than inside the sweep for one reason: it now carries a rule with an
    arithmetic consequence -- the per-OS annotation ruled on 2026-08-22 -- and a rule with a
    consequence needs a guard that can exercise it without running a multi-hour gate.
    `check-roster-format.ps1` is that guard; it dot-sources this file exactly as the sweep does.

    Two things are parsed out of a row:

      COLUMNS      | [`pkg`](url) | <matched> | <disclosed> | What it exercises. |
                   Columns 2 and 3 are the WINDOWS record for the Go 1.23.1 era, per the per-OS
                   ruling. They are authoritative and never blended with any other OS's numbers.

      ANNOTATIONS  Inside the What-it-exercises cell, as its own middle-dot-separated segment
                   placed last, immediately before the ` <dot> [proof](...)` link:

                       <dot> linux: 302
                       <dot> linux: 18 + 1

                   `<goos>: <matched>` records that OS's matching-verdict count; the optional
                   `+ <disclosed>` records its disclosed count and is omitted when zero, mirroring
                   the blank Disclosed column. `windows` is deliberately NOT a valid key -- the
                   columns ARE the Windows expectation, and a row claiming otherwise is a
                   contradiction the parser refuses rather than silently prefers one half of.

    The same document also carries the EXCLUSION LEDGER (the "Excluded packages" table), read by
    Get-ExclusionLedgerRows. A ledger row's first cell is a PLAIN code span on purpose -- the
    roster row's linked [`pkg`](url) shape is what $RosterRowPattern anchors on, so the two tables
    in one document can never be confused by either parser; the document's own HTML comment
    beneath the ledger states the same rule from the other side.

    Nothing here has side effects; it defines four functions and returns.

.NOTES
    Requires PowerShell 5.1 (Windows) or PowerShell 7+ (any platform).
    No non-ASCII literal appears in this file: the middle-dot separator is spelled by code point,
    the same discipline the sweep's Go-duration parser uses for the two micro signs.
#>

# The cell-segment separator the roster's What-it-exercises column uses (U+00B7 MIDDLE DOT). Spelled
# by code point so this file stays ASCII and cannot be mojibaked by a PowerShell argument pass.
$RosterSegmentSeparator = [string][char]0x00B7

# The OS keys an annotation may carry: the corpus's non-Windows platform flavors (layout L3 emits
# windows, linux and darwin). `windows` is absent ON PURPOSE -- see Get-ValidatedRosterRows.
$RosterOsKeys = @('linux', 'darwin')

# One row of the table. Row shape:
#   | [`net/http/internal/ascii`](https://...) | 13 | 1 | What it exercises. |
$RosterRowPattern = '^\|\s*\[`([^`]+)`\]\([^)]*\)\s*\|\s*(\d+)\s*\|\s*(\d*)\s*\|'

# The host-conditional annotation (predates the per-OS one, unchanged): the run of backticked,
# comma-separated names right after the colon, stopping at the first text that is not one.
$RosterConditionalPattern = 'host-conditional\s*(?:\([^)]*\))?\s*:\s*((?:`[^`]+`\s*,\s*)*`[^`]+`)'

# The per-OS expectation annotation. Anchored on the cell separator at BOTH ends (the closing
# lookahead also admits the row's terminating pipe) so it can only match a segment of its own --
# prose that happens to say "on linux: five of them" is not an annotation and must not read as one.
$RosterOsPattern =
    [regex]::Escape($RosterSegmentSeparator) +
    '\s*([a-z][a-z0-9]*)\s*:\s*(\d+)\s*(?:\+\s*(\d+)\s*)?(?=' +
    [regex]::Escape($RosterSegmentSeparator) + '|\||$)'

# One row of the exclusion ledger (the "Excluded packages" table in the same document). Its first
# cell is a PLAIN code span, never the roster row's linked [`pkg`](url) shape -- that difference is
# what keeps the two tables apart. Row shape:
#   | `os/user` | <verdicts> | E2 | Mechanism prose. | [ruling][exclusion-ruling] |
$ExclusionLedgerRowPattern = '^\|\s*`([^`]+)`\s*\|\s*([^|]*?)\s*\|\s*([^|]*?)\s*\|'

# The ruled exclusion classes (owner ruling 2026-08-25): E1 no eligible tests on the target
# platform, E2 broken oracle, E3 the test's subject is the replaced representation.
$ExclusionLedgerClasses = @('E1', 'E2', 'E3')

<#
.SYNOPSIS
    The GOOS flavor this host validates -- the corpus flavor a build here actually binds.
.DESCRIPTION
    `_paths.ps1` pins $env:GoTargetOS to 'linux' on a Linux host precisely because every L3 csproj
    defaults the property to `windows` when it is EMPTY. That default is the whole rule: the
    environment variable wins where one is set, and 'windows' is what an unset one means -- on
    Windows because it is right, and on macOS because darwin's corpus does not build yet and keeps
    the status-quo default until its own lane earns one.

    The $IsLinux fallback covers a consumer that dot-sourced this file WITHOUT _paths.ps1; it is
    inert on Windows PowerShell 5.1, where $IsLinux does not exist ($null -> falsey).
#>
function Get-SweepTargetGoos {
    if (-not [string]::IsNullOrWhiteSpace($env:GoTargetOS)) {
        return $env:GoTargetOS.Trim().ToLowerInvariant()
    }

    if ($IsLinux) { return 'linux' }

    return 'windows'
}

<#
.SYNOPSIS
    Parses the roster table into row objects.
.OUTPUTS
    One PSCustomObject per row: Package, Expected, Disclosed, Conditional (string[]), and OS
    (hashtable of goos -> @{ Expected; Disclosed }).
#>
function Get-ValidatedRosterRows {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path $Path)) { throw "Cannot find the validated-package table at $Path" }

    # ReadAllLines rather than Get-Content: PowerShell 5.1 reads a BOM-less UTF-8 file as ANSI,
    # which would split the separator's two bytes into two characters. The column captures are
    # ASCII either way, but the annotation anchors on that separator, so the encoding is now
    # load-bearing and is stated rather than inherited.
    $lines = [System.IO.File]::ReadAllLines($Path)

    $rows = New-Object System.Collections.Generic.List[object]

    foreach ($line in $lines) {
        if ($line -notmatch $RosterRowPattern) { continue }

        # Row fields FIRST: every -match below overwrites $Matches.
        $rowPackage = $Matches[1]
        $rowExpected = [int]$Matches[2]
        $rowDisclosed = if ($Matches[3]) { [int]$Matches[3] } else { 0 }

        $rowConditional = @()
        if ($line -match $RosterConditionalPattern) {
            $rowConditional = @([regex]::Matches($Matches[1], '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
        }

        $rowOs = @{}
        foreach ($match in [regex]::Matches($line, $RosterOsPattern)) {
            $key = $match.Groups[1].Value

            # A `windows:` annotation is a contradiction, not a preference: columns 2 and 3 ARE the
            # Windows expectation, so a row carrying both would hold two Windows answers with no
            # rule for which wins. Refused by name rather than ignored, because an ignored one would
            # read to its author as recorded.
            if ($key -eq 'windows') {
                throw ("Roster row '$rowPackage' carries a 'windows:' per-OS annotation. The Tests " +
                    'and Disclosed COLUMNS are the Windows expectation -- record a Windows count ' +
                    'there, never as an annotation.')
            }

            if ($RosterOsKeys -notcontains $key) {
                throw ("Roster row '$rowPackage' carries an unknown per-OS annotation key '$key'. " +
                    "Known keys: $($RosterOsKeys -join ', ').")
            }

            if ($rowOs.ContainsKey($key)) {
                throw "Roster row '$rowPackage' carries more than one '$key' annotation."
            }

            $rowOs[$key] = [PSCustomObject]@{
                Expected  = [int]$match.Groups[2].Value
                Disclosed = if ($match.Groups[3].Success) { [int]$match.Groups[3].Value } else { 0 }
            }
        }

        [void]$rows.Add([PSCustomObject]@{
            Package     = $rowPackage
            Expected    = $rowExpected
            Disclosed   = $rowDisclosed
            Conditional = $rowConditional
            OS          = $rowOs
        })
    }

    return $rows.ToArray()
}

<#
.SYNOPSIS
    Parses the exclusion-ledger table ("Excluded packages") into row objects.
.OUTPUTS
    One PSCustomObject per row: Package, Verdicts (the raw cell text -- a naive count where one
    exists, an em dash where no baseline exists to count against), Class.
#>
function Get-ExclusionLedgerRows {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path $Path)) { throw "Cannot find the validated-package table at $Path" }

    # ReadAllLines for the same encoding reason Get-ValidatedRosterRows states.
    $lines = [System.IO.File]::ReadAllLines($Path)

    $rows = New-Object System.Collections.Generic.List[object]

    foreach ($line in $lines) {
        if ($line -notmatch $ExclusionLedgerRowPattern) { continue }

        [void]$rows.Add([PSCustomObject]@{
            Package  = $Matches[1]
            Verdicts = $Matches[2]
            Class    = $Matches[3]
        })
    }

    return $rows.ToArray()
}

<#
.SYNOPSIS
    The expectation a row must be validated against on a given GOOS.
.DESCRIPTION
    A verdict count is a fact about (package, OS). Under the row's own annotation for this GOOS,
    that annotation is the expectation; otherwise the Windows columns stand -- which is exactly
    right on Windows, and on another OS is the honest interim the ruling names: the row is compared
    against the Windows number and reported comparison-validated-at-count when it differs.

    Source says which of the two answered: 'columns' or the goos key.
#>
function Get-RosterRowExpectation {
    param(
        [Parameter(Mandatory)][PSCustomObject] $Row,
        [Parameter(Mandatory)][string] $Goos
    )

    $key = $Goos.Trim().ToLowerInvariant()

    if ($key -ne 'windows' -and $Row.OS -and $Row.OS.ContainsKey($key)) {
        return [PSCustomObject]@{
            Expected  = $Row.OS[$key].Expected
            Disclosed = $Row.OS[$key].Disclosed
            Source    = $key
        }
    }

    return [PSCustomObject]@{
        Expected  = $Row.Expected
        Disclosed = $Row.Disclosed
        Source    = 'columns'
    }
}

<#
.SYNOPSIS
    Classifies one row's live result against the expectation in force. Pure -- no I/O, no state.
.DESCRIPTION
    The whole three-bucket rule in one place, so it can be exercised without running the gate:

      pass              the count met the expectation in force (and, for an annotated row, so did
                        the disclosed count)
      host-conditional  the surplus was PROVEN to be the row's named host-conditional verdicts
                        (that proof is evidence-based and lives in the sweep; this takes its answer)
      disclosed-moved   an annotated row's matching count agreed and its DISCLOSED count did not --
                        roster maintenance, never host capability, and never absorbed
      unbanked-count    comparison-validated-at-count: a validated run on an OS this row has no
                        expectation for. Neither a pass nor a drift failure
      count             the banked expectation and reality disagree; one of them is now wrong

    On Windows the reachable set is exactly what it was before the OS dimension existed -- pass,
    host-conditional, count -- because Source is 'columns' for every row (no disclosed check, per
    below) and TargetGoos is 'windows' (no unbanked bucket).

    The COLUMNS path deliberately does not check the disclosed count: a banked Windows row's
    disclosures are enforced where they always were, against the committed proof page, and a second
    enforcement here would move a banked path's verdicts for no new information. An annotation's
    `+ D` half is new ground and is checked where it is written.
#>
function Get-SweepRowClassification {
    param(
        [Parameter(Mandatory)][PSCustomObject] $Expectation,
        [Parameter(Mandatory)][int] $Got,
        [int] $GotDisclosed = 0,
        [Parameter(Mandatory)][string] $TargetGoos,
        [switch] $HostConditionalAccepted
    )

    $disclosedAgrees = ($Expectation.Source -eq 'columns') -or ($GotDisclosed -eq $Expectation.Disclosed)

    if (-not $disclosedAgrees) { return 'disclosed-moved' }
    if ($HostConditionalAccepted) { return 'host-conditional' }
    if ($Got -eq $Expectation.Expected) { return 'pass' }

    if ($TargetGoos.Trim().ToLowerInvariant() -ne 'windows' -and $Expectation.Source -eq 'columns') {
        return 'unbanked-count'
    }

    return 'count'
}
