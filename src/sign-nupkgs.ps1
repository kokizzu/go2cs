#Requires -Version 5.1
<#
.SYNOPSIS
    Author-sign every *.nupkg in a folder with the code-signing certificate held in this
    machine's certificate store (typically propagated from a cryptographic card).

.DESCRIPTION
    The signing half of the offline-signing release flow: `release-nuget.bat` Phase 1 packs,
    this signs, Phase 2 pushes. It signs with `dotnet nuget sign` -- part of the SDK the
    repository already requires -- rather than a separately-installed `nuget.exe`.

    CENSUS BY DEFAULT, like the repository's other instruments: a bare run reports what it
    WOULD sign, verifies the certificate is reachable, and signs nothing. -Apply signs.

    THE FINGERPRINT IS SHA-256, NOT THE THUMBPRINT WINDOWS SHOWS YOU. The certificate dialog's
    "Thumbprint" field is SHA-1 (40 hex characters); NuGet requires SHA-256/384/512 (64+
    characters) and rejects the shorter value. This script detects a SHA-1-length fingerprint
    and prints the SHA-256 of the matching certificate rather than failing 307 times in a row.

.PARAMETER PackageDir
    Directory holding the *.nupkg. Defaults to src\artifacts\nupkg (release-nuget's own output).

.PARAMETER Fingerprint
    SHA-256 fingerprint of the signing certificate. Defaults to $env:NuGetCertFingerprint.

.PARAMETER Timestamper
    RFC 3161 timestamping URL. A timestamp is what keeps a signature valid after the
    certificate expires, so it is required, not optional.

.PARAMETER Apply
    Actually sign. Without it the run is a census.

.PARAMETER Overwrite
    Re-sign packages that already carry a signature.

.EXAMPLE
    .\sign-nupkgs.ps1
    Census: what would be signed, and whether the certificate is reachable.

.EXAMPLE
    .\sign-nupkgs.ps1 -Apply
    Sign every package in the default folder.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $PackageDir,
    [string] $Fingerprint = $env:NuGetCertFingerprint,
    [string] $Timestamper = 'http://timestamp.digicert.com',
    [switch] $Apply,
    [switch] $Overwrite
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_paths.ps1')

if (-not $PackageDir) { $PackageDir = Join-Path $SrcRoot 'artifacts/nupkg' }

function Write-Step([string] $Message) { Write-Host "==> $Message" -ForegroundColor Cyan }
function Write-Bad([string] $Message)  { Write-Host $Message -ForegroundColor Red }

# ---- the certificate ------------------------------------------------------------------------
# Resolved BEFORE any package is touched. A signing run that discovers its certificate is
# unreachable on package 200 of 307 has wasted the operator's card session and left a
# half-signed folder, which is the state this check exists to prevent.

if (-not $Fingerprint) {
    Write-Bad 'No certificate fingerprint. Pass -Fingerprint or set $env:NuGetCertFingerprint.'
    Write-Host ''
    Write-Host 'To find it (with the card inserted and its certificate propagated to the store):' -ForegroundColor DarkGray
    Write-Host '  Get-ChildItem Cert:\CurrentUser\My | Where-Object {' -ForegroundColor DarkGray
    Write-Host '      $_.EnhancedKeyUsageList.ObjectId -contains "1.3.6.1.5.5.7.3.3" } |' -ForegroundColor DarkGray
    Write-Host '    ForEach-Object { $_.GetCertHashString("SHA256") }' -ForegroundColor DarkGray
    exit 1
}

$Fingerprint = $Fingerprint.Trim().Replace(' ', '').ToUpperInvariant()

# SHA-1 is 40 hex characters and is exactly what the Windows certificate dialog labels
# "Thumbprint" -- the single most likely wrong value to arrive here. Name it, and resolve the
# right one rather than making the operator go find it.
if ($Fingerprint.Length -eq 40) {
    Write-Bad "That is a SHA-1 thumbprint (40 characters). NuGet requires SHA-256 (64)."
    $match = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $Fingerprint }

    if ($match) {
        Write-Host ''
        Write-Host "  The SHA-256 fingerprint of that certificate is:" -ForegroundColor Yellow
        Write-Host "    $($match[0].GetCertHashString('SHA256'))" -ForegroundColor Yellow
        Write-Host ''
        Write-Host "  Set it with:  [Environment]::SetEnvironmentVariable('NuGetCertFingerprint','$($match[0].GetCertHashString('SHA256'))','User')" -ForegroundColor DarkGray
    }
    exit 1
}

if ($Fingerprint.Length -lt 64 -or $Fingerprint -notmatch '^[0-9A-F]+$') {
    Write-Bad "Fingerprint is not a SHA-256/384/512 hex value: '$Fingerprint'"
    exit 1
}

$cert = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
    Where-Object { $_.GetCertHashString('SHA256') -eq $Fingerprint } | Select-Object -First 1

if (-not $cert) {
    Write-Bad "No certificate with that SHA-256 fingerprint in CurrentUser\My or LocalMachine\My."
    Write-Host '  If the certificate lives on a cryptographic card, it reaches the store only when the' -ForegroundColor DarkGray
    Write-Host '  card is INSERTED and its certificate has propagated (re-insert the card, or register it' -ForegroundColor DarkGray
    Write-Host '  through the card manager). `certutil -scinfo -silent` reports what the card itself holds.' -ForegroundColor DarkGray
    exit 1
}

Write-Step "Certificate: $($cert.Subject -replace ',.*','')"
Write-Host "    issuer     : $($cert.Issuer -replace ',.*','')"
Write-Host "    expires    : $($cert.NotAfter.ToString('yyyy-MM-dd'))"
Write-Host "    private key: $(if ($cert.HasPrivateKey) { 'present' } else { 'NOT PRESENT -- signing will fail' })"

if (-not $cert.HasPrivateKey) {
    Write-Bad 'The certificate has no usable private key in this session (card removed?).'
    exit 1
}

$daysLeft = ($cert.NotAfter - (Get-Date)).Days
if ($daysLeft -lt 30) { Write-Host "    ⚠ expires in $daysLeft day(s)" -ForegroundColor Yellow }

# ---- the packages ---------------------------------------------------------------------------

if (-not (Test-Path $PackageDir)) { Write-Bad "No such folder: $PackageDir"; exit 1 }

$packages = @(Get-ChildItem (Join-Path $PackageDir '*.nupkg') -File | Sort-Object Name)

if ($packages.Count -eq 0) { Write-Bad "No .nupkg files in $PackageDir"; exit 1 }

Write-Step "$($packages.Count) package(s) in $PackageDir"

if (-not $Apply) {
    Write-Host ''
    Write-Host "CENSUS ONLY -- nothing was signed. Re-run with -Apply to sign." -ForegroundColor Yellow
    Write-Host "  first : $($packages[0].Name)"
    Write-Host "  last  : $($packages[-1].Name)"
    Write-Host ''
    Write-Host "  A card session signs them one at a time; if the card prompts for a PIN per" -ForegroundColor DarkGray
    Write-Host "  signature rather than per session, $($packages.Count) prompts is what -Apply means." -ForegroundColor DarkGray
    Write-Host "  Test the prompt behaviour on a COPY of two packages before committing to a full run." -ForegroundColor DarkGray
    exit 0
}

# ---- signing --------------------------------------------------------------------------------

$signArgs = @(
    '--certificate-fingerprint', $Fingerprint
    '--certificate-store-name', 'My'
    '--certificate-store-location', 'CurrentUser'
    '--timestamper', $Timestamper
)
if ($Overwrite) { $signArgs += '--overwrite' }

$signed = 0
$failed = @()
$started = Get-Date

foreach ($pkg in $packages) {
    if (-not $PSCmdlet.ShouldProcess($pkg.Name, 'sign')) { continue }

    Write-Host ("  [{0,3}/{1}] {2}" -f ($signed + $failed.Count + 1), $packages.Count, $pkg.Name) -NoNewline

    $out = & dotnet nuget sign $pkg.FullName @signArgs 2>&1

    if ($LASTEXITCODE -eq 0) {
        $signed++
        Write-Host '  signed' -ForegroundColor Green
    }
    else {
        $failed += $pkg.Name
        Write-Host '  FAILED' -ForegroundColor Red
        $out | Select-Object -Last 3 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkGray }

        # A card yanked mid-run, or a session that expired, fails every remaining package the
        # same way. Three consecutive failures is the shape of that, and burning the rest of a
        # 300-package run to re-learn it helps nobody.
        if ($failed.Count -ge 3 -and $signed -eq 0) {
            Write-Bad 'Three failures with no successes -- stopping. Check the card and the session.'
            break
        }
    }
}

$elapsed = [int]((Get-Date) - $started).TotalSeconds
Write-Host ''
Write-Host "signed $signed / $($packages.Count)  ($elapsed s)" -ForegroundColor $(if ($failed.Count) { 'Red' } else { 'Green' })

if ($failed.Count) {
    Write-Bad "$($failed.Count) failure(s):"
    $failed | ForEach-Object { Write-Host "    $_" }
    exit 1
}

Write-Host ''
Write-Step 'Verify a sample before pushing:'
Write-Host "  dotnet nuget verify `"$($packages[0].FullName)`"" -ForegroundColor DarkGray
exit 0
