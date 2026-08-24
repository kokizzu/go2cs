<#
.SYNOPSIS
    The TFM-hop instrument (DotNetMigration.md section 5.1): census by default, -Apply to act, -WhatIf
    honored. Encodes CENSUS-tfm-inventory.md's classes so a TFM bump is a dispatch, not a discovery.

.DESCRIPTION
    Commissioned by user directive (mailbox, 2026-08-24) as a first-class runbook instrument. A bare
    run REPORTS every site class and changes nothing. -Apply edits exactly the SOURCE set (Class B's
    property line AND the seventeen project files no emitter reaches, the converter's two csproj
    templates, the nine embedded publish profiles, the CI SDK channel, and the present-tense Class-E
    doc lines) and then SELF-VERIFIES: a re-census must find zero remaining apply-set sites, and that
    property is the gate.

    WHAT THIS SCRIPT NEVER DOES, by design (the census's own rules):
      - It never edits GENERATED files. The Class-A csproj are emission output; after -Apply
        this script NAMES the regens the operator owes (the three-target -platforms merge,
        UpdateTestTargets --createTargetFiles, `go generate .` in src\go2cs) and the section 8 gates. A
        script that edits generated files is the fourth trap wearing a helpful face.
        The test of Class A is REACHABILITY, not location: seventeen project files sit in those same
        trees (or beside them) that no emitter emits, so no regen can level them. Those are Class B,
        they are edited here, and they are subtracted from the Class-A count so it reads true.
      - It never touches Class C (must-not-change): go2cs-gen stays netstandard2.0 (Roslyn analyzer
        contract), push-nuget's matching path stays, and dated measurement provenance keeps saying
        the version it measured. These are ENFORCED refusals with reasons, not omissions.
      - It does not run the regen, the gate ladder, or the judgement. section 8's checklist stays the
        operator's; this script is steps 3-4 (and the CI/doc lines of steps 9-10) made mechanical.

.PARAMETER To
    The target TFM. Defaults to net10.0 (the current hop); hop N+1 passes its own.

.PARAMETER From
    The TFM being retired. Defaults to net9.0.

.PARAMETER Apply
    Perform the edits. Without it, census-and-report only.

.NOTES
    File I/O per the repo's documented trap: [System.IO.File]::ReadAllText/WriteAllText with UTF-8
    no-BOM, line endings preserved -- never PS 5.1 Get-Content/Out-File (the mojibake class).
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$To = 'net10.0',
    [string]$From = 'net9.0',
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

# Paths from the shared primitives, never re-derived (repo rule).
. (Join-Path $PSScriptRoot '_paths.ps1')
$src = $PSScriptRoot
$repo = Split-Path $src -Parent

function Read-Text([string]$Path) { [System.IO.File]::ReadAllText($Path) }

# Does the file begin with a UTF-8 BOM? ReadAllText silently STRIPS one, so a naive round-trip
# through Read-Text/Write-Text drops it -- an out-of-scope byte change in a file whose intended edit
# is a single token, and the same encoding-damage class the repo's csproj I/O rule exists to prevent.
function Test-Utf8Bom([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    $head = New-Object byte[] 3
    $stream = [System.IO.File]::OpenRead($Path)
    try { $read = $stream.Read($head, 0, 3) } finally { $stream.Dispose() }
    return ($read -eq 3 -and $head[0] -eq 0xEF -and $head[1] -eq 0xBB -and $head[2] -eq 0xBF)
}

function Write-Text([string]$Path, [string]$Text) {
    # PRESERVE the file's existing BOM state rather than imposing one. The converter emits UTF-8
    # no-BOM, so the generated families round-trip unchanged either way -- but the hand-written
    # Class-B project files were authored in Visual Studio and several DO carry a BOM. Hardcoding
    # no-BOM rewrote three of them (golib, GenTests, UpdateTestTargets) on the first run of this
    # class; encoding is the file's, not the instrument's, to decide.
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding((Test-Utf8Bom $Path))))
}

# --- The APPLY SET: every source-of-truth site, with the exact old/new text per site --------------
# Exactness is the safety: a site that no longer matches is REPORTED as moved, never fuzzily edited.
# Doc lines edit the TFM token only, inside the quoted exact context the census recorded.
$applySites = @(
    # THE EDIT (census section 8 step 4): the one property everything conditioned hangs off.
    @{ File = "$src\Directory.Build.props";
       Old  = "<TargetFramework Condition=`"'`$(TargetFramework)'==''`">$From</TargetFramework>";
       Why  = 'Class B: the property of record' },

    # D': the converter's two embedded csproj templates (their conditioned FALLBACKS).
    #     (Class B's project files are appended below, after this literal array.)
    @{ File = "$src\go2cs\csproj-template.xml";
       Old  = "<TargetFramework Condition=`"'`$(TargetFramework)'==''`">$From</TargetFramework>";
       Why  = "D': emission fallback -- editing it self-invalidates go2cs.exe via route #5's predicate" },
    @{ File = "$src\go2cs\test-csproj-template.xml";
       Old  = "<TargetFramework Condition=`"'`$(TargetFramework)'==''`">$From</TargetFramework>";
       Why  = "D': test-host emission fallback" },

    # CI: the SDK channel default + its description (census section 8 step 9; the PROBE path already
    # derives since Class D, so only the channel selection moves).
    @{ File = "$repo\.github\workflows\os-matrix.yml";
       Old  = 'default: 9.0.x';
       New  = 'default: 10.0.x';
       Why  = 'step 9: SDK 9 cannot build the new TFM' }
)

# --- Class B: the project files NO EMITTER REACHES (census section 3, plus its own recommended fold)
# Every one carries the same conditioned fallback the generated corpus does, and in-repo every one is
# INERT: src\Directory.Build.props is auto-imported ABOVE the project body, so it assigns
# TargetFramework first and each project's Condition="'$(TargetFramework)'==''" then evaluates FALSE.
# That inertness is by design (see that file's own header) -- and it is exactly why this class can sit
# stale behind green gates.
#
# Where these lines DO govern is the tree deploy-core.ps1 stages: it copies src\core while EXCLUDING
# core's Directory.Build.props, and the root props it writes at the target pins only $(go2csPath),
# never the framework. Nothing assigns the TFM there, so each project's own line becomes
# authoritative -- and a holdout left at $From that references a package already moved to $To is a
# real NU1201, because $To -> $From is legal (downlevel) while $From -> $To is not. The shape is
# therefore: every in-repo gate green, the DEPLOYED and published artifact broken.
#
# Reachability, not preference, is what puts a file here: no regen can level these, so a hop that
# only regenerates leaves them behind permanently. The default is that the hop moves the whole tree;
# a project that must STAY belongs in Class C with its reason, never silently here. Every entry below
# was checked for its role and its references before being admitted, and none warranted staying.
$unreachableProjects = @(
    # --- Deployed by deploy-core.ps1, so these are the ones with real NU1201 exposure -------------
    @{ File = "$src\core\golib\golib.csproj";
       Why  = 'hand-written runtime, emitted by nothing; the published go.lib every converted project binds' },
    @{ File = "$src\core\testing\testing.csproj";
       Why  = 'skip-listed (isNonConvertedStdLibPackage); references ../time/time.csproj -- the deployed NU1201 of record' },
    @{ File = "$src\core\unsafe\unsafe.csproj";
       Why  = 'skip-listed; compiler intrinsic, references golib only' },
    @{ File = "$src\core\internal\concurrent\internal.concurrent.csproj";
       Why  = 'whole-file hand-own => unmarkedFileCount == 0 => the driver continues before writeProjectFile; references six moved packages' },
    @{ File = "$src\core\internal\godebug\internal.godebug.csproj";
       Why  = 'same whole-file hand-own shape; references six moved packages' },
    @{ File = "$src\core\internal\weak\internal.weak.csproj";
       Why  = 'same whole-file hand-own shape; references runtime + internal/abi' },
    @{ File = "$src\core\internal\runtime\syscall\internal.runtime.syscall.csproj";
       Why  = 'platform-exclusive (linux): absent from the reference target, so the -platforms merge never writes its csproj' },
    @{ File = "$src\core\crypto\x509\internal\macos\crypto.x509.internal.macos.csproj";
       Why  = 'platform-exclusive (darwin): same merge gap; references time/runtime/bytes -- NU1201 on a darwin deploy' },
    @{ File = "$src\core\vendor\golang.org\x\net\route\vendor.golang.org.x.net.route.csproj";
       Why  = 'platform-exclusive (darwin): same merge gap; references os/runtime/syscall -- NU1201 on a darwin deploy' },

    # --- Repo-only harness and tooling: never deployed, so inert everywhere today. Moved so the tree
    #     reads true and the NEXT hop finds nothing left behind, not to fix a break.
    @{ File = "$src\tests\Behavioral\BehavioralRunner\BehavioralRunner.csproj";
       Why  = 'hand-written harness; the behavioral regen levels the 637 emitted projects but not its own runner' },
    @{ File = "$src\tests\Behavioral\BehavioralTests\BehavioralTests.csproj";
       Why  = 'hand-written MSTest host; same blind spot as the runner' },
    @{ File = "$src\tests\Performance\PerformanceRunner\PerformanceRunner.csproj";
       Why  = 'hand-written harness; the transpile regenerates the 14 Perf* csproj but not the runner' },
    @{ File = "$src\tests\GolibTests\GolibTests.csproj";        Why = 'hand-written gate instrument' },
    @{ File = "$src\tests\GenTests\GenTests.csproj";            Why = 'hand-written; exercises go2cs-gen internals' },
    @{ File = "$src\tests\ChannelTests\ChannelTests.csproj";    Why = 'hand-written' },
    @{ File = "$src\tests\GenericTests\GenericTests.csproj";    Why = 'hand-written' },
    @{ File = "$src\utilities\UpdateTestTargets\UpdateTestTargets.csproj"; Why = 'hand-written golden re-baseliner' }
)

foreach ($project in $unreachableProjects) {
    $applySites += @{ File = $project.File;
                      Old  = "<TargetFramework Condition=`"'`$(TargetFramework)'==''`">$From</TargetFramework>";
                      Why  = "Class B (no emitter reaches it): $($project.Why)" }
}

# golib's NoWarn rationale states the live TFM as present-tense fact, not as dated provenance -- it
# explains which warnings cannot fire on the framework the project TARGETS, so it moves with it.
$applySites += @{ File = "$src\core\golib\golib.csproj";
                  Old  = "cannot fire on $From";
                  Why  = 'Class B: golib prose stating the live TFM as present-tense fact' }

# D'': the nine embedded publish profiles, TWO sites each (PublishDir and TargetFramework -- both,
# or the publish lands in a folder named for the old TFM).
foreach ($pubxml in Get-ChildItem "$src\go2cs\profiles" -Filter *.pubxml -File) {
    $applySites += @{ File = $pubxml.FullName;
                      Old  = "bin/Release/$From/publish/";
                      Why  = "D'': PublishDir names the TFM folder" }
    $applySites += @{ File = $pubxml.FullName;
                      Old  = "<TargetFramework>$From</TargetFramework>";
                      Why  = "D'': the profile's own TFM" }
}

# Class E: present-tense doc lines (census section 6), each edited inside its exact recorded context.
# DotNetMigration.md and CIMatrix.md are deliberately REPORT-ONLY: most of their mentions are
# scouting history (Class-C-shaped), and CIMatrix:67 should describe Class D's DERIVED probe
# rather than gain a new TFM literal -- both are prose judgements, not token swaps.
$applySites += @(
    @{ File = "$repo\docs\README.md";        Old = "cd bin/Debug/$From";  Why = 'Class E: visitor-facing command' },
    @{ File = "$repo\CLAUDE.md";             Old = "(target **$From**, C# latest)"; Why = 'Class E: orientation fact' },
    @{ File = "$repo\CLAUDE.md";             Old = "its ``bin/Debug/$From``"; Why = 'Class E: operational instruction' },
    @{ File = "$repo\docs\Glossary.md";      Old = "bin/Debug/$From/<AssemblyName>.dll"; Why = 'Class E: definition' },
    @{ File = "$repo\docs\Glossary.md";      Old = "<TargetFramework>$From</TargetFramework>"; Why = 'Class E: definition example' },
    @{ File = "$repo\docs\ConversionStrategies-Reference.md";
       Old = "<TargetFramework Condition=`"'`$(TargetFramework)'==''`">$From</TargetFramework>";
       Why = 'Class E: the emitted form as documented' }
)

# --- MUST NOT CHANGE (Class C), enforced with reasons --------------------------------------------
$mustNotChange = @(
    @{ File = "$src\gen\go2cs-gen\go2cs-gen.csproj"; Text = '<TargetFramework>netstandard2.0</TargetFramework>';
       Why  = 'Roslyn analyzers load in the compiler; netstandard2.0 is the contract, not a default' },
    @{ File = "$src\push-nuget.ps1"; Text = 'netstandard2.0';
       Why  = 'pairs with the analyzer TFM above; correct as-is' },
    @{ File = "$src\core\runtime\managed_impl.cs"; Text = "288 B on net9.0";
       Why  = 'dated measurement provenance in a hand-owned file' },
    @{ File = "$src\core\testing\testing.cs"; Text = "net9.0/9.0.18";
       Why  = 'dated measurement provenance' }
)

# --- Census ---------------------------------------------------------------------------------------
Write-Host "migrate-tfm: $From -> $To  ($(if ($Apply) { 'APPLY' } else { 'census only' }))"
Write-Host ''

$pending = @()
$moved = @()

foreach ($site in $applySites) {
    if (-not (Test-Path $site.File)) { $moved += "$($site.File) -- file missing"; continue }
    $text = Read-Text $site.File
    $new = if ($site.ContainsKey('New')) { $site.New } else { ($site.Old -replace [regex]::Escape($From), $To) }
    if ($text.Contains($site.Old)) { $pending += @{ Site = $site; New = $new } }
    elseif (-not $text.Contains($new)) { $moved += "$($site.File) -- expected text not found: $($site.Old)" }
}

Write-Host "apply-set sites still at ${From}: $($pending.Count)"
foreach ($p in $pending) { Write-Host ("  {0}`n      [{1}]" -f $p.Site.File.Substring($repo.Length + 1), $p.Site.Why) }

if ($moved.Count -gt 0) {
    Write-Host ''
    Write-Warning "sites that match NEITHER ${From} nor ${To} -- the census is stale against the tree; fix the census, then this script:"
    foreach ($m in $moved) { Write-Warning "  $m" }
}

# Generated corpus (Class A): counted, never edited. The Class-B project files above live INSIDE
# these same directories, so they must be subtracted -- counted as Class A they read as "a regen
# levels them" when no regen can reach them, which is how twelve of them stayed at $From through a
# full three-target merge and a behavioral re-transpile.
$classARoots = @("$src\core", "$src\tests\Behavioral", "$src\tests\Performance")
$unreachablePaths = @{}
foreach ($project in $unreachableProjects) { $unreachablePaths[$project.File] = $true }

$classA = @(Get-ChildItem $classARoots -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
    Where-Object { -not $unreachablePaths.ContainsKey($_.FullName) -and (Read-Text $_.FullName).Contains(">$From<") })
$shadowed = @($unreachableProjects | Where-Object { $path = $_.File; @($classARoots | Where-Object { $path.StartsWith("$_\") }).Count -gt 0 })
Write-Host ''
Write-Host "Class A (generated; regen levels them, this script never touches them): $($classA.Count) csproj at $From"
Write-Host "  (excludes $($shadowed.Count) Class-B project files sitting inside those trees that no emitter reaches)"

# Class C verification: the protected values must still be present (their ABSENCE is the alarm).
Write-Host ''
foreach ($c in $mustNotChange) {
    $state = if ((Test-Path $c.File) -and (Read-Text $c.File).Contains($c.Text)) { 'intact' } else { 'MISSING -- investigate' }
    Write-Host ("Class C {0}: {1}  [{2}]" -f $state, $c.File.Substring($repo.Length + 1), $c.Why)
}

if (-not $Apply) {
    Write-Host ''
    Write-Host 'Census only. Re-run with -Apply to edit the apply set (never the generated corpus).'
    exit 0
}

# --- Apply ----------------------------------------------------------------------------------------
Write-Host ''
foreach ($p in $pending) {
    if ($PSCmdlet.ShouldProcess($p.Site.File, "replace '$($p.Site.Old)'")) {
        $text = Read-Text $p.Site.File
        Write-Text $p.Site.File ($text.Replace($p.Site.Old, $p.New))
        Write-Host "edited: $($p.Site.File.Substring($repo.Length + 1))"
    }
}

# --- Self-verify: idempotence is the gate (only for a run that actually edited) -------------------
if ($WhatIfPreference) {
    Write-Host ''
    Write-Host "WhatIf: $($pending.Count) site(s) would be edited; nothing was. Self-verify skipped by design."
    exit 0
}
$remaining = 0
foreach ($site in $applySites) {
    if ((Test-Path $site.File) -and (Read-Text $site.File).Contains($site.Old)) { $remaining++ }
}
Write-Host ''
if ($remaining -ne 0) {
    Write-Error "self-verify FAILED: $remaining apply-set site(s) still at $From after -Apply."
    exit 1
}
Write-Host "self-verify: zero apply-set sites remain at $From."
Write-Host ''
Write-Host 'WHAT THIS SCRIPT DID NOT DO -- the operator owes, in CENSUS-tfm-inventory.md section 8 order:'
Write-Host '  1. go test -count=1 ./...  (src\go2cs -- template guards; -count=1 required, section 5.2)'
Write-Host '  2. The regens: seeded three-target -platforms merge; UpdateTestTargets --createTargetFiles;'
Write-Host '     go generate .  (Class A levels here, not by hand)'
Write-Host '  3. The section 4 gate ladder (purging bin/obj/Generated between flavor switches), CNR + accounting'
Write-Host '  4. Purge stale emitted publish profiles (**/Properties/PublishProfiles/ -- gitignored, never overwritten)'
Write-Host "  5. The commit-message staleness statement: generated csproj still read $From; inert, level on regen"
