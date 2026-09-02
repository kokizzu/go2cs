// PlatformExclusive.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The ONE definition of "is this behavioral package native to this host", shared by the two
// harnesses that enumerate behavioral packages: BehavioralRunner and the MSTest BehavioralTestBase.
// LINKED into both by path, for the same reason ConverterBuildInputs.cs is -- the runners take no
// assembly dependency on each other, so a shared assembly is not available, and three copies of a
// predicate is three chances for them to disagree about which packages a host may measure.
//
// The class this gates bites in BOTH directions and was invisible in each until it was named. A
// behavioral package whose Go source calls unix-only syscall API (Socketpair, UnixRights, SendmsgN)
// cannot be TYPE-CHECKED on Windows; one calling FindFirstFile or the WSA surface cannot be
// type-checked on Linux. The converter then emits a best-effort conversion, and every harness
// reported the result as a failure or -- worse -- as a byte-identical pass over a file it never
// regenerated. Marking the package makes the skip LOUD and BY NAME, which is the point: a silent
// drop from the enumeration would trade one invisible problem for another.
//
// NOT ALWAYS A TYPE-CHECK FAILURE, which is why the skip lines say "cannot measure" rather than
// "cannot type-check". Most marked packages fail at TRANSPILE (the converter cannot type-check
// unix-only or Win32-only syscall API), but LocalTimeZone type-checks anywhere and fails at OUTPUT:
// its converted form reaches syscall.GetTimeZoneInformation and faults on kernel32 off Windows.
// One marker covers both by construction -- a non-native host skips EVERY phase -- so the wording
// is the thing that has to stay honest about which phase actually bites.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

internal static class PlatformExclusive
{
    // Line-anchored on purpose. The attribute NAME appears in prose -- this file's own header is an
    // example -- and an unanchored match would gate a package on a comment. Same reasoning as the
    // corpus's GoManualConversion census, which reported 63 marked files against a real 40 until it
    // was anchored.
    private static readonly Regex s_marker =
        new(@"^\s*\[(?:go\.)?GoPlatformExclusive\s*\(([^)]*)\)\]", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex s_platform = new("\"([^\"]+)\"", RegexOptions.Compiled);

    /// <summary>
    /// The GOOS names a behavioral package is native to, or an empty array when it is unmarked
    /// (i.e. native everywhere, which is the overwhelming majority).
    /// </summary>
    public static string[] PlatformsFor(string packageDir)
    {
        string infoFile = Path.Combine(packageDir, "package_info.cs");

        if (!File.Exists(infoFile))
            return Array.Empty<string>();

        Match marker = s_marker.Match(File.ReadAllText(infoFile));

        if (!marker.Success)
            return Array.Empty<string>();

        return s_platform.Matches(marker.Groups[1].Value)
            .Select(m => m.Groups[1].Value)
            .ToArray();
    }

    /// <summary>
    /// The GOOS this host measures as. Honors GoTargetOS so a cross-target run gates on the target
    /// it is actually building for rather than on the machine it happens to run on -- the corpus's
    /// per-GOOS layout makes that a real distinction, not a hypothetical one.
    /// </summary>
    public static string HostGoos
    {
        get
        {
            string target = Environment.GetEnvironmentVariable("GoTargetOS");

            if (!string.IsNullOrWhiteSpace(target))
                return target.Trim().ToLowerInvariant();

            if (OperatingSystem.IsWindows())
                return "windows";

            return OperatingSystem.IsMacOS() ? "darwin" : "linux";
        }
    }

    /// <summary>
    /// True when the package is marked and this host is NOT among its platforms. An unmarked
    /// package is never skipped.
    /// </summary>
    public static bool ShouldSkip(string packageDir, out string platforms)
    {
        string[] native = PlatformsFor(packageDir);
        platforms = string.Join(", ", native);

        return native.Length > 0 && !native.Contains(HostGoos, StringComparer.OrdinalIgnoreCase);
    }
}
