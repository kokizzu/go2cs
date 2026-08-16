// zsyscall_windows_wsa_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of the OVERLAPPED (asynchronous) half of the Windows socket surface --
// the SUBMIT SEAM of the managed netpoller arc. Design and rulings:
// docs/phase4/DESIGN-netpoll-managed-poller.md §4.3/§4.4/§4.5 (§9 RULED 2026-08-13; the accept
// handover RULED 2026-08-14).
//
// WHAT THE POLLER ALONE COULD NOT DO. internal/poll's ten runtime_poll* contracts are hand-owned in
// internal/poll/windows/runtime_netpoll_impl.cs, and they make a completion WAKE a blocked goroutine.
// That is half an arc. The other half is that the overlapped submissions execIO issues must actually
// reach the kernel and complete into memory the managed side can read -- and today they cannot, for
// three separate reasons that this file answers one at a time.
//
// (1) THE NATIVE-LAYOUT WALL, the established class with new members. syscall.WSABuf is
// `{uint32 Len; ж<byte> Buf}` -- a MANAGED REFERENCE where native WSABUF wants a raw CHAR* -- so the
// address of a WSABuf is not a native WSABUF. Same class as Timezoneinformation / win32finddata1 /
// RawSockaddrInet4, same remedy: a blittable [StructLayout(Sequential)] mirror with a field-for-field
// copy at the boundary. AcceptEx's output buffer is the class's DECODE side: net hands it a
// slice<RawSockaddrAny> reinterpreted as bytes, whose managed layout cannot hold the native sockaddr
// block at all, so the mirror there is a NATIVE STAGING buffer transcribed back at parse time.
//
// (2) THE LIFETIME WALL, which is what ASYNC adds to that class. The established mirror is "a LOCAL
// at the call site, trivially stable for exactly that long" (syscall_windows_impl.cs). An overlapped
// operation breaks that premise twice over. The kernel retains the OVERLAPPED and the buffer pointers
// until COMPLETION -- unbounded -- and `&o.o` is an interior field address inside a reference-bearing
// container, which golib's address model explicitly CANNOT hold still: EnsureStableAddress pins
// PinnableStorage, which for a struct-field reference recurses to the container's storage, and the
// ж<operation> box's slot is null because `operation` holds managed references (ж<FD>, WSABuf.Buf,
// slice<WSABuf>, ж<RawSockaddrAny>). So the address the generated wrapper would hand the kernel is
// transient by construction -- the pipe-EOF defect (ж.cs's war story) with an unbounded window.
// Second, the OVERLAPPED is the operation's kernel-side IDENTITY: execIO names the same `&o.o` at
// THREE call sites -- submit, CancelIoEx, WSAGetOverlappedResult -- and cancellation matches BY
// ADDRESS, so any scheme minting a fresh native copy per call breaks cancellation outright.
//
// THE REMEDY IS A PER-OPERATION RECORD (OverlappedOp below) owning native-lifetime state, keyed by
// the ж<Overlapped> the waiter names the operation by. That key works, and it was verified rather
// than assumed: ж<T>.Equals for a struct-field reference compares (resolved source pointer, field
// identity token) and GetHashCode agrees with it, so all three of execIO's `Ꮡo.of(operation.Ꮡo)`
// mints resolve to ONE record -- and so do the mints of two SEPARATE FD.Read calls, because
// SameSource resolves an `of()` chain recursively rather than by object reference. ж.cs says this
// property exists to serve "the address-keyed runtime semaphores in the hand-owned
// sync/internal-poll implementations", so this arc reuses a guarantee the corpus already depends on.
// The one bound worth carrying: equality is SOURCE-POINTER identity, so a future call site that
// re-BOXED the operation (a standard heap box rather than `Ꮡfd.of(FD.Ꮡrop)`) would silently mint a
// second record.
//
// (3) THE REFERENCE GRAPH, which is why the wake-up goes through golib. internal/poll REFERENCES
// syscall, so the completion callback here cannot call back into the poller. golib is the one
// assembly both sides see, and GoAsyncIO is the descriptor-keyed rendezvous: the poller registers a
// readiness sink for its descriptor at pollOpen, and the callback below signals it knowing only the
// descriptor and the mode -- which is all a completion callback CAN know. MODE is known from the
// WRAPPER, not from the operation: WSARecv/AcceptEx are always the read operation and
// WSASend/ConnectEx always the write one, because each FD has exactly one rop and one wop for its
// lifetime.
//
// WHERE THE CLR ASSOCIATION IS MADE, and why it is LAZY. Binding happens at the FIRST SUBMIT, not at
// pollOpen (design §4.2 as amended at S2). Go's poller REJECTS completions it does not own --
// pollOperationFromOverlappedEntry compares the completion key against the pollDesc in the operation
// and drops a mismatch (go.dev/issue/58870) -- while ThreadPoolBoundHandle has no equivalent: its
// callback resolves state FROM the NativeOverlapped it allocated, so a FOREIGN overlapped arriving at
// that port is MISREAD rather than ignored. internal/poll has a live producer of one (FD.WSAIoctl
// passes the CALLER's syscall.Overlapped, which being all-scalar in a standard box really does pin
// and really does reach the kernel). Binding lazily removes the hazard structurally: a socket that
// only ever sees foreign overlapped IO is never associated at all.
//
// THE OVERLAPPED IS FREED AT THE NEXT SUBMIT, NOT IN THE CALLBACK, and that is deliberate. execIO
// harvests with WSAGetOverlappedResult AFTER its wait returns, and that call reads Internal /
// InternalHigh out of the very control block the kernel wrote -- so the block must outlive the
// completion callback. Freeing at the next Rearm (and at teardown) also covers the skipSyncNotif
// path, where a synchronously-successful submit posts NO completion packet and no callback ever runs.
//
// WHAT IS NOT HERE, per the board's do-it-when-a-suite-reaches-it ruling: the UDP family
// (WSARecvFrom, WSASendto and the internal/syscall/windows WSASendtoInet4/6, WSARecvMsg, WSASendMsg)
// and TransmitFile. They are the same machinery with more staging; nothing on the TCP
// listen/dial/accept/read/write path touches them. WSAGetOverlappedResult lives in
// internal/syscall/windows and has its own companion file there; it needs exactly ONE property from
// this one -- the operation's native address -- which it reads through GoAsyncIO.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using GoAsyncIO = go.golib.GoAsyncIO;
using IGoAsyncOperation = go.golib.IGoAsyncOperation;
using Goroutine = go.golib.Goroutine;
using NativeMemory = System.Runtime.InteropServices.NativeMemory;

// Hand-owned (no zsyscall_windows_wsa_impl.go exists, so a reconvert never regenerates this file).
// The declarations it replaces are registered in the converter's manualConversionFuncs, which is what
// turns their generated bodies into placeholders.
[module: go.GoManualConversion]

// Native mirrors, NativeOverlapped* and the staging buffers are all pointer work. Declared rather
// than inherited -- see zsyscall_windows_impl.cs.
[module: go.GoRequiresUnsafe]

namespace go;

partial class syscall_package
{
    // ---- Modes -----------------------------------------------------------------------------------
    //
    // internal/poll names a mode with the RUNE ('r' / 'w'); the readiness sink speaks the same
    // vocabulary, since the poller is the only consumer.
    private const int wsaModeRead = 'r';
    private const int wsaModeWrite = 'w';

    // ---- Native mirrors --------------------------------------------------------------------------

    // WSABUF exactly as Windows lays it out: a length and a RAW pointer. The converted WSABuf holds
    // that pointer as a golib ж<byte>, which is a managed reference -- the whole of defect (1).
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeWSABuf
    {
        public uint32 Len;
        public byte* Buf;
    }

    // ---- The per-descriptor association ----------------------------------------------------------

    // A SafeHandle that does NOT own its handle. ThreadPoolBoundHandle.BindHandle needs a SafeHandle,
    // but the socket belongs to internal/poll from open to close -- an owning wrapper would let the
    // finalizer close a descriptor the Go program still holds.
    private sealed class UnownedHandle : SafeHandle
    {
        internal UnownedHandle(nint value) : base(nint.Zero, ownsHandle: false) => SetHandle(value);

        public override bool IsInvalid => handle == nint.Zero || handle == -1;

        protected override bool ReleaseHandle() => true;
    }

    // Everything one socket needs to run overlapped IO through the CLR's completion port, created
    // EXACTLY once per descriptor (GoAsyncIO's Lazy-backed slot -- a bare GetOrAdd would let two
    // threads both bind, and binding one socket twice is a kernel error) and retired by the poller's
    // pollClose, which runs BEFORE the socket is closed precisely so this can unregister first.
    private sealed class SocketBinding : IDisposable
    {
        internal readonly nuint Descriptor;
        internal readonly ThreadPoolBoundHandle Bound;

        private readonly UnownedHandle m_handle;
        private readonly List<OverlappedOp> m_operations = new();
        private bool m_disposed;

        internal SocketBinding(nuint descriptor)
        {
            Descriptor = descriptor;
            m_handle = new UnownedHandle(unchecked((nint)descriptor));
            Bound = ThreadPoolBoundHandle.BindHandle(m_handle);
        }

        internal void Track(OverlappedOp operation)
        {
            lock (m_operations)
                m_operations.Add(operation);
        }

        public void Dispose()
        {
            List<OverlappedOp> operations;

            lock (m_operations)
            {
                if (m_disposed)
                    return;

                m_disposed = true;
                operations = new List<OverlappedOp>(m_operations);
                m_operations.Clear();
            }

            foreach (OverlappedOp operation in operations)
            {
                GoAsyncIO.RemoveOperationState(operation.Key);
                operation.Dispose();
            }

            Bound.Dispose();
            m_handle.Dispose();
        }
    }

    private static SocketBinding bindingFor(nuint descriptor) =>
        (SocketBinding)GoAsyncIO.GetOrCreateDescriptorState(descriptor, () => new SocketBinding(descriptor));

    // ---- The per-operation record ----------------------------------------------------------------

    // One record per (FD, mode) for the FD's lifetime, holding every piece of state the kernel is
    // allowed to see: the NativeOverlapped the operation is identified by, the native WSABUF array or
    // accept staging block, and -- crucially -- the ж<byte> BOXES whose pins hold the caller's own
    // buffers still. Holding the address would not be enough: the pin is a GCHandle owned by that one
    // box and released with it, so a later InitBuf dropping the last reference to a box the kernel is
    // still writing through would unpin memory mid-flight.
    private sealed unsafe class OverlappedOp : IGoAsyncOperation, IDisposable
    {
        internal readonly SocketBinding Binding;
        internal readonly nint Mode;

        // The key this record is filed under in golib's operation table. Kept so that retiring the
        // DESCRIPTOR retires the operation entries too -- the key is a ж<Overlapped> field reference,
        // which holds its source ж<FD> box alive, so leaving it behind would pin the whole FD graph
        // of every socket the program ever opened.
        internal readonly object Key;

        private readonly PreAllocatedOverlapped m_preallocated;
        private NativeOverlapped* m_native;

        // Native staging: the WSABUF array for send/recv, the AcceptEx output block for accept.
        private void* m_staging;
        private nuint m_stagingLength;

        // Boxes whose GCHandles pin the caller's buffers for the flight; replaced at each submit.
        private readonly List<object> m_pins = new();

        private bool m_disposed;

        internal OverlappedOp(SocketBinding binding, nint mode, object key)
        {
            Binding = binding;
            Mode = mode;
            Key = key;
            m_preallocated = new PreAllocatedOverlapped(completed, this, null);
        }

        // The address the kernel knows this operation by -- and the ENTIRE contract
        // internal/syscall/windows' WSAGetOverlappedResult needs from this package.
        public nuint NativeAddress => (nuint)m_native;

        // Frees the previous control block (see the file header on why not in the callback) and
        // allocates the one this submit will use. A PreAllocatedOverlapped may have only one
        // allocation outstanding, so free-then-allocate is required, not merely tidy.
        internal NativeOverlapped* Rearm()
        {
            releaseOverlapped();
            m_native = Binding.Bound.AllocateNativeOverlapped(m_preallocated);

            m_pins.Clear();

            return m_native;
        }

        internal void Pin(object box) => m_pins.Add(box);

        // Native staging of at least `length` bytes, reused across submits when it already fits.
        internal void* Staging(nuint length)
        {
            if (m_staging is not null && m_stagingLength >= length)
                return m_staging;

            if (m_staging is not null)
                NativeMemory.Free(m_staging);

            m_staging = NativeMemory.AllocZeroed(length);
            m_stagingLength = length;

            return m_staging;
        }

        internal nuint StagingLength => m_stagingLength;

        private void releaseOverlapped()
        {
            if (m_native is null)
                return;

            Binding.Bound.FreeNativeOverlapped(m_native);
            m_native = null;
        }

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            releaseOverlapped();
            m_preallocated.Dispose();

            if (m_staging is not null)
            {
                NativeMemory.Free(m_staging);
                m_staging = null;
                m_stagingLength = 0;
            }

            m_pins.Clear();
        }
    }

    // The completion callback, run on a CLR IO thread-pool thread. It does exactly one thing: name
    // the descriptor and the mode to whoever registered a readiness sink for that descriptor. It
    // deliberately does NOT interpret errorCode -- that is a Win32 code, and execIO branches on WSA
    // codes (WSAECONNRESET where Win32 says ERROR_NETNAME_DELETED), so the harvest asks the OS
    // instead. It deliberately does NOT free the overlapped either (see the file header).
    private static unsafe void completed(uint32 errorCode, uint32 numBytes, NativeOverlapped* native)
    {
        if (ThreadPoolBoundHandle.GetNativeOverlappedState(native) is OverlappedOp operation)
            GoAsyncIO.Signal(operation.Binding.Descriptor, operation.Mode);
    }

    // Resolves (creating exactly once) the record for one operation. `Ꮡoverlapped` is the key: see
    // the file header on why it resolves across execIO's three call sites and across separate calls
    // on one FD.
    private static OverlappedOp operationFor(ΔHandle handle, ж<Overlapped> Ꮡoverlapped, nint mode)
    {
        nuint descriptor = (nuint)(uintptr)handle;

        OverlappedOp operation = (OverlappedOp)GoAsyncIO.GetOrCreateOperationState(Ꮡoverlapped, () => {
            SocketBinding binding = bindingFor(descriptor);
            OverlappedOp created = new(binding, mode, Ꮡoverlapped);

            binding.Track(created);

            return created;
        });

        return operation;
    }

    // ---- WSARecv / WSASend -----------------------------------------------------------------------

    // Copies the caller's WSABuf descriptors into the record's native WSABUF array, pinning each
    // buffer through the ж<byte> box the caller already holds (o.buf.Buf is `Ꮡ(buf, 0)`, an
    // element reference whose (void*) resolves through PinnableStorage to the slice's backing array
    // and pins it for the box's lifetime). Returns the native array's address.
    private static unsafe NativeWSABuf* stageBuffers(OverlappedOp operation, ж<WSABuf> Ꮡbufs, uint32 bufcnt) {
        nuint bytes = (nuint)bufcnt * (nuint)sizeof(NativeWSABuf);
        NativeWSABuf* native = (NativeWSABuf*)operation.Staging(bytes == 0 ? (nuint)sizeof(NativeWSABuf) : bytes);

        for (uint32 i = 0; i < bufcnt; i++) {
            // Index 0 is the common case and the only one a struct-FIELD reference (`&o.buf`) can
            // answer; a multi-buffer submit always names a slice element (`&o.bufs[0]`), which
            // unsafe.Add steps through the backing array.
            ж<WSABuf> Ꮡbuf = i == 0 ? Ꮡbufs : unsafe_package.Add(Ꮡbufs, (uintptr)i);
            ref WSABuf buf = ref Ꮡbuf.Value;

            native[i].Len = buf.Len;

            if (buf.Buf == nil) {
                native[i].Buf = null;
            } else {
                native[i].Buf = (byte*)(void*)buf.Buf;
                operation.Pin(buf.Buf);
            }
        }

        return native;
    }

    public static unsafe error /*err*/ WSARecv(ΔHandle s, ж<WSABuf> Ꮡbufs, uint32 bufcnt, ж<uint32> Ꮡrecvd, ж<uint32> Ꮡflags, ж<Overlapped> Ꮡoverlapped, ж<byte> Ꮡcroutine) {
        OverlappedOp operation = operationFor(s, Ꮡoverlapped, wsaModeRead);
        NativeOverlapped* native = operation.Rearm();
        NativeWSABuf* buffers = stageBuffers(operation, Ꮡbufs, bufcnt);

        // Out-parameters marshal through call-local natives and copy back: they are written only
        // during the SYNCHRONOUS portion (the async result is harvested from the overlapped), so the
        // mirror-is-a-local doctrine holds for them even though it cannot hold for the overlapped.
        // `flags` is IN/OUT -- WSARecv reads it -- so it is seeded from the caller's value.
        uint32 recvd = 0;
        uint32 flags = Ꮡflags == nil ? 0 : Ꮡflags.Value;

        var (r1, _, e1) = Syscall9(procWSARecv.Addr(), 7, (uintptr)s, (uintptr)(void*)buffers, (uintptr)bufcnt, (uintptr)(void*)(&recvd), (uintptr)(void*)(&flags), (uintptr)(void*)native, 0, 0, 0);

        if (Ꮡrecvd != nil) {
            Ꮡrecvd.Value = recvd;
        }

        if (Ꮡflags != nil) {
            Ꮡflags.Value = flags;
        }

        return r1 == socket_error ? errnoErr(e1) : default!;
    }

    public static unsafe error /*err*/ WSASend(ΔHandle s, ж<WSABuf> Ꮡbufs, uint32 bufcnt, ж<uint32> Ꮡsent, uint32 flags, ж<Overlapped> Ꮡoverlapped, ж<byte> Ꮡcroutine) {
        OverlappedOp operation = operationFor(s, Ꮡoverlapped, wsaModeWrite);
        NativeOverlapped* native = operation.Rearm();
        NativeWSABuf* buffers = stageBuffers(operation, Ꮡbufs, bufcnt);

        uint32 sent = 0;

        var (r1, _, e1) = Syscall9(procWSASend.Addr(), 7, (uintptr)s, (uintptr)(void*)buffers, (uintptr)bufcnt, (uintptr)(void*)(&sent), (uintptr)flags, (uintptr)(void*)native, 0, 0, 0);

        if (Ꮡsent != nil) {
            Ꮡsent.Value = sent;
        }

        return r1 == socket_error ? errnoErr(e1) : default!;
    }

    // ---- AcceptEx and the accept HANDOVER --------------------------------------------------------

    // What AcceptEx staged, for GetAcceptExSockaddrs to parse. Parked in a GOROUTINE-keyed slot,
    // because GetAcceptExSockaddrs carries no handle and no overlapped -- see the ruling quoted on
    // the field below.
    private sealed class AcceptStaging
    {
        internal OverlappedOp Operation = null!;
        internal nuint Buffer;
        internal uint32 ReceiveLength;
        internal uint32 LocalLength;
        internal uint32 RemoteLength;
    }

    // ⚠ A COUPLING BETWEEN TWO WRAPPERS GO KEEPS INDEPENDENT, ruled deliberately (coordinator,
    // 2026-08-14; design §4.5) and documented here because nothing in the type system sees it.
    //
    // GetAcceptExSockaddrs' signature is (buf, rxdatalen, laddrlen, raddrlen, *lrsa, *lrsalen, *rrsa,
    // *rrsalen): no handle, no overlapped, no identity of any kind. Go needs none, because `buf`
    // really is the caller's [2]RawSockaddrAny and the kernel wrote native bytes into it. Under go2cs
    // neither half survives -- net passes `Ꮡ(rawsa, 0).Reinterpret<RawSockaddrAny, byte>()`, which is
    // an UNPINNED native-address box over a managed array whose layout is not the native one, so it
    // is unusable as data AND unusable as a table key (its identity is physical, so two evaluations
    // are equal only if the GC did not move the array in between).
    //
    // So AcceptEx parks its native staging block here and GetAcceptExSockaddrs consumes it. THE
    // PREMISE is goroutine affinity: the corpus's only caller is net.netFD.accept, which runs
    // FD.Accept and then GetAcceptExSockaddrs on ONE goroutine, with no other accept interleaved.
    // The key is the GOROUTINE, not a [ThreadStatic]: under the dedicated-thread executor those
    // coincide today, but keying on golib's scheduler-arc identity states the premise instead of
    // relying on the coincidence, and survives an executor that stops making it true. (Cross-arc
    // dependency, deliberately: the scheduler arc's Goroutine registry is what makes this handoff
    // sound.)
    //
    // SINGLE OCCUPANCY, CONSUME EXACTLY ONCE, FAIL BY NAME. A sequence Go permits but the corpus
    // never issues must be loud, never misread: parking over a DIFFERENT operation's park throws, and
    // consuming an empty slot throws. Re-parking the SAME operation is a replace, not a fault --
    // FD.Accept legitimately re-submits `fd.rop` after a WSAECONNRESET without an intervening parse,
    // and the park's content is identical anyway (same record, same staging block).
    //
    // Keys are held WEAKLY. A goroutine that accepted and then died without parsing -- an accept that
    // FAILED, since the success path always parses -- would otherwise leave its entry behind forever,
    // and "one goroutine per accept" is a shape real servers write.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, AcceptStaging> acceptHandoff = new();

    // The goroutine running this call, or the thread when there is none (a host that runs Go code
    // without entering a goroutine scope). Both are stable for the accept-then-parse sequence.
    private static object acceptHandoffKey() => (object?)Goroutine.Current ?? Thread.CurrentThread;

    public static unsafe error /*err*/ AcceptEx(ΔHandle ls, ΔHandle @as, ж<byte> Ꮡbuf, uint32 rxdatalen, uint32 laddrlen, uint32 raddrlen, ж<uint32> Ꮡrecvd, ж<Overlapped> Ꮡoverlapped) {
        // The listening socket owns the overlapped and is the descriptor the completion arrives on;
        // `as` is merely the socket the kernel fills in. Accept is internal/poll's READ operation.
        OverlappedOp operation = operationFor(ls, Ꮡoverlapped, wsaModeRead);
        NativeOverlapped* native = operation.Rearm();

        // The caller's `buf` cannot be used (see the coupling note above); the kernel writes into the
        // record's own native block, which GetAcceptExSockaddrs then parses.
        nuint length = (nuint)rxdatalen + laddrlen + raddrlen;
        void* staging = operation.Staging(length);

        object handoffKey = acceptHandoffKey();

        // The slot is goroutine-PRIVATE by construction, so the read-then-write below needs no lock:
        // only this goroutine can park into it or consume it.
        if (acceptHandoff.TryGetValue(handoffKey, out AcceptStaging? parked) && !ReferenceEquals(parked.Operation, operation)) {
            throw new InvalidOperationException(
                "syscall: AcceptEx staged an accept while another accept on this goroutine was still awaiting GetAcceptExSockaddrs");
        }

        acceptHandoff.AddOrUpdate(handoffKey, new AcceptStaging {
            Operation = operation,
            Buffer = (nuint)staging,
            ReceiveLength = rxdatalen,
            LocalLength = laddrlen,
            RemoteLength = raddrlen
        });

        uint32 recvd = 0;

        var (r1, _, e1) = Syscall9(procAcceptEx.Addr(), 8, (uintptr)ls, (uintptr)@as, (uintptr)staging, (uintptr)rxdatalen, (uintptr)laddrlen, (uintptr)raddrlen, (uintptr)(void*)(&recvd), (uintptr)(void*)native, 0);

        if (Ꮡrecvd != nil) {
            Ꮡrecvd.Value = recvd;
        }

        return r1 == 0 ? errnoErr(e1) : default!;
    }

    // Transcribes one native sockaddr into the MANAGED RawSockaddrAny image that
    // RawSockaddrAny.Sockaddr (syscall_windows_impl.cs) reads -- the exact inverse of the flattening
    // that method performs, and the reason the two are a pair. The Go declaration's own mapping:
    // Family at 0, Addr.Data covering bytes 2..15, Pad covering 16..115.
    private static unsafe ж<RawSockaddrAny> toRawSockaddrAny(byte* native, nint available) {
        var Ꮡany = @new<RawSockaddrAny>();
        ref RawSockaddrAny any = ref Ꮡany.Value;

        if (available >= 2) {
            any.Addr.Family = *(uint16*)native;
        }

        for (nint i = 0; i < 14 && 2 + i < available; i++) {
            any.Addr.Data[i] = unchecked((int8)native[2 + i]);
        }

        for (nint i = 0; i < 100 && 16 + i < available; i++) {
            any.Pad[i] = unchecked((int8)native[16 + i]);
        }

        return Ꮡany;
    }

    public static unsafe void GetAcceptExSockaddrs(ж<byte> Ꮡbuf, uint32 rxdatalen, uint32 laddrlen, uint32 raddrlen, ж<ж<RawSockaddrAny>> Ꮡlrsa, ж<int32> Ꮡlrsalen, ж<ж<RawSockaddrAny>> Ꮡrrsa, ж<int32> Ꮡrrsalen) {
        object handoffKey = acceptHandoffKey();

        if (!acceptHandoff.TryGetValue(handoffKey, out AcceptStaging? staged)) {
            throw new InvalidOperationException(
                "syscall: GetAcceptExSockaddrs called with no AcceptEx staging parked on this goroutine");
        }

        acceptHandoff.Remove(handoffKey);

        byte* buffer = (byte*)staged.Buffer;
        nuint stagedLength = (nuint)staged.ReceiveLength + staged.LocalLength + staged.RemoteLength;

        byte* localAddr = null;
        int32 localLen = 0;
        byte* remoteAddr = null;
        int32 remoteLen = 0;

        Syscall9(procGetAcceptExSockaddrs.Addr(), 8, (uintptr)(void*)buffer, (uintptr)staged.ReceiveLength, (uintptr)staged.LocalLength, (uintptr)staged.RemoteLength, (uintptr)(void*)(&localAddr), (uintptr)(void*)(&localLen), (uintptr)(void*)(&remoteAddr), (uintptr)(void*)(&remoteLen), 0);

        // The kernel's pointers land inside the staging block; bound every read by what is actually
        // there rather than by the 116 bytes a RawSockaddrAny could hold.
        nint localRoom = localAddr is null ? 0 : (nint)(stagedLength - (nuint)(localAddr - buffer));
        nint remoteRoom = remoteAddr is null ? 0 : (nint)(stagedLength - (nuint)(remoteAddr - buffer));

        if (Ꮡlrsa != nil) {
            // ValueSlot, not Value: T here is itself a POINTER (ж<ж<RawSockaddrAny>>), so the
            // caller's variable legitimately holds null on entry -- and Value's nil-pointer check
            // VALUE-PEEKS, so it would panic on the very write that fills it in. ValueSlot is the
            // documented form for exactly this shape (golib ж.cs).
            Ꮡlrsa.ValueSlot = localAddr is null ? default! : toRawSockaddrAny(localAddr, localRoom);
        }

        if (Ꮡrrsa != nil) {
            Ꮡrrsa.ValueSlot = remoteAddr is null ? default! : toRawSockaddrAny(remoteAddr, remoteRoom);
        }

        if (Ꮡlrsalen != nil) {
            Ꮡlrsalen.Value = localLen;
        }

        if (Ꮡrrsalen != nil) {
            Ꮡrrsalen.Value = remoteLen;
        }
    }

    // ---- CancelIoEx ------------------------------------------------------------------------------

    // execIO cancels BY ADDRESS, so the cancel must name the record's one true native control block
    // -- never a fresh copy, which the kernel has never seen.
    //
    // TWO CALLERS, and they need different answers. fd_windows.cs:212 cancels ONE operation (the
    // deadline/close path); fd_windows.cs:403 cancels ALL IO on a handle with a NIL overlapped (the
    // pipe/file close path), which passes straight through. A non-nil overlapped with no record
    // answers ERROR_NOT_FOUND -- which execIO already tolerates as "the IO already completed" --
    // rather than falling back to 0 and cancelling everything on the socket.
    public static unsafe error /*err*/ CancelIoEx(ΔHandle s, ж<Overlapped> Ꮡo) {
        uintptr address = 0;

        if (Ꮡo != nil) {
            if (!GoAsyncIO.TryGetOperationAddress(Ꮡo, out nuint native)) {
                return ((error)ERROR_NOT_FOUND);
            }

            address = native;
        }

        var (r1, _, e1) = Syscall(procCancelIoEx.Addr(), 2, (uintptr)s, address, 0);

        return r1 == 0 ? errnoErr(e1) : default!;
    }

    // ---- LoadConnectEx ---------------------------------------------------------------------------

    // NOT part of the overlapped family, and hand-owned for a reason this arc MEASURED rather than
    // predicted: the design recorded the extension-pointer lookup as "synchronous and already
    // working". It is not. The converted body is defective at BOTH ends of one WSAIoctl call.
    //
    //   IN: `ᏑWSAID_CONNECTEX.Reinterpret<GUID, byte>()`. syscall.GUID holds `Data4 [8]byte` as a
    //   golib array<byte> MANAGED REFERENCE, so the struct is reference-bearing: Reinterpret cannot
    //   alias it as bytes, falls back to a raw-address box with NO pin, and the 16 bytes the kernel
    //   reads are a CLR auto-layout image with an object reference in them -- never WSAID_CONNECTEX.
    //   Windows answers WSAEINVAL, which surfaces through the hand-owned ConnectEx as
    //   "failed to find ConnectEx: An invalid argument was supplied" -- the exact shape crypto/tls's
    //   census banked nine times, one per loopback dial, each dying in ~2 ms.
    //
    //   OUT: `ᏑconnectExFunc.of(connectExFuncᴛ1.Ꮡaddr).Reinterpret<uintptr, byte>()` is an interior
    //   field address inside a struct that holds an `error` -- reference-bearing, so unpinnable, so
    //   transient. Even a successful call would write the function pointer to an address the GC is
    //   free to have moved.
    //
    // The remedy is the package's established one: a blittable native image for the GUID, a native
    // uintptr for the result, both stack locals live for exactly the call, and the answer assigned to
    // the managed field as an ordinary managed write. The GUID VALUE is still the converted
    // declaration's -- read out of ᏑWSAID_CONNECTEX field by field -- so there is one definition of
    // it and this file does not restate a constant.
    public static unsafe error LoadConnectEx() {
        ᏑconnectExFunc.of(connectExFuncᴛ1.Ꮡonce).Do(() => {
            var (s, err) = Socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

            if (err != default!) {
                connectExFunc.err = err;
                return;
            }

            try {
                // The 16-byte native GUID: {Data1 uint32}{Data2 uint16}{Data3 uint16}{Data4 [8]byte},
                // which is the on-the-wire layout Windows compares against.
                byte* guid = stackalloc byte[16];
                ref GUID managed = ref ᏑWSAID_CONNECTEX.Value;

                *(uint32*)guid = managed.Data1;
                *(uint16*)(guid + 4) = managed.Data2;
                *(uint16*)(guid + 6) = managed.Data3;

                for (nint i = 0; i < 8; i++) {
                    guid[8 + i] = managed.Data4[i];
                }

                uintptr address = 0;
                uint32 bytes = 0;

                var (r1, _, e1) = Syscall9(procWSAIoctl.Addr(), 9, (uintptr)s, (uintptr)SIO_GET_EXTENSION_FUNCTION_POINTER, (uintptr)(void*)guid, (uintptr)16, (uintptr)(void*)(&address), (uintptr)(uint32)sizeof(uintptr), (uintptr)(void*)(&bytes), 0, 0);

                if (r1 == socket_error) {
                    connectExFunc.err = errnoErr(e1);
                    return;
                }

                connectExFunc.addr = address;
            } finally {
                // Go closes the probe socket with CloseHandle (not closesocket); kept verbatim.
                CloseHandle(s);
            }
        });

        return connectExFunc.err;
    }

    // ---- The record's native overlapped, for this package's other hand-owns ----------------------

    // ConnectEx lives in syscall_windows_impl.cs (it was hand-owned by the sockaddr lane before this
    // arc existed) and needs the same record machinery as the wrappers above.
    internal static unsafe uintptr rearmOverlapped(ΔHandle handle, ж<Overlapped> Ꮡoverlapped, nint mode) {
        return (uintptr)(void*)operationFor(handle, Ꮡoverlapped, mode).Rearm();
    }
}
