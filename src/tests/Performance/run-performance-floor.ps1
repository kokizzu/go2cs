<#
.SYNOPSIS
    Exploratory performance-FLOOR harness -- startup time, binary size, peak working set.

.DESCRIPTION
    A standalone instrument for docs/PLAN-bflat-perf-exploration.md. It answers a question the
    canonical three-column suite does not: how much of the Native AOT floor (process start, bytes
    on disk, resident set) is recoverable by feature-stripping, and how much of that recovery needs
    a new toolchain (bflat) versus switches the stock .NET SDK already exposes.

    It is DELIBERATELY separate from PerformanceRunner and run-performance.ps1, which it never
    touches and never writes through. It publishes each benchmark once per VARIANT into its own
    bin\Release\expl-<variant>\ tree, verifies the timing-filtered stdout against the Go binary
    exactly as PerformanceRunner's Verify phase does -- nothing that fails Verify is ever timed --
    then reports elapsed, on-disk size, and peak working set as JSON plus a markdown summary.

    Measurement conventions are copied from PerformanceRunner so the numbers are commensurate with
    the canonical table: 1 discarded warmup + N measured single-shot runs, median; PerfStartup
    reports process WALL time (its workload is empty), every other benchmark reports the in-program
    'elapsed_ns:' workload time; peak memory is the process peak working set, polled while alive.

    Variant isolation note: the per-variant publishes deliberately SHARE obj\aot\ and the script
    wipes it between variants. Giving each variant its own intermediate via a command-line
    -p:BaseIntermediateOutputPath does not work -- that is a GLOBAL property, so it propagates into
    all 57 referenced projects, each then carries two obj trees, and both generated AssemblyInfo
    files get compiled (CS0579 duplicate-attribute, across the whole closure).

.PARAMETER Benchmarks
    Comma-separated benchmark project names. Default: the plan's five-benchmark floor set.

.PARAMETER Variants
    Comma-separated variant keys from the table below. Default: A0,A1,A2.

.PARAMETER Runs
    Measured runs per variant (default 5), each preceded by one discarded warmup.

.PARAMETER Phase
    Comma list of: publish,verify,measure (default all three). 'publish' may be skipped to
    re-measure existing trees.

.PARAMETER OutFile
    JSON results path. Default: a timestamped file beside this script under .floor-results\.

.EXAMPLE
    ./run-performance-floor.ps1 -Benchmarks PerfStartup -Variants A0,A1,A2,A3,A4,A5
#>
[CmdletBinding()]
param(
    [string]$Benchmarks = 'PerfStartup,PerfFib,PerfMap,PerfString,PerfChannel',
    [string]$Variants   = 'A0,A1,A2',
    [int]   $Runs       = 5,
    [string]$Phase      = 'publish,verify,measure',
    [string]$OutFile,
    [string]$BflatExe
)
# $NetVersion (and the platform primitives) come from the shared definition -- the TFM census's
# Class D hoist: one spelled TFM for every instrument, never a per-script literal.
. (Join-Path $PSScriptRoot '../../_paths.ps1')

Set-StrictMode -Version Latest

# A native tool writing to stderr must not become a terminating NativeCommandError -- under 'Stop'
# the converter's and MSBuild's ordinary warnings abort the run mid-publish and can leave a live
# child behind (CLAUDE.md, r41).
$ErrorActionPreference = 'Continue'

# Windows PowerShell 5.1 is the only shell on the perf-canon host, so this script stays inside its
# language surface: no Join-String, no ternary, no Process.Kill(bool).

# ---------------------------------------------------------------------------------------------
# Variant table.
#
# A0 is the control: exactly what PerformanceRunner publishes for the canonical AOT column, so a
# floor number is always read against a baseline this script measured itself, on the same machine
# in the same session, rather than against a table row from another day.
#
# A1/A2 are the composite "AOT-min" profiles the plan asks for. A3..A5 isolate one switch each so
# a win can be ATTRIBUTED rather than merely observed. TrimMode stays partial and reflection stays
# on in every variant: golib's fmt formatting and sort's Interface<T> bind members reflectively,
# so stripping either would change behavior, not just size.
# ---------------------------------------------------------------------------------------------
#
# StackTraceSupport and IlcGenerateStackTraceData are genuinely DISTINCT levers, which is why both
# appear: the former is an SDK feature switch (it defines System.Diagnostics.StackTrace.IsSupported
# as a trim substitution, Microsoft.NET.Sdk.targets ~L567), the latter tells ILC not to emit the
# stack-trace metadata blob for the compiled closure at all. Only the second one is a size lever
# proportional to the converted stdlib.
$VariantTable = [ordered]@{
    'A0' = @{ Label = 'baseline (canonical AOT column)'; Props = @() }

    'A1' = @{ Label = 'AOT-min, size'; Props = @(
                'InvariantGlobalization=true', 'UseSystemResourceKeys=true',
                'StackTraceSupport=false', 'IlcGenerateStackTraceData=false',
                'OptimizationPreference=Size') }

    'A2' = @{ Label = 'AOT-min, speed'; Props = @(
                'InvariantGlobalization=true', 'UseSystemResourceKeys=true',
                'StackTraceSupport=false', 'IlcGenerateStackTraceData=false',
                'OptimizationPreference=Speed') }

    # Single-switch isolates, so a win can be ATTRIBUTED to a lever rather than merely observed.
    'A3' = @{ Label = 'InvariantGlobalization only';       Props = @('InvariantGlobalization=true') }
    'A4' = @{ Label = 'StackTraceSupport=false only';      Props = @('StackTraceSupport=false') }
    'A5' = @{ Label = 'OptimizationPreference=Size only';  Props = @('OptimizationPreference=Size') }
    'A6' = @{ Label = 'UseSystemResourceKeys only';        Props = @('UseSystemResourceKeys=true') }
    'A7' = @{ Label = 'IlcGenerateStackTraceData=false only'; Props = @('IlcGenerateStackTraceData=false') }

    # ---- OUT-OF-PROFILE floor probes. NOT candidate publish profiles. ----
    #
    # PLAN-bflat-perf-exploration.md §2 fixes TrimMode=partial for Arm A, and that constraint is
    # correct for a profile anyone would ship: golib's fmt formatting and sort's Interface<T> bind
    # members reflectively, and full trim can strip exactly those. These variants exist ONLY to
    # locate the floor -- the stock AOT binary is ~288 MiB for a program that prints two lines, and
    # a floor map that cannot say how much of that is trim ROOTING rather than codegen is not a
    # map. Passing Verify on five small benchmarks does NOT establish that full trim is safe for
    # the corpus; these benchmarks do not exercise fmt's reflective surface broadly. Read any X row
    # as "the floor is at least this low", never as "adopt this".
    'X1' = @{ Label = 'PROBE: TrimMode=full (out of profile)'; Props = @('TrimMode=full') }
    'X2' = @{ Label = 'PROBE: AOT-min size + TrimMode=full (out of profile)'; Props = @(
                'InvariantGlobalization=true', 'UseSystemResourceKeys=true',
                'StackTraceSupport=false', 'IlcGenerateStackTraceData=false',
                'OptimizationPreference=Size', 'TrimMode=full') }

    # ---- Arm B: bflat. Same ILC/RyuJIT lineage, different driver and different DEFAULTS. ----
    #
    # bflat runs no Roslyn source generators, so it can only compile the benchmark's OWN sources;
    # the whole converted closure is consumed as already-built IL via -r, which is exactly what
    # makes this viable -- the four go2cs-gen generators already ran when the SDK built those
    # assemblies. --stdlib DotNet is the only viable mode (PLAN §1): --stdlib:zero and
    # --no-reflection are out of scope because golib's fmt formatting and sort's Interface<T> bind
    # members reflectively.
    'B1' = @{ Label = 'bflat, DotNet stdlib, -Ot'; Bflat = @('-Ot') }
    'B2' = @{ Label = 'bflat, stripped, -Os'; Bflat = @(
                '-Os', '--no-globalization', '--no-stacktrace-data', '--no-exception-messages') }
}

# ---- paths -----------------------------------------------------------------------------------
$PerfDir = $PSScriptRoot
$SrcRoot = (Resolve-Path (Join-Path $PerfDir '..\..')).Path
$Go2csPathArg = ($SrcRoot -replace '\\', '/').TrimEnd('/') + '/'

# @() is load-bearing: a single-element Split pipes through ForEach-Object as a bare string, and
# under StrictMode $string.Count then throws PropertyNotFoundStrict.
$benchList = @($Benchmarks.Split(',', [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() })
$varList   = @($Variants.Split(',',   [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() })
$phases    = @($Phase.Split(',',      [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim().ToLowerInvariant() })

foreach ($v in $varList) {
    if (-not $VariantTable.Contains($v)) { throw "Unknown variant '$v'. Known: $($VariantTable.Keys -join ',')" }
}

if (-not $OutFile) {
    $resultsDir = Join-Path $PerfDir '.floor-results'
    New-Item -ItemType Directory -Force $resultsDir | Out-Null
    $OutFile = Join-Path $resultsDir ("floor-{0}.json" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
}

# The ILC native link step probes for MSVC link.exe through vswhere; the same PATH prepend
# PerformanceRunner does, for the same reason.
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (Test-Path $vsInstaller) { $env:PATH = "$vsInstaller;$env:PATH" }

# Never hand ILC targets to a machine-wide idle worker node whose environment lacks the above.
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

function Write-Status([string]$Message) {
    Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $Message)
}

# ---- measurement primitives ------------------------------------------------------------------

# Single-shot execution with peak-working-set polling, mirroring PerformanceRunner.RunMeasured.
function Invoke-Measured {
    param([string]$Exe, [string]$WorkDir, [int]$TimeoutMs = 120000)

    $psi = [Diagnostics.ProcessStartInfo]::new()
    $psi.FileName               = $Exe
    $psi.WorkingDirectory       = $WorkDir
    $psi.UseShellExecute        = $false
    $psi.CreateNoWindow         = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true

    $proc = [Diagnostics.Process]::new()
    $proc.StartInfo = $psi

    $sw = [Diagnostics.Stopwatch]::StartNew()
    [void]$proc.Start()

    # Read stdout on a background task so a chatty child can never deadlock on a full pipe.
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $peak = 0L
    try { $peak = $proc.PeakWorkingSet64 } catch { }

    while (-not $proc.WaitForExit(1)) {
        try {
            $proc.Refresh()
            $ws = $proc.PeakWorkingSet64
            if ($ws -gt $peak) { $peak = $ws }
        } catch { }

        if ($sw.ElapsedMilliseconds -gt $TimeoutMs) {
            # .NET Framework (which hosts PS 5.1) has no Kill(entireProcessTree) overload; taskkill
            # /T is the equivalent, and is scoped to this PID so it can never reap a sibling lane.
            & taskkill /F /T /PID $proc.Id 2>&1 | Out-Null
            try { $proc.Kill() } catch { }
            [void]$proc.WaitForExit(5000)
            return [pscustomobject]@{ ExitCode = -1; StdOut = ''; WallMs = $sw.Elapsed.TotalMilliseconds; PeakBytes = $peak }
        }
    }

    $sw.Stop()
    $proc.WaitForExit()

    $result = [pscustomobject]@{
        ExitCode  = $proc.ExitCode
        StdOut    = $stdoutTask.Result
        WallMs    = $sw.Elapsed.TotalMilliseconds
        PeakBytes = $peak
    }
    $proc.Dispose()
    return $result
}

function Get-FilteredOutput([string]$StdOut) {
    $kept = ($StdOut -replace "`r", '').Split("`n") | Where-Object { -not $_.StartsWith('elapsed_ns:') }
    return ($kept -join "`n")
}

function Get-InnerMs([string]$StdOut) {
    foreach ($line in ($StdOut -replace "`r", '').Split("`n")) {
        if ($line.StartsWith('elapsed_ns:')) {
            $ns = 0L
            if ([long]::TryParse($line.Substring('elapsed_ns:'.Length).Trim(), [ref]$ns)) { return $ns / 1000000.0 }
        }
    }
    return 0.0
}

function Get-Median([double[]]$Values) {
    if ($Values.Count -eq 0) { return 0.0 }
    $s = $Values | Sort-Object
    $mid = [int]($s.Count / 2)
    if ($s.Count % 2 -eq 1) { return $s[$mid] }
    return ($s[$mid - 1] + $s[$mid]) / 2.0
}

# On-disk cost of a deployment: the primary executable, and the whole tree minus debug symbols
# (a .pdb ships with neither a Go binary nor a release deployment, so counting it would flatter
# neither column honestly).
function Get-TreeSize([string]$Dir) {
    if (-not (Test-Path $Dir)) { return 0L }
    $files = Get-ChildItem $Dir -Recurse -File | Where-Object { $_.Extension -ne '.pdb' }
    if (-not $files) { return 0L }
    return ($files | Measure-Object -Property Length -Sum).Sum
}

# ---- variant descriptors ---------------------------------------------------------------------
# 'go' and 'jit' are reference columns built by the normal toolchain paths, not published variants.
function Get-VariantExe([string]$Bench, [string]$Variant) {
    switch ($Variant) {
        'go'    { return Join-Path $PerfDir "$Bench\bin\Release\Go\$Bench.exe" }
        'jit'   { return Join-Path $PerfDir "$Bench\bin\Release\$NetVersion\$Bench.exe" }
        default { return Join-Path $PerfDir "$Bench\bin\Release\expl-$Variant\$Bench.exe" }
    }
}

function Get-VariantDir([string]$Bench, [string]$Variant) {
    return Split-Path (Get-VariantExe $Bench $Variant)
}

# Build wall-time per "<bench>/<variant>". This is a REPORTED metric, not diagnostics: the stock
# rooted profile spends ~1000 s in ILC compiling the whole converted closure, while the same
# benchmark under full trim links in ~30 s, and that 30x is one of the exploration's findings.
$buildSeconds = @{}

# ---- Phase: publish ---------------------------------------------------------------------------
if ($phases -contains 'publish') {
    Write-Status "PUBLISH: $($varList.Count) variant(s) x $($benchList.Count) benchmark(s)"

    foreach ($variant in $varList) {
        $spec = $VariantTable[$variant]

        # ---- Arm B: bflat drives its own compiler; there is no MSBuild publish here. ----
        if ($spec.Contains('Bflat')) {
            if (-not $BflatExe) { throw "Variant $variant needs -BflatExe <path to bflat.exe>" }

            # Existence, not just non-emptiness. This script runs at 'Continue', so an unresolvable
            # compiler would not stop the pipeline below: `& $BflatExe` would write a command-not-found
            # record into $log and leave $LASTEXITCODE at whatever the previous native command set --
            # 0, on a healthy run -- and the arm would report "ok" for a benchmark it never compiled
            # (false-green route #6, CLAUDE.md).
            if (-not (Test-Path -LiteralPath $BflatExe)) { throw "-BflatExe does not exist: $BflatExe" }

            foreach ($bench in $benchList) {
                $outDir = Get-VariantDir $bench $variant
                if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
                New-Item -ItemType Directory -Force $outDir | Out-Null

                # The already-built JIT tree is BOTH the reference closure and the source of the
                # SDK-generated global-usings file the converted C# depends on (the <Using> items
                # in the csproj template become PerfX.GlobalUsings.g.cs under obj\). bflat never
                # reads a csproj, so that file must be passed explicitly or nothing compiles.
                $jitDir = Join-Path $PerfDir "$bench\bin\Release\$NetVersion"
                $refs = @(Get-ChildItem $jitDir -Filter *.dll -File | Where-Object { $_.BaseName -ne $bench })

                $srcs = @(Get-ChildItem (Join-Path $PerfDir $bench) -Filter *.cs -File |
                            Where-Object { $_.Name -notlike '*_test.cs' -and $_.Name -ne 'package_test_info.cs' } |
                            ForEach-Object { $_.FullName })

                $globalUsings = Join-Path $PerfDir "$bench\obj\Release\$NetVersion\$bench.GlobalUsings.g.cs"
                if (Test-Path $globalUsings) { $srcs += $globalUsings }

                # go2cs-gen output for the benchmark itself, if any. Measured 2026-08-16: all five
                # floor benchmarks emit ZERO generated files (they declare no interfaces, no
                # embedded structs, no pointer receivers), so for THIS set the harvest is empty --
                # the generators' real output lives inside the referenced core assemblies, already
                # compiled. A benchmark that did emit some would be picked up here.
                $genDir = Join-Path $PerfDir "$bench\Generated"
                if (Test-Path $genDir) {
                    $srcs += @(Get-ChildItem $genDir -Recurse -Filter *.cs -File | ForEach-Object { $_.FullName })
                }

                $bfArgs = @('build') + $srcs +
                          @('--stdlib', 'DotNet', '--arch', 'x64', '--os', 'windows',
                            '-o', (Join-Path $outDir "$bench.exe")) + $spec.Bflat
                foreach ($r in $refs) { $bfArgs += @('-r', $r.FullName) }

                Write-Status "  bflat  $bench [$variant] $($spec.Label)"
                $sw = [Diagnostics.Stopwatch]::StartNew()
                $log = & $BflatExe @bfArgs 2>&1 | Out-String
                $code = $LASTEXITCODE
                $sw.Stop()

                if ($code -eq 0) {
                    $buildSeconds["$bench/$variant"] = [math]::Round($sw.Elapsed.TotalSeconds, 1)
                    Get-ChildItem $outDir -Filter '*.pdb' -File -ErrorAction SilentlyContinue | Remove-Item -Force
                    # IL2xxx trim-analysis warnings are expected and abundant here (golib binds
                    # members reflectively by design); they are not failures. Count, don't dump.
                    $warn = @($log.Split("`n") | Where-Object { $_ -match 'IL\d{4}' }).Count
                    Write-Status ("    ok ({0:N0}s, {1} trim-analysis warnings)" -f $sw.Elapsed.TotalSeconds, $warn)
                } else {
                    Write-Status ("    FAILED exit {0} ({1:N0}s)" -f $code, $sw.Elapsed.TotalSeconds)
                    Write-Host $log
                }
            }
            continue
        }

        foreach ($bench in $benchList) {
            $csproj = Join-Path $PerfDir "$bench\$bench.csproj"
            $outDir = Get-VariantDir $bench $variant

            # Variants differ only by MSBuild properties, so the incremental check cannot be
            # trusted to notice; wipe the shared native intermediate and publish cold. This also
            # self-heals the truncated-obj/LNK1106 trap a killed publish leaves behind.
            $objAot = Join-Path $PerfDir "$bench\obj\aot"
            if (Test-Path $objAot) { Remove-Item $objAot -Recurse -Force }
            if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

            # NOT $args -- that is an automatic variable in PowerShell.
            $pubArgs = @(
                'publish', $csproj, '-nologo', '-clp:ErrorsOnly', '-c', 'Release',
                '-p:PerfAot=true', "-p:go2csPath=$Go2csPathArg", '-o', $outDir
            )
            foreach ($p in $spec.Props) { $pubArgs += "-p:$p" }

            Write-Status "  publish $bench [$variant] $($spec.Label)"
            Write-Status "    props: $($spec.Props -join ' ')"
            $sw = [Diagnostics.Stopwatch]::StartNew()
            $log = & dotnet @pubArgs 2>&1 | Out-String
            $code = $LASTEXITCODE
            $sw.Stop()

            if ($code -eq 0) {
                $buildSeconds["$bench/$variant"] = [math]::Round($sw.Elapsed.TotalSeconds, 1)
                # Each AOT publish drops a ~1.5 GB .pdb next to a ~290 MB executable. It is excluded
                # from every size figure this script reports (Get-TreeSize), is never read at run
                # time, and 16 publishes of it is ~24 GB of pure churn -- so it goes immediately.
                # Nothing measured here changes as a result.
                Get-ChildItem $outDir -Filter '*.pdb' -File -ErrorAction SilentlyContinue | Remove-Item -Force
                Write-Status ("    ok ({0:N0}s)" -f $sw.Elapsed.TotalSeconds)
            } else {
                Write-Status ("    FAILED exit {0} ({1:N0}s)" -f $code, $sw.Elapsed.TotalSeconds)
                Write-Host $log
            }
        }
    }
}

# ---- Phase: verify + measure ------------------------------------------------------------------
$records = [Collections.Generic.List[object]]::new()
$allVariants = @('go', 'jit') + $varList

foreach ($bench in $benchList) {
    $workDir = Join-Path $PerfDir $bench

    # The Go binary is the correctness reference for every other column, so it is run first and
    # unconditionally; a benchmark whose Go binary is missing is skipped entirely rather than
    # measured against nothing.
    $goExe = Get-VariantExe $bench 'go'
    if (-not (Test-Path $goExe)) {
        Write-Status "SKIP $bench -- Go binary missing ($goExe)"
        continue
    }

    $goRef = $null
    if ($phases -contains 'verify') {
        $r = Invoke-Measured -Exe $goExe -WorkDir $workDir
        if ($r.ExitCode -ne 0) { Write-Status "SKIP $bench -- Go binary exit $($r.ExitCode)"; continue }
        $goRef = Get-FilteredOutput $r.StdOut
    }

    foreach ($variant in $allVariants) {
        $exe = Get-VariantExe $bench $variant
        $dir = Get-VariantDir $bench $variant

        $label = $variant
        if ($VariantTable.Contains($variant)) { $label = $VariantTable[$variant].Label }

        $rec = [ordered]@{
            Benchmark = $bench
            Variant   = $variant
            Label     = $label
            Verified  = $false
            Note      = ''
            BuildSec  = $null
            ExeBytes  = 0L
            TreeBytes = 0L
            TimeMs    = $null
            WallMs    = $null
            PeakMB    = $null
        }

        if (-not (Test-Path $exe)) {
            $rec.Note = 'exe missing'
            $records.Add([pscustomobject]$rec)
            continue
        }

        $rec.ExeBytes  = (Get-Item $exe).Length
        $rec.TreeBytes = Get-TreeSize $dir
        if ($buildSeconds.ContainsKey("$bench/$variant")) { $rec.BuildSec = $buildSeconds["$bench/$variant"] }

        # ---- Verify: identical timing-filtered stdout vs Go, or no number. ----
        if ($phases -contains 'verify') {
            $r = Invoke-Measured -Exe $exe -WorkDir $workDir
            if ($r.ExitCode -ne 0) {
                $rec.Note = "run exit $($r.ExitCode)"
                $records.Add([pscustomobject]$rec); continue
            }
            $filtered = Get-FilteredOutput $r.StdOut
            if ($variant -eq 'go') {
                $rec.Verified = $true
            } elseif ($filtered -ceq $goRef) {
                $rec.Verified = $true
            } else {
                $rec.Note = "OUTPUT MISMATCH vs Go: [$filtered] vs [$goRef]"
                Write-Status "  DISQUALIFIED $bench [$variant] -- $($rec.Note)"
                $records.Add([pscustomobject]$rec); continue
            }
        } else {
            $rec.Verified = $true
            $rec.Note = 'verify skipped'
        }

        # ---- Measure ----
        if ($phases -contains 'measure') {
            [void](Invoke-Measured -Exe $exe -WorkDir $workDir)   # discarded warmup

            $walls = [Collections.Generic.List[double]]::new()
            $inners = [Collections.Generic.List[double]]::new()
            $peaks  = [Collections.Generic.List[double]]::new()
            $bad = $false

            for ($i = 0; $i -lt $Runs; $i++) {
                $r = Invoke-Measured -Exe $exe -WorkDir $workDir
                if ($r.ExitCode -ne 0) { $rec.Note = "measure run exit $($r.ExitCode)"; $bad = $true; break }
                $walls.Add($r.WallMs)
                $inners.Add((Get-InnerMs $r.StdOut))
                $peaks.Add([double]$r.PeakBytes)
            }

            if (-not $bad) {
                $rec.WallMs = [math]::Round((Get-Median $walls.ToArray()), 1)
                # PerfStartup's workload is empty; its meaningful number is process wall time.
                $rec.TimeMs = if ($bench -eq 'PerfStartup') { $rec.WallMs } else { [math]::Round((Get-Median $inners.ToArray()), 1) }
                $rec.PeakMB = [math]::Round((Get-Median $peaks.ToArray()) / 1MB, 1)
            }
        }

        Write-Status ("  {0,-12} {1,-4} time={2,10} exe={3,9:N0}KB tree={4,9:N0}KB peak={5,6}MB {6}" -f `
            $bench, $variant, $rec.TimeMs, ($rec.ExeBytes / 1KB), ($rec.TreeBytes / 1KB), $rec.PeakMB, $rec.Note)

        $records.Add([pscustomobject]$rec)
    }
}

# ---- report -----------------------------------------------------------------------------------
$cpu = (Get-ItemProperty 'HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0').ProcessorNameString.Trim()
$env_line = [ordered]@{
    Cpu        = $cpu
    Os         = [Runtime.InteropServices.RuntimeInformation]::OSDescription.Trim()
    GoVersion  = (& go version) -split ' ' | Select-Object -Index 2
    DotnetSdk  = (& dotnet --version).Trim()
    Date       = (Get-Date -Format 'yyyy-MM-dd')
    Runs       = $Runs
}

$payload = [ordered]@{ Environment = $env_line; Variants = $VariantTable; Records = $records }
$payload | ConvertTo-Json -Depth 6 | Set-Content -Path $OutFile -Encoding UTF8
Write-Status "results -> $OutFile"
