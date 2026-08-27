// PointerKindTypeIdentityTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

/// <summary>
/// Pins the R10 interning law across the B1 per-kind split: one Go pointer type is FOUR managed
/// classes, and the reflection bridge must classify every one of them as the ONE canonical
/// <c>ж&lt;T&gt;</c> identity — <c>reflect.DeepEqual(&amp;x.f, &amp;table[i])</c> dies at the
/// <c>Type()</c> gate otherwise (debug/plan9obj, debug/macho, internal/xcoff and crypto/x509 all
/// turned red on exactly this before <see cref="GoReflect.CanonicalBoxType"/> existed).
/// </summary>
[TestClass]
public class PointerKindTypeIdentityTests
{
    private struct Holder
    {
        public long Field;
    }

    private static readonly FieldRefFunc<long> s_fieldAccessor = static p => ref ((ж<Holder>)p).Value.Field;

    [TestMethod]
    public void AllFourKindsShareOneDynamicType()
    {
        ж<Holder> standard = new StandardBox<Holder>(new Holder { Field = 1 });
        ж<long> fieldRef = standard.of(s_fieldAccessor);
        array<long> backing = new(2);
        ж<long> elemRef = new ElemRefBox<long>(backing, 0);
        ж<long> native = new NativeBox<long>(0x1000);
        ж<long> plain = new StandardBox<long>(7L);

        System.Type canonical = typeof(ж<long>);

        Assert.AreEqual(canonical, GoReflect.GoDynamicTypeOf(fieldRef), "field-ref kind must intern as ж<T>");
        Assert.AreEqual(canonical, GoReflect.GoDynamicTypeOf(elemRef), "element-ref kind must intern as ж<T>");
        Assert.AreEqual(canonical, GoReflect.GoDynamicTypeOf(native), "native kind must intern as ж<T>");
        Assert.AreEqual(canonical, GoReflect.GoDynamicTypeOf(plain), "standard kind must intern as ж<T>");
    }

    [TestMethod]
    public void UnsafePointerKeepsItsOwnIdentity()
    {
        // unsafe.Pointer is Go's one NAMED pointer type — its class IS its identity, and the
        // canonicalization must leave it alone (the M exemption, stated as a type here so the
        // test needs no reference to the unsafe assembly).
        Assert.AreEqual(typeof(MarkedPointer), GoReflect.CanonicalBoxType(typeof(MarkedPointer)),
            "an IUnsafePointer-marked box class keeps its own type identity");
        Assert.AreEqual(typeof(ж<uintptr>), GoReflect.CanonicalBoxType(typeof(StandardBox<uintptr>)),
            "an unmarked box of the same pointee still canonicalizes");
    }

    private sealed class MarkedPointer(uintptr value) : StandardBox<uintptr>(value), IUnsafePointer;

    [TestMethod]
    public void FieldAccessorContractServesEveryKind()
    {
        // FieldRef<T>.Create's IL resolves storage through the kind's own ValueSlot — the
        // pre-split IL cast to the standard kind and poked its fields directly, which turned every
        // other kind arriving at a bridge-built accessor into an InvalidCastException.
        FieldRefFunc<long> accessor = FieldRef<Holder>.Create<long>(nameof(Holder.Field));

        ж<Holder> viaStandard = new StandardBox<Holder>(new Holder { Field = 41 });
        Assert.AreEqual(41L, accessor(viaStandard), "standard kind through the built accessor");

        array<Holder> holders = new(1);
        ж<Holder> viaElem = new ElemRefBox<Holder>(holders, 0);
        viaElem.Value = new Holder { Field = 42 };
        Assert.AreEqual(42L, accessor(viaElem), "element-ref kind through the built accessor");
    }
}
