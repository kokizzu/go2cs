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
    [switch] $Overwrite,

    # One process per package: a PIN prompt EACH time, but a per-package pass/fail line.
    # Use it to isolate a failure the batch run reported without a clear owner.
    [switch] $PerPackage
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
    Write-Host "  -Apply signs all $($packages.Count) inside ONE process, so the card is unlocked ONCE." -ForegroundColor DarkGray
    Write-Host "  (The Smart Card KSP caches a PIN per PROCESS; -PerPackage costs one prompt each.)" -ForegroundColor DarkGray
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

$started = Get-Date

# ONE PROCESS, ONE PIN. The Windows Smart Card KSP caches a card PIN for the LIFETIME OF THE
# PROCESS that unlocked it -- so signing N packages with N invocations of `dotnet nuget sign`
# costs N PIN prompts no matter what caching the card middleware offers, because each process
# starts with an empty cache. Measured on the first real signing run: two packages, two prompts.
# `dotnet nuget sign` takes multiple package paths (wildcards included) in a single invocation,
# so the whole folder signs inside ONE process and the card is unlocked ONCE.
#
# The cost of batching is granularity: a failure names the package in NuGet's own output rather
# than in a per-package progress line. That trade is worth one prompt versus three hundred, and
# -PerPackage restores the old behaviour when a specific failure needs isolating.

$glob = Join-Path $PackageDir '*.nupkg'
$failed = @()
$signed = 0

if ($PerPackage) {
    Write-Step "Signing one package per process (-PerPackage): expect a PIN prompt EACH time"

    foreach ($pkg in $packages) {
        if (-not $PSCmdlet.ShouldProcess($pkg.Name, 'sign')) { continue }

        Write-Host ("  [{0,3}/{1}] {2}" -f ($signed + $failed.Count + 1), $packages.Count, $pkg.Name) -NoNewline
        $out = & dotnet nuget sign $pkg.FullName @signArgs 2>&1

        if ($LASTEXITCODE -eq 0) { $signed++; Write-Host '  signed' -ForegroundColor Green }
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
}
else {
    Write-Step "Signing all $($packages.Count) package(s) in ONE process -- expect ONE PIN prompt"

    if ($PSCmdlet.ShouldProcess("$($packages.Count) package(s)", 'sign')) {
        $out = & dotnet nuget sign $glob @signArgs 2>&1
        $out | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

        if ($LASTEXITCODE -eq 0) { $signed = $packages.Count }
        else {
            # NuGet reports which package failed in its own output; re-deriving that here would
            # duplicate a truth the tool already told. Point at it rather than restate it.
            $failed = @("(batch failed -- see NuGet output above; re-run with -PerPackage to isolate)")
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
