<#
.SYNOPSIS
    Packs the go2cs converted Go standard library (plus the go.lib runtime and go.gen source
    generator) as NuGet packages from a fresh Release build, and optionally pushes them to a feed.

.DESCRIPTION
    Targets ONLY src\go2cs-stdlib.slnx -- the generated standard-library solution, which contains
    the ~301 converted stdlib libraries, the hand-owned ones (unsafe, testing) AND the two shared
    infrastructure projects core\golib (go.lib) and gen\go2cs-gen (go.gen). It deliberately never
    packs src\go2cs.slnx, whose behavioral-test and example projects are not publishable.

    MULTIPLATFORM (docs\phase4\DESIGN-multiplatform-corpus.md section 9(a), increment 4). The converted corpus
    is one tree whose platform-varying packages keep per-GOOS sources selected by $(GoTargetOS), so the
    solution is built and packed ONCE PER RID and the flavors are merged into a single nupkg per
    package: lib\<tfm> carries the reference (Windows) flavor as the compile-time asset, and
    runtimes\<rid>\lib\<tfm> carries each shipped RID's runtime assembly. Package IDs, and everything a
    consumer writes, are unchanged. Platform-neutral packages -- the large majority -- are copied
    verbatim from the reference pass and are byte-for-byte what a single-pass release produced.

    All packages share one version, sourced from src\version.props: <GoStdLibVersion>.<GoBuildNumber>
    (base tracks the converted Go release, e.g. 1.23.1; the 4th part is the build/publish counter).
    -Push increments GoBuildNumber by default so every publish is a new version (commit version.props
    afterward to record the release). See -BumpBuild to force or suppress the bump.

    PUBLICATION ALSO FREEZES THE PROOF. Before anything is built, docs\validation\current\ is copied
    to docs\validation\<version>\ (write-once) and the version-pinned validation badge links in every
    src\core\*\README.md are retargeted at it, so a published package's green badge, its proof link
    and the VALIDATION.md it packs all describe the exact binary being pushed. Commit the snapshot,
    the retargeted READMEs and version.props together.

    IT ALSO MINTS THE RELEASE TAG, at that same pre-build moment rather than after the push. Every
    README's C# Source badge links github.com/ritchiecarroll/go2cs/tree/nuget-<version>/src/core/<pkg>,
    so the tag has to exist by the time those READMEs are baked into packages -- tagging afterward
    published a README full of links to a tag that did not exist yet. Creation is idempotent
    (check-then-skip) so a re-run after a failed later phase does not die on "tag already exists".

    SAFETY: pushing to a public feed is an irreversible publish (a version can be unlisted, never
    deleted). This script therefore PACKS ONLY by default; it pushes nothing unless -Push is given,
    and -WhatIf reports each push without performing it. The API key is read from the NUGET_API_KEY
    environment variable (or -ApiKey) and is never written to disk.

.PARAMETER ApiKey
    NuGet API key for -Push. Defaults to the NUGET_API_KEY environment variable.

.PARAMETER Source
    NuGet push source. Defaults to https://api.nuget.org/v3/index.json. May be a local folder feed.

.PARAMETER Configuration
    Build configuration to pack. Defaults to Release.

.PARAMETER OutDir
    Directory to collect the merged .nupkg files. Defaults to src\artifacts\nupkg. Each RID's
    unmerged pack is kept beside them under _flavors\<rid>\ for inspection.

.PARAMETER SkipBuild
    NO LONGER SUPPORTED, and rejected with an explanation. A multiplatform release is built once per
    RID with a different $(GoTargetOS) each time, so there is no single on-disk build to pack.

.PARAMETER BumpBuild
    Force or suppress the GoBuildNumber increment in src\version.props (then commit it). Defaults to
    ON with -Push (each publish is a new version) and OFF for a pack-only run. Pass -BumpBuild:$false
    to publish the CURRENT version without bumping -- e.g. finishing a partially-failed push (with
    --skip-duplicate) or serial automation that manages the version itself.

.PARAMETER Push
    Actually push the packed .nupkg to the feed. Without this switch the script only packs.

.EXAMPLE
    .\push-nuget.ps1
    Pack every package to src\artifacts\nupkg (no push, no bump). Inspect the output, then push.

.EXAMPLE
    .\push-nuget.ps1 -Push
    The normal release: bump the build number, build + pack Release, and push the NEXT version to
    nuget.org (NUGET_API_KEY set).

.EXAMPLE
    .\push-nuget.ps1 -Push -BumpBuild:$false
    Re-push the CURRENT version without bumping -- e.g. to finish a partially-failed publish
    (--skip-duplicate skips the packages already on the feed).
#>
#Requires -Version 5.1
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [string]$Configuration = 'Release',
    [string]$OutDir,
    [switch]$SkipBuild,
    [switch]$BumpBuild,
    [switch]$Push
)

$ErrorActionPreference = 'Stop'

$src = $PSScriptRoot
$slnx = Join-Path $src 'go2cs-stdlib.slnx'
$versionProps = Join-Path $src 'version.props'
if (-not $OutDir) { $OutDir = Join-Path $src 'artifacts\nupkg' }

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

if (-not (Test-Path $slnx)) { throw "Solution not found: $slnx" }
if (-not (Test-Path $versionProps)) { throw "version.props not found: $versionProps" }

# --- Embedded stdlib-metadata staleness gate ------------------------------------------------------
# go2cs/stdlib-metadata.txt is the converter's embedded record of what every converted stdlib package
# EXPORTS across assemblies (its GoTypeAlias aliases and GoImplement records). Under -recurse=nuget the
# converter reads it INSTEAD of the package_info.cs it can no longer find on disk, so it must describe
# the very tree this script is about to pack. If the converted stdlib's exported surface changed and the
# asset was not regenerated, the published packages and the converter disagree -- and the damage lands
# in END USERS' builds (missing `global using` aliases, or a duplicate/absent interface adapter), not
# here. Verify BEFORE anything is built, so the run fails at second zero rather than after a full
# Release build. Regenerate with `go generate .` from src\go2cs and commit the result.
# MSBuild worker nodes PERSIST after a build and are re-entered by the next one. This script runs
# back-to-back solution builds (one per RID, different $(GoTargetOS) each), which is exactly the
# shape the repo's standing rule prescribes this flag for -- and the 1.23.1.7 release's pack race
# (gen's bin empty at pack time after a SUCCEEDED build, 3/3 in the full script, 0/3 isolated,
# 0/2 in binlog-armed repro) fits stale node state around the clean/copy file ops better than
# anything else measured. The healthy binlog shows 16 nodes; fresh nodes per pass cost seconds.
# The assert-and-repair below STAYS: if it never fires again after this flag, node reuse is
# confirmed by alternation at zero repro cost (ledger #5, closed measured-and-hardened).
$env:MSBUILDDISABLENODEREUSE = '1'

$converterDir = Join-Path $src 'go2cs'

if (-not (Get-Command go -ErrorAction SilentlyContinue)) {
    throw "The Go toolchain is required to verify go2cs\stdlib-metadata.txt is in sync with src\core before publishing."
}

Write-Step "Verifying stdlib-metadata.txt matches src\core"
Push-Location $converterDir

try {
    & go test -count=1 -run TestStdLibMetadataInSync . | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "STALE EMBEDDED METADATA: go2cs\stdlib-metadata.txt does not match src\core. " +
              "Run ``go generate .`` from src\go2cs, commit the regenerated asset, then re-run this script. " +
              "Publishing now would ship packages the converter's -recurse=nuget mode describes incorrectly."
    }
}
finally {
    Pop-Location
}

# --- Build-number bump (raw-text edit preserves comments/formatting) -----------------------------
# Each publish should be a NEW version, so the build number is bumped by default when -Push is given
# and left alone for a pack-only run. An explicit -BumpBuild / -BumpBuild:$false always wins -- use
# -BumpBuild:$false to re-push the CURRENT version (e.g. finishing a partially-failed push).
if ($PSBoundParameters.ContainsKey('BumpBuild')) { $doBump = [bool]$BumpBuild } else { $doBump = [bool]$Push }

$propsText = [System.IO.File]::ReadAllText($versionProps)
if ($propsText -notmatch '<GoBuildNumber>(\d+)</GoBuildNumber>') { throw "GoBuildNumber not found in $versionProps" }
$build = [int]$Matches[1]

$bumped = $false

if ($doBump) {
    $newBuild = $build + 1
    if ($PSCmdlet.ShouldProcess($versionProps, "bump GoBuildNumber $build -> $newBuild")) {
        $propsText = $propsText -replace '<GoBuildNumber>\d+</GoBuildNumber>', "<GoBuildNumber>$newBuild</GoBuildNumber>"
        [System.IO.File]::WriteAllText($versionProps, $propsText, $utf8NoBom)
        Write-Step "Bumped GoBuildNumber $build -> $newBuild (commit version.props to record the release)"
        $build = $newBuild
        $bumped = $true
    }
}

if ($propsText -match '<GoStdLibVersion>([^<]+)</GoStdLibVersion>') { $baseVersion = $Matches[1] } else { $baseVersion = '?' }
$fullVersion = "$baseVersion.$build"
Write-Step "Package version: $fullVersion   (solution: go2cs-stdlib.slnx)"

# The version a BUMPING run would publish. Only meaningful when this run did not bump: $build is then
# still the last-published number, so +1 names the next release. After a bump $build already IS that
# number, so the value would be one release too far ahead -- it is set to $null rather than computed
# wrongly, and every consumer below sits on a -not $doBump path where that cannot happen.
if ($doBump) { $wouldBeVersion = $null } else { $wouldBeVersion = "$baseVersion.$($build + 1)" }

# A pack-only INSPECTION run -- the dry run docs\phase4\MILESTONE-75pct-prep.md section 3.3 recommends, and the
# only shape in which the dry-run affordances below engage. Deliberately NARROWER than -not $doBump:
#
#   .\push-nuget.ps1                     $dryRun = $true    the section 3.3 dry run
#   .\push-nuget.ps1 -BumpBuild:$false   $dryRun = $true    same shape, bump explicitly declined
#   .\push-nuget.ps1 -Push               $dryRun = $false   the release
#   .\push-nuget.ps1 -Push -BumpBuild:$false
#                                        $dryRun = $false   a RELEASE (re-push of the current version
#                                                           finishing a partially-failed publish); it
#                                                           does not bump, but it publishes, so it must
#                                                           freeze and verify against the REAL tree
#   .\push-nuget.ps1 -BumpBuild          $dryRun = $false   prepare-the-release-commit; it bumps, so it
#                                                           writes the real write-once snapshot
#
# Excluding -Push whatever its bump setting is what keeps the release path untouched by everything
# below: with -Push, $dryRun is $false by construction and every branch guarded on it is dead code.
#
# CODE-PATH PROOF that -Push behaves exactly as it did before this affordance existed. The dry-run fix
# touches six executable sites, and with -Push every one of them resolves to its pre-existing form:
#
#   1. $wouldBeVersion  -- a new variable. $null when $doBump; read ONLY inside `if ($dryRun)` branches,
#                          so on any -Push run it is either $null or computed-and-never-read.
#   2. $dryRun          -- $false whenever $Push, regardless of $doBump. This is the keystone: it makes
#                          sites 3-6 unreachable on every release path.
#   3. the tag skip message, 4. the "Froze N" message, 6. the "Verified N" message
#                       -- each `if ($dryRun) { new } else { original }`; the else branch reproduces the
#                          previous string literal verbatim, so console output is byte-identical too.
#   5. the snapshot redirect and the ShouldProcess short-circuit
#                       -- `if ($dryRun)` is not taken, so $versionProofs keeps docs\validation\<version>,
#                          and `$dryRun -or $PSCmdlet.ShouldProcess(...)` evaluates ShouldProcess exactly
#                          as the bare call did ($false -or X is X, including its -WhatIf side effect).
#   + the try/finally  -- adds no catch, so exceptions propagate unchanged, and the finally is a no-op
#                          because $dryRunProofRoot is $null on every release path. No `exit` is enclosed.
#
# Net effect with -Push: two variable assignments that nothing reads. Nothing else in the script's
# behaviour, output or side effects moves.
$dryRun = (-not $Push) -and (-not $doBump)

$repoRoot = Split-Path $src -Parent

# --- Release tag ----------------------------------------------------------------------------------
# The tag is minted HERE, before anything is packed, because every package's README BAKES A LINK TO
# IT: the C# Source badge points at github.com/ritchiecarroll/go2cs/tree/nuget-<version>/src/core/<pkg>
# so a reader lands on the exact C# that shipped in the package they hold. Minting the tag after the
# push -- where it used to live, as a Phase-3 instruction -- meant every README on nuget.org linked a
# 404 for however long it took to get around to tagging. Created before the first .nupkg is built,
# the link resolves the moment the package is published.
#
# The tag names the tree this release was built FROM. HEAD here is the last commit before the release
# commit, and the two differ only by version.props, the proof snapshot and the retargeted README
# links -- no converted C# moves between them -- so the tree the badge reaches IS the C# in the
# package. (Committing the release before running this flow would collapse the two; nothing needs it.)
#
# It runs BEFORE the write-once proof snapshot deliberately: a signing failure then costs nothing,
# where the reverse order would leave a frozen directory behind for a release that never happened.
#
# Gated on the bump, because the bump is what makes a run a release -- a pack-only inspection run
# must not mint a release tag. Idempotent by check-then-skip, loudly, so a re-run after a failed
# later phase carries on instead of dying on "tag already exists".
$releaseTag = "nuget-$fullVersion"

if (-not $doBump) {
    # $releaseTag is composed from the UN-bumped $fullVersion, so on a non-bumping run it names the tag
    # of the release already published -- not the one "the run that bumps" would mint. Naming it here
    # misinforms at exactly the moment someone is checking the version arithmetic. A dry run therefore
    # names the would-be tag instead. The -Push -BumpBuild:$false branch keeps today's wording verbatim:
    # it is a release path, and this fix is scoped to leave every release path byte-identical.
    if ($dryRun) {
        Write-Step "No build-number bump this run -- not tagging (the run that bumps mints nuget-$wouldBeVersion)"
    }
    else {
        Write-Step "No build-number bump this run -- not tagging (the run that bumps mints $releaseTag)"
    }
}
elseif (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Warning ("git is not available, so release tag $releaseTag was NOT created. Every package README's C# " +
                   "Source badge links it and will 404 until it exists: create it by hand before publishing " +
                   "(git tag -s $releaseTag -m ""NuGet publication $fullVersion"").")
}
elseif (& git -C $repoRoot tag --list $releaseTag) {
    Write-Step "Release tag $releaseTag already exists -- keeping it (re-run of a partially completed release)"
}
elseif ($PSCmdlet.ShouldProcess($releaseTag, 'create signed release tag at HEAD')) {
    # Signed, per repository convention. A failure here is fatal on purpose: publishing 300 packages
    # whose READMEs all link a tag that does not exist is worse than stopping. If GPG is the problem,
    # the agent must be launched via Gpg4win's gpgconf.
    & git -C $repoRoot tag -s $releaseTag -m "NuGet publication $fullVersion"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create signed release tag $releaseTag ($LASTEXITCODE). Every package README's C# Source badge links this tag -- resolve the signing failure and re-run before publishing."
    }

    Write-Step "Created signed release tag $releaseTag at $(& git -C $repoRoot rev-parse --short HEAD) (push it with the release commit)"
}

# --- Validation proof snapshot + badge retarget ---------------------------------------------------
# Every validated package's README carries a green badge whose link is VERSION-PINNED, and every
# validated package packs that same page as VALIDATION.md. Both point at docs\validation\<version>\,
# which is written ONCE here, at publication, and never rewritten: the proof shown for go.io 1.23.1.3
# stays forever the proof as of that binary, while docs\validation\current\ keeps moving.
#
# Order matters. The snapshot is taken BEFORE the build so the .csproj Exists() guards see the files
# they are about to pack, and the READMEs are retargeted in the same breath so the badge, the link
# and the packed sheet are the one version being published.
$validationDir = Join-Path $repoRoot 'docs\validation'
$currentProofs = Join-Path $validationDir 'current'
$versionProofs = Join-Path $validationDir $fullVersion
$dryRunProofRoot = $null

# WHY A DRY RUN NEEDS ITS OWN SNAPSHOT DIRECTORY (found by the section 3.6 rehearsal, defect D1).
#
# A pack-only run does not bump, so $fullVersion is the LAST-PUBLISHED version and $versionProofs is a
# directory that already exists. The freeze then takes the "keeping it" branch and is never exercised,
# and -- the real damage -- the green-badge verifier below checks TODAY's badges against a snapshot
# frozen at the last release. Every package validated since then links a page that frozen directory
# will never contain, so the verifier throws on the alphabetically first one and the run dies in eight
# seconds having measured nothing. That is not a defect in the tree: it is guaranteed the moment one
# row banks after a release, which is the normal state of this campaign (at the rehearsal: 162 green
# badges against a 126-page snapshot). The documented dry run was simply unrunnable.
#
# The fix mirrors what release morning actually does, without writing to the tree. A dry run freezes
# docs\validation\current into a TEMPORARY directory named for the version a bumping run would publish,
# and the verifier checks the corpus's badges against those pages -- which is a coherent check, because
# current\ is exactly the set the next release will freeze. The freeze branch runs for real (phase 5's
# "Froze N" count is measured, not skipped), and the directory is removed in the finally below.
#
# It is NOT written to docs\validation\<would-be>\ in the tree: that path is write-once and belongs to
# the release that bumps. Pre-creating it would make the release's own precondition check throw.
#
# $fullVersion is deliberately NOT moved to the would-be version. It is the version this run packs, and
# it is what the README badge retarget and verification below compare against; moving it would rewrite
# every README in the tree to advertise a version that was never published, and bake that wrong version
# into the packed READMEs. Only the PROOF-PAGE location moves.
if ($dryRun) {
    $dryRunProofRoot = Join-Path ([System.IO.Path]::GetTempPath()) "go2cs-dryrun-proofs-$PID-$([System.IO.Path]::GetRandomFileName())"
    $versionProofs = Join-Path $dryRunProofRoot $wouldBeVersion
    Write-Step "Dry run -- freezing the would-be $wouldBeVersion snapshot to a temporary directory (the tree is not written)"
}

try {

if (-not (Test-Path $currentProofs)) {
    Write-Warning "No validation proof pages at $currentProofs -- skipping the snapshot and badge retarget."
} else {
    if (Test-Path $versionProofs) {
        # A frozen directory is write-once. Re-publishing the CURRENT version (-BumpBuild:$false, e.g.
        # finishing a partially-failed push -- and any -WhatIf run, which declines the bump)
        # legitimately finds its own snapshot already there; a version that was ACTUALLY bumped a
        # moment ago finding one means the version counter and the docs tree disagree.
        if ($bumped) { throw "Validation snapshot $versionProofs already exists for the newly bumped version $fullVersion. Frozen snapshots are write-once -- reconcile src\version.props with docs\validation before publishing." }
        Write-Step "Validation snapshot $fullVersion already exists (write-once) -- keeping it"
    }
    # ShouldProcess gates writes the USER's tree keeps; a dry run's snapshot is a temporary directory
    # this script deletes itself, so there is nothing to approve or to decline. Short-circuiting on
    # $dryRun therefore also lets a pack-only -WhatIf reach the phases past this block instead of
    # declining the freeze and then failing verification against a directory it just refused to fill.
    # On every release path $dryRun is $false and ShouldProcess is consulted exactly as before.
    elseif ($dryRun -or $PSCmdlet.ShouldProcess($versionProofs, "snapshot docs\validation\current")) {
        New-Item -ItemType Directory -Force $versionProofs | Out-Null
        Copy-Item (Join-Path $currentProofs '*.md') $versionProofs -Force
        $frozenCount = (Get-ChildItem $versionProofs -Filter *.md).Count
        # The count is the phase-5 invariant: it must equal the roster's row count, which must equal the
        # number of green-badge READMEs. A dry run names the would-be version and the temporary location
        # so the line cannot be misread as a write into the tree; the release wording is unchanged.
        if ($dryRun) {
            Write-Step "Froze $frozenCount validation proof page(s) for would-be version $wouldBeVersion at $versionProofs (temporary)"
        }
        else {
            Write-Step "Froze $frozenCount validation proof page(s) at docs\validation\$fullVersion"
        }
    }

    # Retarget the version segment of every green badge link in the converted stdlib's READMEs. Read
    # AND write through [System.IO.File] with UTF-8/no-BOM: PS 5.1's Get-Content reads the converter's
    # BOM-less UTF-8 as ANSI and Out-File re-encodes the damage, which is what mojibake'd the corpus's
    # (c) signs once already. ReadAllText/WriteAllText round-trips the CRLF the converter emitted.
    $utf8NoBomText = New-Object System.Text.UTF8Encoding($false)
    # The segment class excludes whitespace and ')' as well as '/': a hand-owned README's PROSE link
    # (testing's `validation/index.html) was produced...`) has no second '/', and a bare [^/]+ ate
    # everything up to the next stray slash -- collapsing four lines of prose into a broken URL on
    # the 1.23.1.3 release run. A green badge's versioned link always terminates its segment with
    # '/', so the tightened class changes nothing for the links this retarget exists to move.
    #
    # That same class is what lets the Tests badge share its LINE with the Docs badge (added
    # 2026-08-08): the space between the two badges terminates the segment, so a retarget can never
    # run past the proof link into the pkg.go.dev link beside it. The Docs badge is otherwise
    # invisible to every pattern in this block -- it is anchored on 'go2cs.net/validation/', and the
    # verification below on 'badge/Tests-', neither of which a 'badge/Docs-' / 'pkg.go.dev' badge
    # can satisfy. Verified against both a green and a vendored README before the badge landed.
    $badgeLinkPattern = '(https://go2cs\.net/validation/)[^/\s)]+(/)'
    $retargeted = 0

    foreach ($readme in Get-ChildItem (Join-Path $src 'core') -Filter 'README.md' -Recurse -File) {
        $text = [System.IO.File]::ReadAllText($readme.FullName)
        if ($text -notmatch $badgeLinkPattern) { continue }

        $updated = [regex]::Replace($text, $badgeLinkPattern, "`${1}$fullVersion`${2}")
        if ($updated -eq $text) { continue }

        if ($PSCmdlet.ShouldProcess($readme.FullName, "retarget validation badge link to $fullVersion")) {
            [System.IO.File]::WriteAllText($readme.FullName, $updated, $utf8NoBomText)
            $retargeted++
        }
    }

    Write-Step "Retargeted $retargeted README badge link(s) to $fullVersion (commit them with version.props)"

    # Consistency by construction: a converter README re-emission must now be a no-op. The badge line
    # is composed from exactly two inputs -- the published version and the proof page's totals line --
    # so re-deriving it here from the FROZEN snapshot and comparing byte for byte is that re-emission,
    # without needing the Go toolchain or a 4-minute reconvert mid-release.
    $verified = 0

    foreach ($readme in Get-ChildItem (Join-Path $src 'core') -Filter 'README.md' -Recurse -File) {
        $text = [System.IO.File]::ReadAllText($readme.FullName)
        if ($text -notmatch 'badge/Tests-(\d+)%2F(\d+)_validated-brightgreen') { continue }

        $badgeMatched = [int]$Matches[1]
        $badgeTotal = [int]$Matches[2]

        # The dot-id itself contains dots (path.filepath), so its capture excludes only "/" and ")".
        if ($text -notmatch 'https://go2cs\.net/validation/([^/]+)/([^)/]+)\.html') { throw "Green badge without a proof link in $($readme.FullName)" }

        $linkVersion = $Matches[1]
        $dotId = $Matches[2]

        if ($linkVersion -ne $fullVersion) { throw "Green badge in $($readme.FullName) still links $linkVersion, not $fullVersion" }

        $proofPage = Join-Path $versionProofs "$dotId.md"
        if (-not (Test-Path $proofPage)) { throw "Green badge in $($readme.FullName) links a proof page that was not snapshotted: $proofPage" }

        $proofText = [System.IO.File]::ReadAllText($proofPage)
        if ($proofText -notmatch '\*\*(\d+) matched \S+ (\d+) disclosed\*\*') { throw "No totals line in $proofPage" }

        if ($badgeMatched -ne [int]$Matches[1] -or $badgeTotal -ne ([int]$Matches[1] + [int]$Matches[2])) {
            throw "Badge in $($readme.FullName) claims $badgeMatched/$badgeTotal but $proofPage records $($Matches[1]) matched + $($Matches[2]) disclosed"
        }

        $verified++
    }

    if ($dryRun) {
        Write-Step "Verified $verified green badge(s) against the would-be $wouldBeVersion proof pages"
    }
    else {
        Write-Step "Verified $verified green badge(s) against the frozen $fullVersion proof pages"
    }
}

}
finally {
    # Only ever removes a directory this run created under the system temp path; $dryRunProofRoot is
    # $null on every release path, so this is a no-op there. In the finally so a throw anywhere in the
    # snapshot/verify block above still cleans up. (The try's body is left at its original indentation
    # to keep this fix's diff readable -- PowerShell does not care, and `git diff` shows the change
    # rather than a re-indent of ninety unchanged lines.)
    if ($dryRunProofRoot -and (Test-Path $dryRunProofRoot)) {
        Remove-Item $dryRunProofRoot -Recurse -Force -ErrorAction SilentlyContinue
        Write-Step "Removed the dry run's temporary proof snapshot"
    }
}

# --- C# Source badge retarget ---------------------------------------------------------------------
# The C# Source badge (2026-08-08) is version-pinned TWICE -- in its message and in the release tag
# its link resolves against -- and both must move to the version this run is publishing, or a
# published package's README sends the reader to the PREVIOUS release's C#.
#
# Its own block, deliberately outside the proof-snapshot branch above: this badge is on EVERY package
# README, validated or not, and has nothing to do with docs\validation. Gating it on the proof pages
# existing would silently ship stale source links on the one run where they had gone missing.
#
# Both patterns are anchored on literals only this badge carries -- 'badge/Source-@' paired with the
# .NET purple '-512BD4' (the Go Source badge beside it is '-00ADD8', so the colour field is what
# tells the twins apart since the r51d tidy dropped the language text from the message). The version
# class excludes '-' (it terminates at the badge's colour field) and whitespace/')' (so it can never
# run past the badge, the lesson the Tests-badge pattern learned the hard way on the 1.23.1.3 run).
# ⚠ The 1.23.1.5 run shipped with this pattern still spelling r51c's 'Source-C%23_@' form: the text
# retarget silently no-opped against r51d's renamed badge while the link retarget matched, and the
# verifier below SKIPPED files without the stale form instead of failing them -- a vacuous pass.
# Both are corrected here; the verifier now throws on a README with no C# Source badge at all.
$sourceBadgeVersionPattern = '(badge/Source-@)[^-\s)]+(-512BD4)'
$sourceBadgeTagPattern = '(https://github\.com/ritchiecarroll/go2cs/tree/nuget-)[^/\s)]+(/src/core/)'
$sourceRetargeted = 0

foreach ($readme in Get-ChildItem (Join-Path $src 'core') -Filter 'README.md' -Recurse -File) {
    $text = [System.IO.File]::ReadAllText($readme.FullName)
    $updated = $text

    foreach ($pattern in @($sourceBadgeVersionPattern, $sourceBadgeTagPattern)) {
        $updated = [regex]::Replace($updated, $pattern, "`${1}$fullVersion`${2}")
    }

    if ($updated -eq $text) { continue }

    if ($PSCmdlet.ShouldProcess($readme.FullName, "retarget C# Source badge to $fullVersion")) {
        [System.IO.File]::WriteAllText($readme.FullName, $updated, $utf8NoBom)
        $sourceRetargeted++
    }
}

Write-Step "Retargeted $sourceRetargeted C# Source badge(s) to $fullVersion (commit them with version.props)"

# Same consistency-by-construction check the green badges get: the badge is composed from
# version.props and nothing else, so re-deriving it here IS the converter re-emission, and both of
# its pins must name the version being published.
$sourceVerified = 0

foreach ($readme in Get-ChildItem (Join-Path $src 'core') -Filter 'README.md' -Recurse -File) {
    $text = [System.IO.File]::ReadAllText($readme.FullName)
    # Non-package READMEs legitimately carry no badges: the root attribution file, golib's
    # hand-written runtime README, and testdata corpora (plus anything under build output).
    if ($readme.Directory.FullName -eq (Join-Path $src 'core')) { continue }
    if ($readme.FullName -match '\\(testdata|bin|obj)\\' -or $readme.Directory.Name -eq 'golib') { continue }
    if ($text -notmatch 'badge/Source-@([^-\s)]+)-512BD4') { throw "README without a C# Source badge: $($readme.FullName) -- every package README carries one; a no-match here means the badge form drifted and this retarget is no-opping (the vacuous pass that shipped on the 1.23.1.5 run)" }

    if ($Matches[1] -ne $fullVersion) { throw "C# Source badge in $($readme.FullName) states version $($Matches[1]), not $fullVersion" }

    if ($text -notmatch 'https://github\.com/ritchiecarroll/go2cs/tree/nuget-([^/\s)]+)/src/core/') { throw "C# Source badge without a release-tag link in $($readme.FullName)" }

    if ($Matches[1] -ne $fullVersion) { throw "C# Source badge in $($readme.FullName) links tag nuget-$($Matches[1]), not nuget-$fullVersion" }

    $sourceVerified++
}

Write-Step "Verified $sourceVerified C# Source badge(s) pin $fullVersion and its release tag"

# --- Multiplatform pack: ONE nupkg per package, carrying RID-specific assemblies ------------------
# docs\phase4\DESIGN-multiplatform-corpus.md section 9 option (a), staged as section 12 increment 4.
#
# The converted corpus is ONE tree in layout L3: a package whose C# varies by GOOS keeps its
# platform-selected sources in per-GOOS subfolders and $(GoTargetOS) admits exactly one of them to the
# compilation. A PUBLISHED package must nevertheless work on every supported platform without the
# consumer choosing anything, so the solution is built once per RID and the flavors are merged into a
# single nupkg per package:
#
#   go.os/
#     lib/<tfm>/os.dll                        compile-time asset + RID-agnostic runtime fallback
#     runtimes/win-x64/lib/<tfm>/os.dll       runtime asset, selected on win-x64
#     runtimes/linux-x64/lib/<tfm>/os.dll     runtime asset, selected on linux-x64
#
# WHY lib/ RATHER THAN ref/, against NuGet's documented asset selection. NuGet gives `lib/{tfm}/` both
# the `compile` and the `runtime` asset roles; `ref/{tfm}/` gives only `compile`; and
# `runtimes/{rid}/lib/{tfm}/` gives only `runtime`, RID-selected, and REQUIRES a compile asset to exist
# elsewhere in the package. A RID-specific managed assembly is therefore always a two-part shape, and
# the only question is what the compile half is:
#
#   * lib/ carrying the REFERENCE flavor (chosen). Compile-time binding is the reference flavor's
#     surface; at run time the host reads the `runtimeTargets` entries NuGet writes into deps.json for
#     the runtimes/ assets and, when one matches the running RID, uses it INSTEAD of the RID-agnostic
#     lib/ assembly of the same name -- so a portable framework-dependent app resolves the right
#     flavor with no RuntimeIdentifier anywhere in the consumer's project. On a RID this release does
#     not ship, the lib/ assembly is what loads: a Linux-arm64 consumer would silently get Windows
#     behaviour. That is the honest cost of this choice and it is why the shipped RID set is stated in
#     the design rather than inferred.
#   * ref/ carrying a neutral surface (not chosen). It would turn that silent wrong-flavor fallback
#     into a loud missing-assembly failure, which is better -- but there IS no neutral surface to put
#     there: section 6 measures `syscall` at 270 names in common out of 992/2,186/1,899 and `log/syslog` as
#     exporting nothing at all on Windows, so a ref/ assembly would have to be either a synthesised
#     intersection (a build artifact nothing in this repository produces) or one flavor again, which
#     is what lib/ already is, minus the fallback. section 11 records that seam as open.
#
# The reference flavor is the FIRST entry below, and it is Windows: that keeps the compile surface a
# consumer binds against exactly the one today's single-platform packages present, and it matches section 11's
# "let lib/<tfm>/syscall.dll carry the host-of-record flavor".
#
# DEPENDENCIES ARE PER TARGET FRAMEWORK, NEVER PER RID (section 9). A package whose imports differ by GOOS --
# 21 of them carry conditioned <ProjectReference> groups -- therefore declares the UNION of its
# flavors' dependencies, and the merge below computes that union. Both sets restore everywhere; only
# the RID-matched assemblies ever load.
Add-Type -AssemblyName System.IO.Compression.FileSystem

# --- nupkg surgery helpers ------------------------------------------------------------------------
# A .nupkg is an ordinary zip, so the merge below is entry-level: no repack, no re-sign, no NuGet
# authoring API. It runs at PACK time, before the offline signing step a release performs, so it can
# never invalidate a signature.
function Add-GoZipEntry([System.IO.Compression.ZipArchive]$Zip, [string]$Name, [byte[]]$Bytes) {
    if ($Zip.GetEntry($Name)) { throw "Package already contains an entry named $Name" }
    $entry = $Zip.CreateEntry($Name, [System.IO.Compression.CompressionLevel]::Optimal)
    $s = $entry.Open()
    try { $s.Write($Bytes, 0, $Bytes.Length) } finally { $s.Dispose() }
}

function Get-GoZipEntryText([System.IO.Compression.ZipArchiveEntry]$Entry) {
    $ms = New-Object System.IO.MemoryStream
    $s = $Entry.Open()
    try { $s.CopyTo($ms) } finally { $s.Dispose() }
    # Decoding with a BOM-less UTF8Encoding leaves any byte-order mark in the string as a leading
    # U+FEFF, so re-encoding with the same object reproduces the original preamble exactly rather
    # than silently adding or dropping one.
    return (New-Object System.Text.UTF8Encoding($false)).GetString($ms.ToArray())
}

function Set-GoZipEntryText([System.IO.Compression.ZipArchive]$Zip, [string]$Name, [string]$Text) {
    $existing = $Zip.GetEntry($Name)
    if ($existing) { $existing.Delete() }
    Add-GoZipEntry $Zip $Name ((New-Object System.Text.UTF8Encoding($false)).GetBytes($Text))
}

function Test-GoXmlWhitespace($Node) {
    if (-not $Node) { return $false }
    if (@('Whitespace', 'SignificantWhitespace') -contains [string]$Node.NodeType) { return $true }
    return ([string]$Node.NodeType -eq 'Text' -and $Node.Value -match '^\s*$')
}

# Union the flavor's <dependencies> into the base .nuspec's. NuGet declares dependencies per target
# framework only -- never per RID (section 9) -- so a package whose imports differ by GOOS must declare every
# flavor's, and let the RID decide which assemblies actually load. Matching is by id: the version is
# one value across the whole release, so two flavors can only ever disagree about PRESENCE.
function Merge-GoNuspecDependencies([string]$BaseText, [string]$FlavorText, [string]$Id, [string]$Rid) {
    # `dotnet pack` writes the .nuspec with a UTF-8 BOM, which Get-GoZipEntryText deliberately keeps
    # as a leading U+FEFF so the rewrite reproduces it. XmlDocument.LoadXml takes a STRING, where a
    # U+FEFF is an ordinary character and not a legal document prefix ("Data at the root level is
    # invalid. Line 1, position 1."), so it is peeled off here and put back on the way out.
    $bom = ''
    if ($BaseText.Length -and $BaseText[0] -eq [char]0xFEFF) { $bom = [string][char]0xFEFF; $BaseText = $BaseText.Substring(1) }
    if ($FlavorText.Length -and $FlavorText[0] -eq [char]0xFEFF) { $FlavorText = $FlavorText.Substring(1) }

    $baseDoc = New-Object System.Xml.XmlDocument
    $baseDoc.PreserveWhitespace = $true
    $baseDoc.LoadXml($BaseText)

    $flavorDoc = New-Object System.Xml.XmlDocument
    $flavorDoc.PreserveWhitespace = $true
    $flavorDoc.LoadXml($FlavorText)

    # local-name() throughout: the .nuspec's default namespace varies by schema revision and none of
    # this cares which one it is.
    $flavorDeps = $flavorDoc.SelectSingleNode("//*[local-name()='dependencies']")
    if (-not $flavorDeps) { return ($bom + $BaseText) }

    $baseDeps = $baseDoc.SelectSingleNode("//*[local-name()='dependencies']")
    if (-not $baseDeps) {
        # The reference flavor declares nothing and this one does. Import the whole block rather than
        # dropping it -- a dependency that exists on only one platform is exactly the case (a) exists for.
        $metadata = $baseDoc.SelectSingleNode("//*[local-name()='metadata']")
        if (-not $metadata) { throw "No <metadata> in the .nuspec of $Id" }
        [void]$metadata.AppendChild($baseDoc.ImportNode($flavorDeps, $true))
        return ($bom + $baseDoc.OuterXml)
    }

    # Modern `dotnet pack` always emits <group targetFramework=...>; the flat form is handled so this
    # cannot quietly become a no-op if that ever changes.
    $flavorGroups = @($flavorDeps.SelectNodes("*[local-name()='group']"))
    if ($flavorGroups.Count -eq 0) { $flavorGroups = @($flavorDeps) }

    foreach ($fg in $flavorGroups) {
        $tfm = ''
        if ($fg.Attributes -and $fg.Attributes['targetFramework']) { $tfm = $fg.Attributes['targetFramework'].Value }

        $baseGroups = @($baseDeps.SelectNodes("*[local-name()='group']"))
        if ($baseGroups.Count -eq 0) { $baseGroups = @($baseDeps) }

        $bg = $null
        foreach ($g in $baseGroups) {
            $gTfm = ''
            if ($g.Attributes -and $g.Attributes['targetFramework']) { $gTfm = $g.Attributes['targetFramework'].Value }
            if ($gTfm -eq $tfm) { $bg = $g; break }
        }

        if (-not $bg) { [void]$baseDeps.AppendChild($baseDoc.ImportNode($fg, $true)); continue }

        $existing = @($bg.SelectNodes("*[local-name()='dependency']"))
        $have = @($existing | ForEach-Object { $_.Attributes['id'].Value })
        $anchor = $null
        if ($existing.Count) { $anchor = $existing[$existing.Count - 1] }

        foreach ($fd in @($fg.SelectNodes("*[local-name()='dependency']"))) {
            if ($have -contains $fd.Attributes['id'].Value) { continue }

            $imported = $baseDoc.ImportNode($fd, $true)
            if ($anchor -and (Test-GoXmlWhitespace $anchor.PreviousSibling)) {
                # Reproduce the indentation of the line above, so the merged .nuspec stays readable.
                $ws = $anchor.PreviousSibling.CloneNode($true)
                [void]$bg.InsertAfter($ws, $anchor)
                [void]$bg.InsertAfter($imported, $ws)
            }
            else {
                [void]$bg.AppendChild($imported)
            }
            $anchor = $imported
        }
    }

    return ($bom + $baseDoc.OuterXml)
}

# RID -> $(GoTargetOS). ORDER IS SIGNIFICANT: the first entry is the reference flavor (see above).
# Increment 5 adds macOS here, and only here.
$ridFlavors = [ordered]@{
    'win-x64'   = 'windows'
    'linux-x64' = 'linux'
}
$referenceRid = @($ridFlavors.Keys)[0]

if ($SkipBuild) {
    throw ("-SkipBuild cannot produce a multiplatform release. The RID-specific assemblies come from one " +
           "build pass per RID ($(@($ridFlavors.Keys) -join ', ')), each with a different `$(GoTargetOS), " +
           "so no single on-disk build holds them all. Re-run without -SkipBuild.")
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
Get-ChildItem $OutDir -Filter *.nupkg -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

# --- Which packages need RID-specific assets, derived from the corpus itself ----------------------
# Never a hardcoded list. Layout L3's own rule, read back off the tree: a directory named after a GOOS
# that holds NO project file is a per-GOOS SOURCE folder, and its parent is a platform-varying package.
# The "no project file" test is what keeps `internal/syscall/windows` -- a real package whose own name
# is a GOOS -- from being read as `internal/syscall`'s Windows variants, exactly as section 8 specifies for
# the converter's layout-adoption rule.
#
# Every such package ships RID-specific assets whether or not this RID PAIR happens to change its
# emission (a package varying only on darwin, say, produces identical win/linux assemblies). Shipping
# by structure rather than by a byte compare keeps the package shape a predictable function of the
# corpus; the byte compare below is the verification, not the decision.
$coreDir = Join-Path $src 'core'
$goosNames = @('windows', 'linux', 'darwin')
$ridSplitIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

foreach ($dir in Get-ChildItem $coreDir -Recurse -Directory) {
    if ($goosNames -notcontains $dir.Name) { continue }
    if (@(Get-ChildItem $dir.FullName -Filter *.csproj -File).Count -gt 0) { continue }

    $projs = @(Get-ChildItem $dir.Parent.FullName -Filter *.csproj -File | Where-Object { $_.Name -notlike '*.tests.csproj' })
    if ($projs.Count -ne 1) { throw "Expected exactly one library project beside per-GOOS folder $($dir.FullName); found $($projs.Count)" }

    $projText = [System.IO.File]::ReadAllText($projs[0].FullName)
    if ($projText -notmatch '<AssemblyName>([^<]+)</AssemblyName>') { throw "No <AssemblyName> in $($projs[0].FullName)" }
    [void]$ridSplitIds.Add("go.$($Matches[1])")
}

Write-Step "Layout L3: $($ridSplitIds.Count) package(s) carry per-GOOS sources -> RID-specific assemblies"

# --- One build + pack pass per RID ---------------------------------------------------------------
$flavorRoot = Join-Path $OutDir '_flavors'
if (Test-Path $flavorRoot) { Remove-Item $flavorRoot -Recurse -Force }

# REVERSED deliberately, so the REFERENCE flavor is the last pass. Every pass writes the same
# bin\/obj\, so whichever runs last is what a developer's tree is left holding; ending on the
# reference flavor leaves it in exactly the state a plain property-absent build produces, instead of
# silently leaving Linux assemblies behind for the next local run to pick up.
$buildOrder = @($ridFlavors.Keys)
[array]::Reverse($buildOrder)

foreach ($rid in $buildOrder) {
    $goos = $ridFlavors[$rid]
    $flavorOut = Join-Path $flavorRoot $rid
    New-Item -ItemType Directory -Force $flavorOut | Out-Null

    # --no-incremental on EVERY pass, for two independent reasons. (1) The passes share one obj\/bin\,
    # and what differs between them is the <Compile> ITEM SET, not any source timestamp -- a clean
    # compile is the cheap way to be certain pass N never inherits pass N-1's assembly. (2) The flag is
    # not byte-neutral (section 12 increment 3.5's second measurement trap), so a byte comparison between the
    # flavors -- which this script performs below -- is only an instrument if it is held constant.
    #
    # UseSharedCompilation=false is a MEASURED necessity here, not the usual concurrency hygiene.
    # go2cs-gen runs inside the compiler, and with the shared VBCSCompiler every project's generator
    # work funnels through one server process: the same clean Release build of this solution measures
    # ~160 s with csc per project and had not finished after 14 minutes without it (one core busy, 24
    # MSBuild nodes idle). Two RID passes make that the difference between a 6-minute release and an
    # hour-long one.
    Write-Step "[$rid] Building $Configuration at -p:GoTargetOS=$goos (compiles the whole stdlib; several minutes)"
    & dotnet build $slnx -c $Configuration -p:GoTargetOS=$goos -p:GeneratePackageOnBuild=false --no-incremental -p:UseSharedCompilation=false --nologo -v m
    if ($LASTEXITCODE -ne 0) { throw "[$rid] dotnet build failed ($LASTEXITCODE) at -p:GoTargetOS=$goos -- fix build errors before packing" }

    # go2cs-gen is GoTargetOS-neutral, yet under the FULL script's solution build its output copy has
    # been observed missing at pack time (3 of 3 release runs, R 2026-08-23) while the identical build
    # invoked in isolation produces it (3 of 3 probes) -- an unrooted solution-build race. Assert and
    # repair deterministically before the --no-build pack: the direct build is a cheap no-op when the
    # output already exists, and pack cannot proceed without it. Root-cause is boarded (the
    # release-machinery hardening item); this is its assert half landed on the release's critical path.
    $genOut = Join-Path $PSScriptRoot 'gen/go2cs-gen/bin' | Join-Path -ChildPath $Configuration | Join-Path -ChildPath 'netstandard2.0'
    if (-not (Test-Path (Join-Path $genOut 'go2cs-gen.dll'))) {
        Write-Step "[$rid] go2cs-gen output missing after the solution build -- repairing with a direct project build"
        & dotnet build (Join-Path $PSScriptRoot 'gen/go2cs-gen/go2cs-gen.csproj') -c $Configuration -p:UseSharedCompilation=false --nologo -v m
        if ($LASTEXITCODE -ne 0) { throw "[$rid] go2cs-gen repair build failed ($LASTEXITCODE)" }
        if (-not (Test-Path (Join-Path $genOut 'go2cs-gen.dll'))) { throw "[$rid] go2cs-gen output still missing after a direct build -- investigate before packing" }
    }

    # --no-build packs exactly what the pass above produced; the same -p:GoTargetOS is required here
    # too, because it selects the conditioned <ProjectReference> groups the .nuspec is derived from.
    Write-Step "[$rid] Packing -> $flavorOut"
    & dotnet pack $slnx -c $Configuration -o $flavorOut -p:GoTargetOS=$goos -p:GeneratePackageOnBuild=false --no-build --nologo -v m
    if ($LASTEXITCODE -ne 0) { throw "[$rid] dotnet pack failed ($LASTEXITCODE)" }
}

# --- Asset merge ----------------------------------------------------------------------------------
function Read-GoPackageFacts([string]$Path) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $nuspec = @($zip.Entries | Where-Object { $_.FullName -notlike '*/*' -and $_.FullName -like '*.nuspec' })
        if ($nuspec.Count -ne 1) { throw "Expected exactly one root .nuspec in $Path; found $($nuspec.Count)" }

        $reader = New-Object System.IO.StreamReader($nuspec[0].Open())
        try { $text = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($text -notmatch '<id>([^<]+)</id>') { throw "No <id> in the .nuspec of $Path" }
        $id = $Matches[1]

        # Hash the compile/runtime payload only. README.md, VALIDATION.md, the icons and the .nuspec
        # are flavor-independent by construction, and the OPC bookkeeping parts (.psmdcp) carry a
        # freshly minted identifier on every pack, so including them would make every comparison differ.
        $sha = [System.Security.Cryptography.SHA256]::Create()
        $lib = @{}
        $size = @{}
        try {
            foreach ($e in $zip.Entries) {
                if ($e.FullName -notlike 'lib/*') { continue }
                $size[$e.FullName] = $e.Length
                $s = $e.Open()
                try { $lib[$e.FullName] = [BitConverter]::ToString($sha.ComputeHash($s)) } finally { $s.Dispose() }
            }
        } finally { $sha.Dispose() }

        return [pscustomobject]@{ Id = $id; Path = $Path; Lib = $lib; Size = $size }
    }
    finally { $zip.Dispose() }
}

Write-Step "Reading packed flavors"
$facts = @{}
foreach ($rid in $ridFlavors.Keys) {
    $byId = @{}
    foreach ($f in Get-ChildItem (Join-Path $flavorRoot $rid) -Filter *.nupkg) {
        $fact = Read-GoPackageFacts $f.FullName
        if ($byId.ContainsKey($fact.Id)) { throw "[$rid] Two .nupkg claim package id $($fact.Id)" }
        $byId[$fact.Id] = $fact
    }
    $facts[$rid] = $byId
    Write-Step "  [$rid] $($byId.Count) package(s)"
}

$refFacts = $facts[$referenceRid]
$otherRids = @($ridFlavors.Keys | Where-Object { $_ -ne $referenceRid })

# The flavors must agree on the package-ID SET. They do today because every project is in the union
# solution and a platform-exclusive package still builds (to an assembly with no types) everywhere --
# but that is a property of the corpus, not a guarantee, and a package that appeared in only one
# flavor would otherwise be published as a silently single-platform package.
foreach ($rid in $otherRids) {
    $missing = @($refFacts.Keys | Where-Object { -not $facts[$rid].ContainsKey($_) })
    $extra = @($facts[$rid].Keys | Where-Object { -not $refFacts.ContainsKey($_) })
    if ($missing.Count -or $extra.Count) {
        throw "[$rid] package-id set differs from [$referenceRid]: $($missing.Count) missing ($($missing -join ', ')), $($extra.Count) extra ($($extra -join ', '))"
    }
}

# Verification. A package the corpus says is platform-neutral should produce an EQUIVALENT assembly
# under every flavor -- that is section 13.1's finding, re-checked here against the artifacts actually
# being shipped rather than against a build tree. One that does not is promoted to RID-specific
# (correctness first) and reported loudly. The converse -- an L3 package whose flavors happen to match
# for this RID pair -- is only a measurement, and is reported as one.
#
# WHAT "EQUIVALENT" HAS TO MEAN HERE, and why a byte compare cannot be the test. Roslyn's
# deterministic identity fields -- the PE timestamp, the MVID, the PDB id and the PDB content
# checksum -- are hashes OF THE COMPILATION, and they CASCADE: an assembly whose identity moved shifts
# every dependent's identity too, even when every source file and every semantic byte is unchanged.
# Measured on this corpus (increment 4): a byte compare called 216 of 270 platform-neutral packages
# "different", and the difference was ~72 bytes of a ~340 KB assembly in four runs, with the assembly
# LENGTH unchanged -- the same signature section 12's increment-3 proof recorded. Meanwhile the same
# comparison between two SAME-flavor packs reports every assembly byte-identical, so the build is
# reproducible and the cascade is the whole explanation.
#
# So the discriminator is the entry set plus the assembly LENGTH. Identity fields are fixed-size, so a
# pure identity difference can never move it: this test has no false positives, which is what a
# release gate needs. It is a smoke alarm, not a semantic digest -- section 13.1's digest remains the
# instrument that answers the IL question in full, and it is a per-increment measurement rather than a
# per-release one.
$promoted = @()
$variesOnThisPair = 0
$identityOnly = 0
$l3Count = $ridSplitIds.Count   # captured before any promotion below can inflate it

foreach ($id in @($refFacts.Keys | Sort-Object)) {
    $material = $false      # entry set or assembly length differs: a real difference
    $anyByte = $false       # any byte differs at all: includes the identity cascade above

    foreach ($rid in $otherRids) {
        $a = $refFacts[$id]
        $b = $facts[$rid][$id]
        if ($a.Lib.Count -ne $b.Lib.Count) { $material = $true; $anyByte = $true; break }

        foreach ($k in $a.Lib.Keys) {
            if (-not $b.Lib.ContainsKey($k)) { $material = $true; $anyByte = $true; break }
            if ($a.Size[$k] -ne $b.Size[$k]) { $material = $true }
            if ($a.Lib[$k] -ne $b.Lib[$k]) { $anyByte = $true }
        }
        if ($material) { break }
    }

    if ($ridSplitIds.Contains($id)) {
        if ($anyByte) { $variesOnThisPair++ }
    }
    elseif ($material) {
        $promoted += $id
        [void]$ridSplitIds.Add($id)
    }
    elseif ($anyByte) { $identityOnly++ }
}

Write-Step ("Flavor comparison across {0}: {1} of {2} L3 package(s) differ" -f `
            ($ridFlavors.Keys -join '/'), $variesOnThisPair, $l3Count)
Write-Step ("  of {0} platform-neutral package(s): {1} differ materially, {2} differ only in the deterministic-identity fields (expected -- see the note above)" -f `
            ($refFacts.Count - $l3Count), $promoted.Count, $identityOnly)

if ($promoted.Count) {
    Write-Warning ("These packages carry NO per-GOOS sources yet their assemblies differ in LENGTH between flavors, " +
                   "which the identity cascade cannot explain, so they are being shipped with RID-specific assets: " +
                   "$($promoted -join ', '). Investigate before publishing -- the corpus has most likely gained a " +
                   "platform axis this script's L3 derivation cannot see " +
                   "(see docs\phase4\DESIGN-multiplatform-corpus.md section 13.1).")
}

# The merge itself. A neutral package is copied VERBATIM from the reference flavor -- byte for byte the
# package today's single-pass release produced -- so the Windows lane cannot regress through this
# script. Only a RID-specific package is rewritten.
Write-Step "Merging RID assets -> $OutDir"
$merged = 0
$copied = 0

foreach ($id in @($refFacts.Keys | Sort-Object)) {
    $refPath = $refFacts[$id].Path
    $target = Join-Path $OutDir (Split-Path $refPath -Leaf)
    Copy-Item $refPath $target -Force

    if (-not $ridSplitIds.Contains($id)) { $copied++; continue }

    $zip = [System.IO.Compression.ZipFile]::Open($target, 'Update')
    try {
        # The reference flavor's own assets move from lib/ to its RID folder as well as staying in
        # lib/. Duplicating rather than relying on "no RID matched, fall back to lib/" is deliberate:
        # it states each shipped RID's flavor explicitly in the package, so which assembly a RID gets
        # never depends on which flavor lib/ happens to carry.
        $libEntries = @($zip.Entries | Where-Object { $_.FullName -like 'lib/*' })
        foreach ($e in $libEntries) {
            $ms = New-Object System.IO.MemoryStream
            $s = $e.Open()
            try { $s.CopyTo($ms) } finally { $s.Dispose() }
            Add-GoZipEntry $zip ("runtimes/$referenceRid/" + $e.FullName) $ms.ToArray()
        }

        $nuspecEntry = @($zip.Entries | Where-Object { $_.FullName -notlike '*/*' -and $_.FullName -like '*.nuspec' })[0]
        $nuspecText = Get-GoZipEntryText $nuspecEntry

        foreach ($rid in $otherRids) {
            $src2 = [System.IO.Compression.ZipFile]::OpenRead($facts[$rid][$id].Path)
            try {
                foreach ($e in @($src2.Entries | Where-Object { $_.FullName -like 'lib/*' })) {
                    $ms = New-Object System.IO.MemoryStream
                    $s = $e.Open()
                    try { $s.CopyTo($ms) } finally { $s.Dispose() }
                    Add-GoZipEntry $zip ("runtimes/$rid/" + $e.FullName) $ms.ToArray()
                }

                $flavorNuspec = @($src2.Entries | Where-Object { $_.FullName -notlike '*/*' -and $_.FullName -like '*.nuspec' })[0]
                $nuspecText = Merge-GoNuspecDependencies $nuspecText (Get-GoZipEntryText $flavorNuspec) $id $rid
            }
            finally { $src2.Dispose() }
        }

        Set-GoZipEntryText $zip $nuspecEntry.FullName $nuspecText
    }
    finally { $zip.Dispose() }

    $merged++
}

Write-Step "Merged $merged RID-specific package(s); copied $copied platform-neutral package(s) verbatim"

$pkgs = @(Get-ChildItem $OutDir -Filter *.nupkg)
Write-Step "Packed $($pkgs.Count) package(s)"
if ($pkgs.Count -eq 0) { throw "No .nupkg produced in $OutDir" }

# --- Push gate ----------------------------------------------------------------------------------
if (-not $Push) {
    Write-Host ""
    Write-Host "Pack-only (default). Inspect $OutDir, then re-run with -Push to publish." -ForegroundColor Yellow
    exit 0
}

if (-not $ApiKey) { throw "-Push requires an API key: pass -ApiKey or set `$env:NUGET_API_KEY." }

# Publish go.lib and go.gen first (dependencies of every stdlib package). --skip-duplicate makes a
# re-run idempotent; nuget.org indexes asynchronously so strict ordering is a nicety, not required.
$deps = @($pkgs | Where-Object { $_.Name -match '^go\.(lib|gen)\.' })
$rest = @($pkgs | Where-Object { $_.Name -notmatch '^go\.(lib|gen)\.' })
$ordered = $deps + $rest

Write-Step "Pushing $($ordered.Count) package(s) to $Source"
$pushed = 0
foreach ($p in $ordered) {
    if ($PSCmdlet.ShouldProcess($p.Name, "nuget push -> $Source")) {
        & dotnet nuget push $p.FullName --source $Source --api-key $ApiKey --skip-duplicate
        if ($LASTEXITCODE -ne 0) { throw "push failed for $($p.Name) ($LASTEXITCODE)" }
        $pushed++
    }
}
Write-Step "Done. Pushed $pushed package(s) at version $fullVersion."
