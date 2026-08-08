@echo off
setlocal
rem ============================================================================
rem  release-nuget.bat -- publish a new SIGNED release of the go2cs NuGet packages
rem  when signing happens OFFLINE on a separate machine. Three steps:
rem
rem    Phase 1  bump the build number, then build + pack all Release packages.
rem    (manual) copy the packages to the signing machine, sign them
rem             (sign-nupkgs.bat, with NuGetCertFingerprint set), copy them back.
rem    Phase 2  push the signed packages.
rem
rem  The build number is bumped UP FRONT (Phase 1) so the packages that get built,
rem  signed and pushed all carry the SAME new version -- you cannot change a
rem  package's version after it is signed. Commit version.props after a success.
rem
rem  Phase 1 also FREEZES THE VALIDATION PROOF for the new version: it snapshots
rem  docs\validation\current -> docs\validation\<version> (write-once) and retargets
rem  the version-pinned badge links in src\core\*\README.md. Those files are part of
rem  the release commit, not an afterthought -- see the Phase 3 notes at the end.
rem
rem  Phase 1 ALSO MINTS THE SIGNED TAG nuget-<version>, at that same pre-build point
rem  instead of after the push, because every package README's C# Source badge links
rem  github.com/ritchiecarroll/go2cs/tree/nuget-<version>/src/core/<pkg>. Tagging later
rem  meant publishing 300 READMEs that all linked a tag which did not exist yet. So
rem  Phase 3 no longer tells you to tag -- it tells you to PUSH the tag already there.
rem
rem  Requires environment variable NUGET_API_KEY for the push.
rem  NOTE: if you abort before Phase 2 (or Phase 1 fails), version.props was
rem  already bumped -- either re-run and let it advance, or reset it (git checkout
rem  src\version.props) before the next release.
rem ============================================================================

set "SRC=%~dp0"
set "OUTDIR=%SRC%artifacts\nupkg"

echo.
echo === Phase 1: bump version and pack Release packages ===
powershell -NoProfile -ExecutionPolicy Bypass -File "%SRC%push-nuget.ps1" -BumpBuild -OutDir "%OUTDIR%"
if errorlevel 1 (
    echo Phase 1 ^(pack^) FAILED.
    exit /b 1
)

echo.
echo ============================================================================
echo  Packages are in:  %OUTDIR%
echo.
echo  DO THIS NOW ^(offline signing^):
echo    1. Copy  %OUTDIR%\*.nupkg  to the signing machine.
echo    2. Run  sign-nupkgs.bat  there ^(with NuGetCertFingerprint set^).
echo    3. Copy the SIGNED *.nupkg back into  %OUTDIR%  ^(overwrite the originals^).
echo.
echo  Then press any key to push the signed packages -- or close this window to stop.
echo ============================================================================
pause

echo.
echo === Phase 2: push the signed packages ===
if "%NUGET_API_KEY%"=="" (
    echo ERROR: NUGET_API_KEY is not set.
    exit /b 1
)
dotnet nuget push "%OUTDIR%\*.nupkg" --source https://api.nuget.org/v3/index.json --api-key %NUGET_API_KEY% --skip-duplicate
if errorlevel 1 (
    echo Phase 2 ^(push^) FAILED -- the signed packages are still in %OUTDIR%;
    echo re-run just the push manually once resolved.
    exit /b 1
)

rem --- Phase 3: record the release in git -------------------------------------
rem  The published version, its frozen proof snapshot and the badge links that point
rem  at it are ONE fact -- commit them together or a published badge links a
rem  directory that is not in the repository.
rem  The TAG is not part of this step any more: Phase 1 created it (signed) before
rem  anything was packed, so it points at the commit this release was built FROM --
rem  which differs from the release commit only by version.props, the snapshot and
rem  the retargeted README links. No converted C# moves between the two, so the tree
rem  every C# Source badge reaches is the C# that is in the packages. Push it.
rem  Read the version via a temp file, NOT a for /f backquote: cmd's for-parser
rem  treats the ')' inside PowerShell subexpressions as the end of the for body
rem  ("...was unexpected at this time"), which aborted Phase 3 on its first-ever
rem  run (1.23.1.3). Plain command lines tolerate parentheses; for bodies do not.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$x=[xml](Get-Content -Raw '%SRC%version.props'); $p=$x.Project.PropertyGroup; Set-Content -NoNewline -Path '%TEMP%\go2cs-release-ver.txt' -Value ($p.GoStdLibVersion + '.' + $p.GoBuildNumber)"
set /p VER=<"%TEMP%\go2cs-release-ver.txt"
del "%TEMP%\go2cs-release-ver.txt" >nul 2>&1

echo.
echo === Phase 3: record the release ===
echo  Commit these together, then push the commit and the tag Phase 1 minted:
echo.
echo    git add src\version.props docs\validation\%VER% src\core
echo    git commit -S -m "release: go2cs converted stdlib %VER%"
echo    git push ^&^& git push origin nuget-%VER%
echo.
echo  docs\validation\%VER% is FROZEN from here on -- it is the proof every
echo  %VER% package's green badge links and packs as VALIDATION.md.
echo  The tag nuget-%VER% ALREADY EXISTS locally ^(signed, created in Phase 1^);
echo  every published README's C# Source badge links it, so pushing it is not
echo  optional -- until it reaches GitHub those links 404.
exit /b 0
