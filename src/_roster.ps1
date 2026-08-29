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
        [switch] $HostConditionalAccepted,
        [switch] $CapabilityAbsentAccepted
    )

    $disclosedAgrees = ($Expectation.Source -eq 'columns') -or ($GotDisclosed -eq $Expectation.Disclosed)

    if (-not $disclosedAgrees) { return 'disclosed-moved' }
    if ($HostConditionalAccepted) { return 'host-conditional' }
    if ($CapabilityAbsentAccepted) { return 'capability-absent' }
    if ($Got -eq $Expectation.Expected) { return 'pass' }

    if ($TargetGoos.Trim().ToLowerInvariant() -ne 'windows' -and $Expectation.Source -eq 'columns') {
        return 'unbanked-count'
    }

    return 'count'
}

# ---- capability-conditional verdicts (the MIRROR of the host-conditional surplus above) ----------
# Get-SweepRowClassification above assumes a surplus is the only host-dependent shape: the roster
# banks a FLOOR and a more-capable host produces EXTRA verdicts (host-conditional). Some
# capability-bound test blocks run the opposite way -- the roster banks the CEILING, every case the
# capability enables, and a host lacking the prerequisite never spawns the case matrix at all, so
# the whole block collapses to the ONE top-level verdict. crypto/tls's TestBogoSuite is the first of
# these (the BoGo/BoringSSL shim runner): 3,243 sub-verdicts -- 1 parent + 861 pass + 2,381 skip --
# collapse to exactly one. "A lost verdict is never host-conditional" stays true for every OTHER
# shortfall: a caller engages this ONLY for a package it registers, and ONLY when the shortfall
# matches that package's block size exactly -- run-validated-sweep.ps1's
# $capabilityConditionalBlocks table is where that registration lives; this function is the pure
# rule, proven directly by check-roster-format.ps1's fixtures rather than through a roster row (the
# evidence is a comparison record and a proof page, not a table cell).
#
# ⚠ WHAT THE COLLAPSED VERDICT ACTUALLY IS, MEASURED (2026-08-28, the bogo skip-parity lane).
# This rule was first written expecting SKIP on both sides -- "Go's own oracle skips identically
# absent the runner". Go's oracle does no such thing, and the difference is the whole mechanism.
# `TestBogoSuite`'s only skip branches (`bogo_shim_test.go:337-347`) are short-mode, js/wasip1, no
# `go build`, no exec, and the builders' Windows-flake guard -- NONE of them is "the BoGo runner is
# absent". Absent the runner the test **FAILS**, at `bogo_shim_test.go:364`
# (`t.Fatalf("failed to download boringssl: %s", err)`) -- measured directly by pointing GOMODCACHE
# at an empty directory with GOPROXY=off: `--- FAIL: TestBogoSuite (0.35s)`, never a skip.
# So the shape a genuinely capability-less host produces is Go **fail** / C# **fail**, which is
# precisely the SECOND accepted shape of a host-conditional disclosure -- and the converter accounts
# that root as DISCLOSED rather than matched (`matchTerminalStatuses`, testConversion.go), so on
# such a host the live disclosed count is the banked one PLUS the root, by construction. Demanding
# skip-on-both AND an unmoved disclosed count therefore made this rule unfireable for its only
# registered member, in two independent ways at once.
#
# What is NOT capability-absent, and must stay red: the block root AGREEING is the load-bearing
# evidence, because a Go side that PASSED established the whole matrix -- the shortfall is then the
# converted side's alone (crypto/tls on a host whose managed shims miss the BoGo runner's own
# 600 s deadline reads exactly this way: Go pass / C# fail, same 3,243 shortfall, same 400 matched).
# Absorbing that would wave through a real, measured divergence, so the rule below accepts only an
# AGREEING NON-PASS root and refuses every other combination by name.
function Test-CapabilityAbsentDelta {
    param(
        [int] $Expected,           # banked matching-verdict count (roster column 2, the CEILING here)
        [int] $Disclosed,
        [PSCustomObject] $Block,   # @{ Test = <top-level name>; BlockSize = <int> }
        [int] $Got,
        $Comparison,
        [string[]] $BankedNames
    )

    function New-CapabilityAbsentResult([bool] $accepted, [string] $reason) {
        return [PSCustomObject]@{ Accepted = $accepted; Reason = $reason }
    }

    $shortfall = $Expected - $Got
    if ($shortfall -ne $Block.BlockSize) {
        return New-CapabilityAbsentResult $false "shortfall $shortfall does not match $($Block.Test)'s registered block size $($Block.BlockSize) -- a lost verdict outside the named block is never capability-conditional"
    }
    if ($null -eq $Comparison -or $null -eq $Comparison.go -or $null -eq $Comparison.csharp) {
        return New-CapabilityAbsentResult $false 'comparison record carries no per-test verdict maps'
    }

    if ($BankedNames.Count -ne ($Expected + $Disclosed)) {
        return New-CapabilityAbsentResult $false "committed proof page lists $($BankedNames.Count) verdicts where the roster banks $Expected matched + $Disclosed disclosed -- page and table disagree"
    }

    # The block is every banked name that IS the top-level test or one of its Go subtests (the '/'
    # naming go test -json itself uses). This must be exactly BlockSize names, or the committed
    # evidence disagrees with the registered size and nothing below can be trusted.
    $blockPrefix = "$($Block.Test)/"
    $blockNames = @($BankedNames | Where-Object { $_ -eq $Block.Test -or $_.StartsWith($blockPrefix) })
    if ($blockNames.Count -ne $Block.BlockSize) {
        return New-CapabilityAbsentResult $false "the committed proof page names $($blockNames.Count) verdicts under $($Block.Test), not the registered $($Block.BlockSize) -- re-derive the block size before trusting this row"
    }

    $goMap = $Comparison.go
    $csMap = $Comparison.csharp
    $liveNames = @($goMap.Keys)

    # Every banked name OUTSIDE the block must still be present live -- the mechanism absorbs the
    # named block collapsing, nothing else.
    $expectedOutside = @($BankedNames | Where-Object { $blockNames -notcontains $_ })
    $missingOutside = @($expectedOutside | Where-Object { $liveNames -notcontains $_ })
    if ($missingOutside.Count -gt 0) {
        return New-CapabilityAbsentResult $false "banked verdicts outside the block missing from this run: $($missingOutside -join ', ')"
    }

    # No block subtest may appear at all (they were never spawned), and the top-level name must be
    # present and AGREEING -- see the measured note above for why "agreeing" is the test and "skip"
    # is not.
    $liveBlockNames = @($liveNames | Where-Object { $_ -eq $Block.Test -or $_.StartsWith($blockPrefix) })
    if (@($liveBlockNames | Where-Object { $_ -ne $Block.Test }).Count -gt 0) {
        return New-CapabilityAbsentResult $false "subtests under $($Block.Test) appear in this run -- the capability is not cleanly absent, re-diagnose rather than absorb"
    }
    if ($liveBlockNames -notcontains $Block.Test) {
        return New-CapabilityAbsentResult $false "$($Block.Test) itself is missing from this run -- an absent capability must still report its one collapsed verdict"
    }

    $goVerdict = $goMap[$Block.Test]
    $csVerdict = if (-not $csMap.ContainsKey($Block.Test)) { 'absent' } else { $csMap[$Block.Test] }
    if ($goVerdict -ne $csVerdict) {
        return New-CapabilityAbsentResult $false "$($Block.Test): go '$goVerdict' vs C# '$csVerdict' -- an absent capability collapses IDENTICALLY on both runtimes; a disagreement here is a real divergence on a host that has the capability, not a missing one"
    }
    if ($goVerdict -ne 'skip' -and $goVerdict -ne 'fail') {
        return New-CapabilityAbsentResult $false "$($Block.Test): both runtimes report '$goVerdict' -- a PASSING oracle established the case matrix, so the shortfall is the converted side's and is never capability-absent"
    }

    # THE discriminator, and the reason agreement alone is not enough (measured 2026-08-28 on the
    # i7-5820K). A host that HAS the capability but whose converted side misses the runner's own
    # deadline can ALSO show fail/fail: Go fans out all 3,242 BoGo cases and fails a handful of them
    # flakily, the converted side fails at the wall, and every count above is bit-for-bit identical
    # to the capability-absent shape. What is not identical is the FAN-OUT: those 3,242 Go-side rows
    # exist, and `matchTerminalStatuses` withdraws them by name into the comparison record. An
    # absent capability produces NO such rows, because the test dies before its case matrix exists.
    # So a withdrawal under the block is proof the capability was present, and the shortfall is the
    # converted side's alone. (`withdrawn` is omitempty, so a record without one withdrew nothing.)
    $withdrawnUnderBlock = @()
    if ($null -ne $Comparison.withdrawn) {
        $withdrawnUnderBlock = @($Comparison.withdrawn | Where-Object { $_ -eq $Block.Test -or $_.StartsWith($blockPrefix) })
    }
    if ($withdrawnUnderBlock.Count -gt 0) {
        return New-CapabilityAbsentResult $false "$($withdrawnUnderBlock.Count) Go-side verdict(s) under $($Block.Test) were withdrawn -- the case matrix DID fan out, so the capability was present on this host and the lost verdicts are the converted side's, not the capability's"
    }

    # The disclosed accounting is decided by WHICH collapsed verdict this is, because the converter
    # accounts the two shapes differently and the roster's banked number was taken on a host that
    # had the capability:
    #   fail/fail -- the host-conditional shape. matchTerminalStatuses accounts the annotated root
    #                as DISCLOSED (never as an agreed match), so the live count is banked + 1 and
    #                the extra entry must BE this block's root.
    #   skip/skip -- no disclosure fires (a skip is not the pinned failure), so the count is banked
    #                exactly and the root must NOT appear among the disclosures.
    # Anything else is a disclosure that moved for some unrelated reason, which is what this check
    # has always existed to catch.
    $liveDisclosedEntries = @()
    if ($null -ne $Comparison.disclosed) { $liveDisclosedEntries = @($Comparison.disclosed) }
    $liveDisclosed = $liveDisclosedEntries.Count
    # A disclosure entry reads "<TestName> (<class>): <reason>"; the name is everything before the
    # first " (" and is compared whole, so a prefix can never pass for the root.
    $rootIsDisclosed = @($liveDisclosedEntries | Where-Object { ($_ -split ' \(', 2)[0] -eq $Block.Test }).Count -gt 0
    $expectedDisclosed = if ($goVerdict -eq 'fail') { $Disclosed + 1 } else { $Disclosed }
    if ($liveDisclosed -ne $expectedDisclosed) {
        return New-CapabilityAbsentResult $false "disclosed count moved ($liveDisclosed live vs $expectedDisclosed expected for an agreeing '$goVerdict' collapse of $($Block.Test), against $Disclosed banked) -- not a capability-conditional shape"
    }
    if ($goVerdict -eq 'fail' -and -not $rootIsDisclosed) {
        return New-CapabilityAbsentResult $false "$($Block.Test) fails on both runtimes but is not among this run's disclosures -- the extra disclosure is some other row, so this is not the collapse it looks like"
    }
    if ($goVerdict -eq 'skip' -and $rootIsDisclosed) {
        return New-CapabilityAbsentResult $false "$($Block.Test) skips on both runtimes yet is disclosed -- the pinned divergence fired on a shape it does not describe, re-diagnose rather than absorb"
    }

    # Nothing live may go unaccounted for: the outside names plus the one collapsed root, exactly.
    $accountedFor = @($expectedOutside) + @($Block.Test)
    $unaccounted = @($liveNames | Where-Object { $accountedFor -notcontains $_ })
    if ($unaccounted.Count -gt 0) {
        return New-CapabilityAbsentResult $false "live verdicts this row does not account for: $($unaccounted -join ', ')"
    }

    return New-CapabilityAbsentResult $true $null
}

# ---- comparison-record reader --------------------------------------------------------------------
# Reads go2cs_test_comparison.json into the shape the two delta rules above consume: `go`/`csharp`
# as CASE-SENSITIVE (ordinal) dictionaries, `withdrawn`/`disclosed` as arrays or $null.
#
# ⚠ Why not ConvertFrom-Json (measured 2026-08-29, G's net/http pre-staging): a Go test suite can
# legitimately hold verdict names differing ONLY by case -- net/http's
# TestTransportContentEncodingCaseInsensitive spawns .../GZIP and .../gzip pairs, which is exactly
# what a test with that name would do. Windows PowerShell 5.1's JSON->PSObject path folds member
# names case-insensitively and THROWS on such input ("contains the duplicated keys"), and a
# PSObject cannot hold the pair at all -- so both the old parser and the old property-bag shape are
# structurally unable to carry a legal record. The converter's own maps are Go map[string]string,
# case-sensitive by construction (all eleven case-folding sites in testConversion.go are on paths
# and env-var names, none on a test name), so the record is sound; only the PowerShell reader was
# not. JavaScriptSerializer deserializes into case-sensitive dictionaries; the explicit ordinal
# re-copy below makes that deliberate rather than inherited, and a true duplicate key (the same
# exact name twice) still throws loudly on Add.
function ConvertFrom-ComparisonRecord {
    param([string] $Path)

    if (-not $script:comparisonSerializer) {
        Add-Type -AssemblyName System.Web.Extensions
        $script:comparisonSerializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
        $script:comparisonSerializer.MaxJsonLength = [int]::MaxValue
    }

    $raw = $script:comparisonSerializer.DeserializeObject([System.IO.File]::ReadAllText($Path))
    if ($null -eq $raw) { throw "comparison record at $Path deserialized to nothing" }

    $toOrdinalMap = {
        param($member)
        if (-not $raw.ContainsKey($member)) { return $null }
        $map = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::Ordinal)
        foreach ($entry in $raw[$member].GetEnumerator()) { $map.Add([string]$entry.Key, [string]$entry.Value) }
        return , $map
    }

    return [PSCustomObject]@{
        go        = & $toOrdinalMap 'go'
        csharp    = & $toOrdinalMap 'csharp'
        withdrawn = if ($raw.ContainsKey('withdrawn')) { @($raw['withdrawn']) } else { $null }
        disclosed = if ($raw.ContainsKey('disclosed')) { @($raw['disclosed']) } else { $null }
    }
}
