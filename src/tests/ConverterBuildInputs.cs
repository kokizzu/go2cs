// ConverterBuildInputs.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The ONE definition of "what go2cs.exe is built from", shared by every harness that decides whether
// to rebuild the converter before using it: BehavioralRunner, the MSTest BehavioralTestBase, and
// PerformanceRunner. It is LINKED into all three by path -- the two runners are deliberately
// dependency-free (they reference no project, not even golib) so a shared assembly is not available
// -- and it is declared in the GLOBAL namespace so the link costs no using directive in any of them.
//
// FALSE-GREEN route #5 (CLAUDE.md) is what this file exists for. All three predicates asked whether
// any TOP-LEVEL *.go file in src/go2cs was newer than the binary. The converter's build inputs are
// wider than that in two ways, and each one changes what the converter EMITS while touching no
// top-level .go file at all -- so the predicate reported "up to date", the previous binary kept
// running, and every gate validated the OLD emission and went green:
//
//   1. The //go:embed assets. embeddedTemplates.go embeds the two csproj templates, the
//      package_info.cs skeleton, the icons and profiles/*; stdlibMetadata.go embeds
//      stdlib-metadata.txt. Editing one changes every project the converter emits.
//   2. The internal/ packages the converter imports (internal/stdlibmeta and its siblings). A
//      top-level-only enumeration never saw those either.
//
// go.mod / go.sum join them for the same reason: a `go` directive bump or a dependency change alters
// the built binary exactly as a source edit does.
//
// The embedded set is DERIVED from the //go:embed directives themselves rather than listed here, so
// a directive added tomorrow is covered the day it is written and nothing has to be remembered. The
// directive FORMS this resolver understands are pinned from the Go side by
// src/go2cs/embeddedAssets_test.go, which fails the plain `go test ./...` if a directive is ever
// written in a shape this file would silently fail to resolve -- that guard is the other half of the
// remedy, and without it the derivation could go quietly blind again.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

internal static class ConverterBuildInputs
{
    // The literal the directive scan anchors on. Go requires the comment marker and `go:embed` to be
    // adjacent, so this is the whole prefix -- there is no `// go:embed` spelling to accept.
    private const string EmbedDirective = "//go:embed";

    // The one pattern prefix Go defines: it widens a directory match to include names beginning with
    // `.` or `_`, which are otherwise excluded.
    private const string AllPrefix = "all:";

    /// <summary>
    /// Every file the converter binary is built from, under <paramref name="converterSrcDir"/>.
    /// </summary>
    public static IReadOnlyList<string> Enumerate(string converterSrcDir)
    {
        List<string> inputs = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string goFile in GoSourceFiles(converterSrcDir))
        {
            Add(inputs, seen, goFile);

            foreach (string asset in EmbeddedAssets(goFile))
                Add(inputs, seen, asset);
        }

        foreach (string moduleFile in new[] { "go.mod", "go.sum" })
        {
            string path = Path.Combine(converterSrcDir, moduleFile);

            if (File.Exists(path))
                Add(inputs, seen, path);
        }

        return inputs;
    }

    /// <summary>
    /// Whether the converter at <paramref name="converterExePath"/> is older than any of its build
    /// inputs (or absent), i.e. whether a `go build` is owed before the binary may be trusted.
    /// </summary>
    public static bool IsConverterStale(string converterSrcDir, string converterExePath)
    {
        if (!File.Exists(converterExePath))
            return true;

        // FALSE-GREEN route #4 (CLAUDE.md; PLAN-corpus-upgrade H1.4): a TOOLCHAIN hop touches none
        // of the build inputs below, so after installing a new Go release every mtime answer here
        // still said "up to date" and every gate kept running a binary embedding the OLD release's
        // go/parser + go/types front end against the NEW release's sources -- which does not fail
        // cleanly, it degrades into the best-effort "did not fully type-check" path. The stamp is
        // INHERENT: every Go binary embeds runtime.Version(), and `go version <exe>` reads it back
        // (measured: `go version go2cs.exe` prints `<path>: go1.23.1`). A binary whose embedded
        // release differs from the live `go env GOVERSION` is stale exactly as if a source file
        // had changed, whatever its timestamp says.
        //
        // Failure shapes fail STALE-wards on purpose: an unreadable stamp (corrupt or non-Go exe)
        // or an unanswerable GOVERSION forces a rebuild, and a rebuild without a working toolchain
        // then fails LOUDLY at `go build` -- never a silent pass on an unverified binary. The two
        // probes cost one short-lived `go` process each, once per staleness question, which every
        // harness asks once per run.
        string? embedded = EmbeddedGoRelease(converterExePath);
        string? live = LiveGoRelease();

        if (embedded is null || live is null || !string.Equals(embedded, live, StringComparison.Ordinal))
            return true;

        DateTime built = File.GetLastWriteTimeUtc(converterExePath);

        return Enumerate(converterSrcDir).Any(input => File.GetLastWriteTimeUtc(input) > built);
    }

    /// <summary>
    /// The Go release the binary at <paramref name="converterExePath"/> was built with, read from
    /// the buildinfo every Go binary embeds (via <c>go version &lt;exe&gt;</c>), or <c>null</c>
    /// when it cannot be read.
    /// </summary>
    public static string? EmbeddedGoRelease(string converterExePath)
    {
        // Output shape: `<path>: go1.23.1` (a devel toolchain prints a longer token; taken
        // verbatim either way, since equality against GOVERSION is the only question asked).
        string? output = RunGo($"version \"{converterExePath}\"");

        if (output is null)
            return null;

        int separator = output.LastIndexOf(": ", StringComparison.Ordinal);

        if (separator < 0)
            return null;

        string release = output[(separator + 2)..].Trim();

        return release.StartsWith("go", StringComparison.Ordinal) ? release : null;
    }

    /// <summary>
    /// The live toolchain's release (<c>go env GOVERSION</c>), or <c>null</c> when it cannot be
    /// answered.
    /// </summary>
    public static string? LiveGoRelease()
    {
        string? output = RunGo("env GOVERSION");

        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    // One `go` invocation, first stdout line, null on any failure -- the callers above treat null
    // as "stale", so nothing here needs to throw.
    private static string? RunGo(string arguments)
    {
        try
        {
            using Process process = new();

            process.StartInfo.FileName = "go";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            if (!process.Start())
                return null;

            string output = process.StandardOutput.ReadLine() ?? "";

            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();

            if (!process.WaitForExit(30_000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    // Directory names excluded from the walk: the three Go itself ignores when loading packages
    // (`testdata`, and anything beginning with `.` or `_`), plus the harness build output -- `bin` is
    // where go2cs.exe is written, so walking it would compare the binary against itself.
    private static bool IsSkippedDirectory(string name)
    {
        return name.Length == 0 ||
               name[0] == '.' ||
               name[0] == '_' ||
               name.Equals("testdata", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("obj", StringComparison.OrdinalIgnoreCase);
    }

    // Every .go file under root, RECURSIVELY -- internal/ included. The predicates this replaces
    // enumerated the top level only.
    private static IEnumerable<string> GoSourceFiles(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        Stack<string> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            foreach (string file in Directory.EnumerateFiles(directory, "*.go"))
                yield return file;

            foreach (string child in Directory.EnumerateDirectories(directory))
            {
                if (!IsSkippedDirectory(Path.GetFileName(child)))
                    pending.Push(child);
            }
        }
    }

    // The files one .go file embeds. A directive is a line whose first non-space text is exactly
    // `//go:embed` followed by whitespace, so prose that merely NAMES the directive -- both
    // embeddedTemplates.go's header and stdlibMetadata_test.go's do -- is not mistaken for one.
    private static IEnumerable<string> EmbeddedAssets(string goFile)
    {
        string directory = Path.GetDirectoryName(goFile) ?? ".";

        foreach (string line in File.ReadLines(goFile))
        {
            string text = line.TrimStart();

            if (!text.StartsWith(EmbedDirective, StringComparison.Ordinal))
                continue;

            string remainder = text[EmbedDirective.Length..];

            if (remainder.Length > 0 && !char.IsWhiteSpace(remainder[0]))
                continue;

            foreach (string pattern in remainder.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (string asset in Resolve(directory, pattern))
                    yield return asset;
            }
        }
    }

    // Resolves one //go:embed pattern against the directory of the file that declared it. The
    // supported subset -- slash-separated, wildcards in the final segment only, an optional `all:`
    // prefix, no quoting and no `..` -- is asserted from the Go side by embeddedAssets_test.go, so a
    // pattern this cannot resolve fails a gate instead of silently resolving to nothing.
    private static IEnumerable<string> Resolve(string directory, string pattern)
    {
        bool includeHidden = pattern.StartsWith(AllPrefix, StringComparison.Ordinal);

        if (includeHidden)
            pattern = pattern[AllPrefix.Length..];

        string[] segments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            yield break;

        string parent = directory;

        for (int index = 0; index < segments.Length - 1; index++)
            parent = Path.Combine(parent, segments[index]);

        if (!Directory.Exists(parent))
            yield break;

        string last = segments[^1];

        IEnumerable<string> matches = HasWildcard(last)
            ? Directory.EnumerateFileSystemEntries(parent, last)
            : new[] { Path.Combine(parent, last) };

        foreach (string match in matches)
        {
            if (File.Exists(match))
            {
                yield return match;
            }
            else if (Directory.Exists(match))
            {
                foreach (string nested in DirectoryContents(match, includeHidden))
                    yield return nested;
            }
        }
    }

    // A directory match embeds the directory's whole subtree. Names beginning with `.` or `_` are
    // excluded unless the pattern carried the `all:` prefix -- Go's own rule.
    private static IEnumerable<string> DirectoryContents(string directory, bool includeHidden)
    {
        foreach (string file in Directory.EnumerateFiles(directory))
        {
            if (includeHidden || !IsHidden(Path.GetFileName(file)))
                yield return file;
        }

        foreach (string child in Directory.EnumerateDirectories(directory))
        {
            if (!includeHidden && IsHidden(Path.GetFileName(child)))
                continue;

            foreach (string nested in DirectoryContents(child, includeHidden))
                yield return nested;
        }
    }

    private static bool IsHidden(string name)
    {
        return name.Length > 0 && (name[0] == '.' || name[0] == '_');
    }

    private static bool HasWildcard(string segment)
    {
        return segment.IndexOfAny(['*', '?', '[']) >= 0;
    }

    private static void Add(List<string> inputs, HashSet<string> seen, string path)
    {
        string full = Path.GetFullPath(path);

        if (seen.Add(full))
            inputs.Add(full);
    }
}
