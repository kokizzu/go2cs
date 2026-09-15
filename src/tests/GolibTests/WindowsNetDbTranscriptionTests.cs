using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using syscall = go.syscall_package;

namespace GolibTests;

// The value-level guard for syscall's NET-DATABASE transcription --- gethostbyname, getprotobyname
// and getservbyname, cured in syscall/windows/zsyscall_windows_netdb_impl.cs.
//
// WHAT WENT WRONG. Each generated wrapper reinterpreted ws2_32's returned record address as a
// MANAGED BOX in one line, over records whose Name is `ж<byte>` and whose Aliases / AddrList /
// Proto are `ж<ж<byte>>` or `ж<byte>` --- managed references where native hostent / protoent /
// servent carry raw char* and char**. Nothing faults at the cast; the fabrication is at the first
// READ, which materializes the whole record. net's getprotobyname reads `(~p).Proto`, a single
// uint16, and fabricates Name and Aliases on the way to it.
//
// WHY A GUARD AND NOT THE SUITE. ws2_32 answers these from the host's own resolver and service
// database, so a test that CALLED them would assert what this machine's hosts file and
// %SystemRoot%\System32\drivers\etc\services happen to contain, and would flap with both. The
// repair's whole surface is the TRANSCRIPTION, so the package exposes its copy cores and the
// three transcribe seams, and this class drives them with images it builds itself --- no resolver,
// no network, no database.
//
// WHY THE COPY CORES AND NOT THE POINTERS THEY RETURN. `copyNativeCStringBytes` and
// `copyNativeAddrListBytes` answer the managed arrays, which index directly; the production
// wrappers put those behind `ж<byte>` / `ж<ж<byte>>`. A guard that walked the pointer back would be
// asserting golib's pointer-view machinery as much as the copy, and the copy is what this file
// exists to check. The transcribe seams are driven too, for the record's own scalars and for the
// nil answer.
//
// THE IMAGES ARE SHAPED THE WAY ws2_32 SHAPES ONE: the record first, then the text and the address
// bytes it points at, all inside one pinned array, so the entries carry absolute addresses of their
// own array's interior --- the self-referential shape the real buffer has.
//
// Windows-only by nature (the converted syscall package's windows flavour declares these members at
// all), and gated on the HOST rather than on which declaration was compiled --- the distinction
// WindowsNetUserInfoTests states: a guard compiled for one flavour and run on another host sails
// past a declaration-only check and then dies in a module initializer, which reads like a
// regression in whatever is under test. A non-Windows run must read NOT MEASURED instead.
[TestClass]
public class WindowsNetDbTranscriptionTests
{
    private static bool OnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // `struct hostent` as ws2_32 lays it out on x64, and the same mirror the companion declares.
    // Kept here rather than shared so the guard measures the LAYOUT independently of the code under
    // test: a guard that borrows the subject's own definition of the record cannot notice a change
    // to it.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeHostent
    {
        public nuint Name;
        public nuint Aliases;
        public uint16 AddrType;
        public uint16 Length;
        public nuint AddrList;
    }

    private const int NativeHostentSize = 32;

    // The address the zero-byte arm turns on: 10.0.0.1, whose middle two bytes ARE zero. A copier
    // that treated an address as a C string would stop at the first of them and answer one byte.
    private static readonly byte[] AddressWithInteriorZeros = { 10, 0, 0, 1 };
    private static readonly byte[] AddressWithoutZeros = { 192, 168, 1, 200 };

    private const string HostName = "guard.example";
    private const string AliasOne = "guard";
    private const string AliasTwo = "guard.example.test";

    private static byte[] cString(string text)
    {
        byte[] bytes = new byte[text.Length + 1];

        for (int i = 0; i < text.Length; i++)
        {
            bytes[i] = (byte)text[i];
        }

        bytes[text.Length] = 0;

        return bytes;
    }

    // Builds a hostent image the way ws2_32 leaves one: the record, then the name, the alias
    // pointer run and its strings, the address pointer run and its address bytes --- all in ONE
    // pinned array, with every pointer an absolute address of that array's interior.
    private static unsafe byte[] buildHostentImage(out nuint address)
    {
        byte[] name = cString(HostName);
        byte[] aliasOne = cString(AliasOne);
        byte[] aliasTwo = cString(AliasTwo);

        int ptr = IntPtr.Size;
        int recordOffset = 0;
        int nameOffset = NativeHostentSize;
        int aliasPtrsOffset = nameOffset + name.Length;
        int aliasOneOffset = aliasPtrsOffset + 3 * ptr;          // two aliases plus the NULL
        int aliasTwoOffset = aliasOneOffset + aliasOne.Length;
        int addrPtrsOffset = aliasTwoOffset + aliasTwo.Length;
        int addrOneOffset = addrPtrsOffset + 3 * ptr;            // two addresses plus the NULL
        int addrTwoOffset = addrOneOffset + AddressWithInteriorZeros.Length;
        int total = addrTwoOffset + AddressWithoutZeros.Length;

        byte[] buffer = GC.AllocateArray<byte>(total, pinned: true);

        fixed (byte* basePtr = buffer)
        {
            copyInto(basePtr, nameOffset, name);
            copyInto(basePtr, aliasOneOffset, aliasOne);
            copyInto(basePtr, aliasTwoOffset, aliasTwo);
            copyInto(basePtr, addrOneOffset, AddressWithInteriorZeros);
            copyInto(basePtr, addrTwoOffset, AddressWithoutZeros);

            nuint* aliasPtrs = (nuint*)(basePtr + aliasPtrsOffset);
            aliasPtrs[0] = (nuint)(basePtr + aliasOneOffset);
            aliasPtrs[1] = (nuint)(basePtr + aliasTwoOffset);
            aliasPtrs[2] = 0;

            nuint* addrPtrs = (nuint*)(basePtr + addrPtrsOffset);
            addrPtrs[0] = (nuint)(basePtr + addrOneOffset);
            addrPtrs[1] = (nuint)(basePtr + addrTwoOffset);
            addrPtrs[2] = 0;

            NativeHostent* record = (NativeHostent*)(basePtr + recordOffset);
            record->Name = (nuint)(basePtr + nameOffset);
            record->Aliases = (nuint)aliasPtrs;
            record->AddrType = 2;                                 // AF_INET
            record->Length = (uint16)AddressWithInteriorZeros.Length;
            record->AddrList = (nuint)addrPtrs;

            address = (nuint)basePtr;
        }

        return buffer;
    }

    private static unsafe void copyInto(byte* basePtr, int offset, byte[] source)
    {
        for (int i = 0; i < source.Length; i++)
        {
            basePtr[offset + i] = source[i];
        }
    }

    // ARM 1 --- the mirror's own size. Asserted here as well as in the companion, because a guard
    // that borrowed the subject's constant would agree with a wrong one.
    [TestMethod]
    public unsafe void TheHostentMirrorIsTheSizeWs2_32Writes()
    {
        Assert.AreEqual<int>(NativeHostentSize, sizeof(NativeHostent),
            "struct hostent is 32 bytes on x64: two pointers, two shorts, four bytes of padding, " +
            "a pointer. A mirror of any other size reads every field past the first from the " +
            "wrong offset");
    }

    // ARM 2 --- the C-string copy keeps every byte AND the terminator. A reader of a *byte name in
    // Go stops at the NUL, so dropping it would leave the copy unterminated and the next reader
    // walking into whatever follows.
    [TestMethod]
    public unsafe void CStringCopyKeepsEveryByteAndItsTerminator()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("the converted syscall package's netdb members are the windows flavor");
        }

        byte[] source = cString(HostName);

        fixed (byte* sourcePtr = source)
        {
            array<byte> copied = syscall.copyNativeCStringBytes(sourcePtr);

            Assert.AreEqual<int>(source.Length, (int)copied.Length,
                "the copy is the string's bytes PLUS its NUL, which is what a Go reader stops at");

            for (int i = 0; i < source.Length; i++)
            {
                Assert.AreEqual<int>(source[i], (int)copied[i], $"byte {i} of the copied name");
            }
        }
    }

    // ARM 3 --- THE ONE THIS CLASS EXISTS FOR. h_addr_list's entries are BINARY addresses of
    // h_length bytes, not strings, and an IPv4 address contains zero bytes as data. A copier that
    // treated them as C strings would stop at the first zero: 10.0.0.1 would come back as one byte.
    // Both addresses are asserted whole, and the first one is chosen to carry interior zeros.
    [TestMethod]
    public unsafe void AddrListCopyKeepsZeroBytesInsideAnAddress()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("the converted syscall package's netdb members are the windows flavor");
        }

        byte[] buffer = buildHostentImage(out nuint address);

        fixed (byte* basePtr = buffer)
        {
            NativeHostent* record = (NativeHostent*)basePtr;

            array<array<byte>> addresses = syscall.copyNativeAddrListBytes(
                (byte**)record->AddrList, (nint)record->Length);

            Assert.AreEqual<int>(2, (int)addresses.Length, "both addresses the run holds are lifted");

            for (int i = 0; i < AddressWithInteriorZeros.Length; i++)
            {
                Assert.AreEqual<int>(AddressWithInteriorZeros[i], (int)addresses[0][i],
                    $"byte {i} of 10.0.0.1 --- a C-string copy would have stopped at byte 1");
            }

            for (int i = 0; i < AddressWithoutZeros.Length; i++)
            {
                Assert.AreEqual<int>(AddressWithoutZeros[i], (int)addresses[1][i],
                    $"byte {i} of the second address");
            }
        }

        GC.KeepAlive(buffer);
    }

    // ARM 4 --- the transcribe seam over a whole record: the scalars are the record's own, and the
    // three pointer fields are populated rather than nil.
    [TestMethod]
    public unsafe void TranscribedHostentCarriesTheRecordsOwnFields()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("the converted syscall package's netdb members are the windows flavor");
        }

        byte[] buffer = buildHostentImage(out nuint address);

        var Ꮡh = syscall.transcribeHostent(address);

        Assert.IsFalse(Ꮡh == nil, "a populated record transcribes to a record, not to nil");
        Assert.AreEqual<int>(2, (int)Ꮡh.Value.AddrType, "AddrType is the record's own AF_INET");
        Assert.AreEqual<int>(AddressWithInteriorZeros.Length, (int)Ꮡh.Value.Length,
            "Length is the record's own address width, and it is what AddrList is copied by");
        Assert.IsFalse(Ꮡh.Value.Name == nil, "Name is transcribed, not left fabricated");
        Assert.IsFalse(Ꮡh.Value.Aliases == nil, "Aliases is transcribed");
        Assert.IsFalse(Ꮡh.Value.AddrList == nil, "AddrList is transcribed");

        GC.KeepAlive(buffer);
    }

    // ARM 5 --- a null address is nil, not a record over address zero. ws2_32 answers NULL on
    // failure and the wrapper checks the return before transcribing, but the seam is public and
    // must answer for itself.
    [TestMethod]
    public unsafe void ANullAddressTranscribesToNil()
    {
        if (!OnWindows)
        {
            Assert.Inconclusive("the converted syscall package's netdb members are the windows flavor");
        }

        Assert.IsTrue(syscall.transcribeHostent((nuint)0) == nil, "hostent");
        Assert.IsTrue(syscall.transcribeProtoent((nuint)0) == nil, "protoent");
        Assert.IsTrue(syscall.transcribeServent((nuint)0) == nil, "servent");
    }

    // WHAT THIS CLASS DOES NOT ASSERT, named rather than left to be discovered:
    //
    //   * the ws2_32 calls themselves. They answer from the host's resolver and service database,
    //     which is what this guard is built to do without.
    //   * the ALIAS list's contents through the transcribed record. copyNativeCStringBytes is
    //     asserted directly (arm 2) and copyNativeCStringList is the same copier in a loop; reading
    //     the list back through `ж<ж<byte>>` would assert golib's pointer views rather than this
    //     copy.
    //   * protoent's and servent's own scalars through a built image. Their records are the same
    //     shape one field simpler, and arm 5 drives both seams; a full image for each would repeat
    //     arm 4 rather than add to it.
}
