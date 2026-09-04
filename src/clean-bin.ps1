# Clean-Bin.ps1
# Script to remove all bin, obj and Generated folders from a root directory and subdirectories.
# The root defaults to this script's own tree (src\), NEVER the caller's current directory:
# invoking by absolute path from another cwd used to enumerate whatever tree the caller
# happened to be sitting in (near-missed a sibling checkout, 2026-08-15). Pass -Root to
# clean a different tree deliberately.
#
# NON-INTERACTIVE USE -- pass -Force. Without it, a host that cannot reach a human is
# REFUSED with a NON-ZERO exit and a message naming the switch, rather than printing
# "Operation canceled" and exiting 0 having deleted nothing. That was the false-green
# route #6 hole two lanes met independently on 2026-09-03: "Found 2866 folders to delete.
# Operation canceled.", exit 0, disk unchanged, reported as a completed purge, until the
# run was repeated with `echo Y |` piped in.
#
# -Force IS THE CONTRACT, and the reason is measured rather than stylistic. -Confirm:$false
# is honoured too and behaves identically -- in-process on BOTH editions, and through
# `pwsh -File` -- but Windows PowerShell 5.1 cannot bind it through `-File` at all: 5.1
# literalizes -File arguments, so the binder is handed the STRING '$false' and rejects it
# ("Cannot convert 'System.String' to the type 'System.Management.Automation.SwitchParameter'
# required by parameter 'Confirm'"), exiting 1 before this script runs a line. A caller that
# reached for -Confirm:$false in the launcher would therefore get a refusal that looks like
# this script's own exit 1 and is not. Use -Force for anything invoked through -File or the
# .bat launcher; -Confirm:$false is for an in-process `& .\clean-bin.ps1 -Confirm:$false`.
#
# DIRECT INVOCATION needs the execution-policy bypass this script cannot supply for itself:
#     powershell -NoProfile -ExecutionPolicy Bypass -File .\clean-bin.ps1 -Force
# The script is unsigned, so on a host whose execution policy requires signing a bare
# `powershell -NoProfile -File` dies "is not digitally signed" (observed 2026-09-03). That
# failure does NOT reproduce on a box whose LocalMachine policy is already Bypass -- which
# is the reason the bypass belongs in the invocation rather than in an assumption about the
# host. clean-bin.bat already carries it, forwards its arguments and propagates the exit
# code, so `clean-bin.bat -Force` is the shortest correct spelling.
#
# EXIT CODES ARE LOAD-BEARING. A caller that runs a build after a purge must check them --
# a wrapper that captured a non-zero clean and carried on ran a GoTargetOS-switch build
# WITHOUT the purge it reported attempting (2026-09-03).
#     0  nothing was found, or every folder found is gone
#     1  an answer was obtained from a human and it declined the deletion
#     2  the host could not prompt (non-interactive, or redirected stdin at end-of-stream)
#        and -Force was absent -- nothing was deleted
#     3  -WhatIf: folders were found and nothing was deleted, by request
#     4  one or more folders remained after the run, or a removal errored
# A found-but-not-deleted run NEVER exits 0.
# Read those codes through `-File` or clean-bin.bat, which propagate them exactly. A
# `-Command "& .\clean-bin.ps1"` invocation COLLAPSES every non-zero code to 1 (measured:
# a run that printed "Exit 2." was observed by its caller as 1), so the zero/non-zero
# distinction survives there but the specific code does not.
#
# HOW NON-INTERACTIVITY IS DETECTED, AND WHY THESE TESTS:
#   [Console]::IsInputRedirected -- the direct form of the only question that matters,
#       "can Read-Host reach a human?". It is what separates an EMPTY answer that is a
#       person pressing Enter (a decline, exit 1) from an EMPTY answer that is end-of-stream
#       on a redirected stdin (no human at all, exit 2). Mode 2 is exactly the second case.
#   [Environment]::UserInteractive -- the window-station level test; False under a service
#       or any station with no interactive desktop. Necessary but NOT sufficient: it reads
#       True for a harness tool call and for a detached background task, which is why it
#       could not have caught mode 2 on its own.
#   a try/catch around Read-Host -- a host started with -NonInteractive THROWS rather than
#       prompting, and that throw is evidence of the same fact.
# $Host.UI.RawUI availability is deliberately NOT used as the test: ConsoleHost exposes
# RawUI even when stdin is redirected, so it answers "interactive" in precisely the case
# that produced the hole.
# A PIPED answer still works (`echo Y | ...`): the prompt is attempted whenever the host
# has a readable stream, and only an EOF-empty answer is read as "no human". The workaround
# the coordinator used today therefore keeps its meaning instead of silently changing it.
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Root = $PSScriptRoot,
    # Skip the confirmation prompt. THE contract for non-interactive callers.
    [switch]$Force
)

# -Confirm:$false is honoured as an equivalent of -Force. The test is whether the caller
# SPECIFIED it, not what a default happens to be: an unspecified -Confirm must not read as
# a suppression (the same predicate trap the sweep's -TestConfig override paid for).
$confirmSuppressed = $PSBoundParameters.ContainsKey('Confirm') -and (-not $PSBoundParameters['Confirm'])

# -WhatIf is the only other ShouldProcess form this script honours. It is handled below as
# an explicit dry run rather than through per-folder ShouldProcess calls, so that it can
# report a distinct NON-ZERO exit code instead of a zero that reads as a completed purge.

$rootDirectory = (Resolve-Path -Path $Root -ErrorAction Stop).Path

# Count variables to track progress
$totalFoldersFound = 0
$totalFoldersRemoved = 0
$totalErrors = 0

# Refusals and failures go to stderr, EXACTLY ONCE. stderr is visible at a console and is
# captured by both 2>&1 and *>&1; Write-Host writes the Information stream, which 2>&1
# drops entirely, and adding it beside this would print every refusal TWICE in a 2>&1 log
# (measured on the first control run). The lost console color is the price of one line.
function Write-Refusal([string]$message) {
    [Console]::Error.WriteLine($message)
}

Write-Host "Searching for bin, obj and Generated folders in: $rootDirectory" -ForegroundColor Cyan

# Get all bin, obj and Generated directories. The @() is load-bearing on Windows PowerShell
# 5.1: an unwrapped single DirectoryInfo has no .Count there, so a one-folder tree reported
# "No bin, obj or Generated folders found." and exited without deleting it.
$foldersToDelete = @(Get-ChildItem -Path $rootDirectory -Include "bin", "obj", "Generated" -Directory -Recurse -ErrorAction SilentlyContinue)

$totalFoldersFound = $foldersToDelete.Count

if ($totalFoldersFound -eq 0) {
    Write-Host "No bin, obj or Generated folders found." -ForegroundColor Green
    exit 0
}

Write-Host "Found $totalFoldersFound folders to delete." -ForegroundColor Yellow

if ($WhatIfPreference) {
    foreach ($folder in $foldersToDelete) {
        Write-Host "What if: removing $($folder.FullName.Substring($rootDirectory.Length + 1))" -ForegroundColor DarkGray
    }
    Write-Refusal "-WhatIf: $totalFoldersFound folders found, none deleted. Exit 3."
    exit 3
}

# Confirmation gate
if ($Force) {
    Write-Host "-Force: proceeding without confirmation." -ForegroundColor DarkGray
}
elseif ($confirmSuppressed) {
    Write-Host '-Confirm:$false: proceeding without confirmation.' -ForegroundColor DarkGray
}
else {
    $answer = $null
    $answered = $false
    $noPromptReason = $null

    if (-not [Environment]::UserInteractive) {
        $noPromptReason = "[Environment]::UserInteractive is False -- no interactive window station"
    }
    else {
        try {
            $answer = Read-Host "Do you want to proceed with deletion? (Y/N)"
            $answered = $true
        }
        catch {
            $noPromptReason = "Read-Host could not prompt (a -NonInteractive host throws): $($_.Exception.Message)"
        }
    }

    # An empty answer off a REDIRECTED stdin is end-of-stream, not a decline: no human ever
    # saw the prompt. An empty answer at a real console is a person pressing Enter.
    if ($answered -and [string]::IsNullOrWhiteSpace($answer) -and [Console]::IsInputRedirected) {
        $answered = $false
        $noPromptReason = "stdin is redirected and Read-Host returned end-of-stream -- no human answered"
    }

    if (-not $answered) {
        Write-Refusal "This host cannot ask for confirmation: $noPromptReason."
        Write-Refusal "Refusing to delete $totalFoldersFound folders unattended. Re-run with -Force to purge non-interactively. Nothing was deleted. Exit 2."
        exit 2
    }

    if ($answer -notmatch '^\s*(y|yes)\s*$') {
        Write-Refusal "Operation canceled by the answer '$answer'. $totalFoldersFound folders found, none deleted. Exit 1."
        exit 1
    }
}

# Process each folder
foreach ($folder in $foldersToDelete) {
    $relativePath = $folder.FullName.Substring($rootDirectory.Length + 1)

    # A nested match (obj\...\Generated under obj\) is gone once its parent is removed.
    # Attempting it again throws ItemNotFoundException, which counted as a removal FAILURE
    # and would fail an otherwise perfect purge -- the false red this ordering avoids.
    if (-not (Test-Path -LiteralPath $folder.FullName)) {
        Write-Host "Removing: $relativePath - already removed with a parent" -ForegroundColor DarkGray
        $totalFoldersRemoved++
        continue
    }

    try {
        Write-Host "Removing: $relativePath" -ForegroundColor Yellow -NoNewline

        Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction Stop

        Write-Host " - Success" -ForegroundColor Green
        $totalFoldersRemoved++
    }
    catch {
        Write-Host " - Failed: $($_.Exception.Message)" -ForegroundColor Red
        $totalErrors++
    }
}

# Display summary
Write-Host "`nCleanup Summary:" -ForegroundColor Cyan
Write-Host "Folders found: $totalFoldersFound" -ForegroundColor White
Write-Host "Folders removed: $totalFoldersRemoved" -ForegroundColor Green
if ($totalErrors -gt 0) {
    Write-Host "Folders failed: $totalErrors" -ForegroundColor Red
}

# The verdict is what is left on DISK, not what the loop believed: success exits 0 only
# when every folder that was found is gone.
$stillPresent = @($foldersToDelete | Where-Object { Test-Path -LiteralPath $_.FullName })

if ($totalErrors -gt 0 -or $stillPresent.Count -gt 0) {
    Write-Refusal "Cleanup INCOMPLETE: $($stillPresent.Count) of $totalFoldersFound folders still present, $totalErrors removal errors. Exit 4."
    exit 4
}

Write-Host "`nCleanup completed!" -ForegroundColor Cyan
exit 0
