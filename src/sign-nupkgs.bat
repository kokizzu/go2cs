@echo off
rem ============================================================================
rem  sign-nupkgs.bat -- launcher for sign-nupkgs.ps1 (author-sign the packed
rem  *.nupkg with the code-signing certificate in this machine's store).
rem
rem  CENSUS BY DEFAULT: a bare run reports what it would sign and proves the
rem  certificate is reachable. Pass -Apply to actually sign.
rem
rem    sign-nupkgs.bat                     census the default artifacts folder
rem    sign-nupkgs.bat -Apply              sign them
rem    sign-nupkgs.bat -PackageDir D:\pkgs -Apply
rem
rem  The fingerprint comes from %NuGetCertFingerprint% (SHA-256, NOT the SHA-1
rem  value the Windows certificate dialog labels "Thumbprint" -- the script
rem  detects that mistake and prints the right value).
rem ============================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sign-nupkgs.ps1" %*
