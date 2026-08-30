<#
.SYNOPSIS
    Static census guard for the syscall funnel keep-alive emission (RULING 2, 2026-08-30): every
    corpus .cs file's `var kN = ...;` temp-capture (k = U+1D0B, the converter's exclusive glyph for
    this emission) must be paired with exactly one matching `System.GC.KeepAlive(kN);` -- the
    call-site closure this arc's escalation implements.

.DESCRIPTION
    Regenerable and static: it takes any corpus root (a fresh seeded reconvert, or the committed
    src\core tree) and greps every .cs file for two patterns -- the temp declaration and its
    KeepAlive -- counting occurrences rather than hand-maintaining a site list. It does not build or
    execute anything; it is a pure text census over already-emitted output, meant to run BEFORE (and
    far more cheaply than) a full corpus build.

    What it protects against, and why a build alone cannot: an UNPROTECTED pointer-derived uintptr
    argument (`(uintptr)Ꮡx` with no capturing temp) is syntactically valid C# and compiles clean --
    it is exactly the original defect this arc exists to fix. A regression that silently narrows
    pointerDerivedArgSource's detection (or otherwise stops capturing a real site) would not fail the
    corpus build; it would only show up here, as a drop in the counted total or a name mismatch.

.PARAMETER CorpusRoot
    Directory to scan (recursively) for .cs files. Defaults to the committed src\core tree.

.NOTES
    Counts, never asserts a specific number -- the corpus grows and the count is expected to move.
    What must ALWAYS hold: the two counts match (every temp has exactly one KeepAlive, per file, by
    name-multiset), and the total is greater than zero (a census that can report zero and call it
    clean has never been positive-controlled -- CLAUDE.md's own "a gate that has never been made to
    fail proves nothing").
#>

param(
    [string] $CorpusRoot
)

. (Join-Path $PSScriptRoot '_paths.ps1')

if (-not $CorpusRoot) {
    $CorpusRoot = Join-Path $RepoRoot 'src\core'
}

if (-not (Test-Path -LiteralPath $CorpusRoot)) {
    Write-Error "Corpus root not found: $CorpusRoot"
    exit 1
}

$tempPattern = [regex]'var\s+(ᴋ\d+)\s*='
$keepAlivePattern = [regex]'System\.GC\.KeepAlive\((ᴋ\d+)\);'
$funnelCallPattern = [regex]'syscall\.(Syscall(6|9|12|15|18)?|SyscallN)\('

$totalTemps = 0
$totalKeepAlives = 0
$totalFunnelCalls = 0
$mismatchFiles = @()

$csFiles = Get-ChildItem -LiteralPath $CorpusRoot -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|Generated)[\\/]' }

foreach ($file in $csFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)

    $funnelMatches = $funnelCallPattern.Matches($content)

    if ($funnelMatches.Count -eq 0) {
        continue
    }

    $totalFunnelCalls += $funnelMatches.Count

    $tempMatches = $tempPattern.Matches($content)
    $keepAliveMatches = $keepAlivePattern.Matches($content)

    $totalTemps += $tempMatches.Count
    $totalKeepAlives += $keepAliveMatches.Count

    $tempNames = ($tempMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object) -join ','
    $keepAliveNames = ($keepAliveMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object) -join ','

    if ($tempNames -ne $keepAliveNames) {
        $mismatchFiles += Get-RelativeDisplayPath -Path $file.FullName -Root $CorpusRoot
    }
}

Write-Host "syscall funnel keep-alive census: $($csFiles.Count) .cs file(s) scanned under $CorpusRoot"
Write-Host "  funnel call occurrences (informational -- includes non-pointer-arg calls): $totalFunnelCalls"
Write-Host "  captured temps (var kN = ...;):   $totalTemps"
Write-Host "  matching KeepAlive calls:         $totalKeepAlives"

if ($mismatchFiles.Count -gt 0) {
    Write-Error "MISMATCH: $($mismatchFiles.Count) file(s) have a temp/KeepAlive name-multiset mismatch:`n$($mismatchFiles -join "`n")"
    exit 1
}

if ($totalTemps -ne $totalKeepAlives) {
    Write-Error "MISMATCH: total temps ($totalTemps) != total KeepAlives ($totalKeepAlives) -- counted equal per file but not corpus-wide, which should be impossible; re-check the patterns"
    exit 1
}

if ($totalTemps -eq 0) {
    Write-Error "CENSUS IS VACUOUS: zero captured temps found anywhere under $CorpusRoot -- positive control failed (either this corpus predates the fix, or CorpusRoot is wrong)"
    exit 1
}

Write-Host "CENSUS CLEAN: every captured temp has exactly one matching KeepAlive; $totalTemps site(s) protected."
exit 0
