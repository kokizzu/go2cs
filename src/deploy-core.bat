@echo off
rem Launcher for deploy-core.ps1 -- forwards all arguments to PowerShell so the script can be
rem run from cmd without the ExecutionPolicy / -File noise. Examples:
rem   deploy-core                deploy the converted standard library to %GOPATH%\src\go2cs
rem   deploy-core -NoBuild       stage only; skip the verify build
pushd "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy-core.ps1" %*
set "_ec=%ERRORLEVEL%"
popd
exit /b %_ec%
