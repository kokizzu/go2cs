// PackageAncestry.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace go.testing_runtime;

/// <summary>
/// Reconstructs, inside the run sandbox, the directory ANCESTRY <c>go test</c> gives a package, so
/// that a test resolving a path relative to its working directory reaches the same content Go's own
/// run does.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TestHost"/> already reproduced the ancestry's SHAPE — the working directory mirrors the
/// package's whole import path, so its own base name and its parents are named as Go names them. What
/// was missing is CONTENT: the parents were empty, so every cwd-relative read that leaves the package
/// directory failed on layout rather than on behavior. Six packages sat behind exactly that, each
/// reaching for something real one or more levels up — <c>io/ioutil</c> lists <c>..</c> for the
/// sibling <c>io</c> package's own files, <c>go/parser</c> reads <c>../printer/nodes.go</c> in its
/// package initializer, <c>internal/godebugs</c> reads <c>../../../doc/godebug.md</c>,
/// <c>internal/testenv</c> stats <c>../../../bin/go</c>, and <c>internal/coverage/cfile</c> needs a
/// <c>src/go.mod</c> above it for the toolchain's module walk to terminate.
/// </para>
/// <para>
/// <b>This view is an ANCESTRY, deliberately not a GOROOT.</b> GOROOT keeps pointing at the real Go
/// installation, and that distinction is the whole design rather than an omission. Reads THROUGH a
/// junction resolve to real content, but a directory WALK does not descend into one: Go reports a
/// junction from <c>Lstat</c> as an irregular file, so <c>filepath.WalkDir</c> steps over it. Measured
/// against Go 1.23.1 on a junction-mirrored root, a walk counting <c>*.gz</c> under GOROOT finds 0
/// where the real tree has 4, and a walk of <c>src/unicode</c> reports 1 entry against the real 19.
/// Two already-validated packages walk GOROOT that way (<c>compress/gzip</c>'s issue14937 test and
/// <c>path/filepath</c>), so repointing GOROOT at this view would REGRESS them. Leaving GOROOT real
/// costs nothing here, because every member of the class resolves against its working directory, and
/// a read through a junction is faithful. The one shape this cannot serve is a test that requires cwd
/// to sit under the GOROOT the process REPORTS — <c>go/build</c>'s <c>ImportDir(cwd)</c> — which is
/// why that package is censused rather than closed.
/// </para>
/// <para>
/// Directories are linked (a junction on Windows, a symlink elsewhere) and files are hard-linked, so
/// staging is a metadata operation rather than a copy: GOROOT's top level alone carries an 81 MB
/// installer archive that a per-run copy would multiply by every package in a sweep. The PACKAGE's own
/// directory is the exception and is populated with real copies, because it is the one directory a
/// test legitimately writes to.
/// </para>
/// <para>
/// Staging is best-effort by construction. A tree with no usable GOROOT — a clone with no Go
/// installation, a platform that refuses the link — leaves the sandbox exactly as it was before this
/// type existed, which is a working run for every package that does not read above itself.
/// </para>
/// </remarks>
internal static class PackageAncestry
{
    /// <summary>
    /// Stages the package's ancestry under <paramref name="runRoot"/>, mirroring GOROOT from its top
    /// level down to (but not including) the package's own directory.
    /// </summary>
    /// <returns>true when the ancestry was staged; false when it was skipped and the sandbox is
    /// unchanged.</returns>
    public static bool TryStage(string? goRoot, string importPath, string runRoot, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(goRoot))
            return false;

        string goRootSrc = Path.Combine(goRoot, "src");

        // A GOROOT without a source tree is not one this view can mirror. Checked rather than
        // assumed: GOROOT is an environment variable, so it can name anything at all.
        if (!Directory.Exists(goRootSrc))
            return false;

        string[] segments = importPath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return false;

        // The real package directory has to exist for the ancestry to mean anything — a converted
        // package whose Go sources are not in this GOROOT gets the unchanged sandbox.
        if (!Directory.Exists(Path.Combine(goRootSrc, Path.Combine(segments))))
            return false;

        try
        {
            ReclaimAbandonedSandboxes(runRoot);
            MarkOwner(runRoot);

            // GOROOT's own top level, carving out `src` for the descent.
            MirrorLevel(goRoot, runRoot, "src");

            // `src` is a level of the mirror exactly as it is a level of GOROOT: it is what makes
            // ../../.. from internal/godebugs land on the root rather than one short of it, and it is
            // where the toolchain's module walk finds `module std`. The last level is the package's
            // own directory, which is the working directory and is populated separately.
            string[] levels = ["src", .. segments];
            string realLevel = goRoot;
            string mirrorLevel = runRoot;

            for (int i = 0; i < levels.Length - 1; i++)
            {
                realLevel = Path.Combine(realLevel, levels[i]);
                mirrorLevel = Path.Combine(mirrorLevel, levels[i]);
                MirrorLevel(realLevel, mirrorLevel, levels[i + 1]);
            }

            // The package's own directory: real copies of its own files, and NO links for its
            // subdirectories. Those are the fixture staging's business — it creates the named ones and
            // fills testdata from the digest-tracked build output — and a junction there would put a
            // test's writes inside the real GOROOT.
            CopyOwnFiles(Path.Combine(goRootSrc, Path.Combine(segments)), workingDirectory);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // A partially staged ancestry is still a superset of the empty one, so the run continues.
            return false;
        }
    }

    /// <summary>
    /// Guarantees every component of <paramref name="directory"/> below <paramref name="runRoot"/> is
    /// a real directory, replacing any link this view staged with an empty one.
    /// </summary>
    /// <remarks>
    /// The fixture staging writes into ancestor-relative paths — compress/{flate,zlib,lzw} all read
    /// <c>../testdata/</c> — and those ancestors now hold links to the real GOROOT. Writing through
    /// one would put staged fixtures INSIDE the Go installation. Converting the component to an empty
    /// real directory first is what makes the sandbox a sandbox; it also restores exactly the
    /// pre-ancestry contract for those paths, since before this view they were empty too.
    /// </remarks>
    public static void EnsureWritable(string directory, string runRoot)
    {
        string full = Path.GetFullPath(directory);
        string root = Path.GetFullPath(runRoot);

        // Outside the sandbox there is nothing of this view's to unlink, but the directory is still
        // owed to the caller — every caller is about to write into it.
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(full);
            return;
        }

        // Walk root -> leaf so an outer link is replaced before an inner component is examined
        // through it.
        foreach (string component in ComponentsBetween(root, full))
        {
            if (IsLink(component))
            {
                new DirectoryInfo(component).Delete();
                Directory.CreateDirectory(component);
            }
        }

        Directory.CreateDirectory(full);
    }

    /// <summary>
    /// Removes the run sandbox without following the links this view staged.
    /// </summary>
    /// <remarks>
    /// <see cref="Directory.Delete(string, bool)"/> does not traverse a reparse point — verified, and
    /// the guarantee this whole design rests on — but it does not remove one either: it throws
    /// UnauthorizedAccessException and leaves the tree behind. Unlinking each one first is what makes
    /// the sandbox actually go away, and doing it depth-first means a link is gone before anything
    /// recursive reaches its parent.
    /// </remarks>
    public static void Delete(string runRoot)
    {
        if (!Directory.Exists(runRoot))
            return;

        // Unlinking comes FIRST and is exhaustive, because the two halves fail independently and
        // only one of them is dangerous. Removing the files can legitimately fail — a test that
        // shelled out to the Go toolchain leaves handles that outlive the child briefly, and
        // go/build's suite does it on every run — which strands the sandbox. A stranded sandbox full
        // of copies is inert; a stranded sandbox full of links INTO GOROOT is a trap for any tool
        // that later deletes the temp tree and follows reparse points (PowerShell 5.1's
        // Remove-Item -Recurse does). So every link is removed even when its siblings refuse, and a
        // failure to remove the emptied tree afterwards is not allowed to prevent that.
        Unlink(runRoot);

        try
        {
            Directory.Delete(runRoot, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left behind as ordinary files; the links are already gone.
        }

        static void Unlink(string directory)
        {
            string[] children;

            try
            {
                children = Directory.GetDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return;
            }

            foreach (string child in children)
            {
                try
                {
                    if (IsLink(child))
                        new DirectoryInfo(child).Delete();
                    else
                        Unlink(child);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // One link that will not go is not allowed to strand the rest.
                }
            }
        }
    }

    // Names the file that records which process owns a sandbox. Its presence is what makes an
    // abandoned sandbox distinguishable from a running one.
    private const string OwnerFileName = ".go2cs-owner";

    private static void MarkOwner(string runRoot)
    {
        try
        {
            using Process self = Process.GetCurrentProcess();
            Directory.CreateDirectory(runRoot);
            File.WriteAllText(Path.Combine(runRoot, OwnerFileName), $"{self.Id} {self.ProcessName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Without the marker this run's sandbox is simply never reclaimed by a later one.
        }
    }

    /// <summary>
    /// Removes sandboxes for THIS package that were left behind by a host that died without running
    /// its teardown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A normally-finishing run cleans up after itself, but two ways of dying skip the finally
    /// entirely: an uncatchable stack overflow (go/parser's depth suite produced one before the
    /// thread reservation was raised) and an external kill — which this repository documents as a
    /// routine hazard, since a cleanup preamble matching processes by NAME reaps sibling worktrees'
    /// runs. What is stranded then is not inert: it holds links INTO GOROOT, and the whole point of
    /// the teardown ordering is that such a tree must never outlive its run.
    /// </para>
    /// <para>
    /// Reclaiming is scoped so it can never touch a LIVE run, including one belonging to another
    /// worktree. Only sandboxes of the same package are considered — nothing else creates them — and
    /// only those whose recorded owner process is gone. An age threshold would not do: a legitimate
    /// suite can run for hours (hash/maphash takes ~40 minutes, index/suffixarray longer), so
    /// "old" and "abandoned" are different questions and only the second one is safe to act on.
    /// </para>
    /// </remarks>
    private static void ReclaimAbandonedSandboxes(string runRoot)
    {
        string? packageRoot = Path.GetDirectoryName(runRoot);

        if (packageRoot is null || !Directory.Exists(packageRoot))
            return;

        foreach (string sandbox in SafeDirectories(packageRoot))
        {
            if (string.Equals(sandbox, runRoot, StringComparison.OrdinalIgnoreCase) || IsOwnerAlive(sandbox))
                continue;

            try
            {
                Delete(sandbox);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        static string[] SafeDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }
    }

    // A sandbox counts as live unless its marker names a process that is demonstrably gone. Every
    // uncertainty resolves to "alive": an unreadable or absent marker, a malformed one, or a PID
    // whose lookup throws all leave the tree alone, because deleting a running sibling's sandbox is
    // far worse than leaving a dead one behind. The process NAME is compared alongside the id so a
    // recycled PID cannot make an unrelated process vouch for a sandbox.
    private static bool IsOwnerAlive(string sandbox)
    {
        string marker = Path.Combine(sandbox, OwnerFileName);

        if (!File.Exists(marker))
            return true;

        try
        {
            string[] parts = File.ReadAllText(marker).Split(' ', 2, StringSplitOptions.TrimEntries);

            if (parts.Length != 2 || !int.TryParse(parts[0], out int id))
                return true;

            using Process owner = Process.GetProcessById(id);
            return string.Equals(owner.ProcessName, parts[1], StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            // GetProcessById: no such process. The only answer that means "abandoned".
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    // Mirrors ONE directory level: every subdirectory becomes a link to the real one, every file a
    // hard link (a copy where the filesystem refuses one — a different volume, most often). The
    // carve-out is the next level down, which the caller materializes instead.
    private static void MirrorLevel(string realDirectory, string mirrorDirectory, string carveOut)
    {
        Directory.CreateDirectory(mirrorDirectory);

        DirectoryInfo real = new(realDirectory);

        foreach (FileSystemInfo entry in real.EnumerateFileSystemInfos())
        {
            string target = Path.Combine(mirrorDirectory, entry.Name);

            if (File.Exists(target) || Directory.Exists(target))
                continue;

            try
            {
                if (entry is DirectoryInfo)
                {
                    if (string.Equals(entry.Name, carveOut, StringComparison.OrdinalIgnoreCase))
                        continue;

                    CreateDirectoryLink(target, entry.FullName);
                }
                else
                {
                    CreateFileLink(target, entry.FullName);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // One unmirrored entry is a gap in the view, not a failed run.
            }
        }
    }

    // The package's own files, as real copies. Subdirectories are deliberately not touched.
    private static void CopyOwnFiles(string realDirectory, string workingDirectory)
    {
        Directory.CreateDirectory(workingDirectory);

        foreach (FileInfo file in new DirectoryInfo(realDirectory).EnumerateFiles())
        {
            string target = Path.Combine(workingDirectory, file.Name);

            if (File.Exists(target))
                continue;

            try
            {
                file.CopyTo(target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static bool IsLink(string path)
    {
        try
        {
            DirectoryInfo directory = new(path);

            // Attributes on a path that does not exist is (FileAttributes)(-1) — every bit set,
            // ReparsePoint among them — so existence has to be established first or a directory this
            // view never staged reads back as a link and gets "unlinked".
            return directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static IEnumerable<string> ComponentsBetween(string root, string full)
    {
        string relative = Path.GetRelativePath(root, full);

        if (relative is "." or "")
            yield break;

        string current = root;

        foreach (string segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Length == 0)
                continue;

            current = Path.Combine(current, segment);
            yield return current;
        }
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            // Unprivileged everywhere but Windows, and equivalent for this view's purpose: reads
            // resolve, walks do not descend.
            Directory.CreateSymbolicLink(link, target);
            return;
        }

        // A Windows SYMLINK needs SeCreateSymbolicLinkPrivilege — administrator, or Developer Mode —
        // which a test run cannot assume. A JUNCTION is the unprivileged equivalent for directories
        // and has no managed API, so it is set here by hand.
        CreateJunction(link, target);
    }

    private static void CreateFileLink(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            if (CreateHardLinkW(link, target, IntPtr.Zero))
                return;
        }
        else
        {
            try
            {
                File.CreateSymbolicLink(link, target);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        // Different volume, or a filesystem with no link support: the file is small enough to copy or
        // it is not one a test reads.
        File.Copy(target, link);
    }

    private const uint IoReparseTagMountPoint = 0xA0000003;
    private const uint FsctlSetReparsePoint = 0x000900A4;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;

    private static void CreateJunction(string link, string target)
    {
        Directory.CreateDirectory(link);

        // The reparse point stores an NT-namespace path; the print name is the plain one Explorer
        // and `dir` show.
        string substituteName = @"\??\" + Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);
        string printName = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);

        byte[] substitute = System.Text.Encoding.Unicode.GetBytes(substituteName);
        byte[] print = System.Text.Encoding.Unicode.GetBytes(printName);

        // REPARSE_DATA_BUFFER: an 8-byte header, then the 8-byte mount-point sub-header, then the two
        // NUL-terminated names back to back.
        int pathBufferLength = substitute.Length + 2 + print.Length + 2;
        int dataLength = 8 + pathBufferLength;
        int totalLength = 8 + dataLength;

        byte[] buffer = new byte[totalLength];
        int offset = 0;

        void WriteUInt32(uint value)
        {
            BitConverter.GetBytes(value).CopyTo(buffer, offset);
            offset += 4;
        }

        void WriteUInt16(ushort value)
        {
            BitConverter.GetBytes(value).CopyTo(buffer, offset);
            offset += 2;
        }

        WriteUInt32(IoReparseTagMountPoint);
        WriteUInt16((ushort)dataLength);
        WriteUInt16(0);
        WriteUInt16(0);                                    // SubstituteNameOffset
        WriteUInt16((ushort)substitute.Length);            // SubstituteNameLength
        WriteUInt16((ushort)(substitute.Length + 2));      // PrintNameOffset
        WriteUInt16((ushort)print.Length);                 // PrintNameLength

        substitute.CopyTo(buffer, offset);
        print.CopyTo(buffer, offset + substitute.Length + 2);

        using SafeFileHandle handle = CreateFileW(link, GenericWrite, 0, IntPtr.Zero, OpenExisting,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint, IntPtr.Zero);

        if (handle.IsInvalid)
            throw new IOException($"could not open '{link}' to set a junction", Marshal.GetLastWin32Error());

        IntPtr native = Marshal.AllocHGlobal(totalLength);

        try
        {
            Marshal.Copy(buffer, 0, native, totalLength);

            if (!DeviceIoControl(handle, FsctlSetReparsePoint, native, totalLength, IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new IOException($"could not set a junction at '{link}'", Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode,
        IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint ioControlCode, IntPtr inBuffer,
        int inBufferSize, IntPtr outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateHardLinkW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLinkW(string fileName, string existingFileName, IntPtr securityAttributes);
}
