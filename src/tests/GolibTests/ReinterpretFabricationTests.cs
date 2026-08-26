//******************************************************************************************************
//  ReinterpretFabricationTests.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements.
//  See the NOTICE file distributed with this work for additional information regarding copyright
//  ownership.  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License.  You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on
//  an "AS-IS" basis.  WITHOUT WARRANTY OF ANY KIND, either express or implied.  See the License for
//  the specific language governing permissions and limitations.
//
//******************************************************************************************************

using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards the fabrication refusal in <c>PointerExtensions.Reinterpret</c>: when the alias gate has
/// already said no and the DESTINATION type is (or contains) a managed reference, neither fallback
/// may run — both read <c>TDst</c> out of raw storage bytes, and a reference materialized from
/// bytes is a CLR type-safety break (junk dispatch on a quiet heap, an AccessViolationException
/// when the punned bits land unmapped).
/// </summary>
/// <remarks>
/// The live witness is converted <c>time.syncTimer</c> (sleep.cs), which reinterprets
/// <c>channel&lt;Time&gt;</c> → <c>unsafe.Pointer</c> — a CLASS — on every <c>NewTimer</c>. Under
/// the full time suite at <c>GODEBUG=asynctimerchan=2</c> the fabricated reference AV'd inside
/// <c>Pointer.op_Implicit</c> (measured 2026-08-24); three structurally-faithful quiet-heap
/// repros passed, which is the byte-view census's latent-with-live-trigger shape exactly. The Go
/// value of a pointer the managed model cannot represent is NIL, and the live consumer
/// (<c>newTimer</c>, which reads only the nil-bit and recomputes it from the GODEBUG setting)
/// is correct under it.
/// </remarks>
[TestClass]
public class ReinterpretFabricationTests
{
    // The channel shape: a value-type surrogate whose only field is a managed reference.
    private struct SingleRefStruct
    {
        internal object? core;
    }

    // A destination VALUE type that still contains a reference — the other half of
    // IsReferenceOrContainsReferences, and the same fabrication if punned from non-reference bits.
    private struct RefCarryingStruct
    {
        internal string? name;
    }

    // There is deliberately NO golib-level refusal for a reference-typed destination, and the
    // history is worth one paragraph: a refusal (return a default-holding box) was added when the
    // asynctimerchan AV was rooted to syncTimer's channel->unsafe.Pointer pun -- and the full
    // behavioral suite then failed PointerCastSliceRange, because the ж<T> -> ж<U> DOUBLE-POINTER
    // pun (reflect MapOf's `**(**mapType)(unsafe.Pointer(&imap))`) reads one ж instantiation's
    // reference slot as another, which works precisely because the generic layouts coincide -- a
    // LOAD-BEARING pun the refusal broke. The AV is instead killed at its source: the CONVERTER
    // emits the carrying form for unsafe.Pointer targets (see reinterpretManagedEmission), so no
    // emission reaches this fallback with the one destination that measured fatal. The fallback
    // keeps its pre-existing behavior for everything else -- never something newly wrong, in
    // either direction.

    [TestMethod]
    public void ValueToValueReinterpretStillAliases()
    {
        // The control: the guard must not reach the representable arm. A float64's bits read as
        // uint64 through the alias, exactly as before.
        ж<double> box = new StandardBox<double>(1.0);

        ж<ulong> derived = box.Reinterpret<double, ulong>();

        Assert.IsFalse(derived.IsNilPointer, "the representable value-to-value alias must be untouched");
        Assert.AreEqual(0x3FF0000000000000UL, derived.Value, "IEEE-754 bits of 1.0 through the alias");

        // ...and it is an ALIAS, not a copy: a write through the source is visible through it.
        box.Value = 2.0;
        Assert.AreEqual(0x4000000000000000UL, derived.Value, "alias tracks the source storage");
    }
}
