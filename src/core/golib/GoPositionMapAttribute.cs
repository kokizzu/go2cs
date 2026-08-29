// GoPositionMapAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Records the Go source position map for one converted C# file.
/// </summary>
/// <param name="goFile">Identity of the Go source file this C# file was converted from.</param>
/// <param name="csFile">File name of the emitted C# file this record describes.</param>
/// <param name="table">Encoded C#-line to Go-line table for <paramref name="csFile"/>.</param>
/// <remarks>
/// <para>
/// The converter emits one of these per converted file, into the <c>GoSourcePositionMaps</c>
/// section of the package-info file of the compilation that compiles it (<c>package_info.cs</c>;
/// a test variant's records land in its own test-info anchor) — never into the converted source
/// itself, which stays reading like Go. The lookup is assembly-scoped either way: a frame reported
/// by <c>runtime.Caller</c> / <c>runtime.Stack</c> / <c>runtime.Callers</c> names the GO position
/// its C# position was converted from. Both halves — the file identity and the line — come from
/// this single record, which is what makes the pair INDIVISIBLE: a frame either has a record and
/// reports a Go position that exists, or has none and reports its honest converted C# position.
/// Nothing composes a position from the two.
/// </para>
/// <para>
/// <paramref name="goFile"/> is BUILD-SHAPE-FAITHFUL — it is spelled the way Go itself bakes a
/// source path for the same build, decided at conversion time because that is where Go decides it:
/// </para>
/// <list type="bullet">
///   <item><description>
///     A source under <c>GOROOT/src</c> is recorded GOROOT-relative (<c>runtime/debug/stack.go</c>),
///     matching the <c>-trimpath</c> form <c>cmd/go</c> applies to standard library packages.
///   </description></item>
///   <item><description>
///     A source converted BESIDE its emitted C# is recorded as its bare file name
///     (<c>main.go</c>), and the runtime roots it against the C# file's own directory — the
///     absolute path Go bakes for an ordinary build, without baking a machine-specific path into
///     a committed artifact.
///   </description></item>
///   <item><description>
///     Any other source is recorded as its absolute conversion-time path, which is exactly what
///     Go bakes for an ordinary untrimmed build.
///   </description></item>
/// </list>
/// <para>
/// <paramref name="table"/> is Base64 over a delta stream, one record per mapped C# line, ordered
/// by ascending C# line. A byte with its high bit set packs one record: bits 6-4 hold
/// <c>ΔcsLine - 1</c> and bits 3-0 hold the zig-zag encoded <c>ΔgoLine</c>. A <c>0x00</c> byte
/// introduces the extended form, an unsigned varint <c>ΔcsLine - 1</c> followed by an unsigned
/// varint zig-zag <c>ΔgoLine</c>. Lookup is a predecessor search — a C# line between two records
/// belongs to the earlier one — which is the same model Go's own <c>pclntab</c> uses, so a line
/// inside a multi-line emission answers the Go statement it was emitted for.
/// </para>
/// <para>
/// <paramref name="funcLits"/> is the FUNCTION-LITERAL name map, emitted only when the file
/// declares anonymous function literals: one <c>&lt;startLine&gt;-&lt;endLine&gt;:&lt;suffix&gt;</c>
/// entry per literal, semicolon-joined, in GO line space. Go names a literal
/// <c>Outer.funcN</c> — a per-enclosing-function, source-order counter starting at 1 — and a
/// nested literal appends its own per-parent counter (<c>Outer.funcN.M</c>); the suffix records
/// the dotted counter (<c>1</c>, <c>1.2</c>) without the <c>func</c> prefix. The runtime maps a
/// literal frame's C# line through <paramref name="table"/> to its Go line and answers the
/// innermost recorded span containing it, so the frame's name comes from a conversion-time fact
/// rather than from the compiler-generated lambda name, whose closure-group numbering matches
/// Go's counter only by coincidence. A record without this argument leaves those frames on the
/// derived fallback.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GoPositionMapAttribute(string goFile, string csFile, string table, string funcLits = "") : Attribute
{
    /// <summary>
    /// Gets the identity of the Go source file this C# file was converted from.
    /// </summary>
    public string GoFile => goFile;

    /// <summary>
    /// Gets the file name of the emitted C# file this record describes.
    /// </summary>
    public string CsFile => csFile;

    /// <summary>
    /// Gets the encoded C#-line to Go-line table.
    /// </summary>
    public string Table => table;

    /// <summary>
    /// Gets the encoded function-literal name map, or an empty string when the file declares no
    /// recorded literals.
    /// </summary>
    public string FuncLits => funcLits;
}
