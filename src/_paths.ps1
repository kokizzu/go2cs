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

# The corpus's target framework, DERIVED from the property of record -- never restated. Every
# instrument that touches a build output path (bin/<config>/<tfm>/) reads $NetVersion, and
# src\Directory.Build.props owns <TargetFramework> for the whole tree, so that file is what this
# reads.
#
# It used to be a literal here. The TFM census (docs/phase4/CENSUS-tfm-inventory.md, Class D) had
# hoisted nine hardcoded sites out of six files into this one line, which fixed the SPREAD but not
# the KIND: a hoisted literal is still a literal, and it went stale on the very next hop. With the
# tree on net10.0 and this line still saying net9.0 every probe pointed at a bin\Debug\net9.0 that
# does not exist -- the build succeeds, the instrument finds nothing, and a wrapper that had nothing
# to run could report success (false-green route #6, CLAUDE.md). The C# harnesses derive theirs from
# their own bin tail (BehavioralTestBase's pattern); a script has no bin tail at source time, but it
# does have the props file, so it reads that rather than trusting a hand-edit a hop can forget. A
# hop is now ONE edit, in Directory.Build.props, with nothing on the script side to level.
#
# Cheap by construction -- one file read and one regex, no MSBuild and no `dotnet` -- because this
# module is dot-sourced by every instrument on every invocation. Comments are stripped before the
# match so the props file's own prose (which names <TargetFramework> while explaining it) cannot be
# read as the property, and the pattern tolerates the attribute the real element carries (it is
# Condition-guarded; the element's INNER TEXT is the framework). [regex]::Match rather than -match
# keeps this file's no-side-effects promise: -match publishes $Matches into the caller's scope.
#
# There is deliberately NO fallback to a literal. A silent fallback is precisely how this class of
# defect hides -- an instrument that cannot know its own TFM must say so, not guess.
$TargetFrameworkProps = Join-Path $SrcRoot 'Directory.Build.props'

if (-not (Test-Path -LiteralPath $TargetFrameworkProps)) {
    throw "Cannot derive `$NetVersion: the target framework's property of record is missing at $TargetFrameworkProps"
}

$NetVersionMatch = [regex]::Match(
    ([System.IO.File]::ReadAllText($TargetFrameworkProps) -replace '(?s)<!--.*?-->', ''),
    '<TargetFramework(?:\s[^>]*)?>\s*([^<\s]+)\s*</TargetFramework>')

if (-not $NetVersionMatch.Success) {
    throw "Cannot derive `$NetVersion from $TargetFrameworkProps -- expected a <TargetFramework>...</TargetFramework> element (a Condition attribute is fine; the element's inner text is the framework)"
}

$NetVersion = $NetVersionMatch.Groups[1].Value

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
