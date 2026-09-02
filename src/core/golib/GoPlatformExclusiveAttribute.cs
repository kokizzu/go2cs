// GoPlatformExclusiveAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Mark a converted package as NATIVE to specific platforms only, so harnesses skip it by name on
/// any other host rather than reporting it as a failure or, worse, as a vacuous pass.
/// </summary>
/// <remarks>
/// <para>
/// The attribute names the platform(s) the package's Go source can be type-checked and run on. It
/// says NOTHING about which host is reading it -- that comparison belongs to the harness.
/// </para>
/// <para>
/// The class it gates is real and bites in BOTH directions. A package whose Go source calls
/// unix-only <c>syscall</c> API (<c>Socketpair</c>, <c>UnixRights</c>, <c>SendmsgN</c>) cannot
/// type-check on Windows; one calling <c>FindFirstFile</c> or the WSA surface cannot type-check on
/// Linux. Before this marker each side reported the other's packages as NOT MEASURED -- correct but
/// indistinguishable, by name, from a genuine conversion failure, so a real regression could hide
/// among expected lines.
/// </para>
/// <para>
/// A marked package on a non-native host is SKIPPED BY NAME: printed with its platform, counted in
/// the summary, and excluded from the byte-identical and pass counts -- never silently dropped from
/// the enumeration, which would trade one invisible problem for another. On its native host it is
/// enumerated exactly as an unmarked package.
/// </para>
/// <para>
/// Hand-added to a package's <c>package_info.cs</c> beside <c>[GoTestMatchingConsoleOutput]</c>, and
/// preserved across a re-transpile by the same mechanism (measured on ScmRightsSeam: re-transpiling
/// with the marker present leaves the file differing from its pre-marker self by exactly the one
/// added line).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class GoPlatformExclusiveAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="GoPlatformExclusiveAttribute"/>.
    /// </summary>
    /// <param name="platforms">
    /// GOOS names the package is native to, e.g. <c>"linux"</c> or <c>"windows"</c>. More than one
    /// is allowed for a package native to several but not all.
    /// </param>
    public GoPlatformExclusiveAttribute(params string[] platforms) => Platforms = platforms;

    /// <summary>
    /// Gets the GOOS names this package is native to.
    /// </summary>
    public string[] Platforms { get; }
}
