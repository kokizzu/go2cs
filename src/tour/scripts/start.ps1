[CmdletBinding()]
param(
    [string]$ListenAddress = "127.0.0.1:4000",
    [ValidateSet("core", "deployed", "nuget")]
    [string]$Runtime,
    [switch]$NoOpen,
    # Anything else is handed to the server, so every option it takes is
    # reachable from here -- as start.sh already passes "$@" through.
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ServerArguments
)

$ErrorActionPreference = "Stop"
$tourRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $tourRoot "..\..")).Path

Write-Host "Checking Go..."
& go version

Write-Host "Checking .NET..."
& dotnet --version

$goPath = (& go env GOPATH).Trim()
$tourBinary = Join-Path $goPath "bin\tour.exe"
if (-not (Test-Path -LiteralPath $tourBinary)) {
    Write-Host "Installing the official offline Tour of Go..."
    & go install golang.org/x/website/tour@latest
}

$env:GO_TOUR_BIN = $tourBinary

Push-Location $tourRoot
try {
    $arguments = @("run", ".", "-addr=$ListenAddress", "-repo=$repoRoot")
    if ($Runtime) {
        $arguments += "-runtime=$Runtime"
    }
    if ($NoOpen) {
        $arguments += "-no-open"
    }
    if ($ServerArguments) {
        $arguments += $ServerArguments
    }
    & go @arguments
}
finally {
    Pop-Location
}
