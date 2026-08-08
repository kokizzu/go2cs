# set-version.ps1 -- sets the go2cs CONVERTER TOOL version (the go2cs.exe Windows resource).
#
# This is the go2cs *tool* version and is INDEPENDENT of the published NuGet package version:
# the converted stdlib packages, go.lib and go.gen take their version from src\version.props
# (base = converted Go release e.g. 1.23.1, build number bumped per publish by push-nuget.ps1).
# The csproj template and go2cs-gen.csproj no longer carry a <Version> element -- they inherit
# $(Version) from version.props -- so this script only stamps the winres resource.
#
# WINDOWS-ONLY BY NATURE, and deliberately NOT ported (F14, docs/PLAN-linux-operation.md). What it
# stamps is a Windows PE version resource; it also drives ReplaceInFiles.exe and `cmd /c pause`.
# There is nothing for it to do on Linux or macOS, so it fails fast rather than half-running.

# $IsWindows is a PowerShell 6+ automatic variable. On Windows PowerShell 5.1 it does not exist, and
# a bare `-not $IsWindows` would read $null as "not Windows" on the one platform where 5.1 runs.
$isWindowsHost = if ($null -eq (Get-Variable -Name 'IsWindows' -ErrorAction SilentlyContinue)) { $true } else { $IsWindows }

if (-not $isWindowsHost) {
    throw "set-version.ps1 stamps a Windows PE version resource (go-winres + ReplaceInFiles.exe) and has no non-Windows equivalent. Run it from Windows."
}

$version = "0.1.4"
$fullVersion = "$version.0"

# Update version in exe resource file
.\utilities\ReplaceInFiles\ReplaceInFiles.exe -x -e:UTF8 .\go2cs\winres\winres.json "version\`"\: \`"((\*~!~\d+)\.)+(\*~!~\d+)\`" "version\`": \`"$fullVersion\`"

# Rebuild resource file
cd go2cs
go install github.com/tc-hib/go-winres@latest
go-winres make
cd ..

cmd /c "pause"
