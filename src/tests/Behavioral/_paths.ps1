<#
.SYNOPSIS
    Shared path/platform primitives for the behavioral-tree PowerShell instruments. Dot-source it.

.DESCRIPTION
    Four scripts under this directory each recomputed the repository roots, the converter path and the
    executable suffix from their own $PSScriptRoot, with backslash literals baked into every one. That
    made them disagree with each other and made all four wrong off Windows, in the worst possible way:
    a `\`-anchored regex or split does not ERROR on Linux, it silently matches nothing. The two sites
    that mattered most were in check-no-regression.ps1 -- the bin/obj exclusion (which would let the
    walk transpile build output) and the path-depth sort (which would collapse every path to depth 1
    and revert the deepest-first ordering that closed FALSE-GREEN route #3). See F4 in
    docs/PLAN-linux-operation.md.

    Dot-source this file to get one definition of each:

        . (Join-Path $PSScriptRoot '_paths.ps1')

    Nothing here has side effects; it defines variables and two helpers and returns.

.NOTES
    Requires PowerShell 5.1 (Windows) or PowerShell 7+ (any platform).
#>

# $IsWindows is an AUTOMATIC variable in PowerShell 6+ only. On Windows PowerShell 5.1 it does not
# exist, so a bare `if (-not $IsWindows)` reads $null -> falsey -> "not Windows", which is exactly
# backwards on the one platform where 5.1 runs. Resolve it explicitly.
$IsWindowsHost = if ($null -eq (Get-Variable -Name 'IsWindows' -ErrorAction SilentlyContinue)) { $true } else { $IsWindows }

# Executable suffix for a built .NET apphost or Go binary.
$ExeSuffix = if ($IsWindowsHost) { '.exe' } else { '' }

# Path separator regex CLASS, for patterns that must match a path boundary on either platform. Use
# this instead of a literal '\\' in any -match/-split/-notmatch over a filesystem path: Windows
# accepts both separators in practice and Linux only produces '/'.
$SepPattern = '[\\/]'

# Roots, each derived from THIS file's location so every caller agrees.
#
# NOTE the single forward-slash-joined child path rather than pwsh's multi-argument Join-Path. The
# multi-argument form is PowerShell 6+ only -- on Windows PowerShell 5.1, which is what the Windows
# lane actually runs, `Join-Path a b c` is a hard parameter-binding error. Forward slashes inside a
# single child argument are accepted by Join-Path on BOTH platforms and normalize to the host
# separator, so this form is the one that is portable in both directions.
$BehavioralRoot = $PSScriptRoot
$SrcRoot        = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$RepoRoot       = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$ConverterSrc   = Join-Path $SrcRoot 'go2cs'
$Go2csExe       = Join-Path $ConverterSrc "bin/go2cs$ExeSuffix"

# PathDepth counts a path's segments independently of which separator produced them. The behavioral
# walk sorts by this DESCENDING so a nested sub-library package is transpiled before the parent that
# reads its generated package_info.cs.
function Get-PathDepth {
    param([Parameter(Mandatory)][string] $Path)

    return ($Path -split $SepPattern | Where-Object { $_ -ne '' }).Count
}

# Renders an absolute path relative to a root, in the forward-slash form used for reporting and for
# git pathspecs. Trimming BOTH separators is deliberate: a Windows path under a root that was
# resolved with forward slashes leaves the other one behind.
function Get-RelativeDisplayPath {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Root
    )

    $relative = $Path
    if ($Path.Length -gt $Root.Length -and $Path.StartsWith($Root)) {
        $relative = $Path.Substring($Root.Length)
    }

    return $relative.TrimStart('\', '/').Replace('\', '/')
}
