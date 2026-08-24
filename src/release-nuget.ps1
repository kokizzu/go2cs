#Requires -Version 5.1
<#
.SYNOPSIS
    Publish a signed release of the go2cs NuGet packages, end to end, on ONE machine.

.DESCRIPTION
    Pack -> sign -> push -> record, as four phases of one ritual. It supersedes the
    offline-signing flow (`-OfflineSigning` keeps that path for a machine whose signing
    certificate lives elsewhere), and it exists because the certificate now lives HERE: the
    file-shuttle between machines was the only reason the release was ever two commands.

    THE ONLY HUMAN ACTION IS THE PIN. The Smart Card KSP caches a card PIN for the life of the
    process that unlocked it, so the whole package set signs inside ONE `dotnet nuget sign`
    invocation and the card is unlocked exactly once (measured: 6 packages, one prompt, 9 s).

    SIGNING IS MANDATORY, NOT OPTIONAL. The owner's code-signing certificate is REGISTERED with
    nuget.org (2026-08-24), and registration is an enforcement switch: nuget.org rejects any
    package pushed under that account which is not signed by a registered certificate. An
    unsigned push does not publish-with-a-warning -- it fails. That is why Phase 0 proves the
    certificate is reachable before anything is bumped, and why -OfflineSigning still signs
    rather than skipping.

    WHAT IS IRREVERSIBLE, AND WHERE. Phase 3 publishes to nuget.org, and a published version can
    be unlisted but never deleted. Everything before it is recoverable: -WhatIf stops after the
    census, a failed pack leaves only a bumped version.props (restore with
    `git checkout src/version.props` and delete the tag), and a failed sign leaves unsigned
    packages in the artifacts folder. The push is therefore gated on an explicit confirmation
    unless -Yes is passed, and the gate prints exactly what is about to become permanent.

.PARAMETER OfflineSigning
    Stop after packing and print the copy-to-signing-machine instructions, then wait -- the
    pre-2026-08-24 flow, for a machine without the signing certificate.

.PARAMETER Yes
    Skip the pre-push confirmation. For an operator who has read the census and wants the
    ritual to run through the PIN prompt and out the other side without a second keystroke.

.PARAMETER WhatIf
    Census only: verify the certificate is reachable and report what would be packed. Nothing
    is bumped, packed, signed, tagged or pushed.

.EXAMPLE
    .\release-nuget.ps1 -WhatIf
    What would happen, including whether the signing certificate is reachable.

.EXAMPLE
    .\release-nuget.ps1
    The full ritual: pack, sign (one PIN), confirm, push, then print the record-the-release
    commands.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [switch] $OfflineSigning,
    [switch] $Yes,
    [string] $OutDir
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_paths.ps1')

if (-not $OutDir) { $OutDir = Join-Path $SrcRoot 'artifacts/nupkg' }

$push = Join-Path $PSScriptRoot 'push-nuget.ps1'
$sign = Join-Path $PSScriptRoot 'sign-nupkgs.ps1'

function Write-Phase([string] $Message) {
    Write-Host ''
    Write-Host "=== $Message ===" -ForegroundColor Cyan
}
function Die([string] $Message) { Write-Host $Message -ForegroundColor Red; exit 1 }

# The sibling instruments end with `exit`, and an `exit` inside a script invoked with `&` in the
# SAME runspace terminates the HOST -- so this orchestrator died mid-preflight rather than read
# the signer's verdict, the first time Phase 0 ran. Every sibling call therefore goes through a
# CHILD PROCESS, whose exit code is a value rather than a fate. (The card PIN cache is
# unaffected: it lives in the `dotnet nuget sign` process, not in PowerShell.)
function Invoke-Sibling {
    param([string] $Script, [string[]] $Arguments, [switch] $Passthru)
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $Script @Arguments 2>&1
    $code = $LASTEXITCODE
    if ($Passthru) { $out | ForEach-Object { Write-Host $_ } }
    [pscustomobject]@{ Output = $out; ExitCode = $code }
}

# ---- Phase 0: the preconditions, all of them, before anything moves --------------------------
# Every check here is one a later phase would have hit anyway -- the point is to hit them while
# nothing has been bumped, tagged, packed or published. A release that discovers a missing API
# key AFTER minting a signed tag has made work for someone.

Write-Phase 'Phase 0: preflight'

$dirty = @(git -C $RepoRoot status --porcelain 2>$null)
if ($dirty.Count -gt 0) {
    Write-Host 'Working tree is not clean:' -ForegroundColor Red
    $dirty | Select-Object -First 8 | ForEach-Object { Write-Host "    $_" }
    Die 'A release commits version.props, a proof snapshot and retargeted badges together; unrelated changes must not ride along.'
}
Write-Host '  working tree     : clean'

$branch = (git -C $RepoRoot branch --show-current 2>$null)
Write-Host "  branch           : $branch"
if ($branch -ne 'master') { Write-Host '    (not master -- intentional?)' -ForegroundColor Yellow }

if (-not $env:NuGetCertFingerprint -and -not $OfflineSigning) {
    Die 'NuGetCertFingerprint is not set. Set it, or pass -OfflineSigning to sign elsewhere.'
}

if (-not $env:NUGET_API_KEY) {
    Die 'NUGET_API_KEY is not set -- Phase 3 would fail after the packages were already signed.'
}
Write-Host '  NUGET_API_KEY    : set'

# The certificate is proven reachable NOW rather than after a multi-minute pack, by running the
# signer's own census against whatever is currently in the artifacts folder (or an empty one --
# the certificate half of its checks runs regardless).
if (-not $OfflineSigning) {
    $probe = Invoke-Sibling -Script $sign -Arguments @('-PackageDir', $OutDir)
    $certLine = $probe.Output | Where-Object { $_ -match 'Certificate:' } | Select-Object -First 1
    if (-not $certLine) {
        $probe.Output | Select-Object -First 6 | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Die 'The signing certificate is not reachable. Is the card inserted?'
    }
    Write-Host "  signing cert     : $($certLine -replace '.*Certificate: ','')"
}

if ($WhatIfPreference) {
    Write-Phase 'CENSUS ONLY (-WhatIf)'
    Write-Host '  Preconditions pass. Nothing was bumped, packed, signed or pushed.'
    exit 0
}

# ---- Phase 1: bump, pack, freeze the proof, mint the tag -------------------------------------

Write-Phase 'Phase 1: bump version and pack Release packages'
Write-Host '  (the build number bumps UP FRONT so packed, signed and pushed packages carry ONE version)'

$pack = Invoke-Sibling -Script $push -Arguments @('-BumpBuild', '-OutDir', $OutDir) -Passthru
if ($pack.ExitCode -ne 0) {
    Write-Host ''
    Die "Phase 1 (pack) FAILED. version.props was already bumped -- either re-run (it advances again) or restore it: git checkout src/version.props, and delete the tag if one was minted."
}

$x = [xml](Get-Content -Raw (Join-Path $SrcRoot 'version.props'))
$p = $x.Project.PropertyGroup
$ver = "$($p.GoStdLibVersion).$($p.GoBuildNumber)".Trim()
Write-Host "  version          : $ver"

# ---- Phase 2: sign ---------------------------------------------------------------------------

if ($OfflineSigning) {
    Write-Phase 'Phase 2: OFFLINE signing (this machine does not sign)'
    Write-Host "  Packages are in: $OutDir"
    Write-Host ''
    Write-Host '    1. Copy *.nupkg to the signing machine'
    Write-Host '    2. Run sign-nupkgs.bat -Apply there (with NuGetCertFingerprint set)'
    Write-Host '    3. Copy the SIGNED *.nupkg back, overwriting the originals'
    Write-Host ''
    Read-Host 'Press Enter when the signed packages are back in place (Ctrl+C to stop)'
}
else {
    Write-Phase 'Phase 2: sign (ONE PIN prompt for the whole set)'
    $signRun = Invoke-Sibling -Script $sign -Arguments @('-PackageDir', $OutDir, '-Apply') -Passthru
    if ($signRun.ExitCode -ne 0) {
        Write-Host ''
        Die "Phase 2 (sign) FAILED. Nothing was published. The packages in $OutDir are unsigned or partly signed; fix the card session and re-run the signer, or restore version.props to abandon this release."
    }
}

# ---- Phase 3: push -- THE IRREVERSIBLE ONE ---------------------------------------------------

$packages = @(Get-ChildItem (Join-Path $OutDir '*.nupkg') -File)

Write-Phase 'Phase 3: publish to nuget.org'
Write-Host "  version          : $ver"
Write-Host "  packages         : $($packages.Count)"
Write-Host "  source           : https://api.nuget.org/v3/index.json"
Write-Host ''
Write-Host '  A published version can be UNLISTED but never DELETED.' -ForegroundColor Yellow

if (-not $Yes) {
    $answer = Read-Host "  Publish $($packages.Count) package(s) as $ver? (type 'publish' to proceed)"
    if ($answer -ne 'publish') { Die '  Not published. Nothing was sent; the signed packages remain in the artifacts folder.' }
}

dotnet nuget push (Join-Path $OutDir '*.nupkg') --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Die "Phase 3 (push) FAILED. The signed packages are still in $OutDir; re-run the push once resolved -- do NOT re-pack, the version is already minted and signed."
}

# ---- Phase 4: record --------------------------------------------------------------------------
# The published version, its frozen proof snapshot and the badge links pointing at it are ONE
# fact: commit them together or a published badge links a directory that is not in the
# repository. The tag already exists (Phase 1 minted it signed, before anything was packed), and
# every published README's C# Source badge links it -- until it reaches GitHub those links 404.

Write-Phase 'Phase 4: record the release'
Write-Host '  Commit the version, the frozen proof and the retargeted badges TOGETHER, then push'
Write-Host '  the commit and the tag Phase 1 already minted:'
Write-Host ''
Write-Host "    git add src/version.props docs/validation/$ver src/core" -ForegroundColor DarkGray
Write-Host "    git commit -S -m `"release: go2cs converted stdlib $ver`"" -ForegroundColor DarkGray
Write-Host "    git push && git push origin nuget-$ver" -ForegroundColor DarkGray
Write-Host ''
Write-Host "  docs/validation/$ver is FROZEN from here -- it is the proof every $ver package's"
Write-Host '  green badge links and packs as VALIDATION.md.'
exit 0
