@echo off
rem ============================================================================
rem  release-nuget.bat -- launcher for release-nuget.ps1: the SIGNED release,
rem  end to end, on one machine.
rem
rem    Phase 0  preflight (clean tree, API key, signing certificate reachable)
rem    Phase 1  bump the build number, pack, freeze the proof, mint the signed tag
rem    Phase 2  sign every package in ONE process -- ONE card PIN prompt
rem    Phase 3  publish to nuget.org (gated; a version can be unlisted, never deleted)
rem    Phase 4  print the commit-and-push-the-tag commands
rem
rem    release-nuget.bat -WhatIf           census: preconditions only, nothing moves
rem    release-nuget.bat                   the full ritual, with a confirm before the push
rem    release-nuget.bat -Yes              ditto, no confirm (the PIN is still yours)
rem    release-nuget.bat -OfflineSigning   pack here, sign elsewhere (the pre-2026-08-24 flow)
rem
rem  Requires NUGET_API_KEY and (unless -OfflineSigning) NuGetCertFingerprint.
rem  If Phase 1 fails, version.props was already bumped: re-run and let it advance,
rem  or `git checkout src\version.props` and delete the minted tag.
rem ============================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release-nuget.ps1" %*
