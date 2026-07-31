// GoImplementAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Marks an assembly with a structure interface implementation mapping.
/// </summary>
/// <typeparam name="TStruct">Struct type that implements interface.</typeparam>
/// <typeparam name="TInterface">Interface type to implement.</typeparam>
/// <remarks>
/// <para>
/// This attribute is used to auto-generate backend C# code needed to implement
/// an interface for a structure using matching receiver methods.
/// </para>
/// <para>
/// See the <c>ImplementGenerator</c> in the go2cs code generators for details.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class GoImplementAttribute<TStruct, TInterface> : Attribute
{
    /// <summary>
    /// Gets or sets flag that determines if interface is a promoted
    /// field in the structure.
    /// </summary>
    public bool Promoted { get; set; }

    /// <summary>
    /// Gets or sets flag indicating the Go interface cast source was a POINTER to the struct
    /// (<c>var s Iface = &amp;t</c>). The generator then emits an <see cref="IжAdapter"/> wrapper
    /// class holding the receiver box <c>ж&lt;TStruct&gt;</c> — the interface value aliases the
    /// original box exactly as Go's interface holds the <c>*T</c> — instead of the value-boxing
    /// partial-struct implementation (which copies, and cannot bind direct-ж receiver methods).
    /// </summary>
    public bool Pointer { get; set; }

    /// <summary>
    /// Gets or sets flag indicating this records a SELF-REFERENTIAL constraint proxy: the Go
    /// element type <c>TStruct</c> (a pointer type <c>*P</c>) satisfies a generic method-set
    /// constraint interface <c>TInterface</c> (<c>nistPoint[Point]</c>) only structurally, so it
    /// is used as a generic type's type argument (<c>nistCurve[*P224Point]</c>). Because the golib
    /// box <c>ж&lt;TStruct&gt;</c> cannot nominally implement a package interface, the generator
    /// emits a proxy class <c>TStructжTInterface : TInterface&lt;itself&gt;</c> that wraps the box
    /// and carries implicit <c>ж&lt;TStruct&gt;</c>↔proxy conversions, so every self-typed
    /// (<c>T</c>) parameter and result marshals across the boundary automatically. The proxy is
    /// what gets substituted for the type argument at the constrained instantiation. The
    /// <c>TInterface</c> type argument's own type argument is a placeholder — the generator uses
    /// its open definition and closes it over the proxy itself.
    /// </summary>
    public bool ConstraintProxy { get; set; }
}

/// <summary>
/// Marks a generated wrapper whose Go dynamic value is something OTHER than the wrapper itself.
/// </summary>
/// <remarks>
/// The two adapter kinds below answer different unwrap questions, but every consumer asks the same
/// one first — "is this object standing in for another value?" — and for ordinary Go values the
/// answer is no. This base lets that be settled with ONE type test instead of one per kind, which
/// matters because a FAILING interface type test costs ~3 ns (the runtime walks the type's interface
/// map) where a failing class test is free. A type switch and a type assert each pay it per
/// evaluation: probing both kinds separately was ~11.6 ns per iteration of the PerfIface benchmark.
/// </remarks>
public interface IGoAdapter;

/// <summary>
/// Implemented by generated interface-implementation adapters that wrap the receiver box
/// <c>ж&lt;T&gt;</c> of a POINTER-sourced Go interface value.
/// </summary>
/// <remarks>
/// A type assert back to the Go pointer type (<c>s.(*T)</c>) targets the box itself, so the
/// assert machinery unwraps the adapter through <see cref="Box"/>. Adapter equality also
/// delegates to box identity, matching Go pointer-interface equality.
/// </remarks>
public interface IжAdapter : IGoAdapter
{
    /// <summary>
    /// Gets the wrapped receiver box (a <c>ж&lt;T&gt;</c>).
    /// </summary>
    object? Box { get; }
}

/// <summary>
/// Implemented by generated interface-to-interface adapters that wrap another Go interface value.
/// </summary>
/// <remarks>
/// Go interface-to-interface assignment preserves the original dynamic value. The runtime unwraps
/// these adapters through <see cref="Value"/> for type assertions and interface equality.
/// </remarks>
public interface IInterfaceAdapter : IGoAdapter
{
    /// <summary>
    /// Gets the wrapped source interface value.
    /// </summary>
    object? Value { get; }
}

/// <summary>
/// Implemented by generated VALUE-sourced interface-implementation adapters that wrap a COPY of a
/// struct (or a named-func delegate) which this assembly cannot make <c>partial</c> — because the
/// struct is declared in ANOTHER assembly, or is a delegate.
/// </summary>
/// <remarks>
/// The interface value's Go DYNAMIC TYPE is the wrapped struct, never the adapter class, so Go
/// <c>==</c>, a type assert back to the struct type, a type switch and <c>%T</c> all unwrap through
/// <see cref="Value"/> — exactly as the other two kinds do through their own accessor.
/// (image's <c>func (p *NRGBA) At(x, y int) color.Color</c> hands back one of these: the
/// conversion sits in <c>image</c>, where both <c>color.NRGBA</c> and <c>color.Color</c> are
/// foreign.) Without the marker every one of those questions answered against the wrapper class and
/// silently diverged; the value-boxing partial-struct implementation used for a LOCAL struct has no
/// wrapper and never had the problem, which is why only the cross-assembly shape reproduces it.
/// </remarks>
public interface IValueAdapter : IGoAdapter
{
    /// <summary>
    /// Gets the wrapped value copy — the Go dynamic value this adapter stands in for.
    /// </summary>
    object? Value { get; }
}
