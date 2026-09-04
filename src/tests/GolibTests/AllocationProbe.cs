// AllocationProbe.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Runtime.CompilerServices;

namespace GolibTests;

/// <summary>
/// Forces a probe body's allocation to genuinely reach the heap, so an allocation measurement stays
/// a measurement at the configuration validation actually runs under.
/// </summary>
/// <remarks>
/// <para>
/// .NET's escape analysis stack-allocates an object it can prove does not escape its method. Under
/// the validation configuration of record — Release with <c>DOTNET_TieredCompilation=0</c>, i.e.
/// full optimization from the FIRST call, with no tier-0 pass to hide behind — that turns the
/// canonical probe body <c>() =&gt; { _ = new object[4]; }</c> into no allocation at all. Measured in
/// an isolated one-file probe, one build, both configurations: <b>0 B/run at TC0, 56 B/run under
/// default tiering</b>.
/// </para>
/// <para>
/// That zero is not harmless, and it is worst exactly where it looks best. The discarded-object body
/// is the shape an allocation SELF-CONTROL uses — the deliberately-allocating arm that proves the
/// instrument can still see an allocation before any want-zero assertion beside it is believed. A
/// self-control that stack-allocates reports "the probe is blind", which is honest, and every
/// want-zero guard resting on that probe would otherwise have passed VACUOUSLY. Skipping the control
/// or pinning tiering back on for it would leave the whole allocation-guard family unmeasured at the
/// one configuration validation uses, so the escape is added to the BODY instead.
/// </para>
/// <para>
/// Handing the value to a method the JIT will not inline is what defeats the analysis: an object
/// passed to a call the compiler cannot see into escapes by construction, and
/// <see cref="MethodImplOptions.NoInlining"/> is what keeps it from seeing in. Measured in the same
/// probe: both this form and a static-field sink read 56 B/run at TC0, against the discarded body's
/// 0 B.
/// </para>
/// <para>
/// The parameter is GENERIC rather than <see cref="object"/> on purpose. A value-typed shape
/// (<c>@string</c>, <c>slice&lt;T&gt;</c>, <c>ж&lt;T&gt;</c>'s value siblings) instantiates exactly
/// and is not boxed, so the sink adds a call and no bytes — an <see cref="object"/> parameter would
/// box, and a census that compares a charged object COUNT against measured BYTES would then be
/// handed bytes it did not earn, loosening the invariant in the one direction that hides an
/// over-charge.
/// </para>
/// </remarks>
internal static class AllocationProbe
{
    /// <summary>
    /// Consumes <paramref name="value"/> through a call the JIT cannot see into, so whatever it
    /// references must be heap-allocated.
    /// </summary>
    /// <remarks>
    /// The body is deliberately empty: what does the work is the call itself, not anything inside
    /// it. Removing <see cref="MethodImplOptions.NoInlining"/> is the positive control — every
    /// caller below reads RED again at <c>DOTNET_TieredCompilation=0</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Escape<T>(T value)
    {
    }
}
