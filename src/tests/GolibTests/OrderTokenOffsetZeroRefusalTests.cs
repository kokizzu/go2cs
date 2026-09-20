using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// ARM 2a, the OTHER door into the same fault -- the one the arithmetic refusal
// (TokenArithmeticRefusalTests) leaves open BY CONSTRUCTION.
//
// Go 1.24's reflect/all_test.go:1409 is
//
//     func setField[S, V any](in S, offset uintptr, value V) (out S) {
//         *(*V)(unsafe.Add(unsafe.Pointer(&in), offset)) = value
//         return in
//     }
//
// and all_test.go:1510 calls it at OFFSET 0 over `struct{_, a, _ func()}` -- a reference-bearing
// pointee. So the forward conversion registers the box's ORDER TOKEN (a number that is not an
// address, ж.cs:891-897), and the inbound conversion is then handed that EXACT token with a
// DIFFERENT pointee type V. That is the Q44 §10.3 taxonomy's arm 2a
// (Q44RegistryCensus.cs:14): the resolve HITS a live box, so arm 1 does not fire; the number IS the
// allocation base, so `IsTokenArithmetic` is false by construction and arm 3 cannot fire either.
// The conversion fell through to `new NativeBox<V>(token)` and the write that follows went to an
// unmapped page: an UNCATCHABLE AccessViolationException that takes the host down. R measured it as
// 195 tests started and then the abort, at TestIsZero.
//
// The criterion is TokenArithmeticRefusalTests' criterion, one arm over: the offset-0 token case
// must fail CATCHABLY and by name, and the three controls must be UNTOUCHED -- a refusal drawn one
// step too wide starts refusing REAL addresses, which is the class the pinned-provenance route
// exists to serve. The model question (a Go-layout offset into CLR-laid-out storage) stays open;
// this makes it loud instead of fatal.
//
// ⚠ THE REFUSAL SITS AT THE DEREFERENCE, NOT AT THE CONVERSION, and that is measured. A refusal at
// the uintptr operator took SEVEN GolibTests red against an empty base -- PointerTokenConversionTests'
// "loud form" row, four ReinterpretSourceRetentionTests, and one each in RuntimeHashFamilyTests and
// SliceHeaderReinterpretTests -- because the native box OVER THE TOKEN is a deliberate CARRIER: its
// address IS the token, which is how the unpinnable reinterpret class and the boundary wrappers
// recover the source. The tree had already ruled it, at RuntimeHashFamilyTests.cs:182 -- "a
// dereference is the row-level fault the design chose, never a number". This class guards THAT
// fault, made catchable; the conversion arm of it is a control, not the subject.
[TestClass]
public class OrderTokenOffsetZeroRefusalTests
{
    // The reflect fixture's own shape: a struct whose fields are FUNC values. A delegate is a
    // managed reference, so the box gets no pinnable slot (ж.StandardBox.cs:52-66) and its
    // StorageKind is None.
    private struct WithDelegates
    {
        internal Action first;
        internal Action second;
    }

    private struct WithoutReference
    {
        internal nuint first;
        internal nuint second;
    }

    [TestMethod]
    public void AnOrderTokenAtOffsetZeroIsRefusedByName_NotAnsweredWithANativeBoxOverANonAddress()
    {
        ж<WithDelegates> box = new StandardBox<WithDelegates>(new WithDelegates { first = static () => { } });

        // The premise, asserted rather than assumed: reference-bearing pointee, no pinnable slot,
        // so `uintptr n = box` hands out the ORDER TOKEN and not an address.
        Assert.AreEqual(PointerStorage.None, box.StorageKind, "a delegate-bearing pointee has no address to take");

        nuint token = (nuint)(uintptr)box;

        Assert.AreEqual(box.PointerOrderToken, token, "so the forward conversion registered its order token");

        // `*(*V)(unsafe.Add(unsafe.Pointer(&in), 0))` with V != S. THE CONVERSION IS ADMITTED and
        // must be: the native box over the token is the CARRIER the unpinnable reinterpret class and
        // the boundary wrappers recover the source through, and refusing it here took seven
        // GolibTests red (2026-09-20, withdrawn). Its number and its kind are unchanged.
        ж<long> answered = (ж<long>)(uintptr)token;

        Assert.IsTrue(answered.IsNative, "the conversion still answers a native box -- the carrier is load-bearing");
        Assert.AreEqual(token, answered.NativeAddress, "whose address IS the token, so a boundary wrapper still recovers the source");

        // THE FAULT, where the charter puts it. A REAL write is what reflect's setField performs and
        // what used to end the host, so it is exercised here -- but ONLY because the refusal now
        // precedes it. If this ever fails by SURVIVING, the write reached a non-address and the next
        // run is the one that does not report at all.
        PanicException onWrite = Assert.ThrowsException<PanicException>(
            () => answered.Value = 7L,
            "a WRITE through an order token must be refused by name, never delivered to an unmapped page");

        StringAssert.Contains(onWrite.Message, "no address",
            "and the refusal must SAY what it refused -- a message nobody can read is a crash with extra steps");

        // The READ half is lethal for its own reason -- it materializes a T out of the token's
        // bytes -- so it is refused too, and the message names the arm.
        PanicException onRead = Assert.ThrowsException<PanicException>(
            () => _ = answered.Value,
            "and a READ through it must be refused for the same reason");

        StringAssert.Contains(onRead.Message, "arm 2a", "the refusal names the arm it belongs to");

        GC.KeepAlive(box);
    }

    [TestMethod]
    public void TheSLOTAccessorTakesTheSameRefusal_AndTheClassificationIsReadableWithoutDereferencing()
    {
        // THE OTHER DOOR ON THE SAME FAULT, and it is not a restatement of the first. `Value` and
        // `ValueSlot` are SEPARATE overrides carrying SEPARATE guards (ж.NativeBox.cs:105 and :117),
        // and ValueSlot is the one WITHOUT the nil check -- which makes it the door the corpus
        // actually arrives through: `fixed (void* ptr = &this.ValueSlot)` (ж.cs:579), the four
        // assignment and read paths (ж.cs:633, :640, :656, :670, :678) and, above all,
        // `Unsafe.As<T, TDst>(ref ((ж<T>)source).ValueSlot)` (ж.cs:730), which is
        // PointerExtensions.Reinterpret -- the very route this arm's own carrier argument is about.
        //
        // ⚠ THE CLASS ABOVE DRIVES `.Value` THREE TIMES AND `ValueSlot` NOT ONCE (measured
        // 2026-09-20: 3 occurrences against 0). So the guard on the accessor the reinterpret path
        // reaches for was held by review alone, and a deletion of it would have left this file
        // GREEN while restoring exactly the uncatchable write the seat exists to stop. Remove
        // either guard's throw and precisely one of these two methods reds; that separation is the
        // whole of this method's claim.
        ж<WithDelegates> box = new StandardBox<WithDelegates>(new WithDelegates { first = static () => { } });
        nuint token = (nuint)(uintptr)box;

        ж<long> answered = (ж<long>)(uintptr)token;

        // CLASSIFIED WITHOUT BEING TOUCHED -- read BEFORE either dereference below, because that
        // ordering IS the property being asserted. The verdict was computed once by the operator
        // that had the resolved box in hand and rides IN the box (ж.cs:908, `aliasesAnOrderToken:`),
        // so a caller can ASK whether a number is an order token instead of finding out by trying --
        // and trying is the fault. This is also that public member's first consumer: a flag nothing
        // reads is a flag nothing would notice the loss of.
        NativeBox<long> native = (NativeBox<long>)answered;

        Assert.IsTrue(native.AliasesAnOrderToken,
            "the operator resolved the number to a LIVE box's order token and recorded that verdict in the box it answered with");

        // The WRITE half, through the slot: reflect's `setField` assigns, and an assignment to a
        // reference-bearing pointee's field is exactly what routes through a ref-returning slot.
        PanicException onSlotWrite = Assert.ThrowsException<PanicException>(
            () => answered.ValueSlot = 7L,
            "a WRITE through the SLOT must take the refusal too -- it is the accessor with no nil check, not a laxer accessor");

        StringAssert.Contains(onSlotWrite.Message, "no address",
            "and it must be the SAME refusal, saying what it refused");

        StringAssert.Contains(onSlotWrite.Message, "arm 2a",
            "and naming the same arm -- one fault with two doors, not two faults");

        // The READ half, which is lethal for the read's own reason: it materializes a T out of the
        // token's bytes.
        PanicException onSlotRead = Assert.ThrowsException<PanicException>(
            () => _ = answered.ValueSlot,
            "and a READ of the slot is refused for the read half's own reason");

        StringAssert.Contains(onSlotRead.Message, "arm 2a", "the same arm once more");

        GC.KeepAlive(box);
    }

    [TestMethod]
    public void ARITHMETICOnTheSameTokenStillTakesTheArithmeticRefusal_TheNewArmDoesNotDisplaceArm3()
    {
        // Control 1: offset != 0 on the very same fixture. The resolve MISSES (the number is not the
        // token) and `IsTokenArithmetic` fires, so this must keep arm 3's refusal, by arm 3's route.
        ж<WithDelegates> box = new StandardBox<WithDelegates>(new WithDelegates { first = static () => { } });
        nuint token = (nuint)(uintptr)box;

        PanicException caught = Assert.ThrowsException<PanicException>(
            () => _ = (ж<long>)(uintptr)(token + 8),
            "arithmetic on a token is still refused by name");

        StringAssert.Contains(caught.Message, "unsafe pointer arithmetic",
            "and it is still ARM 3's message -- the new arm must not swallow the arithmetic case");

        GC.KeepAlive(box);
    }

    [TestMethod]
    public void TheSamePointeeTypeStillAliasesItsBox_TheRefusalDoesNotEatArm1()
    {
        // Control 2: V == S at offset 0 is arm 1, which RETURNS before any of this. The round trip is
        // load-bearing for %p and for identity; refusing it would be the refusal drawn too wide in
        // the one direction the corpus would notice immediately.
        ж<WithDelegates> box = new StandardBox<WithDelegates>(new WithDelegates { first = static () => { } });
        nuint token = (nuint)(uintptr)box;

        Assert.AreSame(box, (ж<WithDelegates>)(uintptr)token, "an exact token is still its own box");

        GC.KeepAlive(box);
    }

    [TestMethod]
    public void AReferenceFreePointeeAtOffsetZeroKeepsItsNativeBoxOverARealAddress_TheGuardAgainstARefusalDrawnTooWide()
    {
        // Control 3, and the one that matters most: the SAME offset-0, different-pointee shape over
        // PINNED storage. Here the forward conversion pinned and registered a REAL ADDRESS, so the
        // number resolves through the provenance route rather than the order-token route and the
        // native box over it is correct. A refusal keyed on "resolved to another pointee type" alone
        // would take this with it, and nothing else in the tree would notice: the row would still
        // report and the package would still pass.
        ж<WithoutReference> box = new StandardBox<WithoutReference>(new WithoutReference { first = 0xA1, second = 0xB2 });

        Assert.AreEqual(PointerStorage.Pinnable, box.StorageKind, "a reference-free pointee gets a real, pinnable address");

        nuint address = (nuint)(uintptr)box;

        Assert.AreNotEqual(box.PointerOrderToken, address, "the number handed out is the pinned address, not the order token");

        ж<nuint> derived = (ж<nuint>)(uintptr)address;

        Assert.IsTrue(derived.IsNative, "a real address still answers with a native box");

        // AND THE NEGATIVE OF THE NEW ARM, asserted on the FLAG and not only on the read below.
        // The flag is what would refuse this box, so a refusal drawn too wide shows up HERE first
        // -- and it shows up as a classification, before any dereference, which is the one form
        // this control can take that does not depend on the read succeeding.
        Assert.IsFalse(((NativeBox<nuint>)derived).AliasesAnOrderToken,
            "a real pinned address is NOT an order token, and the box that carries it must not say it is");

        // Read THROUGH it, because the whole claim is that this address is real: the first field of
        // the pinned storage is what a `*nuint` at offset 0 names.
        Assert.AreEqual((nuint)0xA1, derived.Value, "and the native box reads the storage it aliases");

        GC.KeepAlive(box);
    }
}
