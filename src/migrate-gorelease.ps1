<#
.SYNOPSIS
    Censuses -- and, with -Apply, performs -- the pin bump half of a Go corpus migration:
    docs\GoCorpusMigration.md's H1.2 (the module's go directive), H2 (<GoStdLibVersion>) and the
    prose half of H12 (the docs that state the release as present-tense fact).

.DESCRIPTION
    A Go-release hop happens repeatedly, and the release number is spelled or derived in many more
    places than the pin. This instrument enumerates EVERY one of them, classifies each, and edits
    only the two classes a human would otherwise edit by hand and miss one of.

    FIVE CLASSES, and the whole point is that only two of them are editable:

      SOURCE-OF-TRUTH   the pin itself. src\version.props' <GoStdLibVersion> (and the build
                        number, which RESETS per release) and src\go2cs\go.mod's `go` directive.
                        -Apply edits these.

      DOC-STATEMENT     prose that states the release as a PRESENT-TENSE fact about what the
                        repository is right now -- the architecture row, the side-by-side sample
                        table and its golang/go blob links, the roster's preamble, the "you need"
                        prerequisite. -Apply edits these, each by a named anchor, never by a blind
                        whole-file substitution.

      DERIVED-BY-REGEN  emitted artifacts that carry the release because the CONVERTER put it
                        there: every src\core\**\README.md badge line, the converted
                        internal/buildcfg zbootstrap files, the docs\validation\current proof
                        pages. NEVER edited -- editing one is drift the next regen silently
                        reverts. The regen that moves them is printed instead.

      DERIVED-AT-RUNTIME  code that READS the pin and therefore follows it for free: the
                        converter's own guards (checkCorpusToolchainPin, corpusPinnedRelease),
                        run-validated-sweep.ps1's pin block, the CI workflow's derive step,
                        push-nuget.ps1, deploy-core.ps1, and every emitted .csproj's
                        $(GoStdLibVersion) proof-file path. Nothing to edit, by construction.

      MUST-NOT-CHANGE   the release named as HISTORY or as MEASUREMENT. Milestone rows, git
                        anchors, NEWS, the hop plans (which name the hop itself, so substituting
                        would destroy them), census records of the form "across the whole Go X
                        standard library, N sites", performance environment stamps, the converter's
                        illustrative comments and hermetic test fixtures, and the write-once
                        docs\validation\<published-version> snapshots that an immutable NuGet
                        version pins. Rewriting any of these turns a true statement false.

    WHAT THIS DOES NOT DO -- deliberately, and the census prints it every run:

      * It does not RECONVERT. It prints the seeded-reconvert ritual and the layout-L3 multi-target
        emission the operator must run (H5, H8), because those are the steps that actually move the
        corpus, and they carry a seeding discipline no script should perform silently.
      * It does not run a single GATE. Not CNR, not the behavioral suite, not the roster sweep.
      * It does not touch the ROSTER's rows or arithmetic. Every row re-validates from the new
        release's own test sources (H10); no substitution can do that.
      * It makes no JUDGEMENT. The hand-own re-audit (H6), the golden-drift triage, the package
        census -- all of those are readings a person makes.

    COMPOSITION WITH set-version.ps1: they do not overlap, contrary to the obvious guess.
    set-version.ps1 stamps the CONVERTER TOOL's Windows PE version resource (winres) and says so in
    its own header; it does not read or write version.props at all. The published package version
    lives in version.props and is owned by exactly two things -- push-nuget.ps1 (which bumps
    <GoBuildNumber> per publish) and this script (which moves <GoStdLibVersion> per migration).
    So this script leaves the tool version to set-version.ps1 entirely, and reports that boundary
    rather than assuming the reader knows it.

.PARAMETER To
    The target Go release, bare (e.g. 1.23.12). Required for -Apply; optional for a census, which
    without it simply reports the sites at the CURRENT pin.

.PARAMETER From
    The release being migrated FROM. Defaults to <GoStdLibVersion> in src\version.props, which is
    the pin's source of truth and is what you want in every ordinary case.

.PARAMETER Apply
    Perform the edits. Without it this changes nothing at all -- census is the default because the
    census is the part that has to be right.

.PARAMETER KeepBuildNumber
    Leave <GoBuildNumber> alone. By default -Apply RESETS it to 0, per the ruling that the build
    number resets per release (docs\PLAN-corpus-upgrade.md H2), so the first publish of the new
    corpus is <To>.1.

.PARAMETER SkipGoMod
    Leave src\go2cs\go.mod's `go` directive alone. Use this when H1 is landing as its own commit
    with the x/tools and x/mod bumps; the runbook wants H1 and H2 in ONE reviewable pair, so the
    default is to move the directive with the pin.

.PARAMETER Quiet
    Suppress the non-actionable classes (DERIVED-*, MUST-NOT-CHANGE) and print only the sites
    -Apply would touch, plus anything unclassified.

.EXAMPLE
    .\migrate-gorelease.ps1
    Census at the current pin. Changes nothing.

.EXAMPLE
    .\migrate-gorelease.ps1 -To 1.23.12
    Census showing exactly what a 1.23.1 -> 1.23.12 hop would edit. Changes nothing.

.EXAMPLE
    .\migrate-gorelease.ps1 -To 1.23.12 -Apply -WhatIf
    Dry run of the apply: every write named, nothing written.

.EXAMPLE
    .\migrate-gorelease.ps1 -To 1.23.12 -Apply
    Perform H1.2 + H2 + H12's prose, then re-census and prove zero sites remain.

.NOTES
    Requires PowerShell 5.1 (Windows) or PowerShell 7+ (any platform). Exit 0 clean, 1 on any
    violation (dirty tree, an anchor that did not match its expected count, a post-apply site
    still at the old release).

    All file I/O is [System.IO.File]::ReadAllText/WriteAllText with UTF8Encoding($false). PS 5.1's
    Get-Content reads the repository's BOM-less UTF-8 as ANSI and Out-File re-encodes the damage --
    that is the documented mojibake trap in CLAUDE.md that once double-encoded the copyright glyph
    across 258 corpus files. Line endings are preserved because a regex replace over the whole text
    never touches the bytes it did not match.
#>
#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $To,
    [string] $From,
    [switch] $Apply,
    [switch] $KeepBuildNumber,
    [switch] $SkipGoMod,
    [switch] $Quiet
)

$ErrorActionPreference = 'Stop'

# Roots come from the one shared definition so this instrument cannot disagree with the sweep, the
# deploy or the behavioral harness about where anything is -- and so it carries no backslash
# literal, which off Windows fails silently rather than loudly.
. (Join-Path $PSScriptRoot '_paths.ps1')

$repo = $RepoRoot
$src = $SrcRoot
$versionProps = Join-Path $src 'version.props'
$converterGoMod = Join-Path $ConverterSrc 'go.mod'

$failures = New-Object System.Collections.Generic.List[string]

function Write-Head {
    param([string] $Text)
    Write-Host ''
    Write-Host $Text -ForegroundColor Cyan
}

# ---- text I/O -----------------------------------------------------------------------------------
# One read and one write helper, so no code path below can reach for Get-Content by accident. The
# BOM is detected and preserved rather than assumed absent: no file this script touches carries one
# today, but the cost of being right is three lines and the cost of being wrong is invisible.

function Read-RepoText {
    param([Parameter(Mandatory)][string] $Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($hasBom) { 3 } else { 0 }

    return [pscustomobject]@{
        Text = [System.Text.Encoding]::UTF8.GetString($bytes, $offset, $bytes.Length - $offset)
        Bom  = $hasBom
    }
}

function Write-RepoText {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][AllowEmptyString()][string] $Text,
        [bool] $Bom = $false
    )

    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($Bom)))
}

# ---- the pin ------------------------------------------------------------------------------------

function Get-PinnedRelease {
    if (-not (Test-Path $versionProps)) {
        throw "version.props not found at $versionProps -- there is no pin to migrate."
    }

    $text = (Read-RepoText $versionProps).Text

    if ($text -notmatch '<GoStdLibVersion>\s*([^<\s]+)\s*</GoStdLibVersion>') {
        throw "No <GoStdLibVersion> in $versionProps -- the pin has no source of truth."
    }

    return $Matches[1].Trim()
}

# The Go tree a corpus-defining conversion would actually READ. GOROOT's own VERSION file wins over
# `go env GOVERSION` for the reason the converter's convertingRelease and the sweep's pin block both
# document: a GOROOT environment variable overrides the selected toolchain's root, so the reported
# version can misdescribe the sources. Advisory here -- this script edits text, it does not convert,
# and refusing a legitimate "bump the pin before you switch the box" would be wrong.
function Get-ToolchainRelease {
    $goVersion = ''
    $goRoot = ''

    try {
        $goRoot = (& go env GOROOT 2>$null)
        if ($goRoot) { $goRoot = "$goRoot".Trim() }
        $reported = (& go env GOVERSION 2>$null)
        if ($reported) { $goVersion = "$reported".Trim() }
    }
    catch {
        return [pscustomobject]@{ Release = ''; Source = 'unavailable'; GoRoot = '' }
    }

    $source = 'go env GOVERSION'

    if ($goRoot) {
        $versionFile = Join-Path $goRoot 'VERSION'

        if (Test-Path $versionFile) {
            # Go 1.21 added a `time <stamp>` line beneath the release; the release is the first line.
            $first = (Read-RepoText $versionFile).Text -split "`n" | Select-Object -First 1

            if ($first) {
                $goVersion = $first.Trim()
                $source = 'GOROOT/VERSION'
            }
        }
    }

    return [pscustomobject]@{
        Release = ($goVersion -replace '^go', '')
        Source  = $source
        GoRoot  = $goRoot
    }
}

# ---- the site table -----------------------------------------------------------------------------
# Every editable site, named individually. {OLD} is replaced by the regex-escaped outgoing release
# and {NEW} by the incoming one, so one table serves every hop.
#
# Expect is load-bearing: an anchor that matches a different number of times than stated means the
# prose moved underneath the table, and that is a finding the operator must see -- never a silent
# partial edit. This is the same shape as the repository's other preflights: by-path, cheap, and
# impossible to pass vacuously.

$editableSites = @(
    # ---- SOURCE-OF-TRUTH ------------------------------------------------------------------------
    @{
        File = 'src/version.props'; Class = 'SOURCE-OF-TRUTH'
        Find = '<GoStdLibVersion>{OLD}</GoStdLibVersion>'
        Replace = '<GoStdLibVersion>{NEW}</GoStdLibVersion>'
        Expect = 1
        Note = 'THE pin. Every runtime guard and the whole published version derive from this one element'
    }
    @{
        File = 'src/go2cs/go.mod'; Class = 'SOURCE-OF-TRUTH'; Skip = { $SkipGoMod }
        # \r? because the repository's working tree is pinned to CRLF: .NET's multiline `$` matches
        # only before \n, so an unguarded anchor silently matches nothing on every checkout.
        Find = '(?m)^go {OLD}\r?$'
        Replace = 'go {NEW}'
        Expect = 1
        Note = "the converter module's go directive (H1.2, ruled: it moves each migration). -SkipGoMod leaves it"
    }

    # ---- DOC-STATEMENT --------------------------------------------------------------------------
    @{
        File = 'CLAUDE.md'; Class = 'DOC-STATEMENT'
        Find = '; Go {OLD}\) auto-converted'
        Replace = '; Go {NEW}) auto-converted'
        Expect = 1
        Note = 'architecture row: the release the corpus on disk was converted from'
    }
    @{
        File = 'docs/README.md'; Class = 'DOC-STATEMENT'
        Find = '\*\*Go {OLD}\*\* source'
        Replace = '**Go {NEW}** source'
        Expect = 1
        Note = 'side-by-side sample section lead-in'
    }
    @{
        File = 'docs/README.md'; Class = 'DOC-STATEMENT'
        Find = '\| Package \| Go {OLD} source \|'
        Replace = '| Package | Go {NEW} source |'
        Expect = 1
        Note = 'side-by-side sample table header'
    }
    @{
        File = 'docs/README.md'; Class = 'DOC-STATEMENT'
        Find = 'github\.com/golang/go/blob/go{OLD}/'
        Replace = 'github.com/golang/go/blob/go{NEW}/'
        Expect = 6
        Note = 'the six blob links the sample table points at -- they must name the tree the committed C# came from'
    }
    @{
        File = 'docs/README.md'; Class = 'DOC-STATEMENT'
        Find = 'packages, Go {OLD}\) compiles cleanly'
        Replace = 'packages, Go {NEW}) compiles cleanly'
        Expect = 1
        Note = 'present-tense compile claim (the package COUNT is re-measured separately; only the release moves here)'
    }
    @{
        File = 'docs/README.md'; Class = 'DOC-STATEMENT'
        Find = '\*\*\[Go {OLD}\]\(https://go\.dev/dl/\)\*\*'
        Replace = '**[Go {NEW}](https://go.dev/dl/)**'
        Expect = 1
        Note = 'the "Try it yourself" prerequisite -- a visitor installs this exact release or the sweep refuses'
    }
    @{
        File = 'docs/ValidatedTestPackages.md'; Class = 'DOC-STATEMENT'
        Find = 'has its own Go {OLD} '
        Replace = 'has its own Go {NEW} '
        Expect = 1
        Note = "roster preamble: the release each row's test suite comes from"
    }
    @{
        File = 'docs/ValidatedTestPackages.md'; Class = 'DOC-STATEMENT'
        Find = 'packages whose Go {OLD} sources define'
        Replace = 'packages whose Go {NEW} sources define'
        Expect = 1
        Note = "the denominator's definition (its VALUE is re-derived at H3/H10; only the release moves here)"
    }
    @{
        File = 'docs/ValidatedTestPackages.md'; Class = 'DOC-STATEMENT'
        Find = 'the Windows record for the Go {OLD} era'
        Replace = 'the Windows record for the Go {NEW} era'
        Expect = 1
        Note = 'the per-OS column rule names its era; H10 re-derives every column into the new one'
    }
    @{
        File = 'docs/Roadmap.md'; Class = 'DOC-STATEMENT'
        Find = '`go build` \(Go {OLD}\)'
        Replace = '`go build` (Go {NEW})'
        Expect = 1
        Note = 'the converter-improvement loop names the toolchain it is run with'
    }
    @{
        File = 'docs/Background.md'; Class = 'DOC-STATEMENT'
        Find = 'versioned `{OLD}\.<build>`'
        Replace = 'versioned `{NEW}.<build>`'
        Expect = 1
        Note = 'the published package version family a consumer sees on nuget.org'
    }
    @{
        File = 'docs/Background.md'; Class = 'DOC-STATEMENT'
        Find = 'packages whose Go {OLD} sources actually define'
        Replace = 'packages whose Go {NEW} sources actually define'
        Expect = 1
        Note = "the completion-goal denominator's definition"
    }
    @{
        File = 'docs/ConversionStrategies.md'; Class = 'DOC-STATEMENT'
        Find = '> Go {OLD}\) wherever possible'
        Replace = '> Go {NEW}) wherever possible'
        Expect = 1
        Note = 'the release the strategy summary draws its real converted snippets from'
    }
)

# Occurrences inside the DOC-STATEMENT files that are HISTORY or MEASUREMENT and must survive the
# hop unchanged. Anything in one of those files matching neither an editable anchor nor one of these
# is reported for REVIEW -- new prose is exactly the thing a migration must not substitute blindly.
$historyAnchors = @(
    @{ File = 'CLAUDE.md'; Find = 'packages of the full conversion \(Go {OLD}\) compile clean'
       Note = 'Phase-3 milestone, commit-anchored' }
    @{ File = 'CLAUDE.md'; Find = "Go {OLD}'s TERMINAL validation marker"
       Note = 'git-anchors table row' }
    @{ File = 'CLAUDE.md'; Find = 'the {OLD} corpus publishes as'
       Note = 'git-anchors table row (the anchor NuGet release)' }
    @{ File = 'docs/README.md'; Find = 'requires go2cs packages \*\*{OLD}\.5 or later\*\*'
       Note = 'the release Linux support FIRST shipped in' }
    @{ File = 'docs/README.md'; Find = 'All \*\*302\*\* packages \(Go {OLD}\) compile with zero errors'
       Note = 'milestone table row, tag-anchored' }
    @{ File = 'docs/README.md'; Find = "Go {OLD}'s terminal validation marker"
       Note = 'milestone table row, tag-anchored' }
    @{ File = 'docs/Roadmap.md'; Find = 'the auto-conversion \(Go {OLD}\) build clean'
       Note = 'dated Phase-3 status block, commit-anchored' }
    @{ File = 'docs/Roadmap.md'; Find = 'the full Go {OLD} standard-library'
       Note = 'Phase-3 outcome over the retired src/go-src-converted tree' }
    @{ File = 'docs/Roadmap.md'; Find = 'Releases since \*\*{OLD}\.5\*\*'
       Note = 'the release Linux converter support FIRST shipped in' }
)

# Path rules for the discovery sweep. FIRST match wins, so order is meaningful: the specific
# validation-snapshot rule has to precede the general docs rule.
$pathClasses = @(
    @{ Match = '^src/version\.props$';                    Class = 'SOURCE-OF-TRUTH'; Note = 'the pin' }
    @{ Match = '^src/go2cs/go\.mod$';                     Class = 'SOURCE-OF-TRUTH'; Note = 'the module go directive' }

    @{ Match = '^src/core/.+/README\.md$';                Class = 'DERIVED-BY-REGEN'; Note = 'converter-emitted badge line (readmeValidationBadge.go); moves by regen, never by edit' }
    # The root attribution files the converter copies VERBATIM out of GOROOT (rootAttributionFiles,
    # stdLibConverter.go). core/VERSION literally holds `go1.23.1` plus Go's own timestamp line, so
    # it is the corpus's own record of which Go tree it came from -- and the reconvert re-copies it
    # from the NEW GOROOT for free. Editing it by hand would be drift the next regen reverts.
    @{ Match = '^src/core/(VERSION|LICENSE|PATENTS|README\.md|SECURITY\.md|CONTRIBUTING\.md)$'
                                                          Class = 'DERIVED-BY-REGEN'; Note = "GOROOT root attribution file, copied verbatim by the reconvert (core/VERSION is Go's own release record)" }
    @{ Match = '^src/core/internal/buildcfg/.+\.cs$';     Class = 'DERIVED-BY-REGEN'; Note = "converted from GOROOT's own generated zbootstrap.go" }
    @{ Match = '^src/core/.+\.csproj$';                   Class = 'DERIVED-AT-RUNTIME'; Note = 'proof-file path built from $(GoStdLibVersion).$(GoBuildNumber)' }
    @{ Match = '^docs/validation/current/';               Class = 'DERIVED-BY-REGEN'; Note = 're-derived per package by the -tests pipeline at H10' }

    @{ Match = '^docs/validation/';                       Class = 'MUST-NOT-CHANGE'; Note = 'write-once published snapshot; the NuGet version pinning it is immutable' }
    @{ Match = '^src/core/testing/';                      Class = 'MUST-NOT-CHANGE'; Note = 'hand-owned test host: a measurement record naming the release it was measured on' }
    @{ Match = '^src/go2cs/.+_test\.go$';                 Class = 'MUST-NOT-CHANGE'; Note = 'hermetic fixture: the release is an INPUT to the function under test, not a pin' }
    @{ Match = '^src/go2cs/.+\.go$';                      Class = 'MUST-NOT-CHANGE'; Note = 'illustrative comment; the code itself derives the release at runtime' }

    @{ Match = '^\.github/';                              Class = 'DERIVED-AT-RUNTIME'; Note = 'the workflow READS <GoStdLibVersion> out of version.props and refuses to hardcode it' }
    @{ Match = '^src/run-validated-sweep\.ps1$';          Class = 'DERIVED-AT-RUNTIME'; Note = 'pin block: compares version.props against GOROOT/VERSION' }
    @{ Match = '^src/push-nuget\.ps1$';                   Class = 'DERIVED-AT-RUNTIME'; Note = 'reads <GoStdLibVersion>, owns <GoBuildNumber> per publish' }
    @{ Match = '^src/deploy-core\.ps1$';                  Class = 'DERIVED-AT-RUNTIME'; Note = 'copies version.props to the deploy root verbatim' }
    @{ Match = '^src/set-version\.ps1$';                  Class = 'MUST-NOT-CHANGE'; Note = 'stamps the CONVERTER TOOL version; its comment merely contrasts itself with version.props' }
    @{ Match = '^src/_roster\.ps1$';                      Class = 'MUST-NOT-CHANGE'; Note = 'explanatory comment naming the era the roster columns were banked in' }
    @{ Match = '^src/release-nuget\.bat$';                Class = 'MUST-NOT-CHANGE'; Note = 'a worked example in a comment' }

    # Fixture module directives. These are a FLOOR, not a pin, and they are inert for the one thing
    # that could have made them matter: build-constraint evaluation derives its go1.N release-tag set
    # from `go env GOVERSION` (loaderReleaseTags -> releaseTagsForVersion), never from a module's own
    # directive -- and that derivation is minor-keyed, so a patch hop has no tag delta at all. Moving
    # them would churn behavioral goldens for nothing. A MINOR hop is different in one narrow way: a
    # fixture that wants to USE a new language feature needs its own directive raised, which is a
    # per-fixture decision and never a substitution.
    @{ Match = '^src/tests/.+/go\.mod$';                  Class = 'MUST-NOT-CHANGE'; Note = 'behavioral/performance fixture module floor; inert for release-tag expansion (that reads GOVERSION)' }
    @{ Match = '^src/tour/go\.mod$';                      Class = 'MUST-NOT-CHANGE'; Note = 'the tour module floor; same reasoning as the fixtures' }
    @{ Match = '^src/tour/.+_test\.go$';                  Class = 'MUST-NOT-CHANGE'; Note = 'hermetic fixture: the version string is an INPUT to the function under test' }
    @{ Match = '^src/tour/.+\.go$';                       Class = 'MUST-NOT-CHANGE'; Note = 'the go.mod the tour EMITS carries a module floor, on the same reasoning as the fixtures' }
    @{ Match = '^src/tour/README\.md$';                   Class = 'MUST-NOT-CHANGE'; Note = 'tour prose naming the release its worked example was captured on' }
    @{ Match = '^src/go2cs/testdata/';                    Class = 'MUST-NOT-CHANGE'; Note = 'hermetic golden for the proof-page renderer; the release is fixture data' }
    @{ Match = '^src/tests/ConverterBuildInputs\.cs$';    Class = 'MUST-NOT-CHANGE'; Note = "illustrative comment showing `go version go2cs.exe` output shape" }
    @{ Match = '^src/tests/Performance/README\.md$';      Class = 'MUST-NOT-CHANGE'; Note = 'measurement environment stamp: the toolchain a benchmark number was measured on' }
    @{ Match = '^src/archived/';                          Class = 'MUST-NOT-CHANGE'; Note = 'archived record' }

    @{ Match = '^docs/NEWS\.md$';                         Class = 'MUST-NOT-CHANGE'; Note = 'the news archive: dated entries about what happened' }
    @{ Match = '^docs/news/';                             Class = 'MUST-NOT-CHANGE'; Note = 'the news archive' }
    @{ Match = '^docs/PLAN-';                             Class = 'MUST-NOT-CHANGE'; Note = 'a plan NAMES the hop (X -> Y); substituting would destroy the very statement' }
    @{ Match = '^docs/phase3/';                           Class = 'MUST-NOT-CHANGE'; Note = 'design/finding record, written against a named release' }
    @{ Match = '^docs/phase4/';                           Class = 'MUST-NOT-CHANGE'; Note = 'design/finding record, written against a named release' }
    @{ Match = '^docs/CleanupBacklog\.md$';               Class = 'MUST-NOT-CHANGE'; Note = 'backlog items describing what a past release shipped' }
    @{ Match = '^docs/CIMatrix\.md$';                     Class = 'MUST-NOT-CHANGE'; Note = 'describes the derive-never-write rule; names no release of its own' }
    @{ Match = '^docs/Performance\.md$';                  Class = 'MUST-NOT-CHANGE'; Note = 'measurement environment stamp: the toolchain a number was measured on' }
    @{ Match = '^docs/StdLibCompileMilestone\.md$';       Class = 'MUST-NOT-CHANGE'; Note = 'milestone record' }
    @{ Match = '^docs/ConversionStrategies-Reference\.md$'; Class = 'MUST-NOT-CHANGE'; Note = 'census records ("across the whole Go X stdlib, N sites") and illustrative doc-link examples' }
    @{ Match = '^docs/GoCorpusMigration\.md$';            Class = 'MUST-NOT-CHANGE'; Note = 'this runbook is version-agnostic by design' }
)

# ---- resolve the two releases -------------------------------------------------------------------

$fromRelease = if ($From) { $From.Trim() } else { Get-PinnedRelease }
$toRelease = if ($To) { $To.Trim() } else { '' }

if ($fromRelease -notmatch '^\d+\.\d+(\.\d+)?$') {
    throw "-From '$fromRelease' is not a bare Go release (expected e.g. 1.23.1)."
}

if ($toRelease -and $toRelease -notmatch '^\d+\.\d+(\.\d+)?$') {
    throw "-To '$toRelease' is not a bare Go release (expected e.g. 1.23.12)."
}

if ($Apply -and -not $toRelease) {
    throw '-Apply needs -To <release>: there is nothing to migrate to.'
}

if ($toRelease -and $toRelease -eq $fromRelease) {
    throw "-To and the current pin are both $fromRelease -- nothing to migrate."
}

$oldPattern = [regex]::Escape($fromRelease)

function Expand-Anchor {
    param([Parameter(Mandatory)][AllowEmptyString()][string] $Template, [switch] $AsReplacement)

    if ($AsReplacement) {
        # The replacement side is literal text, so {OLD} must not carry regex escaping into it.
        return $Template.Replace('{OLD}', $fromRelease).Replace('{NEW}', $toRelease)
    }

    return $Template.Replace('{OLD}', $oldPattern).Replace('{NEW}', [regex]::Escape($toRelease))
}

Write-Host 'go-release migration' -ForegroundColor Cyan
Write-Host "  repository   $repo"
Write-Host "  pinned       $fromRelease   (<GoStdLibVersion>, src/version.props)"

if ($toRelease) {
    Write-Host "  target       $toRelease" -ForegroundColor Yellow
}
else {
    Write-Host '  target       (none given -- census only; pass -To <release> to see the hop)'
}

$toolchain = Get-ToolchainRelease

if ($toolchain.Release) {
    Write-Host "  toolchain    $($toolchain.Release)   (via $($toolchain.Source))"
}
else {
    Write-Host '  toolchain    unavailable (go not on PATH) -- advisory only'
}

$mode = if ($Apply) { 'APPLY' } else { 'CENSUS (read-only)' }
Write-Host "  mode         $mode" -ForegroundColor $(if ($Apply) { 'Yellow' } else { 'Green' })

# ---- the editable census ------------------------------------------------------------------------

Write-Head 'editable sites -- the two classes -Apply touches'

$plan = New-Object System.Collections.Generic.List[object]

foreach ($site in $editableSites) {
    $path = Join-Path $repo $site.File
    $skipped = $false

    if ($site.ContainsKey('Skip') -and (& $site.Skip)) { $skipped = $true }

    if (-not (Test-Path $path)) {
        [void]$failures.Add("$($site.File) -- file not found; the site table names a path this tree does not have")
        continue
    }

    $find = Expand-Anchor $site.Find
    $text = (Read-RepoText $path).Text
    $count = ([regex]::Matches($text, $find)).Count

    [void]$plan.Add([pscustomobject]@{
        File    = $site.File
        Class   = $site.Class
        Find    = $find
        Replace = (Expand-Anchor $site.Replace -AsReplacement)
        Expect  = $site.Expect
        Found   = $count
        Note    = $site.Note
        Skipped = $skipped
    })
}

$lastFile = ''
foreach ($row in $plan) {
    if ($row.File -ne $lastFile) {
        Write-Host ''
        Write-Host "  $($row.File)" -ForegroundColor White
        $lastFile = $row.File
    }

    $state = if ($row.Skipped) { 'skipped' } elseif ($row.Found -eq $row.Expect) { "$($row.Found) site(s)" } else { "$($row.Found) found, EXPECTED $($row.Expect)" }
    $color = if ($row.Skipped) { 'DarkGray' } elseif ($row.Found -eq $row.Expect) { 'Green' } else { 'Red' }

    Write-Host ('    [{0}] {1}' -f $row.Class, $state) -ForegroundColor $color
    Write-Host "        $($row.Note)" -ForegroundColor DarkGray

    if (-not $row.Skipped -and $row.Found -ne $row.Expect) {
        [void]$failures.Add("$($row.File) -- anchor '$($row.Find)' matched $($row.Found) time(s), expected $($row.Expect). The prose moved underneath the site table; re-anchor it before applying.")
    }
}

$editableTotal = ($plan | Where-Object { -not $_.Skipped } | Measure-Object -Property Found -Sum).Sum
if ($null -eq $editableTotal) { $editableTotal = 0 }

# ---- history anchors + review -------------------------------------------------------------------

Write-Head 'history inside those same files -- MUST-NOT-CHANGE, verified present'

$docFiles = $plan | Where-Object { $_.Class -eq 'DOC-STATEMENT' } | ForEach-Object { $_.File } | Sort-Object -Unique
$historyCovered = @{}

foreach ($anchor in $historyAnchors) {
    $path = Join-Path $repo $anchor.File
    if (-not (Test-Path $path)) { continue }

    $find = Expand-Anchor $anchor.Find
    $text = (Read-RepoText $path).Text
    $count = ([regex]::Matches($text, $find)).Count

    $color = if ($count -gt 0) { 'DarkGray' } else { 'Yellow' }
    $state = if ($count -gt 0) { "$count occurrence(s) preserved" } else { 'not present (prose may have moved)' }
    Write-Host ('  {0,-32} {1}' -f $anchor.File, $state) -ForegroundColor $color
    Write-Host "      $($anchor.Note)" -ForegroundColor DarkGray

    if (-not $historyCovered.ContainsKey($anchor.File)) { $historyCovered[$anchor.File] = New-Object System.Collections.Generic.List[string] }
    [void]$historyCovered[$anchor.File].Add($find)
}

# Every line in a DOC-STATEMENT file that names the outgoing release and is covered by NEITHER an
# editable anchor nor a history anchor. New prose lands here, which is exactly where a person has to
# look: a migration must not substitute text nobody classified.
$review = New-Object System.Collections.Generic.List[object]

foreach ($file in $docFiles) {
    $path = Join-Path $repo $file
    $editAnchors = @($plan | Where-Object { $_.File -eq $file -and -not $_.Skipped } | ForEach-Object { $_.Find })
    $histAnchors = @()
    if ($historyCovered.ContainsKey($file)) { $histAnchors = @($historyCovered[$file]) }

    $lines = (Read-RepoText $path).Text -split "`r?`n"

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -notmatch $oldPattern) { continue }

        $covered = $false
        foreach ($a in ($editAnchors + $histAnchors)) {
            if ([regex]::IsMatch($line, $a)) { $covered = $true; break }
        }

        if (-not $covered) {
            [void]$review.Add([pscustomobject]@{ File = $file; Line = ($i + 1); Text = $line.Trim() })
        }
    }
}

Write-Head 'unanchored occurrences in those files -- REVIEW (never auto-edited)'

if ($review.Count -eq 0) {
    Write-Host '  none -- every occurrence in the doc-statement files is classified' -ForegroundColor Green
}
else {
    Write-Host "  $($review.Count) line(s) name $fromRelease but match no anchor. Rule on each, then either" -ForegroundColor Yellow
    Write-Host '  add it to $editableSites (present-tense fact) or to $historyAnchors (history/measurement).' -ForegroundColor Yellow
    foreach ($row in $review) {
        $snippet = $row.Text
        if ($snippet.Length -gt 140) { $snippet = $snippet.Substring(0, 137) + '...' }
        Write-Host ('    {0}:{1}' -f $row.File, $row.Line) -ForegroundColor Yellow
        Write-Host "        $snippet" -ForegroundColor DarkGray
    }
}

# ---- the discovery sweep ------------------------------------------------------------------------
# git grep rather than a filesystem walk, for two reasons the repository has already paid for: a
# default ripgrep honors src/core/.gitignore and under-counts, and only tracked files can be part of
# a migration's commit anyway.

Write-Head "whole-tree sweep for $fromRelease -- every other site, by class"

$sweepArgs = @('grep', '-I', '--no-color', '-c', '-F', '--', $fromRelease)
$rawCounts = & git -C $repo @sweepArgs 2>$null

$buckets = @{}
$unclassified = New-Object System.Collections.Generic.List[object]
$sweptTotal = 0

foreach ($entry in $rawCounts) {
    if (-not $entry) { continue }

    $sep = $entry.LastIndexOf(':')
    if ($sep -lt 1) { continue }

    $relPath = $entry.Substring(0, $sep)
    $hits = [int]$entry.Substring($sep + 1)
    $sweptTotal += $hits

    # The files the editable table already accounts for are reported above, not here.
    if ($docFiles -contains $relPath) { continue }
    if ($relPath -eq 'src/version.props' -or $relPath -eq 'src/go2cs/go.mod') { continue }

    # This script itself names the release only in its own examples.
    if ($relPath -eq 'src/migrate-gorelease.ps1') { continue }

    $class = ''
    $note = ''

    foreach ($rule in $pathClasses) {
        if ($relPath -match $rule.Match) { $class = $rule.Class; $note = $rule.Note; break }
    }

    if (-not $class) {
        [void]$unclassified.Add([pscustomobject]@{ Path = $relPath; Hits = $hits })
        continue
    }

    $key = "$class|$note"
    if (-not $buckets.ContainsKey($key)) {
        $buckets[$key] = [pscustomobject]@{ Class = $class; Note = $note; Files = 0; Hits = 0 }
    }

    $buckets[$key].Files++
    $buckets[$key].Hits += $hits
}

if (-not $Quiet) {
    foreach ($class in @('DERIVED-BY-REGEN', 'DERIVED-AT-RUNTIME', 'MUST-NOT-CHANGE')) {
        $rows = @($buckets.Values | Where-Object { $_.Class -eq $class } | Sort-Object -Property @{ Expression = 'Hits'; Descending = $true })
        if ($rows.Count -eq 0) { continue }

        Write-Host ''
        Write-Host "  $class" -ForegroundColor White
        foreach ($row in $rows) {
            Write-Host ('    {0,4} file(s), {1,5} occurrence(s)  {2}' -f $row.Files, $row.Hits, $row.Note) -ForegroundColor DarkGray
        }
    }
}
else {
    Write-Host '  (suppressed by -Quiet)' -ForegroundColor DarkGray
}

Write-Host ''
if ($unclassified.Count -eq 0) {
    Write-Host '  UNCLASSIFIED: none -- every tracked occurrence falls in a named class' -ForegroundColor Green
}
else {
    Write-Host "  UNCLASSIFIED: $($unclassified.Count) file(s) -- rule on each before the hop lands" -ForegroundColor Yellow
    foreach ($row in ($unclassified | Sort-Object Path)) {
        Write-Host ('    {0}  ({1} occurrence(s))' -f $row.Path, $row.Hits) -ForegroundColor Yellow
    }
}

# ---- what this instrument does NOT do -----------------------------------------------------------

function Write-Handoff {
    $target = if ($toRelease) { $toRelease } else { '<target>' }

    Write-Head 'NOT done here -- the operator owns these'

    Write-Host '  1. The toolchain (H1). Install Go ' -NoNewline
    Write-Host $target -ForegroundColor White -NoNewline
    Write-Host ' side-by-side, bump x/tools and x/mod as their OWN'
    Write-Host '     commit with its own CNR, then rebuild the converter -- a toolchain hop invalidates'
    Write-Host '     go2cs.exe in NO harness predicate, so the build is owed explicitly:'
    Write-Host '        cd src/go2cs && go build -o bin/go2cs' -ForegroundColor White
    Write-Host '        go test ./...' -ForegroundColor White
    Write-Host ''
    Write-Host '  2. The REGEN (H5, H8). Generated artifacts move by conversion, never by edit. Seed the'
    Write-Host '     staging root FIRST -- an unseeded root gives the hand-own marker nothing to detect,'
    Write-Host '     emits every whole-file hand-own as a plain .cs, and breaks per-GOOS layout adoption:'
    Write-Host ''
    Write-Host '        # seed: src/core, src/version.props and docs/validation, mirroring the src/ layout' -ForegroundColor DarkGray
    Write-Host '        # then, single-target (the default windows corpus):' -ForegroundColor DarkGray
    Write-Host '        go2cs -stdlib -comments -go2cspath <staging>/src' -ForegroundColor White
    Write-Host ''
    Write-Host '        # and the layout-L3 multi-target emission this corpus actually carries:' -ForegroundColor DarkGray
    Write-Host '        go2cs -stdlib -comments -platforms windows/amd64,linux/amd64,darwin/amd64 \' -ForegroundColor White
    Write-Host '              -platform-stage <stage> -go2cspath <staging>/src' -ForegroundColor White
    Write-Host ''
    Write-Host '     Delete and re-seed per run; never convert twice into one staging root.'
    Write-Host ''
    Write-Host '  3. The GATES. converter go test, check-no-regression.ps1, the full behavioral suite,'
    Write-Host '     go2cs-stdlib.slnx per target OS, and the roster sweep. None is run here.'
    Write-Host ''
    Write-Host '  4. The JUDGEMENT. The package census (H3), the hand-own .auto differential (H6), the'
    Write-Host '     golden-drift triage, and the per-row roster re-derivation (H10) -- every roster row'
    Write-Host "     re-validates from the new release's own test sources, and no substitution can do that."
    Write-Host ''
    Write-Host "  5. The CONVERTER TOOL version. That is set-version.ps1's (the winres PE resource), and it"
    Write-Host '     is independent of version.props. Untouched here on purpose.'
    Write-Host ''
    Write-Host "  Runbook: docs/GoCorpusMigration.md" -ForegroundColor DarkGray
}

# ---- census exit --------------------------------------------------------------------------------

if (-not $Apply) {
    Write-Handoff

    Write-Head 'census summary'
    Write-Host "  editable sites at ${fromRelease}: $editableTotal occurrence(s) across $(@($plan | Where-Object { -not $_.Skipped -and $_.Found -gt 0 } | ForEach-Object { $_.File } | Sort-Object -Unique).Count) file(s)"
    Write-Host "  tracked occurrences in the whole tree: $sweptTotal"
    Write-Host '  changed: NOTHING (census is the default; pass -Apply to edit)' -ForegroundColor Green

    if ($failures.Count -gt 0) {
        Write-Host ''
        Write-Host "census: $($failures.Count) problem(s)" -ForegroundColor Red
        $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        exit 1
    }

    exit 0
}

# ---- apply --------------------------------------------------------------------------------------

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host 'refusing to apply -- the census is not clean:' -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

# A migration must not mix its edits with anyone else's. Scoped to the files this run would touch:
# an unrelated dirty file elsewhere in the tree is not this script's business.
$targetFiles = @($plan | Where-Object { -not $_.Skipped } | ForEach-Object { $_.File } | Sort-Object -Unique)
$dirty = @(& git -C $repo status --porcelain -- $targetFiles 2>$null | Where-Object { $_ })

if ($dirty.Count -gt 0) {
    Write-Host ''
    Write-Host 'refusing to apply -- the working tree is dirty in files this run would edit:' -ForegroundColor Red
    $dirty | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host '  Commit or stash them first; a migration edit must be reviewable on its own.' -ForegroundColor Red
    exit 1
}

if ($toolchain.Release -and $toolchain.Release -ne $toRelease) {
    Write-Host ''
    Write-Host "  note: the live toolchain is $($toolchain.Release), not $toRelease. The pin bump legitimately" -ForegroundColor Yellow
    Write-Host '  precedes the toolchain switch on a given box, but nothing may RECONVERT until they agree --' -ForegroundColor Yellow
    Write-Host '  checkCorpusToolchainPin and the sweep both refuse, by design.' -ForegroundColor Yellow
}

Write-Head "applying $fromRelease -> $toRelease"

$written = 0
$edits = 0

foreach ($file in $targetFiles) {
    $path = Join-Path $repo $file
    $doc = Read-RepoText $path
    $text = $doc.Text
    $fileEdits = 0

    foreach ($row in ($plan | Where-Object { $_.File -eq $file -and -not $_.Skipped })) {
        # The replacement is LITERAL text, so any '$' in it must be doubled or Regex.Replace would
        # read it as a group reference. None of the anchors carries one today; doubling costs nothing
        # and removes the class of defect entirely.
        $literal = $row.Replace -replace '\$', '$$$$'
        $text = [regex]::Replace($text, $row.Find, $literal)
        $fileEdits += $row.Found
    }

    # The build number resets per release, so the first publish of the new corpus is <To>.1.
    if ($file -eq 'src/version.props' -and -not $KeepBuildNumber) {
        if ($text -match '<GoBuildNumber>(\d+)</GoBuildNumber>') {
            $previous = $Matches[1]

            if ($previous -ne '0') {
                $text = [regex]::Replace($text, '<GoBuildNumber>\d+</GoBuildNumber>', '<GoBuildNumber>0</GoBuildNumber>')
                $fileEdits++
                Write-Host "  version.props: <GoBuildNumber> $previous -> 0 (ruled: resets per release; first publish is $toRelease.1)"
            }
        }
    }

    if ($PSCmdlet.ShouldProcess($path, "$fileEdits edit(s): $fromRelease -> $toRelease")) {
        Write-RepoText -Path $path -Text $text -Bom $doc.Bom
        $written++
    }

    $edits += $fileEdits
    Write-Host ('  {0,-34} {1} edit(s)' -f $file, $fileEdits) -ForegroundColor Green
}

if ($WhatIfPreference) {
    Write-Host ''
    Write-Host "  -WhatIf: $edits edit(s) across $($targetFiles.Count) file(s) NAMED, none written." -ForegroundColor Yellow
    Write-Handoff
    exit 0
}

# ---- post-apply verification --------------------------------------------------------------------
# The claim this instrument makes is that re-running it finds nothing. Prove it here rather than
# asking the operator to take it on faith -- re-read the bytes just written and assert both
# directions: zero editable sites left at the outgoing release, and every one present at the new one.

Write-Head 'post-apply verification'

$remaining = 0
$landed = 0

foreach ($row in ($plan | Where-Object { -not $_.Skipped })) {
    $path = Join-Path $repo $row.File
    $text = (Read-RepoText $path).Text

    $stillOld = ([regex]::Matches($text, $row.Find)).Count

    # The same anchor, re-pointed at the incoming release: $row.Find already carries the ESCAPED old
    # release, so the literal substring to swap is that escaped form.
    $newFind = ($row.Find -replace [regex]::Escape($oldPattern), [regex]::Escape($toRelease))
    $nowNew = ([regex]::Matches($text, $newFind)).Count

    $remaining += $stillOld
    $landed += $nowNew

    if ($stillOld -gt 0) {
        [void]$failures.Add("$($row.File) -- $stillOld occurrence(s) of $fromRelease still match '$($row.Find)' after the edit")
    }

    if ($nowNew -ne $row.Expect) {
        [void]$failures.Add("$($row.File) -- expected $($row.Expect) occurrence(s) at $toRelease, found $nowNew")
    }
}

Write-Host "  editable sites still at ${fromRelease}: $remaining   (must be 0)" -ForegroundColor $(if ($remaining -eq 0) { 'Green' } else { 'Red' })
Write-Host "  editable sites now at ${toRelease}: $landed" -ForegroundColor $(if ($failures.Count -eq 0) { 'Green' } else { 'Red' })

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host "post-apply verification FAILED -- $($failures.Count) problem(s)" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host '  The tree is half-migrated. Revert with: git checkout -- <files> and re-anchor the table.' -ForegroundColor Red
    exit 1
}

Write-Host '  re-running this script now reports ZERO editable sites: idempotent.' -ForegroundColor Green

Write-Handoff

Write-Head 'applied'
Write-Host "  $edits edit(s) across $written file(s): $fromRelease -> $toRelease" -ForegroundColor Green
Write-Host '  Commit H1 (toolchain + directive) and H2 (the pin) as ONE reviewable pair -- between them' -ForegroundColor DarkGray
Write-Host '  the binary claims the new release while version.props still names the old, and a' -ForegroundColor DarkGray
Write-Host '  -recurse=nuget conversion in that window misjudges every module it is handed.' -ForegroundColor DarkGray
exit 0
