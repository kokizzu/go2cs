// GoCgoImportDynamicAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Records one Go <c>//go:cgo_import_dynamic</c> pragma: the bodyless trampoline the emission
/// declares, the dynamic symbol it stands for, and the library that exports it —
/// <c>//go:cgo_import_dynamic libc_fork fork "/usr/lib/libSystem.B.dylib"</c> ⇒
/// <c>[assembly: GoCgoImportDynamic("libc_fork_trampoline", "fork", "/usr/lib/libSystem.B.dylib")]</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the CLASS-B half of the synthetic-PC design's discriminator, and it exists because the two
/// halves of <c>abi.FuncPCABI0</c> want opposite things. A PC read BACK — pprof, <c>runtime.Callers</c>,
/// <c>textAddr</c> — wants a synthetic token that symbolizes and is never dereferenced. A darwin
/// trampoline wants the exact opposite: a REAL, callable address, because the value is what
/// <c>rawSyscall</c> jumps to. Handing that site a token would be the same failure as the
/// <c>return default</c> it replaces — plausible, unique, stable, and fatal on first call.
/// </para>
/// <para>
/// The discriminator is therefore "is this argument a cgo-imported trampoline", and it is decided by
/// the presence of a record here. A stub the <c>PartialStubGenerator</c> marked as having no managed
/// body, WITH a record, is class B and resolves through <see cref="System.Runtime.InteropServices.NativeLibrary"/>;
/// the same stub WITHOUT a record is class C — Go's own assembly (<c>goexit</c>, <c>asyncPreempt</c>,
/// <c>sigtramp</c>, the syscall family), which has nothing to resolve from and no managed equivalent,
/// and stays a loud throw. No silent zero survives on either path.
/// </para>
/// <para>
/// It is an ASSEMBLY attribute rather than a central registry for a reason of ownership: the resolver
/// lives in <c>internal/abi</c> while the trampolines live in <c>syscall</c> and
/// <c>crypto/x509/internal/macos</c>, so the resolver must read data it does not own and cannot
/// reference. Reaching it from the ARGUMENT — the delegate's method, its declaring type, that type's
/// assembly, and this attribute on that assembly — needs no cross-assembly registry, no initialization
/// order, and no contribution at all from a package that declares no trampolines.
/// </para>
/// <para>
/// The map is derivable twice over and the two derivations cross-check for free: every pragma in the
/// darwin tree has the form <c>//go:cgo_import_dynamic libc_&lt;n&gt; &lt;sym&gt; "&lt;lib&gt;"</c>, and
/// <c>&lt;n&gt;</c> equals <c>&lt;sym&gt;</c> across all of them. The converter emits from the pragma it
/// already preserves into the emission, so the record and the comment above the declaration cannot
/// drift apart without the emission itself changing.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class GoCgoImportDynamicAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="GoCgoImportDynamicAttribute"/>.
    /// </summary>
    /// <param name="trampolineName">
    /// The emitted trampoline method's name — the key, matched against
    /// <see cref="System.Reflection.MemberInfo.Name"/> of the delegate's target method.
    /// </param>
    /// <param name="symbol">The dynamic symbol the trampoline stands for.</param>
    /// <param name="library">The library path the pragma names.</param>
    public GoCgoImportDynamicAttribute(string trampolineName, string symbol, string library)
    {
        TrampolineName = trampolineName;
        Symbol = symbol;
        Library = library;
    }

    /// <summary>
    /// Gets the emitted trampoline method's name, e.g. <c>libc_fork_trampoline</c>.
    /// </summary>
    /// <remarks>
    /// The key is the METHOD NAME rather than a handle because an attribute argument must be a
    /// compile-time constant, and a <c>RuntimeMethodHandle</c> is not one. The name is sufficient:
    /// trampolines are file-scoped statics in one package class per platform flavor, so a name is
    /// unique within the assembly the lookup is scoped to.
    /// </remarks>
    public string TrampolineName { get; }

    /// <summary>
    /// Gets the dynamic symbol, e.g. <c>fork</c>.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the exporting library, e.g. <c>/usr/lib/libSystem.B.dylib</c>.
    /// </summary>
    public string Library { get; }
}
