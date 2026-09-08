<#
.SYNOPSIS
    The H5 DELETION PASS: classify, and optionally delete, the files a SEEDED reconvert root still
    holds because the converter has STOPPED emitting them at the target release.

.DESCRIPTION
    THE HOLE THIS CLOSES. The corpus reconvert ritual (CLAUDE.md, "Corpus mechanics") seeds a scratch
    root from src\core before converting, and that seeding is non-negotiable -- an unseeded root gives
    the [module: GoManualConversion] detector nothing to detect and every hand-owned file is clobbered.
    But the seeding buys that protection at a price the ritual never paid: A SEEDED ROOT CANNOT REVEAL
    A FILE THE CONVERTER HAS STOPPED EMITTING. The seed put it there; the conversion simply did not
    rewrite it; the overlay copies it back; and nothing anywhere performs a deletion.

    At an ordinary regen that costs nothing, because the emitted set does not move. AT A RELEASE HOP
    IT IS FATAL. Lane R's 1.24.13 rehearsal (docs/phase4/REHEARSAL-h5-go124.md, section 3) measured 25
    such files, and the FIRST build died on the smallest of them in 116 seconds having measured
    nothing: internal/goexperiment/exp_aliastypeparams_off.cs (seeded, 1.23.12) and
    exp_aliastypeparams_on.cs (emitted, 1.24.13) BOTH declare AliasTypeParams, the package csproj
    globs *.cs so both compile, and the result is CS0102 x2 in a leaf that essentially the whole
    corpus depends on. Three GOEXPERIMENT flips default ON at 1.24.13 (aliastypeparams, swissmap,
    synchashtriemap) plus the FIPS reorganization move or deselect the other 24.

    WHAT A DELETION CANDIDATE IS -- the question this instrument got WRONG on its first real run, and
    the reason the NOT-A-CONVERSION-TARGET class exists. The pass as first landed asked exactly one
    question of every seeded file: "does Go still select this file's principal at the target?" That
    question is only MEANINGFUL for a file the converter EMITS. R's first dry run against a
    three-target scratch (mailbox 1f5e8f276, dry run, NOT applied) produced a 205-row delete set of
    which 117 rows were src\core\golib\*.cs (116) and src\core\go2cs\Symbols.cs (1) -- THE
    HAND-WRITTEN RUNTIME AND THE SYMBOLS SHARED PROJECT -- classified DELETE-ABSENT because
    "package not in std at target" is TRUE and IRRELEVANT for a directory that was never a Go package
    in the first place. The marker-based PROTECTED arm cannot see them: golib correctly carries no
    [module: GoManualConversion] marker, because nothing ever CONVERTS into golib and there is no
    generated body for a marker to displace. THE ONE DIRECTORY THAT NEEDS NO MARKER IS THE ONE THE
    MARKER GUARD DOES NOT PROTECT.

    So the candidate test is now POSITIVE and comes FIRST: a file is a deletion candidate only if it
    belongs to a package the converter actually emits, i.e. its resolved import path is in
    `go list std` AT THE SOURCE RELEASE (the outgoing corpus's own release, -SourceGoRoot) and is not
    skip-listed by the converter's own isNonConvertedStdLibPackage. Everything else under core\ --
    golib\, go2cs\, the skip-listed hand-owned packages (unsafe, testing), and any .cs with no package
    directory at all -- is NOT-A-CONVERSION-TARGET and can never reach a DELETE row. Asking the SOURCE
    rather than the target is what makes a REMOVED package still a candidate: internal/weak is in std
    at 1.23.12 and gone at 1.24.13, which is precisely the DELETE-ABSENT the pass exists to find.

    WHY MODIFICATION TIME ALONE CANNOT DECIDE A DELETION -- the load-bearing caveat. The converter's
    writePackageFile path goes through needToWriteFile (projectFileWriter.go), which SKIPS a write
    whose bytes are identical. So a file whose emission did not CHANGE between the two releases keeps
    its seed timestamp and reads SEEDED, exactly like a file that stopped being emitted. R measured
    1292 seeded-not-rewritten production .cs against 25 real deletions: the seeded set is ~50x the
    deletion set, and a timestamp-only deletion pass would destroy the corpus. The timestamp answers
    only "is this file a CANDIDATE"; GO ITSELF answers "should it exist", via

        go list -f '{{.GoFiles}} {{.CgoFiles}}' <importpath>

    run against the TARGET GOROOT with the corpus's own emission state (CGO_ENABLED=0) and the file's
    own flavour (GOOS). A file whose Go principal is still SELECTED there is kept, whatever its
    timestamp says.

    HOW EMITTED-VS-SEEDED IS DECIDED, and how it differs from the platform census. platformCensus.go
    stamps every seeded file to a fixed sentinel instant (censusSeedSentinel, 2000-01-01Z) and then
    tests emitted := !ModTime.Equal(sentinel) -- an exact, content-independent equality it can afford
    because it did the stamping. This instrument runs AFTER somebody else's reconvert and did not
    stamp anything, so it takes the reconvert's START stamp and mirrors the same rule as a threshold:
    a file modified BEFORE the sentinel was seeded, one modified at or after it was emitted. Pass the
    stamp as -SentinelTime, or as -Sentinel <path> naming a file created immediately before the
    conversion started (whose mtime is then the stamp). Either way the instrument REFUSES to run
    without one -- an absent sentinel would make every file look seeded.

    CLASSES, and what each means. Every candidate lands in exactly one, and every count is printed
    whether or not it is zero (a class that prints only when non-empty cannot be told from a class
    whose predicate never fired):

        NOT-A-CONVERSION-TARGET
                            the converter does not emit into this file's directory at all, so no Go
                            question about it is meaningful. Three disjoint reasons, each printed:
                            a HAND-WRITTEN repository root (golib\, go2cs\); a std package the
                            converter SKIP-LISTS (isNonConvertedStdLibPackage: unsafe, builtin,
                            testing, cmd and cmd/...); or an import path that is not in std at the
                            SOURCE release -- which includes a .cs sitting directly in core\ with no
                            package directory at all. NEVER deleted. Tested FIRST, before the marker
                            scan and before any target lookup.
        PROTECTED           carries the line-anchored [module: GoManualConversion] marker, or is an
                            *_impl.cs companion, INSIDE a package the converter does emit. NEVER
                            deleted, whatever Go says about its principal. A hand-own is the corpus's
                            own code; it is a reconciliation item for a human (R's runtime2.cs /
                            mfinal.cs), never a deletion.
        KEEP-SELECTED       Go still selects the principal at the target for this flavour. This is
                            the dominant class by construction (the needToWriteFile caveat above) and
                            it is the instrument's own negative control: a pass that cannot answer
                            "keep" is a pass that would delete the corpus.
        DELETE-ABSENT       the principal is gone at the target -- the file was removed, or its whole
                            package was (H3 removals: internal/weak, runtime/internal/sys, ...).
        DELETE-DESELECTED   the principal still EXISTS on disk at the target but Go does not select it
                            for this flavour -- a build-tag or GOEXPERIMENT flip. This is the class
                            that killed R's build: exp_aliastypeparams_off.go is present at 1.24.13
                            and simply not chosen.
        UNRESOLVED          no Go principal is derivable INSIDE a package the converter emits --
                            generated metadata (package_info.cs, package_init.cs) and anything else
                            whose stem does not map to a .go file name. NEVER deleted, always listed,
                            and the run EXITS NON-ZERO so a human reads them. R's 25 contains exactly
                            one such row (crypto/ecdh/package_init.cs): the class is real, it is not
                            automatable from a file name, and silently dropping it would be the
                            silent-subtraction failure this repository has already paid for.

    Files the conversion emitted this run are not candidates at all. Neither are the test-host
    artifacts (package_test_info.cs, go2cs_test_host.cs) or any *_test.cs / *.cs.auto / *.g.cs: the
    package csproj <Compile Remove>s them, so a stale one cannot produce the CS0102 this pass exists to
    prevent, and admitting them would bury the real rows under hundreds of UNRESOLVED lines (R
    subtracted 384 test-host artifacts from the same arithmetic). They are counted, not listed.

    EVERY REFUSAL RUNS BEFORE THE DELETION LOOP. The first landing put the UNRESOLVED check AFTER the
    Remove-Item loop, so a run that exited 2 had already deleted -- a report wearing a refusal's exit
    code. Exit 2 now means, without exception, that NOTHING WAS REMOVED: the UNRESOLVED check, the
    delete-set decomposition and the trespass assertion (no delete row may sit under a hand-written
    root or a skip-listed package directory -- a PATH test, derived independently of the `go list`
    that classified it) all run first, and the loop itself re-checks every row it is about to delete.

.PARAMETER Root
    The seeded-and-reconverted scratch src root -- the directory holding core\. NOT the repository's
    own src\ (this instrument deletes files; point it at a scratch root).

.PARAMETER GoRoot
    The TARGET release's GOROOT -- the release the reconvert ran against, and the one whose file
    selection decides every DELETE row.

.PARAMETER ExpectGo
    The release -GoRoot must report, e.g. go1.24.13. The run REFUSES before printing any table when
    `go version` under -GoRoot says anything else. A deletion pass aimed at the wrong release deletes
    the wrong files, so this is a refusal and not a warning.

.PARAMETER SourceGoRoot
    The SOURCE release's GOROOT -- the release the OUTGOING corpus was converted from. It answers the
    only question that makes a file a candidate at all: is this directory something the converter
    EMITS? Mandatory, because a pass that cannot ask it offers the hand-written runtime for deletion
    (see the NOT-A-CONVERSION-TARGET note above). Named -SourceGoRoot against -GoRoot rather than
    renaming the pair -From/-To (handown-census.ps1's spelling) so the target parameter that already
    ships keeps its name.

.PARAMETER ExpectSourceGo
    The release -SourceGoRoot must report. DERIVED when omitted, from <GoStdLibVersion> in
    src\version.props -- the property of record for the release the committed corpus was converted
    from -- so the expectation cannot go stale independently of the corpus. Pass it explicitly when
    the pin has already moved ahead of the staging root you are classifying.

.PARAMETER SentinelTime
    The reconvert's start instant. Files modified before it were seeded; files modified at or after it
    were emitted by the run. Mutually exclusive with -Sentinel.

.PARAMETER Sentinel
    A file whose modification time IS the reconvert's start instant (create it immediately before the
    conversion). Mutually exclusive with -SentinelTime. A missing sentinel file is a refusal.

.PARAMETER Goos
    The GOOS the reconvert targeted, for files that sit FLAT in a package directory. Files inside an
    L3 per-GOOS folder (core\<pkg>\{windows,linux,darwin}\) are asked under THAT folder's flavour
    regardless. Defaults to this host's flavour.

.PARAMETER Goarch
    The GOARCH to ask Go under. Defaults to this host's architecture. Stated explicitly rather than
    inherited so two runs on two boxes cannot disagree silently.

.PARAMETER Apply
    Perform the deletions. WITHOUT it this is a DRY RUN: it prints the table and the counts and
    deletes nothing.

.OUTPUTS
    Exit 0  -- classified, no UNRESOLVED rows (and, with -Apply, the DELETE rows are gone).
    Exit 2  -- classified, and something needs a human: UNRESOLVED rows, or the trespass assertion
               fired. NOTHING WAS DELETED -- every check that can produce this code runs before the
               deletion loop.
    Exit 3  -- refused before classifying anything (bad root, missing/ambiguous sentinel, wrong
               release, unusable toolchain), or a deletion aborted part-way (which says so, and says
               how many files had already been removed).

    Explicit exit codes rather than `throw`, because the exit CODE is the property a caller gates on
    and a throw leaves it to the host (CLAUDE.md, false-green route #6).

.EXAMPLE
    # Dry run, the normal first invocation.
    .\reconvert-deletions.ps1 -Root D:\scratch\h5\src -GoRoot C:\sdk\go1.24.13 `
                              -ExpectGo go1.24.13 -SourceGoRoot C:\sdk\go1.23.12 `
                              -Sentinel D:\scratch\h5\run.stamp

.EXAMPLE
    # Same classification, then delete exactly the DELETE-* rows.
    .\reconvert-deletions.ps1 -Root D:\scratch\h5\src -GoRoot C:\sdk\go1.24.13 `
                              -ExpectGo go1.24.13 -SourceGoRoot C:\sdk\go1.23.12 `
                              -Sentinel D:\scratch\h5\run.stamp -Apply

.NOTES
    Requires PowerShell 5.1 (Windows) or PowerShell 7+ (any platform). Deliberately ASCII-only: a
    BOM-less .ps1 carrying a non-ASCII literal is re-decoded by 5.1's parser under the system codepage
    and the literal silently mojibakes at PARSE time (CLAUDE.md). Keeping the source ASCII removes the
    trap rather than papering it with a BOM.

    Written for docs/GoCorpusMigration.md H5. See docs/phase4/REHEARSAL-h5-go124.md section 3 for the
    measurement that motivated it.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]   $Root,
    [Parameter(Mandatory = $true)][string]   $GoRoot,
    [Parameter(Mandatory = $true)][string]   $ExpectGo,
    [Parameter(Mandatory = $true)][string]   $SourceGoRoot,
    [string]                                 $ExpectSourceGo,
    [datetime]                               $SentinelTime,
    [string]                                 $Sentinel,
    [string]                                 $Goos,
    [string]                                 $Goarch,
    [switch]                                 $Apply
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_paths.ps1')

# ---------------------------------------------------------------------------------------------
# Refusals. Every one of these runs BEFORE a single file is classified, so a refused run cannot be
# mistaken for a clean one: it prints no table at all.
# ---------------------------------------------------------------------------------------------

function Deny {
    param([Parameter(Mandatory = $true)][string] $Message)

    Write-Host ''
    Write-Host "REFUSED: $Message" -ForegroundColor Red
    Write-Host 'Nothing was classified and nothing was deleted.'
    exit 3
}

# Post-classification refusal. Distinct from Deny because the two make DIFFERENT promises: Deny says
# nothing was read, this says nothing was DELETED. Both are checked before the Remove-Item loop.
function Stop-ForReview {
    param([Parameter(Mandatory = $true)][string] $Message)

    Write-Host ''
    Write-Host "STOPPED: $Message" -ForegroundColor Yellow
    Write-Host 'NOTHING WAS DELETED. Dispose of the rows above, then re-run.'
    exit 2
}

if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
    Deny "-Root does not exist or is not a directory: $Root"
}

$RootFull = (Resolve-Path -LiteralPath $Root).Path
$CoreDir  = Join-Path $RootFull 'core'

if (-not (Test-Path -LiteralPath $CoreDir -PathType Container)) {
    Deny "-Root has no core\ directory: $CoreDir -- pass the scratch SRC root (the one holding core\), not the core dir itself"
}

# The repository's own corpus is never a deletion target. This instrument exists to run against a
# throwaway reconvert root; pointing it at src\core would delete tracked files that a seeded root's
# arithmetic says are stale, on a tree where they are not.
$RepoCore = Join-Path $SrcRoot 'core'

if ($CoreDir.TrimEnd('\', '/') -ieq $RepoCore.TrimEnd('\', '/')) {
    Deny "-Root resolves to the REPOSITORY corpus ($RepoCore). This pass runs against a scratch reconvert root only."
}

# Exactly one sentinel form. Both, or neither, is ambiguous -- and an absent sentinel would make
# every file on disk look seeded, i.e. would offer the whole corpus for deletion.
$haveTime = $PSBoundParameters.ContainsKey('SentinelTime')
$haveFile = -not [string]::IsNullOrWhiteSpace($Sentinel)

if ($haveTime -and $haveFile) {
    Deny 'pass exactly one of -SentinelTime or -Sentinel, not both'
}

if (-not $haveTime -and -not $haveFile) {
    Deny 'pass -SentinelTime <datetime> or -Sentinel <path> -- the reconvert start stamp. Without it every file reads SEEDED.'
}

if ($haveFile) {
    if (-not (Test-Path -LiteralPath $Sentinel -PathType Leaf)) {
        Deny "-Sentinel file is missing: $Sentinel"
    }

    $SentinelStamp = (Get-Item -LiteralPath $Sentinel).LastWriteTimeUtc
}
else {
    $SentinelStamp = $SentinelTime.ToUniversalTime()
}

# The SOURCE release expectation, DERIVED rather than restated. <GoStdLibVersion> in version.props is
# the property of record for the release the committed corpus was converted from, and the reconvert
# being classified started from THAT corpus -- so deriving it here means the expectation cannot drift
# from the tree independently. Comments are stripped first because version.props' own prose names the
# element while explaining it (the _paths.ps1 $NetVersion precedent, same trap, same remedy). There is
# deliberately no fallback: an instrument that cannot know the source release must say so.
if ([string]::IsNullOrWhiteSpace($ExpectSourceGo)) {
    $VersionProps = Join-Path $SrcRoot 'version.props'

    if (-not (Test-Path -LiteralPath $VersionProps -PathType Leaf)) {
        Deny "cannot derive -ExpectSourceGo: the corpus release's property of record is missing at $VersionProps -- pass -ExpectSourceGo explicitly"
    }

    $stdLibVersionMatch = [regex]::Match(
        ([System.IO.File]::ReadAllText($VersionProps) -replace '(?s)<!--.*?-->', ''),
        '<GoStdLibVersion(?:\s[^>]*)?>\s*([^<\s]+)\s*</GoStdLibVersion>')

    if (-not $stdLibVersionMatch.Success) {
        Deny "cannot derive -ExpectSourceGo from $VersionProps -- expected a <GoStdLibVersion>...</GoStdLibVersion> element. Pass -ExpectSourceGo explicitly."
    }

    $ExpectSourceGo       = 'go' + $stdLibVersionMatch.Groups[1].Value
    $ExpectSourceGoOrigin = "derived from <GoStdLibVersion> in $VersionProps"
}
else {
    $ExpectSourceGoOrigin = 'passed as -ExpectSourceGo'
}

if ([string]::IsNullOrWhiteSpace($Goos))   { $Goos   = $HostGoos }
if ([string]::IsNullOrWhiteSpace($Goarch)) { $Goarch = $HostGoarch }

if ([string]::IsNullOrWhiteSpace($Goos)) {
    Deny 'cannot derive the target GOOS on this host -- pass -Goos explicitly'
}

# The environment every `go` child runs under. GOROOT and GOTOOLCHAIN=local pin the release (an
# `auto` toolchain would silently switch and answer about a release nobody named); CGO_ENABLED=0 is
# the corpus's own emission state, and it CHANGES the selected file set for cgo-conditional packages;
# GOWORK=off and an empty GOFLAGS stop an ambient workspace or flag from moving the answer.
$GoEnvBase = @{
    'GOTOOLCHAIN'  = 'local'
    'CGO_ENABLED'  = '0'
    'GOWORK'       = 'off'
    'GOFLAGS'      = ''
    'GO111MODULE'  = ''
}

# A toolchain is a VALUE here rather than a pair of script-scope variables, because there are now two
# of them (source and target) and every `go` child must be unambiguous about which one answered. A
# bare `go` off PATH is never used: it resolves to whatever the ambient release happens to be and
# would answer the file-selection question about the wrong corpus.
function New-Toolchain {
    param(
        [Parameter(Mandatory = $true)][string] $GoRootPath,
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $ParameterName
    )

    if (-not (Test-Path -LiteralPath $GoRootPath -PathType Container)) {
        Deny "$Label GOROOT does not exist or is not a directory: $GoRootPath (pass $ParameterName)"
    }

    $full = (Resolve-Path -LiteralPath $GoRootPath).Path
    $exe  = Join-Path $full "bin/go$ExeSuffix"

    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
        Deny "no Go toolchain at $exe (pass $ParameterName <the $Label release's GOROOT>)"
    }

    $srcDir = Join-Path $full 'src'

    if (-not (Test-Path -LiteralPath $srcDir -PathType Container)) {
        Deny "$Label GOROOT has no src\ directory: $srcDir"
    }

    return [pscustomobject]@{ Label = $Label; Root = $full; Exe = $exe; SrcDir = $srcDir }
}

$TargetGo = New-Toolchain -GoRootPath $GoRoot       -Label 'target' -ParameterName '-GoRoot'
$SourceGo = New-Toolchain -GoRootPath $SourceGoRoot -Label 'source' -ParameterName '-SourceGoRoot'

function Invoke-Go {
    param(
        [Parameter(Mandatory = $true)][object]   $Toolchain,
        [Parameter(Mandatory = $true)][string[]] $Arguments,
        [Parameter(Mandatory = $true)][string]   $ForGoos
    )

    $saved = @{}
    $vars  = @{}

    foreach ($key in $GoEnvBase.Keys) { $vars[$key] = $GoEnvBase[$key] }

    $vars['GOROOT'] = $Toolchain.Root
    $vars['GOOS']   = $ForGoos
    $vars['GOARCH'] = $Goarch

    foreach ($key in $vars.Keys) {
        $saved[$key] = [System.Environment]::GetEnvironmentVariable($key)
        [System.Environment]::SetEnvironmentVariable($key, $vars[$key])
    }

    $previousLocation = (Get-Location).Path

    # A FAILING `go list` is not an error here -- it is the EVIDENCE that decides DELETE-ABSENT and
    # that tells an L3 per-GOOS folder from a real package whose leaf is a GOOS name. Under the
    # script's `Stop` preference a native command's stderr line becomes a terminating
    # NativeCommandError (the r41 trap CLAUDE.md documents for the converter's own WARNINGs), so the
    # preference is scoped to `Continue` across the invocation and the EXIT CODE is read instead.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    try {
        # From the toolchain's own GOROOT\src so a std import path resolves with no module context
        # of the caller's.
        Set-Location -LiteralPath $Toolchain.SrcDir

        $exe    = $Toolchain.Exe
        $output = & $exe @Arguments 2>&1
        # Captured BEFORE anything else touches $LASTEXITCODE (CLAUDE.md: a pipe reports the pipe's).
        $code   = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference

        Set-Location -LiteralPath $previousLocation

        foreach ($key in $saved.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $saved[$key])
        }
    }

    return [pscustomobject]@{
        ExitCode = $code
        Text     = ($output | Out-String)
    }
}

# The release gates, one per toolchain. PRINTING a pin is not CHECKING it (CLAUDE.md); this compares
# and refuses. Both are gated: a deletion pass whose SOURCE release is wrong misjudges which
# directories the converter emits into, which is the class of error the NOT-A-CONVERSION-TARGET arm
# exists to prevent.
function Assert-Release {
    param(
        [Parameter(Mandatory = $true)][object] $Toolchain,
        [Parameter(Mandatory = $true)][string] $Expected
    )

    $probe = Invoke-Go -Toolchain $Toolchain -Arguments @('version') -ForGoos $Goos

    if ($probe.ExitCode -ne 0) {
        Deny "``go version`` under $($Toolchain.Root) failed (exit $($probe.ExitCode)): $($probe.Text.Trim())"
    }

    $text = $probe.Text.Trim()

    if ($text -notmatch ('(^|\s)' + [regex]::Escape($Expected) + '(\s|$)')) {
        Deny "$($Toolchain.Label) toolchain mismatch -- expected '$Expected' and $($Toolchain.Exe) reports '$text'"
    }

    return $text
}

$versionText       = Assert-Release -Toolchain $TargetGo -Expected $ExpectGo
$sourceVersionText = Assert-Release -Toolchain $SourceGo -Expected $ExpectSourceGo

# ---------------------------------------------------------------------------------------------
# Predicates.
# ---------------------------------------------------------------------------------------------

# The LINE-ANCHORED marker scan CLAUDE.md's corpus mechanics mandate, matching the converter's own
# two accepted spellings (with or without the `go.` qualifier, with or without the Attribute suffix,
# per containsModuleMarker in directiveOperations.go). Unanchored, this over-counts by every bodyless
# partial placeholder comment that merely MENTIONS the marker (~63 against the real 40-odd).
$HandOwnMarkerPattern = '(?m)^\s*\[\s*module\s*:\s*(go\.)?\s*GoManualConversion(Attribute)?\s*\]'

# MSBuild's directories, not the conversion's -- mirrors isBuildOutputDirectory (platformCensus.go).
$BuildOutputDirs = @('bin', 'obj', 'Generated')

$KnownGoos = @('windows', 'linux', 'darwin')

# The converter's OWN skip-list, MIRRORED. `go list std` names unsafe and testing like any other
# package, so no Go question can exclude them -- the converter's isNonConvertedStdLibPackage
# (src\go2cs\stdLibConverter.go) is the only authority, and this is a copy of it. A copy DRIFTS, so
# reconvertDeletionsSkipList_test.go extracts the literal below out of this file and compares it to
# the converter's set under the plain `go test ./...` in src\go2cs. Keep the assignment on ONE line
# and in this spelling; the guard's regex reads it.
$NonConvertedStdPackages = @('unsafe', 'builtin', 'testing', 'cmd')
$NonConvertedStdPrefixes = @('cmd/')

# The two roots under core\ that are the REPOSITORY's own C#, not converter output: golib (the
# hand-written runtime) and go2cs (the Symbols shared project). These are LITERALS and cannot be
# anything else -- they correspond to no Go import path at any release, so no `go list` at either end
# can name them, and they carry no [module: GoManualConversion] marker because nothing ever converts
# into them and there is no generated body for a marker to displace. That is exactly why the marker
# guard could not see them and why R's first dry run offered all 117 of their files for deletion.
$HandWrittenRoots = @('golib', 'go2cs')

function Test-HandOwnMarker {
    param([Parameter(Mandatory = $true)][string] $Path)

    # ReadAllText, never Get-Content: 5.1's Get-Content decodes BOM-less UTF-8 as ANSI, and this file
    # is only ever read (never written), so the round-trip damage would be silent.
    $text = [System.IO.File]::ReadAllText($Path)

    return [regex]::IsMatch($text, $HandOwnMarkerPattern)
}

# The SOURCE release's std package set, one `go list std` per flavour, cached. This is the set the
# converter's -stdlib driver walks, so membership in it IS "the converter emits into this directory".
# It is fetched per GOOS because std membership is flavour-dependent: crypto/x509/internal/macos is in
# std under darwin and absent under windows, internal/runtime/syscall is in std under linux and absent
# under windows -- and each of those has an L3 per-GOOS folder in the corpus that a windows-only set
# would misclassify.
$SourceStdCache = @{}

function Get-SourceStdSet {
    param([Parameter(Mandatory = $true)][string] $ForGoos)

    if ($SourceStdCache.ContainsKey($ForGoos)) {
        return $SourceStdCache[$ForGoos]
    }

    $result = Invoke-Go -Toolchain $SourceGo -Arguments @('list', 'std') -ForGoos $ForGoos

    if ($result.ExitCode -ne 0) {
        Deny "``go list std`` under the source release ($($SourceGo.Root), GOOS=$ForGoos) failed (exit $($result.ExitCode)): $($result.Text.Trim())"
    }

    $set = New-Object 'System.Collections.Generic.HashSet[string]'

    foreach ($line in ($result.Text -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -ne '') { $null = $set.Add($trimmed) }
    }

    # An EMPTY set would make every file NOT-A-CONVERSION-TARGET -- the safe direction for a deletion
    # pass, and therefore exactly the way this instrument could go silently vacuous: it would delete
    # nothing and report a clean run. A std list is ~300 packages; anything under 100 is a broken
    # probe, not a small standard library.
    if ($set.Count -lt 100) {
        Deny "``go list std`` under the source release (GOOS=$ForGoos) returned only $($set.Count) package(s) -- a std list is ~300. Refusing rather than classifying every file as not-a-conversion-target."
    }

    $SourceStdCache[$ForGoos] = $set

    return $set
}

# Cache: one `go list` per (importpath, goos) at the TARGET. A full corpus is ~350 packages x 1
# flavour; without this it would be one process per FILE.
$SelectionCache = @{}

function Get-GoSelection {
    param(
        [Parameter(Mandatory = $true)][string] $ImportPath,
        [Parameter(Mandatory = $true)][string] $ForGoos
    )

    $key = "$ForGoos|$ImportPath"

    if ($SelectionCache.ContainsKey($key)) {
        return $SelectionCache[$key]
    }

    $result = Invoke-Go -Toolchain $TargetGo -Arguments @('list', '-f', '{{.GoFiles}} {{.CgoFiles}}', $ImportPath) -ForGoos $ForGoos

    $selection = [pscustomobject]@{
        PackageExists = ($result.ExitCode -eq 0)
        Files         = New-Object 'System.Collections.Generic.HashSet[string]'
        Diagnostic    = $result.Text.Trim()
    }

    if ($selection.PackageExists) {
        foreach ($token in ($result.Text -replace '[\[\]]', ' ' -split '\s+')) {
            if ($token -ne '') { $null = $selection.Files.Add($token) }
        }
    }

    $SelectionCache[$key] = $selection

    return $selection
}

# Resolve a candidate's package import path and flavour from its location under core\.
#
# THE AMBIGUITY THIS HANDLES, and why it is not a name test. Layout L3 puts a package's
# platform-varying files in core\<pkg>\{windows,linux,darwin}\ -- so `crypto\rand\windows\` is a
# per-GOOS FOLDER of package crypto/rand. But `internal\syscall\windows\` is a REAL Go package whose
# own last segment happens to be a GOOS name (measured at 1.24.13: `go list internal/syscall/windows`
# succeeds and lists twelve files, while `go list crypto/rand/windows` says "is not in std"). A
# name-only rule gets one of those two backwards. So the rule is: ask Go about the full directory
# first; only if NEITHER release knows it AND the last segment is a GOOS name is it an L3 folder.
#
# The SOURCE is consulted for that second question as well as the target, because a real package that
# was REMOVED at the target is still a real package: without it, a removed `<pkg>/<goos>` package
# would be read as an L3 folder of its parent and reported under the parent's name. Its files are a
# DELETE either way, so this sharpens the row's principal rather than changing its class -- but a row
# that names the wrong package is a row a human misreads.
function Resolve-Principal {
    param([Parameter(Mandatory = $true)][string] $RelativePath)

    $parts = $RelativePath -split '/'

    # A .cs sitting directly in core\ belongs to no package. Tested on the PART COUNT, not on the
    # length of a computed directory list: PowerShell's `0..($n - 2)` with $n = 1 is the range 0..-1,
    # which counts DOWNWARDS and yields @(0, -1) -- two indices, both resolving to the file name. The
    # landed instrument computed the list first and tested its Count, so core\GlobalUsings.cs became
    # the two-segment import path "GlobalUsings.cs/GlobalUsings.cs", which no `go list` knows, and the
    # file was reported for deletion.
    if ($parts.Count -lt 2) {
        return $null
    }

    $stem = [System.IO.Path]::GetFileNameWithoutExtension($parts[-1])
    $dirs = @($parts[0..($parts.Count - 2)])

    $fullDir = ($dirs -join '/')

    $asPackage = Get-GoSelection -ImportPath $fullDir -ForGoos $Goos

    if ($asPackage.PackageExists) {
        return [pscustomobject]@{ ImportPath = $fullDir; Goos = $Goos; Principal = "$stem.go"; Selection = $asPackage }
    }

    $leaf = $dirs[-1]

    if ($dirs.Count -ge 2 -and ($KnownGoos -contains $leaf) -and
        -not (Get-SourceStdSet -ForGoos $Goos).Contains($fullDir)) {

        $parentPath = ($dirs[0..($dirs.Count - 2)] -join '/')
        $selection  = Get-GoSelection -ImportPath $parentPath -ForGoos $leaf

        return [pscustomobject]@{ ImportPath = $parentPath; Goos = $leaf; Principal = "$stem.go"; Selection = $selection }
    }

    # Neither a package Go knows at the target nor an L3 folder: either the package is gone at the
    # target (a DELETE row) or the directory was never converter output at all (a
    # NOT-A-CONVERSION-TARGET row). The source-std test in Test-ConversionTarget tells those apart;
    # this reports it under its own path so the row reads honestly either way, with the failed lookup
    # as its selection.
    return [pscustomobject]@{ ImportPath = $fullDir; Goos = $Goos; Principal = "$stem.go"; Selection = $asPackage }
}

# Does the converter EMIT into this file's directory? Asked FIRST, of every candidate, because a
# "does Go still select the principal" answer is meaningless where the answer here is no -- which is
# how 116 golib files and Symbols.cs came to be classified DELETE-ABSENT on this instrument's first
# real run. Returns a reason and a grouping key when the answer is no.
function Test-ConversionTarget {
    param(
        [Parameter(Mandatory = $true)][string] $RelativePath,
        [Parameter(Mandatory = $false)]        $Resolved
    )

    $root = ($RelativePath -split '/')[0]

    # A PATH test, deliberately independent of anything `go list` says -- see the $HandWrittenRoots
    # note. It is also the assertion the deletion loop re-checks, so the two derivations that must
    # agree before a file is removed do not share an instrument.
    if ($HandWrittenRoots -contains $root) {
        return [pscustomobject]@{
            IsTarget = $false; Group = "$root/"
            Reason   = "hand-written repository code under $root\ -- never converter output"
        }
    }

    if ($null -eq $Resolved) {
        return [pscustomobject]@{
            IsTarget = $false; Group = '<core root>'
            Reason   = 'no package directory -- a .cs directly under core\ is not converter output'
        }
    }

    $importPath = $Resolved.ImportPath

    if ($NonConvertedStdPackages -contains $importPath) {
        return [pscustomobject]@{
            IsTarget = $false; Group = $importPath
            Reason   = "std package skip-listed by the converter (isNonConvertedStdLibPackage): $importPath"
        }
    }

    foreach ($prefix in $NonConvertedStdPrefixes) {
        if ($importPath.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            return [pscustomobject]@{
                IsTarget = $false; Group = $prefix
                Reason   = "std path skip-listed by the converter (isNonConvertedStdLibPackage): $importPath"
            }
        }
    }

    if (-not (Get-SourceStdSet -ForGoos $Resolved.Goos).Contains($importPath)) {
        return [pscustomobject]@{
            IsTarget = $false; Group = $importPath
            Reason   = "not a std package at the source release ($ExpectSourceGo, GOOS=$($Resolved.Goos)): $importPath"
        }
    }

    return [pscustomobject]@{ IsTarget = $true; Group = ''; Reason = '' }
}

# ---------------------------------------------------------------------------------------------
# Walk and classify.
# ---------------------------------------------------------------------------------------------

Write-Host ''
Write-Host '=== reconvert deletion pass ===============================================' -ForegroundColor Cyan
Write-Host "  root              $RootFull"
Write-Host "  target toolchain  $versionText"
Write-Host "  target GOROOT     $($TargetGo.Root)"
Write-Host "  source toolchain  $sourceVersionText   ($ExpectSourceGoOrigin)"
Write-Host "  source GOROOT     $($SourceGo.Root)"
Write-Host "  flavour asked     GOOS=$Goos GOARCH=$Goarch CGO_ENABLED=0"
Write-Host ("  seed sentinel     {0:yyyy-MM-dd HH:mm:ss}Z  (modified before this = seeded)" -f $SentinelStamp)
Write-Host ("  mode              {0}" -f $(if ($Apply) { 'APPLY -- deletions will be performed' } else { 'DRY RUN -- nothing will be deleted' }))
Write-Host ''

$rows = New-Object System.Collections.ArrayList

$totalCs        = 0
$emittedCount   = 0
$excludedCount  = 0

$allCs = Get-ChildItem -LiteralPath $CoreDir -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue

foreach ($file in $allCs) {
    $relative = Get-RelativeDisplayPath -Path $file.FullName -Root $CoreDir

    # MSBuild's own output, never the conversion's.
    $segments = $relative -split '/'
    $inBuildOutput = $false

    foreach ($segment in $segments) {
        if ($BuildOutputDirs -contains $segment) { $inBuildOutput = $true; break }
    }

    if ($inBuildOutput) { continue }

    $totalCs++

    # Emitted by THIS run -> not a candidate. Mirrors platformCensus's modification-time rule, as a
    # threshold rather than an equality because this instrument did not stamp the seed itself.
    if ($file.LastWriteTimeUtc -ge $SentinelStamp) {
        $emittedCount++
        continue
    }

    $name = $file.Name

    # <Compile Remove>d artifacts: a stale one cannot collide, so it is counted and not listed.
    if ($name -eq 'package_test_info.cs' -or $name -eq 'go2cs_test_host.cs' -or
        $name -like '*_test.cs' -or $name -like '*.cs.auto' -or $name -like '*.g.cs') {
        $excludedCount++
        continue
    }

    # FIRST question, before the marker scan and before any target lookup: does the converter emit
    # into this directory at all? Everything downstream presumes it does.
    $resolved = Resolve-Principal -RelativePath $relative
    $target   = Test-ConversionTarget -RelativePath $relative -Resolved $resolved

    if (-not $target.IsTarget) {
        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = ''; Class = 'NOT-A-CONVERSION-TARGET'
            Reason = $target.Reason; Group = $target.Group; Full = $file.FullName
        })

        continue
    }

    $isImpl   = ($name -like '*_impl.cs')
    $isMarked = $false

    if (-not $isImpl) { $isMarked = Test-HandOwnMarker -Path $file.FullName }

    if ($isImpl -or $isMarked) {
        $reason = $(if ($isImpl) { '*_impl.cs companion' } else { '[module: GoManualConversion]' })

        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = ''; Class = 'PROTECTED'; Reason = $reason; Group = ''; Full = $file.FullName
        })

        continue
    }

    # Generated metadata has no Go principal by construction. It is NOT automatable from a file name
    # -- R's 25 contains crypto/ecdh/package_init.cs, a genuine stale row -- so it is surfaced rather
    # than silently dropped, and it makes the run exit non-zero.
    if ($name -eq 'package_info.cs' -or $name -eq 'package_init.cs' -or $name -eq 'package_info_internal_test.cs') {
        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = ''; Class = 'UNRESOLVED'
            Reason = 'generated metadata (no Go principal)'; Group = ''; Full = $file.FullName
        })

        continue
    }

    $principalLabel = "$($resolved.ImportPath)/$($resolved.Principal)"

    if (-not $resolved.Selection.PackageExists) {
        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = $principalLabel; Class = 'DELETE-ABSENT'
            Reason = "package not in std at target"; Group = ''; Full = $file.FullName
        })

        continue
    }

    if ($resolved.Selection.Files.Contains($resolved.Principal)) {
        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = $principalLabel; Class = 'KEEP-SELECTED'
            Reason = "selected for $($resolved.Goos)"; Group = ''; Full = $file.FullName
        })

        continue
    }

    $onDisk = Join-Path $TargetGo.SrcDir (($resolved.ImportPath -replace '/', [System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar + $resolved.Principal)

    if (Test-Path -LiteralPath $onDisk -PathType Leaf) {
        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = $principalLabel; Class = 'DELETE-DESELECTED'
            Reason = "present but not selected for $($resolved.Goos)"; Group = ''; Full = $file.FullName
        })
    }
    else {
        $null = $rows.Add([pscustomobject]@{
            Path = $relative; Principal = $principalLabel; Class = 'DELETE-ABSENT'
            Reason = 'principal removed at target'; Group = ''; Full = $file.FullName
        })
    }
}

# ---------------------------------------------------------------------------------------------
# Report. Counts print unconditionally -- a class that appears only when non-empty cannot be told
# from a class whose predicate never fired.
# ---------------------------------------------------------------------------------------------

$classOrder = @('DELETE-ABSENT', 'DELETE-DESELECTED', 'UNRESOLVED', 'PROTECTED', 'NOT-A-CONVERSION-TARGET', 'KEEP-SELECTED')

foreach ($class in $classOrder) {
    $inClass = @($rows | Where-Object { $_.Class -eq $class })

    Write-Host ("--- {0} ({1}) " -f $class, $inClass.Count).PadRight(75, '-')

    if ($class -eq 'KEEP-SELECTED') {
        # The dominant class by construction; listing it whole buries everything else. Its COUNT is
        # the negative control that matters (an instrument that cannot answer "keep" deletes a corpus),
        # and -Verbose prints the rows for anyone who wants them.
        foreach ($row in $inClass) { Write-Verbose ("  {0}  <- {1}" -f $row.Path, $row.Principal) }
        continue
    }

    if ($class -eq 'NOT-A-CONVERSION-TARGET') {
        # Rolled up by GROUP rather than listed: golib alone is over a hundred rows and would bury
        # every actionable line, and the group IS the finding (which directory, and why). Each group
        # prints its count and one representative reason; -Verbose prints the rows.
        foreach ($group in ($inClass | Group-Object Group | Sort-Object Name)) {
            Write-Host ("  {0,-46} {1,4}  {2}" -f $group.Name, $group.Count, $group.Group[0].Reason)
        }

        foreach ($row in $inClass) { Write-Verbose ("  {0}  ({1})" -f $row.Path, $row.Reason) }
        continue
    }

    foreach ($row in ($inClass | Sort-Object Path)) {
        if ($row.Principal -eq '') {
            Write-Host ("  {0}`n      {1}" -f $row.Path, $row.Reason)
        }
        else {
            Write-Host ("  {0}`n      <- {1}  ({2})" -f $row.Path, $row.Principal, $row.Reason)
        }
    }
}

$deleteRows = @($rows | Where-Object { $_.Class -like 'DELETE-*' })
$unresolved = @($rows | Where-Object { $_.Class -eq 'UNRESOLVED' })

function Write-Counts {
    Write-Host ''
    Write-Host '  counts' -ForegroundColor Cyan
    Write-Host ("    production .cs under core        {0}" -f $totalCs)
    Write-Host ("      emitted by this run            {0}" -f $emittedCount)
    Write-Host ("      <Compile Remove>d artifacts    {0}" -f $excludedCount)
    Write-Host ("      seeded candidates              {0}" -f $rows.Count)

    foreach ($class in $classOrder) {
        Write-Host ("        {0,-24} {1}" -f $class, @($rows | Where-Object { $_.Class -eq $class }).Count)
    }

    # The source std sets the candidate test consulted, one line per flavour actually asked. A count
    # beside a set claim (CLAUDE.md): a set nobody counted is a set nobody checked.
    Write-Host ''
    Write-Host '  source std sets consulted' -ForegroundColor Cyan

    foreach ($flavour in ($SourceStdCache.Keys | Sort-Object)) {
        Write-Host ("    go list std ({0}, GOOS={1,-8}) {2} package(s)" -f $ExpectSourceGo, $flavour, $SourceStdCache[$flavour].Count)
    }
}

Write-Counts

# ---------------------------------------------------------------------------------------------
# Pre-deletion. EVERYTHING that can refuse runs here, before a single Remove-Item: the first landing
# of this instrument put the UNRESOLVED check after the loop, so exit 2 meant "deleted, then
# complained". Exit 2 now means zero files removed.
# ---------------------------------------------------------------------------------------------

Write-Host ''
Write-Host '  delete set' -ForegroundColor Cyan

foreach ($class in @('DELETE-ABSENT', 'DELETE-DESELECTED')) {
    Write-Host ("    {0,-24} {1}" -f $class, @($rows | Where-Object { $_.Class -eq $class }).Count)
}

Write-Host ("    {0,-24} {1}" -f 'total', $deleteRows.Count)

# The trespass assertion. By construction no NOT-A-CONVERSION-TARGET row can be in $deleteRows -- the
# candidate test runs first and `continue`s. This re-derives the same property from the PATH alone, so
# a future edit that reorders the classification, or a skip-list that drifts from the converter's,
# fails LOUDLY here instead of deleting the hand-written runtime. Two derivations, one instrument each.
function Test-ProtectedPath {
    param([Parameter(Mandatory = $true)][string] $RelativePath)

    $segments = $RelativePath -split '/'
    $root     = $segments[0]

    if ($HandWrittenRoots -contains $root) { return "under the hand-written root $root\" }

    # A skip-listed PACKAGE protects its own directory, not its subtree: testing\ is hand-owned and
    # testing\fstest\ is an ordinary converted package. Guarded on the segment count before the range
    # is formed, for the `0..-1` reason spelled out in Resolve-Principal.
    if ($segments.Count -gt 1) {
        $dir = ($segments[0..($segments.Count - 2)] -join '/')

        if ($NonConvertedStdPackages -contains $dir) {
            return "in the converter-skip-listed package $dir"
        }
    }

    foreach ($prefix in $NonConvertedStdPrefixes) {
        if ($RelativePath.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            return "under the converter-skip-listed path $prefix"
        }
    }

    return $null
}

$trespassers = @()

foreach ($row in $deleteRows) {
    $why = Test-ProtectedPath -RelativePath $row.Path
    if ($null -ne $why) { $trespassers += [pscustomobject]@{ Path = $row.Path; Why = $why } }
}

if ($trespassers.Count -gt 0) {
    Write-Host ''
    Write-Host ("TRESPASS ASSERTION FIRED -- {0} delete row(s) sit where the converter never emits:" -f $trespassers.Count) -ForegroundColor Red

    foreach ($row in ($trespassers | Sort-Object Path)) {
        Write-Host ("    {0}`n        {1}" -f $row.Path, $row.Why) -ForegroundColor Red
    }

    Stop-ForReview 'the classification and the path test disagree. This is an instrument defect, not a corpus finding.'
}

if ($Apply -and $unresolved.Count -gt 0) {
    Write-Host ''
    Write-Host ("UNRESOLVED: {0} seeded file(s) have no derivable Go principal." -f $unresolved.Count) -ForegroundColor Yellow
    Stop-ForReview '-Apply refuses while UNRESOLVED rows stand. Dispose of each (they are listed above), then re-run with -Apply.'
}

if (-not $Apply) {
    Write-Host ''
    Write-Host ("DRY RUN -- {0} file(s) would be deleted, {1} unresolved. Re-run with -Apply to delete." -f $deleteRows.Count, $unresolved.Count) -ForegroundColor Yellow
}
else {
    Write-Host ''
    Write-Host ("APPLY -- deleting {0} file(s)" -f $deleteRows.Count) -ForegroundColor Yellow

    $deleted = 0

    foreach ($row in ($deleteRows | Sort-Object Path)) {
        # Belt and braces: the same path test the assertion above ran, re-asked immediately before the
        # irreversible act. Unreachable while the assertion stands, which is the point -- if it ever
        # fires the run stops HERE rather than continuing through the rest of the list.
        $why = Test-ProtectedPath -RelativePath $row.Path

        if ($null -ne $why) {
            Write-Host ''
            Write-Host ("DELETION ABORTED MID-LOOP at {0} ({1})" -f $row.Path, $why) -ForegroundColor Red
            Write-Host ("{0} file(s) had already been removed. The tree is PART-DELETED; discard the staging root." -f $deleted) -ForegroundColor Red
            exit 3
        }

        Remove-Item -LiteralPath $row.Full -Force
        $deleted++
        Write-Host ("    deleted  {0}" -f $row.Path)
    }

    $survivors = @($deleteRows | Where-Object { Test-Path -LiteralPath $_.Full })

    Write-Host ''
    Write-Host ("  deleted {0} of {1}; {2} survived" -f $deleted, $deleteRows.Count, $survivors.Count)

    if ($survivors.Count -gt 0) {
        foreach ($row in $survivors) { Write-Host ("    SURVIVED  {0}" -f $row.Path) -ForegroundColor Red }
        Write-Host 'DELETION INCOMPLETE' -ForegroundColor Red
        exit 3
    }

    # Re-count by RE-WALKING the tree, not by re-printing the classification. The counts above
    # describe what was PLANNED; this one is measured off disk afterwards, so the two can disagree
    # and a disagreement is visible. (An earlier draft re-called the same counter here and printed
    # byte-identical numbers under a comment claiming they described the result -- a comment that
    # claims a behaviour the code lacks reads as the census.)
    $remaining = @(Get-ChildItem -LiteralPath $CoreDir -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue |
        Where-Object {
            $rel = Get-RelativeDisplayPath -Path $_.FullName -Root $CoreDir
            $keep = $true
            foreach ($segment in ($rel -split '/')) { if ($BuildOutputDirs -contains $segment) { $keep = $false; break } }
            $keep
        })

    Write-Host ''
    Write-Host '  after (re-walked from disk)' -ForegroundColor Cyan
    Write-Host ("    production .cs under core        {0}   (was {1}, minus {2} deleted)" -f $remaining.Count, $totalCs, $deleted)

    if ($remaining.Count -ne ($totalCs - $deleted)) {
        Write-Host ("    ARITHMETIC MISMATCH -- expected {0}" -f ($totalCs - $deleted)) -ForegroundColor Red
        exit 3
    }
}

Write-Host ''

if ($unresolved.Count -gt 0) {
    Write-Host ("UNRESOLVED: {0} seeded file(s) have no derivable Go principal and were NOT deleted." -f $unresolved.Count) -ForegroundColor Yellow
    Write-Host 'A human must dispose of each before the overlay. Exiting non-zero so this is not passed over.'
    exit 2
}

Write-Host 'No unresolved rows.' -ForegroundColor Green
exit 0
