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

                   A row may also carry an EXECUTION annotation, the same segment shape:

                       <dot> execution: release-tc0

                   That one is not a count and not a platform -- it is the local execution CONFIG
                   the row's pipeline leg must run under (owner ruling 2026-08-30, Option A). The
                   Applicable/Expected semantics are untouched by it: a row's banked numbers mean
                   exactly what they meant, and the annotation says only HOW the converted host is
                   published and run to produce them. Unannotated rows are the default path and
                   nothing about them moves.

    The same document also carries the EXCLUSION LEDGER (the "Excluded packages" table), read by
    Get-ExclusionLedgerRows. A ledger row's first cell is a PLAIN code span on purpose -- the
    roster row's linked [`pkg`](url) shape is what $RosterRowPattern anchors on, so the two tables
    in one document can never be confused by either parser; the document's own HTML comment
    beneath the ledger states the same rule from the other side.

    Nothing here has side effects; it defines pure functions and returns.

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

# The permanently-inapplicable form of the same annotation (ruled 2026-08-29, from the registry
# row): a package that cannot exist on an OS carries `<goos>: n/a` -- never pending, never
# validated, counted in neither the Linux header's numerator nor its applicable denominator. Same
# both-ends anchoring as the numeric form, for the same prose-immunity reason.
$RosterOsNaPattern =
    [regex]::Escape($RosterSegmentSeparator) +
    '\s*([a-z][a-z0-9]*)\s*:\s*n/a\s*(?=' +
    [regex]::Escape($RosterSegmentSeparator) + '|\||$)'

# ---- the per-row EXECUTION annotation (owner ruling 2026-08-30, Option A) ------------------------
# Some Go tests assert a liveness property Go's compiler provides and the CLR's DEFAULT execution
# config does not: `codegen-liveness` names the class. The tier-0 A/B measured that the class is a
# CONFIG artifact rather than a structural one -- internal/weak's suite validates 4/4 under a Release
# publish with DOTNET_TieredCompilation=0, five consecutive runs -- but the flip is NOT globally
# safe: two residuals (internal/godebug's line attribution, log/slog's pc=0) are TC0-ONLY failures.
# So the ruling is per-row opt-in, and this annotation is how a row opts in.
#
# It is an EXECUTION property, never a platform one. Nothing about Applicable/Expected moves: the
# row's Tests and Disclosed columns still mean what they meant, the per-OS annotations still answer
# for their own OS, and an unannotated row's pipeline leg is byte-for-byte the leg it always was.
# The value names a config, not a flag list -- Get-RosterExecutionArgs owns that mapping, so the
# roster never has to know what the converter's command line looks like.
$RosterExecutionValues = @('release-tc0')

# Same both-ends separator anchoring as the per-OS forms, for the same prose-immunity reason: a
# sentence reading "the execution: it runs Release" is not an annotation and must not read as one.
# The value admits internal hyphens (release-tc0) where an OS key does not.
$RosterExecutionPattern =
    [regex]::Escape($RosterSegmentSeparator) +
    '\s*execution\s*:\s*([a-z][a-z0-9]*(?:-[a-z0-9]+)*)\s*(?=' +
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
    `_paths.ps1` pins $env:GoTargetOS to the HOST's own flavor on every non-Windows host, precisely
    because every L3 csproj defaults the property to `windows` when it is EMPTY. That default is the
    whole rule: the environment variable wins where one is set, and 'windows' is what an unset one
    means -- which is right on Windows and only there.

    2026-09-02: this paragraph used to end "...and on macOS because darwin's corpus does not build
    yet and keeps the status-quo default until its own lane earns one". That wall is CLOSED -- the
    darwin corpus compiles clean, census run 32649840220 at c003d32af, zero errors on osx-x64 and
    osx-arm64 -- so _paths.ps1 pins `darwin` on a macOS host and this function reports it. Darwin's
    remaining gap is the RUN layer (docs/phase4/FINDING-darwin-run-layer.md), which sits downstream
    of the flavor and is not a reason to bind the wrong one.

    The $IsMacOS/$IsLinux fallbacks cover a consumer that dot-sourced this file WITHOUT _paths.ps1;
    both are inert on Windows PowerShell 5.1, where neither variable exists ($null -> falsey).
#>
function Get-SweepTargetGoos {
    if (-not [string]::IsNullOrWhiteSpace($env:GoTargetOS)) {
        return $env:GoTargetOS.Trim().ToLowerInvariant()
    }

    if ($IsMacOS) { return 'darwin' }
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
                Expected   = [int]$match.Groups[2].Value
                Disclosed  = if ($match.Groups[3].Success) { [int]$match.Groups[3].Value } else { 0 }
                Applicable = $true
            }
        }

        # The n/a form, with the numeric form's refusals repeated verbatim: a key this loop admits
        # that the one above refuses would make `windows: n/a` a back door, and a row carrying both
        # `linux: N` and `linux: n/a` holds two answers with no rule for which wins.
        foreach ($match in [regex]::Matches($line, $RosterOsNaPattern)) {
            $key = $match.Groups[1].Value

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
                Expected   = $null
                Disclosed  = $null
                Applicable = $false
            }
        }

        # The execution annotation. Refused by NAME on an unknown value rather than ignored: a row
        # whose author wrote `execution: release` would otherwise run the default path while reading,
        # to that author, as opted in -- which is the silent-config failure this whole annotation
        # exists to make impossible. Two of them is two answers with no rule for which wins.
        $rowExecution = $null
        foreach ($match in [regex]::Matches($line, $RosterExecutionPattern)) {
            $value = $match.Groups[1].Value

            if ($RosterExecutionValues -notcontains $value) {
                throw ("Roster row '$rowPackage' carries an unknown execution annotation " +
                    "'$value'. Known configs: $($RosterExecutionValues -join ', ').")
            }

            if ($null -ne $rowExecution) {
                throw "Roster row '$rowPackage' carries more than one execution annotation."
            }

            $rowExecution = $value
        }

        [void]$rows.Add([PSCustomObject]@{
            Package     = $rowPackage
            Expected    = $rowExpected
            Disclosed   = $rowDisclosed
            Conditional = $rowConditional
            OS          = $rowOs
            Execution   = $rowExecution
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
            Expected   = $Row.OS[$key].Expected
            Disclosed  = $Row.OS[$key].Disclosed
            Source     = $key
            Applicable = $Row.OS[$key].Applicable
        }
    }

    return [PSCustomObject]@{
        Expected   = $Row.Expected
        Disclosed  = $Row.Disclosed
        Source     = 'columns'
        Applicable = $true
    }
}

<#
.SYNOPSIS
    The converter arguments one execution config implies. Pure -- no I/O, no state.
.DESCRIPTION
    The single place that knows what a config NAME means on a command line, so the roster records an
    intent and the sweep spells no flags of its own. An empty/absent config is the default path and
    contributes NOTHING: the invocation an unannotated row produces is character-for-character the
    invocation it produced before this annotation existed, which is the guarantee the ruling rests on.

    `release-tc0` maps to the converter's `-test-config Release` (2026-09-02: generalized from the
    retired `-test-release-tc0` bool into `-test-config Debug|Release` + `-test-tiered`; Release's own
    DEFAULT is untiered, exactly matching this annotation's meaning, so no `-test-tiered` is added
    here -- this mapping is the config's ORIGINAL meaning, not a new one). The honest seam for BOTH
    halves of the config, still: the publish CONFIGURATION is decided inside the converter's own
    `dotnet publish` invocation (publishTestHost, testConversion.go), where Release passes an explicit
    `-p:go2csPath` -- the template's `Condition="'$(go2csPath)'==''"` guard is written to be
    overridden exactly that way, so a Release publish still binds THIS tree rather than the deployed
    `~/go2cs` root. The run half, `DOTNET_TieredCompilation=0` by default at Release, rides the same
    flag (testHostRunEnv), because a Release publish alone does not retire tier-0: a program can start
    at tier-0 and simply never run long enough to be promoted.

    An unknown config throws rather than degrading to the default -- see the parser's refusal above
    for why a silently-ignored config is the failure this design exists to prevent.
.OUTPUTS
    A string[] of converter arguments; empty for the default path.
#>
function Get-RosterExecutionArgs {
    param([string] $Execution)

    if ([string]::IsNullOrWhiteSpace($Execution)) { return @() }

    switch ($Execution) {
        'release-tc0' { return @('-test-config', 'Release') }
        default {
            throw ("Unknown execution config '$Execution'. Known configs: " +
                "$($RosterExecutionValues -join ', ').")
        }
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
      host-limit        a shortfall PROVEN to be a registered block the converted side could not
                        produce on this host, absorbed by the block root's own COMMITTED host-limit
                        disclosure. The third host state (Test-HostLimitDelta); same evidence
                        discipline as the two above -- this takes its answer
      disclosed-moved   an annotated row's matching count agreed and its DISCLOSED count did not --
                        roster maintenance, never host capability, and never absorbed
      unbanked-count    comparison-validated-at-count: a validated run on an OS this row has no
                        expectation for. Neither a pass nor a drift failure
      not-applicable    the row is annotated `<goos>: n/a` -- the package cannot exist on this OS,
                        so there is nothing to measure, now or ever (ruled 2026-08-29). The sweep
                        removes such rows before running them; this answer exists so a caller that
                        classifies anyway gets the truth rather than a count comparison against null
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
        [switch] $CapabilityAbsentAccepted,
        [switch] $HostLimitAccepted
    )

    # Before any count math: an inapplicable expectation has null counts, and comparing against
    # them would classify by accident.
    if ($Expectation.PSObject.Properties['Applicable'] -and -not $Expectation.Applicable) { return 'not-applicable' }

    $disclosedAgrees = ($Expectation.Source -eq 'columns') -or ($GotDisclosed -eq $Expectation.Disclosed)

    if (-not $disclosedAgrees) { return 'disclosed-moved' }
    if ($HostConditionalAccepted) { return 'host-conditional' }
    if ($CapabilityAbsentAccepted) { return 'capability-absent' }
    if ($HostLimitAccepted) { return 'host-limit' }
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

# ---- host-limited verdicts (the THIRD host state, and the SECOND shortfall shape) -----------------
# The rule above owns the shortfall an ABSENT capability produces, and its LAST check refuses, by
# name, every shortfall whose Go side FANNED THE MATRIX OUT -- correctly, on the evidence IT reads:
# the case matrix existed, so the lost verdicts are the converted side's alone. What that rule cannot
# see is the one artifact that changes what such a loss MEANS: a COMMITTED `host-limit` disclosure
# pinning the block root. That class is the project's existing, reviewed vocabulary for exactly this
# shape -- a divergence the converted host's DEPLOYMENT SHAPE structurally cannot close, banked with
# a failure signature and a self-retiring condition rather than waved through -- and the converter's
# own compare oracle already accepts the row under it (matchTerminalStatuses), which is why the
# pipeline reports the very same run as `status: validated, matched: true` while only the sweep's
# COUNT gate refuses.
#
# So a capability-conditional block has THREE host states, not two:
#
#   runner present, host fast enough   the block fans out on both sides and every sub-verdict
#                                      MATCHES -- the roster's banked ceiling (crypto/tls 3643 + 1)
#   runner absent                      the matrix never exists: the block collapses to one verdict on
#                                      BOTH runtimes with NO fan-out, and the two counts fall
#                                      together -- Test-CapabilityAbsentDelta
#   runner present, host too slow      Go fans the matrix out, the converted side dies on the
#                                      runner's own fixed deadline, and the block root becomes a
#                                      DISCLOSED divergence -- this rule (crypto/tls 400 + 2)
#
# THE DISCRIMINATOR IS THE FAN-OUT, NOT THE ROOT'S VERDICT (measured 2026-09-01, i7 coordinator, the
# run this rule was written against). The tempting reading is that state 3 is the Go-pass/C#-fail
# pair and state 2 the agreeing non-pass one; it is NOT, and a rule built that way would refuse the
# very host it exists for. crypto/tls's own manifest documents both arms and the sweep measured the
# second: `TestBogoSuite` go='fail' C#='fail', 3,242 rows withdrawn, root disclosed `host-limit`,
# status validated. Go's oracle fans out all 3,242 cases in under a minute and its ROOT still fails
# on a handful of them; the converted side dies at the 600 s wall with the pinned signature; and the
# compare oracle admits either arm (hostConditionalFailureMatches takes the primary signature OR the
# host-conditional one). So the two rules partition the space on the WITHDRAWALS -- that rule refuses
# any, this one requires exactly the block's own -- and no run can be read both ways.
#
# The third state is NOT a weaker second: it carries strictly MORE evidence. A capability-absent
# collapse has no fan-out to point at, so its discriminator is the ABSENCE of withdrawn rows; this
# shape's discriminator is their PRESENCE, name for name. The converter withdraws every Go-side row
# beneath a signature-matched disclosed root and publishes the list, so the record itself ENUMERATES
# the lost verdicts -- and this rule requires that enumeration to BE the block's banked sub-verdicts
# exactly, in both directions, rather than merely to count to its size.
#
# What is NOT host-limited, and must stay red: any shortfall the committed manifest does not pin as
# `host-limit`; any shortfall whose withdrawn set is not exactly the block's banked sub-verdicts (a
# rogue loss cancelling against a rogue withdrawal is the arithmetic this refuses to be fooled by);
# any run whose converted side did not FAIL on the block root; a root that SKIPPED on the Go side (Go
# has no capability-absent skip branch here, and a skipping root cannot have fanned anything out);
# any surviving block subtest; any banked verdict outside the block going missing; any live verdict
# the row does not account for; and any disclosed movement other than the block root itself joining.
# The MISSING PIN is the load-bearing refusal -- this rule can only ever absorb what a reviewed,
# committed, self-retiring disclosure already describes, so it generalizes to no other shortfall and
# to no unpinned package. And note what the sweep never even reaches: a converted side that failed
# for some OTHER reason is a MISMATCH inside the converter's own oracle, so no "Validated N" line is
# printed and the row fails as FAIL, not COUNT. The signature pin does that work upstream of here.
$HostLimitDisclosureClass = 'host-limit'

# The Go-side root verdicts this rule admits, and the arms crypto/tls's manifest documents: `pass`
# (the primary pinned divergence -- Go clean, converted side over the wall) and `fail` (Go's own
# oracle red on a handful of the cases it fanned out). `skip` is absent ON PURPOSE, and so is an
# absent root: neither can coexist with the fan-out this rule demands.
$HostLimitGoRootVerdicts = @('pass', 'fail')

function Test-HostLimitDelta {
    param(
        [int] $Expected,           # banked matching-verdict count (roster column 2, the CEILING here)
        [int] $Disclosed,          # banked disclosed count (roster column 3)
        [PSCustomObject] $Block,   # @{ Test = <top-level name>; BlockSize = <int> }
        [int] $Got,
        $Comparison,
        [string[]] $BankedNames,
        $Pin                       # the COMMITTED manifest entry for $Block.Test: @{ Class; Signature }
    )

    function New-HostLimitResult([bool] $accepted, [string] $reason) {
        return [PSCustomObject]@{ Accepted = $accepted; Reason = $reason }
    }

    # Ordinal, for the same case-sensitivity reason ConvertFrom-ComparisonRecord states -- and a SET
    # rather than an array because this rule compares two three-thousand-name populations to one
    # another: the sibling's `-notcontains` scans are quadratic and would spend minutes here.
    function New-OrdinalSet([string[]] $names) {
        $set = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
        foreach ($name in $names) { [void]$set.Add($name) }
        return , $set
    }

    $shortfall = $Expected - $Got
    if ($shortfall -ne $Block.BlockSize) {
        return New-HostLimitResult $false "shortfall $shortfall does not match $($Block.Test)'s registered block size $($Block.BlockSize) -- a lost verdict outside the named block is never host-limited"
    }
    if ($null -eq $Comparison -or $null -eq $Comparison.go -or $null -eq $Comparison.csharp) {
        return New-HostLimitResult $false 'comparison record carries no per-test verdict maps'
    }

    # THE ADMISSION GATE. Everything below proves WHICH verdicts were lost; this proves the loss was
    # already disclosed, committed and reviewed. Without it the rule would be a general "accept a
    # block-sized shortfall", which is the change this design exists to not be.
    if ($null -eq $Pin) {
        return New-HostLimitResult $false "the committed disclosure manifest does not pin $($Block.Test) -- an undisclosed shortfall is a divergence, never a host state"
    }
    if ($Pin.Class -ne $HostLimitDisclosureClass) {
        return New-HostLimitResult $false "the committed manifest pins $($Block.Test) as '$($Pin.Class)', not '$HostLimitDisclosureClass' -- only that class names a deployment-shape ceiling this rule may absorb"
    }

    if ($BankedNames.Count -ne ($Expected + $Disclosed)) {
        return New-HostLimitResult $false "committed proof page lists $($BankedNames.Count) verdicts where the roster banks $Expected matched + $Disclosed disclosed -- page and table disagree"
    }

    $blockPrefix = "$($Block.Test)/"
    $blockNames = @($BankedNames | Where-Object { $_ -eq $Block.Test -or $_.StartsWith($blockPrefix) })
    if ($blockNames.Count -ne $Block.BlockSize) {
        return New-HostLimitResult $false "the committed proof page names $($blockNames.Count) verdicts under $($Block.Test), not the registered $($Block.BlockSize) -- re-derive the block size before trusting this row"
    }

    $blockSet = New-OrdinalSet $blockNames
    $goMap = $Comparison.go
    $csMap = $Comparison.csharp
    $liveNames = @($goMap.Keys)
    $liveSet = New-OrdinalSet $liveNames

    # Every banked name OUTSIDE the block must still be present live -- the mechanism absorbs the
    # named block, nothing else.
    $expectedOutside = @($BankedNames | Where-Object { -not $blockSet.Contains($_) })
    $missingOutside = @($expectedOutside | Where-Object { -not $liveSet.Contains($_) })
    if ($missingOutside.Count -gt 0) {
        return New-HostLimitResult $false "banked verdicts outside the block missing from this run: $($missingOutside -join ', ')"
    }

    # No block subtest may survive in the COMPARED set (each was withdrawn beneath the disclosed
    # root), and the root itself must be there to carry the disclosure.
    $liveBlockNames = @($liveNames | Where-Object { $_ -eq $Block.Test -or $_.StartsWith($blockPrefix) })
    if (@($liveBlockNames | Where-Object { $_ -ne $Block.Test }).Count -gt 0) {
        return New-HostLimitResult $false "subtests under $($Block.Test) survive in this run's compared set -- the block did not collapse, re-diagnose rather than absorb"
    }
    if ($liveBlockNames -notcontains $Block.Test) {
        return New-HostLimitResult $false "$($Block.Test) itself is missing from this run -- a host-limited block must still report its one collapsed root"
    }

    # The root's verdict pair. The CONVERTED side must be the half that failed -- that is the whole
    # claim of a host limit -- and the Go root must be one of the two arms the manifest documents.
    # Note this is NOT the discriminator against the capability-absent shape; the fan-out below is.
    $goVerdict = $goMap[$Block.Test]
    $csVerdict = if (-not $csMap.ContainsKey($Block.Test)) { 'absent' } else { $csMap[$Block.Test] }
    if ($csVerdict -ne 'fail') {
        return New-HostLimitResult $false "$($Block.Test): go '$goVerdict' vs C# '$csVerdict' -- a host-limited block's converted side FAILS on the pinned signature; anything else is not this shape"
    }
    if ($HostLimitGoRootVerdicts -notcontains $goVerdict) {
        return New-HostLimitResult $false "$($Block.Test): go '$goVerdict' -- a host-limited block's Go root is '$($HostLimitGoRootVerdicts -join "' or '")'; a skipped or absent root cannot have fanned out the matrix this rule requires"
    }

    # THE FAN-OUT, NAME FOR NAME -- the binding property AND the discriminator. The shortfall count
    # above says only "the right SIZE went missing"; this says the missing rows are the block's own
    # banked sub-verdicts and nothing else, in both directions. A rogue loss cancelling against a
    # rogue withdrawal is the exact arithmetic this refuses to be fooled by. It is also the evidence
    # the capability-absent rule cannot have -- there the matrix never existed, and that rule refuses
    # on ANY withdrawal under the block -- so requiring exactly the block's own partitions the two
    # rules cleanly whatever the roots report. Withdrawals OUTSIDE the block are left alone, as they
    # are there: one would move the shortfall and be caught by the first check.
    $withdrawn = @()
    if ($null -ne $Comparison.withdrawn) { $withdrawn = @($Comparison.withdrawn) }
    $withdrawnUnderBlock = @($withdrawn | Where-Object { $_ -eq $Block.Test -or $_.StartsWith($blockPrefix) })
    $withdrawnSet = New-OrdinalSet $withdrawnUnderBlock
    $expectedWithdrawn = @($blockNames | Where-Object { $_ -ne $Block.Test })
    $expectedWithdrawnSet = New-OrdinalSet $expectedWithdrawn

    $notWithdrawn = @($expectedWithdrawn | Where-Object { -not $withdrawnSet.Contains($_) })
    $unexpectedWithdrawn = @($withdrawnUnderBlock | Where-Object { -not $expectedWithdrawnSet.Contains($_) })
    if ($notWithdrawn.Count -gt 0 -or $unexpectedWithdrawn.Count -gt 0) {
        $detail = @()
        if ($notWithdrawn.Count -gt 0) { $detail += "$($notWithdrawn.Count) banked sub-verdict(s) were NOT withdrawn (first: $($notWithdrawn[0]))" }
        if ($unexpectedWithdrawn.Count -gt 0) { $detail += "$($unexpectedWithdrawn.Count) withdrawn name(s) the proof page does not bank (first: $($unexpectedWithdrawn[0]))" }
        return New-HostLimitResult $false ("the withdrawn Go-side rows are not exactly $($Block.Test)'s banked sub-verdicts -- " +
            ($detail -join '; ') + ' -- the shortfall must BE the block and nothing else')
    }

    # The disclosed accounting: the banked disclosures plus the block root, which the compare oracle
    # accounts as DISCLOSED (never as an agreed match) in this shape. Anything else is a disclosure
    # that moved for an unrelated reason, which is what this family of checks exists to catch.
    $liveDisclosedEntries = @()
    if ($null -ne $Comparison.disclosed) { $liveDisclosedEntries = @($Comparison.disclosed) }
    $expectedDisclosed = $Disclosed + 1
    if ($liveDisclosedEntries.Count -ne $expectedDisclosed) {
        return New-HostLimitResult $false "disclosed count moved ($($liveDisclosedEntries.Count) live vs $expectedDisclosed expected -- the $Disclosed banked plus $($Block.Test) itself) -- not a host-limited shape"
    }

    # A disclosure entry reads "<TestName> (<class>): <reason>", so the class the compare oracle
    # ACTUALLY applied to this run is readable here -- and is cross-checked against the committed pin
    # rather than assumed from it. Record and pin disagreeing means one of them is describing a
    # different divergence than the other.
    $rootEntry = @($liveDisclosedEntries | Where-Object { ($_ -split ' \(', 2)[0] -eq $Block.Test })
    if ($rootEntry.Count -eq 0) {
        return New-HostLimitResult $false "$($Block.Test) is not among this run's disclosures -- the extra disclosure is some other row, so this is not the collapse it looks like"
    }
    $liveClass = if ($rootEntry[0] -match ('^' + [regex]::Escape($Block.Test) + '\s\(([^)]+)\):')) { $Matches[1] } else { '' }
    if ($liveClass -ne $Pin.Class) {
        return New-HostLimitResult $false "$($Block.Test) is disclosed as '$liveClass' in this run but pinned as '$($Pin.Class)' in the committed manifest -- record and pin disagree"
    }

    # Nothing live may go unaccounted for: the outside names plus the one collapsed root, exactly.
    $accountedFor = New-OrdinalSet (@($expectedOutside) + @($Block.Test))
    $unaccounted = @($liveNames | Where-Object { -not $accountedFor.Contains($_) })
    if ($unaccounted.Count -gt 0) {
        return New-HostLimitResult $false "live verdicts this row does not account for: $($unaccounted -join ', ')"
    }

    return New-HostLimitResult $true $null
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

    $text = [System.IO.File]::ReadAllText($Path)

    # Two readers, one shape, because neither type exists on both editions: System.Web.Extensions is
    # .NET Framework only (it cannot load under PowerShell 7 -- every Linux host, and any Windows host
    # driving these instruments with pwsh rather than 5.1), and System.Text.Json is not present on 5.1.
    # Both must yield ORDINAL dictionaries for the case-sensitivity reason above. `ConvertFrom-Json
    # -AsHashtable` is deliberately NOT used on Core: it happens to preserve case-only pairs on 7.5,
    # but that is inherited behaviour rather than a stated contract, and the whole point of the
    # explicit re-copy below is that the ordinal choice is deliberate.
    $goMap = $null
    $csMap = $null
    $withdrawn = $null
    $disclosed = $null

    if ($PSVersionTable.PSEdition -eq 'Desktop') {
        if (-not $script:comparisonSerializer) {
            Add-Type -AssemblyName System.Web.Extensions
            $script:comparisonSerializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
            $script:comparisonSerializer.MaxJsonLength = [int]::MaxValue
        }

        $raw = $script:comparisonSerializer.DeserializeObject($text)
        if ($null -eq $raw) { throw "comparison record at $Path deserialized to nothing" }

        $toOrdinalMap = {
            param($member)
            if (-not $raw.ContainsKey($member)) { return $null }
            $map = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::Ordinal)
            foreach ($entry in $raw[$member].GetEnumerator()) { $map.Add([string]$entry.Key, [string]$entry.Value) }
            return , $map
        }

        $goMap = & $toOrdinalMap 'go'
        $csMap = & $toOrdinalMap 'csharp'
        $withdrawn = if ($raw.ContainsKey('withdrawn')) { @($raw['withdrawn']) } else { $null }
        $disclosed = if ($raw.ContainsKey('disclosed')) { @($raw['disclosed']) } else { $null }
    }
    else {
        $document = $null
        try {
            $document = [System.Text.Json.JsonDocument]::Parse($text)
            if ($null -eq $document) { throw "comparison record at $Path deserialized to nothing" }
            $root = $document.RootElement

            $toOrdinalMap = {
                param($member)
                $element = [System.Text.Json.JsonElement]::new()
                if (-not $root.TryGetProperty($member, [ref] $element)) { return $null }
                $map = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::Ordinal)
                foreach ($property in $element.EnumerateObject()) { $map.Add($property.Name, $property.Value.GetString()) }
                return , $map
            }

            $toArray = {
                param($member)
                $element = [System.Text.Json.JsonElement]::new()
                if (-not $root.TryGetProperty($member, [ref] $element)) { return $null }
                return , @($element.EnumerateArray() | ForEach-Object { $_.GetString() })
            }

            $goMap = & $toOrdinalMap 'go'
            $csMap = & $toOrdinalMap 'csharp'
            $withdrawn = & $toArray 'withdrawn'
            $disclosed = & $toArray 'disclosed'
        }
        finally {
            if ($null -ne $document) { $document.Dispose() }
        }
    }

    return [PSCustomObject]@{
        go        = $goMap
        csharp    = $csMap
        withdrawn = $withdrawn
        disclosed = $disclosed
    }
}
