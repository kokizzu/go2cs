using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

[TestClass]
public class GoAsyncIOTests
{
    // GoAsyncIO is the rendezvous between a package that SUBMITS asynchronous OS operations and one
    // that WAITS for them, where the reference graph forbids the callback from calling back
    // (docs/phase4/DESIGN-netpoll-managed-poller.md §4.4). Its corpus consumers are the netpoll
    // hand-owns — internal/poll registers a readiness sink per descriptor; syscall's overlapped
    // wrappers own the per-descriptor association and the per-operation records; and
    // internal/syscall/windows reads one property back, the operation's native address.
    //
    // Two of its requirements are NOT satisfied by the obvious implementation and were measured
    // rather than reasoned: create-EXACTLY-once (a bare ConcurrentDictionary.GetOrAdd runs its
    // factory on several threads and keeps one result, which for these consumers means native
    // allocations with no owner and, worse, a socket associated with a completion port twice), and
    // sink REPLACEMENT (the kernel reissues descriptor numbers after a close, so a registration that
    // refused to overwrite would wake a retired waiter). Both get a test below, and the first is a
    // real contention test rather than an assertion about intent.

    // Descriptors are per-test so parallel test execution cannot collide on the shared tables.
    private static int s_next = 0x5000;

    private static nuint nextDescriptor() => (nuint)Interlocked.Add(ref s_next, 1);

    private sealed class disposableState : IDisposable
    {
        internal int Disposals;

        public void Dispose() => Interlocked.Increment(ref Disposals);
    }

    private sealed class addressedState : IGoAsyncOperation
    {
        public nuint NativeAddress { get; init; }
    }

    [TestMethod]
    public void SignalReachesTheRegisteredSink()
    {
        nuint descriptor = nextDescriptor();
        List<nint> modes = new();

        GoAsyncIO.SetReadiness(descriptor, mode => { lock (modes) modes.Add(mode); });

        Assert.IsTrue(GoAsyncIO.Signal(descriptor, 'r'));
        Assert.IsTrue(GoAsyncIO.Signal(descriptor, 'w'));

        CollectionAssert.AreEqual(new List<nint> { 'r', 'w' }, modes);

        GoAsyncIO.RemoveDescriptor(descriptor);
    }

    [TestMethod]
    public void SignalForAnUnregisteredDescriptorIsASilentNoOp()
    {
        // Not politeness: Signal runs on an IO completion callback, where an escaping exception ends
        // the process, and a completion racing its own descriptor's close is a race the poller
        // contract permits.
        Assert.IsFalse(GoAsyncIO.Signal(nextDescriptor(), 'r'));
    }

    [TestMethod]
    public void ReadinessSinkIsReplaceableAndRemovable()
    {
        nuint descriptor = nextDescriptor();
        int first = 0, second = 0;

        GoAsyncIO.SetReadiness(descriptor, _ => first++);
        GoAsyncIO.Signal(descriptor, 'r');

        // REPLACEMENT is the load-bearing case: the kernel reissues descriptor numbers, so the
        // second registration must displace the first rather than being refused or queued behind it.
        GoAsyncIO.SetReadiness(descriptor, _ => second++);
        GoAsyncIO.Signal(descriptor, 'r');

        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);

        GoAsyncIO.SetReadiness(descriptor, null);
        Assert.IsFalse(GoAsyncIO.Signal(descriptor, 'r'));
        Assert.AreEqual(1, second);
    }

    [TestMethod]
    public void DescriptorStateIsCreatedExactlyOnceUnderContention()
    {
        // THE MEASURED REQUIREMENT. A bare GetOrAdd builds one object per racing thread and keeps
        // one; here that object is a completion-port association, and associating one socket twice
        // is a kernel error. This asserts the FACTORY ran once, not merely that one result was
        // handed back.
        nuint descriptor = nextDescriptor();
        int built = 0;
        object[] results = new object[32];

        using Barrier gate = new(results.Length);

        Parallel.For(0, results.Length, i => {
            gate.SignalAndWait();
            results[i] = GoAsyncIO.GetOrCreateDescriptorState(descriptor, () => {
                Interlocked.Increment(ref built);
                return new object();
            });
        });

        Assert.AreEqual(1, built);

        foreach (object result in results)
            Assert.AreSame(results[0], result);

        GoAsyncIO.RemoveDescriptor(descriptor);
    }

    [TestMethod]
    public void OperationStateIsCreatedExactlyOnceUnderContention()
    {
        // Same requirement one level down, and the one the prototype actually caught: a contention
        // run built TEN operation records where the contract wants one, each owning a preallocated
        // overlapped and native staging buffers — nine native leaks with no owner.
        object key = new();
        int built = 0;
        object[] results = new object[32];

        using Barrier gate = new(results.Length);

        Parallel.For(0, results.Length, i => {
            gate.SignalAndWait();
            results[i] = GoAsyncIO.GetOrCreateOperationState(key, () => {
                Interlocked.Increment(ref built);
                return new object();
            });
        });

        Assert.AreEqual(1, built);

        foreach (object result in results)
            Assert.AreSame(results[0], result);

        GoAsyncIO.RemoveOperationState(key);
    }

    [TestMethod]
    public void OperationStateIsKeyedByThePointersOwnEquality()
    {
        // The corpus keys operations by a ж<T> field reference, whose equality is (resolved source
        // pointer, field identity) rather than object identity — that is what makes three separate
        // mints of `&o.o` at three call sites resolve to ONE record. The table must therefore use the
        // KEY's equality, never ReferenceEquals.
        ж<pairOfInts> box = new(new pairOfInts());
        object first = GoAsyncIO.GetOrCreateOperationState(box.of(pairOfInts.Ꮡsecond), () => new object());
        object again = GoAsyncIO.GetOrCreateOperationState(box.of(pairOfInts.Ꮡsecond), () => new object());

        Assert.AreSame(first, again);

        // A DIFFERENT field of the same source is a different operation.
        object other = GoAsyncIO.GetOrCreateOperationState(box.of(pairOfInts.Ꮡfirst), () => new object());

        Assert.AreNotSame(first, other);

        GoAsyncIO.RemoveOperationState(box.of(pairOfInts.Ꮡsecond));
        GoAsyncIO.RemoveOperationState(box.of(pairOfInts.Ꮡfirst));
    }

    // A stand-in for the corpus's `operation` struct: two fields, so a field reference to each has
    // the same SOURCE and a different field identity.
    private struct pairOfInts
    {
        internal nint first;
        internal nint second;

        internal static ref nint Ꮡfirst(ref pairOfInts instance) => ref instance.first;
        internal static ref nint Ꮡsecond(ref pairOfInts instance) => ref instance.second;
    }

    [TestMethod]
    public void OperationAddressIsReportedOnlyByAnAddressedState()
    {
        object addressed = new();
        object plain = new();

        GoAsyncIO.GetOrCreateOperationState(addressed, () => new addressedState { NativeAddress = 0xDEAD });
        GoAsyncIO.GetOrCreateOperationState(plain, () => new object());

        // This one property is the ENTIRE contract between the package that issues an operation and
        // the package that harvests its result.
        Assert.IsTrue(GoAsyncIO.TryGetOperationAddress(addressed, out nuint address));
        Assert.AreEqual((nuint)0xDEAD, address);

        Assert.IsFalse(GoAsyncIO.TryGetOperationAddress(plain, out _));
        Assert.IsFalse(GoAsyncIO.TryGetOperationAddress(new object(), out _));

        GoAsyncIO.RemoveOperationState(addressed);
        GoAsyncIO.RemoveOperationState(plain);
    }

    [TestMethod]
    public void RemoveDescriptorRetiresBothTheSinkAndTheState()
    {
        nuint descriptor = nextDescriptor();
        disposableState state = new();
        int signals = 0;

        GoAsyncIO.SetReadiness(descriptor, _ => signals++);
        GoAsyncIO.GetOrCreateDescriptorState(descriptor, () => state);

        Assert.IsTrue(GoAsyncIO.TryGetDescriptorState(descriptor, out object? found));
        Assert.AreSame(state, found);

        // The waiter calls this from pollClose, BEFORE the descriptor itself is closed, so the
        // submitting package can still legally unregister from whatever it associated the descriptor
        // with. Disposal of the state is part of that contract.
        GoAsyncIO.RemoveDescriptor(descriptor);

        Assert.AreEqual(1, state.Disposals);
        Assert.IsFalse(GoAsyncIO.TryGetDescriptorState(descriptor, out _));
        Assert.IsFalse(GoAsyncIO.Signal(descriptor, 'r'));
        Assert.AreEqual(0, signals);
    }

    [TestMethod]
    public void RemoveDescriptorDoesNotBuildStateItIsAboutToDiscard()
    {
        // A descriptor that never submitted has no state to retire, and retiring it must not
        // MATERIALIZE one — every pollable FD reaches pollClose, including the ones that only ever
        // listened.
        nuint descriptor = nextDescriptor();

        GoAsyncIO.RemoveDescriptor(descriptor);

        Assert.IsFalse(GoAsyncIO.TryGetDescriptorState(descriptor, out _));
    }
}
