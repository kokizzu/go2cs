// GoAsyncIO.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace go.golib;

/// <summary>
/// The seam between a package that SUBMITS asynchronous OS operations and a package that WAITS for
/// them, for descriptor-based IO where the two cannot reference one another.
/// </summary>
/// <remarks>
/// <para>
/// WHY THIS LIVES IN GOLIB, WHICH IS THE WHOLE REASON IT EXISTS. Go's network poller is one package
/// (<c>internal/poll</c>) and its overlapped submissions are others (<c>syscall</c>,
/// <c>internal/syscall/windows</c>); <c>internal/poll</c> REFERENCES both, so a completion callback
/// raised on the submitting side has no legal way to call back into the waiter. Go closes that loop
/// with pointer arithmetic — its poller reads the enclosing <c>operation</c> back out of the
/// OVERLAPPED it dequeued — which go2cs cannot reproduce, because a <c>ж&lt;T&gt;</c> field
/// reference does not expose the object it was taken from. What remains is a PUSH through the one
/// assembly both sides see, which is this one. See
/// <c>docs/phase4/DESIGN-netpoll-managed-poller.md</c> §4.4.
/// </para>
/// <para>
/// It is deliberately PLATFORM-NEUTRAL and mechanism-neutral: a descriptor, a mode, an address, and
/// two opaque state slots. Naming Windows' completion machinery here would drag one platform's IO
/// model into the runtime library every converted program links. The submitting package owns the
/// mechanism; this type owns only the rendezvous.
/// </para>
/// <para>
/// THE DESCRIPTOR IS THE KEY because it is the one identity both sides independently hold — the
/// waiter receives it when it registers the descriptor, and a submitting wrapper receives it as its
/// own first argument. Nothing else in either package's signature is common to both.
/// </para>
/// </remarks>
public static class GoAsyncIO
{
    // Descriptor -> readiness sink. The sink takes the MODE the completion belongs to; the waiter's
    // own closure supplies everything else it needs.
    private static readonly ConcurrentDictionary<nuint, Action<nint>> s_sinks = new();

    // Descriptor -> the submitting package's per-descriptor state, opaque here.
    private static readonly ConcurrentDictionary<nuint, Lazy<object>> s_descriptorState = new();

    // Operation key -> the submitting package's per-operation state, opaque here. The key is
    // whatever pointer the WAITER names the operation by, which is why it is typed as `object`:
    // golib must not know what a `*Overlapped` is. Equality is the key's own — for a go2cs pointer
    // that is (source box, field identity), which is exactly the property that makes three separate
    // mints of `&o.o` at three call sites resolve to ONE record.
    private static readonly ConcurrentDictionary<object, Lazy<object>> s_operationState = new();

    /// <summary>
    /// Registers (or replaces, or with <c>null</c> removes) the readiness sink for
    /// <paramref name="descriptor"/>.
    /// </summary>
    /// <param name="descriptor">OS descriptor the sink speaks for.</param>
    /// <param name="sink">Called with the mode of each completion; <c>null</c> unregisters.</param>
    /// <remarks>
    /// REPLACEMENT IS REQUIRED, not merely tolerated: the kernel reissues descriptor numbers after a
    /// close, so a registration that refused to overwrite would leave a retired waiter being woken
    /// for a descriptor it no longer owns. Measured while prototyping this seam.
    /// </remarks>
    public static void SetReadiness(nuint descriptor, Action<nint>? sink)
    {
        if (sink is null)
            s_sinks.TryRemove(descriptor, out _);
        else
            s_sinks[descriptor] = sink;
    }

    /// <summary>
    /// Signals that an operation on <paramref name="descriptor"/> completed in
    /// <paramref name="mode"/>.
    /// </summary>
    /// <param name="descriptor">OS descriptor the completion belongs to.</param>
    /// <param name="mode">Mode of the completed operation, in the waiter's own vocabulary.</param>
    /// <returns><c>true</c> when a sink was registered and ran.</returns>
    /// <remarks>
    /// A signal for an UNREGISTERED descriptor is a silent no-op by contract, for a reason that is
    /// not politeness: this runs on an IO completion callback, where an escaping exception ends the
    /// process, and a completion racing its own descriptor's close is a race the poller contract
    /// permits.
    /// </remarks>
    public static bool Signal(nuint descriptor, nint mode)
    {
        if (!s_sinks.TryGetValue(descriptor, out Action<nint>? sink))
            return false;

        sink(mode);

        return true;
    }

    /// <summary>
    /// Gets the submitting package's state for <paramref name="descriptor"/>, creating it EXACTLY
    /// once.
    /// </summary>
    /// <param name="descriptor">OS descriptor the state belongs to.</param>
    /// <param name="factory">Builds the state on the one thread that wins the race.</param>
    /// <returns>The one state object for this descriptor.</returns>
    /// <remarks>
    /// EXACTLY-ONCE IS LOAD-BEARING AND <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,
    /// Func{TKey,TValue})"/> DOES NOT PROVIDE IT. Its factory may run on several threads with only
    /// one result kept; a contention test over the bare form built ten operation records where the
    /// contract wants one, and each discarded record owned native allocations with no owner left to
    /// free them. Per-descriptor state has the same requirement for a stronger reason still — on
    /// Windows that object is the completion-port association, and associating one socket twice is a
    /// kernel error. <see cref="Lazy{T}"/> with
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> is the shape that actually holds.
    /// </remarks>
    public static object GetOrCreateDescriptorState(nuint descriptor, Func<object> factory) =>
        s_descriptorState.GetOrAdd(descriptor, _ => new Lazy<object>(factory, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    /// <summary>
    /// Gets the submitting package's state for <paramref name="descriptor"/> without creating it.
    /// </summary>
    /// <param name="descriptor">OS descriptor to look up.</param>
    /// <param name="state">The state, when one exists.</param>
    /// <returns><c>true</c> when a state object exists for this descriptor.</returns>
    public static bool TryGetDescriptorState(nuint descriptor, out object? state)
    {
        state = s_descriptorState.TryGetValue(descriptor, out Lazy<object>? lazy) ? lazy.Value : null;

        return state is not null;
    }

    /// <summary>
    /// Retires <paramref name="descriptor"/>: removes its readiness sink and its state, disposing
    /// the state when it is disposable.
    /// </summary>
    /// <param name="descriptor">OS descriptor to retire.</param>
    /// <remarks>
    /// Called by the WAITER when it stops owning the descriptor, which on the netpoll path is
    /// <c>pollClose</c> — deliberately BEFORE the descriptor itself is closed, so a submitting
    /// package can unregister from whatever OS machinery it associated the descriptor with while
    /// that association is still legal. Disposal never closes the descriptor; that stays the
    /// waiter's throughout.
    /// </remarks>
    public static void RemoveDescriptor(nuint descriptor)
    {
        s_sinks.TryRemove(descriptor, out _);

        if (s_descriptorState.TryRemove(descriptor, out Lazy<object>? lazy) && lazy.IsValueCreated)
            (lazy.Value as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Gets the submitting package's state for one operation, creating it EXACTLY once.
    /// </summary>
    /// <param name="key">Pointer the operation is named by, on both sides.</param>
    /// <param name="factory">Builds the state on the one thread that wins the race.</param>
    /// <returns>The one state object for this operation.</returns>
    /// <remarks>See <see cref="GetOrCreateDescriptorState"/> for why this is not a bare
    /// <c>GetOrAdd</c>.</remarks>
    public static object GetOrCreateOperationState(object key, Func<object> factory) =>
        s_operationState.GetOrAdd(key, _ => new Lazy<object>(factory, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    /// <summary>
    /// Gets the submitting package's state for one operation without creating it.
    /// </summary>
    /// <param name="key">Pointer the operation is named by.</param>
    /// <param name="state">The state, when one exists.</param>
    /// <returns><c>true</c> when a state object exists for this operation.</returns>
    public static bool TryGetOperationState(object key, out object? state)
    {
        state = s_operationState.TryGetValue(key, out Lazy<object>? lazy) ? lazy.Value : null;

        return state is not null;
    }

    /// <summary>
    /// Removes one operation's state.
    /// </summary>
    /// <param name="key">Pointer the operation is named by.</param>
    /// <returns>The removed state, or <c>null</c> when there was none.</returns>
    public static object? RemoveOperationState(object key) =>
        s_operationState.TryRemove(key, out Lazy<object>? lazy) && lazy.IsValueCreated ? lazy.Value : null;

    /// <summary>
    /// Reports the native address of the operation named by <paramref name="key"/>, when its state
    /// exposes one.
    /// </summary>
    /// <param name="key">Pointer the operation is named by.</param>
    /// <param name="address">The operation's native address.</param>
    /// <returns><c>true</c> when an address is available.</returns>
    /// <remarks>
    /// This is the ENTIRE contract between two SUBMITTING packages that must agree on one
    /// in-flight operation — the one that issues it and the one that harvests its result. Narrowing
    /// it to an address is what keeps the harvest calling the real OS routine against the real
    /// control block, instead of re-deriving the OS's own error mapping from whatever a completion
    /// callback happened to report.
    /// </remarks>
    public static bool TryGetOperationAddress(object key, out nuint address)
    {
        address = TryGetOperationState(key, out object? state) && state is IGoAsyncOperation operation
            ? operation.NativeAddress
            : 0;

        return address != 0;
    }
}

/// <summary>
/// Implemented by a submitting package's per-operation state to expose the operation's native
/// control block to the package that harvests its result.
/// </summary>
public interface IGoAsyncOperation
{
    /// <summary>
    /// Address of the operation's native control block, or 0 when none is currently allocated.
    /// </summary>
    nuint NativeAddress { get; }
}
