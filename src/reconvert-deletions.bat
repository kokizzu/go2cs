@echo off
rem Launcher for reconvert-deletions.ps1 -- the H5 deletion pass over a seeded reconvert root.
rem Forwards all arguments to PowerShell so the script can be run from cmd without the
rem ExecutionPolicy / -File noise, and propagates the script's exit code (0 clean, 2 unresolved
rem rows, 3 refused) exactly -- callers gate on it. Examples:
rem   reconvert-deletions -Root D:\scratch\h5\src -GoRoot C:\sdk\go1.24.13 -ExpectGo go1.24.13 -Sentinel D:\scratch\h5\run.stamp
rem   reconvert-deletions -Root ... -GoRoot ... -ExpectGo ... -Sentinel ... -Apply
pushd "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0reconvert-deletions.ps1" %*
set "_ec=%ERRORLEVEL%"
popd
exit /b %_ec%
