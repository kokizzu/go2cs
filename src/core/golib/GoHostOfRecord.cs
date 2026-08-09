// GoHostOfRecord.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace go;

/// <summary>
/// Announces, once, when a converted program is running a build of the standard library that was
/// made for a different operating system than the one it is running on.
/// </summary>
/// <remarks>
/// <para>
/// This exists to make ONE documented cost of the NuGet packaging shape audible. A converted package
/// whose C# varies by GOOS ships a per-RID assembly for every runtime identifier the release
/// supports, plus a RID-agnostic <c>lib/</c> copy that NuGet resolves when no shipped RID matches the
/// running one. That fallback copy has to carry SOME flavor, and it carries the host of record —
/// Windows. On a RID the release does not ship (linux-arm64, say), a consumer therefore loads the
/// Windows flavor and gets Windows answers from a Linux machine: silently, and with no failure until
/// something platform-specific gives a wrong result.
/// </para>
/// <para>
/// The alternative packaging shapes were considered and rejected for reasons that have not changed
/// (see push-nuget.ps1's header): a <c>ref/</c> assembly would turn the fallback into a loud missing-
/// assembly error, but there is no neutral surface to put in it — <c>syscall</c> exports 270 names in
/// common out of 992/2,186/1,899, and <c>log/syslog</c> exports nothing at all on Windows. So the
/// fallback stays, and this makes it announce itself instead of being a footnote in a design document.
/// </para>
/// <para>
/// It is deliberately a WARNING and not an exception. The mismatch is not necessarily fatal — a
/// program using only pure-managed packages will behave correctly — and turning a working, if
/// unsupported, configuration into a hard failure would be a worse trade than telling the truth about
/// it once.
/// </para>
/// <para>
/// Platform-NEUTRAL by construction: it detects the host at run time and takes the compile-time GOOS
/// as an argument, so this file compiles to identical IL in every flavor of the runtime. Only the
/// caller (<c>core/runtime/hostofrecord_impl.cs</c>) differs between flavors, and only because the
/// constant it passes does.
/// </para>
/// </remarks>
public static class GoHostOfRecord
{
    private static int s_announced;

    /// <summary>
    /// Warns on standard error, at most once per process, when <paramref name="bakedGOOS"/> — the
    /// GOOS this assembly was built for — is not the operating system it is running on.
    /// </summary>
    /// <param name="bakedGOOS">The compile-time <c>runtime.GOOS</c> constant of the calling assembly.</param>
    public static void Verify(@string bakedGOOS)
    {
        string baked = bakedGOOS.ToString();
        string host = HostGOOS();

        // An unrecognized host says nothing. A warning naming a platform this build does not know how
        // to identify would be a guess, and a false alarm here is worse than a missed one: the whole
        // value of the message is that seeing it means something is genuinely wrong.
        if (host.Length == 0 || string.Equals(baked, host, StringComparison.Ordinal))
        {
            return;
        }

        if (Interlocked.Exchange(ref s_announced, 1) != 0)
        {
            return;
        }

        Console.Error.WriteLine(
            $"go2cs: WARNING - this program is running the \"{baked}\" build of the converted Go standard " +
            $"library on a \"{host}\" host.\n" +
            "  A published go2cs package carries a per-RID assembly for each runtime identifier the release\n" +
            "  ships, plus a RID-agnostic fallback that carries the host-of-record flavor (windows). Running\n" +
            "  on any other RID resolves that fallback, which is what has happened here.\n" +
            "  Platform-specific behavior - syscalls, file paths, time zones, process creation - will be\n" +
            $"  wrong or will fail. Publish for a supported runtime identifier, or build the corpus from\n" +
            $"  source with -p:GoTargetOS={host}.");
    }

    /// <summary>
    /// The running operating system named as Go names it, or <c>""</c> when it is one this build does
    /// not recognize.
    /// </summary>
    private static string HostGOOS()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "darwin";
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return "freebsd";
        }

        // OSPlatform.Create covers the remaining GOOS values .NET can report but has no
        // OperatingSystem.Is* helper for. Anything else falls through to silence.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ILLUMOS")))
        {
            return "illumos";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("SOLARIS")))
        {
            return "solaris";
        }

        return "";
    }
}
