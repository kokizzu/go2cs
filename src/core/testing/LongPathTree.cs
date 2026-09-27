// LongPathTree.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

using System;
using System.IO;
using System.Text;
using System.Threading;

// go2cs HAND-OWNED (whole file) — part of the Phase-4 test host (see PackageAncestry.cs for the
// ownership rationale).
[module: go.GoManualConversion]

namespace go.testing_runtime;

/// <summary>
/// Makes a directory tree removable by .NET when it is deeper than the platform's PATH_MAX.
/// </summary>
/// <remarks>
/// <para>
/// Go removes a tree relative to directory descriptors (os/removeall_at.go), so no call it makes
/// names more than one component and depth is irrelevant. .NET's deletion names FULL paths, and on
/// Linux a path past PATH_MAX throws ArgumentException ("The value cannot be an empty string") out of
/// both <c>Directory.Delete(path, true)</c> and an enumerate-and-recurse walk — measured 2026-09-26 on
/// the WSL arm, in the host and in a standalone .NET 10 program. os's TestGetwdDeep builds exactly
/// such a tree in its TempDir; it PASSED in C# and its cleanup then failed it as an
/// infrastructure-error, and the sandbox it stranded made the next os run's reclaim throw before its
/// first test.
/// </para>
/// <para>
/// <see cref="Flatten"/> keeps every path the deletion will name under the limit, with the one
/// operation that moves depth without naming it: a directory whose own path is nearing the limit is
/// RENAMED to a short name directly under the root being removed (its path is still short enough to
/// name, because its parent's was), and the walk continues from there. The tree's contents are
/// unchanged, only re-parented within the tree that is about to be deleted, so the caller's own
/// removal — with its own link and read-only semantics — then runs unchanged over a tree it can name.
/// Links are never followed, only real directories are moved, and nothing leaves the root.
/// </para>
/// <para>
/// Windows names long paths natively, so there this is a no-op.
/// </para>
/// </remarks>
internal static class LongPathTree
{
    // NAME_MAX is 255 on every Unix this host targets; PATH_MAX is 4096 on Linux and 1024 on the BSDs,
    // Darwin among them. A directory at or under the threshold can name any child: its own bytes, the
    // separator, a longest-possible component and the terminating NUL still fit.
    private const int NameMax = 255;
    private static readonly int s_pathMax = OperatingSystem.IsLinux() ? 4096 : 1024;
    private static readonly int s_threshold = s_pathMax - NameMax - 2;

    private static int s_moved;

    /// <summary>
    /// Re-parents every directory of <paramref name="root"/> that is too deep for .NET to name, so a
    /// subsequent full-path removal of <paramref name="root"/> succeeds. Best-effort: an entry that
    /// cannot be moved is left where it is, and the caller's removal reports it as it always has.
    /// </summary>
    public static void Flatten(string root)
    {
        if (OperatingSystem.IsWindows() || !Directory.Exists(root))
            return;

        // A worklist, not recursion: the tree being flattened is by definition deep.
        System.Collections.Generic.Stack<string> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            if (directory != root && Encoding.UTF8.GetByteCount(directory) > s_threshold)
            {
                string moved = Path.Combine(root, $".go2cs-deep-{Interlocked.Increment(ref s_moved)}-{Environment.ProcessId}");

                try
                {
                    Directory.Move(directory, moved);
                    directory = moved;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue;
                }
            }

            string[] children;

            try
            {
                children = Directory.GetDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (string child in children)
            {
                if (!IsLink(child))
                    pending.Push(child);
            }
        }
    }

    // A link is removed as the link itself by every caller, never traversed, so it is never moved or
    // descended into here either — its target lies outside the tree being removed.
    private static bool IsLink(string path)
    {
        try
        {
            return new DirectoryInfo(path).LinkTarget is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }
}
