# run-h10-dispatch.ps1 - the H10 per-row dispatch driver.
#
# Reads the MACHINE-READABLE plan emitted by docs/phase4/hopA-inputs/shardmap.py --emit-plan and runs
# one worker's rows, in order, slice by slice, with the ruled cooldown gap between slices. Coordinator
# ruling e0d5121e2 section 7: "an H10 dispatch script that reads the emitted map and runs a worker's
# rows in order with the cooldown gaps of section 4 -- C2 designs it, i9 accepts it on one slice."
#
# Design record: docs/phase4/DESIGN-h10-dispatch-driver.md. Read that before changing anything here;
# the three refusals below are the whole point of the script and each one is there because the
# alternative fails SILENTLY.
#
# ---------------------------------------------------------------------------------------------------
# WHY THIS READS A TSV AND NOT THE MAP REPORT
#
# The generator's stdout is a report for a human and it defeats a parser three ways, each silently
# (measured 2026-09-13):
#
#   1. the row lists WRAP at column 118, so a line is not a record;
#   2. worker names carry SPACES and PARENTHESES ("i9-13900K (sweeper)"), so whitespace is not a
#      delimiter;
#   3. the SAME worker appears in EVERY `W` section with a DIFFERENT row set -- R-LAPTOP holds 85 rows
#      at W=3 and 60 at W=4 -- so a driver grepping its own worker name takes whichever section comes
#      first and dispatches 25 rows it was not assigned, with nothing anywhere reading wrong.
#
# So: `W` is a required parameter and a COLUMN in the plan. A driver that does not say which fleet size
# it is dispatching gets NO rows and a refusal, never the wrong ones.
#
# ---------------------------------------------------------------------------------------------------
# THE COOLDOWN IS BETWEEN SLICES, NOT BETWEEN ROWS
#
# Ruled at 4327ab7e1 section 2: slice cap 40 minutes, ten-minute cooldown, two slices and one gap for
# the i9's reserved leg. The cap is a BLAST-RADIUS limit on a box with one recorded thermal death, not
# a performance tuning knob -- and `-ShardCount`'s own comment says the gap belongs to the caller,
# which is this script. Both numbers are READ FROM THE PLAN rather than written here, so the ruling
# lives in one place and a plan generated under a different cap dispatches under that cap.
#
# A row is INDIVISIBLE: the sweep's unit of dispatch is a package. crypto/dsa alone is 1,317 s, which
# is why no cap below 21.95 minutes can exist, and why a slice holding one over-cap row is legal.
[CmdletBinding()]
param(
    # Every one of these three is MANDATORY and none has a default, deliberately. A default fleet size
    # or a guessed worker is the silent-wrong-rows failure above wearing a convenience.
    [Parameter(Mandatory)][string] $Plan,
    [Parameter(Mandatory)][string] $Worker,
    [Parameter(Mandatory)][int]    $FleetSize,

    # 0 = every slice this worker holds. i9's acceptance runs ONE slice, which is what this is for.
    [int] $OnlySlice = 0,

    # Print the dispatch -- every row, every slice boundary, every cooldown -- and run nothing. This is
    # the acceptance vehicle: it is decidable without a sweep, so a reader can check the driver
    # dispatches the rows the plan assigns before spending an hour finding out.
    [switch] $DryRun,

    # Per-row timings, TSV. Defaults beside the plan so a run is self-documenting.
    [string] $TimingOut,

    # The sweep. Defaulted RELATIVE TO THIS SCRIPT rather than to the caller's working directory,
    # because a driver resolved from $PWD runs a different tree's sweep than the one it was read from.
    [string] $SweepScript,

    # Escape hatch for the ramp experiment inside the recon leg (C2's (iii), ruled a slot there and not
    # before). -1 means "use the plan's". It is NOT a way to skip the discipline: 0 is accepted and
    # LOGGED AS A DEPARTURE on every slice boundary, so a run without gaps cannot be quietly mistaken
    # for a run with them.
    [int] $CooldownSecondsOverride = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Deny([string] $message) {
    Write-Host ''
    Write-Host "DISPATCH REFUSED: $message" -ForegroundColor Red
    exit 2
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $SweepScript) {
    $SweepScript = Join-Path $scriptDir 'run-validated-sweep.ps1'
}

if (-not (Test-Path -LiteralPath $Plan)) { Deny "no plan at '$Plan'" }
if (-not (Test-Path -LiteralPath $SweepScript)) {
    Deny "no sweep script at '$SweepScript' -- pass -SweepScript, or run this from a tree that has one"
}
if ($FleetSize -lt 1) { Deny "-FleetSize must be 1 or more (got $FleetSize)" }
if ($OnlySlice -lt 0) { Deny "-OnlySlice must be 0 (all slices) or a slice number (got $OnlySlice)" }

# ---------------------------------------------------------------- read the plan
# ReadAllText, not Get-Content: PS 5.1's Get-Content splits on line endings and hands back an array
# whose CR is already gone, so the CR REPORT below could never be anything but zero. Reading the bytes
# as text keeps the question answerable.
$planText = [System.IO.File]::ReadAllText($Plan)
$crCount = ([regex]::Matches($planText, "`r")).Count

# CRLF is NORMALISED and then REPORTED, rather than refused. The plan is a generated artifact and a
# Windows checkout may legitimately hand it back with CRLF; the digest is computed over LF-joined
# records so it is checkout-invariant. Reporting the count keeps the normalisation from hiding
# anything -- this lane has already paid twice for a CR that a reader silently translated away.
$planLines = $planText -replace "`r`n", "`n" -replace "`r", "`n"
$planLines = $planLines -split "`n"

$meta = @{}
$dataLines = New-Object System.Collections.Generic.List[string]
foreach ($line in $planLines) {
    if ($line.Length -eq 0) { continue }
    if ($line.StartsWith('#')) {
        $parts = $line.Substring(1) -split "`t"
        if ($parts.Count -ge 2) { $meta[$parts[0]] = $parts[1..($parts.Count - 1)] }
        continue
    }
    if ($line.StartsWith("W`tworker`t")) { continue }
    $dataLines.Add($line)
}

foreach ($required in @('version', 'digest', 'rows', 'slice_cap_seconds', 'cooldown_seconds')) {
    if (-not $meta.ContainsKey($required)) {
        Deny "the plan carries no #$required line -- this is not a plan emitted by shardmap.py --emit-plan"
    }
}
if ($meta['version'][0] -ne '1') {
    Deny "plan version '$($meta['version'][0])' is not 1 -- this driver reads version 1"
}

# ---------------------------------------------------------------- the DIGEST gate
# The same principle the coordinator ruled for the generator's INPUT (e0d5121e2 section 5: refuse when
# the parse does not reproduce the declared digest), applied to its OUTPUT. It runs BEFORE any row is
# selected, so a truncated or hand-edited plan cannot dispatch even one package.
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $builder = New-Object System.Text.StringBuilder
    foreach ($line in $dataLines) { [void] $builder.Append($line); [void] $builder.Append("`n") }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($builder.ToString())
    $computed = ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') }) -join ''
}
finally {
    $sha.Dispose()
}

$declaredDigest = $meta['digest'][0]
if ($computed -ne $declaredDigest) {
    Deny ("the plan's #digest does not reproduce over its own rows." + [Environment]::NewLine +
          "       declared $declaredDigest" + [Environment]::NewLine +
          "       computed $computed" + [Environment]::NewLine +
          "       $($dataLines.Count) row(s) parsed, #rows says $($meta['rows'][0]). A plan whose digest does not" + [Environment]::NewLine +
          "       reproduce has been truncated or edited, and dispatching from it would run a set nobody derived.")
}

$declaredRows = [int] $meta['rows'][0]
if ($dataLines.Count -ne $declaredRows) {
    Deny "#rows says $declaredRows and $($dataLines.Count) row(s) parsed (the digest matched, so this is a header/body disagreement in the generator, not a damaged file)"
}

# ---------------------------------------------------------------- parse the rows
$allRows = New-Object System.Collections.Generic.List[PSCustomObject]
foreach ($line in $dataLines) {
    $f = $line -split "`t"
    if ($f.Count -ne 7) {
        Deny "a plan row has $($f.Count) tab-separated field(s), expected 7: '$line'"
    }
    $allRows.Add([PSCustomObject]@{
        W        = [int] $f[0]
        Worker   = $f[1]
        Slice    = [int] $f[2]
        Seq      = [int] $f[3]
        Package  = $f[4]
        Cost     = [int] $f[5]
        Reserved = ($f[6] -eq '1')
    })
}

# ---------------------------------------------------------------- select, and refuse informatively
# Each refusal NAMES WHAT IS AVAILABLE. A bare "no rows" sends the reader to re-read the plan by eye,
# which is exactly the manual step the digest and the required -FleetSize exist to remove.
$atFleet = @($allRows | Where-Object { $_.W -eq $FleetSize })
if ($atFleet.Count -eq 0) {
    $sizes = (($allRows | ForEach-Object { $_.W } | Sort-Object -Unique) -join ', ')
    Deny "the plan carries no rows at -FleetSize $FleetSize. Fleet sizes present: $sizes"
}

$mine = @($atFleet | Where-Object { $_.Worker -eq $Worker })
if ($mine.Count -eq 0) {
    $workers = (($atFleet | ForEach-Object { $_.Worker } | Sort-Object -Unique) | ForEach-Object { "'$_'" }) -join ', '
    Deny ("no worker '$Worker' at -FleetSize $FleetSize. Workers present at that size: $workers" +
          [Environment]::NewLine +
          "       (the name must match the plan EXACTLY, parentheses and spaces included -- quote it)")
}

$sliceNumbers = @($mine | ForEach-Object { $_.Slice } | Sort-Object -Unique)
if ($OnlySlice -gt 0) {
    if ($sliceNumbers -notcontains $OnlySlice) {
        Deny "worker '$Worker' has no slice $OnlySlice at -FleetSize $FleetSize. Slices present: $($sliceNumbers -join ', ')"
    }
    $sliceNumbers = @($OnlySlice)
}

$cooldownSeconds = [int] $meta['cooldown_seconds'][0]
$cooldownSource = 'the plan'
if ($CooldownSecondsOverride -ge 0) {
    $cooldownSeconds = $CooldownSecondsOverride
    $cooldownSource = 'the -CooldownSecondsOverride flag -- A DEPARTURE FROM THE RULED GAP'
}
$sliceCapSeconds = [int] $meta['slice_cap_seconds'][0]

if (-not $TimingOut) {
    $TimingOut = Join-Path (Split-Path -Parent (Resolve-Path -LiteralPath $Plan)) 'h10-dispatch-timings.tsv'
}

# ---------------------------------------------------------------- announce what will happen
$modeWord = 'DISPATCH'
if ($DryRun) { $modeWord = 'DRY RUN -- nothing is executed' }

Write-Host ''
Write-Host "H10 dispatch driver -- $modeWord"
Write-Host "  plan            : $Plan"
if ($meta.ContainsKey('block')) {
    Write-Host "  measurement     : $($meta['block'] -join ' / ')"
}
if ($meta.ContainsKey('projection')) {
    # Printed on EVERY run, not once in a doc: section 3.2's rule is "say which it is, and gate
    # dispatch on it", and an operator reading a wall clock against this plan must know the costs are
    # a lower bound before they conclude the box is slow.
    Write-Host "  !! projection   : $($meta['projection'] -join ' -- ')"
}
Write-Host "  digest          : $declaredDigest (reproduced over $($dataLines.Count) row(s))"
Write-Host "  CR bytes in plan: $crCount (normalised before digesting; reported so the normalisation hides nothing)"
Write-Host "  worker          : $Worker   at W = $FleetSize"
Write-Host "  slice cap       : $sliceCapSeconds s ($([math]::Round($sliceCapSeconds / 60.0, 1)) min), from the plan"
Write-Host "  cooldown        : $cooldownSeconds s ($([math]::Round($cooldownSeconds / 60.0, 1)) min) BETWEEN slices, from $cooldownSource"
Write-Host "  sweep           : $SweepScript"
Write-Host "  timings         : $TimingOut"

$plannedCost = ($mine | Where-Object { $sliceNumbers -contains $_.Slice } | Measure-Object -Property Cost -Sum).Sum
$plannedCount = @($mine | Where-Object { $sliceNumbers -contains $_.Slice }).Count
$gapCount = [math]::Max(0, $sliceNumbers.Count - 1)
Write-Host ''
Write-Host ("  $plannedCount row(s) over $($sliceNumbers.Count) slice(s) [$($sliceNumbers -join ', ')], " +
            "$plannedCost i9-s of measured cost, $gapCount cooldown gap(s) = " +
            "$([math]::Round(($plannedCost + $gapCount * $cooldownSeconds) / 60.0, 1)) min including gaps")

# ---------------------------------------------------------------- run
$timings = New-Object System.Collections.Generic.List[string]
$timings.Add("w`tworker`tslice`tseq`tpackage`tcost_i9_s`treserved`twall_s`texit")
$failures = New-Object System.Collections.Generic.List[string]
$rowsRun = 0
$sliceOrdinal = 0

foreach ($sliceNumber in $sliceNumbers) {
    $sliceOrdinal++
    $sliceRows = @($mine | Where-Object { $_.Slice -eq $sliceNumber } | Sort-Object Seq)
    $sliceCost = ($sliceRows | Measure-Object -Property Cost -Sum).Sum

    Write-Host ''
    Write-Host ("=== slice $sliceNumber of [$($sliceNumbers -join ', ')] :: $($sliceRows.Count) row(s), " +
                "$sliceCost i9-s projected ===")

    foreach ($row in $sliceRows) {
        $marker = ''
        if ($row.Reserved) { $marker = ' [reserved]' }
        Write-Host ("  -> {0,-40} {1,6} i9-s{2}" -f $row.Package, $row.Cost, $marker)

        if ($DryRun) {
            $timings.Add("$($row.W)`t$($row.Worker)`t$($row.Slice)`t$($row.Seq)`t$($row.Package)`t$($row.Cost)`t$(if ($row.Reserved) { 1 } else { 0 })`t`tDRYRUN")
            continue
        }

        $started = Get-Date
        # -Filter <pkg> -Exact is the sweep's own documented per-package campaign interface: exact path
        # match, because substring 'io' sweeps bufio and io/fs alongside io and a per-row driver would
        # re-sweep large rows repeatedly.
        & $SweepScript -Filter $row.Package -Exact -SkipBuild:($rowsRun -gt 0)
        # CAPTURED IMMEDIATELY, before anything touches $? or a pipe. Floor 7, and the reason
        # safe-push.sh exists.
        $rowExit = $LASTEXITCODE
        $wall = [int] ((Get-Date) - $started).TotalSeconds
        $rowsRun++

        $reservedFlag = 0
        if ($row.Reserved) { $reservedFlag = 1 }
        $timings.Add("$($row.W)`t$($row.Worker)`t$($row.Slice)`t$($row.Seq)`t$($row.Package)`t$($row.Cost)`t$reservedFlag`t$wall`t$rowExit")

        if ($rowExit -ne 0) {
            $failures.Add("$($row.Package) (slice $($row.Slice), exit $rowExit)")
            Write-Host "     row exit $rowExit after $wall s" -ForegroundColor Yellow
        }
        else {
            Write-Host "     ok, $wall s"
        }
    }

    # The gap goes AFTER a slice and never after the last one -- a trailing sleep delays a result and
    # protects nothing. Written as an explicit ordinal test rather than "if not last", because the
    # off-by-one here costs ten minutes of every run and would never look wrong.
    if ($sliceOrdinal -lt $sliceNumbers.Count) {
        if ($cooldownSeconds -le 0) {
            Write-Host ''
            Write-Host ("  !! NO COOLDOWN between slice $sliceNumber and the next: the gap is $cooldownSeconds s from " +
                        "$cooldownSource. The ruled discipline is a ten-minute gap; this run is a DEPARTURE and " +
                        "its readings are not comparable with a gapped run.") -ForegroundColor Yellow
        }
        elseif ($DryRun) {
            Write-Host ''
            Write-Host "  (cooldown $cooldownSeconds s would run here)"
        }
        else {
            Write-Host ''
            Write-Host "  cooldown $cooldownSeconds s before slice $($sliceNumbers[$sliceOrdinal])..."
            Start-Sleep -Seconds $cooldownSeconds
        }
    }
}

# ---------------------------------------------------------------- the verdict, derived
[System.IO.File]::WriteAllText($TimingOut, (($timings -join "`n") + "`n"))
Write-Host ''
Write-Host "timings written: $TimingOut ($($timings.Count - 1) row(s))"

if ($DryRun) {
    Write-Host ''
    Write-Host "DRY RUN COMPLETE -- $plannedCount row(s) would run over $($sliceNumbers.Count) slice(s) with $gapCount gap(s). Nothing was executed."
    exit 0
}

# Derived from the count and exited on -- never a hardcoded verdict line. A confident parenthetical
# over a count that falsifies it is a check that cannot go red, and this repository has written one.
if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host "DISPATCH FAILED: $($failures.Count) of $rowsRun row(s) exited non-zero" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  $f" }
    exit 1
}

Write-Host ''
Write-Host "DISPATCH CLEAN: $rowsRun of $rowsRun row(s) exit 0 over $($sliceNumbers.Count) slice(s)" -ForegroundColor Green
exit 0
