// zsyscall_windows_ptrout_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of the generated syscall wrappers that take a Go `**T`
// OUT-PARAMETER -- a slot the caller owns and the KERNEL writes a raw address into.
//
// This is a SECOND class, distinct from the struct-passing one zsyscall_windows_impl.cs
// answers. There the defect is a LAYOUT: the record's fields sit at the wrong offsets.
// Here there is no record at all -- the argument is one machine word -- and the defect is
// that a golib pointer box has no such word to lend.
//
// WHAT GO WRITES, AND WHAT IT MEANS.
//
//     var storeCtx *CertContext
//     CertAddCertificateContextToStore(store, leafCtx, ADD_ALWAYS, &storeCtx)
//
// `&storeCtx` is the ADDRESS OF A POINTER VARIABLE: eight bytes of stack the kernel
// overwrites with a PCCERT_CONTEXT. The conversion renders it `ж<ж<CertContext>>`, a
// managed box whose storage is an OBJECT REFERENCE. There is no eight-byte slot to give.
//
// BOTH ANSWERS golib's `ж<T>` -> `uintptr` operator can give are wrong here, and the second
// is worse than the first. Measured on this corpus:
//
//   * while the held pointer is still null -- which is every out-parameter BEFORE the call --
//     the operator answers 0, because the value-peeking `IsNull` reports a heap-boxed null
//     reference as a nil pointer. Passing 0 tells Windows "no output wanted": `ppStoreContext`
//     is documented OPTIONAL, so the call SUCCEEDS, returns no error, and the caller reads back
//     the nil it started with. `crypto/x509`'s systemVerify then nil-dereferences three frames
//     away, which is where this was first seen;
//
//   * once the held pointer is NON-null the same operator answers a real MANAGED address --
//     measured as a live GC-heap address -- and the kernel would write a raw native pointer
//     over a slot the collector reads as an object reference. That is heap corruption, and it
//     is silent until the next collection.
//
// So the 0 is the accidentally SAFER of the two, and NEITHER is fixable in the operator: no
// single address is both writable by the kernel as eight raw bytes and readable by the managed
// side as a `ж<T>`. The two representations are incompatible, and reconciling them needs a
// SYNC POINT -- a moment when the raw word the kernel wrote is turned back into a pointer box.
// Only the wrapper knows that moment (it is "after the call returns"). That is why the remedy
// is HERE and not in `ж.cs`, and why the operator is deliberately left exactly as it is: its
// answer is correct for the case it was written for (`syscall.Write` hands `writeFile` a nil
// `*Overlapped`, and Go's own `uintptr(unsafe.Pointer(nil))` is 0).
//
// THE REMEDY, one shape for the whole class: a native cell local to the call, its address
// handed to the kernel, and `publishPointerOut` turning what came back into the caller's
// pointer. The cell is a stack local, live for exactly the call -- the mirror-is-a-local
// doctrine this package established for the synchronous members of the other class.
//
// WHICH MEMBERS ARE HERE, AND WHY THE REST ARE NOT. The corpus emits THIRTEEN wrappers of this
// shape (eleven in `syscall`, two in `internal/syscall/windows`). The transcription above is
// mechanical and would compile for all of them; it is applied to five, on the standing
// fix-it-when-a-suite-reaches-it rule plus one addition -- a member is taken when a VALUE-LEVEL
// guard can prove it, because "it no longer returns nil" is exactly the kind of evidence this
// class's history says not to trust:
//
//   ConvertSidToStringSid / ConvertStringSidToSid  -- `**uint16` and `**SID`. Reached by
//       syscall.SID.String() and syscall.StringToSid, and a ROUND TRIP, so a wrong address is
//       caught by the value. `SID` is Go's empty struct -- an opaque handle nothing reads
//       through in managed code -- so a native box is not merely safe here but exactly right.
//   NetGetJoinInformation                          -- `**uint16` over netapi32, freed with
//       NetApiBufferFree. A third DLL and a different lifetime, which is what makes the guard
//       evidence for a CLASS rather than for one advapi32 accident.
//   CertAddCertificateContextToStore / CertGetCertificateChain -- `**CertContext` /
//       `**CertChainContext`, the measured consumer (crypto/x509's Windows system verifier).
//
// All five are guarded at value level by the PointerOutParameter behavioral output test.
//
// DELIBERATELY NOT TAKEN, each for a stated reason rather than for lack of effort:
//
//   DnsQuery / _DnsQuery (`**DNSRecord`) -- the pointee is a LINKED native chain whose converted
//       record holds managed references, so publishing the address alone would replace a silent
//       nil with a fabricated-reference landmine. That is the OTHER class, and it wants the whole
//       chain transcribed the way zsyscall_windows_addrinfo_impl.cs transcribes ADDRINFOW. It
//       belongs to a `net` DNS arc.
//   getQueuedCompletionStatus / GetQueuedCompletionStatus (`**Overlapped`) -- an OVERLAPPED's
//       identity is owned by the netpoll arc's per-operation record (zsyscall_windows_wsa_impl.cs),
//       which keys on the ж<Overlapped> the waiter names. Handing back a bare native box would
//       mint an identity that arc does not know about. It belongs to netpoll.
//   GetFullPathName (`**uint16`), NetUserGetInfo (`**byte`), and internal/syscall/windows'
//       CreateEnvironmentBlock (`**uint16`) / NetUserGetLocalGroups (`**byte`) -- the same safe
//       shape as the five above, with no consumer in the corpus today and therefore no value-level
//       proof available. Go's own syscall.FullPath passes nil for GetFullPathName's fname, so even
//       its one caller does not exercise it.
//
// A wrapper's absence from this file is NOT evidence it is sound -- see the same warning on the
// struct-passing census in docs/phase4/BOARD-next-validation-candidates.md.

using System;

// Hand-owned (no zsyscall_windows_ptrout_impl.go exists, so a reconvert never regenerates this
// file). The declarations it replaces are registered in the converter's manualConversionFuncs,
// which is what turns their generated bodies into placeholders.
[module: go.GoManualConversion]

// The native out-cells and their addresses are pointer work. Declared rather than inherited --
// see zsyscall_windows_impl.cs.
[module: go.GoRequiresUnsafe]

namespace go;

partial class syscall_package
{
    // Publishes the raw address the kernel wrote into a native out-cell back into the caller's Go
    // pointer variable.
    //
    // ValueSlot, NOT Value: the caller's box legitimately holds null on entry -- that is what an
    // out-parameter IS -- and `Value`'s nil-pointer guard VALUE-PEEKS, so it would panic on the
    // very write that fills the slot in. ValueSlot is golib's documented form for exactly this
    // shape (ж.cs), and GetAcceptExSockaddrs writes its two `**RawSockaddrAny` results the same way.
    //
    // A ZERO address publishes the nil pointer rather than a box over address 0, with no special
    // case needed: ж<T>'s native constructor already treats a zero address as nil, matching Go's
    // `(*T)(unsafe.Pointer(uintptr(0))) == nil`. A nil `slot` is legal and means "no output
    // wanted" -- crypto/x509's createStoreContext passes literal nil when adding intermediates.
    private static void publishPointerOut<T>(ж<ж<T>> slot, nuint written)
    {
        if (slot != nil)
        {
            slot.ValueSlot = (ж<T>)(uintptr)written;
        }
    }

    // ---- advapi32: the SID pair -------------------------------------------------------------

    // ConvertSidToStringSidW(sid, &stringSid). The result is a LocalAlloc'd UTF-16 string the
    // caller frees with LocalFree, and `uint16` is blittable, so the published native box is read
    // through directly by utf16PtrToString -- no transcription is owed beyond the address itself.
    public static unsafe error /*err*/ ConvertSidToStringSid(ж<SID> Ꮡsid, ж<ж<uint16>> ᏑstringSid) {
        nuint cell = 0;
        uintptr cellAddr = ᏑstringSid == nil ? 0 : (uintptr)(void*)(&cell);

        var (r1, _, e1) = Syscall(procConvertSidToStringSidW.Addr(), 2, (uintptr)Ꮡsid, cellAddr, 0);

        if (r1 == 0) {
            // Left untouched on failure, as Go leaves it: the kernel wrote nothing.
            return errnoErr(e1);
        }

        publishPointerOut(ᏑstringSid, cell);

        return default!;
    }

    // ConvertStringSidToSidW(stringSid, &sid). `SID` is Go's `struct{}` -- an OPAQUE HANDLE, never
    // read through in managed code (SID.Len asks the OS via GetLengthSid; SID.Copy hands the
    // address straight back to CopySid) -- so a native-address box is the whole and correct answer
    // for this member, with no layout question behind it.
    public static unsafe error /*err*/ ConvertStringSidToSid(ж<uint16> ᏑstringSid, ж<ж<SID>> Ꮡsid) {
        nuint cell = 0;
        uintptr cellAddr = Ꮡsid == nil ? 0 : (uintptr)(void*)(&cell);

        var (r1, _, e1) = Syscall(procConvertStringSidToSidW.Addr(), 2, (uintptr)ᏑstringSid, cellAddr, 0);

        if (r1 == 0) {
            return errnoErr(e1);
        }

        publishPointerOut(Ꮡsid, cell);

        return default!;
    }

    // ---- netapi32 ----------------------------------------------------------------------------

    // NetGetJoinInformation(server, &name, &bufType). Reports its failure through the RETURN VALUE
    // rather than GetLastError, so a non-zero r0 becomes that Errno directly and no last-error is
    // consulted -- the generated body's own reading, kept verbatim. The name buffer is freed with
    // NetApiBufferFree, not LocalFree.
    public static unsafe error /*neterr*/ NetGetJoinInformation(ж<uint16> Ꮡserver, ж<ж<uint16>> Ꮡname, ж<uint32> ᏑbufType) {
        nuint cell = 0;
        uintptr cellAddr = Ꮡname == nil ? 0 : (uintptr)(void*)(&cell);

        var (r0, _, _) = Syscall(procNetGetJoinInformation.Addr(), 3, (uintptr)Ꮡserver, cellAddr, (uintptr)ᏑbufType);

        if (r0 != 0) {
            return ((Errno)r0);
        }

        publishPointerOut(Ꮡname, cell);

        return default!;
    }

    // ---- crypt32: the two members crypto/x509's system verifier reaches ----------------------
    //
    // CertAddCertificateContextToStore and CertGetCertificateChain are BOTH members of this class
    // and NEITHER is written here: they live in zsyscall_windows_certchain_impl.cs, because for both
    // of them the out-cell above is only half the answer.
    //
    // The out-cell publishes the REAL PCCERT_CONTEXT / PCCERT_CHAIN_CONTEXT the kernel produced,
    // which is what got crypto/x509 past `storeCtx == nil` when this file was written. Reading
    // THROUGH those pointers is the other class: the converted CertContext holds EncodedCert and
    // CertInfo as `ж<T>` MANAGED REFERENCES, and CertChainContext / CertSimpleChain /
    // CertChainElement likewise, so `storeCtx.Store` and the whole chain walk in root_windows.cs read
    // a native record at managed offsets. That was recorded here as "the CryptoAPI chain-walk arc; it
    // is not this one" -- and the measurement that arc opened with is that the two are NOT separable
    // at this call site, since `storeCtx.Store` is an ARGUMENT of the very next call. So both
    // wrappers publish a managed VIEW that remembers its native identity, and the certchain file
    // states both remedies in one place. `publishPointerOut` above is unchanged and still serves the
    // three members that genuinely want an opaque native box.
}
