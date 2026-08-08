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

    The primitives themselves now live one level up, at src\_paths.ps1, because the sweep, the deploy
    and the performance wrapper need the same ones and a src-level script should not reach into the
    test tree for them. This file dot-sources that one and adds what is behavioral-specific, so every
    variable name a caller already binds ($IsWindowsHost, $ExeSuffix, $SepPattern, $SrcRoot,
    $RepoRoot, $ConverterSrc, $Go2csExe, Get-PathDepth, Get-RelativeDisplayPath) is unchanged.

    Dot-source this file to get one definition of each:

        . (Join-Path $PSScriptRoot '_paths.ps1')

    Nothing here has side effects; it defines variables and two helpers and returns.

.NOTES
    Requires PowerShell 5.1 (Windows) or PowerShell 7+ (any platform).
#>

# Dot-sourcing chains: these land in the caller's scope, exactly as if they were defined here.
. (Join-Path $PSScriptRoot '../../_paths.ps1')

# The one root that is specific to this tree.
$BehavioralRoot = $PSScriptRoot
