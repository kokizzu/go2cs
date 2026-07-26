// GoValueCloneAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Names the fields of a converted Go struct that a by-value copy must DEEP-copy: the fields whose
/// type is a fixed-size array, or another struct that itself carries one. go2cs-gen turns this into
/// the struct's <see cref="IGoValueClone"/> implementation — a <c>Clone()</c> that copies the value
/// and then re-clones exactly these fields.
/// </summary>
/// <remarks>
/// The converter is the single source of truth here: only it has the Go type information that
/// decides which fields need the deep copy, and it emits the matching <c>.Clone()</c> at every Go
/// by-value copy site. EMBEDDED (promoted) struct members are deliberately NOT listed — they are
/// held as <c>ж&lt;T&gt;</c> boxes whose accessor writes THROUGH the box, so assigning one in a
/// clone would corrupt the source; embedded-struct copy aliasing is a separate, pre-existing gap.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class GoValueCloneAttribute(params string[] fields) : Attribute
{
    /// <summary>Names of the fields a by-value copy must re-clone.</summary>
    public string[] Fields { get; } = fields;
}
