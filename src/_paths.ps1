<#
.SYNOPSIS
    Shared path/platform primitives for every PowerShell instrument under src\. Dot-source it.

.DESCRIPTION
    The behavioral tree got these first (src\tests\Behavioral\_paths.ps1, 2026-08-08) because that is
    where the two dangerous sites lived. But the primitives are not behavioral-specific: the sweep
    (src\run-validated-sweep.ps1), the deploy (src\deploy-core.ps1) and the performance wrapper
    (src\tests\Performance\run-performance.ps1) all need the same executable suffix and the same
    separator-agnostic helpers, and a src-level script reaching into the TEST tree for them would be
    backwards.

    So the primitives live here, at the src root, and the behavioral helper now extends this file
    rather than restating it. That matters most for $IsWindowsHost: getting it wrong is silent and
    backwards on exactly the platform this repository banks its corpus from (see the note below), so
    there must be ONE copy of that reasoning, not one per consumer.

    Dot-source this file to get one definition of each:

        . (Join-Path $PSScriptRoot '_paths.ps1')          # from src\
        . (Join-Path $PSScriptRoot '../_paths.ps1')       # from a src\<sub>\ script

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

# The corpus's target framework, spelled ONCE. Every instrument that touches a build output path
# (bin/<config>/<tfm>/) reads this instead of embedding the string: the TFM census
# (docs/phase4/CENSUS-tfm-inventory.md, Class D) found nine hardcoded sites across six files, each
# a FALSE-RED generator at a TFM hop -- build succeeds, probe misses, instrument reports a corpus
# failure. The C# harnesses DERIVE theirs from their own bin tail (BehavioralTestBase's pattern).
#
# This DERIVES too, and the 2026-08-24 hop is why. Class D hoisted five hand-edits into one literal
# here and called it "the one hand-edit a hop owes on the script side" -- then the TFM moved, the
# literal was NOT in migrate-tfm's apply set, and two lanes lost time to exactly the false-red the
# hoist existed to prevent (one LOUD -- run-behavioral.ps1 threw CommandNotFoundException on a
# net9.0 path; one SILENT -- run-performance.ps1 exited 0 having measured nothing; same root cause,
# two symptoms, so neither symptom alone is a sufficient check). A hoist still needs an editor; a
# DERIVATION needs nobody. Directory.Build.props sits beside this file and IS the property of
# record, so the script side can read the same single source the projects do -- which the original
# comment missed by asking only whether a *bin tail* existed.
$NetVersionPropsPath = Join-Path $PSScriptRoot 'Directory.Build.props'
$NetVersion = [regex]::Match(
    [System.IO.File]::ReadAllText($NetVersionPropsPath),
    '<TargetFramework[^>]*>(net[0-9]+\.[0-9]+)</TargetFramework>').Groups[1].Value
if (-not $NetVersion) {
    # Fail LOUD rather than hand every instrument an empty path segment -- an empty $NetVersion is
    # precisely the false-red this block exists to kill, and a silent one is the worse half.
    throw "_paths.ps1: could not derive the target framework from $NetVersionPropsPath"
}

# The corpus flavor a NON-Windows host binds by default. Every L3 csproj defaults `GoTargetOS` to
# `windows` when the property is EMPTY (the corpus reference target), which is right on Windows and
# wrong everywhere else: a linux host then builds the windows flavor, whose `os_package` module
# initializer faults on `DllImport("kernel32.dll")` the moment a program touches it (measured: the
# 2026-08-21 Linux census — 10 of a 34-project behavioral shard, each self-diagnosed by the corpus's
# own RID banner naming exactly this remedy). MSBuild maps environment variables to properties, and
# the csproj default is condition-guarded on empty, so ONE inherited env var is the entire binding —
# every child `dotnet` invocation of every instrument picks it up, with an explicit `-p:GoTargetOS`
# or a pre-set env var still winning. Windows behavior is untouched by construction.
#
# Scoped to $IsLinux, not `-not $IsWindowsHost`: a macOS host must NOT inherit `linux` (its own
# flavor is `darwin`, and that corpus does not build today — 19 pre-existing errors, censused), so
# darwin keeps the status-quo windows default until its own lane earns a binding. $IsLinux is a
# pwsh 6+ automatic variable; on Windows PowerShell 5.1 it does not exist, which resolves $null →
# falsey → the block is inert exactly where it should be.
if ($IsLinux -and [string]::IsNullOrEmpty($env:GoTargetOS)) {
    $env:GoTargetOS = 'linux'
}

# Pin GO2CSPATH on Linux so the converter's child-env `$(go2csPath)` race has one value on both
# names. When the var is unset, the converter defaults it to `~/go2cs` AND os.Setenv()s it into
# its own environment (main.go:93); every pipeline child then inherits that entry — clone-root,
# no trailing separator — BESIDE the correct injected `go2csPath=<src>/` (testConversion.go:5663).
# POSIX environs are case-sensitive so both entries coexist, but MSBuild maps environment
# variables onto properties CASE-INSENSITIVELY, and which entry wins the one `$(go2csPath)` slot
# is enumeration-order-dependent — a per-process coin flip. A losing draw resolves the analyzer
# and every stdlib ProjectReference against `<clone>gen/...`/`<clone>core/...` (no separator, no
# src) → MSB9008 + a CS0246 storm on every golib type, reported by the sweep as a total suite
# failure (`Go="pass" C#=""`). That intermittency killed three Linux measurement campaigns before
# the binlog named it: `Property 'go2csPath' with value '/root/go2cs' expanded from the
# environment.` Pinning the var to the slash-terminated src root makes either race winner
# correct. Windows is untouched twice over: the block is $IsLinux-scoped, and Windows env blocks
# are case-insensitive at the OS level (one slot, injection wins deterministically) — the race is
# structurally POSIX-only. An already-set GO2CSPATH still wins here (empty-guard), and every
# instrument still passes -go2cspath explicitly; this pin only stops the converter's own default
# from leaking a wrong root into its children. The complete converter-side fix (dedupe at the
# child-env builder) is priced on the board (2026-08-21 entry).
if ($IsLinux -and [string]::IsNullOrEmpty($env:GO2CSPATH)) {
    $env:GO2CSPATH = $PSScriptRoot.TrimEnd('/', '\') + '/'
}

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
# separator (measured on 5.1.26100: `Join-Path 'C:\x\y' 'core/net/http'` -> 'C:\x\y\core\net\http'),
# so this form is the one that is portable in both directions AND byte-identical to the backslash
# literals it replaces.
#
# ⚠ PowerShell variable names are CASE-INSENSITIVE, so a consumer's local $srcRoot / $repoRoot is
# the SAME variable as $SrcRoot / $RepoRoot here, not a shadow of it. Several callers assign one
# (deploy-core's `$srcRoot = $PSScriptRoot`, check-no-regression's `$repoRoot = $RepoRoot`) and that
# is safe only because the values coincide. If a caller ever needs a root that is NOT one of these,
# give it a distinct name rather than a different capitalization.
$SrcRoot      = $PSScriptRoot
$RepoRoot     = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ConverterSrc = Join-Path $SrcRoot 'go2cs'
$Go2csExe     = Join-Path $ConverterSrc "bin/go2cs$ExeSuffix"

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
