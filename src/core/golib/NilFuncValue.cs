// NilFuncValue.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// The canonical typed nil for a FUNC type crossing into INTERFACE space — the delegate-shaped
/// sibling of <c>ж&lt;T&gt;.NilBox</c> (one nil encoding system-wide). A Go func emits as a
/// managed delegate whose nil IS <c>null</c>: correct in every func-typed slot, and type-erasing
/// inside an interface, where Go packs (type=func-type, value=nil) — a NON-nil interface whose
/// <c>%T</c> prints the func type and whose type assertion succeeds with a nil result.
/// </summary>
/// <remarks>
/// Minted ONLY by <see cref="GoReflect.CanonicalNilFunc"/> (interned per delegate type) at the
/// eface boundary — reflect's <c>packInterfaceValue</c> and internal/reflectlite's
/// <c>valueInterface</c> — for a null read out of a FUNC-kinded slot. Every read-back path
/// resolves it away: <see cref="GoReflect.GoDynamicTypeOf"/> answers the delegate type, the type
/// assertion (<c>builtin.TryTypeAssert</c>) succeeds against exactly that type with the null
/// delegate as the value, <see cref="GoReflect.TryMarshalAssignable"/> stores it as null, and
/// <see cref="GoReflect.IsNilGoValue"/> answers nil — so the carrier can never be stored into a
/// func-typed slot or observed as itself.
/// </remarks>
public sealed class NilFuncValue
{
    /// <summary>The managed delegate type this nil func carries — Go's eface type word.</summary>
    public Type Type { get; }

    internal NilFuncValue(Type type)
    {
        Type = type;
    }
}
