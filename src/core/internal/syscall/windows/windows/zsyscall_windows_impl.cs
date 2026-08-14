// zsyscall_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// A DECLARED CAPABILITY LIMIT, not a repair — the one member of the syscall struct-passing class
// whose established remedy is measured unreachable. Coordinator ruling 2026-08-14; the full
// mechanism and the three costed remedies are in
// docs/phase4/BOARD-next-validation-candidates.md, "RETRACTED — `os`'s REGRESSION is a HOST
// CAPABILITY, and the killer is SHARE_INFO_2".
//
// WHAT BREAKS. `NetShareAdd` is handed the address of a `SHARE_INFO_2`, which holds four `ж<uint16>`
// pointer fields and four `uint32`s. The CLR auto-layouts a struct containing references, so it
// groups the references FIRST: dumped by reflection from the built assembly the record is 48 bytes
// against the native 56, and every field lands somewhere it does not belong —
//
//   | native SHARE_INFO_2 (x64) | off | C# storage actually there | value handed to netapi32   |
//   |---------------------------|-----|---------------------------|----------------------------|
//   | LPWSTR shi2_netname       |   0 | Netname (object ref)      | a managed ref, read as runes|
//   | DWORD  shi2_type          |   8 | low half of Remark (nil)  | 0                          |
//   | LPWSTR shi2_remark        |  16 | Path (object ref)         | a managed reference        |
//   | DWORD  shi2_permissions   |  24 | low half of Passwd (nil)  | 0                          |
//   | DWORD  shi2_max_uses      |  28 | high half of Passwd (nil) | 0                          |
//   | DWORD  shi2_current_uses  |  32 | Type                      | 0x40000000                 |
//   | LPWSTR shi2_path          |  40 | MaxUses (=1), CurrentUses | 0x0000000000000001         |
//   | LPWSTR shi2_passwd        |  48 | past the end of the record| whatever follows on the heap|
//
// netapi32 dereferences shi2_path — the pointer value 1 — and the PROCESS dies with 0xC0000005.
// Proven without go2cs by a standalone probe: a blittable [StructLayout(Sequential)] record with
// real LPWSTRs returns rc=0 and genuinely creates the share; object references at the NATIVE
// offsets SURVIVE with rc=123 (ERROR_INVALID_NAME), because a managed reference is at least a
// readable address; only the measured go2cs layout faults. So the fault is the field REORDERING,
// not the presence of managed references.
//
// WHY THE ESTABLISHED REMEDY DOES NOT REACH IT. Every other member of this class (syscall's
// GetTimeZoneInformation, findFirstFile1/findNextFile1, Process32First/Next, the sockaddr family)
// is repaired by hand-owning the wrapper against a blittable mirror and copying field-for-field at
// the boundary. That needs the wrapper to SEE the struct, and here it never does: the only caller
// in all of GOROOT is os's TestNetworkSymbolicLink, which writes `(*byte)(unsafe.Pointer(&p))`.
// That converts to `Ꮡp.Reinterpret<windows.SHARE_INFO_2, byte>()`, and `Reinterpret` correctly
// REFUSES to alias a reference-bearing struct as `byte`, falling back to `(ж<byte>)(uintptr)box` —
// a native-address box with the managed identity already gone. There is nothing left to copy from.
// Reading the struct back out of that raw address would fabricate managed references from it, which
// ж.PointerExtensions.cs names as a CLR type-safety break and "strictly worse than the
// wrong-but-contained read the address route produces". The durable answer belongs to the ж-box
// arc: once the non-aliasing Reinterpret fallback RETAINS its source object, a hand-owned wrapper
// can reach the struct and the ordinary mirror-and-copy applies. Until then, this.
//
// WHAT THIS BUYS. Before it, a host where the Server service is reachable — elevated session,
// LanmanServer running, i.e. a host where Go's own TestNetworkSymbolicLink PASSES — dies at
// test ~32 of os's 174 and measures NOTHING; `os` reads 31 of 679 where the board records 681 of
// 683. Failing by name converts that whole-suite process death into ONE loud row. It is a real
// mismatch rather than a skip, and that is the honest report: Go passes this test and go2cs cannot.
//
// THE BOUNDARY OF THE LIMIT, so it is readable in place. `internal/syscall/windows` holds six more
// wrappers of the same shape. None is hand-owned here — the board's standing rule is to fix a
// censused wrapper when a suite reaches it, not speculatively — and each is repairable by the
// ORDINARY mirror remedy, because each receives the struct as a typed pointer rather than through a
// byte reinterpret:
//
//   | Wrapper                          | Non-blittable struct                                   | Reached by                                    |
//   |----------------------------------|--------------------------------------------------------|-----------------------------------------------|
//   | NetShareAdd (THIS FILE)          | SHARE_INFO_2 (Netname, Remark, Path, Passwd)             | os's TestNetworkSymbolicLink — the only caller |
//   | GetAdaptersAddresses             | IpAdapterAddresses (nine ж<T>, array<byte>, array<uint32>)| net.Interfaces                                |
//   | Module32First / Module32Next     | ModuleEntry32 (array<uint16> Module, ExePath)            | syscall's own suite                            |
//   | GetFileInformationByHandleEx     | FILE_ID_BOTH_DIR_INFO / FILE_FULL_DIR_INFO               | os's readdir — ALREADY ANSWERED, and the worked |
//   |                                  | (array<uint16> names)                                    | precedent: os/windows/dir_windows_impl.cs reads |
//   |                                  |                                                          | the kernel buffer at NATIVE offsets            |
//   | WSASendMsg / WSARecvMsg          | WSAMsg (ж<syscall.WSABuf>)                               | net's UDP OOB path                             |
//   | NetUserGetLocalGroups            | ж<ж<byte>> out-buffer                                    | os/user                                        |
//
// Whichever remedy eventually lands for this one, verify at VALUE level as the class demands: the
// probe above is the oracle — the share must actually be created and NetShareDel must remove it.

using System;

// Hand-owned (no zsyscall_windows_impl.go exists, so a reconvert never regenerates this file);
// the converter drops NetShareAdd from zsyscall_windows.cs via manualConversionFuncs and leaves a
// placeholder comment where its body was.
[module: go.GoManualConversion]

namespace go.@internal.syscall;

partial class windows_package
{
    /// <summary>
    /// Declares the capability limit above rather than corrupting the caller's process.
    /// </summary>
    /// <remarks>
    /// Go's signature is preserved exactly (<c>neterr error</c>), but the failure is a THROW, not a
    /// returned <c>error</c>: a returned error would let a caller treat it as an ordinary
    /// <c>NERR_*</c> and continue, and Go's own test treats exactly two of those as a skip. The
    /// limit is not a runtime condition the caller can retry around — it is a property of the
    /// conversion — so it announces itself the way the other declared-unimplemented boundaries in
    /// the corpus do.
    /// </remarks>
    public static error /*neterr*/ NetShareAdd(ж<uint16> ᏑserverName, uint32 level, ж<byte> Ꮡbuf, ж<uint16> ᏑparmErr)
    {
        throw new NotSupportedException(
            "internal/syscall/windows: NetShareAdd is not supported by the converted runtime — " +
            "SHARE_INFO_2 carries managed references, so the CLR auto-layouts it 48 bytes with the " +
            "references grouped first, and netapi32 would dereference the integer 1 as shi2_path " +
            "(access violation). The buffer reaches this wrapper as a raw address with its managed " +
            "identity already discarded, so the blittable-mirror remedy used elsewhere in this class " +
            "cannot be applied here. See docs/phase4/BOARD-next-validation-candidates.md, " +
            "\"RETRACTED — os's REGRESSION is a HOST CAPABILITY, and the killer is SHARE_INFO_2\".");
    }
}
