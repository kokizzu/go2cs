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

# The GOOS flavor THIS HOST is native to -- one derivation, for the same reason $IsWindowsHost is one
# derivation: $IsMacOS and $IsLinux are PowerShell 6+ automatic variables that DO NOT EXIST on
# Windows PowerShell 5.1, where each reads $null -> falsey, so a consumer restating the ladder gets
# it silently wrong on exactly the platform 5.1 runs on. The ladder therefore starts from the host
# fact already resolved above and only then consults the pwsh-only variables, which is safe because
# 5.1 can never reach them.
#
# An unrecognized non-Windows host answers $null rather than a guess. Every consumer of this value
# treats $null as "leave the status-quo default alone" -- naming a flavor the corpus may not carry
# would be the guess this repository's harness rules forbid.
$HostGoos =
    if ($IsWindowsHost) { 'windows' }
    elseif ($IsMacOS)   { 'darwin' }
    elseif ($IsLinux)   { 'linux' }
    else                { $null }

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
# The binding is DERIVED from $HostGoos rather than scoped to a literal, so it covers every
# non-Windows flavor the corpus carries and cannot go stale one host at a time. Windows is excluded
# by the value, not by a platform test: 'windows' IS the csproj default, so the one host that
# already binds the right flavor gets no variable at all and its behavior is untouched by
# construction. A $null $HostGoos (a host this file cannot name) is excluded the same way.
#
# History, kept because the SCOPE moved and the reason it moved matters:
#
#   2026-08-21 -- introduced scoped to $IsLinux precisely, on the stated ground that "a macOS host
#   must NOT inherit `linux` (its own flavor is `darwin`, and that corpus does not build today --
#   19 pre-existing errors, censused), so darwin keeps the status-quo windows default until its own
#   lane earns a binding."
#
#   2026-09-02 -- WIDENED to darwin: that wall is CLOSED and the scope reason expired with it. The
#   darwin corpus compiles clean -- census run 32649840220 at c003d32af, ZERO errors on osx-x64 AND
#   osx-arm64, the wall history 19 -> 10 -> 9 -> 0 inside ~24 hours of the first darwin build ever
#   attempted, re-confirmed green at master by the 2026-08-25 census (run 32852475367, both legs).
#   Until this amendment a LOCAL darwin instrument still silently took the WINDOWS flavor on the
#   strength of a wall that no longer existed. CI never had the gap: .github/workflows/os-matrix.yml
#   binds GoTargetOS from matrix.goos at job level, which is why this stayed invisible.
#
# What darwin still lacks is a RUN layer, not a compile flavor: its libc trampolines are bodyless
# partials that PartialStubGenerator fills with a throw, so a converted program exits 2 on its first
# syscall (docs/phase4/FINDING-darwin-run-layer.md -- characterized 2026-08-25, not fixed). That is
# an argument for building darwin's run layer; it is NOT an argument for compiling the windows
# flavor on a Mac, which is wrong at SOURCE SELECTION -- before any run layer is reached -- and
# makes a darwin host measure a corpus it is not.
if ($HostGoos -and $HostGoos -ne 'windows' -and [string]::IsNullOrEmpty($env:GoTargetOS)) {
    $env:GoTargetOS = $HostGoos
}

# Pin GO2CSPATH on every non-Windows host so the converter's child-env `$(go2csPath)` race has one
# value on both names. When the var is unset, the converter defaults it to `~/go2cs` AND
# os.Setenv()s it into its own environment (main.go:93); every pipeline child then inherits that
# entry — clone-root, no trailing separator — BESIDE the correct injected `go2csPath=<src>/`
# (testConversion.go:5663).
# POSIX environs are case-sensitive so both entries coexist, but MSBuild maps environment
# variables onto properties CASE-INSENSITIVELY, and which entry wins the one `$(go2csPath)` slot
# is enumeration-order-dependent — a per-process coin flip. A losing draw resolves the analyzer
# and every stdlib ProjectReference against `<clone>gen/...`/`<clone>core/...` (no separator, no
# src) → MSB9008 + a CS0246 storm on every golib type, reported by the sweep as a total suite
# failure (`Go="pass" C#=""`). That intermittency killed three Linux measurement campaigns before
# the binlog named it: `Property 'go2csPath' with value '/root/go2cs' expanded from the
# environment.` Pinning the var to the slash-terminated src root makes either race winner
# correct. Windows is untouched twice over: the pin is scoped away from it by $HostGoos, and
# Windows env blocks are case-insensitive at the OS level (one slot, injection wins
# deterministically) — the race is structurally POSIX-only. An already-set GO2CSPATH still wins
# here (empty-guard), and every instrument still passes -go2cspath explicitly; this pin only stops
# the converter's own default from leaking a wrong root into its children. The complete
# converter-side fix (dedupe at the child-env builder) is priced on the board (2026-08-21 entry).
#
#   2026-09-02 -- SCOPE WIDENED from $IsLinux to $HostGoos, in the same cut as the GoTargetOS pin
#   above and on a ground this comment was already making: the race "is structurally POSIX-only",
#   and darwin IS POSIX -- case-sensitive environ, the same converter default, the same
#   case-insensitive MSBuild property mapping -- so a macOS host could lose the identical coin flip
#   while the $IsLinux literal said nothing about it. Scoping on the derived value rather than on a
#   platform test leaves Linux and Windows byte-for-byte what they were ($HostGoos is 'linux' and
#   'windows' respectively, so the guard admits and excludes exactly whom it did) and stops the
#   scope going stale one host at a time -- which is precisely how the GoTargetOS pin above went
#   stale, and why both are now derived from one host fact instead of two literals.
if ($HostGoos -and $HostGoos -ne 'windows' -and [string]::IsNullOrEmpty($env:GO2CSPATH)) {
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
