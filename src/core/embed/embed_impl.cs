// embed_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// HAND-OWNED COMPANION to the converted embed package. It exists for one reason: `FS.files` is
// unexported, so only a file compiled into `embed_package` can set it, and something has to turn an
// assembly's embedded resources into the `file` list the converted `Open` / `ReadDir` / `ReadFile`
// already know how to read.
//
// ⚠ THERE ARE NO DECISIONS IN THIS FILE, and that is the design rather than a happy accident. Every
// rule that can be gotten subtly wrong — glob and directory expansion, the hidden-name asymmetry
// between `testdata` and `testdata/*`, the `all:` prefix, the empty-directory drop, the synthesized
// directory markers and the (dir, elem) order the binary search in `readDir` depends on — is
// resolved by the CONVERTER at conversion time (src/go2cs/embedDirective.go) and arrives here as an
// already-ordered list of names. This file looks each one up and copies bytes.
//
// The names are `go.embed/<import-path>[_test]/<path>` manifest resources, emitted as
// `<EmbeddedResource LogicalName="…">` items by the converter's project writers. An
// EmbeddedResource rather than a copied file because //go:embed promises the bytes are IN the
// binary: that is what survives a single-file publish and Native AOT, where a loose file does not.
//
// `file.hash` is left zero. The Go linker writes a truncated SHA-256 there and nothing in the
// converted package ever reads it (verified over embed.cs at the 1.24.13 corpus tip) — computing
// one would be ceremony with no reader.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace go;

public static partial class embed_package
{
    /// <summary>
    /// Builds an <see cref="FS"/> from an assembly's embedded resources.
    /// </summary>
    /// <param name="assembly">Assembly carrying the resources.</param>
    /// <param name="prefix">LogicalName prefix, <c>go.embed/&lt;import-path&gt;[_test]/</c>.</param>
    /// <param name="names">
    /// Every entry, ALREADY IN embed.FS's (dir, elem) ORDER, as the converter resolved them. A name
    /// ending in <c>/</c> is a synthesized directory marker and carries no resource.
    /// </param>
    /// <remarks>
    /// The order is a contract, not a convenience: <c>readDir</c> binary-searches this list and
    /// relies on a directory's contents forming one contiguous run. It is asserted, never re-sorted
    /// here — re-sorting would put the rule in two places and let them disagree.
    /// </remarks>
    public static FS ΔEmbedFS(Assembly assembly, @string prefix, params string[] names)
    {
        slice<@file> files = new(names.Length);

        for (nint i = 0; i < names.Length; i++)
        {
            string name = names[i];

            files[i] = name.EndsWith("/", StringComparison.Ordinal)
                ? new @file(name: name)
                : new @file(name: name, data: ΔEmbedString(assembly, prefix, name));
        }

        return new FS(files: Ꮡ(files));
    }

    /// <summary>
    /// Reads one embedded resource as a Go string.
    /// </summary>
    public static @string ΔEmbedString(Assembly assembly, @string prefix, @string name)
    {
        return new @string(ΔEmbedRaw(assembly, prefix, name));
    }

    /// <summary>
    /// Reads one embedded resource as a Go byte slice, over any byte-sized element type — Go permits
    /// <c>[]T</c> for a named byte type, which <c>embed/internal/embedtest</c>'s <c>TestAliases</c>
    /// exercises through <c>[]T</c>, <c>[]uint8</c> and <c>[]EmbedUint8</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ THE REINTERPRET IS THE MECHANISM, not an optimization over a conversion. This read
    /// <c>(T)Convert.ChangeType(raw[i], typeof(T))</c>, which is a VALUE conversion through
    /// <see cref="IConvertible"/> — and a Go named byte type converts to a C# struct that implements
    /// no such thing, so it threw <c>InvalidCastException: Invalid cast from 'System.Byte' to
    /// '…embedtest_internal_test_package+T'</c> the first time a corpus row reached it. The doc
    /// above named that exact case while the code could not express it: the fault was mine, at the
    /// only line that had to know what a Go named byte type IS.
    /// </para>
    /// <para>
    /// A named byte type has the layout of a byte and no conversion to perform, so the bytes are
    /// REINTERPRETED rather than converted. <c>sizeof(T)</c> is asserted and anything else refused
    /// LOUDLY: the constraint <c>unmanaged</c> admits every blittable struct, and silently
    /// reinterpreting a four-byte element over a one-byte source would read past the buffer. Go
    /// itself permits only a byte-sized element here, so the refusal can never fire on a valid
    /// directive — it fires on a converter that resolved the element type wrongly, which is the one
    /// caller this method has.
    /// </para>
    /// </remarks>
    public static slice<T> ΔEmbedBytes<T>(Assembly assembly, @string prefix, @string name) where T : unmanaged
    {
        // ⚠ Unsafe.SizeOf<T>() and NOT `sizeof(T)`: this package compiles with
        // <AllowUnsafeBlocks>false</AllowUnsafeBlocks> (embed.csproj), so the sizeof OPERATOR over a
        // generic unmanaged T needs an unsafe context this file may not open. Both helpers here are
        // ordinary managed calls; flipping the package's unsafe switch to satisfy one line would be
        // a corpus change for a reinterpretation that does not need one.
        int elementSize = Unsafe.SizeOf<T>();

        if (elementSize != 1)
            throw new InvalidOperationException($"go:embed resource \"{prefix}{name}\" cannot be read as a slice of \"{typeof(T)}\": a Go byte-slice embed has a ONE-byte element type and this one is {elementSize} bytes");

        byte[] raw = ΔEmbedRaw(assembly, prefix, name);
        slice<T> result = new(raw.Length);

        for (nint i = 0; i < raw.Length; i++)
            result[i] = Unsafe.BitCast<byte, T>(raw[i]);

        return result;
    }

    /// <summary>
    /// The one resource read. A MISSING resource throws rather than yielding empty: an empty
    /// //go:embed variable is precisely the defect this whole mechanism exists to remove, and it
    /// stayed invisible for a corpus hop because nothing complained. The message names the logical
    /// name it looked for, so a LogicalName that drifted from the emitted initializer says so.
    /// </summary>
    private static byte[] ΔEmbedRaw(Assembly assembly, @string prefix, @string name)
    {
        string logicalName = prefix.ToString() + name.ToString();

        using Stream? stream = assembly.GetManifestResourceStream(logicalName);

        if (stream is null)
            throw new InvalidOperationException($"go:embed resource \"{logicalName}\" is not present in assembly \"{assembly.GetName().Name}\"");

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
