<#
.SYNOPSIS
    Packs the go2cs converted Go standard library (plus the go.lib runtime and go.gen source
    generator) as NuGet packages from a fresh Release build, and optionally pushes them to a feed.

.DESCRIPTION
    Targets ONLY src\go2cs-stdlib.slnx -- the generated standard-library solution, which contains
    the ~301 converted stdlib libraries, the hand-owned ones (unsafe, testing) AND the two shared
    infrastructure projects core\golib (go.lib) and gen\go2cs-gen (go.gen). It deliberately never
    packs src\go2cs.slnx, whose behavioral-test and example projects are not publishable.

    All packages share one version, sourced from src\version.props: <GoStdLibVersion>.<GoBuildNumber>
    (base tracks the converted Go release, e.g. 1.23.1; the 4th part is the build/publish counter).
    -Push increments GoBuildNumber by default so every publish is a new version (commit version.props
    afterward to record the release). See -BumpBuild to force or suppress the bump.

    PUBLICATION ALSO FREEZES THE PROOF. Before anything is built, docs\validation\current\ is copied
    to docs\validation\<version>\ (write-once) and the version-pinned validation badge links in every
    src\core\*\README.md are retargeted at it, so a published package's green badge, its proof link
    and the VALIDATION.md it packs all describe the exact binary being pushed. Commit the snapshot,
    the retargeted READMEs and version.props together, and tag the commit nuget-<version>.

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
    Directory to collect the .nupkg files. Defaults to src\artifacts\nupkg.

.PARAMETER SkipBuild
    Skip the Release build and pack the output already on disk (e.g. a build from Visual Studio).
    By default the script does a fresh Release build first and aborts if it fails.

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

# --- Validation proof snapshot + badge retarget ---------------------------------------------------
# Every validated package's README carries a green badge whose link is VERSION-PINNED, and every
# validated package packs that same page as VALIDATION.md. Both point at docs\validation\<version>\,
# which is written ONCE here, at publication, and never rewritten: the proof shown for go.io 1.23.1.3
# stays forever the proof as of that binary, while docs\validation\current\ keeps moving.
#
# Order matters. The snapshot is taken BEFORE the build so the .csproj Exists() guards see the files
# they are about to pack, and the READMEs are retargeted in the same breath so the badge, the link
# and the packed sheet are the one version being published.
$repoRoot = Split-Path $src -Parent
$validationDir = Join-Path $repoRoot 'docs\validation'
$currentProofs = Join-Path $validationDir 'current'
$versionProofs = Join-Path $validationDir $fullVersion

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
    elseif ($PSCmdlet.ShouldProcess($versionProofs, "snapshot docs\validation\current")) {
        New-Item -ItemType Directory -Force $versionProofs | Out-Null
        Copy-Item (Join-Path $currentProofs '*.md') $versionProofs -Force
        Write-Step "Froze $((Get-ChildItem $versionProofs -Filter *.md).Count) validation proof page(s) at docs\validation\$fullVersion"
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

    Write-Step "Verified $verified green badge(s) against the frozen $fullVersion proof pages"
}

# --- Fresh Release pack -------------------------------------------------------------------------
New-Item -ItemType Directory -Force $OutDir | Out-Null
Get-ChildItem $OutDir -Filter *.nupkg -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

# Fresh Release build first (default) so a compile error fails the run BEFORE anything is packed or
# pushed. -SkipBuild packs whatever is already built on disk (e.g. a Release build from Visual Studio).
# The explicit GeneratePackageOnBuild=false is a guard: csprojs default it off, and this keeps a
# future regression from scattering .nupkg into every project's bin\ during the release build.
if ($SkipBuild) {
    Write-Step "Skipping build (-SkipBuild): packing existing $Configuration output"
} else {
    Write-Step "Building $Configuration (compiles the whole stdlib; several minutes)"
    & dotnet build $slnx -c $Configuration -p:GeneratePackageOnBuild=false --nologo -v m
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE) -- fix build errors before packing" }
}

# Pack from the built output. --no-build honors -SkipBuild and avoids a redundant second build;
# GeneratePackageOnBuild=false leaves packaging to 'dotnet pack' -> $OutDir only (mirrors deploy-core.ps1).
Write-Step "Packing $Configuration -> $OutDir"
& dotnet pack $slnx -c $Configuration -o $OutDir -p:GeneratePackageOnBuild=false --no-build --nologo -v m
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed ($LASTEXITCODE)" }

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
